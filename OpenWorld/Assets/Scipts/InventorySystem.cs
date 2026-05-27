using UnityEngine;
using TMPro;

public class InventorySystem : MonoBehaviour
{
    public bool[] unlockedSlots = new bool[4]; 
    public int currentSlot = 0;

    [Header("Resource Counters")]
    public int woodLogCount = 0; 
    public TextMeshProUGUI logCounterUIElement; 

    [Header("Gas Bottle Persistent Fuel Logic")]
    public float gasBottleFuelCapacity = 0f; 
    public TextMeshProUGUI gasBottleFuelUIElement; 

    [Header("Hand Model References")]
    public GameObject visualAxeInHand; 
    public GameObject visualLogInHand; 
    public GameObject visualGasInHand; 

    void Start()
    {
        unlockedSlots[0] = true;  
        unlockedSlots[1] = false; 
        unlockedSlots[2] = false; 
        unlockedSlots[3] = false; 

        UpdateHandItemVisuals();
        UpdateLogCounterUI();
        UpdateGasFuelUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) ChangeActiveSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ChangeActiveSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ChangeActiveSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) ChangeActiveSlot(3); 

        UpdateGasFuelUI();
    }

    // OPTIMIZED METHOD: Now accepts an optional initialFuel parameter (defaults to 10f if picked up elsewhere)
    public void AddItemToHotbar(int slotIndex, string itemType, float initialFuel = 10f)
    {
        if (slotIndex >= unlockedSlots.Length) return;
        unlockedSlots[slotIndex] = true; 
        
        if (itemType == "Log" && slotIndex == 2)
        {
            woodLogCount++; 
            if (woodLogCount > 3) woodLogCount = 3; 
        }

        if (itemType == "GasBottle" && slotIndex == 3)
        {
            // Sync with the custom fuel value passed from the ground object
            gasBottleFuelCapacity = initialFuel;
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
        if (visualAxeInHand != null) visualAxeInHand.SetActive(currentSlot == 1 && unlockedSlots[1]);
        if (visualLogInHand != null) visualLogInHand.SetActive(currentSlot == 2 && unlockedSlots[2] && woodLogCount > 0);
        if (visualGasInHand != null) visualGasInHand.SetActive(currentSlot == 3 && unlockedSlots[3]);
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

    public void EmptyGasCan()
    {
        gasBottleFuelCapacity = 0f;
        unlockedSlots[3] = false;
        if (currentSlot == 3) ChangeActiveSlot(0);
        UpdateHandItemVisuals();
        UpdateGasFuelUI();
    }

    public void DropGasCanFromInventory()
    {
        unlockedSlots[3] = false;
        if (currentSlot == 3) ChangeActiveSlot(0);
        UpdateHandItemVisuals();
        UpdateGasFuelUI();
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
        if (logCounterUIElement != null) logCounterUIElement.text = woodLogCount.ToString();
    }

    public void UpdateGasFuelUI()
    {
        if (gasBottleFuelUIElement != null)
        {
            if (unlockedSlots[3])
            {
                float percentage = (gasBottleFuelCapacity / 10f) * 100f;
                gasBottleFuelUIElement.text = $"{Mathf.CeilToInt(percentage)}%";
            }
            else
            {
                gasBottleFuelUIElement.text = "0%";
            }
        }
    }
}