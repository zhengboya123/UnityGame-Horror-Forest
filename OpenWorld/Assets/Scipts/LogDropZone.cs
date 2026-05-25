using UnityEngine;

public class LogDropZone : MonoBehaviour
{
    public static LogDropZone Instance { get; private set; }

    [Header("Warehouse Data")]
    public int totalLogsInWarehouse = 0;
    public bool isPlayerInsideZone = false;

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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInsideZone = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInsideZone = false;
        }
    }
}