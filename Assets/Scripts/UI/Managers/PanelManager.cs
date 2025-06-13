using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[System.Serializable]
public class PanelGridConfig<ItemData>
{
    public GameObject buttonPrefab;
    public GameObject panelPrefab;
    public Transform parentPanel;
    public ItemData[] items;
    public int cellsPerGrid = 9;
    public float animationDuration = 0.3f;
    public float animationDelay = 0.05f;
    public System.Action<GameObject, ItemData> configureCell;
}

public class PanelManager<ItemData>
{
    private readonly PanelGridConfig<ItemData> config;
    private GameObject panel;
    private GameObject[] cellButtons;
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
        UpdateCells(0);
    }

    private void CreatePanel()
    {
        panel = Object.Instantiate(config.panelPrefab, config.parentPanel);
        panel.transform.localPosition = Vector3.zero;
    }

    private void CreateCells()
    {
        cellButtons = new GameObject[config.cellsPerGrid];
        cellTweeners = new DG.Tweening.Core.TweenerCore<Vector3, Vector3, DG.Tweening.Plugins.Options.VectorOptions>[config.cellsPerGrid];
        for (int i = 0; i < config.cellsPerGrid; i++)
        {
            cellButtons[i] = Object.Instantiate(config.buttonPrefab, panel.transform);
            cellButtons[i].SetActive(false);
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
                cell.transform.localScale = Vector3.zero;
                if (cellTweeners[i] != null && cellTweeners[i].active)
                {
                    cellTweeners[i].Kill();
                }
                config.configureCell?.Invoke(cell, config.items[itemIndex]);
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
    
    // Новый метод для принудительного обновления текущей страницы
    public void RefreshCurrentPage()
    {
        UpdateCells(currentPage);
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