#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Tools > Rain > Setup Game Sfx
///   - makes the "Game Sfx" object,
///   - makes the Tags "Document" and "Key" and gives them their sounds (Assets/Audio/SFX/Doc-Pickup.mp3, Key-Pickup.mp3),
///   - tags every ItemPickup that has no tag yet: ids/names with "key" -> Key, everything else -> Document.
/// Change an item's sound later by changing its Tag in the Inspector (top left), or add more rows to Tag Sounds.
/// </summary>
public static class GameSfxMenu
{
    [MenuItem("Tools/Rain/Setup Game Sfx")]
    public static void Setup()
    {
        UnityEditorInternal.InternalEditorUtility.AddTag("Document");
        UnityEditorInternal.InternalEditorUtility.AddTag("Key");

        GameSfx sfx = Object.FindAnyObjectByType<GameSfx>(FindObjectsInactive.Include);
        if (sfx == null)
        {
            GameObject go = new GameObject("Game Sfx");
            Undo.RegisterCreatedObjectUndo(go, "Game Sfx");
            sfx = go.AddComponent<GameSfx>();
        }

        AudioClip doc = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Doc-Pickup.mp3");
        AudioClip key = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Key-Pickup.mp3");
        if (doc == null) Debug.LogWarning("Assets/Audio/SFX/Doc-Pickup.mp3 not found.");
        if (key == null) Debug.LogWarning("Assets/Audio/SFX/Key-Pickup.mp3 not found.");

        Undo.RecordObject(sfx, "Game Sfx sounds");
        sfx.tagSounds = new[]
        {
            new GameSfx.TagSound { tag = "Document", clip = doc, volume = 1f },
            new GameSfx.TagSound { tag = "Key", clip = key, volume = 1f },
        };
        sfx.defaultPickupSound = doc;
        EditorUtility.SetDirty(sfx);

        int tagged = 0;
        foreach (ItemPickup ip in Object.FindObjectsByType<ItemPickup>(FindObjectsInactive.Include))
        {
            if (!ip.gameObject.CompareTag("Untagged")) continue;
            string s = ((ip.itemId ?? "") + " " + (ip.itemName ?? "")).ToLower();
            Undo.RecordObject(ip.gameObject, "Tag item");
            ip.gameObject.tag = (s.Contains("key") || s.Contains("กุญแจ")) ? "Key" : "Document";
            EditorUtility.SetDirty(ip.gameObject);
            tagged++;
        }

        // notice boards (code papers + pinned documents) and other code clues get the Document tag
        foreach (BoardClue bc in Object.FindObjectsByType<BoardClue>(FindObjectsInactive.Include))
        {
            Transform pin = bc.transform.Find("Pinned Document");
            foreach (GameObject g in pin != null ? new[] { bc.gameObject, pin.gameObject } : new[] { bc.gameObject })
                tagged += TagDocument(g);
        }
        // drawers with an item inside: key items -> Key, everything else -> Document
        foreach (DrawerSpot ds in Object.FindObjectsByType<DrawerSpot>(FindObjectsInactive.Include))
        {
            if (string.IsNullOrEmpty(ds.itemId) || !ds.gameObject.CompareTag("Untagged")) continue;
            string s2 = ((ds.itemId ?? "") + " " + (ds.itemName ?? "")).ToLower();
            Undo.RecordObject(ds.gameObject, "Tag drawer");
            ds.gameObject.tag = (s2.Contains("key") || s2.Contains("กุญแจ")) ? "Key" : "Document";
            EditorUtility.SetDirty(ds.gameObject);
            tagged++;
        }
        foreach (CodeClue cc in Object.FindObjectsByType<CodeClue>(FindObjectsInactive.Include))
            tagged += TagDocument(cc.gameObject);

        EditorSceneManager.MarkSceneDirty(sfx.gameObject.scene);
        Selection.activeGameObject = sfx.gameObject;
        Debug.Log("Game Sfx ready: Tags Document + Key have their sounds, " + tagged + " item(s) tagged. Save the scene (Ctrl+S).");
    }

    private static int TagDocument(GameObject g)
    {
        if (!g.CompareTag("Untagged")) return 0;
        Undo.RecordObject(g, "Tag document");
        g.tag = "Document";
        EditorUtility.SetDirty(g);
        return 1;
    }
}
#endif
