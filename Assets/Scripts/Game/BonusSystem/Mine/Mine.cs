// Файл: Mine.cs
using UnityEngine;

namespace UshiSoft.UACPF
{
    public class Mine : MonoBehaviour
    {
        [SerializeField] private float damage = 50f;          // Урон от взрыва
        [SerializeField] private GameObject explosionEffect;  // Визуальный эффект взрыва (опционально)
        [SerializeField] private float lifetime = 20f;        // Время жизни мины, если никто не наехал

        private CarControllerBase owner; // Владелец мины

        public void SetOwner(CarControllerBase car)
        {
            owner = car;
        }

        private void Start()
        {
            // Уничтожаем мину через некоторое время, чтобы не захламлять сцену
            Destroy(gameObject, lifetime);
        }

        private void OnTriggerEnter(Collider other)
        {
            // Ищем компонент здоровья у объекта, который коснулся мины
            CarHealth targetHealth = other.GetComponentInParent<CarHealth>();
            if (targetHealth != null)
            {
                // Проверяем, не является ли столкнувшийся объект владельцем мины
                if (targetHealth.GetComponent<CarControllerBase>() == owner) return;

                // Наносим урон и передаем информацию об атакующем
                targetHealth.TakeDamage(damage, owner);
                
                // Создаем эффект взрыва, если он назначен
                if (explosionEffect != null)
                {
                    Instantiate(explosionEffect, transform.position, Quaternion.identity);
                }
                
                // Уничтожаем мину
                Destroy(gameObject);
            }
        }
    }
}