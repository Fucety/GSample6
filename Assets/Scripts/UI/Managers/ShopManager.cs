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
            configureCell = ConfigureSkinCell
        };
        panelManager = new PanelManager<SkinData>(config);
    }

    private void ConfigureSkinCell(GameObject button, SkinData skin)
    {
        if (string.IsNullOrEmpty(skin.spriteName)) return;

        Sprite skinSprite = spriteAtlas.GetSprite(skin.spriteName);
        if (skinSprite != null)
        {
            if (button.transform.Find("IconMask/Icon").TryGetComponent<Image>(out var childImage))
            {
                childImage.sprite = skinSprite;
            }
        }

        if (button.transform.Find("PriceBack/Price").TryGetComponent<TextMeshProUGUI>(out var priceText))
        {
            priceText.text = $"{skin.price}";
        }

        var priceBack = button.transform.Find("PriceBack").gameObject;
        bool isUnlocked = PlayerDataManager.Instance.IsSkinUnlocked(skin.spriteName);
        bool isEquipped = PlayerDataManager.Instance.GetEquippedSkin() == skin.spriteName;

        var buttonComponent = button.GetComponent<Button>();
        buttonComponent.onClick.RemoveAllListeners();

        if (isEquipped)
        {
            priceBack.SetActive(false);
            buttonComponent.interactable = false;
        }
        else if (isUnlocked)
        {
            priceBack.SetActive(false);
            buttonComponent.interactable = true;
            buttonComponent.onClick.AddListener(() => EquipSkin(skin.spriteName));
        }
        else
        {
            priceBack.SetActive(true);
            buttonComponent.interactable = true;
            buttonComponent.onClick.AddListener(() => TryBuySkin(skin));
        }
    }

    private void TryBuySkin(SkinData skin)
    {
        if (PlayerDataManager.Instance.TrySpendCoins(skin.price))
        {
            PlayerDataManager.Instance.UnlockSkin(skin.spriteName);
            EquipSkin(skin.spriteName);
        }
        else
        {
            Debug.LogWarning("Недостаточно монет для покупки!");
            // Здесь можно добавить визуальный фидбек
        }
    }

    private void EquipSkin(string spriteName)
    {
        PlayerDataManager.Instance.EquipSkin(spriteName);
        panelManager.RefreshCurrentPage();
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