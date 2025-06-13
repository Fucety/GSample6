using UnityEngine;

namespace UshiSoft.UACPF
{
    [CreateAssetMenu(fileName = "ShieldBonus", menuName = "KartingRush/Bonuses/Shield")]
    public class ShieldBonus : BonusBase
    {
        [SerializeField] private float duration = 5f;       // Длительность щита
        [SerializeField] private float totalTouchDamage = 15f;   // Общий урон от касания за всю длительность
        [SerializeField] private float damageTickRate = 0.5f; // Интервал нанесения урона (каждые N секунд)
        [SerializeField] private GameObject customShieldEffectPrefab;
        [SerializeField] private bool shieldGrantsImmunity = true; // новый параметр

        public override void Activate(CarControllerBase car)
        {
            var health = car.GetComponent<CarHealth>();
            if (health == null) return;

            // Передаем shieldGrantsImmunity в метод ActivateShield
            health.ActivateShield(duration, customShieldEffectPrefab, totalTouchDamage, damageTickRate, car, shieldGrantsImmunity);
        }
    }
}