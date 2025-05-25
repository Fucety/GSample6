using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UshiSoft.UACPF
{
    public enum GameState
    {
        Waiting, // Ожидание старта
        Racing,  // Гонка
        Paused,  // Пауза
        Finished // Финиш
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private int totalLaps = 3; // Количество кругов
        [SerializeField] private float startDelay = 3f; // Задержка перед стартом (сек)
        [SerializeField] private List<CarControllerBase> racers; // Все участники гонки

        private GameState state = GameState.Waiting; // Текущее состояние
        private float startTimer; // Таймер старта
        private int playerCoins; // Монеты игрока
        private int playerEliminations; // Количество устранённых соперников
        private CarControllerBase playerCar; // Машина игрока

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

            // Находим машину игрока
            playerCar = racers.Find(r => r.GetComponent<PlayerCarControl>() != null);

            // Регистрируем всех гонщиков в TrackManager
            foreach (var racer in racers)
            {
                TrackManager.Instance.RegisterRacer(racer);
            }
        }

        private void Start()
        {
            startTimer = startDelay;
            GameEvents.OnCountdownStarted.Invoke(startDelay); // Запускаем обратный отсчёт
        }

        private void Update()
        {
            if (state == GameState.Waiting)
            {
                startTimer -= Time.deltaTime;
                if (startTimer <= 0f)
                {
                    StartRace();
                }
            }
        }

        // Запуск гонки
        private void StartRace()
        {
            state = GameState.Racing;
            foreach (var racer in racers)
            {
                racer.enabled = true; // Включаем управление
            }
            GameEvents.OnRaceStarted.Invoke();
        }

        // Завершение гонки
        public void FinishRace()
        {
            if (state != GameState.Racing) return;

            state = GameState.Finished;
            foreach (var racer in racers)
            {
                racer.enabled = false; // Отключаем управление
            }

            // Определяем позицию игрока
            int playerPosition = CalculatePlayerPosition();
            int placeCoins = playerPosition switch
            {
                1 => 300, // 1-е место
                2 => 200, // 2-е место
                3 => 100, // 3-е место
                4 => 50,  // 4-е место
                _ => 0
            };
            AddCoins(placeCoins + playerEliminations * 50); // Награда за место и устранения
            GameEvents.OnRaceFinished.Invoke(playerPosition, playerCoins, playerEliminations);
        }

        // Расчёт позиции игрока
        private int CalculatePlayerPosition()
        {
            var sortedRacers = racers.OrderByDescending(r => TrackManager.Instance.GetProgress(r)).ToList();
            return sortedRacers.IndexOf(playerCar) + 1;
        }

        // Добавление монет
        public void AddCoins(int amount)
        {
            playerCoins += amount;
            GameEvents.OnCoinsAdded.Invoke(amount);
        }

        // Регистрация устранения
        public void RegisterElimination(CarControllerBase eliminatedCar)
        {
            if (state != GameState.Racing || eliminatedCar == playerCar) return;
            playerEliminations++;
            AddCoins(50); // Награда за устранение
            GameEvents.OnElimination.Invoke(eliminatedCar);
        }

        // Пауза/возобновление
        public void TogglePause()
        {
            if (state == GameState.Racing)
            {
                state = GameState.Paused;
                Time.timeScale = 0f;
                GameEvents.OnPauseToggled.Invoke(true);
            }
            else if (state == GameState.Paused)
            {
                state = GameState.Racing;
                Time.timeScale = 1f;
                GameEvents.OnPauseToggled.Invoke(false);
            }
        }

        // Обновление круга (вызывается из TrackManager)
        public void UpdateLap(int currentLap, int totalLaps)
        {
            GameEvents.OnLapUpdated.Invoke(currentLap, totalLaps);
        }

        public GameState State => state;
        public CarControllerBase PlayerCar => playerCar;
        public int TotalLaps => totalLaps;
    }
}