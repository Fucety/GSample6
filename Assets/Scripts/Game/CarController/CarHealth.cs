using UnityEngine;

namespace UshiSoft.UACPF
{
    public class CarHealth : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f; // Максимальное здоровье
        private float currentHealth;
        private bool isShielded; // Активен ли щит
        private float shieldTimer; // Таймер щита
        private int eliminations; // Количество устранённых противников

        private void Start()
        {
            currentHealth = maxHealth;
            eliminations = 0;
        }

        private void Update()
        {
            if (isShielded)
            {
                shieldTimer -= Time.deltaTime;
                if (shieldTimer <= 0f)
                    isShielded = false;
            }
        }

        public void TakeDamage(float damage)
        {
            if (isShielded) return;

            currentHealth -= damage;
            if (currentHealth <= 0f)
            {
                GameManager.Instance.RegisterElimination(GetComponent<CarControllerBase>());
                gameObject.SetActive(false); // Уничтожение машины
            }
        }

        public void ActivateShield(float duration)
        {
            isShielded = true;
            shieldTimer = duration;
        }

        // Респавн машины
        public void Respawn()
        {
            currentHealth = maxHealth;
            gameObject.SetActive(true);
            ActivateShield(3f); // 3 секунды неуязвимости после респавна
            GetComponent<CheckpointTrigger>().Respawn();
        }

        // Инкремент устранений
        public void AddElimination()
        {
            eliminations++;
        }

        // Свойство для доступа к количеству устранений
        public int Eliminations => eliminations;
    }
}