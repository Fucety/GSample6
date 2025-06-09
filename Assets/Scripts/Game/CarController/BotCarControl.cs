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
        [SerializeField, Range(0.3f, 1f)] private float aggressionLevel = 0.7f;
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
            navMeshAgent.speed = 15f;
            navMeshAgent.acceleration = 20f;
            navMeshAgent.angularSpeed = 240f;
            navMeshAgent.radius = 1.0f;
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
            pathUpdateTimerInternal -= Time.deltaTime;
            if (pathUpdateTimerInternal > 0f && currentState != BotState.SeekPoint && targetPosition != Vector3.zero) return;

            pathUpdateTimerInternal = pathUpdateInterval;

            if (bonusHandler != null && !bonusHandler.HasBonus)
            {
                Collider nearestBonus = FindNearestCollider(bonusLayer, visionRadius);
                if (nearestBonus != null)
                {
                    // Отключаем NavMeshAgent при обнаружении бонуса, как и в режиме точки
                    if (navMeshAgent.enabled) navMeshAgent.enabled = false;
                    SetNewTarget(nearestBonus.transform.position, BotState.SeekBonus);
                    return;
                }
            }

            if (bonusHandler != null && (bonusHandler.HasBonus || Random.value < aggressionLevel * 0.2f))
            {
                Collider nearestRival = FindNearestCollider(rivalLayer, visionRadius, gameObject);
                if (nearestRival != null)
                {
                    SetNewTarget(nearestRival.transform.position, BotState.SeekRival);
                    return;
                }
            }

            if (currentState != BotState.SeekPoint || !IsTargetValid(targetPosition) || Vector3.Distance(transform.position, targetPosition) < targetReachDistance)
            {
                SetNewTarget(GetRandomPoint(), BotState.SeekPoint);
            }
            else if (navMeshAgent.enabled && (!navMeshAgent.hasPath || navMeshAgent.isPathStale))
            {
                RecalculatePathToTarget();
            }
        }

        /// Устанавливает новую цель и обновляет состояние движения.
        private void SetNewTarget(Vector3 newPos, BotState newState)
        {
            targetPosition = newPos;
            if (currentState != newState || Vector3.Distance(targetPosition, navMeshAgent.destination) > 1f)
            {
                currentState = newState;
                RecalculatePathToTarget();
            }
        }

        /// Осуществляет движение к текущей цели.
        private void MoveToCurrentTarget()
        {
            if (Vector3.Distance(transform.position, targetPosition) < targetReachDistance)
            {
                if (currentState == BotState.SeekRival && bonusHandler != null && bonusHandler.HasBonus)
                {
                    UseBonus(true);
                }
                pathUpdateTimerInternal = -1f;
                return;
            }

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
                    if (navMeshAgent.enabled) navMeshAgent.ResetPath();
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
                currentState = BotState.SeekPoint;
                targetPosition = GetRandomPoint();
                RecalculatePathToTarget();
                return;
            }

            if (navMeshAgent.enabled) navMeshAgent.enabled = false;

            RaycastHit hitFwd, hitLeft, hitRight;
            bool obsFwd = Physics.Raycast(transform.position + transform.up * 0.5f, transform.forward, out hitFwd, avoidDistance, obstacleLayer | rivalLayer);
            bool obsLeft = Physics.Raycast(transform.position + transform.up * 0.5f, Quaternion.Euler(0, -avoidAngle, 0) * transform.forward, out hitLeft, avoidDistance * 0.8f, obstacleLayer | rivalLayer);
            bool obsRight = Physics.Raycast(transform.position + transform.up * 0.5f, Quaternion.Euler(0, avoidAngle, 0) * transform.forward, out hitRight, avoidDistance * 0.8f, obstacleLayer | rivalLayer);

            float steer = 0;
            float throttle = aggressionLevel * 0.6f;
            isReversing = false;

            if (obsFwd || (obsLeft && obsRight))
            {
                isReversing = true;
                throttle = reverseThrottle;
                if (obsLeft && !obsRight) steer = 1f;
                else if (obsRight && !obsLeft) steer = -1f;
                else steer = (Random.value > 0.5f) ? 1f : -1f;
            }
            else if (obsLeft) steer = 1f;
            else if (obsRight) steer = -1f;

            _carController.SteerInput = steer;
            currentSteerInput = steer;
            _carController.ThrottleInput = throttle;
            _carController.BrakeInput = 0f;
            _carController.Reverse = isReversing;
        }

        #endregion

        #region Ввод и столкновения

        /// Применяет входные данные для управления автомобилем.
        private void ApplyCarInputs()
        {
            if (currentState == BotState.Avoid) return;

            if (!navMeshAgent.enabled) navMeshAgent.enabled = true;
            if (!navMeshAgent.isOnNavMesh) { TryWarpToNavMesh(); return; }

            if (!navMeshAgent.hasPath || navMeshAgent.pathPending)
            {
                _carController.ThrottleInput = 0f;
                _carController.SteerInput = 0f;
                _carController.BrakeInput = 0.1f;
                _carController.Reverse = false;
                return;
            }

            Vector3 steeringTargetPos = navMeshAgent.steeringTarget;
            Vector3 dirToSteeringTarget = (steeringTargetPos - transform.position).normalized;
            float angleToTarget = Vector3.SignedAngle(transform.forward, dirToSteeringTarget, Vector3.up);

            float steer = Mathf.Clamp(angleToTarget / maxSteerAngle, -1f, 1f);
            currentSteerInput = Mathf.SmoothDamp(currentSteerInput, steer, ref steerVelocity, steerSmoothTime);

            float throttle = aggressionLevel;
            if (Mathf.Abs(angleToTarget) > 30f) throttle *= 0.6f;

            float brake = 0f;
            if (navMeshAgent.remainingDistance < navMeshAgent.stoppingDistance + targetReachDistance)
            {
                throttle *= Mathf.Clamp01(navMeshAgent.remainingDistance / (targetReachDistance + 0.1f));
                brake = 0.3f;
            }

            _carController.ThrottleInput = throttle;
            _carController.SteerInput = currentSteerInput;
            _carController.BrakeInput = brake;
            _carController.Reverse = false;

            if (currentState == BotState.SeekRival && bonusHandler != null && bonusHandler.HasBonus)
            {
                UseBonus(false);
            }
        }

        /// Обрабатывает столкновения и включает режим избегания.
        private void OnCollisionEnter(Collision collision)
        {
            if (currentState == BotState.Avoid && avoidTimer > reverseTimeOnCollision * 0.5f) return;

            bool isSignificantCollision = ((1 << collision.gameObject.layer) & (obstacleLayer | rivalLayer)) != 0;
            if (isSignificantCollision && collision.gameObject != gameObject)
            {
                currentState = BotState.Avoid;
                avoidTimer = reverseTimeOnCollision;
                if (navMeshAgent.enabled) navMeshAgent.ResetPath();

                isReversing = true;
                _carController.ThrottleInput = reverseThrottle;

                Vector3 relativeContact = transform.InverseTransformPoint(collision.contacts[0].point);
                _carController.SteerInput = (relativeContact.x > 0) ? -1f : 1f;
                currentSteerInput = _carController.SteerInput;

                _carController.Reverse = isReversing;
            }
        }

        /// Использует бонус, если он доступен.
        private void UseBonus(bool forceUse)
        {
            if (bonusHandler != null && bonusHandler.HasBonus && forceUse)
            {
                bonusHandler.ActivateBonus();
                pathUpdateTimerInternal = -1f;
            }
        }

        #endregion

        #region Вспомогательные

        /// Ищет ближайший коллайдер в заданном радиусе и слое.
        private Collider FindNearestCollider(LayerMask layer, float radius, GameObject selfToIgnore = null)
        {
            return Physics.OverlapSphere(transform.position, radius, layer)
                .Where(c => (selfToIgnore == null || c.gameObject != selfToIgnore) && 
                            !Physics.Linecast(transform.position + transform.up * 0.5f, c.bounds.center, obstacleLayer))
                .OrderBy(c => Vector3.Distance(transform.position, c.transform.position))
                .FirstOrDefault();
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