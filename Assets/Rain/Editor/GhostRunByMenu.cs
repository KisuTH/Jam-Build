#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Tools > Rain > Add Ghost Run-By : makes an object with a copy of the chase ghost inside it.</summary>
public static class GhostRunByMenu
{
    [MenuItem("Tools/Rain/Add Ghost Run-By")]
    public static void Add()
    {
        GameObject go = new GameObject("Ghost Run-By");
        Undo.RegisterCreatedObjectUndo(go, "Ghost Run-By");
        Vector3 pos = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        GhostRunBy run = go.AddComponent<GhostRunBy>();

        // pick the room that contains the middle of the Scene view (or the nearest one)
        Room best = null; float bd = float.MaxValue;
        foreach (Room r in Object.FindObjectsByType<Room>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Vector2 c = (r.boundsMin + r.boundsMax) * 0.5f;
            float d = Vector2.Distance(c, pos);
            if (pos.x >= r.boundsMin.x && pos.x <= r.boundsMax.x && pos.y >= r.boundsMin.y && pos.y <= r.boundsMax.y) { best = r; break; }
            if (d < bd) { bd = d; best = r; }
        }
        run.room = best;

        GhostChaseEvent chase = Object.FindAnyObjectByType<GhostChaseEvent>(FindObjectsInactive.Include);
        if (chase != null && chase.ghost != null)
        {
            GameObject copy = Object.Instantiate(chase.ghost, go.transform);
            copy.name = "Run-By Ghost";
            copy.transform.localPosition = Vector3.zero;
            copy.SetActive(false);
            Undo.RegisterCreatedObjectUndo(copy, "Run-By Ghost");
            run.ghost = copy;
        }
        else Debug.LogWarning("No chase ghost found to copy: drag a ghost object onto 'Ghost' of Ghost Run-By.");

        EditorSceneManager.MarkSceneDirty(go.scene);
        Selection.activeGameObject = go;
        Debug.Log("Ghost Run-By made in the middle of the Scene view Its 'Room' is the room in the middle of the Scene view: change it if wrong. Drag your sound onto 'Sound', then save the scene (Ctrl+S).");
    }
}
#endif
