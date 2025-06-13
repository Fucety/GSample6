using UnityEngine;
using UnityEngine.Events;

public class MenuActions : MonoBehaviour
{
    // Событие для выбора скина
    public static UnityEvent<string> SkinSelected = new UnityEvent<string>();
    
    public static UnityEvent<int> LanguageWindow = new UnityEvent<int>();
    public static UnityEvent<int> RewardWindow = new UnityEvent<int>();
    public static UnityEvent<int> SettingsMenu = new UnityEvent<int>();
    public static UnityEvent<int> ShopWindow = new UnityEvent<int>();
    public static UnityEvent<int> SoundWindow = new UnityEvent<int>();

    // Вызывает событие выбора скина
    public static void SelectSkin(string spriteName)
    {
        SkinSelected.Invoke(spriteName);
    }

    // Вызывает событие активации окна языка
    public static void LanguageWActivate(int value)
    {
        LanguageWindow.Invoke(value);
    }

    // Вызывает событие активации окна наград
    public static void RewardWActivate(int value)
    {
        RewardWindow.Invoke(value);
    }

    // Вызывает событие активации меню настроек
    public static void SettingsMActivate(int value)
    {
        SettingsMenu.Invoke(value);
    }

    // Вызывает событие активации окна магазина
    public static void ShopWActivate(int value)
    {
        ShopWindow.Invoke(value);
    }

    // Вызывает событие активации окна звука
    public static void SoundWActivate(int value)
    {
        SoundWindow.Invoke(value);
    }
}
