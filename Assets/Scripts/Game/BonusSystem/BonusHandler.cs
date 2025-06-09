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
            // изменено: ищем базовый класс управления машиной
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
            // Если это игрок и UIManager доступен, показываем иконку бонуса
            
            if (GameManager.Instance.PlayerCar != null && carController == GameManager.Instance.PlayerCar)
            {

                if (UIManager.Instance != null)
                {

                    UIManager.Instance.ShowBonusPickup((BonusBase)bonus);

                }
                // Иначе можно залогировать или выполнить иное действие
                else
                {
                    Debug.LogWarning("UIManager не установлен, пропускаем показ бонуса.");
                }
            }
        }

        // Активация бонуса
        public void ActivateBonus()
        {
            if (currentBonus != null)
            {
                currentBonus.Activate(carController); // Выполняем действие бонуса
                // При использовании бонуса отключаем иконку, если это машина игрока
                if (GameManager.Instance.PlayerCar != null && carController == GameManager.Instance.PlayerCar)
                {
                    UIManager.Instance.HideBonusPickupIcon();
                }
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