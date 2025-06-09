using UnityEngine;

namespace UshiSoft.UACPF
{
    public class CarHealth : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        private float currentHealth;
        private bool isShielded;
        private float shieldTimer;
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
            if (isShielded)
            {
                if ((shieldTimer -= Time.deltaTime) <= 0f)
                    isShielded = false;
            }
        }

        public void TakeDamage(float damage, CarControllerBase attacker = null)
        {
            if (isShielded || isDead) return;
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

        public void ActivateShield(float duration)
        {
            isShielded = true;
            shieldTimer = duration;
        }

        // Обновленный метод респавна
        public void Respawn()
        {
            currentHealth = maxHealth;
            isDead = false;
            lastAttacker = null;
            gameObject.SetActive(true);

            // 1. Перемещаем машину на точку спавна
            var checkpointTrigger = GetComponent<CheckpointTrigger>();
            if (checkpointTrigger != null)
            {
                checkpointTrigger.Respawn();
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] CheckpointTrigger не найден при респавне!");
            }

            // 2. Если это бот, сбрасываем его AI
            var botControl = GetComponent<BotCarControl>();
            if (botControl != null)
            {
                botControl.Respawn();
            }

            // 3. Активируем щит неуязвимости
            ActivateShield(3f);
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