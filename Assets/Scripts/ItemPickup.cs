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
    [TextArea] public string pickupMessage = "Picked up an item";

    public void Pickup()
    {
        if (Inventory.Instance == null) return;

        Inventory.Instance.AddItem(new InventoryItem(itemId, itemName, itemDescription, icon));

        // Log it in the Case Book and remember that the player has found it
        if (CaseNotebook.Instance != null) CaseNotebook.Instance.AddClue("item_" + itemId, itemName, itemDescription);
        if (StoryFlags.Instance != null) StoryFlags.Instance.Set("got_" + itemId);

        if (DialogueUI.Instance != null) DialogueUI.Instance.Show(pickupMessage);
        gameObject.SetActive(false);
    }
}
