using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;
using TMPro;
using YG;

[System.Serializable]
public class RewardData
{
    public string spriteName;
    public int ads;
    public int coins;
}

[CreateAssetMenu(fileName = "RewardPanelData", menuName = "PanelData/RewardPanelData")]
public class RewardPanelDataSO : ScriptableObject
{
    public RewardData[] rewards;
}

public class RewardManager : MonoBehaviour
{
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private GameObject shopPrefab;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private SpriteAtlas spriteAtlas;
    [SerializeField] private RewardPanelDataSO panelData;
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private float animationDelay = 0.05f;

    private PanelManager<RewardData> panelManager;

    private void Start()
    {
        var config = new PanelGridConfig<RewardData>
        {
            buttonPrefab = buttonPrefab,
            panelPrefab = shopPrefab,
            parentPanel = shopPanel.transform,
            items = panelData.rewards,
            cellsPerGrid = 9,
            animationDuration = animationDuration,
            animationDelay = animationDelay,
            configureCell = ConfigureRewardCell
        };
        panelManager = new PanelManager<RewardData>(config);
    }

    private void ConfigureRewardCell(GameObject button, RewardData reward)
    {
        if (string.IsNullOrEmpty(reward.spriteName)) return;

        Sprite skinSprite = spriteAtlas.GetSprite(reward.spriteName);
        if (skinSprite != null)
        {
            if (button.transform.Find("Icon").TryGetComponent<Image>(out var childImage))
            {
                childImage.sprite = skinSprite;
            }
            else
            {
                Debug.LogWarning($"Компонент Image не найден по пути Icon.");
            }
        }
        else
        {
            Debug.LogWarning($"Спрайт с именем {reward.spriteName} не найден в атласе.");
        }

        if (button.transform.Find("Price").TryGetComponent<TextMeshProUGUI>(out var priceText))
        {
            priceText.text = reward.ads > 0 ? $"{reward.ads} ads" : $"{reward.coins} coins";
        }

        var buttonComponent = button.GetComponent<Button>();
        buttonComponent.onClick.RemoveAllListeners();

        bool isUnlocked = PlayerDataManager.Instance.IsRewardUnlocked(reward.spriteName);
        if (isUnlocked)
        {
            buttonComponent.interactable = false;
            priceText.text = "Unlocked";
        }
        else if (reward.ads > 0)
        {
            buttonComponent.onClick.AddListener(() => TryClaimRewardWithAds(reward));
        }
        else
        {
            buttonComponent.onClick.AddListener(() => TryBuyRewardWithCoins(reward));
        }
    }

    private void TryClaimRewardWithAds(RewardData reward)
    {
        Debug.Log($"[RewardManager] Запрос на показ рекламы для награды {reward.spriteName}...");
        YG2.RewardedAdvShow("reward_" + reward.spriteName, () =>
        {
            PlayerDataManager.Instance.UnlockReward(reward.spriteName);
            panelManager.RefreshCurrentPage();
            Debug.Log($"[RewardManager] Награда {reward.spriteName} разблокирована после просмотра рекламы.");
        });
    }

    private void TryBuyRewardWithCoins(RewardData reward)
    {
        if (PlayerDataManager.Instance.TrySpendCoins(reward.coins))
        {
            PlayerDataManager.Instance.UnlockReward(reward.spriteName);
            panelManager.RefreshCurrentPage();
            Debug.Log($"[RewardManager] Награда {reward.spriteName} куплена за {reward.coins} монет.");
        }
        else
        {
            Debug.LogWarning($"[RewardManager] Недостаточно монет для покупки награды {reward.spriteName}.");
        }
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