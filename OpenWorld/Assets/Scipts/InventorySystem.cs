using UnityEngine;
using TMPro;

public class InventorySystem : MonoBehaviour
{
    public bool[] unlockedSlots = new bool[3]; 
    public int currentSlot = 0;

    [Header("Resource Counters")]
    public int woodLogCount = 0; 
    public TextMeshProUGUI logCounterUIElement; 

    [Header("Hand Model References")]
    public GameObject visualAxeInHand; 
    public GameObject visualLogInHand; // Set this to Wood_LogVisual in the Inspector

    void Start()
    {
        unlockedSlots[0] = true;  
        unlockedSlots[1] = false; 
        unlockedSlots[2] = false; 

        UpdateHandItemVisuals();
        UpdateLogCounterUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) ChangeActiveSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ChangeActiveSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ChangeActiveSlot(2);
    }

    public void AddItemToHotbar(int slotIndex, string itemType)
    {
        unlockedSlots[slotIndex] = true; 
        
        if (itemType == "Log" || slotIndex == 2)
        {
            woodLogCount++; 
            if (woodLogCount > 3) woodLogCount = 3; 
        }

        ChangeActiveSlot(slotIndex);
        UpdateLogCounterUI();
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
            // Only displays log POV when slot 3 is selected, slot is unlocked, and log count > 0
            visualLogInHand.SetActive(currentSlot == 2 && unlockedSlots[2] && woodLogCount > 0);
        }
    }

    public bool CanPickupLog() => woodLogCount < 3;
    
    public bool IsHoldingAxe() => currentSlot == 1 && unlockedSlots[1]; 

    public void EmptyLogsForProcessing()
    {
        woodLogCount = 0;
        unlockedSlots[2] = false;
        
        if (currentSlot == 2) ChangeActiveSlot(0); 
        
        UpdateHandItemVisuals();
        UpdateLogCounterUI();
    }

    public void DropAxeFromInventory()
    {
        unlockedSlots[1] = false; 
        if (currentSlot == 1) ChangeActiveSlot(0); 
        UpdateHandItemVisuals();
    }

    public void DropSingleLogFromInventory()
    {
        if (woodLogCount > 0)
        {
            woodLogCount--;
            if (woodLogCount == 0)
            {
                unlockedSlots[2] = false;
                if (currentSlot == 2) ChangeActiveSlot(0); 
            }
            UpdateHandItemVisuals(); 
            UpdateLogCounterUI();
        }
    }

    public void UpdateLogCounterUI()
    {
        if (logCounterUIElement != null)
        {
            // Outputs a simple single digit count representation (e.g., "0", "1", "2")
            logCounterUIElement.text = woodLogCount.ToString();
        }
    }
}