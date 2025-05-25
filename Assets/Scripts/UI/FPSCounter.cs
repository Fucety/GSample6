using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;
using System;

public class AdvancedFPSCounter : MonoBehaviour
{
    // UI компонент для отображения FPS
    [SerializeField] private TextMeshProUGUI fpsText;
    // Новый UI компонент для отображения минимального FPS
    [SerializeField] private TextMeshProUGUI minFPSText;
    // Флаг для активации/деактивации логирования
    [SerializeField] private bool enableLogging = true;
    
    // Интервал обновления UI (в секундах)
    [SerializeField] private float uiUpdateInterval = 0.5f;
    // Порог FPS для регистрации просадок
    [SerializeField] private float lowFpsThreshold = 30f;
    // Целевой FPS для приложения
    [SerializeField] private int targetFPS = 120;

    private float deltaTime; // Для сглаживания значений FPS
    private float uiUpdateTimer; // Таймер для обновления UI
    private FPSLogger fpsLogger; // Компонент для логирования
    // Новое поле для хранения минимального FPS за сессию
    private float sessionMinFPS = float.MaxValue;

    void Awake()
    {
        // Инициализация настроек качества
        QualitySettings.vSyncCount = 0;
        if (targetFPS > 0)
        {
            Application.targetFrameRate = targetFPS;
        }

        // Инициализация логгера только если логирование включено
        if (enableLogging){
            fpsLogger = new FPSLogger(lowFpsThreshold);
            Debug.Log($"FPS logging enabled. Threshold: {lowFpsThreshold} FPS");
        }
        else
            fpsLogger = null;
    }

    void Update()
    {
        // Вычисление FPS с использованием сглаживания
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        float currentFPS = 1.0f / deltaTime;
        // Обновляем минимальный FPS за сессию
        sessionMinFPS = Mathf.Min(sessionMinFPS, currentFPS);

        // Логирование FPS если включено и логгер существует
        if (fpsLogger != null)
        {
            fpsLogger.LogFPS(currentFPS, Time.time);
        }

        uiUpdateTimer += Time.deltaTime;
        if (uiUpdateTimer >= uiUpdateInterval)
        {
            UpdateFPSDisplay(currentFPS);
            uiUpdateTimer = 0f;
        }
    }

    // Обновление текста FPS в UI
    private void UpdateFPSDisplay(float fps)
    {
        if (fpsText != null)
        {
            fpsText.text = $"{Mathf.CeilToInt(fps)} FPS";
        }
        if (minFPSText != null)
        {
            minFPSText.text = $"Min: {Mathf.CeilToInt(sessionMinFPS)} FPS";
        }
    }

    // Сохранение логов при выходе, только если логирование включено
    void OnDestroy()
    {
        // Сохраняем лог только если логгер существует
        if (fpsLogger != null)
        {
            fpsLogger.SaveLog();
        }
    }
}

// Класс для логирования FPS с выделенной ответственностью
public class FPSLogger
{
    private readonly float lowFpsThreshold; // Порог для определения просадок FPS
    private readonly List<FPSData> fpsDataList; // Список для хранения данных FPS
    private readonly string logFilePath; // Путь к файлу лога

    public FPSLogger(float threshold)
    {
        lowFpsThreshold = threshold;
        fpsDataList = new List<FPSData>();
        // Новая логика: логи сохраняются в папке Logs внутри Assets
        string logsFolder = Path.Combine(Application.dataPath, "Logs");
        Directory.CreateDirectory(logsFolder);
        logFilePath = Path.Combine(logsFolder, $"FPS_Log_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
    }

    // Логирование текущего значения FPS
    public void LogFPS(float fps, float time)
    {
        var fpsData = new FPSData
        {
            Timestamp = time,
            FPS = fps,
            IsLowFPS = fps < lowFpsThreshold
        };

        fpsDataList.Add(fpsData);

        // Логирование в консоль для отладки
        if (fpsData.IsLowFPS)
        {
            Debug.LogWarning($"Low FPS detected: {fps:F1} at {time:F2} seconds");
        }
    }

    // Сохранение логов в файл
    public void SaveLog()
    {
        try
        {
            using (StreamWriter writer = new StreamWriter(logFilePath))
            {
                writer.WriteLine("Timestamp,FPS,IsLowFPS");
                foreach (var data in fpsDataList)
                {
                    writer.WriteLine($"{data.Timestamp:F2},{data.FPS:F1},{data.IsLowFPS}");
                }
            }
            Debug.Log($"FPS log saved to: {logFilePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to save FPS log: {ex.Message}");
        }
    }
}

// Структура для хранения данных FPS
public struct FPSData
{
    public float Timestamp; // Время в секундах с начала игры
    public float FPS; // Значение FPS
    public bool IsLowFPS; // Флаг низкого FPS
}