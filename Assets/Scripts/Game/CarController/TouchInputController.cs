using UnityEngine;
using UnityEngine.EventSystems;

namespace UshiSoft.UACPF
{
    // Контроллер сенсорного ввода (джойстик и двойное касание)
    public class TouchInputController : MonoBehaviour, IInputProvider
    {
        [SerializeField, Min(50f)] private float joystickRadius = 50f; // Радиус джойстика
        [SerializeField, Min(0.1f)] private float doubleTapTime = 0.5f; // Время для двойного касания (увеличено для тестирования)
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
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.touches[i];

                // Пропускаем касания над UI (например, кнопки меню)
                if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    Debug.Log($"Касание {touch.fingerId} над UI, пропускаем.");
                    continue;
                }

                // --- Сценарий 1: Это касание уже назначено активному джойстику ---
                if (touch.fingerId == joystickTouchId)
                {
                    HandleJoystick(touch);
                    continue; // Касание обработано джойстиком, переходим к следующему
                }

                // --- Сценарий 2: Это новое касание (TouchPhase.Began), которое нужно назначить ---
                if (touch.phase == TouchPhase.Began)
                {
                    // Сначала пытаемся обработать это новое касание как потенциальное двойное касание бонуса.
                    // HandleBonusTap обновит tapCount и lastTapTime на основе этого касания.
                    HandleBonusTap();
                    Debug.Log($"Касание {touch.fingerId} - Обработано как потенциальное касание бонуса. tapCount после: {tapCount}");

                    // Если бонус был активирован этим касанием (т.е. это было второе касание двойного тапа)
                    if (bonusActivated)
                    {
                        Debug.Log($"Касание {touch.fingerId} - Бонус активирован, касание поглощено двойным тапом.");
                        continue; // Это касание поглощено активацией бонуса.
                    }
                    // Если бонус НЕ был активирован (т.е. это было одиночное касание, или слишком медленно для двойного тапа)
                    // И в данный момент нет активного джойстика, то это касание становится джойстиком.
                    else if (joystickTouchId == -1)
                    {
                        joystickTouchId = touch.fingerId;
                        isJoystickActive = true;
                        ui.ShowJoystick(touch.position);
                        Debug.Log($"Касание {touch.fingerId} - Бонус не активирован, назначено НОВЫМ джойстиком. Позиция: {touch.position}");
                        // Фаза 'Began' для джойстика обрабатывается путем присвоения ID и отображения UI.
                        // Не нужно дополнительно вызывать HandleJoystick(touch) для 'Began', так как это уже сделано неявно.
                        continue; // Это касание теперь поглощено джойстиком.
                    }
                    // В противном случае (если это одиночное касание, бонус не активирован, И джойстик уже активен другим пальцем)
                    // это одиночное касание просто игнорируется для целей ввода (ни джойстик, ни двойное касание).
                    Debug.Log($"Касание {touch.fingerId} - Одиночное касание, не бонус и не новый джойстик.");
                }
            } // Конец цикла по касаниям

            // --- Логика деактивации джойстика ---
            // Если джойстик был активен, но соответствующее ему касание больше не присутствует, деактивируем его.
            if (joystickTouchId != -1 && !IsTouchActive(joystickTouchId))
            {
                if (isJoystickActive) Debug.Log("Джойстик деактивирован (назначенное касание завершилось).");
                DeactivateJoystick();
            }
            // Если вообще нет касаний, убеждаемся, что джойстик деактивирован.
            else if (Input.touchCount == 0 && isJoystickActive)
            {
                Debug.Log("Джойстик деактивирован (касаний на экране не осталось).");
                DeactivateJoystick();
            }
        }

        private void HandleJoystick(Touch touch)
        {
            switch (touch.phase)
            {
                // Фаза Began обрабатывается выше в HandleTouchInput для нового назначения джойстика
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    Vector2 delta = touch.position - ui.JoystickCenter;
                    delta = Vector2.ClampMagnitude(delta, joystickRadius);
                    joystickInput = delta / joystickRadius;
                    ui.UpdateJoystickHandle(touch.position);
                    // Debug.Log($"Джойстик ID: {touch.fingerId}, Ввод: {joystickInput}");
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    // Деактивация обрабатывается в цикле HandleTouchInput или при проверке в конце кадра
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
            // Debug.Log($"HandleBonusTap вызван. Текущее время: {currentTime}, Время последнего тапа: {lastTapTime}, Начальный tapCount: {tapCount}");

            if (currentTime - lastTapTime < doubleTapTime)
            {
                tapCount++;
                // Debug.Log($"Касание в пределах времени двойного тапа. Новый tapCount: {tapCount}");
                if (tapCount >= 2)
                {
                    bonusActivated = true;
                    tapCount = 0; // Сбрасываем счетчик после активации
                    lastTapTime = 0; // Сбрасываем время, чтобы предотвратить тройные тапы
                    Debug.Log("БОНУС АКТИВИРОВАН!");
                }
            }
            else
            {
                tapCount = 1; // Первое касание новой последовательности
                // Debug.Log($"Касание вне времени двойного тапа. Новый tapCount: {tapCount}");
            }
            lastTapTime = currentTime; // Обновляем время последнего касания
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
                Debug.Log("IsBonusActivated() возвращает true, бонус будет активирован PlayerCarControl.");
                bonusActivated = false; // Сбрасываем флаг после активации
                return true;
            }
            return false;
        }
    }
}