using System.Collections;
using UnityEngine;

/// <summary>
/// Runs the "go through a door" scene change inside one scene:
///   1. the player walks up to the door by itself (keys locked)
///   2. the player steps into the door (fades out)
///   3. the screen fades to black
///   4. the player is moved to the other side, the camera limits change
///   5. the screen fades back in, the player fades in and steps away from the door
/// Put ONE of these in the scene (the builder adds it to GameSystems).
/// Needs a ScreenFlash on the Canvas for the screen fade (without it the change is instant).
/// </summary>
public class RoomTransition : MonoBehaviour
{
    public static RoomTransition Instance { get; private set; }

    [Header("Timing (seconds)")]
    [SerializeField] private float stepIntoDoorTime = 0.4f;
    [SerializeField] private float fadeOutTime = 0.5f;
    [SerializeField] private float stayBlackTime = 0.4f;
    [SerializeField] private float fadeInTime = 0.5f;

    [Tooltip("Wait this long after a transition ends before another door can be used.")]
    [SerializeField] private float cooldownAfter = 0.6f;

    [Header("Look")]
    [SerializeField] private Color fadeColor = Color.black;

    private bool busy;
    private float readyTime;   // Time.time after which a new transition may start

    /// <summary>The room the player is in (null until the first door is used).</summary>
    public Room CurrentRoom { get; private set; }
    /// <summary>True while a transition runs AND during the short cooldown after it.</summary>
    public bool IsBusy => busy || Time.time < readyTime;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Show only the room the player starts in (rooms with a "Content" object)
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;
        System.Collections.Generic.List<Room> rooms = Room.All;
        Room start = null;
        foreach (Room r in rooms)
        {
            if (r.content == null) continue;
            float x = player.transform.position.x;
            if (x >= r.boundsMin.x && x <= r.boundsMax.x) { start = r; break; }
        }
        if (start != null) ShowOnly(start);
    }

    // Turns on this room's content and turns off every other room's content
    private void ShowOnly(Room room)
    {
        foreach (Room r in Room.All)
        {
            if (r.content != null) r.content.SetActive(r == room);
        }
    }

    public void Go(RoomDoor door)
    {
        // Set busy HERE (not inside the coroutine) so clicks in the same frame can't start two
        if (IsBusy) return;
        busy = true;
        StartCoroutine(Run(door));
    }

    private IEnumerator Run(RoomDoor door)
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null || door.arrivePoint == null)
        {
            Debug.LogWarning("RoomTransition: needs a Player (tag 'Player') and the door's Arrive Point.", door);
            busy = false;
            yield break;
        }

        PlayerMovement2D move = player.GetComponent<PlayerMovement2D>();
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        SpriteRenderer[] sprites = player.GetComponentsInChildren<SpriteRenderer>();
        Vector3 baseScale = player.transform.localScale;
        Vector3 startPos = player.transform.position;
        PlayerAnimation pa = player.GetComponentInChildren<PlayerAnimation>();

        // try/finally: whatever goes wrong inside (an error, the object being disabled...),
        // the player and the click lock are always given back, so a door can never get "stuck".
        try
        {
            if (DialogueUI.Instance != null) DialogueUI.Instance.Hide();

            // 1. Walk up to the door (something in the way, like a fallen pot, cannot stop it for long)
            if (move != null)
            {
                move.AutoWalkTo(door.transform.position.x, 0.15f, 1.5f);   // stop right in front of the doorway
                while (move.IsAutoWalking) yield return null;
                move.SetControlLocked(true);
            }

            // 2. Glide the last bit so the player is exactly in front of the doorway, then step into it
            if (body != null) body.simulated = false;   // no gravity / collisions while sliding
            startPos = player.transform.position;
            float doorX = door.transform.position.x;
            float glide = 0f;
            while (glide < 0.2f)
            {
                glide += Time.deltaTime;
                float x = Mathf.Lerp(startPos.x, doorX, Mathf.SmoothStep(0f, 1f, glide / 0.2f));
                player.transform.position = new Vector3(x, startPos.y, startPos.z);
                yield return null;
            }
            startPos = new Vector3(doorX, startPos.y, startPos.z);
            player.transform.position = startPos;

            if (pa != null) pa.SetDoorPose(PlayerAnimation.DoorPose.Enter);   // back view: walking into the door
            yield return DepthWalk(player.transform, sprites, startPos, baseScale, door, 1f, 0f, stepIntoDoorTime);

            // 3. Screen to black
            if (ScreenFlash.Instance != null)
            {
                ScreenFlash.Instance.FadeToColor(fadeColor, fadeOutTime);
                yield return new WaitForSeconds(fadeOutTime);
            }

            // 4. Move the player and the camera limits to the other side
            Vector3 arrive = door.arrivePoint.position;
            // Start slightly "deeper" in the other doorway (small and invisible), then walk out toward the camera
            player.transform.position = new Vector3(arrive.x, arrive.y + door.depthDistance, startPos.z);
            player.transform.localScale = baseScale * door.depthScale;
            EnterRoom(door.arriveRoom);
            if (pa != null) pa.SetDoorPose(PlayerAnimation.DoorPose.Exit);   // front view: walking out toward the camera

            yield return new WaitForSeconds(stayBlackTime);

            // 5. Fade back in and walk out toward the camera
            if (ScreenFlash.Instance != null) ScreenFlash.Instance.FadeToClear(fadeInTime);
            Vector3 outStart = new Vector3(arrive.x, arrive.y + door.depthDistance, startPos.z);
            yield return DepthWalk(player.transform, sprites, outStart, baseScale, door, 0f, 1f, fadeInTime, true);

            if (pa != null) pa.SetDoorPose(PlayerAnimation.DoorPose.None);

            // Back to the real position and physics
            player.transform.position = new Vector3(arrive.x, arrive.y, startPos.z);
            player.transform.localScale = baseScale;
            if (body != null)
            {
                body.simulated = true;
                body.position = new Vector2(arrive.x, arrive.y);
                body.linearVelocity = Vector2.zero;
            }
            Physics2D.SyncTransforms();

            // A message the first time this door is used
            if (!door.used && !string.IsNullOrEmpty(door.firstTimeMessage) && DialogueUI.Instance != null)
                DialogueUI.Instance.Show(door.firstTimeMessage);
            door.used = true;

            // Step away from the door
            if (move != null)
            {
                move.AutoWalkTo(arrive.x + door.stepOutDirection * door.stepOutDistance, 0.1f, 1.5f);
                while (move.IsAutoWalking) yield return null;
            }
        }
        finally
        {
            // Always runs: give everything back
            if (player != null)
            {
                if (pa != null) pa.SetDoorPose(PlayerAnimation.DoorPose.None);
                player.transform.localScale = baseScale;
                SetPlayerAlpha(sprites, 1f);
                if (body != null) body.simulated = true;
                if (move != null) move.SetControlLocked(false);
            }
            busy = false;
            readyTime = Time.time + cooldownAfter;
        }
    }

    private void EnterRoom(Room room)
    {
        if (room == null) return;
        CurrentRoom = room;
        ShowOnly(room);   // only the room we are in is drawn / active

        Camera cam = Camera.main;
        if (cam == null) return;

        CameraFollow2D follow = cam.GetComponent<CameraFollow2D>();
        if (follow != null)
        {
            follow.SetBounds(room.boundsMin, room.boundsMax);
            follow.SnapToTarget();
        }
        else
        {
            // Another follow script is on the camera (no limits): at least jump it to the player
            Vector3 p = GameObject.FindWithTag("Player").transform.position;
            cam.transform.position = new Vector3(p.x, cam.transform.position.y, cam.transform.position.z);
        }
    }

    // Slides the player up/down the screen while scaling and fading (walking into / out of a doorway).
    // goingIn: depthT goes 0 -> 1 (up and smaller). coming out: the same but reversed.
    private IEnumerator DepthWalk(Transform player, SpriteRenderer[] sprites, Vector3 from, Vector3 baseScale,
        RoomDoor door, float alphaFrom, float alphaTo, float duration, bool comingOut = false)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            // going in: depth 0 -> 1 ; coming out: depth 1 -> 0
            float depth = comingOut ? 1f - k : k;
            player.position = new Vector3(from.x, (comingOut ? from.y - door.depthDistance : from.y) + door.depthDistance * depth, from.z);
            player.localScale = baseScale * Mathf.Lerp(1f, door.depthScale, depth);
            SetPlayerAlpha(sprites, Mathf.Lerp(alphaFrom, alphaTo, k));
            yield return null;
        }
        float endDepth = comingOut ? 0f : 1f;
        player.position = new Vector3(from.x, (comingOut ? from.y - door.depthDistance : from.y) + door.depthDistance * endDepth, from.z);
        player.localScale = baseScale * Mathf.Lerp(1f, door.depthScale, endDepth);
        SetPlayerAlpha(sprites, alphaTo);
    }

    // Fades the player's sprites from one alpha to another (as if stepping through a doorway)
    private IEnumerator FadePlayer(SpriteRenderer[] sprites, float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            SetPlayerAlpha(sprites, Mathf.Lerp(from, to, t / duration));
            yield return null;
        }
        SetPlayerAlpha(sprites, to);
    }

    private static void SetPlayerAlpha(SpriteRenderer[] sprites, float alpha)
    {
        foreach (SpriteRenderer sr in sprites)
        {
            Color c = sr.color;
            sr.color = new Color(c.r, c.g, c.b, alpha);
        }
    }
}
