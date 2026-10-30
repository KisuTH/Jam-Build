using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Cheat mode. On the pause screen (ESC) press Ctrl+O (or F8): a small box asks for the cheat code.
/// Type 0607 and press Enter to turn cheat mode ON (do it again to turn it OFF).
/// While it is ON, opening any keypad (door keypad or the Pass Code panel) unlocks that door at once -
/// no password, no guessing the digits.
/// Put it on an object in the scene (Tools > Rain > Setup Cheat Mode makes the "Cheat Mode" object).
/// To switch the whole cheat off: untick the object (or this component). Delete the object to remove it.
/// </summary>
public class CheatMode : MonoBehaviour
{
    public const string Code = "0607";
    private static bool cheatOn;   // stays on while the loop restarts the scene
    private static CheatMode instance;

    /// <summary>Cheat mode is ON. Always false while the "Cheat Mode" object (or this component) is disabled or missing.</summary>
    public static bool Active { get { return cheatOn && instance != null && instance.isActiveAndEnabled; } }

    private void OnEnable() { instance = this; }
    private void OnDisable() { if (instance == this) instance = null; }

    private JamBuild.Pause.PauseMenu pause;
    private GameObject canvasGo, entryPanel, badge;
    private TMP_Text entryText, statusText;
    private bool entryOpen;
    private string entered = "";
    private string status = "";

    private void Update()
    {
        if (pause == null) pause = FindAnyObjectByType<JamBuild.Pause.PauseMenu>(FindObjectsInactive.Include);
        bool paused = pause != null && pause.IsPaused && !pause.IsSettingsOpen;
        Keyboard kb = Keyboard.current;

        if (!paused)
        {
            if (entryOpen) SetEntry(false);
        }
        else if (kb != null)
        {
            if ((kb.oKey.wasPressedThisFrame && kb.ctrlKey.isPressed) || kb.f8Key.wasPressedThisFrame) SetEntry(!entryOpen);   // Ctrl+O (F8 too: the Unity Editor can swallow Ctrl+O)
            else if (entryOpen) ReadKeys(kb);
        }

        if (badge != null && badge.activeSelf != Active) badge.SetActive(Active);
    }

    private void ReadKeys(Keyboard kb)
    {
        for (int d = 0; d < 10; d++)
        {
            Key k = (Key)((int)Key.Digit0 + d);
            Key n = (Key)((int)Key.Numpad0 + d);
            if ((kb[k].wasPressedThisFrame || kb[n].wasPressedThisFrame) && entered.Length < Code.Length)
            {
                entered += d.ToString();
                status = "";
            }
        }
        if (kb.backspaceKey.wasPressedThisFrame && entered.Length > 0) entered = entered.Substring(0, entered.Length - 1);
        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
        {
            if (entered == Code)
            {
                cheatOn = !cheatOn;
                status = cheatOn ? "CHEAT MODE: ON" : "CHEAT MODE: OFF";
            }
            else status = "Wrong code";
            entered = "";
        }
        Refresh();
    }

    private void SetEntry(bool open)
    {
        entryOpen = open;
        entered = "";
        status = "";
        Build();
        entryPanel.SetActive(open);
        Refresh();
    }

    private void Refresh()
    {
        if (entryText == null) return;
        string s = "";
        for (int i = 0; i < Code.Length; i++)
            s += (i < entered.Length ? "*" : "_") + (i < Code.Length - 1 ? " " : "");
        entryText.text = "CHEAT CODE   " + s;
        statusText.text = status.Length > 0 ? status : "type the code, then press Enter  (Ctrl+O to close)";
        statusText.color = status == "Wrong code" ? new Color(1f, 0.45f, 0.4f) : (status.Length > 0 ? new Color(0.5f, 1f, 0.55f) : new Color(1f, 1f, 1f, 0.6f));
    }

    private void Build()
    {
        if (canvasGo != null) return;
        canvasGo = new GameObject("CheatCanvas");
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;
        CanvasScaler cs = canvasGo.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;

        entryPanel = new GameObject("Entry", typeof(RectTransform), typeof(Image));
        entryPanel.transform.SetParent(canvasGo.transform, false);
        entryPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);
        entryPanel.GetComponent<Image>().raycastTarget = false;
        RectTransform pr = entryPanel.GetComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(0.5f, 0f);
        pr.sizeDelta = new Vector2(760, 150);
        pr.anchoredPosition = new Vector2(0, 60);

        entryText = MakeText("Code", entryPanel.transform, 46, new Vector2(0, 30), Color.white);
        statusText = MakeText("Status", entryPanel.transform, 26, new Vector2(0, -35), Color.white);

        badge = new GameObject("Badge", typeof(RectTransform));
        badge.transform.SetParent(canvasGo.transform, false);
        RectTransform br = badge.GetComponent<RectTransform>();
        br.anchorMin = br.anchorMax = br.pivot = new Vector2(0.5f, 1f);
        br.sizeDelta = new Vector2(400, 40);
        br.anchoredPosition = new Vector2(0, -8);
        TMP_Text bt = badge.AddComponent<TextMeshProUGUI>();
        bt.text = "CHEAT MODE";
        bt.alignment = TextAlignmentOptions.Center;
        bt.fontSize = 24;
        bt.color = new Color(1f, 0.35f, 0.3f, 0.85f);
        bt.raycastTarget = false;
        badge.SetActive(Active);
    }

    private static TMP_Text MakeText(string name, Transform parent, float size, Vector2 pos, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(720, 60);
        r.anchoredPosition = pos;
        TMP_Text t = go.AddComponent<TextMeshProUGUI>();
        t.alignment = TextAlignmentOptions.Center;
        t.fontSize = size;
        t.color = color;
        t.raycastTarget = false;
        return t;
    }
}
