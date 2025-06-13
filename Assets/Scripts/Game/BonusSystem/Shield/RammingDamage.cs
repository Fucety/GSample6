using UnityEngine;

namespace UshiSoft.UACPF
{
    public class RammingDamage : MonoBehaviour
    {
        private float damagePerSecond; // Урон в секунду
        private CarControllerBase owner;
        private CarHealth ownerHealth; // Ссылка на CarHealth владельца

        private float damageTickRate; // Интервал нанесения урона, теперь приходит из SO
        private float lastDamageTime; // Время последнего нанесения урона

        private void Start()
        {
            var col = GetComponent<Collider>();
            if(col != null && !col.isTrigger)
            {
                col.isTrigger = true;
                Debug.Log($"[{gameObject.name}] Коллайдер переведен в режим Trigger.");
            }
        }
        
        // Измененная сигнатура Initialize для принятия damageTickRate из SO
        public void Initialize(float totalDamage, float duration, float tickRate, CarControllerBase car)
        {
            damageTickRate = tickRate; // Присваиваем значение из SO
            
            if (duration > 0 && damageTickRate > 0)
            {
                // Если общий урон за длительность, вычисляем урон в секунду
                damagePerSecond = totalDamage / duration; 
            }
            else if (duration > 0 && damageTickRate <= 0) // Если нет тикрейта, урон наносится мгновенно при первом касании
            {
                 Debug.LogWarning($"[{gameObject.name}] damageTickRate <= 0, урон будет нанесен один раз. Проверьте настройки ShieldBonus/SuperShieldBonus.");
                 damagePerSecond = totalDamage; // В этом случае, это просто общий урон
                 damageTickRate = 1000f; // Устанавливаем очень большой кулдаун, чтобы было только одно срабатывание
            }
            else // Если duration <= 0, то damagePerSecond это просто totalDamage
            {
                damagePerSecond = totalDamage;
                damageTickRate = 1000f; // Если длительности нет, урон наносится один раз.
            }
            
            owner = car;
            if (owner != null)
            {
                ownerHealth = owner.GetComponent<CarHealth>();
            }
            
            lastDamageTime = -damageTickRate; // Инициализируем, чтобы первый тик урона сработал сразу
        }

        private void OnTriggerEnter(Collider other)
        {
            // Здесь можно добавить эффект, когда машина ВПЕРВЫЕ задевает щит
        }

        private void OnTriggerStay(Collider other)
        {
            if (!gameObject.activeInHierarchy || owner == null || ownerHealth == null || !ownerHealth.IsShieldActive())
            {
                return;
            }

            if (Time.time < lastDamageTime + damageTickRate)
            {
                return; 
            }

            var targetHealth = other.gameObject.GetComponentInParent<CarHealth>();
            
            if(targetHealth != null && targetHealth.gameObject != owner.gameObject)
            {
                float actualDamage = damagePerSecond * damageTickRate;

                // Убедимся, что урон не отрицательный
                if (actualDamage < 0) actualDamage = 0;

                Debug.Log($"[{gameObject.name}] Нанесение постепенного урона: {actualDamage:F2} объекту {other.gameObject.name} (каждые {damageTickRate:F2}с)");
                targetHealth.TakeDamage(actualDamage, owner);
                lastDamageTime = Time.time; 
            }
        }

        private void OnTriggerExit(Collider other)
        {
            // Здесь можно добавить эффект, когда машина ПЕРЕСТАЕТ задевать щит
        }
    }
}