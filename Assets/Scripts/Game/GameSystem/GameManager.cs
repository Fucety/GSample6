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

        [SerializeField] private float startDelay = 3f;
        [SerializeField] private List<CarControllerBase> racers;
        [SerializeField] private float matchDuration = 180f;
        [SerializeField] private float respawnDelay = 3f;

        private GameState state = GameState.Waiting;
        private float startTimer;
        private float matchTimer;
        private int playerCoins;
        private int playerEliminations;
        private CarControllerBase playerCar;
        private Dictionary<CarControllerBase, int> racerEliminations;

        private void Awake()
        {
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

            playerCar = racers.Find(r => r.GetComponent<PlayerCarControl>() != null);
            if (playerCar == null)
            {
                Debug.LogError("GameManager: Машина игрока не найдена в списке racers!");
            }

            racerEliminations = new Dictionary<CarControllerBase, int>();
            foreach (var racer in racers)
            {
                racerEliminations[racer] = 0;
            }
        }

        private void Start()
        {
            startTimer = startDelay;
            matchTimer = matchDuration;
            GameEvents.OnCountdownStarted.Invoke(startDelay);
        }

        private void Update()
        {
            if (state == GameState.Waiting)
            {
                if ((startTimer -= Time.deltaTime) <= 0f)
                {
                    StartMatch();
                }
            }
            else if (state == GameState.Fighting)
            {
                if ((matchTimer -= Time.deltaTime) <= 0f)
                {
                    FinishMatch();
                }
            }
        }

        private void StartMatch()
        {
            state = GameState.Fighting;
            foreach (var racer in racers)
            {
                racer.enabled = true;
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
                racer.enabled = false;
            }
            // В `playerEliminations` уже хранятся итоговые устранения игрока
            GameEvents.OnMatchFinished.Invoke(playerCoins, playerEliminations);
        }

        public void RegisterElimination(CarControllerBase eliminatedCar)
        {
            if (state != GameState.Fighting || eliminatedCar == null) return;

            var killer = eliminatedCar.GetComponent<CarHealth>()?.LastAttacker;
            if (killer != null)
            {
                // Проверяем, есть ли у убийцы компонент CarHealth
                var attackerHealth = killer.GetComponent<CarHealth>();
                if (attackerHealth != null)
                {
                    attackerHealth.AddElimination();
                    // Обновляем статистику в словаре
                    if (racerEliminations.ContainsKey(killer))
                    {
                        racerEliminations[killer] = attackerHealth.Eliminations;
                    }
                }
                
                // Если убийца - игрок, обновляем его личный счетчик и начисляем монеты
                if (killer == playerCar)
                {
                    playerEliminations++;
                    CurrencyManager.Instance.AddCoinsForElimination(50);
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

        /// <summary>
        /// Возвращает отсортированный список участников по количеству устранений.
        /// </summary>
        /// <returns>Список пар (Участник, Устранения) от лучшего к худшему.</returns>
        public List<KeyValuePair<CarControllerBase, int>> GetLeaderboard()
        {
            // Сортируем словарь со статистикой по убыванию очков и возвращаем как список
            return racerEliminations.OrderByDescending(kvp => kvp.Value).ToList();
        }
    }
}