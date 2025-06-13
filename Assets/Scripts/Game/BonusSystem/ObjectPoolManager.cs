using UnityEngine;
using System.Collections.Generic;

namespace UshiSoft.UACPF
{
    public class ObjectPoolManager : MonoBehaviour
    {
        public static ObjectPoolManager Instance { get; private set; }

        [System.Serializable]
        public class Pool
        {
            public string tag; // Тэг для идентификации типа объекта
            public GameObject prefab; // Префаб, который будет использоваться
            public int initialSize; // Начальный размер пула
        }

        [SerializeField] private List<Pool> pools;
        private Dictionary<string, Queue<GameObject>> poolDictionary;
        private Dictionary<string, Pool> poolConfigs; // Для быстрого доступа к конфигурации пула по тэгу

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // DontDestroyOnLoad(gameObject); // Если менеджер пула должен быть глобальным между сценами
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            poolDictionary = new Dictionary<string, Queue<GameObject>>();
            poolConfigs = new Dictionary<string, Pool>();

            foreach (Pool pool in pools)
            {
                Queue<GameObject> objectPool = new Queue<GameObject>();

                for (int i = 0; i < pool.initialSize; i++)
                {
                    GameObject obj = Instantiate(pool.prefab, transform); // Создаем как дочерний объект менеджера
                    obj.SetActive(false); // Изначально деактивируем
                    objectPool.Enqueue(obj);
                }
                poolDictionary.Add(pool.tag, objectPool);
                poolConfigs.Add(pool.tag, pool); // Сохраняем конфигурацию
            }
        }

        // Метод для получения объекта из пула
        public GameObject GetPooledObject(string tag)
        {
            if (!poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
                return null;
            }

            GameObject objectToSpawn;

            // Если пул пуст, создаем новый объект и добавляем его в пул (динамическое расширение)
            if (poolDictionary[tag].Count == 0)
            {
                Debug.LogWarning($"Pool '{tag}' is exhausted. Creating new object to expand the pool.");
                
                // Проверяем, есть ли у нас конфигурация для этого тэга
                if (!poolConfigs.ContainsKey(tag))
                {
                    Debug.LogError($"No pool configuration found for tag '{tag}'. Cannot create new object.");
                    return null;
                }

                GameObject newObj = Instantiate(poolConfigs[tag].prefab, transform); // Используем prefab из конфига
                newObj.SetActive(false); // Деактивируем, т.к. его активируют ниже
                poolDictionary[tag].Enqueue(newObj); // Добавляем новый объект в очередь пула
            }
            
            objectToSpawn = poolDictionary[tag].Dequeue();

            objectToSpawn.SetActive(true); // Активируем перед возвратом
            // Отсоединяем от менеджера пула, чтобы он мог двигаться свободно
            objectToSpawn.transform.SetParent(null); 
            return objectToSpawn;
        }

        // Метод для возврата объекта в пул
        public void ReturnPooledObject(string tag, GameObject obj)
        {
            if (!poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning($"Pool with tag {tag} doesn't exist. Destroying object {obj.name}.");
                Destroy(obj); 
                return;
            }

            obj.SetActive(false); // Деактивируем перед возвратом
            // Возвращаем в иерархию менеджера пула для чистоты сцены
            obj.transform.SetParent(transform); 
            poolDictionary[tag].Enqueue(obj);
        }
    }
}