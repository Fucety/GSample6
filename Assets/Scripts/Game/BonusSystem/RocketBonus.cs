using UnityEngine;

namespace UshiSoft.UACPF
{
    [CreateAssetMenu(fileName = "RocketBonus", menuName = "KartingRush/Bonuses/Rocket")]
    public class RocketBonus : BonusBase
    {
        [SerializeField] private GameObject rocketPrefab; // Префаб ракеты

        public override void Activate(CarControllerBase car)
        {
            // Создаем ракету перед машиной
            Vector3 spawnPosition = car.transform.position + car.transform.forward * 2f;
            Instantiate(rocketPrefab, spawnPosition, car.transform.rotation);
        }
    }
}