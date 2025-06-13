using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Text;

namespace UshiSoft.UACPF
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }
        
        [Header("Панели")]
        [SerializeField] private GameObject countdownPanel; // Вернули countdownPanel
        [SerializeField] private GameObject arenaUIPanel;     // Вернули arenaUIPanel
        [SerializeField] private GameObject resultsPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject bonusPickupPanel;

        [Header("Элементы UI")]
        [SerializeField] private TextMeshProUGUI countdownText; // Вернули countdownText
        [SerializeField] private TextMeshProUGUI coinsThisMatchText; // Текст для монет, заработанных за матч
        [SerializeField] private TextMeshProUGUI eliminationsText;
        [SerializeField] private Image bonusIcon; // Вернули bonusIcon
        
        private RewardHandler rewardHandler; // Ссылка на наш новый компонент
        
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
            
            // Находим RewardHandler на панели результатов
            if (resultsPanel != null)
            {
                rewardHandler = resultsPanel.GetComponent<RewardHandler>();
            }
        }
        
        private void Start()
        {
            // При старте просто прячем все панели.
            // UI Manager не должен принимать решений о том, что показывать.
            // Он ждет команды от GameManager через события.
            //HideAllPanels();

            // Инициализируем текст, чтобы избежать пустых полей
            if (coinsThisMatchText != null) UpdateCoinsThisMatch(0);
            if (eliminationsText != null) UpdateEliminations(0);
        }

        // Показать обратный отсчёт перед началом матча
        public void ShowCountdown(float duration)
        {
            if (countdownPanel != null)
            {
                countdownPanel.SetActive(true); // Активируем панель отсчета
                StartCoroutine(UpdateCountdownCoroutine(duration)); // Запускаем корутину обновления текста отсчета
                Debug.Log("[UIManager] Countdown Panel активирована."); // Для отладки
            }
            else
            {
                Debug.LogError("[UIManager] Панель обратного отсчета (countdownPanel) не назначена в инспекторе!"); // Сообщение об ошибке, если панель не назначена
            }
        }

        private IEnumerator UpdateCountdownCoroutine(float duration)
        {
            float timer = duration;
            while (timer > 0f)
            {
                countdownText.text = Mathf.CeilToInt(timer).ToString(); // Обновляем текст отсчета
                timer -= Time.deltaTime;
                yield return null;
            }
            countdownText.text = "GO!"; // Сообщение "СТАРТ!" в конце отсчета
            yield return new WaitForSeconds(1f);
            
            // После завершения отсчета панель просто скрывается.
            // Показ ArenaUI произойдет по событию OnMatchStarted,
            // которое будет вызвано GameEventHub после завершения отсчета.
            countdownPanel.SetActive(false); // Деактивируем панель отсчета
            Debug.Log("[UIManager] Countdown Panel деактивирована после отсчета."); // Для отладки
        }

        // Показать интерфейс арены
        public void ShowArenaUI()
        {
            if (arenaUIPanel != null)
            {
                arenaUIPanel.SetActive(true); // Активируем панель UI арены
            }
        }

        // Скрыть интерфейс арены
        public void HideArenaUI()
        {
            if (arenaUIPanel != null)
            {
                arenaUIPanel.SetActive(false); // Деактивируем панель UI арены
            }
        }

        public void ShowMatchResults(int coins, int eliminations)
        {
            if (resultsPanel == null || rewardHandler == null)
            {
                Debug.LogError("Панель результатов или RewardHandler не настроены!");
                return;
            }
            
            HideAllPanels();
            resultsPanel.SetActive(true);
            
            // Просто передаем данные в RewardHandler, он сделает остальное
            rewardHandler.Initialize(coins, eliminations);
        }
        
        public void ShowPauseMenu()
        {
            if (pausePanel != null) pausePanel.SetActive(true); // Активируем панель паузы
        }

        public void HidePauseMenu()
        {
            if (pausePanel != null) pausePanel.SetActive(false); // Деактивируем панель паузы
        }

        // Новая функция для обновления текстового поля с авторазмером:
        private void UpdateTMPText(TextMeshProUGUI tmp, string value)
        {
            if (tmp == null) return;
            tmp.enableAutoSizing = true;       // Включаем авторазмер TMP
            tmp.text = value;                  // Обновляем текст
            tmp.ForceMeshUpdate();             // Обновляем меш для вычисления размера
            float computedSize = tmp.fontSize; // Получаем вычисленный размер
            tmp.enableAutoSizing = false;      // Отключаем авторазмер
            tmp.fontSize = computedSize;       // Применяем получившийся размер как обычный fontSize
        }

        // Этот метод обновляет UI на арене для монет, заработанных за матч
        public void UpdateCoinsThisMatch(int coins)
        {
            if (coinsThisMatchText != null)
            {
                // Используем UpdateTMPText для обновления текста монет с авторазмером
                UpdateTMPText(coinsThisMatchText, $"{coins}");
            }
        }

        public void UpdateEliminations(int eliminations)
        {
            UpdateTMPText(eliminationsText, $"{eliminations}");
        }
        
        public void ShowBonusPickup(BonusBase bonus)
        {
            if (bonusPickupPanel != null && bonusIcon != null && bonus.Icon != null)
            {
                bonusPickupPanel.SetActive(true); // Активируем панель подбора бонуса
                bonusIcon.sprite = bonus.Icon; // Устанавливаем иконку бонуса
            }
        }

        public void HideBonusPickupIcon()
        {
            if (bonusPickupPanel != null) bonusPickupPanel.SetActive(false); // Деактивируем панель подбора бонуса
        }

        // Этот метод теперь используется только для полного сброса UI
        // или когда явно требуется скрыть все панели.
        public void HideAllPanels()
        {
            if (countdownPanel != null) countdownPanel.SetActive(false); // Скрываем панель отсчета
            if (arenaUIPanel != null) arenaUIPanel.SetActive(false); // Скрываем UI арены
            if (resultsPanel != null) resultsPanel.SetActive(false); // Скрываем панель результатов
            if (pausePanel != null) pausePanel.SetActive(false); // Скрываем панель паузы
            if (bonusPickupPanel != null) bonusPickupPanel.SetActive(false); // Скрываем панель подбора бонуса
        }
    }
}