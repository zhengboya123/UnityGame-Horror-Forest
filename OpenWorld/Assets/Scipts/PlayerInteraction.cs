using UnityEngine;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
<<<<<<< Updated upstream
    public Transform cameraTransform; 
    public float interactionDistance = 4.5f;
    public TextMeshProUGUI hintTextElement; // Drag your TextMeshPro asset here
    
=======
    public Transform cameraTransform;
    public float interactionDistance = 4.5f;
    public TextMeshProUGUI hintTextElement; // Drag your TextMeshPro asset here
   
    [Header("Drop Prefab References")]
    public GameObject axePrefab; // Drag your ground Axe Prefab asset here!
    public GameObject logPrefab; // Drag your ground Log Prefab asset here!

>>>>>>> Stashed changes
    private InventorySystem inventory;

    void Start()
    {
        inventory = GetComponent<InventorySystem>();
<<<<<<< Updated upstream
=======
        ClearPrompt();
>>>>>>> Stashed changes
    }

    void Update()
    {
        ManageInteractionRaycast();
<<<<<<< Updated upstream
=======
        HandleWarehouseZoneInputs();
        HandleAbandonItemInput(); // Checks for [Q] presses every frame
>>>>>>> Stashed changes
    }

    void ManageInteractionRaycast()
    {
        RaycastHit hit;
<<<<<<< Updated upstream
        // Project a structural logic raycast forward through your center crosshair dot
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, interactionDistance))
        {
            // Verify if your center point crosshair crosses a collider marked with the Pickup Tag
            if (hit.collider.CompareTag("Pickup"))
            {
                string objectName = hit.collider.gameObject.name;

                // DYNAMIC STRING WRITER SYSTEM: Matches display sentences directly to items
                if (hintTextElement != null)
                {
                    hintTextElement.gameObject.SetActive(true);
                    
                    if (objectName.Contains("Axe"))
                    {
                        hintTextElement.text = "Press [E] to pick up the Axe";
                    }
                    else if (objectName.Contains("Log") || objectName.Contains("Wood"))
                    {
                        hintTextElement.text = "Press [E] to pick up the Wood Log";
                    }
                }

                // COLLECTION DESTRUCT SEQUENCE
                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (objectName.Contains("Axe"))
                    {
                        inventory.AddItemToHotbar(1, "Axe"); // Assigns data to slot 2 (Index 1)
                        ClearPrompt();
                        Destroy(hit.collider.gameObject);
                    }
                    else if (objectName.Contains("Log") || objectName.Contains("Wood"))
                    {
                        inventory.AddItemToHotbar(2, "Wood_Log"); // Assigns data to slot 3 (Index 2)
                        ClearPrompt();
                        Destroy(hit.collider.gameObject);
                    }
                }
                return; // Blocks execution falling lower while actively looking at an item
            }
        }

        // If you look back into open space or sky, safely clear out your text overlay HUD elements
        ClearPrompt();
=======
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, interactionDistance))
        {
            GameObject lookTarget = hit.collider.gameObject;
            string objectName = lookTarget.name.ToLower();

            // --- CRITICAL FILTER: ONLY CHECK IF IT HAS YOUR PICKUP TAG ---
            if (lookTarget.CompareTag("Pickup"))
            {
                hintTextElement.gameObject.SetActive(true);

                // --- FIXED RULE 1: IT IS AN AXE PICKUP ---
                if (objectName.Contains("axe"))
                {
                    hintTextElement.text = "Press [E] to pick up the Axe";

                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        inventory.AddItemToHotbar(1, "Axe");
                        ClearPrompt();
                        Destroy(lookTarget);
                    }
                }
                // --- FIXED RULE 2: IT IS A LOOSE LOG RESOURCE ---
                else if (objectName.Contains("log") || objectName.Contains("wood"))
                {
                    if (inventory.CanPickupLog())
                    {
                        hintTextElement.text = $"Press [E] to pick up Wood Log ({inventory.woodLogCount}/3)";
                       
                        if (Input.GetKeyDown(KeyCode.E))
                        {
                            inventory.AddItemToHotbar(2, "Wood_Log");
                            ClearPrompt();
                            Destroy(lookTarget);
                        }
                    }
                    else
                    {
                        hintTextElement.text = "<color=red>Hands Full! Drop logs at Warehouse. Press [P] to store.</color>";
                    }
                }
            }
            else
            {
                ClearPrompt();
            }
        }
        else
        {
            ClearPrompt();
        }
    }

    void HandleWarehouseZoneInputs()
    {
        if (LogDropZone.Instance == null || !LogDropZone.Instance.isPlayerInsideZone) return;

        hintTextElement.gameObject.SetActive(true);
        hintTextElement.text = $"<b>WAREHOUSE ZONE</b>\nStored Logs: {LogDropZone.Instance.totalLogsInWarehouse}\nYour Load: ({inventory.woodLogCount}/3)\n[P] Store 1 Log | [G] Take 1 Log";

        if (Input.GetKeyDown(KeyCode.P) && inventory.woodLogCount > 0)
        {
            LogDropZone.Instance.totalLogsInWarehouse += 1;
            inventory.woodLogCount -= 1;
            
            inventory.UpdateCounterUI();
            inventory.UpdateHandItemVisuals();
        }

        if (Input.GetKeyDown(KeyCode.G) && LogDropZone.Instance.totalLogsInWarehouse > 0)
        {
            if (inventory.CanPickupLog())
            {
                LogDropZone.Instance.totalLogsInWarehouse--;
                inventory.AddItemToHotbar(2, "Wood_Log");
            }
        }
    }

    
    void HandleAbandonItemInput()
    {
        if (!Input.GetKeyDown(KeyCode.Q)) return;

        // --- SMART HORIZONTAL SPAWN CALCULATION ---
        Vector3 forwardHorizontal = cameraTransform.forward;
        forwardHorizontal.y = 0;
        forwardHorizontal.Normalize(); 

        Vector3 spawnPosition = cameraTransform.position + (forwardHorizontal * 1.8f);
        Quaternion spawnRotation = Quaternion.identity; 

        // CASE 1: Active slot is 1 (The Axe) AND you actually have it unlocked
        if (inventory.currentSlot == 1 && inventory.unlockedSlots[1])
        {
            if (axePrefab != null)
            {
                GameObject droppedAxe = Instantiate(axePrefab, spawnPosition, spawnRotation);
                droppedAxe.name = "Axe_Pickup";
                droppedAxe.tag = "Pickup"; 

                Collider existingCollider = droppedAxe.GetComponentInChildren<Collider>();
                if (existingCollider == null)
                {
                    BoxCollider newCollider = droppedAxe.AddComponent<BoxCollider>();
                    newCollider.isTrigger = false;
                }
                else
                {
                    existingCollider.isTrigger = false;
                }
                
                Rigidbody rb = droppedAxe.GetComponent<Rigidbody>();
                if (rb == null) rb = droppedAxe.AddComponent<Rigidbody>();
                
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.linearVelocity = Vector3.zero;
                rb.WakeUp();
                
                Physics.SyncTransforms();
                
                inventory.DropAxeFromInventory();
                Debug.Log("Abandoned Axe from active slot!");
                return; 
            }
        }
        
        // CASE 2: Active slot is 2 (The Logs) AND you are carrying at least 1 log
        else if (inventory.currentSlot == 2 && inventory.unlockedSlots[2] && inventory.woodLogCount > 0)
        {
            if (logPrefab != null)
            {
                GameObject droppedLog = Instantiate(logPrefab, spawnPosition, spawnRotation);
                droppedLog.name = "Wood_Log";
                droppedLog.tag = "Pickup"; 

                Collider existingCollider = droppedLog.GetComponentInChildren<Collider>();
                if (existingCollider == null)
                {
                    BoxCollider newCollider = droppedLog.AddComponent<BoxCollider>();
                    newCollider.isTrigger = false;
                }
                else
                {
                    existingCollider.isTrigger = false;
                }
                
                Rigidbody rb = droppedLog.GetComponent<Rigidbody>();
                if (rb == null) rb = droppedLog.AddComponent<Rigidbody>();
                
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.linearVelocity = Vector3.zero;
                rb.WakeUp();
                
                Physics.SyncTransforms();
                
                inventory.DropSingleLogFromInventory();
                Debug.Log("Tossed 1 Wood Log from active slot!");
                return;
            }
        }
>>>>>>> Stashed changes
    }

    void ClearPrompt()
    {
<<<<<<< Updated upstream
        if (hintTextElement != null) 
        {
            hintTextElement.gameObject.SetActive(false);
            hintTextElement.text = ""; // Wipes string value safely to prevent ghost overlap boxes
=======
        if (hintTextElement != null)
        {
            hintTextElement.text = "";
            hintTextElement.gameObject.SetActive(false);
>>>>>>> Stashed changes
        }
    }
}