using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace UshiSoft.UACPF
{
    // Управляет спавном и респавном бонусных коробок
    public class BonusSpawnManager : MonoBehaviour
    {
        [SerializeField] private BonusBox bonusBoxPrefab; // Префаб коробки
        [SerializeField] private Transform[] spawnPoints; // Точки спавна
        [SerializeField] private float respawnTime = 5f;  // Время респавна

        private List<BonusBox> activeBoxes; // Список активных коробок

        private void Awake()
        {
            activeBoxes = new List<BonusBox>();
            SpawnAllBoxes();
        }

        // Создает коробки во всех точках спавна
        private void SpawnAllBoxes()
        {
            foreach (Transform point in spawnPoints)
            {
                BonusBox box = Instantiate(bonusBoxPrefab, point.position, point.rotation, transform);
                activeBoxes.Add(box);
            }
        }

        // Запрос на респавн коробки
        public void RequestRespawn(BonusBox box)
        {
            StartCoroutine(RespawnBox(box));
        }

        // Логика респавна коробки
        private IEnumerator RespawnBox(BonusBox box)
        {
            yield return new WaitForSeconds(respawnTime);
            box.Respawn(); // Восстанавливаем коробку
        }
    }
}