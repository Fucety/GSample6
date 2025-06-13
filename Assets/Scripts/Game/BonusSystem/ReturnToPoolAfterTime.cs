using UnityEngine;
using System.Collections;

namespace UshiSoft.UACPF
{
    public class ReturnToPoolAfterTime : MonoBehaviour
    {
        [SerializeField] private float returnDelay = 1f; // Задержка перед возвратом в пул
        private string poolTag; // Тэг пула, из которого был взят объект

        public void Initialize(string tag)
        {
            poolTag = tag;
            StartCoroutine(ReturnRoutine());
        }

        private IEnumerator ReturnRoutine()
        {
            yield return new WaitForSeconds(returnDelay);
            if (ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.ReturnPooledObject(poolTag, gameObject);
            }
            // else { Destroy(gameObject); } // Запасной вариант
        }
    }
}