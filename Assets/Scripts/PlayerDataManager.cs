using UnityEngine;
using System.Collections.Generic;
using YG;
using System;

// Этот класс-посредник предоставляет удобные методы для работы с сохранениями через YG2.
public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    // Событие для оповещения об изменении монет
    public static event Action<int> OnCoinsChanged;

    // Имя скина по умолчанию для новых игроков
    private const string DefaultSkin = "Car_Skin_01";

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
        }
    }

    private void OnEnable()
    {
        YG2.onDefaultSaves += InitializeOnFirstLaunch;
    }

    private void OnDisable()
    {
        YG2.onDefaultSaves -= InitializeOnFirstLaunch;
    }

    private void InitializeOnFirstLaunch()
    {
        Debug.Log("[PlayerDataManager] Первый запуск! Выдаем дефолтные значения.");
        YG2.saves.totalCoins = 0;
        YG2.saves.unlockedSkins = new List<string> { DefaultSkin };
        YG2.saves.equippedSkin = DefaultSkin;
        YG2.saves.unlockedRewards = new List<string>(); // Инициализация списка наград
        YG2.SaveProgress();
        OnCoinsChanged?.Invoke(YG2.saves.totalCoins);
    }

    // --- API для работы с монетами ---

    public void AddCoins(int amount)
    {
        if (amount < 0) return;
        YG2.saves.totalCoins += amount;
        YG2.SaveProgress();
        OnCoinsChanged?.Invoke(YG2.saves.totalCoins);
        Debug.Log($"[PlayerDataManager] Добавлено {amount} монет. Новый баланс: {YG2.saves.totalCoins}");
    }

    public bool TrySpendCoins(int amount)
    {
        if (amount < 0) return false;
        if (YG2.saves.totalCoins >= amount)
        {
            YG2.saves.totalCoins -= amount;
            YG2.SaveProgress();
            OnCoinsChanged?.Invoke(YG2.saves.totalCoins);
            Debug.Log($"[PlayerDataManager] Потрачено {amount} монет. Новый баланс: {YG2.saves.totalCoins}");
            return true;
        }
        Debug.LogWarning($"[PlayerDataManager] Недостаточно монет для траты: {amount}");
        return false;
    }

    public int GetCoins()
    {
        return YG2.saves.totalCoins;
    }

    // --- API для работы со скинами ---

    public void UnlockSkin(string skinName)
    {
        if (IsSkinUnlocked(skinName)) return;
        YG2.saves.unlockedSkins.Add(skinName);
        YG2.SaveProgress();
        Debug.Log($"[PlayerDataManager] Скин {skinName} разблокирован.");
    }

    public bool IsSkinUnlocked(string skinName)
    {
        return YG2.saves.unlockedSkins.Contains(skinName);
    }

    public void EquipSkin(string skinName)
    {
        if (!IsSkinUnlocked(skinName)) return;
        YG2.saves.equippedSkin = skinName;
        YG2.SaveProgress();
        MenuActions.SelectSkin(skinName);
        Debug.Log($"[PlayerDataManager] Скин {skinName} экипирован.");
    }

    public string GetEquippedSkin()
    {
        return string.IsNullOrEmpty(YG2.saves.equippedSkin) ? DefaultSkin : YG2.saves.equippedSkin;
    }

    // --- API для работы с наградами ---

    public void UnlockReward(string rewardName)
    {
        if (IsRewardUnlocked(rewardName)) return;
        YG2.saves.unlockedRewards.Add(rewardName);
        YG2.SaveProgress();
        Debug.Log($"[PlayerDataManager] Награда {rewardName} разблокирована.");
    }

    public bool IsRewardUnlocked(string rewardName)
    {
        return YG2.saves.unlockedRewards.Contains(rewardName);
    }
}