using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Anything the player can click and inspect: windows, tables, pictures, doors, items.
/// Needs a Collider2D. Set what happens in the "On Interact" event in the Inspector.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Interactable2D : MonoBehaviour
{
    [Header("Info")]
    public string displayName = "Object";
    [TextArea] public string description = "";

    [Header("Range")]
    [Tooltip("Max distance between the player and this object. 0 = no limit.")]
    public float maxDistance = 2.5f;
    [TextArea] public string tooFarMessage = "ไกลเกินไป...";

    [Header("Requirement (optional)")]
    [Tooltip("Item id needed to use this (e.g. 'old_key'). Leave empty if nothing is needed.")]
    public string requiredItemId = "";
    public bool consumeRequiredItem = true;
    [TextArea] public string missingItemMessage = "ใช้ไม่ได้... ต้องมีอะไรบางอย่างก่อน";

    [Header("Events")]
    public UnityEvent onInteract = new UnityEvent();

    private Collider2D col;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    public void Interact(Transform player)
    {
        if (maxDistance > 0f && player != null)
        {
            Vector2 closest = col.ClosestPoint(player.position);
            if (Vector2.Distance(player.position, closest) > maxDistance)
            {
                Say(tooFarMessage);
                return;
            }
        }

        if (!string.IsNullOrEmpty(requiredItemId))
        {
            Inventory inv = Inventory.Instance;
            if (inv == null || !inv.HasItem(requiredItemId))
            {
                Say(missingItemMessage);
                return;
            }
            if (consumeRequiredItem) inv.RemoveItem(requiredItemId);
        }

        onInteract?.Invoke();
    }

    // ===== Ready-made functions to hook into "On Interact" in the Inspector =====

    public void SayDescription()
    {
        Say(description);
    }

    public void Say(string text)
    {
        if (DialogueUI.Instance != null) DialogueUI.Instance.Show(text);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
