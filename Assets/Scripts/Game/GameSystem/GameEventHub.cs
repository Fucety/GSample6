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
            GameEvents.OnMatchStarted.AddListener(OnMatchStarted);
            GameEvents.OnMatchFinished.AddListener(OnMatchFinished);
            GameEvents.OnCoinsAdded.AddListener(OnCoinsAdded);
            GameEvents.OnEliminationsUpdated.AddListener(OnEliminationsUpdated);
            GameEvents.OnElimination.AddListener(OnElimination);
            GameEvents.OnPauseToggled.AddListener(OnPauseToggled);
        }

        private void OnDisable()
        {
            // Отписка от событий
            GameEvents.OnCountdownStarted.RemoveListener(OnCountdownStarted);
            GameEvents.OnMatchStarted.RemoveListener(OnMatchStarted);
            GameEvents.OnMatchFinished.RemoveListener(OnMatchFinished);
            GameEvents.OnCoinsAdded.RemoveListener(OnCoinsAdded);
            GameEvents.OnEliminationsUpdated.RemoveListener(OnEliminationsUpdated);
            GameEvents.OnElimination.RemoveListener(OnElimination);
            GameEvents.OnPauseToggled.RemoveListener(OnPauseToggled);
        }

        private void OnCountdownStarted(float duration)
        {
            uiManager.ShowCountdown(duration);
        }

        private void OnMatchStarted()
        {
            uiManager.ShowArenaUI();
            foreach (var bot in bots)
            {
                bot.enabled = true; // Включаем управление ботами
            }
        }

        private void OnMatchFinished(int coins, int eliminations)
        {
            uiManager.ShowMatchResults(coins, eliminations);
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

        private void OnEliminationsUpdated(int eliminations)
        {
            uiManager.UpdateEliminations(eliminations);
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
    }
}