using UnityEngine;

/// <summary>
/// Marks a room and the area the camera may show while the player is in it.
/// Put it on an empty GameObject. Boundaries are in WORLD positions: Bounds Min = bottom-left corner,
/// Bounds Max = top-right corner (a cyan box is drawn when the object is selected).
/// A RoomDoor can point at a Room, so the camera limits change when the player goes through.
/// </summary>
public class Room : MonoBehaviour
{
    public string roomName = "Room";
    public Vector2 boundsMin = new Vector2(-15f, -5f);
    public Vector2 boundsMax = new Vector2(15f, 5f);

    [Tooltip("Optional: an object that holds everything in this room (walls, furniture, items, doors). " +
             "When set, RoomTransition shows ONLY the room the player is in and hides the others.")]
    public GameObject content;

    /// <summary>Every active Room in the scene (no slow "find objects" call needed).</summary>
    public static readonly System.Collections.Generic.List<Room> All = new System.Collections.Generic.List<Room>();

    private void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = (boundsMin + boundsMax) * 0.5f;
        Vector3 size = boundsMax - boundsMin;
        Gizmos.DrawWireCube(center, size);
    }
}
