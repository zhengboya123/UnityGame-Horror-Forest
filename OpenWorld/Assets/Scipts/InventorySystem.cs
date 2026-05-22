using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    // Index 0 = Empty, Index 1 = Axe, Index 2 = Wood Log
    public bool[] unlockedSlots = new bool[3]; 
    public int currentSlot = 0;

    // Drag your HandContainer's child 'axe' GameObject here
    public GameObject visualAxeInHand; 

    void Start()
    {
        unlockedSlots[0] = true;  // Bare hands are always unlocked
        unlockedSlots[1] = false; // Locked until you press E on ground axe
        unlockedSlots[2] = false; // Locked until you chop a tree

        ForceHandUpdate();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchToSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchToSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchToSlot(2);
    }

    // THIS IS THE EXACT INTERACTION METHOD CALLED BY THE PICKUP SCRIPT
    public void AddItemToHotbar(int slotIndex, string itemType)
    {
        unlockedSlots[slotIndex] = true; // Permanently unlocks the slot data
        Debug.Log("Inventory saved! Unlocked slot: " + slotIndex);
        
        // Force the player to equip it immediately upon pickup
        SwitchToSlot(slotIndex);
    }

    void SwitchToSlot(int index)
    {
        if (index < 0 || index >= unlockedSlots.Length) return;

        currentSlot = index;
        ForceHandUpdate();
    }

    public void ForceHandUpdate()
    {
        if (visualAxeInHand == null) return;

        // Strictly check if we are on slot 2 AND we actually picked up the axe
        if (currentSlot == 1 && unlockedSlots[1] == true)
        {
            visualAxeInHand.SetActive(true);
        }
        else
        {
            // Turn it off if we switch to slot 1 or 3, or if we don't own it yet
            visualAxeInHand.SetActive(false);
        }
    }

    public bool IsHoldingAxe()
    {
        return currentSlot == 1 && unlockedSlots[1] == true;
    }
<<<<<<< Updated upstream
=======

    public void EmptyLogsForProcessing()
    {
        woodLogCount = 0;
        UpdateCounterUI();
        UpdateHandItemVisuals();
    }

    public bool IsHoldingAxe() { return currentSlot == 1 && unlockedSlots[1]; }

    // Called when dropping the Axe
    public void DropAxeFromInventory()
    {
        unlockedSlots[1] = false; // Lock the slot back up
        if (currentSlot == 1)
        {
            ChangeActiveSlot(0); // Safely switch back to empty hands (Slot 1)
        }
        UpdateHandItemVisuals();
    }

    // Called when tossing 1 Log
    public void DropSingleLogFromInventory()
    {
        if (woodLogCount > 0)
        {
            woodLogCount--;
            
            // If they threw their very last log, lock the slot and clear hands
            if (woodLogCount == 0)
            {
                unlockedSlots[2] = false;
                if (currentSlot == 2)
                {
                    ChangeActiveSlot(0); // Switch to empty hands
                }
            }
            
            UpdateCounterUI();
            UpdateHandItemVisuals(); // Keeps showing the log model if count > 0!
        }
    }
>>>>>>> Stashed changes
}