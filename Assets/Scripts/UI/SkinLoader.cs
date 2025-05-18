using UnityEngine;

// Класс для загрузки и применения скинов
public class SkinLoader
{
    private readonly ShopPanelDataSO panelData;
    private const string SELECTED_SKIN_KEY = "SelectedSkin";

    public SkinLoader(ShopPanelDataSO panelData)
    {
        if (panelData == null) Debug.LogError("ShopPanelDataSO не может быть null!");
        this.panelData = panelData;
    }

    // Возвращает данные скина по имени
    public SkinData ApplySkin(string spriteName)
    {
        if (panelData.skins == null || panelData.skins.Length == 0)
        {
            Debug.LogWarning("Список скинов пуст!");
            return null;
        }

        foreach (var skin in panelData.skins)
            if (skin.spriteName == spriteName) return skin;

        Debug.LogWarning($"Скин {spriteName} не найден!");
        return null;
    }

    // Применяет скин к объекту
    public void ApplySkinToObject(GameObject targetObject, string spriteName)
    {
        if (targetObject == null) { Debug.LogError("Целевой объект не указан!"); return; }

        var skin = ApplySkin(spriteName);
        if (skin == null || skin.modelPrefab == null) { Debug.LogWarning($"Модель для скина {spriteName} не найдена!"); return; }

        // Поиск существующей модели
        GameObject existingModel = null;
        foreach (Transform child in targetObject.transform)
            if (child.name == skin.modelPrefab.name + "(Clone)") { existingModel = child.gameObject; break; }

        // Активация или создание модели
        if (existingModel != null)
        {
            foreach (Transform child in targetObject.transform)
                child.gameObject.SetActive(child.gameObject == existingModel);
        }
        else
        {
            foreach (Transform child in targetObject.transform) child.gameObject.SetActive(false);
            var newModel = Object.Instantiate(skin.modelPrefab, targetObject.transform);
            newModel.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        PlayerPrefs.SetString(SELECTED_SKIN_KEY, spriteName);
        PlayerPrefs.Save();
    }

    // Применяет сохраненный или первый скин
    public void ApplySavedSkinToObject(GameObject targetObject)
    {
        string savedSkin = PlayerPrefs.GetString(SELECTED_SKIN_KEY, string.Empty);
        if (!string.IsNullOrEmpty(savedSkin))
            ApplySkinToObject(targetObject, savedSkin);
        else if (panelData.skins != null && panelData.skins.Length > 0)
            ApplySkinToObject(targetObject, panelData.skins[0].spriteName);
        else
            Debug.LogWarning("Список скинов пуст, невозможно применить скин!");
    }
}