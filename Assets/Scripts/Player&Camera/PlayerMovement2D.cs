using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 2D side-scroller movement (New Input System).
///   A / D = walk left / right
///   W     = jump
///   S     = crouch (hold). Slower, and the player gets shorter.
///
/// Setup: Rigidbody2D (Gravity Scale above 0, e.g. 3, Freeze Rotation Z) + a Collider2D
/// (BoxCollider2D is fine). The ground needs a Collider2D that is NOT a trigger.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : MonoBehaviour
{
    [Header("Walking")]
    [SerializeField] private float moveSpeed = 4f;

    [Header("Running (hold Left Shift)")]
    [Tooltip("OFF for now. Tick this to let Left Shift make the player run.")]
    [SerializeField] private bool allowRunning = false;
    [Tooltip("2 = twice the walking speed while Left Shift is held.")]
    [SerializeField] private float runSpeedMultiplier = 1.8f;

    [Header("Jump")]
    [Tooltip("How high the jump goes, in world units.")]
    [SerializeField] private float jumpHeight = 2.2f;
    [Tooltip("Multiplies Jump Height (1 = as set above). Raise it for a higher jump.")]
    [SerializeField] private float jumpHeightMultiplier = 1.6f;

    [Header("Crouch")]
    [Tooltip("0.6 = crouching makes the player 60% as tall.")]
    [SerializeField] private float crouchHeightMultiplier = 0.6f;
    [Tooltip("0.5 = half walking speed while crouched.")]
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;

    [Header("Ground Check")]
    [Tooltip("Which layers count as ground. Default = everything (triggers are always ignored).")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private float groundCheckDistance = 0.08f;

    private Rigidbody2D rb;
    private Collider2D col;
    private Vector3 standingScale;

    // Used by room transitions: the game can lock the keys and walk the player by itself
    private bool controlLocked;
    private float autoLastX, autoStuckTime, autoAge;   // detects an automatic walk that is blocked (wall) so it ends early
    private bool autoWalking;
    private float autoTargetX;
    private float autoStopDistance;
    private float autoTimeLeft;

    private float moveInput;
    private bool jumpQueued;
    private bool crouchHeld;
    private bool isCrouching;
    private bool isGrounded;
    private bool runHeld;
    private Key moveLeftKey = Key.A;
    private Key moveRightKey = Key.D;

    private void OnEnable()
    {
        RefreshMovementBindings();
        JamBuild.Options.NotebookOptions.ControlsChanged += RefreshMovementBindings;
    }

    private void OnDisable()
    {
        JamBuild.Options.NotebookOptions.ControlsChanged -= RefreshMovementBindings;
    }

    private void RefreshMovementBindings()
    {
        moveLeftKey = JamBuild.Options.NotebookOptions.SavedMoveLeftKey;
        moveRightKey = JamBuild.Options.NotebookOptions.SavedMoveRightKey;
    }

    public bool IsAutoWalking => autoWalking;
    public bool IsGrounded => isGrounded;
    public bool IsCrouching => isCrouching;
    public bool IsWalkingOnGround => isGrounded && Mathf.Abs(moveInput) > .01f && (!controlLocked || autoWalking);
    /// <summary>True while Left Shift is held and the player is moving on foot (not crouching).</summary>
    public bool IsRunning => runHeld && !isCrouching && Mathf.Abs(moveInput) > 0.01f;

    // Runs when you first add the script in the Editor: sets sensible Rigidbody2D values.
    private void Reset()
    {
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        body.gravityScale = 3f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        standingScale = transform.localScale;
        if (GetComponent<PlayerFootstepAudio>() == null) gameObject.AddComponent<PlayerFootstepAudio>();

        if (col == null)
            Debug.LogError("PlayerMovement2D needs a Collider2D (for example BoxCollider2D) on the Player.", this);

        if (rb.gravityScale <= 0f)
            Debug.LogWarning("PlayerMovement2D: Gravity Scale is 0, so jumping will not work. Set it above 0 (e.g. 3).", this);

        // Zero friction so the player does not stick to walls while pushing against them in the air
        if (col != null && col.sharedMaterial == null)
            col.sharedMaterial = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };
    }

    /// <summary>Locks or unlocks the keyboard (the player stands still while locked).</summary>
    public void SetControlLocked(bool locked)
    {
        controlLocked = locked;
        if (locked)
        {
            moveInput = 0f;
            jumpQueued = false;
        }
    }

    /// <summary>
    /// Walks the player to targetX by itself (keys are ignored meanwhile).
    /// It stops when within stopDistance of targetX, or after maxSeconds (e.g. if something blocks the way).
    /// Wait for it with:  while (player.IsAutoWalking) yield return null;
    /// </summary>
    public void AutoWalkTo(float targetX, float stopDistance, float maxSeconds)
    {
        autoTargetX = targetX;
        autoStopDistance = stopDistance;
        autoTimeLeft = maxSeconds;
        autoLastX = transform.position.x;
        autoStuckTime = 0f;
        autoAge = 0f;
        autoWalking = true;
        jumpQueued = false;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
        {
            moveInput = 0f;
            jumpQueued = crouchHeld = runHeld = false;
            return;
        }
        if (autoWalking)
        {
            runHeld = false;
            crouchHeld = false;
            autoTimeLeft -= Time.deltaTime;
            float dx = autoTargetX - transform.position.x;

            // not getting anywhere (a wall in the way)? stop after a very short time instead of waiting for the timeout
            autoAge += Time.deltaTime;
            if (autoAge > 0.4f && Mathf.Abs(transform.position.x - autoLastX) < 0.5f * Time.deltaTime) autoStuckTime += Time.deltaTime;
            else autoStuckTime = 0f;
            autoLastX = transform.position.x;

            if (Mathf.Abs(dx) <= autoStopDistance || autoTimeLeft <= 0f || autoStuckTime > 0.25f)
            {
                autoWalking = false;
                moveInput = 0f;
            }
            else
            {
                moveInput = Mathf.Sign(dx);
            }
            return;
        }

        if (controlLocked)
        {
            runHeld = false;
            moveInput = 0f;
            crouchHeld = false;
            jumpQueued = false;
            return;
        }

        Keyboard kb = Keyboard.current;
        if (kb == null)
        {
            runHeld = false;
            moveInput = 0f;
            crouchHeld = false;
            return;
        }

        moveInput = 0f;
        if (kb[moveLeftKey].isPressed) moveInput -= 1f;
        if (kb[moveRightKey].isPressed) moveInput += 1f;

        crouchHeld = kb.sKey.isPressed;
        runHeld = allowRunning && kb.leftShiftKey.isPressed;
        if (kb.wKey.wasPressedThisFrame) jumpQueued = true;
    }

    private void FixedUpdate()
    {
        if (col == null) return;

        isGrounded = CheckGrounded();
        UpdateCrouch();

        // Walk
        float speed = moveSpeed * (isCrouching ? crouchSpeedMultiplier : (runHeld ? runSpeedMultiplier : 1f));
        Vector2 v = rb.linearVelocity;
        v.x = moveInput * speed;

        // Jump (only from the ground, and not while crouching)
        if (jumpQueued && isGrounded && !isCrouching)
        {
            float gravity = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
            v.y = Mathf.Sqrt(2f * gravity * jumpHeight * jumpHeightMultiplier);
        }
        jumpQueued = false;

        rb.linearVelocity = v;
    }

    // ---------- Crouch ----------

    private void UpdateCrouch()
    {
        // Crouch only on the ground (so you can't shrink in mid-air)
        if (crouchHeld && isGrounded && !isCrouching)
            SetCrouch(true);
        else if (isCrouching && !crouchHeld && !HasCeilingAbove())
            SetCrouch(false);
    }

    private void SetCrouch(bool crouch)
    {
        float feetY = col.bounds.min.y;

        Vector3 s = standingScale;
        if (crouch) s.y = standingScale.y * crouchHeightMultiplier;
        transform.localScale = s;
        isCrouching = crouch;

        // Keep the feet on the same spot: scaling happens around the center, so shift back
        Physics2D.SyncTransforms();
        float shift = feetY - col.bounds.min.y;
        transform.position += new Vector3(0f, shift, 0f);
        Physics2D.SyncTransforms();
    }

    // True if standing up now would put the head inside something solid
    private bool HasCeilingAbove()
    {
        Bounds b = col.bounds;
        float extra = b.size.y * (1f / crouchHeightMultiplier - 1f);
        Vector2 center = new Vector2(b.center.x, b.max.y + extra * 0.5f);
        Vector2 size = new Vector2(b.size.x * 0.9f, extra);
        return AnySolidInBox(center, size);
    }

    // ---------- Ground check ----------

    private bool CheckGrounded()
    {
        Bounds b = col.bounds;
        Vector2 center = new Vector2(b.center.x, b.min.y - groundCheckDistance * 0.5f);
        Vector2 size = new Vector2(b.size.x * 0.9f, groundCheckDistance);
        return AnySolidInBox(center, size);
    }

    // Looks for a solid (non-trigger) collider in the box that is not the player itself
    private bool AnySolidInBox(Vector2 center, Vector2 size)
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f, groundLayers);
        foreach (Collider2D h in hits)
        {
            if (h == col || h.isTrigger) continue;
            return true;
        }
        return false;
    }
}
