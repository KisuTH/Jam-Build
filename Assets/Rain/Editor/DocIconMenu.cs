#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class DocIconMenu
{
    [MenuItem("Tools/Rain/Assign Doc Picture To Red File Items")]
    public static void Assign()
    {
        Sprite doc = null;
        foreach (string guid in AssetDatabase.FindAssets("doc t:Sprite"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith("/Img/Item/doc.png")) continue;
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
                if (o is Sprite sp) { doc = sp; break; }
            if (doc != null) break;
        }
        if (doc == null) { Debug.LogWarning("doc.png (Img/Item) not found."); return; }

        int n = 0;
        foreach (DrawerSpot d in Object.FindObjectsByType<DrawerSpot>(FindObjectsInactive.Include))
        {
            string key = (d.itemId + " " + d.itemName).ToLower();
            if (!(key.Contains("red") || key.Contains("file") || key.Contains("folder") || key.Contains("doc"))) continue;
            Undo.RecordObject(d, "Doc icon");
            d.icon = doc;
            d.iconResource = "";
            EditorUtility.SetDirty(d);
            n++;
        }
        foreach (ItemPickup p in Object.FindObjectsByType<ItemPickup>(FindObjectsInactive.Include))
        {
            string key = p.name.ToLower();
            if (!(key.Contains("red") || key.Contains("file") || key.Contains("folder") || key.Contains("doc"))) continue;
            Undo.RecordObject(p, "Doc icon");
            p.icon = doc;
            EditorUtility.SetDirty(p);
            n++;
        }
        Debug.Log("Doc picture set on " + n + " item(s). Save the scene (Ctrl+S).");
    }
}
#endif
