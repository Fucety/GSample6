using UnityEngine;

namespace UshiSoft.UACPF
{
    [CreateAssetMenu(fileName = "BoostBonus", menuName = "KartingRush/Bonuses/Boost")]
    public class BoostBonus : BonusBase
    {
        [SerializeField] private float boostForce = 50f;  // Сила ускорения
        [SerializeField] private float duration = 3f;       // Длительность ускорения
        [SerializeField] private float boostRampTime = 0.3f; // Время нарастания/замедления буста
        [SerializeField] private float reversePhaseDuration = 0.5f; // Длительность обратной (замедляющей) фазы

        // Новые параметры для особого щита
        [SerializeField] private GameObject specialShieldEffectPrefab;
        [SerializeField] private float specialShieldTotalTouchDamage = 10f;
        [SerializeField] private float specialDamageTickRate = 0.3f;
        [SerializeField] private bool shieldGrantsImmunity = false; // новый параметр
        
        public override void Activate(CarControllerBase car)
        {
            // Применяем буст с учетом boostRampTime и reversePhaseDuration
            car.ApplyBoost(boostForce, duration, boostRampTime, reversePhaseDuration);
            
            // Активируем особый щит по принципу ShieldBonus
            var health = car.GetComponent<CarHealth>();
            if (health != null)
            {
                health.ActivateShield(duration, specialShieldEffectPrefab, specialShieldTotalTouchDamage, specialDamageTickRate, car, shieldGrantsImmunity);
            }
        }
    }
}