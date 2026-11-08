#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Tools > Rain > Noo Calls Player Before Old-Key Door
/// Only the door that needs "old_key" is changed: after the key check it also needs the flag "talked_to_noo"
/// (turned on when the player talks to Noo, "anim_anon_0"); until then Noo calls out and the door stays shut.
/// The earlier gate on the keypad ("Pass Code2") is switched back to the prefab's original values.
/// </summary>
public static class NooCallsPlayerMenu
{
    const string Flag = "talked_to_noo";

    [MenuItem("Tools/Rain/Noo Calls Player Before Old-Key Door")]
    static void Run()
    {
        Interactable2D door = null;
        Interactable2D pad = null;
        Interactable2D nooI = null;
        foreach (Interactable2D i in Object.FindObjectsByType<Interactable2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (i.requiredItemId == "old_key") door = i;
            if (i.gameObject.name == "anim_anon_0" && nooI == null) nooI = i;
            if (i.transform.root.name == "Pass Code2" || i.gameObject.name == "Pass Code2") pad = i;
        }
        if (door == null || nooI == null)
        {
            Debug.LogWarning("Could not find the door that needs 'old_key' and/or Noo ('anim_anon_0') in the open scene.");
            return;
        }

        // 1. Noo turns the flag on when talked to
        var a = new SerializedObject(nooI);
        a.FindProperty("setsFlag").stringValue = Flag;
        a.ApplyModifiedProperties();

        // 2. only the old_key door waits for it
        var b = new SerializedObject(door);
        SerializedProperty rf = b.FindProperty("requiredFlags");
        rf.arraySize = 1;
        rf.GetArrayElementAtIndex(0).stringValue = Flag;
        b.FindProperty("missingFlagsMessage").stringValue = "Noo: Wait... come here and talk to me first.";
        b.ApplyModifiedProperties();

        // 3. undo the earlier keypad gate (back to the prefab's own values)
        if (pad != null)
        {
            var c = new SerializedObject(pad);
            foreach (string name in new[] { "requiredFlags", "missingFlagsMessage" })
            {
                SerializedProperty p = c.FindProperty(name);
                if (p != null && PrefabUtility.IsPartOfPrefabInstance(pad)) PrefabUtility.RevertPropertyOverride(p, InteractionMode.AutomatedAction);
            }
            c.ApplyModifiedProperties();
        }

        EditorSceneManager.MarkSceneDirty(nooI.gameObject.scene);
        Debug.Log("Done: '" + door.name + "' (old_key door) now needs '" + Flag + "'; keypad gate reverted. Save the scene (Ctrl+S).");
    }
}
#endif
