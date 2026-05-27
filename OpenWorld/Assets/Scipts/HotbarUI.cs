using UnityEngine;

public class HotbarUI : MonoBehaviour
{
    public InventorySystem playerInventory; // Reference to our main inventory tracking component
    public RectTransform[] slotTransforms;  // Holds positions for Slot1, Slot2, Slot3, Slot4
    public RectTransform selectorBox;       // Reference to our moving white selection box

    void Update()
    {
        // Safety check to ensure everything is hooked up in the Unity editor inspector
        if (playerInventory != null && selectorBox != null && slotTransforms.Length > 0)
        {
            int currentActiveIndex = playerInventory.currentSlot;
            
            // FIXED: Dynamically opens the limit boundary to read any slot up to your full array length!
            if (currentActiveIndex >= 0 && currentActiveIndex < slotTransforms.Length)
            {
                // Force the white SelectorBox to jump right to the active slot's visual position
                selectorBox.anchoredPosition = slotTransforms[currentActiveIndex].anchoredPosition;
            }
        }
    }
}