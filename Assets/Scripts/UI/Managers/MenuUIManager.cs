using UnityEngine;
using TMPro;

// Управляет элементами UI только в главном меню
public class MenuUIManager : MonoBehaviour
{
    public static MenuUIManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI coinsText; // Ссылка на текст с монетами

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Добавляем метод авторазмера текста
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

    // Публичный метод для обновления текста с монетами
    public void UpdateCoins(int amount)
    {
        if (coinsText != null)
        {
            UpdateTMPText(coinsText, amount.ToString());
        }
        else
        {
            Debug.LogError("[MenuUIManager] Текст для монет не назначен в инспекторе!");
        }
    }
}