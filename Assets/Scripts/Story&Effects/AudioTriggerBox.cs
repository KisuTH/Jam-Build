using UnityEngine;

/// <summary>
/// An invisible box. When the Player walks into it, an audio clip plays.
/// It can fire only ONCE per loop: after the player dies the scene restarts and it is ready again.
///
/// Add it to an empty object (or use Tools > Rain > Add Audio Trigger Box, which makes one in the middle of the Scene view),
/// then drag your sound onto "Clip". Move / resize the box with its Box Collider 2D (the green frame in the Scene view).
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class AudioTriggerBox : MonoBehaviour
{
    public AudioClip clip;
    [Range(0f, 4f)]
    [Tooltip("1 = normal. 2 = twice as loud (plays 2 copies on top of each other), 3 = x3 ... up to 4.")]
    public float volume = 1f;

    [Header("Conditions (all must be true, leave empty = always)")]
    [Tooltip("Story flags that must already be on, e.g. got_register.")]
    public string[] requiredFlags = new string[0];
    [Tooltip("Item ids that must be in the Player's inventory.")]
    public string[] requiredItems = new string[0];
    [Tooltip("Ticked = it plays only the first time the player touches it in this loop. Unticked = every time the player enters.")]
    public bool onlyOncePerLoop = true;

    private bool played;

    // makes the collider a trigger as soon as the component is added
    private void Reset()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(2f, 6f);
    }

    private void OnTriggerEnter2D(Collider2D other) { TryPlay(other); }

    // standing inside the box when the condition becomes true also counts (only for "once per loop")
    private void OnTriggerStay2D(Collider2D other) { if (onlyOncePerLoop) TryPlay(other); }

    private void TryPlay(Collider2D other)
    {
        if (onlyOncePerLoop && played) return;
        if (!IsPlayer(other)) return;
        if (!ConditionsMet()) return;
        played = true;
        PlayLoud();
    }

    private bool ConditionsMet()
    {
        foreach (string f in requiredFlags)
        {
            if (string.IsNullOrEmpty(f)) continue;
            if (StoryFlags.Instance == null || !StoryFlags.Instance.Has(f)) return false;
        }
        foreach (string id in requiredItems)
        {
            if (string.IsNullOrEmpty(id)) continue;
            if (Inventory.Instance == null || !Inventory.Instance.HasItem(id)) return false;
        }
        return true;
    }

    private void PlayLoud()
    {
        float v = Mathf.Max(0f, volume);
        while (v > 0.001f)
        {
            GameSfx.Play(clip, Mathf.Min(1f, v));
            v -= 1f;
        }
    }

    private static bool IsPlayer(Collider2D c)
    {
        if (c.CompareTag("Player")) return true;
        if (c.attachedRigidbody != null && c.attachedRigidbody.CompareTag("Player")) return true;
        return c.GetComponentInParent<PlayerMovement2D>() != null;
    }

    private void OnDrawGizmos()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null) return;
        Gizmos.color = played ? new Color(0.6f, 0.6f, 0.6f, 0.9f) : new Color(0.2f, 1f, 0.4f, 0.9f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.offset, box.size);
    }
}
