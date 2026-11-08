using System.Collections.Generic;
using System.Reflection;
using JamBuild.Options;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Adds a "CONTROL MODE: PC / MOBILE" choice to the CONTROL page of the Settings notebook.
/// It creates itself and builds the choice at runtime - nothing to add to the scene, no prefab to edit.
///   PC     = keyboard + mouse only (no touch buttons)
///   MOBILE = touch buttons on screen (walk / jump / crouch), tap an object to interact
/// The choice is saved. Until the player picks one, the game decides by itself (touch device = Mobile).
/// </summary>
public class ControlModeSetting : MonoBehaviour
{
    private static ControlModeSetting instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (instance != null) return;
        GameObject go = new GameObject("ControlModeSetting");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<ControlModeSetting>();
    }

    private class Entry
    {
        public NotebookOptions owner;
        public TextMeshProUGUI title, pcLabel, mobileLabel;
        public GameObject pcTick, mobileTick;
    }

    private readonly List<Entry> entries = new List<Entry>();
    private readonly HashSet<NotebookOptions> done = new HashSet<NotebookOptions>();
    private float nextScan;
    private static FieldInfo pagesField, englishField;

    private void OnDestroy() { if (instance == this) instance = null; }

    private void Update()
    {
        if (Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime + 0.5f;
            foreach (NotebookOptions n in FindObjectsByType<NotebookOptions>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (n == null || done.Contains(n)) continue;
                Entry e = Build(n);
                done.Add(n);   // even if it failed, do not try again every frame
                if (e != null) entries.Add(e);
            }
        }

        bool mobile = MobileControls.Active;
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            Entry e = entries[i];
            if (e.owner == null) { entries.RemoveAt(i); continue; }
            if (!e.owner.isActiveAndEnabled) continue;
            e.pcTick.SetActive(!mobile);
            e.mobileTick.SetActive(mobile);
            bool en = IsEnglish(e.owner);
            Set(e.title, en, "โหมดควบคุม", "CONTROL MODE");
            Set(e.pcLabel, en, "คอมพิวเตอร์", "PC");
            Set(e.mobileLabel, en, "มือถือ", "MOBILE");
        }
    }

    private static bool IsEnglish(NotebookOptions n)
    {
        if (englishField == null) englishField = typeof(NotebookOptions).GetField("english", BindingFlags.NonPublic | BindingFlags.Instance);
        return englishField != null && (bool)englishField.GetValue(n);
    }

    private static void Set(TextMeshProUGUI t, bool english, string thai, string en)
    {
        string want = english ? en : Loc.Shape(thai);
        if (t != null && t.text != want) t.text = want;
    }

    // ---------------------------------------------------------------- building

    private Entry Build(NotebookOptions n)
    {
        if (pagesField == null) pagesField = typeof(NotebookOptions).GetField("pages", BindingFlags.NonPublic | BindingFlags.Instance);
        NotebookOptions.Page[] pages = pagesField != null ? pagesField.GetValue(n) as NotebookOptions.Page[] : null;
        if (pages == null || pages.Length < 3 || pages[2] == null || pages[2].left == null || pages[2].right == null) return null;
        RectTransform left = pages[2].left, right = pages[2].right;

        // copy the look of the existing CONTROL page text
        TextMeshProUGUI style = null;
        Transform d0 = left.Find("Direction0");
        if (d0 != null) style = d0.GetComponent<TextMeshProUGUI>();
        if (style == null) style = left.GetComponentInChildren<TextMeshProUGUI>(true);
        if (style == null) return null;
        Color ink = style.color;

        // copy the look of an existing tick box (VIDEO page)
        Color paper = new Color(0.88f, 0.85f, 0.72f, 1f), red = new Color32(168, 39, 32, 255);
        if (pages[0] != null && pages[0].right != null)
        {
            Transform inner = pages[0].right.Find("windowModes_0/BoxInner");
            Transform tick = pages[0].right.Find("windowModes_0/SelectedTick");
            if (inner != null && inner.GetComponent<Graphic>() != null) paper = inner.GetComponent<Graphic>().color;
            if (tick != null && tick.GetComponent<Graphic>() != null) red = tick.GetComponent<Graphic>().color;
        }

        // the "click a key to change" hint is wider than the page in Thai: wrap it inside the page, under the new choices
        Transform hint = right.Find("RebindHint");
        if (hint != null)
        {
            TextMeshProUGUI ht = hint.GetComponent<TextMeshProUGUI>();
            RectTransform hr = (RectTransform)hint;
            hr.anchoredPosition = new Vector2(60, -455);
            hr.sizeDelta = new Vector2(440, 60);
            if (ht != null)
            {
                ht.textWrappingMode = TextWrappingModes.Normal;
                ht.overflowMode = TextOverflowModes.Overflow;
                ht.alignment = TextAlignmentOptions.TopLeft;
            }
        }

        Entry e = new Entry { owner = n };
        e.title = MakeText(left, "ControlMode_Label", "CONTROL MODE", 60, 387, 440, 52, 25, style, ink, TextAlignmentOptions.MidlineLeft);
        MakeChoice(right, "ControlModeChoice_0", 42, 350, style, ink, paper, red, out e.pcLabel, out e.pcTick, () => MobileControls.SetMobileMode(false));
        MakeChoice(right, "ControlModeChoice_1", 227, 350, style, ink, paper, red, out e.mobileLabel, out e.mobileTick, () => MobileControls.SetMobileMode(true));
        return e;
    }

    private static void MakeChoice(RectTransform parent, string name, float x, float y, TextMeshProUGUI style, Color ink, Color paper, Color red,
        out TextMeshProUGUI label, out GameObject tick, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform rt = MakeRect(parent, name, x, y, 142, 99);
        Image hit = rt.gameObject.AddComponent<Image>();
        hit.color = new Color(0, 0, 0, 0);
        Button b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = hit;
        b.onClick.AddListener(onClick);

        label = MakeText(rt, "Label", "", -4, 0, 150, 36, 18, style, ink, TextAlignmentOptions.Center);
        MakeShape(rt, "Box", 57, 48, 28, 28, ink, NotebookPaper.Shape.Rounded, 3);
        MakeShape(rt, "BoxInner", 60, 51, 22, 22, paper, NotebookPaper.Shape.Rounded, 1);
        tick = MakeShape(rt, "SelectedTick", 53, 31, 46, 47, red, NotebookPaper.Shape.Tick, 12).gameObject;
        tick.SetActive(false);
    }

    private static RectTransform MakeRect(Transform parent, string name, float x, float y, float w, float h)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = new Vector2(0, 1);
        r.pivot = new Vector2(0, 1);
        r.anchoredPosition = new Vector2(x, -y);
        r.sizeDelta = new Vector2(w, h);
        return r;
    }

    private static RectTransform MakeShape(Transform parent, string name, float x, float y, float w, float h, Color color, NotebookPaper.Shape shape, float radius)
    {
        RectTransform r = MakeRect(parent, name, x, y, w, h);
        r.gameObject.AddComponent<NotebookPaper>().Configure(shape, color, false, radius);
        return r;
    }

    private static TextMeshProUGUI MakeText(Transform parent, string name, string value, float x, float y, float w, float h, float size,
        TextMeshProUGUI style, Color color, TextAlignmentOptions align)
    {
        RectTransform r = MakeRect(parent, name, x, y, w, h);
        TextMeshProUGUI t = r.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = style.font;
        t.fontSharedMaterial = style.fontSharedMaterial;
        t.text = value;
        t.fontSize = size;
        t.color = color;
        t.raycastTarget = false;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }
}
