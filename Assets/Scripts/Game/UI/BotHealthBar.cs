using UnityEngine;
using UnityEngine.UI;
using UshiSoft.UACPF;

public class BotHealthBar : MonoBehaviour
{
    [SerializeField] private Canvas healthCanvas; // Канвас в режиме World Space, должен быть дочерним элементом
    [SerializeField] private Image fillImage;       // Изображение для заполнения полоски
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, 0f); // Смещение над ботом

    private CarHealth carHealth;
    private Camera mainCamera;

    private void Awake()
    {
        carHealth = GetComponent<CarHealth>();
        if (carHealth == null)
            Debug.LogError("BotHealthBar требует наличия компонента CarHealth на объекте!");
        
        if (healthCanvas == null)
            Debug.LogError("BotHealthBar: необходимо назначить healthCanvas через инспектор!");
        if (fillImage == null)
            Debug.LogError("BotHealthBar: необходимо назначить fillImage через инспектор!");
        
        mainCamera = Camera.main;
    }

    private void Update()
    {
        // Обновление позиции канвы над ботом
        healthCanvas.transform.position = transform.position + offset;

        // Поворот канвы так, чтобы она смотрела на камеру
        if (mainCamera != null)
        {
            Vector3 direction = healthCanvas.transform.position - mainCamera.transform.position;
            healthCanvas.transform.rotation = Quaternion.LookRotation(direction);
        }
        
        // Обновление значения заполнения полоски на основе текущего здоровья
        if (carHealth != null)
        {
            float fill = Mathf.Clamp01(carHealth.CurrentHealth / carHealth.MaxHealth);
            fillImage.fillAmount = fill;
        }
    }
}
