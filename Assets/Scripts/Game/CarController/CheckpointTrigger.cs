using UnityEngine;
using UnityEngine.AI;

namespace UshiSoft.UACPF
{
    // Управляет триггерами контрольных точек и респавном машины
    [RequireComponent(typeof(Rigidbody))] // Убедимся, что Rigidbody всегда есть
    public class CheckpointTrigger : MonoBehaviour
    {
        private CarControllerBase carController;
        private Rigidbody rb; // Ссылка на компонент Rigidbody

        private void Awake()
        {
            carController = GetComponent<CarControllerBase>();
            if (carController == null) Debug.LogError("CheckpointTrigger требует CarControllerBase!", this);
            
            // Получаем Rigidbody один раз для производительности
            rb = GetComponent<Rigidbody>();
        }

        // Перемещает машину на точку спавна
        public void Respawn()
        {
            Transform spawnPoint = SpawnPointManager.Instance.GetAvailableSpawnPoint();
            if (spawnPoint != null)
            {
                // --- НАЧАЛО ИСПРАВЛЕНИЙ ---

                // 1. Корректно телепортируем физический объект через Rigidbody
                // Это предпочтительнее, чем менять transform.position напрямую
                rb.position = spawnPoint.position;
                rb.rotation = spawnPoint.rotation;

                // 2. Сбрасываем всю инерцию, чтобы машина не "улетала" со спавна
                rb.linearVelocity  = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                
                // --- КОНЕЦ ИСПРАВЛЕНИЙ ---

                // Синхронизируем NavMeshAgent С НОВОЙ ПОЗИЦИЕЙ
                var navMeshAgent = GetComponent<NavMeshAgent>();
                if (navMeshAgent != null)
                {
                    // Warp() должен вызываться после перемещения, чтобы он знал правильную позицию
                    navMeshAgent.Warp(spawnPoint.position);
                }

                SpawnPointManager.Instance.SetSpawnCooldown(spawnPoint);
                
                var bonusHandler = GetComponent<BonusHandler>();
                if (bonusHandler != null)
                {
                    bonusHandler.ClearBonus();
                }
            }
            else
            {
                Debug.LogWarning($"Не найдено свободной точки спавна для {gameObject.name}! Респавн на месте.");
            }
        }
    }
}