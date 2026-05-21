using UnityEngine;

public class HotbarUI : MonoBehaviour
{
    public InventorySystem playerInventory; // Reference to our main inventory tracking component
    public RectTransform[] slotTransforms;  // Array to hold the positions of Slot1, Slot2, Slot3
    public RectTransform selectorBox;       // Reference to our moving white selection box

    void Update()
    {
        // Safety check to ensure everything is hooked up in the Unity editor inspector
        if (playerInventory != null && selectorBox != null && slotTransforms.Length > 0)
        {
            int currentActiveIndex = playerInventory.currentSlot;
            
            // Check to ensure the slot number matches our visual array limit
            if (currentActiveIndex >= 0 && currentActiveIndex < slotTransforms.Length)
            {
                // Force the white SelectorBox to copy the exact interface coordinates of our active slot
                selectorBox.anchoredPosition = slotTransforms[currentActiveIndex].anchoredPosition;
            }
        }
    }
}