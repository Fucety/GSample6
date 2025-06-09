using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace UshiSoft.UACPF
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [SerializeField] private GameObject countdownPanel; // Панель обратного отсчёта
        [SerializeField] private TextMeshProUGUI countdownText; // Текст отсчёта
        [SerializeField] private GameObject arenaUIPanel; // Панель арены
        [SerializeField] private TextMeshProUGUI coinsText; // Текст монет
        [SerializeField] private TextMeshProUGUI eliminationsText; // Текст устранений
        [SerializeField] private GameObject resultsPanel; // Панель результатов
        [SerializeField] private TextMeshProUGUI resultsText; // Текст результатов
        [SerializeField] private GameObject pausePanel; // Панель паузы
        [SerializeField] private GameObject bonusPickupPanel; // Панель бонуса
        [SerializeField] private Image bonusIcon; // Иконка бонуса

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

        private void Start()
        {
            // Инициализируем интерфейс арены
            HideAllPanels();
            arenaUIPanel.SetActive(true);
            if (coinsText != null)
                UpdateCoins(0);
            if (eliminationsText != null)
                UpdateEliminations(0);
        }

        // Показать обратный отсчёт перед началом матча
        public void ShowCountdown(float duration)
        {
            HideAllPanels();
            countdownPanel.SetActive(true);
            StartCoroutine(UpdateCountdown(duration));
        }

        // Обновление обратного отсчёта
        private IEnumerator UpdateCountdown(float duration)
        {
            float timer = duration;
            while (timer > 0f)
            {
                countdownText.text = Mathf.CeilToInt(timer).ToString();
                timer -= Time.deltaTime;
                yield return null;
            }
            countdownText.text = "СТАРТ!";
            yield return new WaitForSeconds(1f);
            countdownPanel.SetActive(false);
            ShowArenaUI();
        }

        // Показать интерфейс арены
        public void ShowArenaUI()
        {
            HideAllPanels();
            if (arenaUIPanel != null)
                arenaUIPanel.SetActive(true);
        }

        // Показать результаты матча
        public void ShowMatchResults(int coins, int playerEliminations)
        {
            HideAllPanels();
            resultsPanel.SetActive(true);
            // Формируем таблицу мест
            System.Text.StringBuilder leaderboard = new System.Text.StringBuilder();
            var rankings = GameManager.Instance.GetLeaderboard();
            for (int i = 0; i < rankings.Count; i++)
            {
                var racer = rankings[i];
                string name = racer.Key.GetComponent<PlayerCarControl>() != null ? "Игрок" : $"Бот {i}";
                leaderboard.AppendLine($"{i + 1}. {name}: {racer.Value} устранений");
            }
            resultsText.text = $"Монеты: {coins}\nВаши устранения: {playerEliminations}\n\nТаблица мест:\n{leaderboard}";
        }

        // Показать панель паузы
        public void ShowPauseMenu()
        {
            if (pausePanel != null)
                pausePanel.SetActive(true);
        }

        // Скрыть панель паузы
        public void HidePauseMenu()
        {
            if (pausePanel != null)
                pausePanel.SetActive(false);
        }

        // Обновить отображение монет
        public void UpdateCoins(int coins)
        {
            if (coinsText != null)
            {
                coinsText.text = $"{coins}";
                AdjustFontSize(coinsText, coins.ToString().Length);
            }
        }

        // Обновить отображение устранений
        public void UpdateEliminations(int eliminations)
        {
            if (eliminationsText != null)
            {
                eliminationsText.text = $"{eliminations}";
                AdjustFontSize(eliminationsText, eliminations.ToString().Length);
            }
        }

        // Показать иконку подобранного бонуса
        public void ShowBonusPickup(BonusBase bonus)
        {
            if (bonusPickupPanel != null && bonusIcon != null && bonus.Icon != null)
            {
                bonusPickupPanel.SetActive(true);
                bonusIcon.sprite = bonus.Icon;
            }
        }

        // Скрыть иконку бонуса
        public void HideBonusPickupIcon()
        {
            if (bonusPickupPanel != null)
                bonusPickupPanel.SetActive(false);
        }

        // Скрыть все панели
        private void HideAllPanels()
        {
            if (countdownPanel != null) countdownPanel.SetActive(false);
            if (arenaUIPanel != null) arenaUIPanel.SetActive(false);
            if (resultsPanel != null) resultsPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            if (bonusPickupPanel != null) bonusPickupPanel.SetActive(false);
        }

        // Изменён метод AdjustFontSize
        private void AdjustFontSize(TextMeshProUGUI text, int count)
        {
            if (text == null) return;
            switch (count)
            {
                case 1:
                    text.fontSize = 130f;
                    break;
                case 2:
                    text.fontSize = 115f;
                    break;
                case 3:
                    text.fontSize = 100f;
                    break;
                case 4:
                    text.fontSize = 85f;
                    break;
                default:
                    text.fontSize = 100f;
                    break;
            }
        }
    }
}