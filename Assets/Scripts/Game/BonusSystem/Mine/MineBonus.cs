using UnityEngine;

namespace UshiSoft.UACPF
{
    [CreateAssetMenu(fileName = "MineBonus", menuName = "KartingRush/Bonuses/Mine")]
    public class MineBonus : BonusBase
    {
        // [SerializeField] private GameObject minePrefab; // Больше не нужен, будем использовать тэг
        [SerializeField] private string minePoolTag = "Mine"; // Тэг для пула мины
        [SerializeField] private float placementDistance = 3f; // На каком расстоянии позади машины ставить мину

        public override void Activate(CarControllerBase car)
        {
            if (ObjectPoolManager.Instance == null)
            {
                Debug.LogError("ObjectPoolManager.Instance is null! Cannot activate mine bonus.");
                return;
            }

            // Рассчитываем позицию для установки мины позади машины
            Vector3 spawnPosition = car.transform.position - car.transform.forward * placementDistance;
            
            // Получаем мину из пула
            GameObject mineObj = ObjectPoolManager.Instance.GetPooledObject(minePoolTag);
            if (mineObj != null)
            {
                mineObj.transform.position = spawnPosition;
                mineObj.transform.rotation = Quaternion.identity; // Мины обычно не имеют специфичной ориентации
                
                Mine mineComponent = mineObj.GetComponent<Mine>();
                if (mineComponent != null)
                {
                    mineComponent.SetOwner(car); // Устанавливаем владельца и сбрасываем состояние
                }
            }
        }
    }
}