#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tools > Rain > Apply Dialogue Background
/// Puts the picture "IMG_2219" (Setting Game by Gim/Img/Dialogue Background) on the dialogue box:
/// the box sprite is IMG_2219_0, the colour is reset to white, and the box keeps the picture's proportions.
/// </summary>
public static class DialogueBackgroundMenu
{
    private const string Path = "Assets/Setting Game by Gim/Img/Dialogue Background/IMG_2219.png";
    private const string SpriteName = "IMG_2219_0";
    private const float WidthFraction = 0.6f;   // box width as a fraction of the screen width
    private const float BottomFraction = 0.02f; // gap under the box

    [MenuItem("Tools/Rain/Apply Dialogue Background")]
    public static void Apply()
    {
        DialogueUI ui = Object.FindAnyObjectByType<DialogueUI>(FindObjectsInactive.Include);
        if (ui == null) { Debug.LogWarning("No DialogueUI in the open scene."); return; }
        SerializedProperty p = new SerializedObject(ui).FindProperty("panel");
        GameObject panel = p != null ? p.objectReferenceValue as GameObject : null;
        if (panel == null) { Debug.LogWarning("DialogueUI has no Panel assigned."); return; }
        Image img = panel.GetComponent<Image>();
        if (img == null) img = Undo.AddComponent<Image>(panel);

        Sprite sprite = null;
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(Path))
            if (o is Sprite s && s.name == SpriteName) { sprite = s; break; }
        if (sprite == null) { Debug.LogWarning("Sprite " + SpriteName + " not found in " + Path); return; }

        Undo.RecordObject(img, "Dialogue background");
        img.sprite = sprite;
        img.type = Image.Type.Simple;
        img.preserveAspect = false;
        img.color = Color.white;
        EditorUtility.SetDirty(img);

        // keep the picture's proportions on a 16:9 screen
        float aspect = sprite.rect.width / sprite.rect.height;
        float h = WidthFraction * (16f / 9f) / aspect;
        RectTransform rt = panel.GetComponent<RectTransform>();
        Undo.RecordObject(rt, "Dialogue background");
        rt.anchorMin = new Vector2(0.5f - WidthFraction * 0.5f, BottomFraction);
        rt.anchorMax = new Vector2(0.5f + WidthFraction * 0.5f, BottomFraction + h);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        EditorUtility.SetDirty(rt);

        // the text sits in the middle of the box (not stuck to its top edge), with side padding
        SerializedProperty tp = new SerializedObject(ui).FindProperty("dialogueText");
        TMPro.TMP_Text txt = tp != null ? tp.objectReferenceValue as TMPro.TMP_Text : null;
        if (txt != null)
        {
            RectTransform tr = txt.rectTransform;
            Undo.RecordObject(tr, "Dialogue text");
            Undo.RecordObject(txt, "Dialogue text");
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.pivot = new Vector2(0.5f, 0.5f);
            tr.offsetMin = new Vector2(60f, 25f);
            tr.offsetMax = new Vector2(-60f, -25f);
            txt.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            txt.margin = Vector4.zero;
            EditorUtility.SetDirty(tr); EditorUtility.SetDirty(txt);
        }

        EditorSceneManager.MarkSceneDirty(panel.scene);
        Debug.Log("Dialogue box now uses " + SpriteName + " (box height " + (h * 100f).ToString("0") + "% of the screen). Save the scene (Ctrl+S).");
    }
}
#endif
