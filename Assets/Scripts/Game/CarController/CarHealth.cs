using UnityEngine;

namespace UshiSoft.UACPF
{
    public class CarHealth : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f; // Максимальное здоровье
        private float currentHealth;
        private bool isShielded; // Активен ли щит
        private float shieldTimer; // Таймер щита

        private void Start()
        {
            currentHealth = maxHealth;
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
    }
}