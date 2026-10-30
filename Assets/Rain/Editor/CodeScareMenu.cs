#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Tools > Rain > Setup Code Paper Scare
/// Makes the "Code Paper Scare" object (CodeScareEvent) with its own copy of the ghost picture.
/// The trigger flag is taken from the dressing-room drawer's item (DrawerSpot "wound_drawer"). Drag your sound onto "Scare Sound".
/// Safe to run twice: it only fills in what is missing.
/// </summary>
public static class CodeScareMenu
{
    [MenuItem("Tools/Rain/Setup Code Paper Scare")]
    public static void Setup()
    {
        CodeScareEvent ev = Object.FindAnyObjectByType<CodeScareEvent>(FindObjectsInactive.Include);
        GameObject root;
        if (ev == null)
        {
            root = new GameObject("Code Paper Scare");
            Undo.RegisterCreatedObjectUndo(root, "Code Paper Scare");
            ev = root.AddComponent<CodeScareEvent>();
        }
        else root = ev.gameObject;

        // trigger flag: the item inside the dressing-room drawer
        foreach (DrawerSpot ds in Object.FindObjectsByType<DrawerSpot>(FindObjectsInactive.Include))
        {
            if (ds.drawerId != "wound_drawer" || string.IsNullOrEmpty(ds.itemId)) continue;
            Undo.RecordObject(ev, "Scare flag");
            ev.triggerFlag = "got_" + ds.itemId;
            break;
        }

        // the ghost: a copy of the chase ghost
        if (ev.ghost == null)
        {
            GhostChaseEvent chase = Object.FindAnyObjectByType<GhostChaseEvent>(FindObjectsInactive.Include);
            if (chase == null || chase.ghost == null)
                Debug.LogWarning("No 'Ghost Chase' ghost found to copy: run Tools > Rain > Setup Ghost Chase first, or drag a ghost object onto 'Ghost' of Code Paper Scare.");
            else
            {
                GameObject copy = Object.Instantiate(chase.ghost, root.transform);
                copy.name = "Scare Ghost";
                copy.transform.localPosition = Vector3.zero;
                copy.SetActive(false);
                Undo.RegisterCreatedObjectUndo(copy, "Scare Ghost");
                Undo.RecordObject(ev, "Scare ghost");
                ev.ghost = copy;
            }
        }

        // where the ghost appears: just left of the bed in the dressing room
        if (ev.spawnPoint == null)
        {
            Transform pt = root.transform.Find("Scare Spawn Point");
            if (pt == null)
            {
                GameObject pg = new GameObject("Scare Spawn Point");
                Undo.RegisterCreatedObjectUndo(pg, "Scare Spawn Point");
                pg.transform.SetParent(root.transform, false);
                pt = pg.transform;
                pt.position = FindLeftOfBed();
            }
            Undo.RecordObject(ev, "Scare spawn point");
            ev.spawnPoint = pt;
        }

        EditorUtility.SetDirty(ev);
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log("Scare Spawn Point is at x " + ev.spawnPoint.position.x.ToString("0.0") + " (left of the bed). Move it in the Scene view if it is not in the right place.");
        Debug.Log("Code Paper Scare is ready (trigger flag: " + ev.triggerFlag + "). Drag your sound onto 'Scare Sound', then save the scene (Ctrl+S).");
    }

    // The room with the wound drawer; the bed = the biggest picture in it with "bed" in its name (not "bedside").
    private static Vector3 FindLeftOfBed()
    {
        Vector2 drawerPos = Vector2.zero;
        bool haveDrawer = false;
        foreach (DrawerSpot ds in Object.FindObjectsByType<DrawerSpot>(FindObjectsInactive.Include))
            if (ds.drawerId == "wound_drawer") { drawerPos = ds.transform.position; haveDrawer = true; break; }

        Room room = null;
        float roomArea = float.MaxValue;
        if (haveDrawer)
            foreach (Room r in Object.FindObjectsByType<Room>(FindObjectsInactive.Include))
            {
                if (drawerPos.x < r.boundsMin.x || drawerPos.x > r.boundsMax.x) continue;
                if (drawerPos.y < Mathf.Min(r.boundsMin.y, r.boundsMax.y) || drawerPos.y > Mathf.Max(r.boundsMin.y, r.boundsMax.y)) continue;
                float a = Mathf.Abs((r.boundsMax.x - r.boundsMin.x) * (r.boundsMax.y - r.boundsMin.y));
                if (a < roomArea) { roomArea = a; room = r; }
            }

        SpriteRenderer bed = null;
        float best = 0f;
        foreach (SpriteRenderer sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include))
        {
            if (sr.sprite == null) continue;
            string n = (sr.name + " " + sr.sprite.name).ToLower();
            if (!n.Contains("bed") || n.Contains("bedside")) continue;
            Vector3 c = sr.bounds.center;
            if (room != null && (c.x < room.boundsMin.x || c.x > room.boundsMax.x)) continue;
            float area = sr.bounds.size.x * sr.bounds.size.y;
            if (area > best) { best = area; bed = sr; }
        }

        float y = haveDrawer ? drawerPos.y : 0f;
        if (bed != null) return new Vector3(bed.bounds.min.x - 1.5f, y, 0f);
        Debug.LogWarning("No 'bed' picture found: the Scare Spawn Point is put at the left side of the room. Move it to the left of the bed in the Scene view.");
        if (room != null) return new Vector3(room.boundsMin.x + 3f, y, 0f);
        return new Vector3(0f, y, 0f);
    }
}
#endif
