using UnityEngine;

public class PickupItem : MonoBehaviour
{
    public enum ItemType { Axe, Log }
    [Header("Item Configuration")]
    public ItemType itemType;
}