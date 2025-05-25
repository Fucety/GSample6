using UnityEngine;
using UnityEngine.Events;

namespace UshiSoft.UACPF
{
    // Класс для определения событий игры
    public static class GameEvents
    {
        // Событие обратного отсчёта
        [System.Serializable] public class CountdownEvent : UnityEvent<float> { }
        public static CountdownEvent OnCountdownStarted = new CountdownEvent();

        // Событие старта гонки
        [System.Serializable] public class RaceStartEvent : UnityEvent { }
        public static RaceStartEvent OnRaceStarted = new RaceStartEvent();

        // Событие финиша гонки
        [System.Serializable] public class RaceFinishEvent : UnityEvent<int, int, int> { } // позиция, монеты, устранения
        public static RaceFinishEvent OnRaceFinished = new RaceFinishEvent();

        // Событие начисления монет
        [System.Serializable] public class CoinsAddedEvent : UnityEvent<int> { }
        public static CoinsAddedEvent OnCoinsAdded = new CoinsAddedEvent();

        // Событие устранения соперника
        [System.Serializable] public class EliminationEvent : UnityEvent<CarControllerBase> { }
        public static EliminationEvent OnElimination = new EliminationEvent();

        // Событие паузы/возобновления
        [System.Serializable] public class PauseEvent : UnityEvent<bool> { } // true - пауза, false - возобновление
        public static PauseEvent OnPauseToggled = new PauseEvent();

        // Событие изменения круга
        [System.Serializable] public class LapUpdateEvent : UnityEvent<int, int> { } // текущий круг, всего кругов
        public static LapUpdateEvent OnLapUpdated = new LapUpdateEvent();
    }
}