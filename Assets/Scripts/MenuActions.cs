using UnityEngine;
using UnityEngine.Events;

public class MenuActions : MonoBehaviour
{   

    public static UnityEvent<int> LanguageWindow = new UnityEvent<int>();
    public static UnityEvent<int> RewardWindow = new UnityEvent<int>();
    public static UnityEvent<int> SettingsMenu = new UnityEvent<int>();
    public static UnityEvent<int> ShopWindow = new UnityEvent<int>();
    public static UnityEvent<int> SoundWindow = new UnityEvent<int>();

    public static void LanguageWActivate(int value)
    {
        LanguageWindow.Invoke(value);
    }

    public static void RewardWActivate(int value)
    {
        RewardWindow.Invoke(value);
    }

    public static void SettingsMActivate(int value)
    {
        SettingsMenu.Invoke(value);
    }

    public static void ShopWActivate(int value)
    {
        ShopWindow.Invoke(value);
    }

    public static void SoundWActivate(int value)
    {
        SoundWindow.Invoke(value);
    }
}
