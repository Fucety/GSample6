using UnityEngine;
using DG.Tweening;

public class AnimationManager : MonoBehaviour
{
    [System.Flags]
    private enum ActionType
    {
        None = 0,
        LanguageWindow = 1 << 0,
        RewardWindow = 1 << 1,
        SettingsMenu = 1 << 2,
        ShopWindow = 1 << 3,
        SoundWindow = 1 << 4
    }

    [Header("Основные настройки")]
    [SerializeField] private ActionType selectedActions; // Выбор нескольких действий через инспектор
    [SerializeField] private Vector3 targetPosition;
    [SerializeField] private float duration;

    [Header("Управление анимацией")]
    [SerializeField] private Ease animationEasePreset = Ease.InOutBack; // Выбор предустановленной кривой
    [SerializeField] private AnimationCurve customAnimationCurve; // Пользовательская кривая
    [SerializeField] private bool useCustomCurve = false; // Флаг использования пользовательской кривой

    [Header("Управление якорями")]
    [SerializeField] private bool changeAnchor = false; // включить переключение якорей
    [SerializeField] private Vector2 targetAnchor = new Vector2(1, 1); // желаемые якоря для угла
    [SerializeField] private Vector2 targetPivot = new Vector2(1, 1);  // желаемый pivot для угла

    [Header("Деактивация")]
    [SerializeField] private bool deactivation = true; // Деактивировать объект или нет
    [SerializeField] private bool OnStartOrInMiddle = false; // Деактивация в начале или в середине анимации
        
    private Vector2 initialAnchorMin;
    private Vector2 initialAnchorMax;
    private Vector2 initialPivot;
    private RectTransform rectButton = null;            // Компонент RectTransform
    private Vector3 initialPosition;                   // Исходная позиция
    private bool AltToggled = false;                   // Флаг состояния анимации
    private bool isProcessing = false;                 // Флаг обработки события

    void Start()
    {
        
        rectButton = gameObject.GetComponent<RectTransform>();
        initialPosition = rectButton.anchoredPosition; // Сохраняем начальную позицию

        // Сохраняем исходные значения якорей и pivot
        initialAnchorMin = rectButton.anchorMin;
        initialAnchorMax = rectButton.anchorMax;
        initialPivot = rectButton.pivot;

        // Подписка на действия
        if (selectedActions.HasFlag(ActionType.LanguageWindow))
        {
            MenuActions.LanguageWindow.AddListener(HandleEvent);
        }

        if (selectedActions.HasFlag(ActionType.RewardWindow))
        {
            MenuActions.RewardWindow.AddListener(HandleEvent);
        }

        if (selectedActions.HasFlag(ActionType.SettingsMenu))
        {
            MenuActions.SettingsMenu.AddListener(HandleEvent);
        }

        if (selectedActions.HasFlag(ActionType.ShopWindow))
        {
            MenuActions.ShopWindow.AddListener(HandleEvent);
        }

        if (selectedActions.HasFlag(ActionType.SoundWindow))
        {
            MenuActions.SoundWindow.AddListener(HandleEvent);
        }

        if(deactivation && !OnStartOrInMiddle)
            gameObject.SetActive(false);

    }

    private void HandleEvent(int isToggled)
    {
        if (isProcessing) return; // Пропускаем событие, если уже обрабатывается другое
        isProcessing = true;

        // Запускаем анимацию
        ToggleAnimation(isToggled);

        // Очистка: освобождаем очередь событий после выполнения
        Invoke(nameof(ClearProcessingFlag), duration);
    }

    private void ClearProcessingFlag()
    {
        isProcessing = false;
    }

    #region Move
    private void ToggleAnimation(int isToggled)
    {
        // Вычисляем конечную позицию в зависимости от значения isToggled
        Vector3 destination;
        if (isToggled == 0)
        {
            bool newToggled = !AltToggled;
            destination = newToggled ? targetPosition : initialPosition;
            AltToggled = newToggled;
        }
        else if (isToggled != 0)
        {
            if (isToggled == -1)
            {
                destination = initialPosition;
                AltToggled = false;
            }
            else if (isToggled == 1)
            {
                destination = targetPosition;
                AltToggled = true;
            }
            else
            {
                return;
            }
        }
        else
        {
            return;
        }

        // Если объект уже находится в точке назначения, не активировать его и не запускать анимацию
        if (Vector3.Distance(rectButton.anchoredPosition, destination) < 0.01f)
            return;

        gameObject.SetActive(true);
        Debug.Log($"Event Triggered with value: {isToggled}");

        if (isToggled == 0)
        {
            AnimateToPosition(destination);
        }
        else if (isToggled != 0)
        {
            Tween t = AnimateToPosition(destination);
            if ((isToggled == -1 && deactivation && !OnStartOrInMiddle) ||
                (isToggled == 1 && deactivation && OnStartOrInMiddle))
            {
                t.OnComplete(() => { gameObject.SetActive(false); });
            }
        }
    }

    private Tween AnimateToPosition(Vector3 position)
    {
        // Добавленный код: если объект уже в целевой позиции, вернуть мгновенный tween
        // if (Vector3.Distance(rectButton.anchoredPosition, position) < 0.01f)
        // {
        //     return DOTween.To(() => 0, x => { }, 0, 0);
        // }

        if (changeAnchor)
        {
            // Сохраняем текущую привязанную позицию
            Vector2 currentAnchoredPosition = rectButton.anchoredPosition;
        
            if (position == targetPosition)
            {
                // Вычисляем разницу между старыми и новыми якорями
                Vector2 anchorDelta = targetAnchor - rectButton.anchorMin;
            
                // Устанавливаем новые якоря и pivot
                rectButton.anchorMin = targetAnchor;
                rectButton.anchorMax = targetAnchor;
                rectButton.pivot = targetPivot;

                // Корректируем позицию с учетом смещения якорей
                Vector2 adjustedPosition = currentAnchoredPosition - new Vector2(
                    anchorDelta.x * rectButton.parent.GetComponent<RectTransform>().rect.width,
                    anchorDelta.y * rectButton.parent.GetComponent<RectTransform>().rect.height
                );
            
                // Устанавливаем скорректированную позицию
                rectButton.anchoredPosition = adjustedPosition;
            }
            else if (position == initialPosition)
            {
                // Вычисляем разницу между текущими и исходными якорями
                Vector2 anchorDelta = initialAnchorMin - rectButton.anchorMin;
            
                // Устанавливаем исходные якоря и pivot
                rectButton.anchorMin = initialAnchorMin;
                rectButton.anchorMax = initialAnchorMax;
                rectButton.pivot = initialPivot;

                // Корректируем позицию с учетом смещения якорей
                Vector2 adjustedPosition = currentAnchoredPosition - new Vector2(
                    anchorDelta.x * rectButton.parent.GetComponent<RectTransform>().rect.width,
                    anchorDelta.y * rectButton.parent.GetComponent<RectTransform>().rect.height
                );
            
                // Устанавливаем скорректированную позицию
                rectButton.anchoredPosition = adjustedPosition;
            }
        }
        Tween tween;
        if (useCustomCurve && customAnimationCurve != null)
        {
            tween = rectButton.DOAnchorPos(position, duration).SetEase(customAnimationCurve);

        }
        else
        {
            tween = rectButton.DOAnchorPos(position, duration).SetEase(animationEasePreset);

        }
        return tween;
    }
    #endregion
}
