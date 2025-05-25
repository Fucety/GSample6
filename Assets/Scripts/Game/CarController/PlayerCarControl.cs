using UnityEngine;
using UshiSoft.Common;

namespace UshiSoft.UACPF
{
    [DisallowMultipleComponent]
    public class PlayerCarControl : DriverBase
    {
        [Header("Steering Settings")]
        [SerializeField, Min(0.001f)] private float steerTime = 0.1f; // Время для полного поворота руля
        [SerializeField, Min(0.001f)] private float steerReleaseTime = 0.1f; // Время возврата руля в нейтраль
        [SerializeField] private bool steerLimitByFriction = false; // Ограничивать угол поворота трением
        [SerializeField, Min(0f)] private float steerMu = 2f; // Коэффициент трения для ограничения

        [Header("Throttle and Brake Settings")]
        [SerializeField, Min(0.001f)] private float throttleTime = 0.1f; // Время для полного нажатия газа
        [SerializeField, Min(0.001f)] private float throttleReleaseTime = 0.1f; // Время отпускания газа
        [SerializeField, Min(0.001f)] private float brakeTime = 0.1f; // Время для полного нажатия тормоза
        [SerializeField, Min(0.001f)] private float brakeReleaseTime = 0.1f; // Время отпускания тормоза

        [Header("Reverse Settings")]
        [SerializeField] private bool autoShiftToReverse = true; // Автоматическое переключение на задний ход
        [SerializeField, Min(0f)] private float switchToReverseSpeedKPH = 1f; // Скорость для переключения на задний ход

        [Header("Input Settings")]
        [SerializeField] private TouchInputController touchInputController; // Контроллер сенсорного ввода
        [SerializeField] private bool debugMobileMode = false; // Режим отладки: имитировать мобильное устройство в редакторе

        private IInputProvider inputProvider; // Провайдер ввода (клавиатура или сенсор)
        private IBonus activeBonus; // Текущий активный бонус

        protected override void Awake()
        {
            base.Awake();
            InitializeInputProvider();
        }

        // Инициализация провайдера ввода в зависимости от платформы или режима отладки
        private void InitializeInputProvider()
        {
            // Используем сенсорный ввод, если это мобильное устройство или включён режим отладки
            if ((Application.isMobilePlatform || (debugMobileMode && Application.isEditor)) && touchInputController != null)
            {
                inputProvider = touchInputController; // Сенсорный ввод для смартфонов или отладки
            }
            else
            {
                inputProvider = new KeyboardInputProvider(); // Клавиатурный ввод для ПК
            }
        }

        protected override void Drive()
        {
            UpdateSteerInput();
            UpdateThrottleAndBrakeInput();
            CheckBonusActivation();
        }

        protected override void Stop()
        {
            // Полная остановка: максимальный тормоз, плавное отпускание газа
            _carController.BrakeInput = 1f;
            var throttleInput = inputProvider.GetThrottleInput();
            var time = throttleInput != 0f ? throttleTime : throttleReleaseTime;
            _carController.ThrottleInput = Mathf.MoveTowards(_carController.ThrottleInput, throttleInput, Time.deltaTime / time);
        }

        // Обновление ввода для рулевого управления
        private void UpdateSteerInput()
        {
            var maxSteerInput = 1f;
            if (steerLimitByFriction)
            {
                // Ограничение угла поворота на основе трения и скорости
                var speed = _carController.Speed;
                var minTurnR = (speed * speed) / (steerMu * Physics.gravity.magnitude);
                if (minTurnR > 0f)
                {
                    var optimalSteerAngle = Mathf.Asin(_carController.Wheelbase / minTurnR) * Mathf.Rad2Deg;
                    maxSteerInput = Mathf.Min(optimalSteerAngle / _carController.MaxSteerAngle, 1f);
                }
            }

            var steerInput = inputProvider.GetSteerInput();
            steerInput = Mathf.Clamp(steerInput, -maxSteerInput, maxSteerInput);

            var time = steerInput != 0f ? steerTime : steerReleaseTime;

            // Плавный поворот руля, сбрасываем при смене направления
            if (steerInput != 0f && Mathf.Sign(steerInput) != Mathf.Sign(_carController.SteerInput))
                _carController.SteerInput = 0f;

            _carController.SteerInput = Mathf.MoveTowards(_carController.SteerInput, steerInput, Time.deltaTime / time);
        }

        // Обновление ввода для газа и тормоза
        private void UpdateThrottleAndBrakeInput()
        {
            var throttleInput = inputProvider.GetThrottleInput();
            var brakeInput = inputProvider.GetBrakeInput();

            // Автоматическое переключение на задний ход
            if (autoShiftToReverse && _carController.IsGrounded())
            {
                var speedKPH = _carController.ForwardSpeed * UshiMath.MPSToKPH;
                if (_carController.Reverse)
                {
                    if (throttleInput > 0f && speedKPH > -switchToReverseSpeedKPH)
                        _carController.Reverse = false;
                }
                else
                {
                    if (brakeInput > 0f && speedKPH < switchToReverseSpeedKPH)
                        _carController.Reverse = true;
                }

                // Инверсия газа и тормоза при движении назад
                if (_carController.Reverse)
                    (throttleInput, brakeInput) = (brakeInput, throttleInput);
            }

            // Плавное изменение газа
            var throttleTime = throttleInput != 0f ? this.throttleTime : throttleReleaseTime;
            _carController.ThrottleInput = Mathf.MoveTowards(_carController.ThrottleInput, throttleInput, Time.deltaTime / throttleTime);

            // Плавное изменение тормоза
            var brakeTime = brakeInput != 0f ? this.brakeTime : brakeReleaseTime;
            _carController.BrakeInput = Mathf.MoveTowards(_carController.BrakeInput, brakeInput, Time.deltaTime / brakeTime);
        }

        // Проверка активации бонуса
        private void CheckBonusActivation()
        {
            if (inputProvider.IsBonusActivated() && activeBonus != null)
            {
                activeBonus.Activate(_carController);
                activeBonus = null; // Сбрасываем бонус после использования
            }
        }

        // Установка нового бонуса
        public void SetBonus(IBonus bonus)
        {
            activeBonus = bonus;
        }
    }
}