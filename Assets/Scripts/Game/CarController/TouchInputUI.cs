using UnityEngine;
using UnityEngine.UI;

namespace UshiSoft.UACPF
{
    // UI-компонент для отображения сенсорного джойстика
    public class TouchInputUI : MonoBehaviour
    {
        [SerializeField] private RectTransform joystickBackground; // Фон джойстика
        [SerializeField] private RectTransform joystickHandle; // Ручка джойстика
        [SerializeField] private Canvas canvas; // Canvas для масштабирования

        private Vector2 joystickCenter; // Центр джойстика в экранных координатах
        private Vector2 initialBackgroundPos; // Начальная позиция фона

        private void Awake()
        {
            initialBackgroundPos = joystickBackground.anchoredPosition;
            joystickBackground.gameObject.SetActive(false);
        }

        public Vector2 JoystickCenter => joystickCenter;

        public void ShowJoystick(Vector2 screenPosition)
        {
            joystickBackground.gameObject.SetActive(true);
            joystickCenter = screenPosition;

            // Конвертируем экранные координаты в anchoredPosition для Canvas
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.GetComponent<RectTransform>(),
                screenPosition,
                canvas.worldCamera,
                out Vector2 localPoint);
            joystickBackground.anchoredPosition = localPoint;

            joystickHandle.anchoredPosition = Vector2.zero;
        }

        public void UpdateJoystickHandle(Vector2 screenPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.GetComponent<RectTransform>(),
                screenPosition,
                canvas.worldCamera,
                out Vector2 localPoint);
            Vector2 delta = localPoint - joystickBackground.anchoredPosition;
            joystickHandle.anchoredPosition = delta;
        }

        public void HideJoystick()
        {
            joystickBackground.gameObject.SetActive(false);
            joystickBackground.anchoredPosition = initialBackgroundPos;
            joystickHandle.anchoredPosition = Vector2.zero;
        }
    }
}