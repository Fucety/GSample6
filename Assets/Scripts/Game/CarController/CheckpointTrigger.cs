using UnityEngine;
using UnityEngine.AI;

namespace UshiSoft.UACPF
{
    // Управляет триггерами контрольных точек и респавном машины
    public class CheckpointTrigger : MonoBehaviour
    {
        private CarControllerBase carController;

        private void Awake()
        {
            carController = GetComponent<CarControllerBase>();
            if (carController == null) Debug.LogError("CheckpointTrigger требует CarControllerBase!", this);
        }

        private void OnTriggerEnter(Collider other)
        {
            // Логика для контрольных точек, если потребуется (например, обновление последней точки)
            // Подбор бонусов обрабатывается BonusBox
        }

        // Перемещает машину на точку спавна
        public void Respawn()
        {
            Transform spawnPoint = SpawnPointManager.Instance.GetAvailableSpawnPoint();
            if (spawnPoint != null)
            {
                transform.position = spawnPoint.position;
                transform.rotation = spawnPoint.rotation;
                var navMeshAgent = GetComponent<NavMeshAgent>();
                if (navMeshAgent != null)
                {
                    navMeshAgent.Warp(spawnPoint.position); // Синхронизация с NavMesh
                }
                SpawnPointManager.Instance.SetSpawnCooldown(spawnPoint); // Установка кулдауна
                // Очистка бонуса при респавне
                var bonusHandler = GetComponent<BonusHandler>();
                if (bonusHandler != null)
                {
                    bonusHandler.ClearBonus();
                }
            }
        }
    }
}