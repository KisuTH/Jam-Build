using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Wound-care table puzzle ("ห้องทำแผลอนามัย"). The window is built in code (no scene setup needed):
/// call WoundPuzzleUI.Open() from an Interactable2D's "On Interact" (use WoundPuzzleUI.OpenStatic via a
/// WoundPuzzleOpener component, or the builder does it for you).
///
/// The player sees the table and must click the tools in the order written on the instruction paper:
///   1 tray  ->  2 bandage roll  ->  3 scissors  ->  4 medicine bottle.
/// Wrong tool / wrong order = a message and the choice resets (no death, just try again).
/// When the 4 are right a "Open the drawer" button appears: it gives the evidence item,
/// logs the Case Book clue and sets the story flag "wound_solved".
/// </summary>
public class WoundPuzzleUI : MonoBehaviour
{
    public const string SolvedFlag = "wound_solved";
    
    private static WoundPuzzleUI instance;

    // Tools: the item picture (sprite name in Resources/WoundPuzzle/wound_items), normalised centre on the table
    // picture (0..1, origin top-left), width as a fraction of the picture width, and which step it is (0 = decoy).
    // Correct items fly onto the tray (item_0): "land" = where, "landScale" = how big.
    private struct Tool
    {
        public string en, th, sprite; public float x, y, w; public int step; public Vector2 land; public float landScale;
        public Tool(string en, string th, string sprite, float x, float y, float w, int step, float lx = 0f, float ly = 0f, float ls = 0.7f)
        { this.en = en; this.th = th; this.sprite = sprite; this.x = x; this.y = y; this.w = w; this.step = step;
          this.land = new Vector2(lx, ly); this.landScale = ls; }
    }

    private static readonly Tool[] Tools =
    {
        new Tool("Tray",            "ถาด",               "item_0",  0.30f, 0.40f, 0.30f, 1),
        new Tool("Bandage roll",    "ผ้าพันแผล",         "item_2",  0.64f, 0.30f, 0.11f, 2, 0.24f, 0.40f),
        new Tool("Scissors",        "กรรไกร",            "item_4",  0.66f, 0.52f, 0.12f, 3, 0.30f, 0.43f),
        new Tool("Medicine bottle", "ยา / น้ำยาฆ่าเชื้อ", "item_3",  0.77f, 0.33f, 0.06f, 4, 0.37f, 0.40f),
        new Tool("Gauze pads",      "ผ้าก๊อซ",           "item_8",  0.26f, 0.68f, 0.13f, 0),
        new Tool("Dressing patch",  "แผ่นปิดแผล",        "item_1",  0.50f, 0.40f, 0.12f, 0),
        new Tool("Antiseptic box",  "กล่องยาฆ่าเชื้อ",   "item_10", 0.45f, 0.67f, 0.14f, 0),
        new Tool("Old papers",      "กระดาษเก่า",        "item_6",  0.80f, 0.60f, 0.13f, 0),
    };

    private const float PicW = 1280f, PicH = 720f;   // the table picture on screen (16:9)

    private static readonly string[] StepEn = { "Prepare the tray", "Place the bandage", "Prepare the wound tools", "Prepare the medicine" };
    private static readonly string[] StepTh = { "เตรียมถาด", "ใส่ผ้าพันแผล", "เตรียมอุปกรณ์ทำแผล", "เตรียมยา / น้ำยาฆ่าเชื้อ" };

    private GameObject root;
    private TMP_Text messageText;
    private TMP_Text[] slotTexts = new TMP_Text[4];
    private GameObject[] marks;               // the green highlight + number on each tool
    private TMP_Text[] markNumbers;
    private Sprite tableSprite;
    private RectTransform[] toolRects;
    private Sprite[] itemSprites;
    private Vector2[] homePos;

    private int next = 1;                     // which step the player must do now
    private bool solved;
    private bool busy;

    // ---------- opening / closing ----------

    public static void OpenStatic()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("WoundPuzzleUI");
            instance = go.AddComponent<WoundPuzzleUI>();
        }
        instance.Open();
    }

    private void OnDestroy() { Loc.Changed -= RefreshAll; if (instance == this) instance = null; }

    private void Open()
    {
        if (root == null) Build();
        solved = StoryFlags.Instance != null && StoryFlags.Instance.Has(SolvedFlag);
        ResetChoices();
        if (solved)
        {
            for (int i = 0; i < 4; i++) Mark(ToolIndexOfStep(i + 1), i + 1, true);
            next = 5;
            FillSlots(4);
            SetMessage("Already solved. The drawer in this room is unlocked.",
                       "แก้ไปแล้ว ลิ้นชักในห้องนี้ถูกปลดล็อกแล้ว");
        }
        else
        {
            SetMessage("Arrange the tools in the order on the instruction paper. Click a tool.",
                       "เรียงอุปกรณ์ให้ถูกลำดับตามเอกสาร คลิกที่อุปกรณ์");
        }
        root.SetActive(true);
        LoopManager.SetPlayerLocked(true);
        if (DialogueUI.Instance != null) DialogueUI.Instance.Hide();
    }

    public void Close()
    {
        if (root != null) root.SetActive(false);
        LoopManager.SetPlayerLocked(false);
    }

    private void Update()
    {
        if (root != null && root.activeSelf && Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
        UpdateHover();
    }

    private Outline[] outlines;
    private RectTransform hoverRect;
    private TMP_Text hoverText;

    // white outline + the item's name under the cursor
    private void UpdateHover()
    {
        if (outlines == null || hoverRect == null || root == null || !root.activeSelf) return;
        int hit = -1;
        if (!solved && Mouse.current != null)
        {
            Vector2 mp = Mouse.current.position.ReadValue();
            for (int i = toolRects.Length - 1; i >= 0; i--)    // the item drawn on top wins
                if (RectTransformUtility.RectangleContainsScreenPoint(toolRects[i], mp)) { hit = i; break; }
        }
        for (int i = 0; i < outlines.Length; i++) if (outlines[i] != null) outlines[i].enabled = (i == hit);
        if (hit < 0) { hoverRect.gameObject.SetActive(false); return; }

        hoverRect.gameObject.SetActive(true);
        hoverText.text = L(Tools[hit].en, Tools[hit].th);
        RectTransform r = toolRects[hit];
        hoverRect.anchoredPosition = new Vector2(r.anchoredPosition.x, r.anchoredPosition.y - r.sizeDelta.y * r.localScale.y * 0.5f - 8f);
        hoverRect.SetAsLastSibling();
    }

    // ---------- puzzle logic ----------

    private void ResetChoices()
    {
        StopAllCoroutines();
        busy = false;
        next = 1;
        for (int i = 0; i < marks.Length; i++)
        {
            marks[i].SetActive(false);
            toolRects[i].anchoredPosition = homePos[i];     // everything goes back onto the table
            toolRects[i].localScale = Vector3.one;
        }
        FillSlots(0);
    }

    private void Click(int toolIndex)
    {
        if (busy || solved || next > 4) return;
        Tool t = Tools[toolIndex];

        if (t.step == next)
        {
            Mark(toolIndex, next);
            FillSlots(next);
            next++;
            if (next > 4)
            {
                StartCoroutine(Finish());
            }
            else
            {
                SetMessage("Good. Next: step " + next + ".", "ถูกต้อง ต่อไป: ขั้นที่ " + next);
            }
        }
        else
        {
            string en = t.step == 0 ? "\"" + t.en + "\" is not on the instruction paper." : "Wrong order. Start again.";
            string th = t.step == 0 ? "\"" + t.th + "\" ไม่ได้อยู่ในเอกสารคำสั่ง" : "ลำดับไม่ถูกต้อง เริ่มใหม่";
            StartCoroutine(Wrong(toolIndex, en, th));
        }
    }

    private IEnumerator Wrong(int toolIndex, string en, string th)
    {
        busy = true;
        SetMessage(en, th);
        marks[toolIndex].SetActive(true);
        markNumbers[toolIndex].text = "X";
        marks[toolIndex].GetComponent<Image>().color = new Color(0.8f, 0.1f, 0.1f, 0.45f);
        yield return new WaitForSecondsRealtime(0.9f);
        ResetChoices();
        SetMessage(en + "  (Read the paper in the room again.)", th + "  (ลองกลับไปอ่านกระดาษในห้องอีกครั้ง)");
    }

    private void Mark(int toolIndex, int number, bool instant = false)
    {
        if (toolIndex < 0) return;
        Tool t = Tools[toolIndex];
        bool flies = t.step >= 2;   // the tray stays; the other tools are put on it
        marks[toolIndex].SetActive(true);
        marks[toolIndex].GetComponent<Image>().color = flies ? new Color(0.2f, 0.8f, 0.3f, 0f) : new Color(0.2f, 0.8f, 0.3f, 0.35f);
        markNumbers[toolIndex].text = number.ToString();
        if (!flies) return;
        Vector2 to = new Vector2(t.land.x * PicW, -t.land.y * PicH);
        if (instant)
        {
            toolRects[toolIndex].anchoredPosition = to;
            toolRects[toolIndex].localScale = Vector3.one * t.landScale;
        }
        else StartCoroutine(Fly(toolRects[toolIndex], to, t.landScale));
    }

    // the tool slides onto the tray and gets a bit smaller
    private IEnumerator Fly(RectTransform rt, Vector2 to, float scale)
    {
        Vector2 from = rt.anchoredPosition;
        float s0 = rt.localScale.x, t = 0f, dur = 0.35f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
            rt.anchoredPosition = Vector2.Lerp(from, to, k);
            rt.localScale = Vector3.one * Mathf.Lerp(s0, scale, k);
            yield return null;
        }
        rt.anchoredPosition = to;
        rt.localScale = Vector3.one * scale;
    }

    private static int ToolIndexOfStep(int step)
    {
        for (int i = 0; i < Tools.Length; i++) if (Tools[i].step == step) return i;
        return -1;
    }

    private void FillSlots(int count)
    {
        for (int i = 0; i < 4; i++)
        {
            bool done = i < count;
            if (done) Bind(slotTexts[i], (i + 1) + ".  " + StepEn[i], (i + 1) + ".  " + StepTh[i]);
            else Bind(slotTexts[i], (i + 1) + ".  ?", (i + 1) + ".  ?");
            slotTexts[i].color = done ? new Color(0.6f, 1f, 0.65f) : new Color(0.55f, 0.55f, 0.55f);
        }
    }

    // All 4 right: unlock the drawer (story flag), close the window and tell the player to go and look
    private IEnumerator Finish()
    {
        solved = true;
        busy = true;
        if (StoryFlags.Instance != null) StoryFlags.Instance.Set(SolvedFlag);
        SetMessage("Everything is in order. A drawer in this room unlocks with a click.",
                   "ครบทุกอย่างตามลำดับแล้ว ลิ้นชักในห้องนี้ปลดล็อกดัง \"แกร๊ก\"");
        yield return new WaitForSecondsRealtime(1.6f);
        Close();
        if (DialogueUI.Instance != null)
            DialogueUI.Instance.Show(L("The drawer clicked open... It looks like there is something inside. I should go and take a look.",
                                       "ลิ้นชักเปิดออกแล้ว... เหมือนจะมีบางอย่างอยู่ข้างใน ไปดูหน่อยดีกว่า"));
    }

    // ---------- helpers ----------

    private static string L(string en, string th) { return Loc.Thai ? Loc.Shape(th) : en; }

    private void SetMessage(string en, string th) { if (messageText != null) Bind(messageText, en, th); }

    // Every text keeps its English + Thai version, so the window follows the language button at any time
    private readonly System.Collections.Generic.Dictionary<TMP_Text, string[]> bound =
        new System.Collections.Generic.Dictionary<TMP_Text, string[]>();

    private void Bind(TMP_Text t, string en, string th)
    {
        if (t == null) return;
        bound[t] = new[] { en, th };
        t.text = L(en, th);
    }

    private void RefreshAll()
    {
        foreach (var kv in bound) if (kv.Key != null) kv.Key.text = L(kv.Value[0], kv.Value[1]);
    }

    // ---------- building the window ----------

    private void Build()
    {
        tableSprite = LoadWhole("WoundPuzzle/wound_table_empty");
        if (tableSprite == null)
        {
            Debug.LogWarning("WoundPuzzleUI: pictures not found. Run  Tools > Rain > Setup Wound Puzzle Pictures.");
            tableSprite = Resources.Load<Sprite>("WoundPuzzle/wound_table");
        }
        itemSprites = Resources.LoadAll<Sprite>("WoundPuzzle/wound_items");

        if (EventSystem.current == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        root = new GameObject("WoundPuzzleCanvas");
        root.transform.SetParent(transform, false);
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();

        // dark background (blocks clicks on the game behind)
        Image dim = NewImage("Dim", root.transform, new Color(0.04f, 0.04f, 0.05f, 0.96f));
        Stretch(dim.rectTransform);

        TMP_Text title = NewText("Title", root.transform, "", 44, TextAlignmentOptions.Left);
        Bind(title, "Emergency dressing room: arrange the tools", "ห้องทำแผลอนามัย: จัดอุปกรณ์ทำแผล");
        Place(title.rectTransform, new Vector2(0, 1), new Vector2(60, -40), new Vector2(1500, 70));

        TMP_Text sub = NewText("Sub", root.transform, "", 26, TextAlignmentOptions.Left);
        sub.color = new Color(0.7f, 0.7f, 0.72f);
        Bind(sub, "Follow the order on the instruction paper to open the drawer and get the evidence.",
                  "เรียงอุปกรณ์ให้ถูกต้องตามลำดับที่ระบุในเอกสาร เพื่อเปิดลิ้นชักและได้หลักฐาน");
        Place(sub.rectTransform, new Vector2(0, 1), new Vector2(60, -100), new Vector2(1500, 44));

        // the picture of the table
        GameObject picGO = new GameObject("Table", typeof(RectTransform));
        picGO.transform.SetParent(root.transform, false);
        Image pic = picGO.AddComponent<Image>();
        pic.sprite = tableSprite;
        pic.color = tableSprite != null ? Color.white : new Color(0.3f, 0.3f, 0.3f);
        RectTransform pr = pic.rectTransform;
        pr.anchorMin = pr.anchorMax = new Vector2(0, 1);
        pr.pivot = new Vector2(0, 1);
        pr.anchoredPosition = new Vector2(60, -160);
        pr.sizeDelta = new Vector2(PicW, PicH);   // 16:9, like the picture

        marks = new GameObject[Tools.Length];
        markNumbers = new TMP_Text[Tools.Length];
        toolRects = new RectTransform[Tools.Length];
        homePos = new Vector2[Tools.Length];
        outlines = new Outline[Tools.Length];
        for (int i = 0; i < Tools.Length; i++) BuildHotspot(pr, i);

        // the name that shows under the item the cursor is on (always on top of the items)
        Image tipBg = NewImage("HoverName", pr, new Color(0f, 0f, 0f, 0.8f));
        tipBg.raycastTarget = false;
        hoverRect = tipBg.rectTransform;
        hoverRect.anchorMin = hoverRect.anchorMax = new Vector2(0f, 1f);
        hoverRect.pivot = new Vector2(0.5f, 1f);
        hoverRect.sizeDelta = new Vector2(360, 56);
        hoverText = NewText("Text", tipBg.transform, "", 30, TextAlignmentOptions.Center);
        hoverText.raycastTarget = false;
        hoverText.enableAutoSizing = true;
        hoverText.fontSizeMin = 14; hoverText.fontSizeMax = 30;
        Stretch(hoverText.rectTransform);
        tipBg.gameObject.SetActive(false);

        // right panel: the 4 steps
        Image panel = NewImage("Steps", root.transform, new Color(0.10f, 0.10f, 0.12f, 1f));
        RectTransform pnl = panel.rectTransform;
        pnl.anchorMin = pnl.anchorMax = new Vector2(1, 1);
        pnl.pivot = new Vector2(1, 1);
        pnl.anchoredPosition = new Vector2(-60, -160);
        pnl.sizeDelta = new Vector2(480, 520);

        TMP_Text head = NewText("Head", panel.transform, "", 32, TextAlignmentOptions.Left);
        Bind(head, "Required order", "ลำดับที่ต้องจัด");
        Place(head.rectTransform, new Vector2(0, 1), new Vector2(24, -18), new Vector2(430, 50));
        for (int i = 0; i < 4; i++)
        {
            slotTexts[i] = NewText("Slot" + i, panel.transform, "", 30, TextAlignmentOptions.Left);
            Place(slotTexts[i].rectTransform, new Vector2(0, 1), new Vector2(24, -90 - i * 90), new Vector2(430, 70));
        }
        FillSlots(0);

        // message line
        messageText = NewText("Message", root.transform, "", 32, TextAlignmentOptions.Left);
        messageText.color = new Color(0.95f, 0.9f, 0.7f);
        Place(messageText.rectTransform, new Vector2(0, 0), new Vector2(60, 150), new Vector2(1500, 90));

        // buttons
        GameObject closeBtn = NewButton("CloseButton", root.transform, L("Close (Esc)", "ปิด (Esc)"),
            new Color(0.35f, 0.15f, 0.15f), Close, new Vector2(1, 1), new Vector2(-60, -40), new Vector2(260, 70));
        Bind(closeBtn.GetComponentInChildren<TMP_Text>(), "Close (Esc)", "ปิด (Esc)");

        Loc.Changed += RefreshAll;
        root.SetActive(false);
    }

    private Sprite FindItem(string name)
    {
        if (itemSprites != null)
            foreach (Sprite sp in itemSprites) if (sp != null && sp.name == name) return sp;
        return null;
    }

    // a sprite that covers the whole texture (the picture may have been imported as a "multiple" sprite)
    private static Sprite LoadWhole(string path)
    {
        foreach (Sprite sp in Resources.LoadAll<Sprite>(path))
            if (sp != null && Mathf.Approximately(sp.rect.width, sp.texture.width) && Mathf.Approximately(sp.rect.height, sp.texture.height))
                return sp;
        Texture2D tex = Resources.Load<Texture2D>(path);
        return tex == null ? null : Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private void BuildHotspot(RectTransform parent, int index)
    {
        Tool t = Tools[index];
        GameObject go = new GameObject("Tool: " + t.en, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        Sprite sp = FindItem(t.sprite);
        img.sprite = sp;
        img.preserveAspect = true;
        img.color = sp != null ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.6f);   // grey box if the picture is missing
        float w = t.w * PicW;
        float h = sp != null ? w * sp.rect.height / sp.rect.width : w;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        homePos[index] = new Vector2(t.x * PicW, -t.y * PicH);
        rt.anchoredPosition = homePos[index];
        toolRects[index] = rt;

        Outline ol = img.gameObject.AddComponent<Outline>();
        ol.effectColor = Color.white;
        ol.effectDistance = new Vector2(5f, 5f);
        ol.enabled = false;
        outlines[index] = ol;

        Button b = go.AddComponent<Button>();
        b.targetGraphic = img;
        b.transition = Selectable.Transition.None;
        int captured = index;
        b.onClick.AddListener(() => Click(captured));

        // the green "picked" overlay with the step number
        Image mark = NewImage("Mark", go.transform, new Color(0.2f, 0.8f, 0.3f, 0.35f));
        Stretch(mark.rectTransform);
        mark.raycastTarget = false;
        TMP_Text num = NewText("Number", mark.transform, "", 64, TextAlignmentOptions.Center);
        num.raycastTarget = false;
        Stretch(num.rectTransform);
        marks[index] = mark.gameObject;
        markNumbers[index] = num;
        mark.gameObject.SetActive(false);
    }

    private static Image NewImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private static TMP_Text NewText(string name, Transform parent, string text, float size, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        JamBuild.Options.NotebookTextSettings.Apply(t);
        t.alignment = align;
        t.color = new Color(0.92f, 0.92f, 0.92f);
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        return t;
    }

    private static GameObject NewButton(string name, Transform parent, string label, Color color,
        UnityEngine.Events.UnityAction onClick, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        Image img = NewImage(name, parent, color);
        RectTransform rt = img.rectTransform;
        Place(rt, anchor, pos, size);
        rt.pivot = anchor;
        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.onClick.AddListener(onClick);
        TMP_Text t = NewText("Label", img.transform, label, 32, TextAlignmentOptions.Center);
        Stretch(t.rectTransform);
        return img.gameObject;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // anchor + offset from that corner (top-left anchors use negative y)
    private static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(anchor.x == 0 ? 0 : 1, anchor.y == 0 ? 0 : 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }
}
