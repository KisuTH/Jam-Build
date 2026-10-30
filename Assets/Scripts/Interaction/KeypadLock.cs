using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// A keypad beside a locked door. Click it (Interactable2D -> On Interact -> KeypadLock.Open): the keypad picture
/// opens in the middle of the screen. Type the 4-digit code of this life (the digits found on the boards).
///   keys 1-9, 0 : type   |   *  : clear   |   #  : confirm   |   red button / Esc : close
/// Right code  -> story flag "records_unlocked" is set (the locked door's Interactable2D requires it).
/// Wrong code  -> the AI scolds; after 3 wrong tries the player dies and the loop restarts.
/// The whole window is built at runtime, nothing else is needed in the scene.
/// </summary>
public class KeypadLock : MonoBehaviour
{
    public const string DefaultFlag = "records_unlocked";

    public Sprite keypadSprite;
    public string unlockFlag = DefaultFlag;
    public int maxWrongTries = 3;

    [Header("Fixed code (optional, e.g. the radio room: 021604)")]
    [Tooltip("Empty = the 4-digit code of this life. Filled = this exact code (any length).")]
    public string fixedCode = "";
    [Tooltip("Off = a wrong code only shows ERR and clears (no AI call, no death).")]
    public bool killOnWrong = true;

    private int CodeLen { get { return string.IsNullOrEmpty(fixedCode) ? LoopManager.CodeLength : fixedCode.Length; } }

    [Header("The addict's attacks (look left / right with the arrow keys or the side buttons)")]
    public bool enableAttacks = true;
    public float firstAttackDelay = 6f;
    public float minInterval = 7f;
    public float maxInterval = 12f;
    public float reactionTime = 4f;
    public float stareTime = 0.6f;

    // normalised positions on the keypad picture (314 x 551 px). y counted from the TOP.
    private static readonly Rect ScreenRect = new Rect(55f / 314f, 50f / 551f, 125f / 314f, 50f / 551f);
    private static readonly Rect CloseRect = new Rect(203f / 314f, 42f / 551f, 55f / 314f, 56f / 551f);
    private static readonly float[] ColX = { 38f / 314f, 125f / 314f, 207f / 314f };
    private static readonly float[] RowY = { 185f / 551f, 268f / 551f, 352f / 551f, 440f / 551f };
    private const float KeyW = 74f / 314f, KeyH = 66f / 551f;
    private static readonly string[] Keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "*", "0", "#" };

    private GameObject root;
    private TMP_Text display;
    private string entered = "";
    private bool busy;
    private int wrong;

    private int view;          // -1 = looking left, 0 = keypad, 1 = looking right
    private int attackSide;    // 0 = nobody coming, -1 = from the left, 1 = from the right
    private float nextAttackIn, attackTimer, stare, cueClearTime;
    private TMP_Text cue, sideLabel;
    private GameObject pad, sideView, threat;

    public bool IsOpen => root != null && root.activeSelf;

    public void Open()
    {
        if (StoryFlags.Instance != null && StoryFlags.Instance.Has(unlockFlag))
        {
            if (DialogueUI.Instance != null) DialogueUI.Instance.Show("The lock is already open.");
            return;
        }
        if (LoopManager.Ensure() != null && LoopManager.Ensure().IsDying) return;
        Build();
        entered = "";
        busy = false;
        attackSide = 0;
        nextAttackIn = firstAttackDelay;
        cue.text = "";
        Refresh(Color.black);
        root.SetActive(true);
        SetView(0);
        LoopManager.SetPlayerLocked(true);
    }

    public void Close()
    {
        if (busy) return;
        if (root != null) root.SetActive(false);
        LoopManager.SetPlayerLocked(false);
    }

    private void Update()
    {
        if (!IsOpen || busy) return;
        Keyboard kb = Keyboard.current;
        if (kb == null) return;
        if (kb.escapeKey.wasPressedThisFrame) { if (view != 0) SetView(0); else Close(); return; }
        if (kb.leftArrowKey.wasPressedThisFrame) LookLeft();
        if (kb.rightArrowKey.wasPressedThisFrame) LookRight();
        if (cueClearTime > 0f && Time.time > cueClearTime)
        {
            cueClearTime = 0f;
            if (attackSide == 0) cue.text = "";
        }
        if (enableAttacks) UpdateAttacks();
        if (view != 0) return;
        if (kb.backspaceKey.wasPressedThisFrame) Press("*");
        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) Press("#");
        for (int d = 0; d < 10; d++)
        {
            Key k = (Key)((int)Key.Digit0 + d);
            Key n = (Key)((int)Key.Numpad0 + d);
            if (kb[k].wasPressedThisFrame || kb[n].wasPressedThisFrame) Press(d.ToString());
        }
    }

    private void Press(string key)
    {
        if (busy || view != 0) return;
        if (key == "*") { entered = ""; Refresh(Color.black); return; }
        if (key == "#") { Submit(); return; }
        if (entered.Length >= CodeLen) return;
        entered += key;
        Refresh(Color.black);
    }

    private void Refresh(Color c)
    {
        if (display == null) return;
        string s = "";
        for (int i = 0; i < CodeLen; i++)
            s += (i < entered.Length ? entered[i].ToString() : "_") + (i < CodeLen - 1 ? " " : "");
        display.text = s;
        display.color = c;
    }

    private void Submit()
    {
        if (entered.Length < CodeLen) return;
        busy = true;
        attackSide = 0;
        bool ok = string.IsNullOrEmpty(fixedCode) ? LoopManager.IsCorrect(entered) : entered == fixedCode;
        if (ok) StartCoroutine(Correct());
        else StartCoroutine(Wrong());
    }

    private IEnumerator Correct()
    {
        display.text = "OPEN";
        display.color = new Color(0.05f, 0.45f, 0.1f);
        if (StoryFlags.Instance != null) StoryFlags.Instance.Set(unlockFlag);
        yield return new WaitForSeconds(0.9f);
        busy = false;
        Close();
        if (DialogueUI.Instance != null) DialogueUI.Instance.Show("The lock clicks open.");
    }

    private IEnumerator Wrong()
    {
        wrong++;
        display.text = "ERR";
        display.color = new Color(0.7f, 0.05f, 0.05f);
        CameraShake2D.Shake(0.25f, 0.05f);
        yield return new WaitForSeconds(0.7f);

        if (!killOnWrong)   // e.g. the radio room: just try again
        {
            entered = "";
            Refresh(Color.black);
            busy = false;
            yield break;
        }

        if (wrong >= maxWrongTries)
        {
            root.SetActive(false);
            busy = false;
            LoopManager.Ensure().FailPuzzle(CallSituation.WrongCode);
            yield break;
        }

        root.SetActive(false);
        if (AICaller.Instance != null)
            yield return AICaller.Instance.CallRoutine(CallSituation.WrongCode, 1, true);
        else if (DialogueUI.Instance != null)
            DialogueUI.Instance.Show("WRONG. " + (maxWrongTries - wrong) + " left");
        entered = "";
        Refresh(Color.black);
        root.SetActive(true);
        SetView(0);
        attackSide = 0;
        nextAttackIn = firstAttackDelay;
        busy = false;
    }

    // ===== Looking left / right, the addict =====

    public void LookLeft() { if (!busy) SetView(view == 1 ? 0 : -1); }
    public void LookRight() { if (!busy) SetView(view == -1 ? 0 : 1); }

    private void SetView(int v)
    {
        view = v;
        stare = 0f;
        if (pad != null) pad.SetActive(view == 0);
        if (sideView != null)
        {
            sideView.SetActive(view != 0);
            sideLabel.text = view < 0 ? "< " + Loc.T("Looking left...") : Loc.T("Looking right...") + " >";
        }
        UpdateThreat();
    }

    private void UpdateThreat()
    {
        if (threat == null) return;
        if (attackSide != 0) ShadowEnemyVisual.ApplyToUI(threat, attackSide);
        threat.SetActive(view != 0 && attackSide == view);
    }

    private void UpdateAttacks()
    {
        if (attackSide == 0)
        {
            nextAttackIn -= Time.deltaTime;
            if (nextAttackIn <= 0f)
            {
                attackSide = Random.value < 0.5f ? -1 : 1;
                attackTimer = reactionTime;
                stare = 0f;
                cue.text = Loc.T("...footsteps. Someone is coming. Look around!");
                UpdateThreat();
            }
            return;
        }
        if (view == attackSide)
        {
            stare += Time.deltaTime;
            if (stare >= stareTime)
            {
                attackSide = 0;
                stare = 0f;
                nextAttackIn = Random.Range(minInterval, maxInterval);
                cue.text = Loc.T("He backs away into the dark. Keep going...");
                cueClearTime = Time.time + 2.5f;
                UpdateThreat();
            }
            return;
        }
        stare = 0f;
        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            int side = attackSide;
            attackSide = 0;
            busy = false;
            root.SetActive(false);
            cue.text = "";
            LoopManager.Ensure().Die(side);
        }
    }

    // ===== Window =====

    private void Build()
    {
        if (root != null) return;
        EnsureEventSystem();

        root = new GameObject("KeypadWindow");
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        CanvasScaler cs = root.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();

        // dark backdrop (click outside = close)
        GameObject dim = NewImage("Dim", root.transform, new Color(0, 0, 0, 0.7f));
        Stretch(dim.GetComponent<RectTransform>());
        Button dimBtn = dim.AddComponent<Button>();
        dimBtn.transition = Selectable.Transition.None;
        dimBtn.onClick.AddListener(Close);

        float h = 780f, w = h * 314f / 551f;
        pad = NewImage("Pad", root.transform, Color.white);
        Image padImg = pad.GetComponent<Image>();
        padImg.sprite = keypadSprite;
        if (keypadSprite == null) padImg.color = new Color(0.55f, 0.52f, 0.45f);
        RectTransform pr = pad.GetComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(w, h);
        pr.anchoredPosition = Vector2.zero;

        // display on the white screen
        GameObject d = new GameObject("Display", typeof(RectTransform));
        d.transform.SetParent(pad.transform, false);
        PlaceNormalised(d.GetComponent<RectTransform>(), ScreenRect);
        display = d.AddComponent<TextMeshProUGUI>();
        display.alignment = TextAlignmentOptions.Center;
        display.enableAutoSizing = true;
        display.fontSizeMin = 10; display.fontSizeMax = 60;
        display.fontStyle = FontStyles.Bold;
        display.raycastTarget = false;

        // keys
        for (int i = 0; i < 12; i++)
        {
            string key = Keys[i];
            Rect r = new Rect(ColX[i % 3], RowY[i / 3], KeyW, KeyH);
            MakeButton("Key" + key, pad.transform, r, () => Press(key));
        }
        MakeButton("Close", pad.transform, CloseRect, Close);

        // warning text at the top
        GameObject cg = new GameObject("Cue", typeof(RectTransform));
        cg.transform.SetParent(root.transform, false);
        cue = cg.AddComponent<TextMeshProUGUI>();
        cue.alignment = TextAlignmentOptions.Center;
        cue.fontSize = 40;
        cue.color = new Color(1f, 0.85f, 0.6f);
        cue.raycastTarget = false;
        RectTransform cr = cue.rectTransform;
        cr.anchorMin = new Vector2(0.1f, 1f); cr.anchorMax = new Vector2(0.9f, 1f);
        cr.pivot = new Vector2(0.5f, 1f); cr.anchoredPosition = new Vector2(0, -30); cr.sizeDelta = new Vector2(0, 90);

        // the side view (dark, with the addict when he is there)
        sideView = NewImage("SideView", root.transform, new Color(0.12f, 0.12f, 0.14f, 0.96f));
        Stretch(sideView.GetComponent<RectTransform>());
        sideView.GetComponent<Image>().raycastTarget = false;
        GameObject sl = new GameObject("Label", typeof(RectTransform));
        sl.transform.SetParent(sideView.transform, false);
        sideLabel = sl.AddComponent<TextMeshProUGUI>();
        sideLabel.alignment = TextAlignmentOptions.Center;
        sideLabel.fontSize = 36;
        sideLabel.color = new Color(0.6f, 0.6f, 0.6f);
        sideLabel.raycastTarget = false;
        RectTransform slr = sideLabel.rectTransform;
        slr.anchorMin = new Vector2(0.1f, 0.1f); slr.anchorMax = new Vector2(0.9f, 0.2f);
        slr.offsetMin = slr.offsetMax = Vector2.zero;
        threat = NewImage("Addict", sideView.transform, Color.white);
        RectTransform tr = threat.GetComponent<RectTransform>();
        tr.anchorMin = tr.anchorMax = tr.pivot = new Vector2(0.5f, 0.5f);
        tr.sizeDelta = new Vector2(260, 620);
        ShadowEnemyVisual.ApplyToUI(threat, 1);
        threat.SetActive(false);
        sideView.SetActive(false);

        // side buttons (always on top)
        SideButton("LookLeft", "<", new Vector2(0f, 0.5f), new Vector2(40, 0), LookLeft);
        SideButton("LookRight", ">", new Vector2(1f, 0.5f), new Vector2(-40, 0), LookRight);

        root.SetActive(false);
    }

    private static void SideButton(string name, string txt, Vector2 anchor, Vector2 pos, UnityEngine.Events.UnityAction act)
    {
        // parent is found from the caller's root through the scene: created under 'Canvas' of the window
        Transform parent = GameObject.Find("KeypadWindow").transform;
        GameObject g = NewImage(name, parent, new Color(0.25f, 0.1f, 0.1f, 0.85f));
        RectTransform r = g.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = r.pivot = anchor;
        r.sizeDelta = new Vector2(90, 220);
        r.anchoredPosition = pos + new Vector2(anchor.x < 0.5f ? 45 : -45, 0);
        g.AddComponent<Button>().onClick.AddListener(act);
        GameObject t = new GameObject("Text", typeof(RectTransform));
        t.transform.SetParent(g.transform, false);
        TMP_Text tx = t.AddComponent<TextMeshProUGUI>();
        tx.text = txt; tx.fontSize = 70; tx.alignment = TextAlignmentOptions.Center; tx.raycastTarget = false;
        RectTransform tr = tx.rectTransform;
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = tr.offsetMax = Vector2.zero;
    }

    private static void MakeButton(string name, Transform parent, Rect r, UnityEngine.Events.UnityAction act)
    {
        GameObject g = NewImage(name, parent, Color.white);
        PlaceNormalised(g.GetComponent<RectTransform>(), r);
        Button b = g.AddComponent<Button>();
        ColorBlock cb = b.colors;
        cb.normalColor = new Color(1, 1, 1, 0f);
        cb.highlightedColor = new Color(1, 1, 1, 0.14f);
        cb.pressedColor = new Color(1, 1, 1, 0.35f);
        cb.selectedColor = new Color(1, 1, 1, 0f);
        cb.fadeDuration = 0.05f;
        b.colors = cb;
        b.onClick.AddListener(act);
    }

    private static GameObject NewImage(string name, Transform parent, Color c)
    {
        GameObject g = new GameObject(name, typeof(RectTransform), typeof(Image));
        g.transform.SetParent(parent, false);
        g.GetComponent<Image>().color = c;
        return g;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // r: x,y from the LEFT / TOP, in 0..1 of the parent
    private static void PlaceNormalised(RectTransform rt, Rect r)
    {
        rt.anchorMin = new Vector2(r.x, 1f - r.y - r.height);
        rt.anchorMax = new Vector2(r.x + r.width, 1f - r.y);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    public static void EnsureEventSystem()
    {
        EventSystem es = EventSystem.current != null ? EventSystem.current : Object.FindAnyObjectByType<EventSystem>();
        if (es == null)
        {
            GameObject g = new GameObject("EventSystem");
            es = g.AddComponent<EventSystem>();
        }
        if (es.GetComponent<InputSystemUIInputModule>() == null && es.GetComponent<BaseInputModule>() == null)
        {
            InputSystemUIInputModule m = es.gameObject.AddComponent<InputSystemUIInputModule>();
            m.AssignDefaultActions();
        }
    }
}
