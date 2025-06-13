using UnityEngine;
using System.Collections.Generic;

namespace UshiSoft.UACPF
{
    public class CarHealth : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;

        private class ShieldInstance 
        { 
            public float timer; 
            public GameObject effect; 
            public bool grantImmunity; // новый флаг иммунитета
        }
        private List<ShieldInstance> activeShields = new List<ShieldInstance>();

        private float currentHealth;
        private int eliminations;
        private bool isDead = false;
        private CarControllerBase lastAttacker;

        [SerializeField] private GameObject deathParticlePrefab;

        private void Start()
        {
            currentHealth = maxHealth;
        }

        private void Update()
        {
            for (int i = activeShields.Count - 1; i >= 0; i--)
            {
                activeShields[i].timer -= Time.deltaTime;
                if (activeShields[i].timer <= 0f)
                {
                    activeShields[i].effect.SetActive(false);
                    activeShields.RemoveAt(i);
                }
            }
        }

        public void TakeDamage(float damage, CarControllerBase attacker = null)
        {
            // Блокировать урон, только если существует щит с включенным иммунитетом
            bool hasImmuneShield = activeShields.Exists(shield => shield.grantImmunity);
            if (hasImmuneShield || isDead) return;
            lastAttacker = attacker;
            currentHealth -= damage;
            if (currentHealth <= 0f && !isDead)
            {
                Die();
            }
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;
            Debug.Log($"[{gameObject.name}] Уничтожен.");
            if(deathParticlePrefab != null)
            {
                Instantiate(deathParticlePrefab, transform.position, Quaternion.identity);
            }
            if (GameManager.Instance != null)
            {
                var carController = GetComponent<CarControllerBase>();
                if (carController != null)
                {
                    GameManager.Instance.RegisterElimination(carController);
                }
            }
            gameObject.SetActive(false);
        }

        // Изменённый метод ActivateShield с новым параметром grantImmunity
        public void ActivateShield(float duration, GameObject customPrefab, float totalTouchDamage, float damageTickRate, CarControllerBase owner, bool grantImmunity)
        {
            if (activeShields.Count > 0)
            {
                Debug.Log($"[{gameObject.name}] Уже есть активный щит. Новый щит не будет активирован.");
                return;
            }

            if (customPrefab != null)
            {
                var shieldObj = Instantiate(customPrefab, transform);
                shieldObj.SetActive(true); // Убеждаемся, что объект щита активирован
                var ramming = shieldObj.GetComponent<RammingDamage>();
                if (ramming != null)
                {
                    ramming.Initialize(totalTouchDamage, duration, damageTickRate, owner);
                }
                else
                {
                    Debug.LogWarning($"[{customPrefab.name}] Префаб щита не содержит компонента RammingDamage. Урон от касания не будет работать.");
                }
                activeShields.Add(new ShieldInstance { timer = duration, effect = shieldObj, grantImmunity = grantImmunity });
            }
        }

        public void Respawn()
        {
            currentHealth = maxHealth;
            isDead = false;
            lastAttacker = null;
            gameObject.SetActive(true);

            // При респавне деактивируем и очищаем все активные щиты.
            // Это важно, чтобы избежать "висячих" щитов после смерти.
            foreach (var shield in activeShields)
            {
                if (shield.effect != null)
                {
                    shield.effect.SetActive(false);
                }
            }
            activeShields.Clear(); // Очищаем список после деактивации

            var checkpointTrigger = GetComponent<CheckpointTrigger>();
            if (checkpointTrigger != null)
            {
                checkpointTrigger.Respawn();
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] CheckpointTrigger не найден при респавне!");
            }

            var botControl = GetComponent<BotCarControl>();
            if (botControl != null)
            {
                botControl.Respawn();
            }
        }

        // Этот метод по-прежнему нужен для RammingDamage для проверки активности щита
        public bool IsShieldActive()
        {
            return activeShields.Count > 0;
        }

        public void AddElimination()
        {
            eliminations++;
            if (GetComponent<PlayerCarControl>() != null)
                UIManager.Instance.UpdateEliminations(eliminations);
        }

        public int Eliminations => eliminations;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public CarControllerBase LastAttacker => lastAttacker;
    }
}