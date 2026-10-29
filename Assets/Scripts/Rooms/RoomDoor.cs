using UnityEngine;

/// <summary>
/// A door that takes the player to another place with a fade: the player walks into the door,
/// the screen fades to black, then fades back in at the other side.
///
/// Put it on a door object that also has an Interactable2D (and a Collider2D). In Interactable2D's
/// "On Interact" add  RoomDoor.Enter.  Locks work as usual (Required Item / Required Flags on the
/// Interactable2D): if the player cannot open the door, nothing happens.
///
/// Make each door a PAIR: one door per side, each pointing at the other side's arrive point.
/// </summary>
public class RoomDoor : MonoBehaviour
{
    [Tooltip("Where the player appears on the other side (an empty object placed in front of the other door).")]
    public Transform arrivePoint;

    [Tooltip("The room the player ends up in. Sets the camera limits. Optional.")]
    public Room arriveRoom;

    [Tooltip("After arriving, the player walks a little away from the door. +1 = to the right, -1 = to the left.")]
    public float stepOutDirection = 1f;
    public float stepOutDistance = 0.9f;

    [TextArea] [Tooltip("A message shown the FIRST time this door is used. Optional.")]
    public string firstTimeMessage = "";

    [Header("Doorway look (depth walk)")]
    [Tooltip("How far the player walks 'into the background' when entering / out of the foreground when leaving (world units, up on screen).")]
    public float depthDistance = 0.45f;
    [Tooltip("Player size when fully inside the doorway (1 = no shrink).")]
    [Range(0.5f, 1f)] public float depthScale = 0.8f;

    [System.NonSerialized] public bool used;

    /// <summary>Hook this into Interactable2D's "On Interact".</summary>
    public void Enter()
    {
        if (RoomTransition.Instance == null)
        {
            Debug.LogWarning("RoomDoor: there is no RoomTransition in the scene (put one on GameSystems).", this);
            return;
        }
        RoomTransition.Instance.Go(this);
    }
}
