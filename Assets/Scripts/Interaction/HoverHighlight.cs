using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Hover hint: when the mouse is over something the player can click (an Interactable2D), a light-blue frame is drawn
/// around it and a small label tells what the click does: Take / Open door / Open drawer / Use / Inspect.
/// No setup: it creates itself when the game starts (same picking rule as ClickInteractor2D).
/// Texts follow the language button.
/// </summary>
public class HoverHighlight : MonoBehaviour
{
    private enum Kind { Inspect, Take, Door, Drawer, Use, Continue }

    private static readonly Color Cyan = new Color(1f, 1f, 1f, 1f);

    private Canvas canvas;
    private RectTransform frame;
    private Image[] edges = new Image[4];
    private Image fill;
    private RectTransform labelBox;
    private TextMeshProUGUI label;
    private Camera cam;

    // silhouette outline (copies of the sprite in flat white, behind the real sprite)
    private Material outlineMat;
    private bool outlineMatTried;
    private GameObject outlineHolder;
    private readonly System.Collections.Generic.List<SpriteRenderer> outlinePool = new System.Collections.Generic.List<SpriteRenderer>();
    private const float OutlinePixels = 3.5f;
    private static readonly Vector2[] Dirs =
    {
        new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1),
        new Vector2(0.7f, 0.7f), new Vector2(-0.7f, 0.7f), new Vector2(0.7f, -0.7f), new Vector2(-0.7f, -0.7f)
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (FindAnyObjectByType<HoverHighlight>() != null) return;
        GameObject go = new GameObject("HoverHighlight");
        DontDestroyOnLoad(go);
        go.AddComponent<HoverHighlight>();
    }

    private void Update()
    {
        Interactable2D target = FindTarget();
        if (target == null) { Hide(); return; }

        Collider2D col = target.GetComponent<Collider2D>();
        if (col == null) { Hide(); return; }
        Bounds b = col.bounds;
        System.Collections.Generic.List<SpriteRenderer> srs = GatherSprites(target, b);
        bool outlined = srs.Count > 0 && GetOutlineMaterial() != null;
        Show(b, Classify(target), !outlined);
        if (outlined) DrawOutline(srs); else HideOutline();
    }

    // The visible sprites of the target (the character / item itself), not a huge background it may contain
    private System.Collections.Generic.List<SpriteRenderer> GatherSprites(Interactable2D t, Bounds colBounds)
    {
        var list = new System.Collections.Generic.List<SpriteRenderer>();
        float colArea = Mathf.Max(0.0001f, colBounds.size.x * colBounds.size.y);
        foreach (SpriteRenderer sr in t.GetComponentsInChildren<SpriteRenderer>(false))
        {
            if (!sr.enabled || sr.sprite == null || sr.color.a < 0.05f) continue;
            Vector3 sz = sr.bounds.size;
            if (sz.x * sz.y > colArea * 3f) continue;
            list.Add(sr);
            if (list.Count >= 6) break;
        }
        return list;
    }

    private Material GetOutlineMaterial()
    {
        if (outlineMat != null) return outlineMat;
        if (outlineMatTried) return null;
        outlineMatTried = true;
        Shader sh = Resources.Load<Shader>("Shaders/HoverOutline");
        if (sh == null) sh = Shader.Find("Rain/HoverOutline");
        if (sh != null && sh.isSupported)
        {
            outlineMat = new Material(sh);
            outlineMat.color = Color.white;
        }
        return outlineMat;
    }

    private void DrawOutline(System.Collections.Generic.List<SpriteRenderer> srs)
    {
        if (outlineHolder == null)
        {
            outlineHolder = new GameObject("HoverOutline");
            outlineHolder.transform.SetParent(transform, false);
        }
        outlineHolder.SetActive(true);

        float wpp = 2f * cam.orthographicSize / Mathf.Max(1, Screen.height);
        float d = OutlinePixels * wpp;
        int need = srs.Count * Dirs.Length;
        while (outlinePool.Count < need)
        {
            GameObject g = new GameObject("o");
            g.transform.SetParent(outlineHolder.transform, false);
            SpriteRenderer r = g.AddComponent<SpriteRenderer>();
            r.sharedMaterial = outlineMat;
            outlinePool.Add(r);
        }
        int k = 0;
        foreach (SpriteRenderer src in srs)
        {
            for (int i = 0; i < Dirs.Length; i++, k++)
            {
                SpriteRenderer o = outlinePool[k];
                o.gameObject.SetActive(true);
                o.sprite = src.sprite;
                o.flipX = src.flipX; o.flipY = src.flipY;
                o.sortingLayerID = src.sortingLayerID;
                o.sortingOrder = src.sortingOrder - 1;
                Transform ot = o.transform;
                ot.SetPositionAndRotation(src.transform.position + (Vector3)(Dirs[i] * d), src.transform.rotation);
                ot.localScale = src.transform.lossyScale;
            }
        }
        for (; k < outlinePool.Count; k++) outlinePool[k].gameObject.SetActive(false);
    }

    private void HideOutline()
    {
        if (outlineHolder != null && outlineHolder.activeSelf) outlineHolder.SetActive(false);
    }

    private Interactable2D FindTarget()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return null;
        if (cam == null) cam = Camera.main;
        if (cam == null) return null;

        if (RoomTransition.Instance != null && RoomTransition.Instance.IsBusy) return null;
        if (DialogueSequence.IsAnyPlaying) return null;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return null;

        Vector2 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        // innermost wins (a document pinned on a board beats the board)
        Interactable2D best = null;
        int bestDepth = -1;
        foreach (Collider2D hit in Physics2D.OverlapPointAll(world))
        {
            Interactable2D t = hit.GetComponentInParent<Interactable2D>();
            if (t == null) continue;
            int depth = 0;
            for (Transform tr = t.transform; tr != null; tr = tr.parent) depth++;
            if (depth > bestDepth) { best = t; bestDepth = depth; }
        }
        return best;
    }

    private static Kind Classify(Interactable2D t)
    {
        if (t.GetComponent<ItemPickup>() != null) return Kind.Take;
        if (t.GetComponent<RoomDoor>() != null) return Kind.Door;
        if (t.GetComponent<DrawerSpot>() != null) return Kind.Drawer;
        if (t.GetComponent<ChapterEnd>() != null) return Kind.Continue;

        int n = t.onInteract.GetPersistentEventCount();
        for (int i = 0; i < n; i++)
        {
            Object target = t.onInteract.GetPersistentTarget(i);
            string method = t.onInteract.GetPersistentMethodName(i);
            if (target is PasscodeUI || target is WoundPuzzleOpener) return Kind.Use;
            if (target is ItemPickup) return Kind.Take;
            if (target is RoomDoor) return Kind.Door;
            if (target is ChapterEnd) return Kind.Continue;
            if (method != null && method.StartsWith("Open")) return Kind.Use;
        }
        return Kind.Inspect;
    }

    private static string Text(Kind k)
    {
        bool th = Loc.Thai;
        string s;
        switch (k)
        {
            case Kind.Take: s = th ? "เก็บ" : "Take"; break;
            case Kind.Door: s = th ? "เปิดประตู" : "Open door"; break;
            case Kind.Drawer: s = th ? "เปิดลิ้นชัก" : "Open drawer"; break;
            case Kind.Use: s = th ? "ใช้งาน" : "Use"; break;
            case Kind.Continue: s = th ? "ไปต่อ" : "Continue"; break;
            default: s = th ? "ตรวจสอบ" : "Inspect"; break;
        }
        return th ? Loc.Shape(s) : s;
    }

    // ---------- drawing ----------

    private void Show(Bounds b, Kind kind, bool drawFrame = true)
    {
        if (canvas == null) Build();

        // the 4 corners of the object's box in screen pixels
        Vector3 a = cam.WorldToScreenPoint(new Vector3(b.min.x, b.min.y, b.center.z));
        Vector3 c = cam.WorldToScreenPoint(new Vector3(b.max.x, b.max.y, b.center.z));
        float x0 = Mathf.Min(a.x, c.x), x1 = Mathf.Max(a.x, c.x);
        float y0 = Mathf.Min(a.y, c.y), y1 = Mathf.Max(a.y, c.y);
        const float pad = 4f;
        x0 -= pad; y0 -= pad; x1 += pad; y1 += pad;

        canvas.gameObject.SetActive(true);
        frame.gameObject.SetActive(drawFrame);
        frame.anchoredPosition = new Vector2(x0, y0);
        frame.sizeDelta = new Vector2(x1 - x0, y1 - y0);

        label.text = Text(kind);
        label.ForceMeshUpdate();
        Vector2 size = label.GetPreferredValues();
        labelBox.sizeDelta = new Vector2(size.x + 28f, size.y + 12f);

        // label above the frame (below it if there is no room), kept inside the screen
        float lx = Mathf.Clamp((x0 + x1) * 0.5f - labelBox.sizeDelta.x * 0.5f, 4f, Screen.width - labelBox.sizeDelta.x - 4f);
        float ly = y1 + 6f;
        if (ly + labelBox.sizeDelta.y > Screen.height - 4f) ly = y0 - labelBox.sizeDelta.y - 6f;
        labelBox.anchoredPosition = new Vector2(lx, ly);
    }

    private void Hide()
    {
        HideOutline();
        if (canvas != null && canvas.gameObject.activeSelf) canvas.gameObject.SetActive(false);
    }

    private void Build()
    {
        GameObject cgo = new GameObject("HoverCanvas");
        cgo.transform.SetParent(transform, false);
        canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;   // above the game UI, below the puzzle windows (300)
        // no CanvasScaler: 1 canvas unit = 1 screen pixel, so the frame lines up with the object

        GameObject f = new GameObject("Frame", typeof(RectTransform));
        f.transform.SetParent(cgo.transform, false);
        frame = (RectTransform)f.transform;
        frame.anchorMin = frame.anchorMax = Vector2.zero;
        frame.pivot = Vector2.zero;

        fill = MakeImage("Fill", frame, new Color(Cyan.r, Cyan.g, Cyan.b, 0.03f));
        Stretch(fill.rectTransform, 0, 0, 0, 0);
        const float t = 3f;
        edges[0] = MakeImage("Top", frame, Cyan);      SetEdge(edges[0], 0, 1, 1, 1, 0, -t, 0, 0);
        edges[1] = MakeImage("Bottom", frame, Cyan);   SetEdge(edges[1], 0, 0, 1, 0, 0, 0, 0, t);
        edges[2] = MakeImage("Left", frame, Cyan);     SetEdge(edges[2], 0, 0, 0, 1, 0, 0, t, 0);
        edges[3] = MakeImage("Right", frame, Cyan);    SetEdge(edges[3], 1, 0, 1, 1, -t, 0, 0, 0);

        GameObject lb = new GameObject("Label", typeof(RectTransform));
        lb.transform.SetParent(cgo.transform, false);
        labelBox = (RectTransform)lb.transform;
        labelBox.anchorMin = labelBox.anchorMax = Vector2.zero;
        labelBox.pivot = Vector2.zero;
        Image bg = lb.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.06f, 0.06f, 0.92f);
        bg.raycastTarget = false;

        GameObject tx = new GameObject("Text", typeof(RectTransform));
        tx.transform.SetParent(lb.transform, false);
        label = tx.AddComponent<TextMeshProUGUI>();
        label.fontSize = 28;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 1f, 1f);
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        Stretch(label.rectTransform, 0, 0, 0, 0);

        cgo.SetActive(false);
    }

    private static Image MakeImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    // anchors (minX,minY,maxX,maxY) + offsets (minX,minY,maxX,maxY)
    private static void SetEdge(Image img, float ax0, float ay0, float ax1, float ay1, float ox0, float oy0, float ox1, float oy1)
    {
        RectTransform r = img.rectTransform;
        r.anchorMin = new Vector2(ax0, ay0);
        r.anchorMax = new Vector2(ax1, ay1);
        r.offsetMin = new Vector2(ox0, oy0);
        r.offsetMax = new Vector2(ox1, oy1);
    }

    private static void Stretch(RectTransform rt, float l, float b, float r, float t)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(l, b);
        rt.offsetMax = new Vector2(-r, -t);
    }
}
