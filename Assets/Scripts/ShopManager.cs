using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;
using TMPro;

public class ShopManager : MonoBehaviour
{
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private GameObject shopPrefab;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private SpriteAtlas spriteAtlas;
    [SerializeField] private ShopPanelDataSO panelData;
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private float animationDelay = 0.05f;

    private PanelManager<SkinData> panelManager;

    private void Start()
    {
        var config = new PanelGridConfig<SkinData>
        {
            buttonPrefab = buttonPrefab,
            panelPrefab = shopPrefab,
            parentPanel = shopPanel.transform,
            items = panelData.skins,
            cellsPerGrid = 9,
            animationDuration = animationDuration,
            animationDelay = animationDelay,
            configureCell = (button, skin) =>
            {
                if (string.IsNullOrEmpty(skin.spriteName)) return;

                Sprite skinSprite = spriteAtlas.GetSprite(skin.spriteName);
                if (skinSprite != null)
                {
                    if (button.transform.Find("IconMask/Icon").TryGetComponent<Image>(out var childImage))
                    {
                        childImage.sprite = skinSprite;
                    }
                    else
                    {
                        Debug.LogWarning($"Компонент Image не найден по пути IconMask/Icon.");
                    }
                }
                else
                {
                    Debug.LogWarning($"Спрайт с именем {skin.spriteName} не найден в атласе.");
                }

                if (button.transform.Find("PriceBack/Price").TryGetComponent<TextMeshProUGUI>(out var priceText))
                {
                    priceText.text = $"{skin.price} $";
                }
                else
                {
                    Debug.LogWarning($"Компонент TextMeshProUGUI не найден по пути PriceBack/Price.");
                }

                int price = skin.price;
                button.GetComponent<Button>().onClick.RemoveAllListeners();
                button.GetComponent<Button>().onClick.AddListener(() => OnSkinSelected(skin.spriteName, price));
            }
        };

        panelManager = new PanelManager<SkinData>(config);
    }

    private void OnSkinSelected(string spriteName, int price)
    {
        Debug.Log($"Выбран скин: {spriteName}, Цена: {price}$");
    }

    public void MoveRight()
    {
        panelManager.MoveRight();
    }

    public void MoveLeft()
    {
        panelManager.MoveLeft();
    }
}