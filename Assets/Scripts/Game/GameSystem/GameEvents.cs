using UnityEngine;
using UnityEngine.Events;

namespace UshiSoft.UACPF
{
    public static class GameEvents
    {
        public static UnityEvent<float> OnCountdownStarted = new UnityEvent<float>();
        public static UnityEvent OnMatchStarted = new UnityEvent();
        public static UnityEvent<int, int> OnMatchFinished = new UnityEvent<int, int>();
        public static UnityEvent<int> OnCoinsAdded = new UnityEvent<int>();
        public static UnityEvent<int> OnEliminationsUpdated = new UnityEvent<int>();
        public static UnityEvent<CarControllerBase> OnElimination = new UnityEvent<CarControllerBase>();
        public static UnityEvent<bool> OnPauseToggled = new UnityEvent<bool>();
    }
}