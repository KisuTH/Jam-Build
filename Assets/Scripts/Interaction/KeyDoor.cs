using UnityEngine;

/// <summary>
/// A door that needs a key. Put it on a door that already has Interactable2D + RoomDoor, and set the Interactable2D's
/// "On Interact" to KeyDoor.Interact (instead of RoomDoor.Enter). The builder does this.
///   1st click with the key : "Used the key" and the door unlocks (the player does not walk in yet).
///   next click             : the player walks through the door as usual.
///   click without the key  : "The door is locked".
/// The key stays in the bag (so the door can be used again).
/// </summary>
[RequireComponent(typeof(RoomDoor))]
public class KeyDoor : MonoBehaviour
{
    public string doorId = "door";
    public string itemId = "key";
    [TextArea] public string lockedEnglish = "The door is locked. I need a key.";
    [TextArea] public string lockedThai = "ประตูล็อกอยู่ ต้องมีกุญแจ";

    [Tooltip("Remove the key from the bag when the door unlocks.")]
    public bool consumeKey = false;

    public string UnlockedFlag { get { return "unlocked_" + doorId; } }

    public bool IsUnlocked { get { return StoryFlags.Instance != null && StoryFlags.Instance.Has(UnlockedFlag); } }
    public bool HasKey { get { return Inventory.Instance != null && Inventory.Instance.HasItem(itemId); } }

    public void Interact()
    {
        RoomDoor door = GetComponent<RoomDoor>();

        if (IsUnlocked)
        {
            door.Enter();
            return;
        }

        if (!HasKey)
        {
            Say(Loc.Thai ? (string.IsNullOrEmpty(lockedThai) ? Loc.T(lockedEnglish) : lockedThai) : lockedEnglish);
            return;
        }

        string keyName = itemId;
        foreach (InventoryItem it in Inventory.Instance.Items)
            if (it.id == itemId) { keyName = it.displayName; break; }

        if (StoryFlags.Instance != null) StoryFlags.Instance.Set(UnlockedFlag);
        if (consumeKey) Inventory.Instance.RemoveItem(itemId);
        Say(Loc.Thai ? "ใช้" + Loc.T(keyName) + " แล้ว ประตูปลดล็อก" : "Used the " + keyName + ". The door unlocks.");
    }

    private void Say(string text)
    {
        if (DialogueUI.Instance != null) DialogueUI.Instance.Show(Loc.Thai ? Loc.Shape(text) : text);
    }
}
