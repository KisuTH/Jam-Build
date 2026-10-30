using System.Collections;
using UnityEngine;

/// <summary>
/// A quick jump scare: the first time the Player opens the door and enters "Room", a ghost appears on the
/// RIGHT of the Player and runs LEFT (or the other way: see Direction), straight past them, then disappears.
/// Add it with Tools > Rain > Add Ghost Run-By, then drag your sound onto "Sound".
/// Plays once per loop (the scene restarts when the player dies).
/// </summary>
public class GhostRunBy : MonoBehaviour
{
    [Header("When")]
    [Tooltip("The room the Player enters (the Room object of that room).")]
    public Room room;
    [Tooltip("Ticked = ignore 'Room': the ghost runs the first time the player arrives through ANY door.")]
    public bool anyRoom = false;
    [Tooltip("Wait this long after the player has arrived in the room.")]
    public float delayAfterEntering = 0.3f;

    [Header("Ghost")]
    [Tooltip("The ghost that runs by (a copy of the chase ghost; the menu fills this in).")]
    public GameObject ghost;
    public enum RunDirection { RightToLeft, LeftToRight }
    [Tooltip("Which way the ghost runs across the screen.")]
    public RunDirection direction = RunDirection.RightToLeft;
    [Tooltip("Ticked = the ghost picture looks LEFT by default. Unticked = it looks RIGHT (it is flipped to face left while running).")]
    public bool spriteFacesLeft = true;
    [Tooltip("Units per second. Higher = faster. Only used when 'Cross Time' is 0.")]
    public float speed = 22f;
    [Tooltip("With 'Use Screen Edges': how many SECONDS the ghost takes to cross the whole screen. Smaller = faster (0.4 = very fast). 0 = use Speed instead.")]
    public float crossTime = 0.4f;
    [Tooltip("Ticked = the ghost starts just outside the RIGHT edge of the screen and runs to just outside the LEFT edge. Unticked = uses Start / End Distance from the Player.")]
    public bool useScreenEdges = true;
    [Tooltip("How far to the right of the Player the ghost starts.")]
    public float startDistance = 14f;
    [Tooltip("How far to the left of the Player the ghost ends before it disappears.")]
    public float endDistance = 14f;
    [Tooltip("Raise / lower the ghost's feet.")]
    public float feetOffset = 0f;

    [Header("Conditions (all must be true, leave empty = always)")]
    public string[] requiredFlags = new string[0];
    public string[] requiredItems = new string[0];

    [Header("Effects")]
    public AudioClip sound;
    [Range(0f, 4f)] public float soundVolume = 1f;
    public float shakeTime = 0.3f;
    public float shakeStrength = 0.15f;

    [Tooltip("Ticked = only the first time the player enters the room in this loop.")]
    public bool onlyOncePerLoop = true;

    private bool played;
    private bool running;
    private bool wasBusy;

    private void Awake()
    {
        if (ghost != null) ghost.SetActive(false);
    }

    private void Update()
    {
        RoomTransition rt = RoomTransition.Instance;
        if (rt == null) return;

        // an "entry" = a door transition has just finished (the player arrived in a room)
        bool busyNow = rt.IsBusy;
        bool entered = wasBusy && !busyNow;
        wasBusy = busyNow;
        if (!entered) return;

        if (running || ghost == null || (room == null && !anyRoom)) return;
        if (onlyOncePerLoop && played) return;
        Room now = rt.CurrentRoom;
        Debug.Log("Ghost Run-By: the player entered room '" + (now != null ? now.name : "none") +
                  "'. Run-By waits for '" + (anyRoom ? "any room" : room.name) + "'.", this);
        if (now == null) return;
        if (!anyRoom && now != room && !PlayerInside(room)) return;
        if (!ConditionsMet()) return;   // not ready yet: the next time the player enters a room it is checked again
        GameObject p = GameObject.FindWithTag("Player");
        if (p == null) return;
        played = true;
        StartCoroutine(Run(p.transform));
    }

    private static bool PlayerInside(Room r)
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p == null || r == null) return false;
        Vector3 v = p.transform.position;
        return v.x >= r.boundsMin.x && v.x <= r.boundsMax.x && v.y >= r.boundsMin.y && v.y <= r.boundsMax.y;
    }

    private IEnumerator Run(Transform player)
    {
        running = true;
        if (delayAfterEntering > 0f) yield return new WaitForSeconds(delayAfterEntering);
        float px = player.position.x;
        float dir = direction == RunDirection.RightToLeft ? -1f : 1f;   // -1 = moves left, +1 = moves right
        float startX = px - dir * startDistance;
        float endX = px + dir * endDistance;

        Collider2D pc = player.GetComponentInChildren<Collider2D>();
        float groundY = (pc != null ? pc.bounds.min.y : player.position.y) + feetOffset;

        ghost.transform.position = new Vector3(startX, player.position.y, 0f);
        ghost.SetActive(true);
        float halfGhost = 0f;
        foreach (SpriteRenderer r in ghost.GetComponentsInChildren<SpriteRenderer>(true))
            halfGhost = Mathf.Max(halfGhost, r.bounds.extents.x);
        Camera cam = Camera.main;
        if (useScreenEdges && cam != null && cam.orthographic)
        {
            float halfW = cam.orthographicSize * cam.aspect;
            startX = cam.transform.position.x - dir * (halfW + halfGhost);
            endX = cam.transform.position.x + dir * (halfW + halfGhost);
            ghost.transform.position = new Vector3(startX, ghost.transform.position.y, 0f);
        }
        foreach (SpriteRenderer sr in ghost.GetComponentsInChildren<SpriteRenderer>(true))
            sr.flipX = dir < 0f ? !spriteFacesLeft : spriteFacesLeft;
        SpriteRenderer main = ghost.GetComponentInChildren<SpriteRenderer>(true);
        if (main != null)
        {
            Vector3 p = ghost.transform.position;
            p.y += groundY - main.bounds.min.y;
            ghost.transform.position = p;
        }

        float runSpeed = speed;
        if (useScreenEdges && crossTime > 0f) runSpeed = Mathf.Abs(startX - endX) / crossTime;

        PlayLoud();
        if (shakeTime > 0f) CameraShake2D.Shake(shakeTime, shakeStrength);

        while ((endX - ghost.transform.position.x) * dir > 0f)
        {
            Vector3 p = ghost.transform.position;
            p.x += dir * runSpeed * Time.deltaTime;
            ghost.transform.position = p;
            yield return null;
        }

        ghost.SetActive(false);
        running = false;
    }

    private void PlayLoud()
    {
        if (sound == null) return;
        float v = Mathf.Max(0f, soundVolume);
        while (v > 0.001f) { GameSfx.Play(sound, Mathf.Min(1f, v)); v -= 1f; }
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
}
