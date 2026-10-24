
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMove : MonoBehaviour
{
    public float moveSpeed = 5f;
    public SpriteRenderer spriteRenderer;

    private Rigidbody2D rb;
    private float moveX;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        moveX = 0f;

        if (Keyboard.current == null) return;

        if (Keyboard.current.aKey.isPressed ||
            Keyboard.current.leftArrowKey.isPressed)
        {
            moveX = -1f;
        }
        else if (Keyboard.current.dKey.isPressed ||
                 Keyboard.current.rightArrowKey.isPressed)
        {
            moveX = 1f;
        }

        // พลิกภาพตัวละครซ้าย-ขวา
        if (spriteRenderer != null)
        {
            if (moveX > 0)
                spriteRenderer.flipX = true;
            else if (moveX < 0)
                spriteRenderer.flipX = false;
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(
            moveX * moveSpeed,
            rb.linearVelocity.y
        );
    }
}
