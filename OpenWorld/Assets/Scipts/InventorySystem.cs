using UnityEngine;
using TMPro;

public class InventorySystem : MonoBehaviour
{
    public bool[] unlockedSlots = new bool[3]; 
    public int currentSlot = 0;

    [Header("Item Collection Counters")]
    public int woodLogCount = 0;
    public const int MAX_LOG_CAPACITY = 3; 

    [Header("First-Person Visual Models")]
    public GameObject visualAxeInHand; 
    public GameObject visualLogInHand; 

    [Header("UI Display Connections")]
    public TextMeshProUGUI logTextUI; // Drag 'WoodInventoryDisplay' text object here!

    void Start()
    {
        unlockedSlots[0] = true;  
        unlockedSlots[1] = false; 
        unlockedSlots[2] = false; 

        UpdateCounterUI();
        UpdateHandItemVisuals();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) ChangeActiveSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ChangeActiveSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ChangeActiveSlot(2);
    }

    public bool CanPickupLog()
    {
        return woodLogCount < MAX_LOG_CAPACITY;
    }

    public void AddItemToHotbar(int slotIndex, string itemName)
    {
        unlockedSlots[slotIndex] = true;
        
        if (itemName == "Wood_Log")
        {
            if (woodLogCount >= MAX_LOG_CAPACITY) return; 
            woodLogCount++;
        }

        UpdateCounterUI();
        ChangeActiveSlot(slotIndex); 
    }

    void ChangeActiveSlot(int index)
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

    public void UpdateCounterUI()
    {
        if (logTextUI != null) 
        {
            // CRITICAL FIX: Forces the UI slot text box to instantly display the number digit, clearing old text strings
            logTextUI.text = woodLogCount > 0 ? woodLogCount.ToString() : "";
        }
    }

    public void EmptyLogsForProcessing()
    {
        woodLogCount = 0;
        UpdateCounterUI();
        UpdateHandItemVisuals();
    }

    public bool IsHoldingAxe() { return currentSlot == 1 && unlockedSlots[1]; }
}