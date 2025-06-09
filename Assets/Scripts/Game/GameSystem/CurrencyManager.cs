using UnityEngine;
using System.Threading.Tasks;

namespace UshiSoft.UACPF
{
    public class CurrencyManager : MonoBehaviour
    {
        public static CurrencyManager Instance { get; private set; }

        [SerializeField] private float comboWindow = 10f; // Время для комбо (сек)
        [SerializeField] private float comboMultiplier = 1.25f; // Множитель за комбо (+25%)

        private int coins; // Текущие монеты
        private float lastEliminationTime; // Время последнего устранения
        private int comboCount; // Текущий счётчик комбо

        private void Awake()
        {
            // Singleton: обеспечиваем единственный экземпляр
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
            // Загрузка сохранённых монет через Yandex Games SDK
            coins = await LoadCoinsAsync();
            UIManager.Instance.UpdateCoins(coins);
        }

        // Добавление монет за устранение с учётом комбо
        public void AddCoinsForElimination(int baseAmount)
        {
            float currentTime = Time.time;
            bool isCombo = (currentTime - lastEliminationTime) <= comboWindow;

            // Увеличиваем счётчик комбо, если в окне комбо
            if (isCombo)
            {
                comboCount++;
            }
            else
            {
                comboCount = 1; // Сбрасываем комбо
            }

            // Рассчитываем награду с учётом множителя комбо
            int amount = Mathf.RoundToInt(baseAmount * Mathf.Pow(comboMultiplier, comboCount - 1));
            coins += amount;
            lastEliminationTime = currentTime;

            // Сохраняем монеты и обновляем UI
            SaveCoinsAsync();
            UIManager.Instance.UpdateCoins(coins);
            Debug.Log($"[CurrencyManager] Добавлено {amount} монет (комбо x{comboCount}). Всего: {coins}");
        }

        // Добавление монет за другие действия (например, задания)
        public void AddCoins(int amount)
        {
            coins += amount;
            SaveCoinsAsync();
            UIManager.Instance.UpdateCoins(coins);
            Debug.Log($"[CurrencyManager] Добавлено {amount} монет. Всего: {coins}");
        }

        // Трата монет (например, в магазине)
        public bool SpendCoins(int amount)
        {
            if (coins >= amount)
            {
                coins -= amount;
                SaveCoinsAsync();
                UIManager.Instance.UpdateCoins(coins);
                Debug.Log($"[CurrencyManager] Потрачено {amount} монет. Остаток: {coins}");
                return true;
            }
            Debug.LogWarning($"[CurrencyManager] Недостаточно монет для траты: {amount}, доступно: {coins}");
            return false;
        }

        // Асинхронная загрузка монет через Yandex Games SDK
        private async Task<int> LoadCoinsAsync()
        {
            try
            {
                // Используем Yandex Games SDK для загрузки данных
                //return await YandexGamesIntegration.Instance.LoadPlayerData("coins", 0);
                return 0; // Заглушка, пока SDK не подключен
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CurrencyManager] Ошибка загрузки монет: {ex.Message}");
                return 0; // Возвращаем 0 в случае ошибки
            }
        }

        // Асинхронное сохранение монет
        private async void SaveCoinsAsync()
        {
            try
            {
                // Используем Yandex Games SDK для сохранения данных
                //await YandexGamesIntegration.Instance.SavePlayerData("coins", coins);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CurrencyManager] Ошибка сохранения монет: {ex.Message}");
            }
        }

        public int Coins => coins;
    }
}