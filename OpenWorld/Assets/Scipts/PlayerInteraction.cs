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

        // Shoot a ray out from the center of the player camera forward

        RaycastHit hit;

        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, interactionDistance))

        {

            string objectName = hit.collider.gameObject.name;



            // --- RULE 1: LOOKING AT THE AXE PICKUP ---

            if (objectName.Contains("Axe"))

            {

                hintTextElement.text = "Press [E] to pick up the Axe";



                if (Input.GetKeyDown(KeyCode.E))

                {

                    inventory.AddItemToHotbar(1, "Axe");

                    ClearPrompt();

                    Destroy(hit.collider.gameObject);

                }

            }

            // --- RULE 2: LOOKING AT A WOOD LOG (WITH 3-LOG CARRY LIMIT) ---

            else if (objectName.Contains("Log") || objectName.Contains("Wood"))

            {

                // Talk to the inventory system to check if we have space left

                if (inventory.CanPickupLog())

                {

                    // Shows current count, like: "Press [E] to pick up Wood Log (1/3)"

                    hintTextElement.text = $"Press [E] to pick up Wood Log ({inventory.woodLogCount}/3)";

                   

                    if (Input.GetKeyDown(KeyCode.E))

                    {

                        inventory.AddItemToHotbar(2, "Wood_Log");

                        ClearPrompt();

                        Destroy(hit.collider.gameObject);

                    }

                }

                else

                {

                    // Hands are full! Blocks input and displays a red warning text

                    hintTextElement.text = "<color=red>Hands Full! Drop logs at the House Factory</color>";

                }

            }

            // If raycast hits an object but it isn't something we can pick up, clear the UI text

            else

            {

                ClearPrompt();

            }

        }

        else

        {

            // If raycast misses completely into the empty sky, clear the UI text

            ClearPrompt();

        }

    }

    void ClearPrompt()

    {

        if (hintTextElement != null)

        {

            hintTextElement.text = "";

        }

    }

}  

