using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class SkinData
{
    public string spriteName; // Имя скина
    public int price;         // Цена скина
}

[CreateAssetMenu(fileName = "ShopPanelData", menuName = "PanelData/ShopPanelData")]
public class ShopPanelDataSO : ScriptableObject
{
    public SkinData[] skins;

    // Метод для обеспечения сохранения изменений
    #if UNITY_EDITOR
    private void OnValidate()
    {
        // Устанавливаем флаг "грязного" состояния и сохраняем ассет
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    #endif
}