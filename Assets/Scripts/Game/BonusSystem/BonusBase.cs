using UnityEngine;

namespace UshiSoft.UACPF
{
    // Интерфейс для всех бонусов
    public interface IBonus
    {
        void Activate(CarControllerBase car);
    }

    // Базовый класс для бонусов
    public abstract class BonusBase : ScriptableObject, IBonus
    {
        [SerializeField] private string bonusName;
        [SerializeField] private Sprite icon;
        [SerializeField] private BonusRarity rarity;

        public string BonusName => bonusName;
        public Sprite Icon => icon;
        public BonusRarity Rarity => rarity;

        public abstract void Activate(CarControllerBase car);
    }

    public enum BonusRarity
    {
        Common,
        Rare,
        Epic
    }
}