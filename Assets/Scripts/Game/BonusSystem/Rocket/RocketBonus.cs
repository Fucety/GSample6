using UnityEngine;

namespace UshiSoft.UACPF
{
    [CreateAssetMenu(fileName = "RocketBonus", menuName = "KartingRush/Bonuses/Rocket")]
    public class RocketBonus : BonusBase
    {
        // [SerializeField] private GameObject rocketPrefab; // Больше не нужен, будем использовать тэг
        [SerializeField] private string rocketPoolTag = "Rocket"; // Тэг для пула ракеты

        public override void Activate(CarControllerBase car)
        {
            if (ObjectPoolManager.Instance == null)
            {
                Debug.LogError("ObjectPoolManager.Instance is null! Cannot activate rocket bonus.");
                return;
            }

            float spawnDistance = 2f; 
            float spawnHeightOffset = 0.5f; 

            Vector3 spawnPosition = car.transform.position + car.transform.forward * spawnDistance + Vector3.up * spawnHeightOffset;
            
            // Получаем ракету из пула
            GameObject rocketObj = ObjectPoolManager.Instance.GetPooledObject(rocketPoolTag);
            if (rocketObj != null)
            {
                rocketObj.transform.position = spawnPosition;
                rocketObj.transform.rotation = car.transform.rotation;
                Rocket rocket = rocketObj.GetComponent<Rocket>();
                if (rocket != null)
                {
                    rocket.SetOwner(car); // Устанавливаем владельца и сбрасываем состояние
                }
            }
        }
    }
}