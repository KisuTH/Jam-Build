using TMPro;
using UnityEngine;

/// <summary>
/// A notice board (baked into the room art) that may hide one passcode digit on one of its notes.
/// Put it on an object with Interactable2D + a trigger collider covering the board, and hook
/// "On Interact" -> BoardClue.Reveal.  (Tools > Rain > Add Board Clue does all of this for you.)
///
/// Each loop the game decides whether this board's slot shows a digit. If yes, a number is written on a RANDOM note
/// of the board. After dying the choice is made again, so the digits move to other boards / other notes.
/// </summary>
public class BoardClue : MonoBehaviour
{
    [Range(0, 6)] public int slot = 0;
    [Tooltip("Board size in world units (the dashed rectangle in the Scene view).")]
    public Vector2 boardSize = new Vector2(3.2f, 1.6f);
    [Tooltip("Where the notes are, as -0.5..0.5 of the board size (x right, y up). Edit to match the art.")]
    public Vector2[] notes = { new Vector2(-0.32f, 0.07f), new Vector2(-0.09f, -0.41f), new Vector2(0.11f, 0.02f) };
    [Tooltip("Size of the written digit, in world units (height).")]
    public float digitHeight = 0.28f;
    public Color ink = new Color(0.12f, 0.1f, 0.08f, 1f);
    [Tooltip("Used by the hint system, e.g. 'the notice board in the Waiting Hall'.")]
    public string locationHint = "the notice board";
    [TextArea] public string flavorText = "Notes are pinned to the board. One of them has a number:";
    [TextArea] public string nothingText = "Just old, blank notes. Nothing useful here.";
    public int sortingOrder = 30;
    [Header("Paper that carries the digit")]
    [Tooltip("The paper sprite (item_6). The digit is written on it. Assigned automatically by the menu.")]
    public Sprite paperSprite;
    [Tooltip("Width of the paper in world units.")]
    public float paperWidth = 0.5f;

    [Header("Story document pinned on this board")]
    [Tooltip("A document shown full-page when the board is clicked (before the code paper, if any). Assigned by Tools > Rain > Assign Documents To Boards.")]
    public Sprite documentSprite;
    [Tooltip("Off (default): the document is only pinned on the board as a picture. On: clicking the board also opens it full-page.")]
    public bool showDocumentOnClick = false;

    private static readonly string[] Ordinals = { "first", "second", "third", "fourth" };
    private int shownNote = -1;
    private static System.Action afterClose;
    private static int closedFrame = -1;

    private void Awake() { LoopManager.RegisterSlot(slot, HintText()); }

    // "the notice board in Hall Way, on the left" - so the Hint tells WHICH board (room name + side of the room)
    private string HintText()
    {
        if (locationHint != "the notice board") return locationHint;   // written by hand: keep it
        Vector2 p = transform.position;
        foreach (Room r in Room.All)
        {
            if (p.x < r.boundsMin.x || p.x > r.boundsMax.x || p.y < r.boundsMin.y || p.y > r.boundsMax.y) continue;
            string name = (r.roomName ?? "").Replace('_', ' ').Trim();
            float t = Mathf.InverseLerp(r.boundsMin.x, r.boundsMax.x, p.x);
            string side = t < 0.33f ? "on the left" : (t > 0.67f ? "on the right" : "in the middle");
            string place = string.IsNullOrEmpty(name) || name == "Room" ? "this floor" : name;
            return locationHint + " in " + place + ", " + side;
        }
        return locationHint;
    }
    private void OnDestroy() { LoopManager.UnregisterSlot(slot); }

    // The digit is NOT drawn on the board: it is only seen in the zoomed paper after clicking.

    public void Reveal()
    {
        // the pinned document first; the code paper (if this board has a digit) follows when it is closed
        if (showDocumentOnClick && documentSprite != null) { AddDocumentToCaseBook(); PlayDocumentSound(); ShowDocument(documentSprite, RevealDigit); return; }
        RevealDigit();
    }

    /// <summary>Clicking the document pinned on the board (its own Interactable2D) opens it full-page.</summary>
    public void OpenDocument()
    {
        if (documentSprite == null) return;
        AddDocumentToCaseBook();
        PlayDocumentSound();
        ShowDocument(documentSprite, null);
    }

    private bool docSoundPlayed;

    // The first time the pinned document is taken: the pickup sound of its Tag (GameSfx > Tag Sounds, e.g. "Document")
    private void PlayDocumentSound()
    {
        if (docSoundPlayed || GameSfx.Instance == null) return;
        docSoundPlayed = true;
        Transform pin = transform.Find("Pinned Document");
        GameSfx.Instance.PlayPickup(pin != null ? pin.gameObject : gameObject);
    }

    /// <summary>Opens a document picture full-page (used by the Case Book's "View document" button).</summary>
    public static void ViewDocument(Sprite doc)
    {
        if (doc != null) ShowDocument(doc, null);
    }

    // Reading a pinned document writes it into the Case Book (a page with a "View document" button)
    private void AddDocumentToCaseBook()
    {
        if (CaseNotebook.Instance == null || documentSprite == null) return;
        string n = documentSprite.name;
        string title = "Pinned Document";
        if (n.Contains("Doc_2")) title = "Patient Watch Report No. 2";
        else if (n.Contains("Doc_3")) title = "Patient Watch Report No. 3";
        else if (n.Contains("Doc_4")) title = "Patient Watch Report No. 4";
        else if (n.Contains("NoShare")) title = "Internal Incident Log - Not for Distribution";
        else if (n.Contains("Redact")) title = "Patient History - Restricted Document";
        CaseNotebook.Instance.AddClue("doc_" + n, title,
            "A document pinned on a notice board. Press the button below to read it again.", documentSprite);
    }

    private void RevealDigit()
    {
        int position = LoopManager.PositionForSlot(slot);
        if (position < 0)
        {
            ShowZoom("", nothingText);
            return;
        }
        int digit = LoopManager.DigitForSlot(slot);
        string note = "The " + Ordinals[position] + " digit of the code is " + digit + ".";
        ShowZoom(digit.ToString(), flavorText + " " + note);
        if (LoopManager.Ensure() != null) LoopManager.Ensure().MarkFound(slot);
        if (CaseNotebook.Instance != null)
            CaseNotebook.Instance.AddClue("code_slot_" + slot, "Code: " + Ordinals[position] + " digit", note);
    }

    /// <summary>The digit hidden in the dressing-room drawer: shown as a paper when the player takes the red file.</summary>
    public static void RevealDrawerDigit()
    {
        int slot = LoopManager.DrawerSlot;
        int position = LoopManager.PositionForSlot(slot);
        if (position < 0) return;
        int digit = LoopManager.DigitForSlot(slot);
        string note = "The " + Ordinals[position] + " digit of the code is " + digit + ".";
        string en = "A paper was tucked inside the red file. One number is written on it: " + note;
        string th = "มีกระดาษซุกอยู่ในแฟ้มแดง เขียนตัวเลขไว้หนึ่งตัว: ตัวเลขหลักที่ " + (position + 1) + " ของรหัสคือ " + digit;
        if (LoopManager.Ensure() != null) LoopManager.Ensure().MarkFound(slot);
        if (CaseNotebook.Instance != null)
            CaseNotebook.Instance.AddClue("code_slot_" + slot, "Code: " + Ordinals[position] + " digit", note);

        // use the same paper picture (item_6) as the notice boards outside
        BoardClue any = null;
        foreach (BoardClue b in FindObjectsByType<BoardClue>(FindObjectsInactive.Include))
        {
            if (any == null) any = b;
            if (b.paperSprite != null) { any = b; break; }
        }
        if (any != null && any.paperSprite == null)
        {
            foreach (Sprite s in Resources.LoadAll<Sprite>("WoundPuzzle/wound_items"))
                if (s != null && s.name == "item_6") { any.paperSprite = s; break; }
        }
        if (any != null) any.ShowZoom(digit.ToString(), Loc.Thai ? Loc.Shape(th) : en);
        else if (DialogueUI.Instance != null) DialogueUI.Instance.Show(Loc.Thai ? Loc.Shape(th) : en);
    }

    // ===== Zoomed paper =====
    private static GameObject zoom;

    private void ShowZoom(string bigText, string caption)
    {
        if (zoom != null) Destroy(zoom);
        afterClose = null;
        KeypadLock.EnsureEventSystem();
        if (DialogueUI.Instance != null) DialogueUI.Instance.Hide();

        zoom = new GameObject("BoardZoom");
        Canvas canvas = zoom.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 85;
        UnityEngine.UI.CanvasScaler cs = zoom.AddComponent<UnityEngine.UI.CanvasScaler>();
        cs.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;
        zoom.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // dim background; click anywhere = close
        GameObject dim = new GameObject("Dim", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        dim.transform.SetParent(zoom.transform, false);
        dim.GetComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0.78f);
        RectTransform dr = dim.GetComponent<RectTransform>();
        dr.anchorMin = Vector2.zero; dr.anchorMax = Vector2.one; dr.offsetMin = dr.offsetMax = Vector2.zero;
        UnityEngine.UI.Button btn = dim.GetComponent<UnityEngine.UI.Button>();
        btn.transition = UnityEngine.UI.Selectable.Transition.None;
        btn.onClick.AddListener(CloseZoom);

        // the paper
        GameObject paper = new GameObject("Paper", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        paper.transform.SetParent(zoom.transform, false);
        UnityEngine.UI.Image img = paper.GetComponent<UnityEngine.UI.Image>();
        img.raycastTarget = false;
        float h = 400f, w = 400f;
        if (paperSprite != null)
        {
            img.sprite = paperSprite;
            img.preserveAspect = true;
            Vector2 sz = paperSprite.bounds.size;
            w = h * sz.x / Mathf.Max(0.01f, sz.y);
        }
        else img.color = new Color(0.9f, 0.82f, 0.68f);
        RectTransform pr = paper.GetComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(w, h);
        pr.anchoredPosition = new Vector2(0, 60);
        pr.localRotation = Quaternion.Euler(0, 0, -4f);

        // scribbled lines like handwriting; the digit sits on one of the rows
        int rows = 6;
        int digitRow = string.IsNullOrEmpty(bigText) ? -1 : Random.Range(1, rows - 1);
        for (int r = 0; r < rows; r++)
        {
            float y = (0.5f - (r + 0.9f) / (rows + 0.6f)) * h;   // top to bottom
            if (r == digitRow) continue;
            GameObject ln = new GameObject("Scribble", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            ln.transform.SetParent(paper.transform, false);
            UnityEngine.UI.Image li = ln.GetComponent<UnityEngine.UI.Image>();
            li.color = new Color(ink.r, ink.g, ink.b, 0.5f);
            li.raycastTarget = false;
            RectTransform lr = ln.GetComponent<RectTransform>();
            float lw = w * Random.Range(0.4f, 0.7f);
            lr.sizeDelta = new Vector2(lw, 5f);
            lr.anchorMin = lr.anchorMax = new Vector2(0.5f, 0.5f);
            lr.anchoredPosition = new Vector2(Random.Range(-0.08f, 0.08f) * w - (0.7f * w - lw) * 0.15f, y);
            lr.localRotation = Quaternion.Euler(0, 0, Random.Range(-2f, 2f));
        }
        if (digitRow >= 0)
        {
            GameObject d = new GameObject("Digit", typeof(RectTransform));
            d.transform.SetParent(paper.transform, false);
            RectTransform rt = d.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(70, 60);
            rt.anchoredPosition = new Vector2(Random.Range(-0.12f, 0.12f) * w, (0.5f - (digitRow + 0.9f) / (rows + 0.6f)) * h);
            rt.localRotation = Quaternion.Euler(0, 0, Random.Range(-6f, 6f));
            TMPro.TextMeshProUGUI t = d.AddComponent<TMPro.TextMeshProUGUI>();
            t.text = bigText;
            t.alignment = TMPro.TextAlignmentOptions.Center;
            t.fontStyle = TMPro.FontStyles.Bold;
            t.fontSize = 46;
            t.color = ink;
            t.raycastTarget = false;
        }

        // caption under the paper
        GameObject c = new GameObject("Caption", typeof(RectTransform));
        c.transform.SetParent(zoom.transform, false);
        RectTransform cr = c.GetComponent<RectTransform>();
        cr.anchorMin = cr.anchorMax = cr.pivot = new Vector2(0.5f, 0.5f);
        cr.sizeDelta = new Vector2(1400, 150);
        cr.anchoredPosition = new Vector2(0, -250);
        TMPro.TextMeshProUGUI ct = c.AddComponent<TMPro.TextMeshProUGUI>();
        ct.text = Loc.T(caption);
        ct.alignment = TMPro.TextAlignmentOptions.Top;
        ct.fontSize = 40;
        ct.color = Color.white;
        ct.raycastTarget = false;

        GameObject hn = new GameObject("Hint", typeof(RectTransform));
        hn.transform.SetParent(zoom.transform, false);
        RectTransform hr = hn.GetComponent<RectTransform>();
        hr.anchorMin = hr.anchorMax = hr.pivot = new Vector2(0.5f, 0f);
        hr.sizeDelta = new Vector2(800, 50);
        hr.anchoredPosition = new Vector2(0, 30);
        TMPro.TextMeshProUGUI ht = hn.AddComponent<TMPro.TextMeshProUGUI>();
        ht.text = Loc.T("Click anywhere to close");
        ht.alignment = TMPro.TextAlignmentOptions.Center;
        ht.fontSize = 28;
        ht.color = new Color(1, 1, 1, 0.6f);
        ht.raycastTarget = false;

        LoopManager.SetPlayerLocked(true);
    }

    private static void ShowDocument(Sprite doc, System.Action then)
    {
        if (zoom != null) Destroy(zoom);
        KeypadLock.EnsureEventSystem();
        if (DialogueUI.Instance != null) DialogueUI.Instance.Hide();

        zoom = new GameObject("BoardDocument");
        Canvas canvas = zoom.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;   // above the Case Book too
        UnityEngine.UI.CanvasScaler cs = zoom.AddComponent<UnityEngine.UI.CanvasScaler>();
        cs.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;
        zoom.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        GameObject dim = new GameObject("Dim", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        dim.transform.SetParent(zoom.transform, false);
        dim.GetComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0.82f);
        RectTransform dr = dim.GetComponent<RectTransform>();
        dr.anchorMin = Vector2.zero; dr.anchorMax = Vector2.one; dr.offsetMin = dr.offsetMax = Vector2.zero;
        UnityEngine.UI.Button btn = dim.GetComponent<UnityEngine.UI.Button>();
        btn.transition = UnityEngine.UI.Selectable.Transition.None;
        btn.onClick.AddListener(CloseZoom);

        // the page, as large as fits the screen (keeps its proportions)
        GameObject paper = new GameObject("Document", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        paper.transform.SetParent(zoom.transform, false);
        UnityEngine.UI.Image img = paper.GetComponent<UnityEngine.UI.Image>();
        img.sprite = doc;
        img.preserveAspect = true;
        img.raycastTarget = false;
        Vector2 sz = doc.bounds.size;
        float h = 960f, w = h * sz.x / Mathf.Max(0.01f, sz.y);
        RectTransform pr = paper.GetComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(w, h);
        pr.anchoredPosition = new Vector2(0, 20);

        GameObject hn = new GameObject("Hint", typeof(RectTransform));
        hn.transform.SetParent(zoom.transform, false);
        RectTransform hr = hn.GetComponent<RectTransform>();
        hr.anchorMin = hr.anchorMax = hr.pivot = new Vector2(0.5f, 0f);
        hr.sizeDelta = new Vector2(800, 40);
        hr.anchoredPosition = new Vector2(0, 8);
        TMPro.TextMeshProUGUI ht = hn.AddComponent<TMPro.TextMeshProUGUI>();
        ht.text = Loc.T("Click anywhere to close");
        ht.alignment = TMPro.TextAlignmentOptions.Center;
        ht.fontSize = 26;
        ht.color = new Color(1, 1, 1, 0.6f);
        ht.raycastTarget = false;

        afterClose = then;
        LoopManager.SetPlayerLocked(true);
    }

    private static void CloseZoom()
    {
        if (zoom != null) Destroy(zoom);
        zoom = null;
        closedFrame = Time.frameCount;
        LoopManager.SetPlayerLocked(false);
        System.Action a = afterClose;
        afterClose = null;
        if (a != null) a();   // e.g. the code paper after the document
    }

    private void Update()
    {
        if (zoom == null || Time.frameCount == closedFrame) return;
        UnityEngine.InputSystem.Keyboard kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
            CloseZoom();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(boardSize.x, boardSize.y, 0));
        Gizmos.color = Color.cyan;
        if (notes != null)
            foreach (Vector2 n in notes)
                Gizmos.DrawWireCube(new Vector3(n.x * boardSize.x, n.y * boardSize.y, 0), new Vector3(0.35f, 0.35f, 0));
    }
}
