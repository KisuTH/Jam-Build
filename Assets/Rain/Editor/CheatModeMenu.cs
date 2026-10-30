#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Tools > Rain > Setup Cheat Mode : puts a "Cheat Mode" object in the open scene (untick it to disable the cheat).</summary>
public static class CheatModeMenu
{
    [MenuItem("Tools/Rain/Setup Cheat Mode")]
    public static void Setup()
    {
        CheatMode cm = Object.FindAnyObjectByType<CheatMode>(FindObjectsInactive.Include);
        if (cm == null)
        {
            GameObject go = new GameObject("Cheat Mode");
            Undo.RegisterCreatedObjectUndo(go, "Cheat Mode");
            cm = go.AddComponent<CheatMode>();
        }
        EditorSceneManager.MarkSceneDirty(cm.gameObject.scene);
        Selection.activeGameObject = cm.gameObject;
        Debug.Log("Cheat Mode object is in the scene. Untick it to disable the cheat. Save the scene (Ctrl+S).");
    }
}
#endif
