using UnityEngine;

// Инициализатор для применения скинов при старте игры и при выборе нового скина
public class SkinInitializer : MonoBehaviour
{
    [SerializeField] private ShopPanelDataSO panelData; // ScriptableObject с данными скинов
    [SerializeField] private GameObject standObject; // Объект на сцене (Stand/Скин)
    
    private SkinLoader skinLoader; // Загрузчик скинов

    // Инициализация при старте
    private void Awake()
    {
        // Проверка на наличие необходимых компонентов
        if (panelData == null)
        {
            Debug.LogError("ShopPanelDataSO не привязан в инспекторе!");
            return;
        }
        if (standObject == null)
        {
            Debug.LogError("StandObject не привязан в инспекторе!");
            return;
        }

        // Создаем загрузчик скинов
        skinLoader = new SkinLoader(panelData);
        
        // Применяем сохраненный скин при старте
        ApplySavedSkin();
        
        // Подписываемся на событие выбора скина
        MenuActions.SkinSelected.AddListener(ApplySkin);
    }

    // Применяет сохраненный скин при старте игры
    private void ApplySavedSkin()
    {
        skinLoader.ApplySavedSkinToObject(standObject);
    }

    // Применяет выбранный скин при вызове события
    private void ApplySkin(string spriteName)
    {
        skinLoader.ApplySkinToObject(standObject, spriteName);
    }

    // Отписка от события при уничтожении объекта
    private void OnDestroy()
    {
        MenuActions.SkinSelected.RemoveListener(ApplySkin);
    }
}