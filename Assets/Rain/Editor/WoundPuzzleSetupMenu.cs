#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// The wound puzzle loads its pictures from a Resources folder. This copies the empty dressing table and the item
/// sheet (item_0 tray, item_1 patch, item_2 bandage, item_3 bottle, item_4 scissors, item_6 papers, item_8 gauze,
/// item_10 antiseptic box) there. The originals are left where they are.
/// </summary>
public static class WoundPuzzleSetupMenu
{
    [MenuItem("Tools/Rain/Setup Wound Puzzle Pictures")]
    public static void Setup()
    {
        string table = Find("/Img/Item/Emypy dressing table.png");
        string items = Find("/Img/Item/item.png");
        if (table == null || items == null) { Debug.LogWarning("Wound puzzle pictures not found (Emypy dressing table.png / item.png in Img/Item)."); return; }

        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/WoundPuzzle")) AssetDatabase.CreateFolder("Assets/Resources", "WoundPuzzle");

        Copy(table, "Assets/Resources/WoundPuzzle/wound_table_empty.png");
        Copy(items, "Assets/Resources/WoundPuzzle/wound_items.png");
        AssetDatabase.Refresh();
        Debug.Log("Wound puzzle pictures copied to Assets/Resources/WoundPuzzle.");
    }

    private static string Find(string suffix)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D"))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (p.EndsWith(suffix)) return p;
        }
        return null;
    }

    private static void Copy(string from, string to)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(to) != null) AssetDatabase.DeleteAsset(to);
        AssetDatabase.CopyAsset(from, to);
    }
}
#endif
