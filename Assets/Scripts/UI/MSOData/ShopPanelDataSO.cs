using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class SkinData
{
    public string spriteName;    // Имя скина
    public int price;           // Цена скина
    public GameObject modelPrefab; // Префаб 3D-модели скина
}

[CreateAssetMenu(fileName = "ShopPanelData", menuName = "PanelData/ShopPanelData")]
public class ShopPanelDataSO : ScriptableObject
{
    public SkinData[] skins;

    // Обеспечивает сохранение изменений в редакторе
    #if UNITY_EDITOR
    private void OnValidate()
    {
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    #endif
}