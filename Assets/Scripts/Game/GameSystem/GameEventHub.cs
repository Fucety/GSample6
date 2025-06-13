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
            GameEvents.OnEliminationsUpdated.AddListener(OnEliminationsUpdated);
            GameEvents.OnElimination.AddListener(OnElimination);
            GameEvents.OnPauseToggled.AddListener(OnPauseToggled);
            GameEvents.OnCoinsEarnedThisMatchUpdated.AddListener(OnCoinsEarnedThisMatchUpdated);
        }

        private void OnDisable()
        {
            // Отписка от событий
            GameEvents.OnCountdownStarted.RemoveListener(OnCountdownStarted);
            GameEvents.OnMatchStarted.RemoveListener(OnMatchStarted);
            GameEvents.OnMatchFinished.RemoveListener(OnMatchFinished);
            GameEvents.OnEliminationsUpdated.RemoveListener(OnEliminationsUpdated);
            GameEvents.OnElimination.RemoveListener(OnElimination);
            GameEvents.OnPauseToggled.RemoveListener(OnPauseToggled);
            GameEvents.OnCoinsEarnedThisMatchUpdated.RemoveListener(OnCoinsEarnedThisMatchUpdated);
        }

        private void OnCountdownStarted(float duration)
        {
            // При старте отсчета, скрываем все панели, кроме панели отсчета,
            // которую мы сразу же показываем.
            uiManager.HideAllPanels(); // Скрываем все существующие панели
            uiManager.ShowCountdown(duration); // Показываем панель обратного отсчета
            Debug.Log("[GameEventHub] Отсчет запущен. Показываем Countdown Panel."); // Добавлено для отладки
        }

        private void OnMatchStarted()
        {
            // После завершения отсчета и старта матча, 
            // скрываем все (на случай если что-то осталось) и показываем ArenaUI.
            //uiManager.HideAllPanels(); // Скрываем все панели
            uiManager.ShowArenaUI(); // Показываем UI арены
            foreach (var bot in bots)
            {
                bot.enabled = true; // Включаем управление ботами
            }
        }

        private void OnMatchFinished(int coins, int eliminations)
        {
            // При завершении матча, скрываем все и показываем результаты.
            uiManager.HideAllPanels(); // Скрываем все панели
            uiManager.ShowMatchResults(coins, eliminations); // Показываем результаты матча
            foreach (var bot in bots)
            {
                bot.enabled = false; // Отключаем ботов
            }
        }

        private void OnEliminationsUpdated(int eliminations)
        {
            uiManager.UpdateEliminations(eliminations); // Обновляем отображение убийств в UI
        }

        private void OnElimination(CarControllerBase eliminatedCar)
        {
            // Дополнительная логика, если нужно (например, эффекты)
        }

        private void OnPauseToggled(bool isPaused)
        {
            if (isPaused)
            {
                // При паузе скрываем ArenaUI и показываем меню паузы
                uiManager.HideArenaUI(); // Скрываем UI арены при паузе (предполагается, что такой метод есть или будет добавлен)
                uiManager.ShowPauseMenu(); // Показываем меню паузы
            }
            else
            {
                // При снятии паузы скрываем меню паузы и снова показываем ArenaUI
                uiManager.HidePauseMenu(); // Скрываем меню паузы
                uiManager.ShowArenaUI(); // Показываем UI арены
            }
        }

        private void OnCoinsEarnedThisMatchUpdated(int coins)
        {
            uiManager.UpdateCoinsThisMatch(coins);
        }
    }
}