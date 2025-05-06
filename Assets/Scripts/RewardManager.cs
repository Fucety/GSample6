using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;
using TMPro;

[System.Serializable]
public class RewardData
{
    public string spriteName; // Имя награды
    public int ads;          // Количество рекламы
    public int coins;        // Количество монет
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
            configureCell = (button, reward) =>
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
                    priceText.text = $"{reward.coins} coins";
                }
                else
                {
                    Debug.LogWarning($"Компонент TextMeshProUGUI не найден по пути Price.");
                }

                button.GetComponent<Button>().onClick.RemoveAllListeners();
                button.GetComponent<Button>().onClick.AddListener(() => OnRewardSelected(reward.spriteName, reward.coins));
            }
        };

        panelManager = new PanelManager<RewardData>(config);
    }

    private void OnRewardSelected(string spriteName, int coins)
    {
        Debug.Log($"Выбрана награда: {spriteName}, Монеты: {coins}");
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