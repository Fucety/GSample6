using UnityEngine;

namespace UshiSoft.UACPF
{
    // Управляет бонусной коробкой на трассе
    public class BonusBox : MonoBehaviour
    {
        [SerializeField] private BonusDataSO bonusDatabase; // Ссылка на базу бонусов
        [SerializeField] private GameObject visualObject;     // Визуальный объект коробки

        private Collider boxCollider; // Триггер для подбора
        private BonusSpawnManager spawnController; // Ссылка на контроллер спавна
        private bool isActive = true; // Состояние коробки

        // Инициализация
        private void Awake()
        {
            boxCollider = GetComponent<Collider>();
            spawnController = GetComponentInParent<BonusSpawnManager>();
            if (!boxCollider.isTrigger) Debug.LogError("BonusBox должен иметь триггер-коллайдер!");
            if (spawnController == null) Debug.LogError("BonusBox требует BonusSpawnController в родительском объекте!");
        }

        // Когда машина касается коробки
        private void OnTriggerEnter(Collider other)
        {
            if (!isActive) return;

            CarControllerBase car = other.GetComponentInParent<CarControllerBase>();
            if (car != null)
            {
                BonusHandler handler = car.GetComponent<BonusHandler>();
                if (handler != null && handler.CanPickupBonus())
                {
                    BonusBase bonus = bonusDatabase.GetRandomBonus();
                    handler.SetBonus(bonus);
                    isActive = false; // Отмечаем как неактивную
                    visualObject.SetActive(false); // Скрываем визуал
                    boxCollider.enabled = false; // Отключаем коллайдер
                    spawnController.RequestRespawn(this); // Запрашиваем респавн
                }
            }
        }

        // Восстанавливает коробку (вызывается BonusSpawnController)
        public void Respawn()
        {
            isActive = true;
            visualObject.SetActive(true);
            boxCollider.enabled = true;
        }
    }
}