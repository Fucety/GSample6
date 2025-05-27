using UnityEngine;

namespace UshiSoft.UACPF
{
    public class Rocket : MonoBehaviour
    {
        [SerializeField] private float speed = 20f;    // Скорость ракеты
        [SerializeField] private float lifetime = 5f;  // Время жизни

        private void Start()
        {
            Destroy(gameObject, lifetime); // Уничтожаем через 5 секунд
        }

        private void Update()
        {
            transform.Translate(Vector3.forward * speed * Time.deltaTime); // Движение вперед
        }

        private void OnTriggerEnter(Collider other)
        {
            CarControllerBase target = other.GetComponentInParent<CarControllerBase>();
            if (target != null)
            {
                GameManager.Instance.RegisterElimination(target); // Устраняем машину
                Destroy(gameObject); // Уничтожаем ракету
            }
        }
    }
}