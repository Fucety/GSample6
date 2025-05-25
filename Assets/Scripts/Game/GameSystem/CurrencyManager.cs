using UnityEngine;
using System.Threading.Tasks;

namespace UshiSoft.UACPF
{
    public class CurrencyManager : MonoBehaviour
    {
        public static CurrencyManager Instance { get; private set; }

        private int coins; // Обычные монеты
        private int premium; // Премиум-валюта

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private async void Start()
        {
            // Загрузка сохранённых данных
           // coins = await YandexGamesIntegration.Instance.LoadPlayerData("coins", 0);
           // premium = await YandexGamesIntegration.Instance.LoadPlayerData("premium", 0);
            UIManager.Instance.UpdateCoins(coins);
            UIManager.Instance.UpdatePremium(premium);
        }

        public void AddCoins(int amount)
        {
            coins += amount;
            //YandexGamesIntegration.Instance.SavePlayerData("coins", coins);
            UIManager.Instance.UpdateCoins(coins);
        }

        public void AddPremium(int amount)
        {
            premium += amount;
            //YandexGamesIntegration.Instance.SavePlayerData("premium", premium);
            UIManager.Instance.UpdatePremium(premium);
        }

        public bool SpendCoins(int amount)
        {
            if (coins >= amount)
            {
                coins -= amount;
                //YandexGamesIntegration.Instance.SavePlayerData("coins", coins);
                UIManager.Instance.UpdateCoins(coins);
                return true;
            }
            return false;
        }

        public bool SpendPremium(int amount)
        {
            if (premium >= amount)
            {
                premium -= amount;
               // YandexGamesIntegration.Instance.SavePlayerData("premium", premium);
                UIManager.Instance.UpdatePremium(premium);
                return true;
            }
            return false;
        }

        public int Coins => coins;
        public int Premium => premium;
    }
}