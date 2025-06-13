using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UshiSoft.UACPF
{
    public enum GameState
    {
        Waiting,
        Fighting,
        Paused,
        Finished
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Настройки матча")]
        [SerializeField] private float startDelay = 3f;
        [Tooltip("Список ботов, которые будут участвовать в матче. Машина игрока будет добавлена автоматически при спавне.")]
        [SerializeField] private List<CarControllerBase> racers;
        [SerializeField] private float matchDuration = 180f;
        [SerializeField] private float respawnDelay = 3f;

        [Header("Настройки наград")]
        [SerializeField] private float comboWindow = 10f;
        [SerializeField] private float comboMultiplier = 1.25f;

        private GameState state = GameState.Waiting;
        private float startTimer;
        private float matchTimer;
        private int coinsEarnedThisMatch;
        private int playerEliminations;
        private CarControllerBase playerCar;
        private Dictionary<CarControllerBase, int> racerEliminations;

        // Для логики комбо
        private float lastEliminationTime;
        private int comboCount;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                //DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            // Инициализируем словарь для статистики. Он будет заполняться ботами из инспектора и игроком при спавне.
            racerEliminations = new Dictionary<CarControllerBase, int>();
            foreach (var racer in racers)
            {
                racerEliminations[racer] = 0;
            }
            
            coinsEarnedThisMatch = 0; // Начинаем матч с 0 заработанных монет
            playerEliminations = 0;
            comboCount = 0;
        }

        private void Start()
        {
            startTimer = startDelay;
            matchTimer = matchDuration;
            GameEvents.OnCountdownStarted.Invoke(startDelay);
        }

        private void Update()
        {
            switch (state)
            {
                case GameState.Waiting:
                    if ((startTimer -= Time.deltaTime) <= 0f)
                    {
                        StartMatch();
                    }
                    break;
                case GameState.Fighting:
                    if ((matchTimer -= Time.deltaTime) <= 0f)
                    {
                        FinishMatch();
                    }
                    break;
                case GameState.Paused:
                case GameState.Finished:
                    break;
            }
        }
        
        // --- НОВЫЙ МЕТОД ---
        // Публичный метод для добавления игрока, созданного во время выполнения через PlayerSpawner
        public void RegisterPlayerCar(CarControllerBase playerCarController)
        {
            if (playerCarController == null) return;

            // Добавляем машину игрока в общий список участников
            if (!racers.Contains(playerCarController))
            {
                racers.Add(playerCarController);
            }

            // Назначаем ее как основную машину игрока
            this.playerCar = playerCarController;

            // Инициализируем для нее запись в таблице устранений
            if (!racerEliminations.ContainsKey(playerCarController))
            {
                racerEliminations.Add(playerCarController, 0);
            }

            // <<< ГЛАВНОЕ ИЗМЕНЕНИЕ: ВЫЗЫВАЕМ СОБЫТИЕ >>>
            // Оповещаем всю игру (включая камеру), что игрок готов.
            GameEvents.OnPlayerSpawned.Invoke(playerCarController);
        }

        private void StartMatch()
        {
            state = GameState.Fighting;
            foreach (var racer in racers)
            {
                // Убедимся, что все участники включены
                if(racer != null) racer.enabled = true;
                
                var checkpointTrigger = racer.GetComponent<CheckpointTrigger>();
                if (checkpointTrigger != null)
                {
                    checkpointTrigger.Respawn();
                }
            }
            GameEvents.OnMatchStarted.Invoke();
        }

        public void FinishMatch()
        {
            if (state != GameState.Fighting) return;
            
            state = GameState.Finished;
            foreach (var racer in racers)
            {
                if(racer != null) racer.enabled = false;
            }
            
            GameEvents.OnMatchFinished.Invoke(coinsEarnedThisMatch, playerEliminations);
        }

        public void RegisterElimination(CarControllerBase eliminatedCar)
        {
            if (state != GameState.Fighting || eliminatedCar == null) return;

            var killer = eliminatedCar.GetComponent<CarHealth>()?.LastAttacker;
            if (killer != null)
            {
                var attackerHealth = killer.GetComponent<CarHealth>();
                if (attackerHealth != null)
                {
                    attackerHealth.AddElimination();
                    if (racerEliminations.ContainsKey(killer))
                    {
                        racerEliminations[killer] = attackerHealth.Eliminations;
                    }
                }
                
                if (killer == playerCar)
                {
                    playerEliminations++;
                    // --- НОВАЯ ЛОГИКА ПОДСЧЕТА НАГРАДЫ ---
                    int baseReward = 50;
                    bool isCombo = (Time.time - lastEliminationTime) <= comboWindow;
                    comboCount = isCombo ? comboCount + 1 : 1;
                    int rewardWithCombo = Mathf.RoundToInt(baseReward * Mathf.Pow(comboMultiplier, comboCount - 1));
                    coinsEarnedThisMatch += rewardWithCombo;
                    lastEliminationTime = Time.time;
                    // Обновляем только UI
                    GameEvents.OnCoinsEarnedThisMatchUpdated.Invoke(coinsEarnedThisMatch);
                    GameEvents.OnEliminationsUpdated.Invoke(playerEliminations);
                }
            }

            GameEvents.OnElimination.Invoke(eliminatedCar);
            StartCoroutine(RespawnCarCoroutine(eliminatedCar));
        }

        private IEnumerator RespawnCarCoroutine(CarControllerBase car)
        {
            if (car == null) yield break;
            yield return new WaitForSeconds(respawnDelay);
            var carHealth = car.GetComponent<CarHealth>();
            if (carHealth != null)
            {
                carHealth.Respawn();
            }
        }
        
        public void TogglePause()
        {
            if (state == GameState.Fighting)
            {
                state = GameState.Paused;
                Time.timeScale = 0f;
                GameEvents.OnPauseToggled.Invoke(true);
            }
            else if (state == GameState.Paused)
            {
                state = GameState.Fighting;
                Time.timeScale = 1f;
                GameEvents.OnPauseToggled.Invoke(false);
            }
        }

        public GameState State => state;
        public CarControllerBase PlayerCar => playerCar;
        
        public List<KeyValuePair<CarControllerBase, int>> GetLeaderboard()
        {
            return racerEliminations.OrderByDescending(kvp => kvp.Value).ToList();
        }
    }
}