#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;

public static class BoardClueMenu
{
    private static bool drawing;
    private static bool dragging;
    private static Vector2 startW, endW;

    [MenuItem("Tools/Rain/Assign Paper Sprite To All Board Clues")]
    public static void AssignPaper()
    {
        Sprite s = KeypadMenu.FindSprite("item_6");
        if (s == null) { Debug.LogWarning("Sprite item_6 not found."); return; }
        foreach (BoardClue b in Object.FindObjectsByType<BoardClue>(FindObjectsInactive.Include))
        { Undo.RecordObject(b, "Assign paper"); b.paperSprite = s; EditorUtility.SetDirty(b); }
    }

    private static readonly string[] DocNames = { "Doc_2", "Doc_3", "Doc_4", "Doc_NoShare", "Doc_Redact" };
    private const string DocFolder = "Assets/Setting Game by Gim/Img/Documents/";

    /// <summary>Gives each notice board ONE of the 5 story documents (boards sorted by slot, one document each).</summary>
    [MenuItem("Tools/Rain/Assign Documents To Boards")]
    public static void AssignDocuments()
    {
        Sprite[] docs = new Sprite[DocNames.Length];
        for (int i = 0; i < DocNames.Length; i++)
        {
            string path = DocFolder + DocNames[i] + ".png";
            docs[i] = LoadSprite(path);
            if (docs[i] == null) { Debug.LogWarning("Document picture not found: " + path); return; }
        }
        var boards = new System.Collections.Generic.List<BoardClue>(Object.FindObjectsByType<BoardClue>(FindObjectsInactive.Include));
        boards.Sort((a, b) => a.slot != b.slot ? a.slot.CompareTo(b.slot) : string.CompareOrdinal(a.name, b.name));
        for (int i = 0; i < boards.Count; i++)
        {
            Undo.RecordObject(boards[i], "Assign document");
            boards[i].documentSprite = i < docs.Length ? docs[i] : null;
            EditorUtility.SetDirty(boards[i]);
            PinDocument(boards[i]);
            Debug.Log("Board '" + boards[i].name + "' (slot " + boards[i].slot + ") -> " + (i < docs.Length ? DocNames[i] : "(no document left)"));
        }
        if (boards.Count != docs.Length)
            Debug.LogWarning("Found " + boards.Count + " boards but " + docs.Length + " documents.");
        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
    }

    // the document is pinned on the board as a small picture (child "Pinned Document"): move / resize it freely
    private static void PinDocument(BoardClue b)
    {
        Transform pin = b.transform.Find("Pinned Document");
        if (b.documentSprite == null)
        {
            if (pin != null) Undo.DestroyObjectImmediate(pin.gameObject);
            return;
        }
        bool fresh = pin == null;
        if (fresh)
        {
            GameObject go = new GameObject("Pinned Document");
            Undo.RegisterCreatedObjectUndo(go, "Pin document");
            go.transform.SetParent(b.transform, false);
            go.transform.localPosition = new Vector3(b.boardSize.x * 0.2f, 0f, 0f);
            go.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(-4f, 4f));
            pin = go.transform;
        }
        SpriteRenderer sr = pin.GetComponent<SpriteRenderer>();
        if (sr == null) sr = Undo.AddComponent<SpriteRenderer>(pin.gameObject);
        Undo.RecordObject(sr, "Pin document");
        sr.sprite = b.documentSprite;
        if (fresh)
        {
            // about as big as the white paper notes on the board art (~27% of the board's height), whatever the parent's scale
            Vector3 ls = b.transform.lossyScale;
            float wantH = b.boardSize.y * Mathf.Abs(ls.y) * 0.27f;
            float k = wantH / Mathf.Max(0.01f, b.documentSprite.bounds.size.y);
            pin.localScale = new Vector3(k / Mathf.Max(0.0001f, Mathf.Abs(ls.x)), k / Mathf.Max(0.0001f, Mathf.Abs(ls.y)), 1f);
        }
        KeypadMenu.PutBehindPlayer(sr);
        EditorUtility.SetDirty(sr);

        // its OWN click area (a bit bigger than the picture): click the document = read it full-page
        BoxCollider2D col = pin.GetComponent<BoxCollider2D>();
        if (col == null) col = Undo.AddComponent<BoxCollider2D>(pin.gameObject);
        Undo.RecordObject(col, "Pin document");
        col.isTrigger = true;
        col.offset = b.documentSprite.bounds.center;
        col.size = b.documentSprite.bounds.size * 1.15f;
        Interactable2D it = pin.GetComponent<Interactable2D>();
        if (it == null)
        {
            it = Undo.AddComponent<Interactable2D>(pin.gameObject);
            UnityEventTools.AddPersistentListener(it.onInteract, b.OpenDocument);
        }
        it.displayName = "Document";
        it.maxDistance = 4f;
        EditorUtility.SetDirty(it);
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s != null) return s;
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return null;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ===== Drag a rectangle over each board in the Scene view =====

    [MenuItem("Tools/Rain/Draw Board Clues (drag over each board)")]
    public static void ToggleDrawing()
    {
        drawing = !drawing;
        SceneView.duringSceneGui -= OnScene;
        if (drawing)
        {
            SceneView.duringSceneGui += OnScene;
            Debug.Log("Board drawing ON: in the Scene view, drag a rectangle over each notice board. Press Esc to stop.");
        }
        SceneView.RepaintAll();
    }

    private static void OnScene(SceneView sv)
    {
        if (!drawing) return;
        Event e = Event.current;
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            drawing = false; dragging = false;
            SceneView.duringSceneGui -= OnScene;
            e.Use();
            Debug.Log("Board drawing OFF.");
            return;
        }

        if (e.alt) return;   // let the camera move
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            startW = endW = WorldAt(e.mousePosition);
            dragging = true;
            e.Use();
        }
        else if (e.type == EventType.MouseDrag && dragging)
        {
            endW = WorldAt(e.mousePosition);
            e.Use();
            sv.Repaint();
        }
        else if (e.type == EventType.MouseUp && e.button == 0 && dragging)
        {
            dragging = false;
            endW = WorldAt(e.mousePosition);
            Vector2 size = new Vector2(Mathf.Abs(endW.x - startW.x), Mathf.Abs(endW.y - startW.y));
            if (size.x > 0.3f && size.y > 0.2f) CreateAt((startW + endW) * 0.5f, size);
            e.Use();
        }

        if (dragging)
        {
            Vector3 a = startW, b = endW;
            Handles.DrawSolidRectangleWithOutline(new[] { a, new Vector3(a.x, b.y), b, new Vector3(b.x, a.y) },
                new Color(1, 1, 0, 0.15f), Color.yellow);
        }
        Handles.BeginGUI();
        GUI.Label(new Rect(10, 10, 420, 22), "BOARD DRAW MODE: drag over a board. Esc = stop.", EditorStyles.boldLabel);
        Handles.EndGUI();
    }

    private static Vector2 WorldAt(Vector2 mouse)
    {
        Ray r = HandleUtility.GUIPointToWorldRay(mouse);
        return new Vector2(r.origin.x, r.origin.y);
    }

    private static void CreateAt(Vector2 center, Vector2 size)
    {
        bool[] used = new bool[LoopManager.SpotCount];
        foreach (BoardClue b in Object.FindObjectsByType<BoardClue>(FindObjectsInactive.Include)) used[Mathf.Clamp(b.slot, 0, 6)] = true;
        foreach (CodeClue c in Object.FindObjectsByType<CodeClue>(FindObjectsInactive.Include)) used[Mathf.Clamp(c.slot, 0, 6)] = true;
        int slot = 0;
        while (slot < used.Length - 1 && used[slot]) slot++;

        GameObject go = new GameObject("BoardClue " + slot);
        Undo.RegisterCreatedObjectUndo(go, "Add Board Clue");
        Vector3 p = new Vector3(center.x, center.y, 0f);
        Transform parent = KeypadMenu.RoomParentAt(p);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position = p;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        BoardClue bc = go.AddComponent<BoardClue>();
        bc.slot = slot;
        bc.paperSprite = KeypadMenu.FindSprite("item_6");
        bc.paperWidth = size.x * 0.12f;
        bc.digitHeight = size.x * 0.08f;
        // the collider is in the object's LOCAL space: undo the parent's scale
        Vector3 ls = go.transform.lossyScale;
        col.size = new Vector2(size.x / Mathf.Max(0.0001f, ls.x), size.y / Mathf.Max(0.0001f, ls.y));
        bc.boardSize = col.size;   // local units, so the gizmo / notes follow the collider
        Interactable2D it = go.AddComponent<Interactable2D>();
        it.displayName = "Notice board";
        UnityEventTools.AddPersistentListener(it.onInteract, bc.Reveal);
        EditorUtility.SetDirty(go);
        Debug.Log("Board clue " + slot + " placed" + (parent != null ? " in " + parent.name : " (no room found: not parented)") + ".");
    }

    [MenuItem("Tools/Rain/Add Board Clue (at Scene view)")]
    public static void Add()
    {
        Vector3 p = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
        CreateAt(new Vector2(p.x, p.y), new Vector2(3.2f, 1.6f));
    }
}
#endif
