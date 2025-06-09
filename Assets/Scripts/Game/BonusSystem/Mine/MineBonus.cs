// Файл: MineBonus.cs
using UnityEngine;

namespace UshiSoft.UACPF
{
    [CreateAssetMenu(fileName = "MineBonus", menuName = "KartingRush/Bonuses/Mine")]
    public class MineBonus : BonusBase
    {
        [SerializeField] private GameObject minePrefab; // Префаб объекта мины
        [SerializeField] private float placementDistance = 3f; // На каком расстоянии позади машины ставить мину

        public override void Activate(CarControllerBase car)
        {
            // Рассчитываем позицию для установки мины позади машины
            Vector3 spawnPosition = car.transform.position - car.transform.forward * placementDistance;
            
            // Создаем экземпляр мины из префаба
            GameObject mineObj = Instantiate(minePrefab, spawnPosition, Quaternion.identity);
            
            // Получаем компонент мины и устанавливаем владельца, чтобы избежать самоуничтожения
            Mine mineComponent = mineObj.GetComponent<Mine>();
            if (mineComponent != null)
            {
                mineComponent.SetOwner(car);
            }
        }
    }
}