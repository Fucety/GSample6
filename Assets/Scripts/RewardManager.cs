using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;
using DG.Tweening;
using TMPro;

[System.Serializable]
public class RewardData
{
    public string spriteName; // Имя награды
    public int ads;         // Количество рекламы
    public int coins;       // Количество монет
}

public class RewardManager : MonoBehaviour
{
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private GameObject shopPrefab;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private SpriteAtlas spriteAtlas;
    [SerializeField] private SkinData[] skins; // Массив объектов SkinData
    [SerializeField] private float moveDuration = 0.5f;

    private float moveDistance = -1000f;
    private float rightBoundary = -1000f;
    private float leftBoundary = 0f;
    private GameObject[] gridPanel;
    private int currentGPL = 0;
    private int currentSSN = 0;
    private int cells = 0;
    private readonly int grids = 9; // Количество ячеек на одной панели
    private float currentPositionX = 0f;

    private void Start()
    {
        cells = Mathf.CeilToInt((float)skins.Length / grids) * grids;
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
        foreach (SkinData skin in skins)
        {
            if (string.IsNullOrEmpty(skin.spriteName)) continue;

            // Переход на следующую панель, если текущая заполнена
            if (currentSSN == grids)
            {
                currentGPL++;
                currentSSN = 0;
            }

            Sprite skinSprite = spriteAtlas.GetSprite(skin.spriteName);
            if (skinSprite != null)
            {
                // Создаем новую кнопку
                GameObject newButton = Instantiate(buttonPrefab, gridPanel[currentGPL].transform);

                // Устанавливаем спрайт для кнопки
                Image childImage = newButton.transform.Find("Icon").GetComponent<Image>();
                if (childImage != null)
                {
                    childImage.sprite = skinSprite;
                }

                // Устанавливаем цену
                TextMeshProUGUI priceText = newButton.transform.Find("Price").GetComponent<TextMeshProUGUI>();
                if (priceText != null)
                {
                    priceText.text = $"{skin.price} $";
                }
                else
                {
                    Debug.LogWarning("Компонент TextMeshProUGUI для цены не найден на префабе.");
                }

                // Добавляем обработчик клика с передачей имени и цены
                int price = skin.price; // Локальная переменная, чтобы избежать замыкания
                newButton.GetComponent<Button>().onClick.AddListener(() => SelectSkin(skin.spriteName, price));
                currentSSN++;
            }
            else
            {
                Debug.LogWarning($"Спрайт с именем {skin.spriteName} не найден в атласе.");
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

    private void SelectSkin(string selectedSkinName, int price)
    {
        Debug.Log($"Выбран скин: {selectedSkinName}, Цена: {price}$");
    }
}
