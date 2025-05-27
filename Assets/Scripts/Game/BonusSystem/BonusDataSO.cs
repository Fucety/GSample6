using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UshiSoft.UACPF
{
    // Хранит бонусы и возвращает случайный бонус по редкости
    [CreateAssetMenu(fileName = "BonusDataSO", menuName = "KartingRush/BonusDataSO")]
    public class BonusDataSO : ScriptableObject
    {
        [SerializeField] private List<BonusBase> commonBonuses; // Обычные бонусы (70%)
        [SerializeField] private List<BonusBase> rareBonuses;   // Редкие бонусы (25%)
        [SerializeField] private List<BonusBase> epicBonuses;   // Эпические бонусы (5%)

        // Возвращает случайный бонус с учетом вероятностей
        public BonusBase GetRandomBonus()
        {
            float roll = Random.value; // Случайное число от 0 до 1
            if (roll < 0.05f) // 5% шанс на эпический
            {
                return epicBonuses[Random.Range(0, epicBonuses.Count)];
            }
            else if (roll < 0.30f) // 25% шанс на редкий (0.05 + 0.25)
            {
                return rareBonuses[Random.Range(0, rareBonuses.Count)];
            }
            else // 70% шанс на обычный
            {
                return commonBonuses[Random.Range(0, commonBonuses.Count)];
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
#endif
    }
}