using UnityEngine;

/// <summary>
/// An item the player can collect (old key, photo, clue...).
/// Put it on the same object as Interactable2D, then in Interactable2D's
/// "On Interact" add ItemPickup.Pickup.
/// </summary>
[RequireComponent(typeof(Interactable2D))]
public class ItemPickup : MonoBehaviour
{
    public string itemId = "old_key";
    public string itemName = "กุญแจเก่า";
    [TextArea] public string itemDescription = "กุญแจเก่าสนิมเขรอะ";
    public Sprite icon;
    [TextArea] public string pickupMessage = "ได้รับ: กุญแจเก่า";

    public void Pickup()
    {
        if (Inventory.Instance == null) return;

        Inventory.Instance.AddItem(new InventoryItem(itemId, itemName, itemDescription, icon));

        if (DialogueUI.Instance != null) DialogueUI.Instance.Show(pickupMessage);
        gameObject.SetActive(false);
    }
}
