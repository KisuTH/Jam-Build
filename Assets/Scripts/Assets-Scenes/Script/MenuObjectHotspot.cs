using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// A traced, clickable object in a single-piece menu illustration.
[AddComponentMenu("UI/Menu Object Hotspot")]
[RequireComponent(typeof(CanvasRenderer))]
public class MenuObjectHotspot : MaskableGraphic, ICanvasRaycastFilter,
    IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Vector2[] outline = new Vector2[0];
    [SerializeField, Min(0.1f)] private float outlineWidth = 4f;
    [SerializeField, Min(1f)] private float referenceWidth = 100f;
    private bool hovered;

    public bool IsHovered => hovered;

    // Points use local normalized coordinates, with the origin at the bottom left.
    public void SetOutline(Vector2[] points, float artworkWidth)
    {
        outline = points;
        referenceWidth = Mathf.Max(1f, artworkWidth);
        SetVerticesDirty();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        hovered = false;
        // The invisible hit region must remain eligible for GraphicRaycaster.
        canvasRenderer.cullTransparentMesh = false;
    }

    protected override void OnDisable()
    {
        hovered = false;
        base.OnDisable();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        SetVerticesDirty();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        SetVerticesDirty();
    }

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        if (outline.Length < 3 || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, screenPoint, eventCamera, out Vector2 local)) return false;

        Rect rect = rectTransform.rect;
        if (rect.width <= 0f || rect.height <= 0f) return false;
        Vector2 p = new Vector2((local.x - rect.xMin) / rect.width,
            (local.y - rect.yMin) / rect.height);
        bool inside = false;
        for (int i = 0, j = outline.Length - 1; i < outline.Length; j = i++)
        {
            Vector2 a = outline[i], b = outline[j];
            if ((a.y > p.y) != (b.y > p.y)
                && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)
                inside = !inside;
        }
        return inside;
    }

    private Vector2 LocalPoint(int index, Rect rect)
    {
        Vector2 p = outline[index];
        return new Vector2(rect.xMin + p.x * rect.width, rect.yMin + p.y * rect.height);
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = rectTransform.rect;
        if (!hovered || outline.Length < 3)
        {
            // Transparent geometry provides a stable raycast surface when idle.
            mesh.AddVert(new Vector3(rect.xMin, rect.yMin), Color.clear, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMin, rect.yMax), Color.clear, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMax, rect.yMax), Color.clear, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMax, rect.yMin), Color.clear, Vector2.zero);
            mesh.AddTriangle(0, 1, 2);
            mesh.AddTriangle(0, 2, 3);
            return;
        }

        float halfWidth = outlineWidth * rect.width / referenceWidth * 0.5f;
        int count = outline.Length;
        for (int i = 0; i < count; i++)
        {
            Vector2 p = LocalPoint(i, rect);
            Vector2 previous = (p - LocalPoint((i + count - 1) % count, rect)).normalized;
            Vector2 next = (LocalPoint((i + 1) % count, rect) - p).normalized;
            Vector2 normal = new Vector2(-previous.y, previous.x);
            Vector2 miter = (normal + new Vector2(-next.y, next.x)).normalized;
            float denominator = Vector2.Dot(miter, normal);
            Vector2 offset = Mathf.Abs(denominator) > 0.01f
                ? miter * Mathf.Clamp(halfWidth / denominator, -halfWidth * 3f, halfWidth * 3f)
                : normal * halfWidth;
            mesh.AddVert(p + offset, color, Vector2.zero);
            mesh.AddVert(p - offset, color, Vector2.zero);
        }
        for (int i = 0; i < count; i++)
        {
            int a = i * 2, b = ((i + 1) % count) * 2;
            mesh.AddTriangle(a, b, b + 1);
            mesh.AddTriangle(a, b + 1, a + 1);
        }
    }
}
