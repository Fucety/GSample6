using UnityEngine;
using DG.Tweening; // добавлено для дутвин

namespace UshiSoft.UACPF
{
    // Управляет бонусной коробкой на трассе
    public class BonusBox : MonoBehaviour
    {
        [SerializeField] private BonusDataSO bonusDatabase; // Ссылка на базу бонусов
        [SerializeField] private GameObject visualObject;     // Визуальный объект коробки

        // Новые параметры для настройки через инспектор
        [SerializeField] private float rotationDuration = 3f;
        [SerializeField] private float verticalAmplitude = 0.25f;
        [SerializeField] private float verticalDuration = 2f;

        private Collider boxCollider; // Триггер для подбора
        private BonusSpawnManager spawnController; // Ссылка на контроллер спавна
        private bool isActive = true; // Состояние коробки
        
        // новое поле для хранения исходной высоты анимации
        private float initialY;

        // Инициализация
        private void Awake()
        {
            boxCollider = GetComponent<Collider>();
            spawnController = GetComponentInParent<BonusSpawnManager>();
            if (!boxCollider.isTrigger) Debug.LogError("BonusBox должен иметь триггер-коллайдер!");
            if (spawnController == null) Debug.LogError("BonusBox требует BonusSpawnController в родительском объекте!");
        }
        
        // Добавляем метод Start для инициализации анимации
        private void Start()
        {
            // Сохраняем начальную позицию по оси Y
            initialY = transform.position.y;
            PlayIdleAnimation();
        }

        // Обновленный метод для запуска анимации с использованием настроек из инспектора
        private void PlayIdleAnimation()
        {
            // Бесконечное вращение по оси Y с настраиваемой длительностью
            transform.DORotate(new Vector3(0, 360, 0), rotationDuration, RotateMode.FastBeyond360)
                     .SetEase(Ease.Linear)
                     .SetLoops(-1, LoopType.Restart);
            // Вертикальное перемещение с настраиваемой амплитудой и длительностью
            transform.DOMoveY(initialY + verticalAmplitude, verticalDuration)
                     .SetEase(Ease.InOutSine)
                     .SetLoops(-1, LoopType.Yoyo);
        }
        
        // Когда машина касается коробки
        private void OnTriggerEnter(Collider other)
        {
            if (!isActive) return; // добавлено для использования поля isActive
            // заменено: получаем BonusHandler напрямую
            BonusHandler handler = other.GetComponentInParent<BonusHandler>();
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

        // Восстанавливает коробку (вызывается BonusSpawnController)
        public void Respawn()
        {
            isActive = true;
            visualObject.SetActive(true);
            boxCollider.enabled = true;
        }
    }
}