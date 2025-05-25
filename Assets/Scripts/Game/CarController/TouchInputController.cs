using UnityEngine;
using UnityEngine.EventSystems;

namespace UshiSoft.UACPF
{
    // Контроллер сенсорного ввода (джойстик и двойное касание)
    public class TouchInputController : MonoBehaviour, IInputProvider
    {
        [SerializeField, Min(50f)] private float joystickRadius = 50f; // Радиус джойстика
        [SerializeField, Min(0.1f)] private float doubleTapTime = 0.3f; // Время для двойного касания
        [SerializeField] private TouchInputUI ui; // UI-компонент для отображения

        private Vector2 joystickInput; // Вектор ввода джойстика
        private bool isJoystickActive; // Активен ли джойстик
        private int joystickTouchId = -1; // ID касания для джойстика

        private bool bonusActivated; // Флаг активации бонуса
        private float lastTapTime; // Время последнего касания для кнопки
        private int tapCount; // Счётчик касаний

        private void Update()
        {
            HandleTouchInput();
        }

        private void HandleTouchInput()
        {
            // Обрабатываем все касания
            foreach (Touch touch in Input.touches)
            {
                // Пропускаем касания над UI (например, кнопки меню)
                if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    continue;

                // Джойстик
                if (touch.fingerId == joystickTouchId || (joystickTouchId == -1 && touch.phase == TouchPhase.Began))
                {
                    HandleJoystick(touch);
                }
                // Кнопка бонуса (двойное касание в любой части экрана, кроме джойстика)
                else if (touch.phase == TouchPhase.Began)
                {
                    HandleBonusTap();
                }
            }

            // Деактивируем джойстик, если нет активных касаний
            if (Input.touchCount == 0 || (joystickTouchId != -1 && !IsTouchActive(joystickTouchId)))
            {
                DeactivateJoystick();
            }
        }

        private void HandleJoystick(Touch touch)
        {
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    joystickTouchId = touch.fingerId;
                    isJoystickActive = true;
                    ui.ShowJoystick(touch.position);
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    Vector2 delta = touch.position - ui.JoystickCenter;
                    delta = Vector2.ClampMagnitude(delta, joystickRadius);
                    joystickInput = delta / joystickRadius;
                    ui.UpdateJoystickHandle(touch.position);
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    DeactivateJoystick();
                    break;
            }
        }

        private void DeactivateJoystick()
        {
            joystickTouchId = -1;
            isJoystickActive = false;
            joystickInput = Vector2.zero;
            ui.HideJoystick();
        }

        private void HandleBonusTap()
        {
            float currentTime = Time.time;
            if (currentTime - lastTapTime < doubleTapTime)
            {
                tapCount++;
                if (tapCount >= 2)
                {
                    bonusActivated = true;
                    tapCount = 0;
                }
            }
            else
            {
                tapCount = 1;
            }
            lastTapTime = currentTime;
        }

        private bool IsTouchActive(int touchId)
        {
            foreach (Touch touch in Input.touches)
            {
                if (touch.fingerId == touchId)
                    return true;
            }
            return false;
        }

        // Реализация IInputProvider
        public float GetSteerInput()
        {
            return isJoystickActive ? joystickInput.x : 0f;
        }

        public float GetThrottleInput()
        {
            return isJoystickActive && joystickInput.y > 0 ? joystickInput.y : 0f;
        }

        public float GetBrakeInput()
        {
            return isJoystickActive && joystickInput.y < 0 ? -joystickInput.y : 0f;
        }

        public bool IsBonusActivated()
        {
            if (bonusActivated)
            {
                bonusActivated = false; // Сбрасываем флаг после активации
                return true;
            }
            return false;
        }
    }
}