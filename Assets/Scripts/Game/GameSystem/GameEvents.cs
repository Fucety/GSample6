using UnityEngine;
using UnityEngine.Events;

namespace UshiSoft.UACPF
{
    public static class GameEvents
    {
        public static UnityEvent<float> OnCountdownStarted = new UnityEvent<float>();
        public static UnityEvent OnRaceStarted = new UnityEvent();
        public static UnityEvent<int, int, int> OnRaceFinished = new UnityEvent<int, int, int>();
        public static UnityEvent<int> OnCoinsAdded = new UnityEvent<int>();
        public static UnityEvent<CarControllerBase> OnElimination = new UnityEvent<CarControllerBase>();
        public static UnityEvent<bool> OnPauseToggled = new UnityEvent<bool>();
    }
}