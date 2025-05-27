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

        [SerializeField] private float startDelay = 3f; // Задержка перед стартом (сек)
        [SerializeField] private List<CarControllerBase> racers; // Все участники гонки
        [SerializeField] private float raceDuration = 180f; // Длительность заезда (сек)

        private GameState state = GameState.Waiting; // Текущее состояние
        private float startTimer; // Таймер старта
        private float raceTimer; // Таймер гонки
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
                return;
            }

            // Находим машину игрока
            playerCar = racers.Find(r => r.GetComponent<PlayerCarControl>() != null);
            Debug.Log("GameManager Awake: " + gameObject.name); // Отладка
        }

        private void Start()
        {
            startTimer = startDelay;
            raceTimer = raceDuration;
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
            else if (state == GameState.Racing)
            {
                raceTimer -= Time.deltaTime;
                if (raceTimer <= 0f)
                {
                    FinishRace();
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
                racer.GetComponent<CheckpointTrigger>().Respawn(); // Спавн всех машин
            }
            Debug.Log("StartRace: гонка началась. Вызывается OnRaceStarted.");
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

            // Определяем позицию игрока (по количеству устранений)
            int playerPosition = CalculatePlayerPosition();
            int placeCoins = playerPosition switch
            {
                1 => 300, // 1-е место
                2 => 200, // 2-е место
                3 => 100, // 3-е место
                _ => 0
            };
            AddCoins(placeCoins + playerEliminations * 50); // Награда за место и устранения
            GameEvents.OnRaceFinished.Invoke(playerPosition, playerCoins, playerEliminations);
        }

        // Расчёт позиции игрока по количеству устранений
        private int CalculatePlayerPosition()
        {
            var sortedRacers = racers.OrderByDescending(r => r.GetComponent<CarHealth>().Eliminations).ToList();
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
            if (state != GameState.Racing) return;

            // Если игрок жив, добавляем ему устранение
            if (playerCar != null && playerCar.gameObject.activeInHierarchy)
            {
                playerEliminations++;
                playerCar.GetComponent<CarHealth>().AddElimination();
                AddCoins(50); // Награда за устранение
            }

            GameEvents.OnElimination.Invoke(eliminatedCar);
            // Ресспавн устранённой машины
            eliminatedCar.GetComponent<CarHealth>().Respawn();
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

        public GameState State => state;
        public CarControllerBase PlayerCar => playerCar;
    }
}