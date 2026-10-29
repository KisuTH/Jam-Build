#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class HintPhoneMenu
{
    [MenuItem("Tools/Rain/Assign Phone Picture To Hint Button")]
    public static void Assign()
    {
        Sprite phone = null;
        foreach (string guid in AssetDatabase.FindAssets("phon t:Texture2D"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith("/Img/Item/phon 1.png")) continue;
            Sprite first = null;
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (!(o is Sprite sp)) continue;
                if (first == null) first = sp;
                if (sp.name == "phon 1_1") phone = sp;
            }
            if (phone == null) phone = first;
            break;
        }
        if (phone == null) { Debug.LogWarning("'phon 1' picture (Img/Item) not found."); return; }

        int n = 0;
        foreach (Button b in Object.FindObjectsByType<Button>(FindObjectsInactive.Include))
        {
            if (b.gameObject.name.IndexOf("Hint", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
            Image img = b.GetComponent<Image>();
            if (img == null) continue;
            Undo.RecordObject(img, "Hint phone picture");
            img.sprite = phone;
            img.color = Color.white;
            img.preserveAspect = true;
            EditorUtility.SetDirty(img);
            PrefabUtility.RecordPrefabInstancePropertyModifications(img);
            n++;
        }
        Debug.Log("Phone picture set on " + n + " hint button(s). Save the scene (Ctrl+S).");
    }
}
#endif
