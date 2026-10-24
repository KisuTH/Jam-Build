using UnityEngine;

/// <summary>
/// Makes an Orthographic camera follow the player smoothly (side-scroller style).
/// Attach to the Main Camera. If Target is empty it finds the object tagged "Player".
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFollow2D : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 offset = Vector2.zero;

    [Header("Follow")]
    [Tooltip("Higher = the camera lags behind more. 0 = snaps to the player.")]
    [SerializeField] private float smoothTime = 0.2f;
    [SerializeField] private bool followX = true;
    [Tooltip("OFF = camera height stays fixed (usual for side-scrollers).")]
    [SerializeField] private bool followY = false;

    [Header("Level Bounds (optional)")]
    [Tooltip("Stops the camera from showing anything outside the level.")]
    [SerializeField] private bool useBounds = false;
    [Tooltip("Bottom-left corner of the area the camera is allowed to see (world position).")]
    [SerializeField] private Vector2 boundsMin = new Vector2(-15f, -5f);
    [Tooltip("Top-right corner of the area the camera is allowed to see (world position).")]
    [SerializeField] private Vector2 boundsMax = new Vector2(15f, 5f);

    private Camera cam;
    private Vector3 velocity;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) target = p.transform;
        }

        // Start already centered on the player (no sliding in at the beginning)
        if (target != null) transform.position = Clamp(GetDesiredPosition());
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = Clamp(GetDesiredPosition());

        if (smoothTime <= 0f)
            transform.position = desired;
        else
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }

    /// <summary>Change who the camera follows (e.g. when switching characters).</summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private Vector3 GetDesiredPosition()
    {
        Vector3 pos = transform.position;
        if (followX) pos.x = target.position.x + offset.x;
        if (followY) pos.y = target.position.y + offset.y;
        return pos; // z stays the same
    }

    private Vector3 Clamp(Vector3 pos)
    {
        if (!useBounds || cam == null || !cam.orthographic) return pos;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        float minX = boundsMin.x + halfW;
        float maxX = boundsMax.x - halfW;
        float minY = boundsMin.y + halfH;
        float maxY = boundsMax.y - halfH;

        // If the level is smaller than the screen, just center on it
        pos.x = minX > maxX ? (boundsMin.x + boundsMax.x) * 0.5f : Mathf.Clamp(pos.x, minX, maxX);
        pos.y = minY > maxY ? (boundsMin.y + boundsMax.y) * 0.5f : Mathf.Clamp(pos.y, minY, maxY);
        return pos;
    }

    private void OnDrawGizmosSelected()
    {
        if (!useBounds) return;
        Gizmos.color = Color.cyan;
        Vector3 center = (boundsMin + boundsMax) * 0.5f;
        Vector3 size = boundsMax - boundsMin;
        Gizmos.DrawWireCube(center, size);
    }
}
