#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class KeyIconMenu
{
    [MenuItem("Tools/Rain/Assign Key Picture To Key Items")]
    public static void Assign()
    {
        Sprite key = null;
        foreach (string guid in AssetDatabase.FindAssets("key t:Texture2D"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith("/Img/Item/key.png")) continue;
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
                if (o is Sprite s) { key = s; break; }
            break;
        }
        if (key == null) { Debug.LogWarning("key.png (Img/Item) not found."); return; }

        int n = 0;
        foreach (DrawerSpot d in Object.FindObjectsByType<DrawerSpot>(FindObjectsInactive.Include))
        {
            if (!IsKey(d.itemId) && !IsKey(d.itemName)) continue;
            Undo.RecordObject(d, "Key picture"); d.icon = key; d.iconResource = ""; EditorUtility.SetDirty(d); n++;
        }
        foreach (ItemPickup p in Object.FindObjectsByType<ItemPickup>(FindObjectsInactive.Include))
        {
            if (!IsKey(p.itemId) && !IsKey(p.itemName)) continue;
            if (p.icon != null) continue;
            Undo.RecordObject(p, "Key picture"); p.icon = key; EditorUtility.SetDirty(p); n++;
        }
        Debug.Log("Key picture assigned to " + n + " object(s). Save the scene (Ctrl+S).");
    }

    private static bool IsKey(string s) { return !string.IsNullOrEmpty(s) && s.ToLowerInvariant().Contains("key"); }
}
#endif
