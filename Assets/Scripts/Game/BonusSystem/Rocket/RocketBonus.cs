using UnityEngine;

namespace UshiSoft.UACPF
{
    [CreateAssetMenu(fileName = "RocketBonus", menuName = "KartingRush/Bonuses/Rocket")]
    public class RocketBonus : BonusBase
    {
        [SerializeField] private GameObject rocketPrefab; // Префаб ракеты

        public override void Activate(CarControllerBase car)
        {
            // Определяем позицию спавна ракеты перед машиной
            // Увеличиваем расстояние (например, до 3-4 метров) и добавляем небольшое поднятие по Y
            float spawnDistance = 2f; // Экспериментируйте с этим значением
            float spawnHeightOffset = 0.5f; // Небольшое поднятие над землей/машиной

            Vector3 spawnPosition = car.transform.position + car.transform.forward * spawnDistance + Vector3.up * spawnHeightOffset;
            GameObject rocketObj = Instantiate(rocketPrefab, spawnPosition, car.transform.rotation);
            Rocket rocket = rocketObj.GetComponent<Rocket>();
            if (rocket != null)
            {
                rocket.SetOwner(car); // Устанавливаем владельца
            }
        }
    }
}