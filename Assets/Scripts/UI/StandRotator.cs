using UnityEngine;
using DG.Tweening;
using UnityEngine.EventSystems;

// Скрипт для автоматического и ручного вращения объекта с использованием DOTween и плавной инерцией
public class StandRotator : MonoBehaviour
{
    [SerializeField] private float autoRotateSpeed = 30f; // Базовая скорость автоматического вращения (градусы/сек)
    [SerializeField] private float manualRotateSpeed = 100f; // Скорость ручного вращения (градусы/сек)
    [SerializeField] private float dragThreshold = 0.1f; // Порог для определения начала перетаскивания
    [SerializeField] private float inertiaMultiplier = 2f; // Множитель инерции для ускорения
    [SerializeField] private float inertiaDuration = 1.5f; // Длительность инерционного замедления (сек)

    private bool isDragging; // Флаг, указывающий, происходит ли перетаскивание
    private Vector2 lastMousePosition; // Последняя позиция мыши для расчета вращения
    private Tween autoRotateTween; // Твин для автоматического вращения
    private Tween manualRotateTween; // Твин для ручного вращения
    private Tween inertiaTween; // Твин для инерционного вращения
    private float lastRotationSpeed; // Последняя скорость ручного вращения для инерции
    private bool isInputValid; // Флаг, указывающий, валиден ли ввод

    // Инициализация при старте
    private void Start()
    {
        StartAutoRotation();
    }

    // Обновление каждый кадр
    private void Update()
    {
        HandleManualRotation();
    }

    // Запускает автоматическое вращение объекта
    private void StartAutoRotation()
    {
        // Останавливаем все твины
        autoRotateTween?.Kill();
        inertiaTween?.Kill();
        manualRotateTween?.Kill();

        // Создаем бесконечное вращение вокруг оси Y
        autoRotateTween = transform.DORotate(
            new Vector3(0, 360, 0),
            360f / autoRotateSpeed,
            RotateMode.FastBeyond360)
            .SetLoops(-1, LoopType.Incremental)
            .SetEase(Ease.Linear)
            .SetRelative(true);
    }

    // Обрабатывает ручное вращение мышью
    private void HandleManualRotation()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Проверяем, находится ли указатель мыши над UI
            isInputValid = !EventSystem.current.IsPointerOverGameObject();
            if (!isInputValid)
            {
                return; // Игнорируем нажатие, если над UI
            }

            isDragging = false;
            lastMousePosition = Input.mousePosition;
            // Приостанавливаем автоматическое вращение и инерцию
            autoRotateTween.Pause();
            inertiaTween?.Kill();
            manualRotateTween?.Kill();
        }
        else if (Input.GetMouseButton(0) && isInputValid)
        {
            Vector2 currentMousePosition = Input.mousePosition;
            Vector2 delta = currentMousePosition - lastMousePosition;

            if (delta.magnitude > dragThreshold)
            {
                isDragging = true;
                // Рассчитываем угол и скорость вращения
                float rotationAmount = -delta.x * manualRotateSpeed * Time.deltaTime;
                lastRotationSpeed = -delta.x * manualRotateSpeed; // Сохраняем скорость для инерции
                // Создаем или обновляем твин для ручного вращения
                manualRotateTween?.Kill();
                manualRotateTween = transform.DORotate(
                    new Vector3(0, rotationAmount, 0),
                    Time.deltaTime,
                    RotateMode.Fast)
                    .SetRelative(true)
                    .SetEase(Ease.Linear);
            }

            lastMousePosition = currentMousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            // Останавливаем ручное вращение
            manualRotateTween?.Kill();
            if (isDragging && isInputValid)
            {
                // Запускаем инерционное вращение с плавным переходом
                StartInertiaRotation();
            }
            else
            {
                // Если не было перетаскивания или ввод невалиден, возобновляем автоматическое вращение
                autoRotateTween.Play();
            }
            isDragging = false;
            isInputValid = false; // Сбрасываем флаг ввода
        }
    }

    // Запускает инерционное вращение с плавным замедлением до базовой скорости
    private void StartInertiaRotation()
    {
        // Останавливаем предыдущую инерцию
        inertiaTween?.Kill();

        // Рассчитываем начальную скорость инерции
        float inertiaSpeed = lastRotationSpeed * inertiaMultiplier;
        // Определяем направление вращения
        float direction = Mathf.Sign(inertiaSpeed);
        // Начальная скорость не должна быть меньше базовой
        float clampedInertiaSpeed = Mathf.Max(Mathf.Abs(inertiaSpeed), autoRotateSpeed) * direction;

        // Создаем твин для инерционного вращения
        float currentSpeed = clampedInertiaSpeed;
        inertiaTween = DOTween.To(
            () => currentSpeed,
            x => currentSpeed = x,
            autoRotateSpeed * direction, // Замедляем до базовой скорости с учетом направления
            inertiaDuration)
            .SetEase(Ease.InOutQuad) // Плавное замедление
            .OnUpdate(() =>
            {
                // Применяем текущее вращение на основе скорости
                float rotationAmount = currentSpeed * Time.deltaTime;
                transform.DORotate(
                    new Vector3(0, rotationAmount, 0),
                    Time.deltaTime,
                    RotateMode.Fast)
                    .SetRelative(true)
                    .SetEase(Ease.Linear);
            })
            .OnComplete(StartAutoRotation); // Переходим к автоматическому вращению
    }

    // Останавливает все твины при уничтожении объекта
    private void OnDestroy()
    {
        autoRotateTween?.Kill();
        manualRotateTween?.Kill();
        inertiaTween?.Kill();
    }
}