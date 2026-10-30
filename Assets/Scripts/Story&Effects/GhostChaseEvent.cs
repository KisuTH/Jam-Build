using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The red-folder chase.
///   1. The player picks up the red folder (story flag "got_red_folder"): the camera shakes.
///   2. A ghost walks after the player. It follows through every door: each time the player enters
///      a new room the ghost shows up again behind the player, until the player reaches Anon's room.
///   3. When the player leaves Anon's room, Anon disappears.
///   4. When the player comes back and walks past the spot where Anon stood, the game ends.
/// Put this on an always-active object (the menu Tools > Rain > Setup Ghost Chase builds everything).
/// If the ghost touches the player, the player dies (LoopManager.Die). Jumping over it avoids the hit.
/// </summary>
public class GhostChaseEvent : MonoBehaviour
{
    [Header("Story")]
    [Tooltip("The story flag that starts the chase (ItemPickup sets got_<itemId>).")]
    public string triggerFlag = "got_red_folder";
    [Tooltip("Anon (the object with the dialogue). It is hidden after the player leaves its room.")]
    public GameObject anon;
    [Tooltip("The ghost (starts switched off).")]
    public GameObject ghost;
    [Tooltip("Where the ghost first appears (left of the cabinet with the red folder). Only its X matters. Drag it in the Scene view.")]
    public Transform firstSpawnPoint;

    [Header("Ghost")]
    public float ghostSpeed = 3f;
    [Tooltip("How far behind the player the ghost appears.")]
    public float spawnDistance = 7f;
    [Tooltip("Seconds after the player enters a room before the ghost appears.")]
    public float spawnDelay = 1.2f;
    [Tooltip("In rooms without a fixed spawn point the ghost appears this far inside the map edge nearest to where the player came in.")]
    public float edgeMargin = 3f;
    [Tooltip("The ghost does not appear while the player is closer than this to its spawn place (so it never pops up in the player's face).")]
    public float minSpawnGap = 9f;
    [Tooltip("The ghost kills the player when it gets this close (sideways).")]
    public float catchDistance = 1.0f;
    [Tooltip("...and only if the player is not higher than this above the ghost (jump over it to escape).")]
    public float catchHeight = 1.5f;
    [Tooltip("Raise (+) or lower (-) the ghost's feet from the floor. 0 = feet on the player's floor.")]
    public float feetOffset = 0f;
    [Tooltip("Tick if the ghost picture looks to the LEFT. Untick if it looks to the right.")]
    public bool spriteFacesLeft = true;

    [System.Serializable]
    public class RoomSpawn
    {
        [Tooltip("When the player is in this room...")] public Room room;
        [Tooltip("...the ghost appears at this point (only its X is used).")] public Transform point;
    }
    [Tooltip("Rooms where the ghost must appear at a fixed place (e.g. the far right end of the hallway).")]
    public RoomSpawn[] roomSpawns = new RoomSpawn[0];

    [Header("Camera shake")]
    public float startShakeTime = 1.5f;
    public float startShakeStrength = 0.25f;
    [Tooltip("The screen shakes each time the ghost appears (fades in).")] public float spawnShakeTime = 0.6f;
    public float spawnShakeStrength = 0.2f;

    [Header("Fade")]
    public float fadeInTime = 0.8f;
    [Tooltip("The ghost fades away this fast when the player goes to the next room.")] public float fadeOutTime = 0.5f;
    [Tooltip("The ghost stays in place until it has finished fading in (so the fade can be seen).")] public bool standStillWhileFadingIn = true;

    [Header("Ending")]
    [Tooltip("The box where Anon stood. Once Anon has gone, walking past it (after the red folder) ends the game. Move it in the Scene view.")]
    public Transform anonTriggerBox;
    [TextArea] public string endMessage = "THE END";
    [Tooltip("Scene to load at the end. Empty = just fade to black.")]
    public string endSceneName = "";
    public float fadeTime = 2f;

    [Header("Sound effects (drag audio clips here, all optional)")]
    [Tooltip("Starts when the red folder is picked up (with the camera shake). Loops for the whole chase if 'Loop Chase Start Sound' is ticked.")] public AudioClip chaseStartSound;
    [Tooltip("Ticked = the Chase Start Sound repeats all through the chase, until the ghost catches the player or the game ends.")] public bool loopChaseStartSound = true;
    [Tooltip("Plays each time the ghost appears.")] public AudioClip ghostAppearSound;
    [Tooltip("Loops while the ghost is chasing; louder the closer the ghost is.")] public AudioClip chaseLoopSound;
    [Tooltip("Plays when the ghost catches the player.")] public AudioClip catchSound;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float loopMaxVolume = 0.8f;
    [Range(0f, 1f)] public float loopMinVolume = 0.15f;
    [Tooltip("Farther than this the chase loop is at its quietest.")] public float loopHearDistance = 14f;

    private AudioSource loopSource;

    private enum State { Waiting, Chasing, AtAnon, Ended }
    private State state = State.Waiting;

    private Transform player;
    private Room anonRoom, currentRoom;
    private float spotX;
    private float lastPlayerX;
    private float lastDir;
    private bool ghostWaiting;
    private float spawnTimer;
    private float nextShake;
    private bool anonGone;
    private int entrySide;   // which side of the trigger box the player came in from (0 = not set yet)
    private bool firstSpawnDone;
    private float groundY;
    private float arrivalX;
    private SpriteRenderer ghostSprite;
    private SpriteRenderer[] ghostRenderers;
    private float ghostAlpha = 1f;

    // The fade is applied to every picture of the ghost, again after the Animator each frame, so nothing can override it
    private void SetGhostAlpha(float a)
    {
        ghostAlpha = Mathf.Clamp01(a);
        ApplyGhostAlpha();
    }

    private void ApplyGhostAlpha()
    {
        if (ghostRenderers == null) return;
        foreach (SpriteRenderer r in ghostRenderers)
        {
            if (r == null) continue;
            Color c = r.color; c.a = ghostAlpha; r.color = c;
        }
    }

    private void LateUpdate()
    {
        if (ghost != null && ghost.activeSelf) ApplyGhostAlpha();
    }

    private void Start()
    {
        if (ghost != null) { ghostSprite = ghost.GetComponentInChildren<SpriteRenderer>(true); ghostRenderers = ghost.GetComponentsInChildren<SpriteRenderer>(true); ghost.SetActive(false); }
        if (anon != null) { spotX = anon.transform.position.x; anonRoom = RoomAt(anon.transform.position); }
        if (StoryFlags.Instance != null) StoryFlags.Instance.OnFlagSet += OnFlag;
        if (chaseLoopSound != null)
        {
            loopSource = gameObject.AddComponent<AudioSource>();
            loopSource.clip = chaseLoopSound;
            loopSource.loop = true;
            loopSource.playOnAwake = false;
            loopSource.spatialBlend = 0f;
            loopSource.volume = 0f;
        }
    }

    private AudioSource chaseSource;

    private void StartChaseSound()
    {
        if (chaseStartSound == null) return;
        if (!loopChaseStartSound) { GameSfx.Play(chaseStartSound, sfxVolume); return; }
        if (chaseSource == null)
        {
            chaseSource = gameObject.AddComponent<AudioSource>();
            chaseSource.playOnAwake = false;
            chaseSource.spatialBlend = 0f;
        }
        chaseSource.clip = chaseStartSound;
        chaseSource.loop = true;
        chaseSource.volume = sfxVolume;
        chaseSource.Play();
    }

    private void StopChaseSound()
    {
        if (chaseSource != null) chaseSource.Stop();
    }

    // The chase sound pauses while the ghost is gone and carries on from the same place when the ghost appears again
    private void PauseChaseSound()
    {
        if (chaseSource != null && chaseSource.isPlaying) chaseSource.Pause();
    }

    private void ResumeChaseSound()
    {
        if (chaseSource == null || chaseSource.clip == null) return;
        chaseSource.volume = sfxVolume;
        chaseSource.UnPause();
        if (!chaseSource.isPlaying) chaseSource.Play();
    }

    private void StartLoop()
    {
        if (loopSource != null && !loopSource.isPlaying) { loopSource.volume = loopMinVolume * sfxVolume; loopSource.Play(); }
    }

    private void StopLoop()
    {
        if (loopSource != null && loopSource.isPlaying) loopSource.Stop();
    }

    private void OnDestroy()
    {
        if (StoryFlags.Instance != null) StoryFlags.Instance.OnFlagSet -= OnFlag;
    }

    private void OnFlag(string flag)
    {
        if (state != State.Waiting || flag != triggerFlag) return;
        FindPlayer();
        if (player == null) return;

        state = State.Chasing;
        lastPlayerX = player.position.x;
        lastDir = 0f;
        currentRoom = RoomAt(player.position);
        arrivalX = player.position.x;
        CameraShake2D.Shake(startShakeTime, startShakeStrength);
        StartChaseSound();
        ghostWaiting = true;
        spawnTimer = startShakeTime + spawnDelay;
    }

    private void FindPlayer()
    {
        if (player != null) return;
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;
    }

    private void Update()
    {
        if (state == State.Ended) return;
        FindPlayer();
        if (player == null) return;

        Vector2 p = player.position;
        float dx = p.x - lastPlayerX;
        if (Mathf.Abs(dx) > 0.002f) lastDir = Mathf.Sign(dx);
        lastPlayerX = p.x;

        Room room = RoomAt(p);
        if (room != null && room != currentRoom)
        {
            currentRoom = room;
            entrySide = 0;
            OnRoomChanged();
        }

        // Anon disappears as soon as the player leaves his room (he stays as a trigger box)
        if (!anonGone && anon != null && currentRoom != null && currentRoom != anonRoom)
        {
            anonGone = true;
            anon.SetActive(false);
        }

        switch (state)
        {
            case State.Chasing: UpdateChase(p); break;
            case State.AtAnon: UpdateAtAnon(p); break;
        }
    }

    private void OnRoomChanged()
    {
        if (state != State.Chasing) return;
        bool wasVisible = ghost != null && ghost.activeSelf;
        HideGhost();
        if (currentRoom == anonRoom) { state = State.AtAnon; ghostWaiting = false; return; }
        ghostWaiting = true;
        spawnTimer = spawnDelay + (wasVisible ? fadeOutTime : 0f);   // the next ghost appears after the old one has faded out
        lastDir = 0f;
        if (player != null) arrivalX = player.position.x;
    }

    private void UpdateChase(Vector2 p)
    {
        if (ghost == null) return;
        if (ghostWaiting)
        {
            // wait until the door is finished: a ghost that appears while the player is still in the doorway would fade out again at once
            if (RoomTransition.Instance != null && RoomTransition.Instance.IsBusy) return;
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                float sx = ChooseSpawnX(out bool forced);
                if (forced || Mathf.Abs(sx - p.x) >= minSpawnGap) SpawnGhost(p, sx);
            }
            return;
        }
        if (!ghost.activeSelf) return;

        if (LoopManager.Instance != null && LoopManager.Instance.IsDying) return;   // the player is already dead
        bool inDoor = RoomTransition.Instance != null && RoomTransition.Instance.IsBusy;
        if (fadingOut) return;
        if (inDoor)
        {
            HideGhost();
            ghostWaiting = true;                       // a new ghost comes after the door (also if the room did not change)
            spawnTimer = spawnDelay + fadeOutTime;
            arrivalX = p.x;
            return;
        }   // the player is leaving through the door: the ghost fades out now, while it can still be seen

        Vector3 g = ghost.transform.position;
        float d = p.x - g.x;
        if (!inDoor && !(fadingIn && standStillWhileFadingIn) && Mathf.Abs(d) > catchDistance * 0.5f) g.x += Mathf.Sign(d) * ghostSpeed * Time.deltaTime;
        ghost.transform.position = g;
        PlaceFeet();
        if (ghostSprite != null && Mathf.Abs(d) > 0.05f) ghostSprite.flipX = (d > 0f) != !spriteFacesLeft;

        // touched the player: the player dies
        float dist = Mathf.Abs(d);
        if (loopSource != null)
            loopSource.volume = Mathf.Lerp(loopMaxVolume, loopMinVolume, Mathf.Clamp01(dist / Mathf.Max(1f, loopHearDistance))) * sfxVolume;
        bool visible = ghostSprite == null || ghostAlpha > 0.5f;
        if (!inDoor && visible && dist <= catchDistance && PlayerFeetY() - groundY <= catchHeight)
        {
            state = State.Ended;
            StopLoop();
            StopChaseSound();
            GameSfx.Play(catchSound, sfxVolume);
            LoopManager.Ensure().DieByGhost();
            return;
        }

        // the closer the ghost, the stronger the small shake
        if (dist < 8f && Time.time >= nextShake)
        {
            CameraShake2D.Shake(0.25f, 0.03f + (8f - dist) * 0.012f);
            nextShake = Time.time + 0.25f;
        }
    }

    // Where the ghost appears in the current room. Always the same place for a room:
    //  1. the very first time: the start point (left of the red-folder cabinet),
    //  2. a fixed point set for the room (roomSpawns),
    //  3. otherwise the map edge nearest to where the player came in.
    private float ChooseSpawnX(out bool forced)
    {
        forced = false;
        if (!firstSpawnDone && firstSpawnPoint != null && RoomAt(firstSpawnPoint.position) == currentRoom)
        {
            forced = true;
            return firstSpawnPoint.position.x;
        }
        if (roomSpawns != null)
            foreach (RoomSpawn rs in roomSpawns)
                if (rs != null && rs.room == currentRoom && rs.point != null) return rs.point.position.x;
        if (currentRoom == null) return arrivalX;
        float mid = (currentRoom.boundsMin.x + currentRoom.boundsMax.x) * 0.5f;
        return arrivalX < mid ? currentRoom.boundsMin.x + edgeMargin : currentRoom.boundsMax.x - edgeMargin;
    }

    private void SpawnGhost(Vector2 p, float x)
    {
        ghostWaiting = false;
        firstSpawnDone = true;
        groundY = PlayerFeetY() + feetOffset;
        ghost.transform.position = new Vector3(x, p.y, 0f);
        ghost.SetActive(true);
        PlaceFeet();
        GameSfx.Play(ghostAppearSound, sfxVolume);
        CameraShake2D.Shake(spawnShakeTime, spawnShakeStrength);
        StartLoop();
        ResumeChaseSound();
        StopFade();
        SetGhostAlpha(0f);
        fadeRoutine = StartCoroutine(FadeIn());
    }

    private bool fadingIn;

    private IEnumerator FadeIn()
    {
        fadingIn = true;
        if (ghostSprite == null) { fadingIn = false; yield break; }
        float len = Mathf.Max(0.01f, fadeInTime);
        for (float t = 0f; t < len; t += Time.deltaTime)
        {
            SetGhostAlpha(t / len);
            yield return null;
        }
        SetGhostAlpha(1f);
        fadingIn = false;
        fadeRoutine = null;
    }

    // The ghost fades away (and its sound with it), then it is switched off
    private IEnumerator FadeOutAndHide()
    {
        fadingOut = true;
        if (ghostSprite != null)
        {
            float start = ghostAlpha;
            float loopStart = loopSource != null ? loopSource.volume : 0f;
            float chaseStart = chaseSource != null ? chaseSource.volume : 0f;
            float len = Mathf.Max(0.01f, fadeOutTime);
            for (float t = 0f; t < len; t += Time.deltaTime)
            {
                float k = 1f - t / len;
                SetGhostAlpha(start * k);
                if (loopSource != null) loopSource.volume = loopStart * k;
                if (chaseSource != null) chaseSource.volume = chaseStart * k;
                yield return null;
            }
        }
        StopLoop();
        PauseChaseSound();
        if (ghost != null) ghost.SetActive(false);
        fadingOut = false;
        fadeRoutine = null;
    }

    private Coroutine fadeRoutine;
    private void StopFade()
    {
        fadingIn = false;
        fadingOut = false;
        if (fadeRoutine != null) { StopCoroutine(fadeRoutine); fadeRoutine = null; }
    }

    // The player's feet (bottom of its collider = the floor)
    private float PlayerFeetY()
    {
        Collider2D col = player != null ? player.GetComponent<Collider2D>() : null;
        return col != null ? col.bounds.min.y : (player != null ? player.position.y : 0f);
    }

    // Moves the ghost up/down so the bottom of its picture stands on the floor
    private void PlaceFeet()
    {
        if (ghostSprite == null || ghostSprite.sprite == null) return;
        float bottom = ghostSprite.bounds.min.y;
        Vector3 pos = ghost.transform.position;
        pos.y += groundY - bottom;
        ghost.transform.position = pos;
    }

    private bool fadingOut;

    private void HideGhost()
    {
        if (fadingOut) return;
        StopFade();
        if (ghost != null && ghost.activeSelf) fadeRoutine = StartCoroutine(FadeOutAndHide());
        else { StopLoop(); PauseChaseSound(); }
    }

    // The player came back to Anon's room after the red folder: walking past the trigger box ends the game.
    private void UpdateAtAnon(Vector2 p)
    {
        if (!anonGone || currentRoom != anonRoom) return;
        if (RoomTransition.Instance != null && RoomTransition.Instance.IsBusy) return;   // wait until the player has finished the door
        float boxX = anonTriggerBox != null ? anonTriggerBox.position.x : spotX;
        int side = p.x < boxX ? -1 : 1;
        if (entrySide == 0) { entrySide = side; return; }
        if (side != entrySide) StartCoroutine(EndGame());
    }

    private IEnumerator EndGame()
    {
        state = State.Ended;
        StopLoop();
        StopChaseSound();
        PlayerMovement2D move = player != null ? player.GetComponent<PlayerMovement2D>() : null;
        if (move != null) move.SetControlLocked(true);
        if (DialogueUI.Instance != null) DialogueUI.Instance.Hide();
        if (ScreenFlash.Instance != null) ScreenFlash.Instance.FadeToColor(Color.black, fadeTime);
        yield return new WaitForSeconds(fadeTime);
        if (DialogueUI.Instance != null) DialogueUI.Instance.Show(endMessage);
        yield return new WaitForSeconds(3.5f);
        if (!string.IsNullOrEmpty(endSceneName)) SceneManager.LoadScene(endSceneName);
    }

    // The smallest Room whose box contains the point
    private static Room RoomAt(Vector2 pos)
    {
        Room best = null;
        float bestArea = float.MaxValue;
        foreach (Room r in Room.All)
        {
            if (pos.x < r.boundsMin.x || pos.x > r.boundsMax.x) continue;
            if (pos.y < Mathf.Min(r.boundsMin.y, r.boundsMax.y) || pos.y > Mathf.Max(r.boundsMin.y, r.boundsMax.y)) continue;
            float a = Mathf.Abs((r.boundsMax.x - r.boundsMin.x) * (r.boundsMax.y - r.boundsMin.y));
            if (a < bestArea) { bestArea = a; best = r; }
        }
        return best;
    }
}
