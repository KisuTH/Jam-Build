using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>
/// Drag-and-drop animation controller for the Player (no Animator Controller needed).
/// Lives on the child "Visual" of the Player (Animator + SpriteRenderer next to it).
/// Drag AnimationClips into the slots:
///   Right = clip drawn facing RIGHT (used when walking with D)
///   Left  = clip drawn facing LEFT  (used when walking with A)
///   Leave one empty and the other one is mirrored. (The friend's art all faces LEFT -> use the Left slots.)
/// Crouch (S held): the crouch clip plays once and STAYS on its last frame while S is held.
/// Jump: plays once and stays on its last frame until landing.
/// Door: Enter Door (back view) and Exit Door (front view) are played by RoomTransition.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnimation : MonoBehaviour
{
    [System.Serializable]
    public class DirClip
    {
        [Tooltip("Drawn facing RIGHT")] public AnimationClip right;
        [Tooltip("Drawn facing LEFT")] public AnimationClip left;
        [Tooltip("Hold (freeze) at this time in seconds when this state stays on. 0 = last frame of the clip.")] public float holdTime = 0f;
        [Tooltip("Size of these clips relative to the others (some sheets are drawn smaller). 1 = same.")] public float sizeMultiplier = 1f;
    }

    [System.Serializable]
    public struct SpriteAnchor { public Sprite sprite; public float x; }

    public enum DoorPose { None, Enter, Exit }
    private enum Mode { Loop, Freeze, HoldLast }

    [Header("Animations (drag clips here)")]
    public DirClip idle;
    [Tooltip("Walking (A / D)")] public DirClip walk;
    [Tooltip("Running (hold Left Shift). Empty = the Walk clip played faster")] public DirClip run;
    [Tooltip("In the air (W). Plays once, stays on the last frame")] public DirClip jump;
    [Tooltip("Crouching still (S held). Plays once, stays on the last frame")] public DirClip crouch;
    [Tooltip("Crouching and moving (S + A/D)")] public DirClip crouchWalk;

    [Header("Door (played by RoomTransition)")]
    [Tooltip("Walking INTO the door (back view)")] public AnimationClip enterDoor;
    [Tooltip("Walking OUT of the door (front view)")] public AnimationClip exitDoor;

    [Tooltip("Size of the door clips relative to the others.")] public float doorSizeMultiplier = 1f;

    [Header("Filled by the menu (keeps the body from swaying while walking)")]
    [SerializeField] private List<SpriteAnchor> anchors = new List<SpriteAnchor>();

    [Header("Tuning")]
    [Tooltip("Size of the character. Smaller = smaller character (e.g. 0.1).")]
    [SerializeField] private float spriteWorldScale = 0.12f;
    [Tooltip("Jump clip speed (the jump in the air is shorter than the clip).")]
    [SerializeField] private float jumpPlaybackSpeed = 1.6f;
    [Tooltip("Crouch clip speed.")]
    [SerializeField] private float crouchPlaybackSpeed = 1.5f;
    [Tooltip("If there is no Run clip: how much faster the Walk clip plays while running.")]
    [SerializeField] private float runWalkSpeedUp = 1.7f;

    private Animator anim;
    private Rigidbody2D rb;
    private PlayerMovement2D move;
    private Collider2D col;
    private SpriteRenderer sr;

    private PlayableGraph graph;
    private AnimationMixerPlayable mixer;
    private readonly List<AnimationClip> clips = new List<AnimationClip>();
    private AnimationClip current;
    private Mode currentMode = Mode.Loop;
    private float currentSpeed = 1f;
    private bool facingRight = true;
    private DoorPose doorPose = DoorPose.None;
    private float standingScaleX = 1f;
    private bool skipIntro, prevCrouching;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        anim.runtimeAnimatorController = null;
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponentInParent<Rigidbody2D>();
        // Physics runs at 50 Hz, the screen at 60+: without interpolation the character visibly jitters left and right
        if (rb != null && rb.interpolation == RigidbodyInterpolation2D.None)
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        move = GetComponentInParent<PlayerMovement2D>();
        col = GetComponentInParent<Collider2D>();
        if (transform.parent != null) standingScaleX = Mathf.Abs(transform.parent.localScale.x);
        foreach (SpriteAnchor a in anchors) if (a.sprite != null) anchorX[a.sprite] = a.x;
        BuildGraph();
    }

    private readonly Dictionary<AnimationClip, float> sizeMul = new Dictionary<AnimationClip, float>();
    private readonly Dictionary<AnimationClip, float> holdAt = new Dictionary<AnimationClip, float>();
    private readonly Dictionary<Sprite, float> anchorX = new Dictionary<Sprite, float>();

    private void Add(AnimationClip c, float mul, float hold = 0f)
    {
        if (c == null) return;
        if (!clips.Contains(c)) clips.Add(c);
        sizeMul[c] = mul <= 0f ? 1f : mul;
        holdAt[c] = hold;
    }

    private void BuildGraph()
    {
        foreach (DirClip d in new[] { idle, walk, run, jump, crouch, crouchWalk })
        {
            if (d == null) continue;
            Add(d.right, d.sizeMultiplier, d.holdTime); Add(d.left, d.sizeMultiplier, d.holdTime);
        }
        Add(enterDoor, doorSizeMultiplier); Add(exitDoor, doorSizeMultiplier);
        if (clips.Count == 0) { Debug.LogWarning("PlayerAnimation: no clips assigned.", this); return; }

        graph = PlayableGraph.Create("PlayerAnimation");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        mixer = AnimationMixerPlayable.Create(graph, clips.Count);
        for (int i = 0; i < clips.Count; i++)
        {
            var p = AnimationClipPlayable.Create(graph, clips[i]);
            graph.Connect(p, 0, mixer, i);
            mixer.SetInputWeight(i, 0f);
        }
        var output = AnimationPlayableOutput.Create(graph, "Player", anim);
        output.SetSourcePlayable(mixer);
        graph.Play();
    }

    private void OnDestroy()
    {
        if (graph.IsValid()) graph.Destroy();
    }

    /// <summary>Called by RoomTransition: the walk-into-the-door / walk-out-of-the-door animations.</summary>
    public void SetDoorPose(DoorPose pose) { doorPose = pose; }

    private void Update()
    {
        if (!graph.IsValid()) return;

        // Door animations take over completely
        if (doorPose != DoorPose.None)
        {
            AnimationClip dc = doorPose == DoorPose.Enter ? enterDoor : exitDoor;
            if (dc != null)
            {
                if (sr != null) sr.flipX = false;
                Play(dc, Mode.Loop, 1f);
                return;
            }
        }

        if (rb == null || move == null) return;

        Vector2 v = rb.linearVelocity;
        bool moving = Mathf.Abs(v.x) > 0.05f;
        if (moving) facingRight = v.x > 0f;

        AnimationClip pick;
        bool useRightSlot;
        Mode mode = Mode.Loop;
        float speed = 1f;

        if (move.IsCrouching)
        {
            if (moving) pick = Choose(out useRightSlot, crouchWalk, crouch, idle);
            else
            {
                pick = Choose(out useRightSlot, crouch, idle);
                if (HasClip(crouch)) { mode = Mode.HoldLast; speed = crouchPlaybackSpeed; }
            }
        }
        else if (!move.IsGrounded)
        {
            pick = Choose(out useRightSlot, jump, moving ? walk : idle, idle);
            if (HasClip(jump)) { mode = Mode.HoldLast; speed = jumpPlaybackSpeed; }
        }
        else if (moving)
        {
            bool running = move.IsRunning;
            if (running && HasClip(run)) pick = Choose(out useRightSlot, run, walk, idle);
            else
            {
                pick = Choose(out useRightSlot, walk, idle);
                if (running) speed = runWalkSpeedUp;   // no Run art yet: same walk, faster
            }
        }
        else
        {
            pick = Choose(out useRightSlot, idle, walk);
            if (!HasClip(idle)) mode = Mode.Freeze;   // no Idle art: first frame of Walk
        }

        if (pick == null) return;
        if (sr != null) sr.flipX = (useRightSlot != facingRight);
        // Already crouching (e.g. stopped while crouch-walking): do not replay the "going down" frames
        skipIntro = move.IsCrouching && prevCrouching && mode == Mode.HoldLast;
        prevCrouching = move.IsCrouching;
        Play(pick, mode, speed);
    }

    // First DirClip in the list that has something for the facing direction
    private AnimationClip Choose(out bool usedRightSlot, params DirClip[] chain)
    {
        foreach (DirClip d in chain)
        {
            if (d == null) continue;
            AnimationClip want = facingRight ? d.right : d.left;
            AnimationClip other = facingRight ? d.left : d.right;
            if (want != null) { usedRightSlot = facingRight; return want; }
            if (other != null) { usedRightSlot = !facingRight; return other; }
        }
        usedRightSlot = facingRight;
        return null;
    }

    private static bool HasClip(DirClip d) { return d != null && (d.right != null || d.left != null); }

    private void Play(AnimationClip clip, Mode mode, float speed)
    {
        int idx = clips.IndexOf(clip);
        if (idx < 0) return;
        Playable p = mixer.GetInput(idx);

        if (clip != current)
        {
            for (int i = 0; i < clips.Count; i++)
                mixer.SetInputWeight(i, i == idx ? 1f : 0f);
            p.SetTime(skipIntro ? HoldTime(clip) : 0);
            p.SetSpeed(mode == Mode.Freeze ? 0.0 : speed);
            current = clip;
            currentMode = mode;
            currentSpeed = speed;
        }
        else if (mode != currentMode || speed != currentSpeed)
        {
            if (mode == Mode.Freeze) p.SetTime(0);
            if (currentMode == Mode.HoldLast && mode == Mode.Loop) p.SetTime(0);
            p.SetSpeed(mode == Mode.Freeze ? 0.0 : speed);
            currentMode = mode;
            currentSpeed = speed;
        }

        // Stay on the last frame (a little before the clip's end so a looping clip does not wrap to frame 0)
        if (currentMode == Mode.HoldLast)
        {
            float ht;
            double hold = (holdAt.TryGetValue(clip, out ht) && ht > 0f) ? ht + 0.02 : clip.length - 0.04;
            if (p.GetTime() >= hold)
            {
                p.SetTime(hold);
                p.SetSpeed(0.0);
            }
        }
    }

    // Read live from the slots, so you can tune "Size Multiplier" in the Inspector while the game runs
    private float SizeOf(AnimationClip c)
    {
        if (c == null) return 1f;
        foreach (DirClip d in new[] { idle, walk, run, jump, crouch, crouchWalk })
            if (d != null && (d.right == c || d.left == c)) return d.sizeMultiplier > 0f ? d.sizeMultiplier : 1f;
        if (c == enterDoor || c == exitDoor) return doorSizeMultiplier > 0f ? doorSizeMultiplier : 1f;
        return 1f;
    }

    private double HoldTime(AnimationClip clip)
    {
        float ht;
        return (holdAt.TryGetValue(clip, out ht) && ht > 0f) ? ht + 0.02 : clip.length - 0.04;
    }

    private void LateUpdate()
    {
        if (sr == null || sr.sprite == null || col == null || transform.parent == null) return;

        // Size: the Player's own scale changes when crouching (y only) and during door walks (x and y together).
        // Keep the art undistorted: follow the door scale (depth) but ignore the crouch squash.
        Vector3 ps = transform.parent.lossyScale;
        float depthK = Mathf.Abs(ps.x) / Mathf.Max(0.0001f, standingScaleX);
        float mul = SizeOf(current);
        float world = spriteWorldScale * depthK * mul;
        transform.localScale = new Vector3(
            world / Mathf.Max(0.0001f, Mathf.Abs(ps.x)),
            world / Mathf.Max(0.0001f, Mathf.Abs(ps.y)), 1f);

        // Put the bottom-centre of the sprite on the bottom-centre of the collider
        Vector2 feet = FeetPoint();
        Bounds b = sr.sprite.bounds;
        float ax;
        if (!anchorX.TryGetValue(sr.sprite, out ax)) ax = b.center.x;
        float cx = sr.flipX ? -ax : ax;
        transform.position = new Vector3(
            feet.x - cx * world,
            feet.y - b.min.y * world,
            transform.parent.position.z);
    }

    // Bottom-centre of the Player's collider in world space (also valid while the Rigidbody is switched off)
    private Vector2 FeetPoint()
    {
        BoxCollider2D box = col as BoxCollider2D;
        if (box != null)
            return box.transform.TransformPoint(new Vector3(box.offset.x, box.offset.y - box.size.y * 0.5f, 0f));
        Bounds cb = col.bounds;
        return new Vector2(cb.center.x, cb.min.y);
    }
}
