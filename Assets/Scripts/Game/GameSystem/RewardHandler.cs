using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YG;
using UnityEngine.SceneManagement; // Добавляем для перезагрузки сцены

namespace UshiSoft.UACPF
{
    public class RewardHandler : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI resultsText;
        
        // Только три новые кнопки
        [SerializeField] private Button backToMenuButton;
        [SerializeField] private Button randomBonusButton;
        [SerializeField] private Button playAgainButton;

        private int baseReward;

        private void OnEnable()
        {
            // Добавляем слушателей для новых кнопок
            backToMenuButton.onClick.AddListener(OnBackToMenu);
            randomBonusButton.onClick.AddListener(OnRandomBonus);
            playAgainButton.onClick.AddListener(OnPlayAgain);
        }

        private void OnDisable()
        {
            // Удаляем слушателей для новых кнопок
            backToMenuButton.onClick.RemoveListener(OnBackToMenu);
            randomBonusButton.onClick.RemoveListener(OnRandomBonus);
            playAgainButton.onClick.RemoveListener(OnPlayAgain);
        }

        public void Initialize(int coinsEarned, int eliminations)
        {
            baseReward = coinsEarned;
            System.Text.StringBuilder leaderboardText = new System.Text.StringBuilder();
            leaderboardText.AppendLine("<b><color=#fffb00>РЕЗУЛЬТАТА МАТЧА</color></b>");
            leaderboardText.AppendLine($"Заработано монет: {coinsEarned}");
            leaderboardText.AppendLine($"Ваши устранения: {eliminations}\n");
            leaderboardText.AppendLine("<b>ТАБЛИЦА ЛИДЕРОВ:</b>");

            if (GameManager.Instance != null)
            {
                var rankings = GameManager.Instance.GetLeaderboard();
                for (int i = 0; i < rankings.Count; i++)
                {
                    var racerEntry = rankings[i];
                    string name = racerEntry.Key.GetComponent<PlayerCarControl>() != null ? "<b>Вы</b>" : $"Бот {i + 1}";
                    leaderboardText.AppendLine($"{i + 1}. {name}: {racerEntry.Value} устр.");
                }
            }
            else
            {
                leaderboardText.AppendLine("Таблица лидеров недоступна.");
            }

            resultsText.text = leaderboardText.ToString();
            
            // Делаем новые кнопки активными при инициализации
            backToMenuButton.gameObject.SetActive(true);
            randomBonusButton.gameObject.SetActive(true);
            playAgainButton.gameObject.SetActive(true);
        }
        
        // Функция: Назад в меню, начисление награды и показ интерстициальной рекламы
        private void OnBackToMenu()
        {
            Debug.Log("[RewardHandler] Назад в меню: начисление награды и показ рекламы...");
            FinalizeReward(baseReward); // Начисляем базовую награду
            YG2.InterstitialAdvShow(); // Показываем интерстициальную рекламу
            Debug.Log("[RewardHandler] Переход в главное меню...");
            // Замените "MainMenuScene" на имя вашей сцены главного меню
            SceneManager.LoadScene("Menu"); 
            // Деактивируем кнопку после использования, чтобы избежать повторных нажатий
            backToMenuButton.gameObject.SetActive(false);
            randomBonusButton.gameObject.SetActive(false);
            playAgainButton.gameObject.SetActive(false);
        }

        // Функция: Рандомный бонус от x1,5 до x2 за просмотр рекламы, без перехода в меню
        private void OnRandomBonus()
        {
            Debug.Log("[RewardHandler] Запрос на показ рекламы для получения рандомного бонуса...");
            YG2.RewardedAdvShow("random_bonus", () =>
            {
                Debug.Log("[RewardHandler] Реклама успешно просмотрена, начисляем рандомный бонус!");
                float randomMultiplier = Random.Range(1.5f, 2.01f); // От 1.5 до 2.0 включительно
                int bonusReward = Mathf.RoundToInt(baseReward * randomMultiplier);
                FinalizeReward(bonusReward);
                Debug.Log($"[RewardHandler] Получен рандомный бонус: x{randomMultiplier:F2}. Общая награда: {bonusReward} монет.");
                // Деактивируем кнопку после использования, чтобы избежать повторных нажатий
                randomBonusButton.gameObject.SetActive(false);
            });
        }

        // Функция: Перезапуск сцены с показом интерстициальной рекламы
        private void OnPlayAgain()
        {
            Debug.Log("[RewardHandler] Запрос на показ интерстициальной рекламы перед перезапуском сцены...");
            YG2.InterstitialAdvShow(); // Показываем интерстициальную рекламу
            Debug.Log("[RewardHandler] Перезапуск текущей сцены...");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Перезагружаем текущую сцену
            // Деактивируем кнопку после использования, чтобы избежать повторных нажатий
            backToMenuButton.gameObject.SetActive(false);
            randomBonusButton.gameObject.SetActive(false);
            playAgainButton.gameObject.SetActive(false);
        }

        private void FinalizeReward(int finalAmount)
        {
            PlayerDataManager.Instance.AddCoins(finalAmount);
            resultsText.text += $"<color=red>ПОЛУЧЕНО: {finalAmount} МОНЕТ!</color>";
        }
    }
}