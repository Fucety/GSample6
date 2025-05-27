using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UshiSoft.UACPF
{
    public class SpawnPointManager : MonoBehaviour
    {
        public static SpawnPointManager Instance { get; private set; }

        [SerializeField] private Transform[] spawnPoints; // Точки спавна
        [SerializeField] private float spawnCooldown = 5f; // Время деактивации точки после спавна
        [SerializeField] private float spawnRadius = 2f; // Радиус проверки для предотвращения наложения
        [SerializeField] private LayerMask carLayer; // Слой для машин (игрок и боты)

        private Dictionary<Transform, float> spawnCooldowns; // Таймеры для точек спавна

        private void Awake()
        {
            // Singleton
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            // Инициализация таймеров
            spawnCooldowns = new Dictionary<Transform, float>();
            foreach (var point in spawnPoints)
            {
                spawnCooldowns[point] = 0f;
            }
        }

        private void Update()
        {
            // Обновление таймеров
            foreach (var point in spawnPoints)
            {
                if (spawnCooldowns[point] > 0f)
                {
                    spawnCooldowns[point] -= Time.deltaTime;
                }
            }
        }

        // Получение доступной точки спавна
        public Transform GetAvailableSpawnPoint()
        {
            // Фильтруем активные точки (без кулдауна и без машин поблизости)
            var availablePoints = spawnPoints
                .Where(p => spawnCooldowns[p] <= 0f && !IsOccupied(p.position))
                .ToList();

            if (availablePoints.Count == 0)
            {
                return null; // Нет доступных точек
            }

            // Выбираем случайную точку
            return availablePoints[Random.Range(0, availablePoints.Count)];
        }

        // Проверка, занята ли точка
        private bool IsOccupied(Vector3 position)
        {
            return Physics.OverlapSphere(position, spawnRadius, carLayer).Length > 0;
        }

        // Установка кулдауна для точки спавна
        public void SetSpawnCooldown(Transform spawnPoint)
        {
            if (spawnCooldowns.ContainsKey(spawnPoint))
            {
                spawnCooldowns[spawnPoint] = spawnCooldown;
            }
        }
    }
}