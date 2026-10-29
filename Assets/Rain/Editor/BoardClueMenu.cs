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
