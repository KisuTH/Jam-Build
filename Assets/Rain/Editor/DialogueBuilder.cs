// Menu: Tools > Point and Click > Create Dialogue Object
// Makes a clickable box (an "NPC") in the open scene with a DialogueSequence already connected,
// with 3 example lines. Select it and change the lines in the Inspector.
// Needs a DialogueUI in the scene (the Blockout / Long Level builders add one).
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DialogueBuilder
{
    [MenuItem("Tools/Point and Click/Create Dialogue Object")]
    public static void Create()
    {
        if (Object.FindAnyObjectByType<DialogueUI>() == null)
        {
            EditorUtility.DisplayDialog("Dialogue", "There is no DialogueUI in this scene. Run the Blockout or Long Level builder first.", "OK");
            return;
        }

        // Put it where the Scene view is looking (or under the selected object)
        Vector3 pos = Vector3.zero;
        if (SceneView.lastActiveSceneView != null) pos = SceneView.lastActiveSceneView.pivot;
        pos.z = 0f;

        Sprite sprite = BlockoutSceneBuilder.CreateWhiteSquareSprite();
        GameObject go = BlockoutSceneBuilder.MakeBlock("Dialogue NPC", null, pos, new Vector2(0.9f, 1.8f),
            new Color(0.85f, 0.7f, 0.3f), sprite, 1, true, true);
        if (Selection.activeTransform != null) go.transform.SetParent(Selection.activeTransform, true);

        Interactable2D item = go.AddComponent<Interactable2D>();
        item.displayName = "Dialogue NPC";
        item.maxDistance = 3f;

        DialogueSequence seq = go.AddComponent<DialogueSequence>();
        seq.lines = new DialogueLine[]
        {
            new DialogueLine { speaker = "Stranger", text = "Write what this person says in the Inspector." },
            new DialogueLine { speaker = "Detective", text = "Press + under Lines to add more." },
            new DialogueLine { speaker = "", text = "A line with no speaker is narration." }
        };
        UnityEventTools.AddVoidPersistentListener(item.onInteract, seq.Play);

        Selection.activeGameObject = go;
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Dialogue object created. Click it in Play mode, or edit its Lines in the Inspector.");
    }
}
