using UnityEngine;

namespace UshiSoft.UACPF
{
    public class CheckpointTrigger : MonoBehaviour
    {
        private CarControllerBase carController;

        private void Awake()
        {
            carController = GetComponent<CarControllerBase>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Checkpoint"))
            {
                TrackManager.Instance.OnCheckpointReached(carController, other.transform);
            }
        }
    }
}