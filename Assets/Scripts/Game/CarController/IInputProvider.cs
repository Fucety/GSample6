using UnityEngine;

namespace UshiSoft.UACPF
{
    // Интерфейс для обработки ввода
    public interface IInputProvider
    {
        float GetSteerInput(); // Получение ввода для рулевого управления (-1..1)
        float GetThrottleInput(); // Получение ввода для газа (0..1)
        float GetBrakeInput(); // Получение ввода для тормоза (0..1)
        bool IsBonusActivated(); // Проверка активации бонуса
    }
}