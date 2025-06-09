using UnityEngine;

namespace UshiSoft.UACPF
{
    public class Rocket : MonoBehaviour
    {
        [SerializeField] private float speed = 20f;    // Скорость ракеты
        [SerializeField] private float lifetime = 5f;  // Время жизни
        [SerializeField] private float damage = 100f;       // Новый параметр урона
        [SerializeField] private GameObject explosionEffect; // Новый эффект взрыва

        private bool hasExploded = false; // Добавляем флаг для предотвращения повторного урона

        private CarControllerBase owner; // Добавлено поле для владельца ракеты
        private Rigidbody rb; // Добавляем ссылку на Rigidbody

        private void Awake()
        {
            rb = GetComponent<Rigidbody>(); // Получаем компонент Rigidbody
            if (rb == null)
            {
                // Опционально: добавить Rigidbody, если его нет
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true; // Делаем его кинематическим, так как мы управляем движением вручную
            }
        }

        private void Start()
        {
            Destroy(gameObject, lifetime); // Уничтожаем через 5 секунд
        }

        private void FixedUpdate() // Изменено на FixedUpdate
        {
            // Используем Rigidbody для перемещения, если он есть
            if (rb != null)
            {
                rb.MovePosition(rb.position + transform.forward * speed * Time.fixedDeltaTime);
            }
            else
            {
                // Если Rigidbody нет (что не рекомендуется для физики), используем transform.Translate
                transform.Translate(Vector3.forward * speed * Time.fixedDeltaTime);
            }
        }

        // Новый метод для установки владельца
        public void SetOwner(CarControllerBase owner)
        {
            this.owner = owner;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasExploded) return;

            // Игнорируем столкновение с владельцем ракеты
            // Проверяем, что owner не null и что gameObject, с которым произошло столкновение,
            // не является gameObject владельца.
            if (owner != null && other.gameObject == owner.gameObject)
            {
                return; // Игнорируем это столкновение
            }


            // Пытаемся найти компонент CarHealth сначала на объекте, затем у родителя
            CarHealth targetHealth = other.GetComponent<CarHealth>();
            if (targetHealth == null)
                targetHealth = other.GetComponentInParent<CarHealth>();

            if (targetHealth != null)
            {
                // Если столкнулись с машиной, которая не является владельцем
                Debug.Log($"Rocket hitting {targetHealth.gameObject.name} with damage {damage}. Health before: {targetHealth.CurrentHealth}");
                targetHealth.TakeDamage(damage, owner); // Передаём владельца
                Debug.Log($"Health after damage: {targetHealth.CurrentHealth}");
                hasExploded = true;
            }
            else
            {
                // Если столкнулись с чем-то, что не является машиной (препятствие)
                hasExploded = true;
            }

            // Вне зависимости от наличия CarHealth: воспроизводим эффект взрыва, если он задан, и уничтожаем ракету.
            if (hasExploded)
            {
                if (explosionEffect != null)
                {
                    Instantiate(explosionEffect, transform.position, Quaternion.identity);
                }
                Destroy(gameObject);
            }
        }
    }
}