using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    // Index 0 = Empty hands, Index 1 = Axe, Index 2 = Wood Log
    public bool[] unlockedSlots = new bool[3]; 
    public int currentSlot = 0;

    [Header("Resource Counters")]
    public int woodLogCount = 0; // Tracks logs gathered up to 3 max

    [Header("Hand Model References")]
    public GameObject visualAxeInHand; // Drag your Player Hand's child 'axe' GameObject here
    public GameObject visualLogInHand; // Drag your Player Hand's child 'log' GameObject here

    void Start()
    {
        unlockedSlots[0] = true;  // Bare hands are always unlocked
        unlockedSlots[1] = false; // Locked until you press E on ground axe
        unlockedSlots[2] = false; // Locked until you pick up a log

        UpdateHandItemVisuals();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) ChangeActiveSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ChangeActiveSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ChangeActiveSlot(2);
    }

    public void AddItemToHotbar(int slotIndex, string itemType)
    {
        unlockedSlots[slotIndex] = true; // Permanently unlocks the slot data
        
        if (slotIndex == 2)
        {
            woodLogCount++; // Increments log inventory count
            if (woodLogCount > 3) woodLogCount = 3; // Keep clamped at capacity
        }

        Debug.Log("Inventory saved! Unlocked slot: " + slotIndex + " | Item: " + itemType);
        
        // Force the player to equip it immediately upon pickup
        ChangeActiveSlot(slotIndex);
    }

    public void ChangeActiveSlot(int index)
    {
        if (index < 0 || index >= unlockedSlots.Length) return;

        currentSlot = index;
        UpdateHandItemVisuals();
    }

    public void UpdateHandItemVisuals()
    {
        if (visualAxeInHand != null)
        {
            visualAxeInHand.SetActive(currentSlot == 1 && unlockedSlots[1]);
        }

        if (visualLogInHand != null)
        {
            visualLogInHand.SetActive(currentSlot == 2 && unlockedSlots[2] && woodLogCount > 0);
        }
    }

    public bool CanPickupLog()
    {
        return woodLogCount < 3;
    }

    public bool IsHoldingAxe() 
    { 
        return currentSlot == 1 && unlockedSlots[1]; 
    }

    public void EmptyLogsForProcessing()
    {
        woodLogCount = 0;
        unlockedSlots[2] = false;
        UpdateHandItemVisuals();
    }

    // Called when dropping the Axe via [Q]
    public void DropAxeFromInventory()
    {
        unlockedSlots[1] = false; // Lock the slot back up
        if (currentSlot == 1)
        {
            ChangeActiveSlot(0); // Safely switch back to empty hands
        }
        UpdateHandItemVisuals();
    }

    // Called when tossing 1 Log via [Q]
    public void DropSingleLogFromInventory()
    {
        if (woodLogCount > 0)
        {
            woodLogCount--;
            
            if (woodLogCount == 0)
            {
                unlockedSlots[2] = false;
                if (currentSlot == 2)
                {
                    ChangeActiveSlot(0); // Switch to empty hands
                }
            }
            
            UpdateHandItemVisuals(); 
        }
    }
}