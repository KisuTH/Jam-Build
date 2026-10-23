using UnityEngine;

/// <summary>
/// ไอเท็มที่เก็บได้ ใช้คู่กับ Interactable (ใส่ทั้งสองสคริปต์บนวัตถุเดียวกัน)
/// ใน Interactable -> onInteract ให้เพิ่ม ItemPickup.Pickup
/// </summary>
[RequireComponent(typeof(Interactable))]
public class ItemPickup : MonoBehaviour
{
    public string itemId = "key";
    public string itemName = "กุญแจ";
    public Sprite icon;
    [TextArea] public string pickupMessage = "ได้รับ: กุญแจ";

    public void Pickup()
    {
        if (Inventory.Instance == null) return;

        Inventory.Instance.AddItem(new InventoryItem(itemId, itemName, icon));
        DialogueUI.Instance?.Show(pickupMessage);
        gameObject.SetActive(false);
    }
}
