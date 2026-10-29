#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Tools > Rain > Setup Ghost Chase
/// Builds "Ghost Chase" (GhostChaseEvent) with the shadow ghost as a child, sized like the player and linked to Anon
/// ("anim_anon_0"). Safe to run again: the old one is replaced.
/// </summary>
public static class GhostChaseMenu
{
    private const string Dir = "Assets/Setting Game by Gim/Img/Ghost/";

    [MenuItem("Tools/Rain/Setup Ghost Chase")]
    public static void Setup()
    {
        foreach (GhostChaseEvent old in Object.FindObjectsByType<GhostChaseEvent>(FindObjectsInactive.Include))
            Undo.DestroyObjectImmediate(old.gameObject);

        GameObject anon = GameObject.Find("anim_anon_0");
        if (anon == null) { Debug.LogWarning("Could not find Anon ('anim_anon_0') in the open scene."); return; }
        GameObject player = GameObject.FindWithTag("Player");

        Sprite first = null;
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(Dir + "anim_jogn_idle_shawdow.png"))
            if (o is Sprite s && s.name == "anim_jogn_idle_shawdow_0") { first = s; break; }
        RuntimeAnimatorController ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Dir + "anim_jogn_idle_shawdow_0.controller");
        if (first == null) { Debug.LogWarning("Ghost sprite not found in " + Dir); return; }

        // size and sorting like the player (fallback: Anon)
        float height = 0f;
        int order = 30;
        SpriteRenderer anonSr = anon.GetComponentInChildren<SpriteRenderer>();
        if (anonSr != null) { height = anonSr.bounds.size.y; order = anonSr.sortingOrder + 1; }
        if (player != null)
        {
            Bounds b = new Bounds(); bool has = false; int maxOrder = int.MinValue;
            foreach (SpriteRenderer sr in player.GetComponentsInChildren<SpriteRenderer>())
            {
                if (sr.sprite == null) continue;
                if (!has) { b = sr.bounds; has = true; } else b.Encapsulate(sr.bounds);
                if (sr.sortingOrder > maxOrder) maxOrder = sr.sortingOrder;
            }
            if (has && b.size.y > 0.1f) { height = b.size.y; order = maxOrder + 1; }
        }
        float scale = height > 0.1f ? Mathf.Clamp(height / first.bounds.size.y, 0.05f, 20f) : 1f;

        GameObject root = new GameObject("Ghost Chase");
        Undo.RegisterCreatedObjectUndo(root, "Setup Ghost Chase");
        GhostChaseEvent ev = root.AddComponent<GhostChaseEvent>();

        GameObject g = new GameObject("Ghost");
        g.transform.SetParent(root.transform, false);
        g.transform.localScale = new Vector3(scale, scale, 1f);
        SpriteRenderer gs = g.AddComponent<SpriteRenderer>();
        gs.sprite = first;
        gs.sortingOrder = order;
        Animator an = g.AddComponent<Animator>();
        an.runtimeAnimatorController = ctrl;
        g.SetActive(false);

        ev.anon = anon;
        ev.ghost = g;
        if (player != null)
        {
            ev.feetOffset = 0f;
        }

        // end: credits scene if the project has one in the build
        foreach (EditorBuildSettingsScene sc in EditorBuildSettings.scenes)
            if (sc.path.ToLower().Contains("credit")) { ev.endSceneName = System.IO.Path.GetFileNameWithoutExtension(sc.path); break; }

        PutStartPoint(ev);
        PutHallwayPoint(ev);
        PutAnonBox(ev);
        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log("Ghost Chase ready (ghost scale " + scale.ToString("0.00") + ", end scene '" + ev.endSceneName + "'). Save the scene (Ctrl+S).");
    }

    /// <summary>Puts the ghost's first appearance point to the left of the cabinet with the red folder.</summary>
    [MenuItem("Tools/Rain/Put Ghost Start Left Of Red Folder")]
    public static void PutStartMenu()
    {
        GhostChaseEvent ev = Object.FindAnyObjectByType<GhostChaseEvent>(FindObjectsInactive.Include);
        if (ev == null) { Debug.LogWarning("Run Tools > Rain > Setup Ghost Chase first."); return; }
        PutStartPoint(ev);
        EditorSceneManager.MarkSceneDirty(ev.gameObject.scene);
    }

    /// <summary>In the hallway with the bench, the ghost appears at the far right end.</summary>
    [MenuItem("Tools/Rain/Put Ghost At Hallway Right End")]
    public static void PutHallwayMenu()
    {
        GhostChaseEvent ev = Object.FindAnyObjectByType<GhostChaseEvent>(FindObjectsInactive.Include);
        if (ev == null) { Debug.LogWarning("Run Tools > Rain > Setup Ghost Chase first."); return; }
        PutHallwayPoint(ev);
        EditorSceneManager.MarkSceneDirty(ev.gameObject.scene);
    }

    private static void PutHallwayPoint(GhostChaseEvent ev)
    {
        // the hallway picture "Hall_way" (not the Log one) and its Room
        GameObject hall = null;
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == "Hall_way") { hall = t.gameObject; break; }
        if (hall == null) { Debug.LogWarning("Could not find the hallway ('Hall_way')."); return; }
        Room room = hall.GetComponentInParent<Room>(true);
        if (room == null) { Debug.LogWarning("The hallway has no Room component."); return; }

        float right = room.boundsMax.x;
        foreach (SpriteRenderer sr in hall.GetComponentsInChildren<SpriteRenderer>(true))
            if (sr.sprite != null && sr.bounds.size.x > 20f) right = Mathf.Min(right, sr.bounds.max.x);
        float x = right - 1.5f;   // right at the map edge

        var list = new System.Collections.Generic.List<GhostChaseEvent.RoomSpawn>(ev.roomSpawns ?? new GhostChaseEvent.RoomSpawn[0]);
        list.RemoveAll(r => r == null || r.room == room);
        Transform pt = ev.transform.Find("Ghost Hallway Point");
        if (pt == null)
        {
            GameObject go = new GameObject("Ghost Hallway Point");
            Undo.RegisterCreatedObjectUndo(go, "Ghost hallway point");
            go.transform.SetParent(ev.transform, false);
            pt = go.transform;
        }
        Undo.RecordObject(pt, "Ghost hallway point");
        Undo.RecordObject(ev, "Ghost hallway point");
        pt.position = new Vector3(x, (room.boundsMin.y + room.boundsMax.y) * 0.5f, 0f);
        list.Add(new GhostChaseEvent.RoomSpawn { room = room, point = pt });
        ev.roomSpawns = list.ToArray();
        EditorUtility.SetDirty(ev);
        Debug.Log("Ghost will appear at the far right of the hallway (x " + x.ToString("0.0") + "). Move 'Ghost Hallway Point' to adjust, then Ctrl+S.");
    }

    /// <summary>The trigger box where Anon stands (Anon disappears when the player leaves his room; the box stays).</summary>
    [MenuItem("Tools/Rain/Put Anon Trigger Box")]
    public static void PutAnonBoxMenu()
    {
        GhostChaseEvent ev = Object.FindAnyObjectByType<GhostChaseEvent>(FindObjectsInactive.Include);
        if (ev == null) { Debug.LogWarning("Run Tools > Rain > Setup Ghost Chase first."); return; }
        PutAnonBox(ev);
        EditorSceneManager.MarkSceneDirty(ev.gameObject.scene);
    }

    private static void PutAnonBox(GhostChaseEvent ev)
    {
        if (ev.anon == null) { Debug.LogWarning("The Ghost Chase has no Anon assigned."); return; }
        Transform box = ev.anonTriggerBox != null ? ev.anonTriggerBox : ev.transform.Find("Anon Trigger Box");
        if (box == null)
        {
            GameObject go = new GameObject("Anon Trigger Box");
            Undo.RegisterCreatedObjectUndo(go, "Anon trigger box");
            go.transform.SetParent(ev.transform, false);
            BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = new Vector2(3f, 12f);
            box = go.transform;
            Vector3 ap = ev.anon.transform.position;
            box.position = new Vector3(ap.x, ap.y, 0f);
        }
        Undo.RecordObject(ev, "Anon trigger box");
        ev.anonTriggerBox = box;
        EditorUtility.SetDirty(ev);
        Debug.Log("Anon Trigger Box is at x " + box.position.x.ToString("0.0") + ". After the red folder, walking past it ends the game. Move it in the Scene view if needed, then Ctrl+S.");
    }

    private static void PutStartPoint(GhostChaseEvent ev)
    {
        ItemPickup red = null;
        foreach (ItemPickup ip in Object.FindObjectsByType<ItemPickup>(FindObjectsInactive.Include))
            if (ip.itemId == "red_folder") { red = ip; break; }
        if (red == null) { Debug.LogWarning("Could not find the red folder (item id 'red_folder')."); return; }

        Transform pt = ev.firstSpawnPoint;
        if (pt == null)
        {
            GameObject go = new GameObject("Ghost Start Point");
            Undo.RegisterCreatedObjectUndo(go, "Ghost start point");
            go.transform.SetParent(ev.transform, false);
            pt = go.transform;
        }
        Undo.RecordObject(pt, "Ghost start point");
        Undo.RecordObject(ev, "Ghost start point");
        Vector3 rp = red.transform.position;
        float x = rp.x - 4.5f;   // left of the cabinet (the red folder lies on top of it)
        // keep it inside the room
        foreach (Room r in Object.FindObjectsByType<Room>(FindObjectsInactive.Include))
            if (rp.x >= r.boundsMin.x && rp.x <= r.boundsMax.x && rp.y >= r.boundsMin.y && rp.y <= r.boundsMax.y)
                x = Mathf.Max(x, r.boundsMin.x + 2f);
        pt.position = new Vector3(x, rp.y, 0f);
        ev.firstSpawnPoint = pt;
        EditorUtility.SetDirty(ev);
        Debug.Log("Ghost start point put left of the red folder cabinet (x " + x.ToString("0.0") + "). Move 'Ghost Start Point' in the Scene view to adjust, then Ctrl+S.");
    }
}
#endif
