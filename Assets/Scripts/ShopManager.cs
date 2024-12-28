using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;
using DG.Tweening;

public class ShopManager : MonoBehaviour
{
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private GameObject shopPrefab;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private SpriteAtlas spriteAtlas;
    [SerializeField] private string[] skinSpriteNames;
    [SerializeField] private float moveDuration = 0.5f;

    private float moveDistance = -1000f;
    private float rightBoundary = -1000f; // Начальное значение обновлено
    private float leftBoundary = 0f;
    private GameObject[] gridPanel;
    private int currentGPL = 0;
    private int currentSSN = 0;
    private int cells = 0;
    private readonly int grids = 9;
    private float currentPositionX = 0f;

    private void Start()
    {
        // Подсчитываем общее количество ячеек
        cells = Mathf.CeilToInt((float)skinSpriteNames.Length / grids) * grids;
        rightBoundary = ((cells / grids) - 1) * -1000f;
        currentPositionX = shopPanel.transform.localPosition.x;

        CreateMenu(cells / grids);
        CreateCells();
    }

    private void CreateMenu(int menuCount)
    {
        gridPanel = new GameObject[menuCount];

        for (int i = 0; i < menuCount; i++)
        {
            // Проверяем, существует ли уже shopPrefab в иерархии
            if (gridPanel[i] == null)
            {
                GameObject prefabInstance = Instantiate(shopPrefab, shopPanel.transform);
                int offsetX = i * 1000;
                prefabInstance.transform.localPosition = new Vector3(offsetX, prefabInstance.transform.localPosition.y, prefabInstance.transform.localPosition.z);
                gridPanel[i] = prefabInstance;
            }
        }
    }

    private void CreateCells()
    {
        foreach (string spriteName in skinSpriteNames)
        {
            // Пропускаем пустые имена скинов
            if (string.IsNullOrEmpty(spriteName)) continue;

            if (currentSSN == grids)
            {
                currentGPL++;
                currentSSN = 0;
            }

            Sprite skinSprite = spriteAtlas.GetSprite(spriteName);
            if (skinSprite != null)
            {
                GameObject newButton = Instantiate(buttonPrefab, gridPanel[currentGPL].transform);
                Image childImage = newButton.transform.Find("Icon").GetComponent<Image>();
                if (childImage != null)
                {
                    childImage.sprite = skinSprite;
                    currentSSN++;
                }
                newButton.GetComponent<Button>().onClick.AddListener(() => SelectSkin(spriteName));
            }
            else
            {
                Debug.LogWarning($"Спрайт с именем {spriteName} не найден в атласе.");
            }
        }
    }

    public void MoveRight()
    {
        float targetPositionX = currentPositionX + moveDistance;
        if (targetPositionX < rightBoundary)
        {
            targetPositionX = rightBoundary;
        }
        shopPanel.transform.DOLocalMoveX(targetPositionX, moveDuration).OnComplete(() =>
        {
            currentPositionX = targetPositionX;
        });
    }

    public void MoveLeft()
    {
        float targetPositionX = currentPositionX - moveDistance;
        if (targetPositionX > leftBoundary)
        {
            targetPositionX = leftBoundary;
        }
        shopPanel.transform.DOLocalMoveX(targetPositionX, moveDuration).OnComplete(() =>
        {
            currentPositionX = targetPositionX;
        });
    }

    private void SelectSkin(string selectedSkinName)
    {
        Debug.Log("Выбран скин: " + selectedSkinName);
    }
}
