using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

// ScriptableObject для хранения данных стиля UI
[CreateAssetMenu(fileName = "NewUIStyle", menuName = "UI/Style")]
public class UIStyle : ScriptableObject
{
    [SerializeField] private Color backgroundColor = Color.white;
    [SerializeField] private Color textColor = Color.black;
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Sprite buttonSprite;

    public Color BackgroundColor => backgroundColor;
    public Color TextColor => textColor;
    public TMP_FontAsset Font => font;
    public Sprite ButtonSprite => buttonSprite;
}

// Менеджер стилей для переключения и уведомления об изменении стиля
public class StyleManager : MonoBehaviour
{
    // Синглтон для глобального доступа
    public static StyleManager Instance { get; private set; }

    [SerializeField] private UIStyle defaultStyle;
    private UIStyle currentStyle;

    public event Action<UIStyle> OnStyleChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        currentStyle = defaultStyle;
        OnStyleChanged?.Invoke(currentStyle); // Применяем стиль при старте
    }

    public void SetStyle(UIStyle newStyle)
    {
        if (newStyle != null && newStyle != currentStyle)
        {
            currentStyle = newStyle;
            OnStyleChanged?.Invoke(currentStyle);
        }
    }

    public UIStyle GetCurrentStyle() => currentStyle;
}

// Абстрактный базовый класс для применения стилей к UI элементам
public abstract class UIStyleApplier : MonoBehaviour
{
    protected virtual void Awake()
    {
        StyleManager.Instance.OnStyleChanged += ApplyStyle;
        ApplyStyle(StyleManager.Instance.GetCurrentStyle());
    }

    protected virtual void OnDestroy()
    {
        if (StyleManager.Instance != null)
        {
            StyleManager.Instance.OnStyleChanged -= ApplyStyle;
        }
    }

    protected abstract void ApplyStyle(UIStyle style);
}

// Компонент для применения стиля к Image (например, фон или кнопка)
[RequireComponent(typeof(Image))]
public class UIImageStyleApplier : UIStyleApplier
{
    private Image image;
 
    protected override void Awake()
    {
        image = GetComponent<Image>();
        base.Awake();
    }

    protected override void ApplyStyle(UIStyle style)
    {
        if (image != null && style != null)
        {
            image.color = style.BackgroundColor;
            if (image.sprite == null && style.ButtonSprite != null)
            {
                image.sprite = style.ButtonSprite;
            }
        }
    }
}

// Компонент для применения стиля к текстовым элементам TextMeshPro
[RequireComponent(typeof(TextMeshProUGUI))]
public class UITextStyleApplier : UIStyleApplier
{
    private TextMeshProUGUI text;

    protected override void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
        base.Awake();
    }

    protected override void ApplyStyle(UIStyle style)
    {
        if (text != null && style != null)
        {
            text.color = style.TextColor;
            if (style.Font != null)
            {
                text.font = style.Font;
            }
        }
    }
}