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

    private RectTransform rectButton = null;            // Компонент RectTransform
    private Vector3 initialPosition;                   // Исходная позиция
    private bool AltToggled = false;                   // Флаг состояния анимации
    private bool isProcessing = false;                 // Флаг обработки события

    void Start()
    {
        rectButton = gameObject.GetComponent<RectTransform>();
        initialPosition = rectButton.anchoredPosition; // Сохраняем начальную позицию

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
        Debug.Log($"Event Triggered with value: {isToggled}");
        if (isToggled == 0)
        {
            AltToggled = !AltToggled;
            if (!AltToggled)
            {
                AnimateToPosition(initialPosition);
            }
            else
            {
                AnimateToPosition(targetPosition);
            }
        }
        else if (isToggled != 0)
        {
            if (isToggled == -1)
            {
                AnimateToPosition(initialPosition);
                AltToggled = false;
            }
            else if (isToggled == 1)
            {
                AnimateToPosition(targetPosition);
                AltToggled = true;
            }
        }
    }

    private void AnimateToPosition(Vector3 position)
    {
        if (useCustomCurve && customAnimationCurve != null)
        {
            rectButton.DOAnchorPos(position, duration).SetEase(customAnimationCurve);
        }
        else
        {
            rectButton.DOAnchorPos(position, duration).SetEase(animationEasePreset);
        }
    }
    #endregion
}
