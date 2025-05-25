using UnityEngine;

namespace UshiSoft.UACPF
{
    // Обработка ввода с клавиатуры для ПК
    public class KeyboardInputProvider : IInputProvider
    {
        public float GetSteerInput()
        {
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
                return -1f;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
                return 1f;
            return 0f;
        }

        public float GetThrottleInput()
        {
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
                return 1f;
            return 0f;
        }

        public float GetBrakeInput()
        {
            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
                return 1f;
            return 0f;
        }

        public bool IsBonusActivated()
        {
            return Input.GetKeyDown(KeyCode.Space);
        }
    }
}