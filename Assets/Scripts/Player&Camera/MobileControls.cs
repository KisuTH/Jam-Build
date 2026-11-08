using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Touch controls for phones / tablets (e.g. the itch.io web build opened on a phone).
/// Creates itself - no scene setup needed. Nothing is shown on a desktop with mouse + keyboard.
///
///   [<] [>]  = walk left / right (hold)          [^] = jump          [v] = crouch (hold)
///   Tap an object in the world = interact (same as a mouse click).
///
/// Other scripts use:  MobileControls.Active / Move / Crouch / ConsumeJump()  and  MobileControls.TapThisFrame(out pos)
/// In the Unity Editor, press F10 in Play mode to show the buttons for testing (then click them with the mouse).
/// </summary>
[DefaultExecutionOrder(-100)]
public class MobileControls : MonoBehaviour
{
    // ---------- read by other scripts ----------

    /// <summary>True on a touch device (or after the first touch was seen, or F10 in the Editor).</summary>
    public static bool Active { get; private set; }
    /// <summary>-1 (left) .. +1 (right).</summary>
    public static float Move { get; private set; }
    public static bool Crouch { get; private set; }

    /// <summary>Saved choice from Settings > CONTROL: -1 = automatic (touch device), 0 = PC, 1 = Mobile.</summary>
    public const string ModePref = "ControlMode";
    private static int modeOverride = -1;

    /// <summary>Called by the Settings page: true = show the touch buttons, false = keyboard + mouse only.</summary>
    public static void SetMobileMode(bool mobile)
    {
        modeOverride = mobile ? 1 : 0;
        try { PlayerPrefs.SetInt(ModePref, modeOverride); PlayerPrefs.Save(); } catch { }
        Active = mobile;
        if (!mobile) { Move = 0f; Crouch = false; jumpDown = false; }
    }

    private static bool jumpDown;
    /// <summary>True once per jump-button press.</summary>
    public static bool ConsumeJump()
    {
        bool j = jumpDown;
        jumpDown = false;
        return j;
    }

    /// <summary>
    /// True on the frame a finger touches the screen (anywhere except on these buttons).
    /// Works with several fingers, so tapping an object while holding a walk button is fine.
    /// </summary>
    public static bool TapThisFrame(out Vector2 screenPos, out int touchId)
    {
        screenPos = Vector2.zero;
        touchId = -1;
        Touchscreen ts = Touchscreen.current;
        if (ts == null) return false;
        foreach (var t in ts.touches)
        {
            if (!t.press.wasPressedThisFrame) continue;
            Vector2 p = t.position.ReadValue();
            if (instance != null && instance.OverButton(p)) continue;
            screenPos = p;
            touchId = t.touchId.ReadValue();
            return true;
        }
        return false;
    }

    // ---------- setup ----------

    private static MobileControls instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (instance != null) return;
        GameObject go = new GameObject("MobileControls");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<MobileControls>();
    }

    private enum Btn { Left, Right, Jump, Crouch, Interact }

    private GameObject canvasRoot;
    private RectTransform safeRoot;   // the buttons live inside the phone's safe area (not under the notch / rounded corners)
    private Rect lastSafe;
    private readonly RectTransform[] rects = new RectTransform[5];
    private readonly Image[] images = new Image[5];
    private readonly bool[] held = new bool[5];
    private PlayerMovement2D player;
    private TextMeshProUGUI interactLabel;
    private float nextFind;
    private bool forced;

    private void Awake()
    {
        modeOverride = PlayerPrefs.GetInt(ModePref, -1);
        Active = modeOverride == 1 || (modeOverride < 0 && Application.isMobilePlatform);
        Build();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        Active = false; Move = 0f; Crouch = false; jumpDown = false;
    }

    private void Build()
    {
        canvasRoot = new GameObject("MobileControlsCanvas");
        canvasRoot.transform.SetParent(transform, false);
        Canvas canvas = canvasRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler cs = canvasRoot.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;
        canvasRoot.AddComponent<GraphicRaycaster>();

        GameObject safe = new GameObject("SafeArea", typeof(RectTransform));
        safe.transform.SetParent(canvasRoot.transform, false);
        safeRoot = (RectTransform)safe.transform;
        ApplySafeArea();

        Sprite circle = MakeCircle(128);
        // padX keeps the buttons away from the screen edge, padY lifts them above the dialogue box at the bottom
        float size = 170f, padX = 110f, padY = 260f, gap = 30f;
        // walking: bottom-left     jump / crouch: bottom-right
        MakeButton(Btn.Left,   "<", circle, size, new Vector2(0, 0), new Vector2(padX, padY));
        MakeButton(Btn.Right,  ">", circle, size, new Vector2(0, 0), new Vector2(padX + size + gap, padY));
        MakeButton(Btn.Jump,   "^", circle, size, new Vector2(1, 0), new Vector2(-padX, padY + size + gap));
        MakeButton(Btn.Crouch, "v", circle, size, new Vector2(1, 0), new Vector2(-padX, padY));
        // Interact: left of the crouch button; only visible while something can be used
        MakeButton(Btn.Interact, "!", circle, size, new Vector2(1, 0), new Vector2(-padX - size - gap, padY));
        interactLabel = rects[(int)Btn.Interact].GetComponentInChildren<TextMeshProUGUI>();
        JamBuild.Options.NotebookTextSettings.Apply(interactLabel);
        interactLabel.enableAutoSizing = true;
        interactLabel.fontSizeMin = 24; interactLabel.fontSizeMax = 56;
        interactLabel.margin = new Vector4(10, 10, 10, 10);
        canvasRoot.SetActive(false);
    }

    private void MakeButton(Btn b, string label, Sprite sprite, float size, Vector2 anchor, Vector2 pos)
    {
        GameObject g = new GameObject(b + "Button", typeof(RectTransform), typeof(Image));
        g.transform.SetParent(safeRoot, false);
        RectTransform r = g.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = anchor;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(size, size);
        // anchored at a corner: x grows from the left edge / shrinks from the right edge, y grows upward
        r.anchoredPosition = new Vector2(anchor.x < 0.5f ? pos.x + size * 0.5f : pos.x - size * 0.5f, pos.y + size * 0.5f);
        Image img = g.GetComponent<Image>();
        img.sprite = sprite;
        img.color = new Color(0f, 0f, 0f, 0.35f);   // also blocks UI raycasts, so a tap on it never reaches the game behind
        images[(int)b] = img;
        rects[(int)b] = r;

        GameObject t = new GameObject("Label", typeof(RectTransform));
        t.transform.SetParent(g.transform, false);
        RectTransform tr = t.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = tr.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = t.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 110;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(1f, 1f, 1f, 0.8f);
        tmp.raycastTarget = false;
    }

    private static Sprite MakeCircle(int n)
    {
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float c = (n - 1) * 0.5f, rad = n * 0.5f - 1f;
        Color32[] px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                byte a = (byte)(255f * Mathf.Clamp01(rad - d + 1f));   // soft 1px edge
                px[y * n + x] = new Color32(255, 255, 255, a);
            }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }

    // ---------- every frame ----------

    private void ApplySafeArea()
    {
        Rect sa = Screen.safeArea;
        if (Screen.width <= 0 || Screen.height <= 0 || safeRoot == null) return;
        lastSafe = sa;
        safeRoot.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
        safeRoot.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
        safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
    }

    private void Update()
    {
        Touchscreen ts = Touchscreen.current;
        if (Screen.safeArea != lastSafe) ApplySafeArea();

#if UNITY_EDITOR
        if (Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame) { forced = !forced; Active = forced || Application.isMobilePlatform; }
#endif
        // a laptop with a touch screen: show the buttons only after the first real touch
        if (!Active && modeOverride < 0 && ts != null && ts.primaryTouch.press.wasPressedThisFrame) Active = true;

        // show only while the player can actually walk (not in menus, puzzles, dialogue, cutscenes)
        if (Time.unscaledTime >= nextFind)
        {
            nextFind = Time.unscaledTime + 0.5f;
            if (player == null) player = FindFirstObjectByType<PlayerMovement2D>();
        }
        bool show = Active && player != null && player.isActiveAndEnabled && !player.IsControlLocked && Time.timeScale > 0f;
        if (canvasRoot.activeSelf != show) canvasRoot.SetActive(show);

        bool jumpWas = held[(int)Btn.Jump];
        bool interactWas = held[(int)Btn.Interact];
        for (int i = 0; i < held.Length; i++) held[i] = false;

        if (show)
        {
            if (ts != null)
                foreach (var t in ts.touches)
                {
                    if (!t.press.isPressed) continue;
                    MarkHeld(t.position.ReadValue());
                }
            if ((forced || (modeOverride == 1 && !Application.isMobilePlatform)) && Mouse.current != null && Mouse.current.leftButton.isPressed)
                MarkHeld(Mouse.current.position.ReadValue());
        }

        Move = (held[(int)Btn.Right] ? 1f : 0f) - (held[(int)Btn.Left] ? 1f : 0f);
        Crouch = held[(int)Btn.Crouch];
        if (held[(int)Btn.Jump] && !jumpWas) jumpDown = true;   // a new press, not a held finger
        if (!show) jumpDown = false;

        // Interact button: visible only when an object is in reach; a new press uses it
        Interactable2D tgt = show ? HoverHighlight.MobileTarget : null;
        bool showInteract = tgt != null;
        GameObject ib = rects[(int)Btn.Interact].gameObject;
        if (ib.activeSelf != showInteract) ib.SetActive(showInteract);
        if (showInteract)
        {
            if (interactLabel != null && interactLabel.text != HoverHighlight.MobileLabel) interactLabel.text = HoverHighlight.MobileLabel;
            if (held[(int)Btn.Interact] && !interactWas) { tgt.Interact(player.transform); HoverHighlight.NoteUsed(tgt, player.transform.position.x); }
        }
        else held[(int)Btn.Interact] = false;

        for (int i = 0; i < images.Length; i++)
            images[i].color = new Color(held[i] ? 1f : 0f, held[i] ? 1f : 0f, held[i] ? 1f : 0f, held[i] ? 0.45f : 0.35f);
    }

    private void MarkHeld(Vector2 p)
    {
        for (int i = 0; i < rects.Length; i++)
            if (rects[i].gameObject.activeSelf && RectTransformUtility.RectangleContainsScreenPoint(rects[i], p, null)) held[i] = true;
    }

    private bool OverButton(Vector2 p)
    {
        if (!canvasRoot.activeSelf) return false;
        for (int i = 0; i < rects.Length; i++)
            if (rects[i].gameObject.activeSelf && RectTransformUtility.RectangleContainsScreenPoint(rects[i], p, null)) return true;
        return false;
    }
}
