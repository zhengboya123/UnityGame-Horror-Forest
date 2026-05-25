using UnityEngine;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public Transform cameraTransform;
    public float interactionDistance = 4.5f;
    public TextMeshProUGUI hintTextElement; 

    [Header("Item Prefabs to Drop")]
    public GameObject axePrefab; 
    public GameObject logPrefab; 

    private InventorySystem inventory;
    private Terrain currentTerrain;

    void Start()
    {
        inventory = GetComponent<InventorySystem>();
        currentTerrain = Terrain.activeTerrain;
        ClearPrompt();
    }

    void Update()
    {
        ManageInteractionRaycast();
        HandleAbandonItemInput(); 
    }

    void ManageInteractionRaycast()
    {
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, interactionDistance))
        {
            // 1. LOOKING AT ANY GROUND PICKUP (Both Axe and Log use this tag now)
            if (hit.collider.CompareTag("Pickup"))
            {
                string objectName = hit.collider.gameObject.name;

                // Check if the pickup object is an Axe
                if (objectName.Contains("Axe"))
                {
                    if (inventory != null && !inventory.unlockedSlots[1])
                    {
                        SetPromptText("Press [E] to pick up Axe");
                        if (Input.GetKeyDown(KeyCode.E))
                        {
                            inventory.AddItemToHotbar(1, "Axe");
                            Destroy(hit.collider.gameObject);
                            ClearPrompt();
                        }
                    }
                    else
                    {
                        SetPromptText("You already have an Axe");
                    }
                    return;
                }
                // Check if the pickup object is a Log
                else if (objectName.Contains("Log"))
                {
                    if (inventory != null)
                    {
                        if (inventory.CanPickupLog())
                        {
                            SetPromptText("Press [E] to pick up Log");
                            if (Input.GetKeyDown(KeyCode.E))
                            {
                                inventory.AddItemToHotbar(2, "Log");
                                Destroy(hit.collider.gameObject);
                                ClearPrompt();
                            }
                        }
                        else
                        {
                            SetPromptText("Inventory Full! (Max 3 Logs)");
                        }
                    }
                    return;
                }
            }

            // 2. LOOKING AT THE WAREHOUSE DROP ZONE
            if (hit.collider.CompareTag("WarehouseSpace") || (LogDropZone.Instance != null && LogDropZone.Instance.isPlayerInsideZone))
            {
                if (inventory != null && inventory.woodLogCount > 0)
                {
                    SetPromptText("Press [E] to store logs in warehouse");
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        LogDropZone.Instance.totalLogsInWarehouse += inventory.woodLogCount;
                        Debug.Log("Warehouse total logs: " + LogDropZone.Instance.totalLogsInWarehouse);
                        inventory.EmptyLogsForProcessing();
                        ClearPrompt();
                    }
                }
                else if (inventory != null && inventory.woodLogCount == 0)
                {
                    SetPromptText("No logs in inventory to store");
                }
                return;
            }

            // 3. TERRAIN TREE DETECTION (Bypasses tags entirely)
            if (hit.collider is TerrainCollider && currentTerrain != null)
            {
                TerrainData terrainData = currentTerrain.terrainData;
                float searchRadius = 1.5f; 
                Vector3 terrainPos = hit.point - currentTerrain.transform.position;
                
                foreach (TreeInstance tree in terrainData.treeInstances)
                {
                    Vector3 localTreePos = Vector3.Scale(tree.position, terrainData.size);
                    
                    if (Vector3.Distance(new Vector3(localTreePos.x, terrainPos.y, localTreePos.z), terrainPos) < searchRadius)
                    {
                        if (inventory != null && inventory.IsHoldingAxe())
                        {
                            SetPromptText("Press [Left Click] to chop tree");
                        }
                        else
                        {
                            SetPromptText("Requires an Axe to chop");
                        }
                        return; 
                    }
                }
            }
        }

        ClearPrompt();
    }

    void HandleAbandonItemInput()
    {
        if (!Input.GetKeyDown(KeyCode.Q)) return;
        if (inventory == null) return;

        if (inventory.currentSlot == 1 && inventory.unlockedSlots[1])
        {
            inventory.DropAxeFromInventory();
            SpawnDroppedItem(axePrefab);
            return;
        }

        if (inventory.currentSlot == 2 && inventory.unlockedSlots[2] && inventory.woodLogCount > 0)
        {
            inventory.DropSingleLogFromInventory();
            SpawnDroppedItem(logPrefab);
            return;
        }
    }

    void SpawnDroppedItem(GameObject itemPrefab)
    {
        if (itemPrefab != null)
        {
            Vector3 spawnPosition = transform.position + transform.forward * 1.5f + Vector3.up * 0.5f;
            Instantiate(itemPrefab, spawnPosition, Quaternion.identity);
        }
    }

    void SetPromptText(string text)
    {
        if (hintTextElement != null) hintTextElement.text = text;
    }

    void ClearPrompt()
    {
        if (hintTextElement != null) hintTextElement.text = "";
    }
}