using UnityEngine;

public class MenuCurrencyManager : MonoBehaviour
{
    public static MenuCurrencyManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        PlayerDataManager.OnCoinsChanged += UpdateCoinsUI;
    }

    private void OnDisable()
    {
        PlayerDataManager.OnCoinsChanged -= UpdateCoinsUI;
    }

    private void Start()
    {
        UpdateCoinsUI(PlayerDataManager.Instance.GetCoins());
    }

    private void UpdateCoinsUI(int coins)
    {
        MenuUIManager.Instance.UpdateCoins(coins);
        Debug.Log($"[MenuCurrencyManager] Баланс обновлен: {coins}");
    }

    public bool SpendCoins(int amount)
    {
        return PlayerDataManager.Instance.TrySpendCoins(amount);
    }
}