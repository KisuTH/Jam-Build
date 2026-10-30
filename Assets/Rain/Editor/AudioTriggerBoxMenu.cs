#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Tools > Rain > Add Audio Trigger Box : makes a new trigger box in the middle of the Scene view and selects it.</summary>
public static class AudioTriggerBoxMenu
{
    [MenuItem("Tools/Rain/Add Audio Trigger Box")]
    public static void Add()
    {
        GameObject go = new GameObject("Audio Trigger Box");
        Undo.RegisterCreatedObjectUndo(go, "Audio Trigger Box");
        Vector3 pos = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        go.AddComponent<AudioTriggerBox>();   // Reset() makes the collider a trigger
        EditorSceneManager.MarkSceneDirty(go.scene);
        Selection.activeGameObject = go;
        Debug.Log("Audio Trigger Box made in the middle of the Scene view. Drag your sound onto 'Clip', move it where you want, then save the scene (Ctrl+S).");
    }
}
#endif
