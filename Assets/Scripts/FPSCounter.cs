using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FPSCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI fpsText; // Привяжите UI TextMeshProUGUI элемент в инспекторе
    private float deltaTime;
    // Новый интервал обновления текста
    [SerializeField] private float updateInterval = 0.5f;
    private float updateTimer;
    // Новое поле: целевой FPS
    [SerializeField] private int targetFPS = 120;

    void Awake()
    {
        // Включаем вертикальную синхронизацию
        QualitySettings.vSyncCount = 0;
        // Устанавливаем целевой FPS, если значение больше нуля
        if (targetFPS > 0)
            Application.targetFrameRate = targetFPS;
    }

    void Update()
    {
        // Рассчитываем FPS
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        updateTimer += Time.deltaTime;
        if(updateTimer >= updateInterval)
        {
            float fps = 1.0f / deltaTime;
            fpsText.text = Mathf.CeilToInt(fps) + " FPS";
            updateTimer = 0f;
        }
    }
}
//test2