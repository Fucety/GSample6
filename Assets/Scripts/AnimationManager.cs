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
        rectButton = GetComponent<RectTransform>();
        initialPosition = rectButton.anchoredPosition; // Сохраняем начальную позицию

        // Сохраняем исходные значения якорей и pivot
        initialAnchorMin = rectButton.anchorMin;
        initialAnchorMax = rectButton.anchorMax;
        initialPivot = rectButton.pivot;

        // Подписка на события
        if (selectedActions.HasFlag(ActionType.LanguageWindow))
            MenuActions.LanguageWindow.AddListener(HandleEvent);
        if (selectedActions.HasFlag(ActionType.RewardWindow))
            MenuActions.RewardWindow.AddListener(HandleEvent);
        if (selectedActions.HasFlag(ActionType.SettingsMenu))
            MenuActions.SettingsMenu.AddListener(HandleEvent);
        if (selectedActions.HasFlag(ActionType.ShopWindow))
            MenuActions.ShopWindow.AddListener(HandleEvent);
        if (selectedActions.HasFlag(ActionType.SoundWindow))
            MenuActions.SoundWindow.AddListener(HandleEvent);

        if (deactivation && !OnStartOrInMiddle)
            gameObject.SetActive(false);
    }

    private void HandleEvent(int isToggled)
    {
        if (isProcessing) return;
        isProcessing = true;

        ToggleAnimation(isToggled);
        Invoke(nameof(ClearProcessingFlag), duration);
    }

    private void ClearProcessingFlag()
    {
        isProcessing = false;
    }

    #region Move
    private Tween ToggleAnimation(int isToggled)
    {
        Vector3 destination;
        switch (isToggled)
        {
            case 0:
                bool newToggled = !AltToggled;
                destination = newToggled ? targetPosition : initialPosition;
                AltToggled = newToggled;
                break;
            case -1:
                destination = initialPosition;
                AltToggled = false;
                break;
            case 1:
                destination = targetPosition;
                AltToggled = true;
                break;
            default:
                return null;
        }

        if (Vector3.Distance(rectButton.anchoredPosition, destination) < 0.01f)
            return null;

        gameObject.SetActive(true);
        Debug.Log($"Event Triggered with value: {isToggled}");

        Tween tween = AnimateToPosition(destination);
        if (isToggled != 0 && ((isToggled == -1 && deactivation && !OnStartOrInMiddle) ||
            (isToggled == 1 && deactivation && OnStartOrInMiddle)))
        {
            tween.OnComplete(() => gameObject.SetActive(false));
        }
        return tween;
    }

    private void UpdateAnchors(bool toTarget)
    {
        var newAnchor = toTarget ? targetAnchor : initialAnchorMin;
        var newPivot = toTarget ? targetPivot : initialPivot;
        var currentPos = rectButton.anchoredPosition;

        Vector2 anchorDelta = newAnchor - rectButton.anchorMin;
        rectButton.anchorMin = newAnchor;
        rectButton.anchorMax = newAnchor;
        rectButton.pivot = newPivot;

        RectTransform parentRect = rectButton.parent.GetComponent<RectTransform>();
        rectButton.anchoredPosition = currentPos - new Vector2(
            anchorDelta.x * parentRect.rect.width,
            anchorDelta.y * parentRect.rect.height
        );
    }

    private Tween AnimateToPosition(Vector3 position)
    {
        if (changeAnchor)
        {
            if (position == targetPosition)
            {
                UpdateAnchors(true);
            }
            else if (position == initialPosition)
            {
                UpdateAnchors(false);
            }
        }
        Tween tween = (useCustomCurve && customAnimationCurve != null)
            ? rectButton.DOAnchorPos(position, duration).SetEase(customAnimationCurve)
            : rectButton.DOAnchorPos(position, duration).SetEase(animationEasePreset);
        return tween;
    }
    #endregion
}
