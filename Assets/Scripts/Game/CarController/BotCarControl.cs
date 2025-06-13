using UnityEngine;
using UnityEngine.AI;
using System.Linq;

namespace UshiSoft.UACPF
{
    public class BotCarControl : DriverBase
    {
        #region Поля

        private enum BotState
        {
            SeekPoint,  // Движение к контрольной точке
            SeekBonus,  // Движение к бонусу
            SeekRival,  // Движение к сопернику
            Avoid       // Объезд препятствия или машины
        }

        [Header("Core Setup")]
        [SerializeField] private NavMeshAgent navMeshAgent;
        [SerializeField] private LayerMask obstacleLayer;
        [SerializeField] private LayerMask bonusLayer;
        [SerializeField] private LayerMask rivalLayer;

        [Header("Behavior")]
        [SerializeField] private float visionRadius = 20f;
        [SerializeField] private float targetReachDistance = 3f;
        [SerializeField, Range(0.3f, 1f)] private float aggressionLevel = 0.7f; // Возможно, этот параметр можно будет использовать для других аспектов, но не для выбора цели
        [SerializeField] private float maxSteerAngle = 40f;

        [Header("Avoidance")]
        [SerializeField] private float avoidDistance = 6f;
        [SerializeField] private float avoidAngle = 45f;
        [SerializeField] private float avoidTime = 1.2f;
        [SerializeField] private float reverseTimeOnCollision = 0.6f;
        [SerializeField] private float reverseThrottle = 0.5f;

        [Header("Misc")]
        [SerializeField] private float pathUpdateInterval = 0.5f;
        [SerializeField] private float steerSmoothTime = 0.15f;
        [SerializeField] private float startDelay = 0.2f;
        [SerializeField] private float pointSearchRadius = 25f;

        [Header("Debug Gizmos")]
        [SerializeField] private bool showTargetGizmo = true;
        [SerializeField] private bool showDistanceGizmos = true;

        private BotState currentState;
        private Vector3 targetPosition;
        private float avoidTimer;
        private float startTimer;
        private float pathUpdateTimerInternal;
        private float currentSteerInput;
        private float steerVelocity;
        private bool isReversing;
        private BonusHandler bonusHandler;

        #endregion

        #region Жизненный цикл

        /// Инициализация компонента при запуске.
        protected override void Awake()
        {
            base.Awake();
            bonusHandler = GetComponent<BonusHandler>();
            if (bonusHandler == null) Debug.LogError("BonusHandler не найден!", this);
            InitializeNavMeshAgent();
            startTimer = startDelay;
            RespawnAndSetInitialTarget();
        }

        /// <summary>
        /// Сбрасывает состояние AI, вызывается после респавна.
        /// </summary>
        public void Respawn()
        {
            // Этот метод сбрасывает навигацию и ищет новую цель,
            // что и нужно после телепортации на спавнпоинт.
            RespawnAndSetInitialTarget();
        }

        #endregion

        #region Навигация

        /// Инициализирует настройки NavMeshAgent.
        private void InitializeNavMeshAgent()
        {
            if (navMeshAgent == null) navMeshAgent = GetComponent<NavMeshAgent>();
            if (navMeshAgent == null) { Debug.LogError("NavMeshAgent не найден!", this); enabled = false; return; }

            navMeshAgent.updatePosition = false;
            navMeshAgent.updateRotation = false;
            navMeshAgent.stoppingDistance = targetReachDistance * 0.5f;
            navMeshAgent.speed = 0f;
            navMeshAgent.acceleration = 50f;
            navMeshAgent.angularSpeed = 360f;
            navMeshAgent.radius = 1.5f;
            navMeshAgent.autoRepath = false;
        }

        /// Перенаправляет автомобиль и задаёт первоначальную цель.
        private void RespawnAndSetInitialTarget()
        {
            if (navMeshAgent != null && navMeshAgent.isOnNavMesh) navMeshAgent.Warp(transform.position);
            targetPosition = GetRandomPoint();
            currentState = BotState.SeekPoint;
            RecalculatePathToTarget();
        }

        /// Пересчитывает путь к текущей цели.
        private void RecalculatePathToTarget()
        {
            if (!navMeshAgent.enabled) navMeshAgent.enabled = true;
            if (!navMeshAgent.isOnNavMesh) { if (!TryWarpToNavMesh()) return; }

            if (!navMeshAgent.SetDestination(targetPosition))
            {
                targetPosition = GetRandomPoint();
                navMeshAgent.SetDestination(targetPosition);
            }
        }

        /// Пытается переместить автомобиль на NavMesh, если он вне ее.
        private bool TryWarpToNavMesh()
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                navMeshAgent.Warp(hit.position);
                if (!navMeshAgent.enabled) navMeshAgent.enabled = true;
                return true;
            }
            return false;
        }

        /// Получает случайную точку на NavMesh для задания цели.
        private Vector3 GetRandomPoint()
        {
            for (int i = 0; i < 10; i++)
            {
                Vector3 randomDir = transform.position + Random.insideUnitSphere * pointSearchRadius;
                if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, pointSearchRadius, NavMesh.AllAreas) &&
                    Vector3.Distance(hit.position, transform.position) > targetReachDistance)
                {
                    return hit.position;
                }
            }
            if (NavMesh.SamplePosition(transform.position + transform.forward * 10f, out NavMeshHit fwdHit, 10f, NavMesh.AllAreas))
                return fwdHit.position;
            return transform.position;
        }

        /// Проверяет, является ли указанная точка корректной для перемещения.
        private bool IsTargetValid(Vector3 position)
        {
            if (Vector3.Distance(transform.position, position) < targetReachDistance * 0.5f) return false;
            if (!navMeshAgent.enabled) navMeshAgent.enabled = true;
            bool isValid = navMeshAgent.CalculatePath(position, new NavMeshPath());
            return isValid;
        }

        #endregion

        #region Движение

        /// Основной метод управления движением автомобиля.
        protected override void Drive()
        {
            if (GameManager.Instance.State != GameState.Fighting || navMeshAgent == null) return;

            if (startTimer > 0f)
            {
                startTimer -= Time.deltaTime;
                _carController.ThrottleInput = 0f;
                _carController.SteerInput = 0f;
                _carController.BrakeInput = 1f;
                _carController.Reverse = false;
                return;
            }

            isReversing = false;

            if (currentState == BotState.Avoid)
            {
                UpdateAvoid();
            }
            else
            {
                if (CheckForImmediateObstacles())
                {
                    UpdateAvoid();
                }
                else
                {
                    UpdateStateAndTarget(); 
                    MoveToCurrentTarget();
                }
            }
            ApplyCarInputs();
        }

        /// Обновляет состояние и цель в зависимости от условий игры.
        private void UpdateStateAndTarget()
        {
            // Убеждаемся, что мы не находимся в состоянии "избегания", так как оно имеет приоритет
            if (currentState == BotState.Avoid) return;

            // Обновляем таймер для частоты пересчёта пути
            pathUpdateTimerInternal -= Time.deltaTime;
            // Если таймер не истёк и текущее состояние не SeekPoint (т.е. мы целенаправленно движемся к бонусу/врагу)
            // И если у нас уже есть цель, то ждём истечения таймера.
            if (pathUpdateTimerInternal > 0f && currentState != BotState.SeekPoint && targetPosition != Vector3.zero) return;

            // Сбрасываем таймер после его истечения или если меняем состояние
            pathUpdateTimerInternal = pathUpdateInterval;

            // --- НОВАЯ ЛОГИКА ПРИОРИТЕТОВ ---

            // 1. Если у бота есть бонус, ищем ближайшего соперника.
            if (bonusHandler != null && bonusHandler.HasBonus)
            {
                // Ищем соперника в радиусе видимости
                Collider nearestRival = FindNearestCollider(rivalLayer, visionRadius, gameObject);
                if (nearestRival != null)
                {
                    // Найдена цель-соперник, устанавливаем её и переходим в режим преследования
                    SetNewTarget(nearestRival.transform.position, BotState.SeekRival);
                    // NavMeshAgent должен быть включен для преследования по пути
                    if (!navMeshAgent.enabled) navMeshAgent.enabled = true;
                    return; // Важно: если нашли соперника, выходим из метода, чтобы не перебивать цель
                }
                // Если бонуса нет, но бот не видит соперника, он должен вернуться к поиску точек.
                // Эту часть мы обработаем в блоке по умолчанию, если соперник не найден.
            }

            // 2. Если у бота нет бонуса, ищем ближайший бонус.
            // Это условие будет проверено, только если у нас нет бонуса ИЛИ мы не нашли соперника (из блока выше)
            if (bonusHandler != null && !bonusHandler.HasBonus)
            {
                Collider nearestBonus = FindNearestCollider(bonusLayer, visionRadius);
                if (nearestBonus != null)
                {
                    // Найдена цель-бонус, устанавливаем её и переходим в режим поиска бонуса
                    SetNewTarget(nearestBonus.transform.position, BotState.SeekBonus);
                    // Для движения к бонусу по прямой (если это предполагается) NavMeshAgent можно отключить,
                    // как было в оригинале, но если бонус может быть за препятствием, лучше оставить NavMesh.
                    // В данном случае, оставим его включенным для надёжной навигации по NavMesh.
                    if (!navMeshAgent.enabled) navMeshAgent.enabled = true;
                    return; // Важно: если нашли бонус, выходим
                }
            }

            // 3. Поведение по умолчанию: если ни бонус, ни соперник не найдены (или у бота есть бонус, но нет соперника),
            // бот движется к случайной точке.
            // Это условие также сработает, если текущая цель достигнута или невалидна,
            // или если путь NavMeshAgent устарел/не существует.
            if (currentState != BotState.SeekPoint || !IsTargetValid(targetPosition) || 
                Vector3.Distance(transform.position, targetPosition) < targetReachDistance ||
                (navMeshAgent.enabled && (!navMeshAgent.hasPath || navMeshAgent.isPathStale || navMeshAgent.pathPending))) 
            {
                SetNewTarget(GetRandomPoint(), BotState.SeekPoint);
                if (!navMeshAgent.enabled) navMeshAgent.enabled = true; // Убеждаемся, что NavMeshAgent включен для SeekPoint
            }
            // else { // Если текущая цель валидна и путь не устарел, продолжаем двигаться к ней.
            //     // В этом случае ничего не делаем, текущая цель сохраняется.
            // }
        }

        /// Устанавливает новую цель и обновляет состояние движения.
        private void SetNewTarget(Vector3 newPos, BotState newState)
        {
            // Проверяем, действительно ли цель или состояние изменились, чтобы избежать лишних пересчётов пути.
            // Также учитываем случай, когда NavMeshAgent мог быть отключен (например, в состоянии Avoid).
            bool targetChanged = Vector3.Distance(targetPosition, newPos) > 0.1f; // Небольшой допуск для float сравнений
            bool stateChanged = currentState != newState;
            
            targetPosition = newPos;
            currentState = newState;

            // Всегда пересчитываем путь, если цель или состояние изменились, 
            // или если NavMeshAgent был отключен и теперь его нужно снова включить.
            if (targetChanged || stateChanged || !navMeshAgent.enabled) 
            {
                RecalculatePathToTarget();
            }
        }

        /// Осуществляет движение к текущей цели.
        private void MoveToCurrentTarget()
        {
            // Если бот уже очень близко к цели
            if (Vector3.Distance(transform.position, targetPosition) < targetReachDistance)
            {
                // Если это цель - соперник и у нас есть бонус, используем его
                if (currentState == BotState.SeekRival && bonusHandler != null && bonusHandler.HasBonus)
                {
                    UseBonus(true); // Принудительное использование бонуса
                }
                // Независимо от того, был ли использован бонус или просто достигнута точка,
                // сбрасываем таймер обновления пути, чтобы на следующем шаге `UpdateStateAndTarget`
                // сразу же выбрал новую цель.
                pathUpdateTimerInternal = -1f; 
                return; // Выходим, так как цель достигнута
            }

            // Если NavMeshAgent включен, но путь устарел, отсутствует или ведёт не к той цели
            if (navMeshAgent.enabled && (!navMeshAgent.hasPath || navMeshAgent.isPathStale || Vector3.Distance(navMeshAgent.destination, targetPosition) > 0.5f))
            {
                RecalculatePathToTarget();
            }
        }

        #endregion

        #region Избегание

        /// Проверяет наличие препятствий для немедленного избегания столкновений.
        private bool CheckForImmediateObstacles()
        {
            Vector3[] rayDirs = {
                transform.forward,
                Quaternion.Euler(0, avoidAngle, 0) * transform.forward,
                Quaternion.Euler(0, -avoidAngle, 0) * transform.forward
            };

            foreach (var dir in rayDirs)
            {
                if (Physics.Raycast(transform.position + transform.up * 0.5f, dir, out RaycastHit hit, avoidDistance, obstacleLayer | rivalLayer))
                {
                    if (hit.collider.gameObject == gameObject) continue;

                    currentState = BotState.Avoid;
                    avoidTimer = avoidTime;
                    if (navMeshAgent.enabled) navMeshAgent.ResetPath(); // Сбрасываем текущий путь NavMeshAgent
                    return true;
                }
            }
            return false;
        }

        /// Обновляет маневры для обхода препятствий.
        private void UpdateAvoid()
        {
            avoidTimer -= Time.deltaTime;
            if (avoidTimer <= 0f)
            {
                currentState = BotState.SeekPoint; // Возвращаемся к поиску точки после избегания
                targetPosition = GetRandomPoint();
                RecalculatePathToTarget();
                return;
            }

            if (navMeshAgent.enabled) navMeshAgent.enabled = false; // Отключаем NavMeshAgent для ручного управления во время объезда

            // Лучи для определения препятствий вокруг машины
            RaycastHit hitFwd, hitLeft, hitRight;
            bool obsFwd = Physics.Raycast(transform.position + transform.up * 0.5f, transform.forward, out hitFwd, avoidDistance, obstacleLayer | rivalLayer);
            bool obsLeft = Physics.Raycast(transform.position + transform.up * 0.5f, Quaternion.Euler(0, -avoidAngle, 0) * transform.forward, out hitLeft, avoidDistance * 0.8f, obstacleLayer | rivalLayer);
            bool obsRight = Physics.Raycast(transform.position + transform.up * 0.5f, Quaternion.Euler(0, avoidAngle, 0) * transform.forward, out hitRight, avoidDistance * 0.8f, obstacleLayer | rivalLayer);

            float steer = 0;
            float throttle = aggressionLevel * 0.6f; // Уменьшенный газ при избегании
            isReversing = false;

            if (obsFwd || (obsLeft && obsRight)) // Если прямо или с обоих боков препятствие
            {
                isReversing = true;
                throttle = reverseThrottle; // Задний ход
                if (obsLeft && !obsRight) steer = 1f; // Рулим вправо, если слева препятствие
                else if (obsRight && !obsLeft) steer = -1f; // Рулим влево, если справа препятствие
                else steer = (Random.value > 0.5f) ? 1f : -1f; // Случайное направление, если везде тупик
            }
            else if (obsLeft) steer = 1f; // Рулим вправо, если слева препятствие
            else if (obsRight) steer = -1f; // Рулим влево, если справа препятствие

            _carController.SteerInput = steer;
            currentSteerInput = steer; // Обновляем текущий вход руления
            _carController.ThrottleInput = throttle;
            _carController.BrakeInput = 0f;
            _carController.Reverse = isReversing;
        }

        #endregion

        #region Ввод и столкновения

        /// Применяет входные данные для управления автомобилем.
        private void ApplyCarInputs()
        {
            if (currentState == BotState.Avoid) return; // Управление в режиме избегания происходит в UpdateAvoid()

            // Убеждаемся, что NavMeshAgent включен, если он был отключен (например, после Avoid)
            if (!navMeshAgent.enabled) navMeshAgent.enabled = true;
            // Если бот не на NavMesh, пытаемся переместить его
            if (!navMeshAgent.isOnNavMesh) { TryWarpToNavMesh(); return; }

            // Если у NavMeshAgent нет пути или он ещё вычисляется
            if (!navMeshAgent.hasPath || navMeshAgent.pathPending)
            {
                _carController.ThrottleInput = 0f;
                _carController.SteerInput = 0f;
                _carController.BrakeInput = 0.1f; // Немного тормозим
                _carController.Reverse = false;
                return;
            }

            // Вычисляем направление к следующей точке на пути NavMeshAgent
            Vector3 steeringTargetPos = navMeshAgent.steeringTarget;
            Vector3 dirToSteeringTarget = (steeringTargetPos - transform.position).normalized;
            float angleToTarget = Vector3.SignedAngle(transform.forward, dirToSteeringTarget, Vector3.up);

            // Вычисляем вход руления, ограничиваем его и плавно интерполируем
            float steer = Mathf.Clamp(angleToTarget / maxSteerAngle, -1f, 1f);
            currentSteerInput = Mathf.SmoothDamp(currentSteerInput, steer, ref steerVelocity, steerSmoothTime);

            // Вычисляем вход газа
            float throttle = aggressionLevel;
            // Уменьшаем газ при крутых поворотах для лучшего контроля
            if (Mathf.Abs(angleToTarget) > 30f) throttle *= 0.6f;

            float brake = 0f;
            // Приближаемся к цели: уменьшаем газ и слегка тормозим для плавного подхода
            if (navMeshAgent.remainingDistance < navMeshAgent.stoppingDistance + targetReachDistance)
            {
                throttle *= Mathf.Clamp01(navMeshAgent.remainingDistance / (targetReachDistance + 0.1f));
                brake = 0.3f;
            }

            // Применяем вычисленные вводы к контроллеру машины
            _carController.ThrottleInput = throttle;
            _carController.SteerInput = currentSteerInput;
            _carController.BrakeInput = brake;
            _carController.Reverse = false;

            // Дополнительная логика: если бот преследует соперника и у него есть бонус, пытается использовать его (без принуждения)
            if (currentState == BotState.SeekRival && bonusHandler != null && bonusHandler.HasBonus)
            {
                UseBonus(false); // Не принудительное использование, возможно, зависит от других условий
            }
        }

        /// Обрабатывает столкновения и включает режим избегания.
        private void OnCollisionEnter(Collision collision)
        {
            // Игнорируем столкновения, если бот уже в процессе избегания и не прошло достаточно времени после последнего реверса
            if (currentState == BotState.Avoid && avoidTimer > reverseTimeOnCollision * 0.5f) return;

            // Проверяем, является ли столкновение значимым (с препятствием или соперником)
            bool isSignificantCollision = ((1 << collision.gameObject.layer) & (obstacleLayer | rivalLayer)) != 0;
            if (isSignificantCollision && collision.gameObject != gameObject) // Убеждаемся, что не столкнулись сами с собой
            {
                currentState = BotState.Avoid; // Переходим в режим избегания
                avoidTimer = reverseTimeOnCollision; // Устанавливаем таймер для реверса
                if (navMeshAgent.enabled) navMeshAgent.ResetPath(); // Сбрасываем текущий путь NavMeshAgent

                isReversing = true;
                _carController.ThrottleInput = reverseThrottle; // Включаем задний ход

                // Определяем относительную позицию точки столкновения для руления в противоположную сторону
                Vector3 relativeContact = transform.InverseTransformPoint(collision.contacts[0].point);
                _carController.SteerInput = (relativeContact.x > 0) ? -1f : 1f; // Рулим в сторону, противоположную точке столкновения
                currentSteerInput = _carController.SteerInput;

                _carController.Reverse = isReversing;
            }
        }

        /// Использует бонус, если он доступен.
        private void UseBonus(bool forceUse)
        {
            // Используем бонус, если он есть и либо `forceUse` истинно (достигли цели-соперника),
            // либо текущее состояние - SeekRival (т.е. мы активно преследуем врага)
            if (bonusHandler != null && bonusHandler.HasBonus && (forceUse || currentState == BotState.SeekRival))
            {
                bonusHandler.ActivateBonus();
                pathUpdateTimerInternal = -1f; // Сбрасываем таймер для немедленного перерасчета цели после использования бонуса
            }
        }

        #endregion

        #region Вспомогательные

        /// Ищет ближайший коллайдер в заданном радиусе и слое.
        private Collider FindNearestCollider(LayerMask layer, float radius, GameObject selfToIgnore = null)
        {
            return Physics.OverlapSphere(transform.position, radius, layer) // Получаем все коллайдеры в сфере
                .Where(c => (selfToIgnore == null || c.gameObject != selfToIgnore) && // Исключаем самого себя
                            !Physics.Linecast(transform.position + transform.up * 0.5f, c.bounds.center, obstacleLayer)) // Проверяем видимость
                .OrderBy(c => Vector3.Distance(transform.position, c.transform.position)) // Сортируем по расстоянию
                .FirstOrDefault(); // Возвращаем ближайший
        }

        #endregion

#if UNITY_EDITOR
        #region Отладка

        /// Рисует Gizmos для отладки в редакторе.
        private void OnDrawGizmosSelected()
        {
            if (showTargetGizmo && targetPosition != Vector3.zero)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, targetPosition + Vector3.up * 0.5f);
                Gizmos.DrawSphere(targetPosition + Vector3.up * 0.5f, 0.5f);
            }

            if (showDistanceGizmos)
            {
                Gizmos.color = new Color(0f, 1f, 0.5f, 0.2f);
                Gizmos.DrawWireSphere(transform.position, visionRadius);

                Gizmos.color = new Color(1f, 0.7f, 0f, 0.2f);
                Gizmos.DrawWireSphere(transform.position, pointSearchRadius);

                Gizmos.color = Color.red;
                Vector3 up = Vector3.up * 0.5f;
                Vector3[] rayDirs = {
                    transform.forward,
                    Quaternion.Euler(0, avoidAngle, 0) * transform.forward,
                    Quaternion.Euler(0, -avoidAngle, 0) * transform.forward
                };
                foreach (var dir in rayDirs)
                {
                    Gizmos.DrawRay(transform.position + up, dir * avoidDistance);
                }
            }
        }

        #endregion
#endif
    }
}