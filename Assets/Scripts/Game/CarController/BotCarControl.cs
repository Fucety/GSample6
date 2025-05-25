using UnityEngine;
using UnityEngine.AI;

namespace UshiSoft.UACPF
{
    public class BotCarControl : DriverBase
    {
        [SerializeField] private NavMeshAgent navMeshAgent; // Агент навигации
        [SerializeField, Range(0f, 1f)] private float bonusUseChance = 0.5f; // Шанс активации бонуса
        [SerializeField, Range(0.5f, 1f)] private float aggressionLevel = 0.7f; // Уровень агрессии
        [SerializeField] private float avoidanceDistance = 5f; // Дистанция Raycast
        [SerializeField] private LayerMask obstacleLayer; // Слой для Raycast
        [SerializeField] private float backupTime = 1f; // Время движения назад
        [SerializeField] private float steerAngle = 1f; // Сила поворота
        [SerializeField] private float waypointWidth = 4f; // Ширина прямоугольника waypoint
        [SerializeField] private float waypointLength = 4f; // Длина прямоугольника waypoint
        [SerializeField] private float lookAheadDistance = 2f; // Дистанция направляющей
        [SerializeField] private float startDelay = 0.2f; // Задержка старта

        private Transform[] waypoints; // Точки пути
        private int currentWaypointIndex; // Текущая точка
        private IBonus activeBonus; // Текущий бонус
        private bool isBackingUp; // Движение назад
        private float backupTimer; // Таймер движения назад
        private float startTimer; // Таймер задержки старта

        protected override void Awake()
        {
            base.Awake();
            waypoints = TrackManager.Instance.Waypoints;
            navMeshAgent.updatePosition = false;
            navMeshAgent.updateRotation = false;
            navMeshAgent.speed = 15f * aggressionLevel;
            navMeshAgent.angularSpeed = 360f; // Плавные повороты
            navMeshAgent.acceleration = 30f; // Быстрая реакция
            navMeshAgent.avoidancePriority = Mathf.FloorToInt(aggressionLevel * 100); // Приоритет RVO
            navMeshAgent.radius = 1f; // Радиус избегания
            startTimer = startDelay; // Инициализация задержки
        }

        protected override void Drive()
        {
            if (waypoints.Length == 0 || GameManager.Instance.State != GameState.Racing) return;

            // Задержка старта
            if (startTimer > 0f)
            {
                startTimer -= Time.deltaTime;
                _carController.ThrottleInput = 0f;
                _carController.SteerInput = 0f;
                _carController.BrakeInput = 1f;
                return;
            }

            // Проверка waypoints всегда
            Vector3 targetPos = waypoints[currentWaypointIndex].position;
            if (IsInsideWaypointRect(transform.position, targetPos))
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            }

            if (isBackingUp)
            {
                UpdateBackup();
            }
            else
            {
                UpdateNavigation();
                UpdateBonusUsage();
            }
        }

        private void UpdateNavigation()
        {
            // Проверка достижимости точки
            Vector3 targetPos = waypoints[currentWaypointIndex].position;
            NavMeshPath path = new NavMeshPath();
            if (!navMeshAgent.CalculatePath(targetPos, path) || path.status != NavMeshPathStatus.PathComplete)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
                return;
            }

            navMeshAgent.SetDestination(targetPos);

            // Управление, только если есть путь
            if (!navMeshAgent.hasPath) return;

            // Ограничение направляющей
            Vector3 steeringTarget = navMeshAgent.steeringTarget;
            Vector3 directionToTarget = (steeringTarget - transform.position).normalized;
            steeringTarget = transform.position + directionToTarget * Mathf.Min(Vector3.Distance(transform.position, steeringTarget), lookAheadDistance);

            // Управление
            Vector3 direction = (steeringTarget - transform.position).normalized;
            float steerInput = Vector3.Dot(direction, transform.right) * aggressionLevel;
            steerInput = Mathf.Clamp(steerInput, -1f, 1f);

            // Raycast для избегания
            Vector3[] rayDirections = { transform.forward, Quaternion.Euler(0, 45, 0) * transform.forward, Quaternion.Euler(0, -45, 0) * transform.forward };
            foreach (var dir in rayDirections)
            {
                if (Physics.Raycast(transform.position, dir, out RaycastHit hit, avoidanceDistance, obstacleLayer))
                {
                    Vector3 avoidDir = (transform.position - hit.point).normalized;
                    steerInput += Vector3.Dot(avoidDir, transform.right) * 0.4f;
                    steerInput = Mathf.Clamp(steerInput, -1f, 1f);
                }
            }

            _carController.SteerInput = steerInput;
            _carController.ThrottleInput = aggressionLevel;
            _carController.BrakeInput = Random.value < (1f - aggressionLevel) / 2 ? 0.3f : 0f;
            _carController.Reverse = false; // Движение вперёд
        }

        private bool IsInsideWaypointRect(Vector3 botPos, Vector3 waypointPos)
        {
            Vector3 nextWaypointPos = currentWaypointIndex + 1 < waypoints.Length ? waypoints[currentWaypointIndex + 1].position : waypoints[0].position;
            Vector3 forward = (nextWaypointPos - waypointPos).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            Vector3 localPos = botPos - waypointPos;
            float x = Vector3.Dot(localPos, right); // Координата по ширине
            float z = Vector3.Dot(localPos, forward); // Координата по длине

            return Mathf.Abs(x) <= waypointWidth / 2f && Mathf.Abs(z) <= waypointLength / 2f;
        }

        private void UpdateBackup()
        {
            if (!isBackingUp) return;

            // Отключить NavMeshAgent во время отъезда
            navMeshAgent.enabled = false;

            backupTimer -= Time.deltaTime;
            if (backupTimer <= 0f)
            {
                isBackingUp = false;
                _carController.Reverse = false; // Возвращаем вперёд
                navMeshAgent.enabled = true; // Включаем NavMeshAgent
                return;
            }

            // Проверяем Raycast
            bool hitLeft = Physics.Raycast(transform.position, Quaternion.Euler(0, -45, 0) * transform.forward, avoidanceDistance, obstacleLayer);
            bool hitCenter = Physics.Raycast(transform.position, transform.forward, avoidanceDistance, obstacleLayer);
            bool hitRight = Physics.Raycast(transform.position, Quaternion.Euler(0, 45, 0) * transform.forward, avoidanceDistance, obstacleLayer);

            // Логика поворота
            float steerInput = 0f;
            if (hitLeft && hitCenter && hitRight)
            {
                steerInput = 0f; // Ровно назад
            }
            else if (hitLeft && hitCenter)
            {
                steerInput = -steerAngle; // Назад влево
            }
            else if (hitRight && hitCenter)
            {
                steerInput = steerAngle; // Назад вправо
            }
            else if (hitLeft)
            {
                steerInput = steerAngle; // Сильный поворот вправо
            }
            else if (hitRight)
            {
                steerInput = -steerAngle; // Сильный поворот влево
            }

            // Движение назад
            _carController.Reverse = true;
            _carController.ThrottleInput = 0.5f; // Положительный вход для заднего хода
            _carController.BrakeInput = 0f;
            _carController.SteerInput = steerInput;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Opponent"))
            {
                isBackingUp = true;
                backupTimer = backupTime;
            }
        }

        private void UpdateBonusUsage()
        {
            if (activeBonus != null && Random.value < bonusUseChance * Time.deltaTime)
            {
                activeBonus.Activate(_carController);
                activeBonus = null;
            }
        }

        public void SetBonus(IBonus bonus)
        {
            activeBonus = bonus;
        }

        private void OnDrawGizmos()
        {
            // Отладка Raycast
            Gizmos.color = Color.red;
            Vector3[] rayDirections = { transform.forward, Quaternion.Euler(0, 45, 0) * transform.forward, Quaternion.Euler(0, -45, 0) * transform.forward };
            foreach (var dir in rayDirections)
            {
                Gizmos.DrawRay(transform.position, dir * avoidanceDistance);
            }

            // Отладка прямоугольников waypoints
            if (waypoints != null && waypoints.Length > 0)
            {
                Gizmos.color = Color.green;
                for (int i = 0; i < waypoints.Length; i++)
                {
                    Vector3 pos = waypoints[i].position;
                    Vector3 nextPos = i + 1 < waypoints.Length ? waypoints[i + 1].position : waypoints[0].position;
                    Vector3 forward = (nextPos - pos).normalized;
                    Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

                    Vector3[] corners = {
                        pos + forward * (waypointLength / 2f) + right * (waypointWidth / 2f),
                        pos + forward * (waypointLength / 2f) - right * (waypointWidth / 2f),
                        pos - forward * (waypointLength / 2f) - right * (waypointWidth / 2f),
                        pos - forward * (waypointLength / 2f) + right * (waypointWidth / 2f)
                    };

                    Gizmos.DrawLine(corners[0], corners[1]);
                    Gizmos.DrawLine(corners[1], corners[2]);
                    Gizmos.DrawLine(corners[2], corners[3]);
                    Gizmos.DrawLine(corners[3], corners[0]);
                }
            }
        }
    }
}