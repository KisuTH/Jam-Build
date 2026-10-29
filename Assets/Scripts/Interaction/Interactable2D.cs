using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Anything the player can click and inspect: windows, tables, pictures, doors, items.
/// Needs a Collider2D. Set what happens in the "On Interact" event in the Inspector.
///
/// Order of checks when the player clicks: distance -> required item -> required story flags.
/// If all pass: the clue is logged (if any), the flag is set (if any), then "On Interact" runs.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Interactable2D : MonoBehaviour
{
    [Header("Info")]
    public string displayName = "Object";
    [TextArea] public string description = "";
    [TextArea] [Tooltip("Thai version of the description (used when the language is Thai). Empty = English.")]
    public string descriptionThai = "";

    [Header("Range")]
    [Tooltip("Max distance between the player and this object. 0 = no limit.")]
    public float maxDistance = 2.5f;

    /// <summary>Everything can be reached this many times further than its Max Distance.</summary>
    public static float RangeMultiplier = 1.8f;
    [TextArea] public string tooFarMessage = "Too far away...";

    [Header("Requirement: item (optional)")]
    [Tooltip("Item id needed to use this (e.g. 'old_key'). Leave empty if nothing is needed.")]
    public string requiredItemId = "";
    public bool consumeRequiredItem = true;
    [TextArea] public string missingItemMessage = "I can't use this yet... I need something first.";

    [Header("Requirement: story flags (optional)")]
    [Tooltip("Story flags that must ALL be on before this works, e.g. got_register. Leave empty for none.")]
    public string[] requiredFlags = new string[0];
    [TextArea] public string missingFlagsMessage = "I still don't understand enough to do this...";

    [Header("Clue (optional)")]
    [Tooltip("If filled in, inspecting this object adds a page to the Case Book and turns on a story flag with this name. Leave empty for no clue.")]
    public string clueId = "";
    [Tooltip("Title of the Case Book page. Empty = use Display Name.")]
    public string clueTitle = "";
    [TextArea] [Tooltip("Text of the Case Book page. Empty = use Description.")]
    public string clueText = "";

    [Header("Result (optional)")]
    [Tooltip("Turns this story flag on when the interaction works.")]
    public string setsFlag = "";

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
            // distance from the player's BODY (its collider) to this object, not just from the feet,
            // and a more generous reach (RangeMultiplier) so the player is not "paralysed"
            float dist;
            Collider2D pc = player.GetComponentInChildren<Collider2D>();
            if (pc != null && pc.enabled && !pc.isTrigger)
                dist = Mathf.Max(0f, col.Distance(pc).distance);
            else
                dist = Vector2.Distance(player.position, col.ClosestPoint(player.position));
            if (dist > maxDistance * RangeMultiplier)
            {
                Say(tooFarMessage);
                return;
            }
        }

        // Check everything first, so nothing is used up if something is still missing
        bool needsItem = !string.IsNullOrEmpty(requiredItemId);
        if (needsItem && (Inventory.Instance == null || !Inventory.Instance.HasItem(requiredItemId)))
        {
            Say(missingItemMessage);
            return;
        }

        if (!HasAllRequiredFlags())
        {
            Say(missingFlagsMessage);
            return;
        }

        bool unlockedNow = false;
        if (needsItem && consumeRequiredItem)
        {
            Inventory.Instance.RemoveItem(requiredItemId);
            requiredItemId = "";   // unlocked for good: the key is used up, so the door must not ask for it again
            unlockedNow = true;
        }

        LogClue();
        if (!string.IsNullOrEmpty(setsFlag) && StoryFlags.Instance != null)
            StoryFlags.Instance.Set(setsFlag);

        // A door: the first click only uses the key to unlock it, the next click walks in
        if (unlockedNow && GetComponent<RoomDoor>() != null)
        {
            Say(Loc.Thai ? Loc.Shape("ใช้กุญแจไขประตูแล้ว") : "Unlocked the door with the key.");
            return;
        }

        onInteract?.Invoke();
    }

    private bool HasAllRequiredFlags()
    {
        if (requiredFlags == null) return true;

        foreach (string flag in requiredFlags)
        {
            if (string.IsNullOrEmpty(flag)) continue;
            if (StoryFlags.Instance == null || !StoryFlags.Instance.Has(flag)) return false;
        }
        return true;
    }

    // Adds this object's clue to the Case Book (if it has one) and turns on its story flag
    private void LogClue()
    {
        if (string.IsNullOrEmpty(clueId)) return;

        string title = string.IsNullOrEmpty(clueTitle) ? displayName : clueTitle;
        string text = string.IsNullOrEmpty(clueText) ? description : clueText;

        if (CaseNotebook.Instance != null) CaseNotebook.Instance.AddClue(clueId, title, text);
        if (StoryFlags.Instance != null) StoryFlags.Instance.Set(clueId);
    }

    // ===== Ready-made functions to hook into "On Interact" in the Inspector =====

    public void SayDescription()
    {
        Say(Loc.Thai && !string.IsNullOrEmpty(descriptionThai) ? descriptionThai : description);
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
