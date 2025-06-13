using System.Collections;
using UnityEngine;

namespace UshiSoft.UACPF
{
    [CreateAssetMenu(fileName = "TripleRocketBonus", menuName = "KartingRush/Bonuses/TripleRocket")]
    public class TripleRocketBonus : BonusBase
    {
        // [SerializeField] private GameObject rocketPrefab; // Больше не нужен
        [SerializeField] private string rocketPoolTag = "Rocket"; // Тэг для пула ракеты
        [SerializeField] private float spawnDistance = 2f; 
        [SerializeField] private float spawnHeightOffset = 0.5f; 
        [SerializeField] private float delayBetweenRockets = 0.2f; 

        public override void Activate(CarControllerBase car)
        {
            if (ObjectPoolManager.Instance == null)
            {
                Debug.LogError("ObjectPoolManager.Instance is null! Cannot activate triple rocket bonus.");
                return;
            }
            car.StartCoroutine(LaunchRockets(car));
        }

        private IEnumerator LaunchRockets(CarControllerBase car)
        {
            for (int i = 0; i < 3; i++)
            {
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
                yield return new WaitForSeconds(delayBetweenRockets);
            }
        }
    }
}