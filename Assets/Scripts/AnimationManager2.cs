using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;

[System.Serializable]
public class AnimationConfig
{
    public bool isButton = false;
    public Vector3 targetPosition;
    public float duration = 0.1f;
    public List<WindowAction> affectedWindows = new List<WindowAction>();
    public List<WindowType> subscribedEvents = new List<WindowType>();
}

[System.Serializable]
public class WindowAction
{
    public WindowType window;
    public ActionType actionValue;
}

public enum WindowType { RewardWindow, LanguageWindow, SettingsMenu, ShopWindow, SoundWindow }
public enum ActionType { Activate = 1, Deactivate = -1, Toggle = 0 }
public enum DeactivationTiming { OnStart, InMiddle }

public class AnimationManager2 : MonoBehaviour
{
    #region Поля и Инициализация
    [Header("Configuration")]
    [SerializeField] private AnimationConfig config = new AnimationConfig();
    [SerializeField] private bool usePositionModule = true;
    [SerializeField] private bool useAnchorModule = false;
    [SerializeField] private bool useDeactivation = false;

    [Header("Animation")]
    [SerializeField, ConditionalHide("usePositionModule")] 
    private bool useCustomCurve = false;
    [SerializeField, ConditionalHide("usePositionModule")] 
    private Ease easePreset = Ease.InOutBack;
    [SerializeField, ConditionalHide("usePositionModule")] 
    private AnimationCurve customCurve;

    [Header("Anchors")]
    [SerializeField, ConditionalHide("useAnchorModule")] 
    private Vector2 targetAnchor = Vector2.one;
    [SerializeField, ConditionalHide("useAnchorModule")] 
    private Vector2 targetPivot = Vector2.one;

    [Header("Deactivation")]
    [SerializeField, ConditionalHide("useDeactivation")] 
    private DeactivationTiming deactivationTiming = DeactivationTiming.OnStart;

    private RectTransform rt;
    private Vector3 initialPos;
    private Vector2 initialAnchor, initialPivot;
    private bool isProcessing = false;
    private bool isActive = false;
    private Button btn;

    void Start()
    {
        rt = GetComponent<RectTransform>();
        btn = GetComponent<Button>();
        initialPos = rt.anchoredPosition;
        initialAnchor = rt.anchorMin;
        initialPivot = rt.pivot;

        SetupEventSubscriptions(true);
        
        if (config.isButton && btn != null)
            btn.onClick.AddListener(OnButtonClick);

        if (useDeactivation && deactivationTiming == DeactivationTiming.OnStart)
            gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        SetupEventSubscriptions(false);
        if (config.isButton && btn != null)
            btn.onClick.RemoveListener(OnButtonClick);
    }
    #endregion

    #region Подписки и Обработка Событий
    private void SetupEventSubscriptions(bool subscribe)
    {
        if (config.subscribedEvents == null) return;

        foreach (WindowType type in config.subscribedEvents)
        {
            var evt = GetEventByType(type);
            if (subscribe)
                evt?.AddListener(HandleEvent);
            else
                evt?.RemoveListener(HandleEvent);
        }
    }

    private UnityEngine.Events.UnityEvent<int> GetEventByType(WindowType type)
    {
        switch (type)
        {
            case WindowType.RewardWindow: return MenuActions.RewardWindow;
            case WindowType.LanguageWindow: return MenuActions.LanguageWindow;
            case WindowType.SettingsMenu: return MenuActions.SettingsMenu;
            case WindowType.ShopWindow: return MenuActions.ShopWindow;
            case WindowType.SoundWindow: return MenuActions.SoundWindow;
            default: return null;
        }
    }

    private void HandleEvent(int toggleValue)
    {
        if (isProcessing) return;
        isProcessing = true;

        StartAnimation(toggleValue);
    }

    private void StartAnimation(int toggleValue)
    {
        Tween tween = AnimateWithValue(toggleValue);
        if (tween != null)
        {
            tween.OnComplete(() =>
            {
                isProcessing = false;
                if (useDeactivation)
                {
                    bool shouldDeactivate = (!isActive && deactivationTiming == DeactivationTiming.OnStart) || 
                                           (isActive && deactivationTiming == DeactivationTiming.InMiddle);
                    if (shouldDeactivate)
                        gameObject.SetActive(false);
                }
            });
        }
        else
        {
            Invoke(nameof(ClearProcessingFlag), config.duration);
        }
    }

    private void ClearProcessingFlag() => isProcessing = false;
    #endregion

    #region Обработчики Кнопок
    private void OnButtonClick()
    {
        if (isProcessing || !config.isButton) return;

        // Передаем 0 для переключения состояния (тоггл)
        int toggleValue = 0;
        Debug.Log("isActive: " + isActive + " -> переключение состояния");

        foreach (var action in config.affectedWindows)
        {
            int value = action.actionValue == ActionType.Toggle ? toggleValue : (int)action.actionValue;
            TriggerWindowEvent(action.window, value);
        }
    }

    private void TriggerWindowEvent(WindowType window, int value)
    {
        switch (window)
        {
            case WindowType.RewardWindow: MenuActions.RewardWActivate(value); break;
            case WindowType.LanguageWindow: MenuActions.LanguageWActivate(value); break;
            case WindowType.SettingsMenu: MenuActions.SettingsMActivate(value); break;
            case WindowType.ShopWindow: MenuActions.ShopWActivate(value); break;
            case WindowType.SoundWindow: MenuActions.SoundWActivate(value); break;
        }
    }
    #endregion

    #region Анимация
    private Tween AnimateWithValue(int toggleValue)
    {
        Vector3 destination;
        switch (toggleValue)
        {
            case 0:
                bool newState = !isActive;
                destination = newState ? config.targetPosition : initialPos;
                isActive = newState;
                break;
            case -1:
                destination = initialPos;
                isActive = false;
                break;
            case 1:
                destination = config.targetPosition;
                isActive = true;
                break;
            default:
                return null;
        }

        if (usePositionModule && Vector3.Distance(rt.anchoredPosition, destination) < 0.01f)
            return null;

        gameObject.SetActive(true);
        Debug.Log("Activated: " + gameObject.name);
        if (useAnchorModule)
            UpdateAnchors(isActive);

        Tween tween = null;
        if (usePositionModule)
        {
            tween = (useCustomCurve && customCurve != null)
                ? rt.DOAnchorPos(destination, config.duration).SetEase(customCurve)
                : rt.DOAnchorPos(destination, config.duration).SetEase(easePreset);
        }
        return tween;
    }
    #endregion

    #region Утилиты
    private void UpdateAnchors(bool toTarget)
    {
        var newAnchor = toTarget ? targetAnchor : initialAnchor;
        var newPivot = toTarget ? targetPivot : initialPivot;
        var currentPos = rt.anchoredPosition;

        Vector2 anchorDelta = newAnchor - rt.anchorMin;
        rt.anchorMin = rt.anchorMax = newAnchor;
        rt.pivot = newPivot;

        if (rt.parent is RectTransform parentRect)
        {
            rt.anchoredPosition = currentPos - new Vector2(
                anchorDelta.x * parentRect.rect.width,
                anchorDelta.y * parentRect.rect.height
            );
        }
    }
    #endregion
}

// Атрибут для условного отображения полей в инспекторе
public class ConditionalHideAttribute : PropertyAttribute
{
    public string ConditionalSourceField;
    public bool HiddenValue;

    public ConditionalHideAttribute(string conditionalSourceField, bool hiddenValue = false)
    {
        ConditionalSourceField = conditionalSourceField;
        HiddenValue = hiddenValue;
    }
}