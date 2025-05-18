using UnityEngine;
using UnityEngine.UI;

public class OrientationManager : MonoBehaviour
{
    // Массив UI-элементов, скейл которых будет меняться
    [SerializeField] private RectTransform[] targetUIElements;

    // Скейл для портретной ориентации
    [SerializeField] private Vector3 portraitScale = new Vector3(1f, 1f, 1f);
    // Скейл для ландшафтной ориентации
    [SerializeField] private Vector3 landscapeScale = new Vector3(1.5f, 1.5f, 1f);
    // Скейл для ПК
    [SerializeField] private Vector3 pcScale = new Vector3(1f, 1f, 1f);

    [Header("Debug")]
    [Tooltip("Включите для симуляции мобильного режима в редакторе Unity.")]
    public bool simulateMobile = false;

    private ScreenOrientation lastOrientation;

    void Start()
    {
        lastOrientation = Screen.orientation;
        ApplyOrientation();
    }

    void Update()
    {
        // Проверяем изменение ориентации всегда
        if(lastOrientation != Screen.orientation)
        {
            lastOrientation = Screen.orientation;
            ApplyOrientation();
        }
    }

    void ApplyOrientation()
    {
        if(targetUIElements == null || targetUIElements.Length == 0)
        {
            Debug.LogWarning("Target UI Elements are not assigned!");
            return;
        }
        
        Vector3 targetScale;

        // Если это ПК или редактор (и не симулируем мобильный) — pcScale, иначе по ориентации
        if ((Application.platform == RuntimePlatform.WindowsPlayer ||
            Application.platform == RuntimePlatform.OSXPlayer ||
            Application.platform == RuntimePlatform.LinuxPlayer ||
            Application.platform == RuntimePlatform.WindowsEditor ||
            Application.platform == RuntimePlatform.OSXEditor ||
            Application.platform == RuntimePlatform.LinuxEditor)
            && !simulateMobile)
        {
            targetScale = pcScale;
        }
        else if (Screen.orientation == ScreenOrientation.LandscapeLeft ||
                 Screen.orientation == ScreenOrientation.LandscapeRight)
        {
            targetScale = landscapeScale;
        }
        else // Portrait и все остальные случаи
        {
            targetScale = portraitScale;
        }

        // Применяем выбранный скейл ко всем элементам
        foreach (RectTransform element in targetUIElements)
        {
            if (element != null)
            {
                element.localScale = targetScale;
            }
        }
    }
}
