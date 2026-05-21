using UnityEngine;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    public Transform cameraTransform; 
    public float interactionDistance = 4.5f;
    public TextMeshProUGUI hintTextElement; // Drag your TextMeshPro asset here
    
    private InventorySystem inventory;

    void Start()
    {
        inventory = GetComponent<InventorySystem>();
    }

    void Update()
    {
        ManageInteractionRaycast();
    }

    void ManageInteractionRaycast()
    {
        RaycastHit hit;
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
    }

    void ClearPrompt()
    {
        if (hintTextElement != null) 
        {
            hintTextElement.gameObject.SetActive(false);
            hintTextElement.text = ""; // Wipes string value safely to prevent ghost overlap boxes
        }
    }
}