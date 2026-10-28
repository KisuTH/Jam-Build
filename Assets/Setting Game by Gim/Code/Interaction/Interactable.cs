using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// วัตถุที่ผู้เล่นคลิกเพื่อโต้ตอบได้ (ประตู, ไอเท็ม, NPC ฯลฯ)
/// ตั้งค่าผลลัพธ์ผ่าน UnityEvent ใน Inspector ได้เลย
/// </summary>
public class Interactable : MonoBehaviour
{
    [Header("Info")]
    public string displayName = "วัตถุ";
    [TextArea] public string description = "";

    [Header("Position")]
    [Tooltip("จุดที่ผู้เล่นต้องเดินไปยืน (ถ้าว่างจะใช้ตำแหน่งของวัตถุ)")]
    [SerializeField] private Transform interactPoint;

    [Header("Requirement (optional)")]
    [Tooltip("ไอเท็มที่ต้องมีถึงจะใช้ได้ เช่น กุญแจ (เว้นว่างถ้าไม่ต้องใช้)")]
    [SerializeField] private string requiredItemId = "";
    [SerializeField] private bool consumeRequiredItem = true;
    [TextArea] [SerializeField] private string missingItemMessage = "ใช้ไม่ได้... ต้องมีอะไรบางอย่างก่อน";

    [Header("Events")]
    public UnityEvent<PointAndClickPlayer> onInteract;

    public Vector3 GetInteractPosition()
    {
        return interactPoint != null ? interactPoint.position : transform.position;
    }

    public virtual void Interact(PointAndClickPlayer player)
    {
        if (!string.IsNullOrEmpty(requiredItemId))
        {
            var inv = Inventory.Instance;
            if (inv == null || !inv.HasItem(requiredItemId))
            {
                DialogueUI.Instance?.Show(missingItemMessage);
                return;
            }
            if (consumeRequiredItem) inv.RemoveItem(requiredItemId);
        }

        onInteract?.Invoke(player);
    }

    // ===== ฟังก์ชันสำเร็จรูปสำหรับต่อกับ UnityEvent ใน Inspector =====

    public void SayDescription()
    {
        DialogueUI.Instance?.Show(description);
    }

    public void Say(string text)
    {
        DialogueUI.Instance?.Show(text);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
