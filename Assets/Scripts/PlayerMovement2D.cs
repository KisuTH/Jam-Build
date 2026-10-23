using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Simple 2D keyboard walking (New Input System): A/D = left/right, W/S = up/down.
/// Attach to a Player with a Rigidbody2D (Gravity Scale = 0, Freeze Rotation Z).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;

    [Tooltip("OFF: only one direction at a time (left/right wins if both axes are pressed). ON: diagonals allowed.")]
    [SerializeField] private bool allowDiagonal = false;

    private Rigidbody2D rb;
    private Vector2 input;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null)
        {
            input = Vector2.zero;
            return;
        }

        float x = 0f;
        float y = 0f;

        if (kb.aKey.isPressed) x -= 1f;
        if (kb.dKey.isPressed) x += 1f;
        if (kb.wKey.isPressed) y += 1f;
        if (kb.sKey.isPressed) y -= 1f;

        if (!allowDiagonal && x != 0f) y = 0f;

        input = new Vector2(x, y).normalized;
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + input * moveSpeed * Time.fixedDeltaTime);
    }
}
