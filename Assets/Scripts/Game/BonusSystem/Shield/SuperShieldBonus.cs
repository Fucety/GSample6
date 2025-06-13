using UnityEngine;

namespace UshiSoft.UACPF
{
    [CreateAssetMenu(fileName = "SuperShieldBonus", menuName = "KartingRush/Bonuses/SuperShield")]
    public class SuperShieldBonus : BonusBase
    {
        [SerializeField] private float duration = 8f; 
        [SerializeField] private float totalTouchDamage = 25f; // Общий урон от касания за всю длительность
        [SerializeField] private float damageTickRate = 0.25f; // Интервал нанесения урона (может быть другим для суперщита)
        [SerializeField] private GameObject customShieldEffectPrefab;
        [SerializeField] private bool shieldGrantsImmunity = true; // новый параметр

        public override void Activate(CarControllerBase car)
        {
            var health = car.GetComponent<CarHealth>();
            if (health == null) return;
            
            // Теперь передаем общий урон и интервал тиков в ActivateShield
            health.ActivateShield(duration, customShieldEffectPrefab, totalTouchDamage, damageTickRate, car, shieldGrantsImmunity);
        }
    }
}