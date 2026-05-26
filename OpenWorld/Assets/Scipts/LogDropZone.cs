using UnityEngine;

public class LogDropZone : MonoBehaviour
{
    public static LogDropZone Instance;

    [Header("Warehouse Inventory Matrix")]
    public int totalLogsInWarehouse = 0;
    public bool isPlayerInsideZone = false;

    void Awake()
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