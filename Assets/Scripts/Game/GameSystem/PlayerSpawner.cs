using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Структура для сопоставления имени скина с его префабом
[System.Serializable]
public struct SkinPrefabMapping
{
    // Это имя должно точно совпадать с `skin.spriteName` из вашего `ShopPanelDataSO`
    public string skinName; 
    // Сюда перетаскивается префаб соответствующей машины
    public GameObject carPrefab; 
}

public class PlayerSpawner : MonoBehaviour
{
    [Header("Настройки спавна")]
    [Tooltip("Точка, в которой будет создан префаб машины игрока.")]
    [SerializeField] private Transform playerSpawnPoint;
    [Tooltip("Список всех доступных префабов машин и их имен.")]
    [SerializeField] private List<SkinPrefabMapping> skinPrefabs;

    void Start()
    {
        SpawnPlayerCar();
    }

    private void SpawnPlayerCar()
    {
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("FATAL: PlayerDataManager не найден на сцене! Невозможно определить скин игрока.");
            return;
        }

        // 1. Получаем имя экипированного скина из общего хранилища
        string equippedSkinName = PlayerDataManager.Instance.GetEquippedSkin();

        // 2. Находим соответствующий префаб в списке
        GameObject playerPrefabToSpawn = skinPrefabs.FirstOrDefault(mapping => mapping.skinName == equippedSkinName).carPrefab;

        // 3. Защита: если скин по какой-то причине не найден, используем первый префаб в списке
        if (playerPrefabToSpawn == null)
        {
            Debug.LogWarning($"Префаб для скина '{equippedSkinName}' не найден в списке PlayerSpawner! Будет использован дефолтный префаб.");
            if (skinPrefabs.Count > 0)
            {
                playerPrefabToSpawn = skinPrefabs[0].carPrefab;
            }
            else
            {
                Debug.LogError("FATAL: Список префабов машин в PlayerSpawner пуст! Нечего спавнить.");
                return;
            }
        }

        // 4. Создаем экземпляр префаба в указанной точке
        GameObject playerCarInstance = Instantiate(playerPrefabToSpawn, playerSpawnPoint.position, playerSpawnPoint.rotation);
        playerCarInstance.name = "PlayerCar (Spawned)";

        // 5. Регистрируем созданную машину в игровом менеджере
        var playerCarController = playerCarInstance.GetComponent<UshiSoft.UACPF.CarControllerBase>();
        if (playerCarController != null && UshiSoft.UACPF.GameManager.Instance != null)
        {
            UshiSoft.UACPF.GameManager.Instance.RegisterPlayerCar(playerCarController);
            Debug.Log($"Машина игрока '{equippedSkinName}' создана и успешно зарегистрирована в GameManager.");
        }
        else
        {
            Debug.LogError("Не удалось зарегистрировать машину игрока в GameManager. Проверьте наличие CarControllerBase на префабе и GameManager на сцене.");
        }
    }
}