using UnityEngine;
using System.Collections.Generic;

namespace UshiSoft.UACPF
{
    public class GameEventHub : MonoBehaviour
    {
        [SerializeField] private UIManager uiManager; // UI менеджер
        [SerializeField] private CurrencyManager currencyManager; // Менеджер валют
        [SerializeField] private List<BotCarControl> bots; // Боты

        private void OnEnable()
        {
            // Подписка на события
            GameEvents.OnCountdownStarted.AddListener(OnCountdownStarted);
            GameEvents.OnRaceStarted.AddListener(OnRaceStarted);
            GameEvents.OnRaceFinished.AddListener(OnRaceFinished);
            GameEvents.OnCoinsAdded.AddListener(OnCoinsAdded);
            GameEvents.OnElimination.AddListener(OnElimination);
            GameEvents.OnPauseToggled.AddListener(OnPauseToggled);
            
        }

        private void OnDisable()
        {
            // Отписка от событий
            GameEvents.OnCountdownStarted.RemoveListener(OnCountdownStarted);
            GameEvents.OnRaceStarted.RemoveListener(OnRaceStarted);
            GameEvents.OnRaceFinished.RemoveListener(OnRaceFinished);
            GameEvents.OnCoinsAdded.RemoveListener(OnCoinsAdded);
            GameEvents.OnElimination.RemoveListener(OnElimination);
            GameEvents.OnPauseToggled.RemoveListener(OnPauseToggled);
            
        }

        private void OnCountdownStarted(float duration)
        {
            uiManager.ShowCountdown(duration);
        }

        private void OnRaceStarted()
        {
            uiManager.ShowRaceUI();
            foreach (var bot in bots)
            {
                bot.enabled = true; // Включаем управление ботами
            }
        }

        private void OnRaceFinished(int playerPosition, int coins, int eliminations)
        {
            uiManager.ShowRaceResults(playerPosition, coins, eliminations);
            foreach (var bot in bots)
            {
                bot.enabled = false; // Отключаем ботов
            }
        }

        private void OnCoinsAdded(int amount)
        {
            currencyManager.AddCoins(amount);
            uiManager.UpdateCoins(currencyManager.Coins);
        }

        private void OnElimination(CarControllerBase eliminatedCar)
        {
            // Дополнительная логика, если нужно (например, эффекты)
        }

        private void OnPauseToggled(bool isPaused)
        {
            if (isPaused)
                uiManager.ShowPauseMenu();
            else
                uiManager.HidePauseMenu();
        }

        private void OnLapUpdated(int currentLap, int totalLaps)
        {
            uiManager.UpdateLap(currentLap, totalLaps);
        }
    }
}