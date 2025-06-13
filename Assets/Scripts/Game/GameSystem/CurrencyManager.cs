using UnityEngine;

namespace UshiSoft.UACPF
{
    // Отвечает за применение транзакций к общему балансу игрока
    public class CurrencyManager : MonoBehaviour
    {
        public static CurrencyManager Instance { get; private set; }

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

        public void AddCoinsToTotal(int amount)
        {
            if (PlayerDataManager.Instance == null) return;
            PlayerDataManager.Instance.AddCoins(amount);
        }

        public bool SpendCoins(int amount)
        {
            if (PlayerDataManager.Instance == null) return false;
            return PlayerDataManager.Instance.TrySpendCoins(amount);
        }
    }
}