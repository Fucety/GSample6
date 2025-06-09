using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UshiSoft.UACPF
{
    public class SpawnPointManager : MonoBehaviour
    {
        public static SpawnPointManager Instance { get; private set; }

        [SerializeField] private Transform[] spawnPoints; // Точки спавна, назначаемые в инспекторе для каждой сцены
        [SerializeField] private float spawnCooldown = 5f;
        [SerializeField] private float spawnRadius = 2f;
        [SerializeField] private LayerMask carLayer;

        private Dictionary<Transform, float> spawnCooldowns = new Dictionary<Transform, float>();

        private void Awake()
        {
            // Если это самый первый экземпляр, он становится вечным синглтоном
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSpawnPoints(spawnPoints); // Инициализируем его собственными точками
            }
            // Если экземпляр уже существует, значит, это загрузилась новая сцена
            else
            {
                // Передаем точки из нового менеджера старому (вечному)
                Instance.InitializeSpawnPoints(spawnPoints);
                // После передачи данных этот дубликат можно удалить
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // Обновляем таймеры только у активного экземпляра
            if (Instance != this) return;

            // Копируем ключи, чтобы избежать ошибки изменения коллекции во время итерации
            var points = new List<Transform>(spawnCooldowns.Keys);
            foreach (var point in points)
            {
                if (spawnCooldowns[point] > 0f)
                {
                    spawnCooldowns[point] -= Time.deltaTime;
                }
            }
        }

        // Логика инициализации вынесена в отдельный метод, чтобы ее можно было вызывать повторно
        private void InitializeSpawnPoints(Transform[] newPoints)
        {
            // Обновляем список точек и полностью пересоздаем словарь кулдаунов
            spawnPoints = newPoints;
            spawnCooldowns.Clear();

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning("SpawnPointManager: В этой сцене не назначено ни одной точки спавна!");
                return;
            }

            foreach (var point in spawnPoints)
            {
                if(point != null) // Добавлена проверка на null
                {
                    spawnCooldowns[point] = 0f;
                }
            }
            Debug.Log($"[SpawnPointManager] Инициализировано {spawnCooldowns.Count} новых точек спавна.");
        }

        public Transform GetAvailableSpawnPoint()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                return null;
            }

            // Фильтруем активные точки
            var availablePoints = spawnPoints
                .Where(p => p != null && spawnCooldowns.ContainsKey(p) && spawnCooldowns[p] <= 0f && !IsOccupied(p.position))
                .ToList();

            if (availablePoints.Count == 0)
            {
                return null; // Нет доступных точек
            }

            // Выбираем случайную точку
            return availablePoints[Random.Range(0, availablePoints.Count)];
        }

        private bool IsOccupied(Vector3 position)
        {
            // Проверяем, есть ли машины в радиусе точки спавна
            return Physics.OverlapSphere(position, spawnRadius, carLayer).Length > 0;
        }



        public void SetSpawnCooldown(Transform spawnPoint)
        {
            if (spawnPoint != null && spawnCooldowns.ContainsKey(spawnPoint))
            {
                spawnCooldowns[spawnPoint] = spawnCooldown;
            }
        }
    }
}