using UnityEngine;

namespace UshiSoft.UACPF
{
    // Управляет бонусами на машине
    public class BonusHandler : MonoBehaviour
    {
        private IBonus currentBonus;         // Текущий бонус
        private CarControllerBase carController; // Ссылка на контроллер машины

        // Инициализация
        private void Awake()
        {
            carController = GetComponent<CarControllerBase>();
            if (carController == null) Debug.LogError("BonusHandler требует CarControllerBase!");
        }

        // Может ли машина взять бонус
        public bool CanPickupBonus()
        {
            return currentBonus == null; // Бонус можно взять, если текущего нет
        }

        // Установка бонуса
        public void SetBonus(IBonus bonus)
        {
            currentBonus = bonus;
            if (carController == GameManager.Instance.PlayerCar) // Если это игрок
            {
                UIManager.Instance.ShowBonusPickup((BonusBase)bonus); // Показываем иконку
            }
        }

        // Активация бонуса
        public void ActivateBonus()
        {
            if (currentBonus != null)
            {
                currentBonus.Activate(carController); // Выполняем действие бонуса
                currentBonus = null;                  // Очищаем бонус после использования
            }
        }

        // Проверка наличия бонуса (для ботов)
        public bool HasBonus => currentBonus != null;

        // Очистка бонуса (например, при респавне)
        public void ClearBonus()
        {
            currentBonus = null;
        }
    }
}