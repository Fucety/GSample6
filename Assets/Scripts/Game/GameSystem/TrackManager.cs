using UnityEngine;
using System.Collections.Generic;

namespace UshiSoft.UACPF
{
    public class TrackManager : MonoBehaviour
    {
        public static TrackManager Instance { get; private set; }

        [SerializeField] private Transform[] checkpoints; // Контрольные точки
        [SerializeField] private Transform[] waypoints; // Точки пути для ботов

        private Dictionary<CarControllerBase, int> currentCheckpoints = new Dictionary<CarControllerBase, int>(); // Текущая контрольная точка для каждого гонщика
        private Dictionary<CarControllerBase, int> currentLaps = new Dictionary<CarControllerBase, int>(); // Текущий круг для каждого гонщика
        private Dictionary<CarControllerBase, float> progresses = new Dictionary<CarControllerBase, float>(); // Прогресс (круги + доля круга)

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
        }

        // Инициализация прогресса для гонщика
        public void RegisterRacer(CarControllerBase racer)
        {
            if (!currentCheckpoints.ContainsKey(racer))
            {
                currentCheckpoints[racer] = 0;
                currentLaps[racer] = 0;
                progresses[racer] = 0f;
            }
        }

        // Обработка столкновения с контрольной точкой
        public void OnCheckpointReached(CarControllerBase racer, Transform checkpoint)
        {
            if (!currentCheckpoints.ContainsKey(racer)) return;

            int checkpointIndex = System.Array.IndexOf(checkpoints, checkpoint);
            if (checkpointIndex == (currentCheckpoints[racer] + 1) % checkpoints.Length)
            {
                currentCheckpoints[racer] = checkpointIndex;
                if (checkpointIndex == 0)
                {
                    currentLaps[racer]++;
                    if (racer == GameManager.Instance.PlayerCar && currentLaps[racer] >= GameManager.Instance.TotalLaps)
                    {
                        GameManager.Instance.FinishRace();
                    }
                }
                UpdateProgress(racer);
                if (racer == GameManager.Instance.PlayerCar)
                {
                    GameEvents.OnLapUpdated.Invoke(currentLaps[racer], GameManager.Instance.TotalLaps);
                }
            }
        }

        private void UpdateProgress(CarControllerBase racer)
        {
            progresses[racer] = currentLaps[racer] + (float)currentCheckpoints[racer] / checkpoints.Length;
        }

        public Transform[] Checkpoints => checkpoints;
        public Transform[] Waypoints => waypoints;
        public float GetProgress(CarControllerBase racer) => progresses.ContainsKey(racer) ? progresses[racer] : 0f;
    }
}