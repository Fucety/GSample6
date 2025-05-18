using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[System.Serializable]
public class PanelGridConfig<ItemData>
{
    public GameObject buttonPrefab;           // Префаб кнопки для ячейки
    public GameObject panelPrefab;            // Префаб панели
    public Transform parentPanel;             // Родительская панель для размещения
    public ItemData[] items;                 // Массив данных для ячеек
    public int cellsPerGrid = 9;             // Количество ячеек на панель
    public float animationDuration = 0.3f;   // Длительность анимации ячейки
    public float animationDelay = 0.05f;     // Задержка между анимациями ячеек
    public System.Action<GameObject, ItemData> configureCell; // Делегат для настройки ячейки
}

public class PanelManager<ItemData>
{
    private readonly PanelGridConfig<ItemData> config;
    private GameObject panel;
    private GameObject[] cellButtons;
    // Добавляем массив для хранения tween-ов для каждой ячейки
    private DG.Tweening.Core.TweenerCore<Vector3, Vector3, DG.Tweening.Plugins.Options.VectorOptions>[] cellTweeners;
    private int currentPage = 0;
    private int totalPages;

    public PanelManager(PanelGridConfig<ItemData> config)
    {
        this.config = config;
        ValidateConfig();
        Initialize();
    }

    private void ValidateConfig()
    {
        if (config.buttonPrefab == null) Debug.LogError("Button Prefab не задан.");
        if (config.panelPrefab == null) Debug.LogError("Panel Prefab не задан.");
        if (config.parentPanel == null) Debug.LogError("Parent Panel не задан.");
        if (config.items == null || config.items.Length == 0) Debug.LogError("Items не заданы.");
        if (config.configureCell == null) Debug.LogError("ConfigureCell делегат не задан.");
    }

    private void Initialize()
    {
        totalPages = Mathf.CeilToInt((float)config.items.Length / config.cellsPerGrid);
        CreatePanel();
        CreateCells();
        UpdateCells(0); // Отображаем первую страницу
    }

    private void CreatePanel()
    {
        panel = Object.Instantiate(config.panelPrefab, config.parentPanel);
        panel.transform.localPosition = Vector3.zero;
    }

    private void CreateCells()
    {
        cellButtons = new GameObject[config.cellsPerGrid];
        // Инициализируем массив для tween-ов
        cellTweeners = new DG.Tweening.Core.TweenerCore<Vector3, Vector3, DG.Tweening.Plugins.Options.VectorOptions>[config.cellsPerGrid];
        for (int i = 0; i < config.cellsPerGrid; i++)
        {
            cellButtons[i] = Object.Instantiate(config.buttonPrefab, panel.transform);
            cellButtons[i].SetActive(false); // Изначально ячейки скрыты
        }
    }

    private void UpdateCells(int pageIndex)
    {
        int startIndex = pageIndex * config.cellsPerGrid;
        for (int i = 0; i < config.cellsPerGrid; i++)
        {
            int itemIndex = startIndex + i;
            GameObject cell = cellButtons[i];

            if (itemIndex < config.items.Length && config.items[itemIndex] != null)
            {
                cell.SetActive(true);
                // Сбрасываем масштаб для анимации
                cell.transform.localScale = Vector3.zero;
                // Если для ячейки уже есть активный твин, завершаем его
                if (cellTweeners[i] != null && cellTweeners[i].active)
                {
                    cellTweeners[i].Kill();
                }
                config.configureCell?.Invoke(cell, config.items[itemIndex]);
                // Запоминаем новый твин для ячейки
                cellTweeners[i] = cell.transform.DOScale(Vector3.one, config.animationDuration)
                    .SetDelay(i * config.animationDelay)
                    .SetEase(Ease.OutBack);
            }
            else
            {
                cell.SetActive(false);
            }
        }
    }

    public void MoveRight()
    {
        if (currentPage < totalPages - 1)
        {
            currentPage++;
            UpdateCells(currentPage);
        }
    }

    public void MoveLeft()
    {
        if (currentPage > 0)
        {
            currentPage--;
            UpdateCells(currentPage);
        }
    }
}