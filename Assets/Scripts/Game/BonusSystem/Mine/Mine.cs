using UnityEngine;
using System.Collections;

namespace UshiSoft.UACPF
{
    public class Mine : MonoBehaviour
    {
        [SerializeField] private float damage = 50f;          // Урон от взрыва
        [SerializeField] private string explosionPoolTag = "Explosion"; // Тэг для пула эффектов взрыва
        private string minePoolTag = "Mine"; // Тэг для пула самой мины
        [SerializeField] private float maxLifetime = 20f; // Максимальное время жизни мины в активном состоянии

        [SerializeField] private float activationDelay = 0.2f; // НОВОЕ: Задержка перед тем, как мина станет активной
        private bool isReady = false; // НОВОЕ: Флаг готовности мины

        private CarControllerBase owner; // Владелец мины
        private bool hasExploded = false; // Флаг для предотвращения повторных срабатываний

        private void Awake()
        {
            // Убедитесь, что у мины есть Rigidbody, если вы хотите, чтобы она падала на землю.
            // Если мина просто "висит" в воздухе, Rigidbody не нужен, но тогда
            // убедитесь, что её позиция спавна корректна по высоте.
            // Если мина должна падать, убедитесь, что её Rigidbody не кинематический,
            // а коллайдер не помечен как триггер, пока она не "упадет".
            // В нашем случае, поскольку это мина, она, вероятно, должна быть триггером.
            // Если она падает на землю, может потребоваться коллайдер без триггера на короткое время.
            // Для простоты, пока будем считать, что она спавнится на нужной высоте.
        }

        private void OnEnable() // Используем OnEnable для запуска таймера, так как объекты будут пулиться
        {
            hasExploded = false; // Сбрасываем флаг при активации из пула
            isReady = false;     // НОВОЕ: Мина изначально не готова
            StartCoroutine(ActivateMineAfterDelay()); // НОВОЕ: Запускаем корутину активации
            StartCoroutine(ReturnToPoolAfterLifetime()); // Запускаем корутину для возврата в пул по истечении "времени жизни"
        }

        private IEnumerator ActivateMineAfterDelay() // НОВОЕ: Корутина для задержки активации
        {
            yield return new WaitForSeconds(activationDelay);
            isReady = true; // Мина готова к срабатыванию
        }

        private IEnumerator ReturnToPoolAfterLifetime()
        {
            yield return new WaitForSeconds(maxLifetime);
            if (!hasExploded) // Если мина не взорвалась за время жизни, возвращаем в пул
            {
                Debug.Log($"Mine at {transform.position} lifetime expired, returning to pool.");
                ReturnSelfToPool();
            }
        }

        public void SetOwner(CarControllerBase car)
        {
            owner = car;
            // hasExploded и isReady сбрасываются в OnEnable
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasExploded || !isReady) return; // НОВОЕ: Игнорируем, если не готова или уже взорвалась

            // Ищем компонент здоровья у объекта, который коснулся мины
            CarHealth targetHealth = other.GetComponentInParent<CarHealth>();
            if (targetHealth != null)
            {
                // Проверяем, не является ли столкнувшийся объект владельцем мины
                if (targetHealth.GetComponent<CarControllerBase>() == owner) return;

                // Наносим урон и передаем информацию об атакующем
                Debug.Log($"Mine hitting {targetHealth.gameObject.name} with damage {damage}. Health before: {targetHealth.CurrentHealth}");
                targetHealth.TakeDamage(damage, owner);
                Debug.Log($"Health after damage: {targetHealth.CurrentHealth}");

                hasExploded = true; // Отмечаем, что мина взорвалась
            }
            else // Столкнулись с чем-то, что не является машиной (препятствие)
            {
                hasExploded = true;
            }

            // Взрыв и возврат в пул, если столкновение произошло и не было проигнорировано
            if (hasExploded)
            {
                PlayExplosionEffect(); // Воспроизводим эффект взрыва
                ReturnSelfToPool();     // Возвращаем мину в пул
            }
        }

        private void PlayExplosionEffect()
        {
            if (ObjectPoolManager.Instance != null)
            {
                GameObject explosion = ObjectPoolManager.Instance.GetPooledObject(explosionPoolTag);
                if (explosion != null)
                {
                    explosion.transform.position = transform.position;
                    var returnToPool = explosion.GetComponent<ReturnToPoolAfterTime>();
                    if (returnToPool != null)
                    {
                        returnToPool.Initialize(explosionPoolTag);
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
                ObjectPoolManager.Instance.ReturnPooledObject(minePoolTag, gameObject);
            }
        }
    }
}