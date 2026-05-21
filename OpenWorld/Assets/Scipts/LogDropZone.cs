using UnityEngine;

public class LogDropZone : MonoBehaviour
{
    public static LogDropZone Instance; // Simple link shortcut for the interaction script

    [Header("Warehouse Status")]
    public int totalLogsInWarehouse = 0;
    public bool isPlayerInsideZone = false;

    void Awake()
    {
        Instance = this;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<InventorySystem>() != null)
        {
            isPlayerInsideZone = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<InventorySystem>() != null)
        {
            isPlayerInsideZone = false;
        }
    }
}