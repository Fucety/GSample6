using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UshiSoft.UACPF
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [SerializeField] private GameObject countdownPanel; // Панель обратного отсчёта
        [SerializeField] private TextMeshProUGUI countdownText; // Текст отсчёта
        [SerializeField] private GameObject raceUIPanel; // Панель гонки
        [SerializeField] private TextMeshProUGUI lapText; // Текст кругов
        [SerializeField] private TextMeshProUGUI coinsText; // Текст монет
        [SerializeField] private TextMeshProUGUI premiumText; // Текст премиум-валюты
        [SerializeField] private GameObject resultsPanel; // Панель результатов
        [SerializeField] private TextMeshProUGUI resultsText; // Текст результатов
        [SerializeField] private GameObject pausePanel; // Панель паузы
        [SerializeField] private GameObject bonusPickupPanel; // Панель бонуса
        [SerializeField] private Image bonusIcon; // Иконка бонуса

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

        private void Start()
        {
            HideAllPanels();
        }

        public void ShowCountdown(float duration)
        {
            HideAllPanels();
            countdownPanel.SetActive(true);
            StartCoroutine(UpdateCountdown(duration));
        }

        private System.Collections.IEnumerator UpdateCountdown(float duration)
        {
            float timer = duration;
            while (timer > 0f)
            {
                countdownText.text = Mathf.CeilToInt(timer).ToString();
                timer -= Time.deltaTime;
                yield return null;
            }
            countdownText.text = "GO!";
            yield return new WaitForSeconds(1f);
            countdownPanel.SetActive(false);
        }

        public void ShowRaceUI()
        {
            HideAllPanels();
            raceUIPanel.SetActive(true);
        }

        public void ShowRaceResults(int position, int coins, int eliminations)
        {
            HideAllPanels();
            resultsPanel.SetActive(true);
            resultsText.text = $"Position: {position}\nCoins: {coins}\nEliminations: {eliminations}";
        }

        public void ShowPauseMenu()
        {
            pausePanel.SetActive(true);
        }

        public void HidePauseMenu()
        {
            pausePanel.SetActive(false);
        }

        public void UpdateLap(int currentLap, int totalLaps)
        {
            lapText.text = $"Lap {currentLap}/{totalLaps}";
        }

        public void UpdateCoins(int coins)
        {
            coinsText.text = $"Coins: {coins}";
        }

        public void UpdatePremium(int premium)
        {
            premiumText.text = $"Premium: {premium}";
        }

        public void ShowBonusPickup(BonusBase bonus)
        {
            bonusPickupPanel.SetActive(true);
            bonusIcon.sprite = bonus.Icon;
            StartCoroutine(HideBonusPickup());
        }

        private System.Collections.IEnumerator HideBonusPickup()
        {
            yield return new WaitForSeconds(2f);
            bonusPickupPanel.SetActive(false);
        }

        private void HideAllPanels()
        {
            countdownPanel.SetActive(false);
            raceUIPanel.SetActive(false);
            resultsPanel.SetActive(false);
            pausePanel.SetActive(false);
            bonusPickupPanel.SetActive(false);
        }
    }
}