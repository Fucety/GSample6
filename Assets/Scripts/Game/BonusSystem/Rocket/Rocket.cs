using UnityEngine;

namespace UshiSoft.UACPF
{
    public class Rocket : MonoBehaviour
    {
        [SerializeField] private float speed = 20f;    // Скорость ракеты
        // [SerializeField] private float lifetime = 5f;  // Время жизни (теперь управляется пулом)
        [SerializeField] private float damage = 100f;       // Урон
        [SerializeField] private GameObject explosionEffectPrefab; // Префаб эффекта взрыва (теперь для пула)

        private bool hasExploded = false; 

        private CarControllerBase owner; 
        private Rigidbody rb; 

        private string poolTag = "Rocket"; // Тэг для пула ракеты
        private string explosionPoolTag = "Explosion"; // Тэг для пула эффектов взрыва

        private void Awake()
        {
            rb = GetComponent<Rigidbody>(); 
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true; 
            }
        }

        private void OnEnable() // Используем OnEnable для запуска таймера, так как объекты будут пулиться
        {
            hasExploded = false; // Сбрасываем флаг при активации из пула
            // Запускаем корутину для возврата в пул по истечении "времени жизни"
            // StartCoroutine(ReturnToPoolAfterLifetime(lifetime)); 
            // Однако, для ракеты, которая летит и может столкнуться,
            // более корректно будет использовать только OnTriggerEnter/OnCollisionEnter
            // для возврата в пул. Если ракета пролетает мимо цели, она будет висеть
            // в сцене. Можно добавить таймер для возврата в пул, если не столкнулась.
            // Для этого нужна ссылка на таймер, который будет отменяться при столкновении.
        }

        // Если вы хотите, чтобы ракета возвращалась в пул, если не столкнулась
        private float currentLifetimeTimer;
        [SerializeField] private float maxLifetime = 5f; // Максимальное время жизни для ракеты в полете

        private void Update()
        {
            if (hasExploded) return; // Если уже взорвалась, не обновляем

            currentLifetimeTimer += Time.deltaTime;
            if (currentLifetimeTimer >= maxLifetime)
            {
                // Если время жизни истекло, возвращаем в пул
                // И не запускаем эффект взрыва (так как не было столкновения)
                ReturnSelfToPool();
            }
        }

        private void FixedUpdate() 
        {
            if (hasExploded) return; // Не двигаем взорвавшуюся ракету

            if (rb != null)
            {
                rb.MovePosition(rb.position + transform.forward * speed * Time.fixedDeltaTime);
            }
        }

        public void SetOwner(CarControllerBase car)
        {
            owner = car;
            hasExploded = false; // Сбрасываем флаг при новом использовании
            currentLifetimeTimer = 0f; // Сбрасываем таймер жизни
        }

        private void OnTriggerEnter(Collider other)
        {
            // Добавляем проверку: если объект из слоя "Bonus", выходим
            if (other.gameObject.layer == LayerMask.NameToLayer("Bonus"))
                return;
                
            if (hasExploded) return;
            
            // Игнорируем столкновение с владельцем
            if (owner != null && other.gameObject == owner.gameObject)
            {
                return;
            }
            
            CarHealth targetHealth = other.GetComponent<CarHealth>();
            if (targetHealth == null)
                targetHealth = other.GetComponentInParent<CarHealth>();

            if (targetHealth != null && targetHealth.gameObject != owner.gameObject) // Убеждаемся, что не попадаем в себя
            {
                Debug.Log($"Rocket hitting {targetHealth.gameObject.name} with damage {damage}. Health before: {targetHealth.CurrentHealth}");
                targetHealth.TakeDamage(damage, owner);
                Debug.Log($"Health after damage: {targetHealth.CurrentHealth}");
                hasExploded = true; // Устанавливаем флаг, что ракета взорвалась
            }
            else if (targetHealth == null) // Столкнулись с чем-то, что не является машиной (препятствие)
            {
                hasExploded = true;
            }

            // Взрыв и возврат в пул, если столкновение произошло и не было проигнорировано
            if (hasExploded)
            {
                PlayExplosionEffect();
                ReturnSelfToPool();
            }
        }

        private void PlayExplosionEffect()
        {
            if (explosionEffectPrefab != null && ObjectPoolManager.Instance != null)
            {
                GameObject explosion = ObjectPoolManager.Instance.GetPooledObject(explosionPoolTag);
                if (explosion != null)
                {
                    explosion.transform.position = transform.position;
                    // Если эффект взрыва имеет компонент, который должен его вернуть в пул,
                    // то он должен быть реализован в самом эффекте.
                    // Например, скрипт на эффекте, который через N секунд возвращает себя в пул.
                    // Для примера, я просто покажу, как получить его и запустить таймер
                    var returnToPool = explosion.GetComponent<ReturnToPoolAfterTime>();
                    if (returnToPool != null)
                    {
                        returnToPool.Initialize(explosionPoolTag); // Передаем тэг пула, чтобы эффект знал куда вернуться
                    }
                    else
                    {
                        Debug.LogWarning($"Explosion effect {explosion.name} is missing ReturnToPoolAfterTime component!");
                    }
                }
            }
        }

        private void ReturnSelfToPool()
        {
            if (ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.ReturnPooledObject(poolTag, gameObject);
            }
            // else { Destroy(gameObject); } // Запасной вариант, если менеджера нет
        }
    }
}