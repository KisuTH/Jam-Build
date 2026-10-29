// Menu: Tools > Rain > Apply Dialogue Box Background
// Puts Assets/Dialogue/IMG_2219.png behind the text of the dialogue box (DialogueUI).
// The picture is a full 1920x1080 frame with the box drawn in the lower middle, so the
// dialogue panel is moved/resized onto that box and the picture is stretched around it.
// Safe to run again.
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class DialogueBackgroundBuilder
{
    private const string TexPath = "Assets/Dialogue/IMG_2219.png";
    private const string BgName = "DialogueBG";

    // Where the box sits inside the 1920x1080 picture (pixels, origin top-left)
    private const float BoxL = 355f, BoxR = 1605f, BoxTop = 667f, BoxBottom = 977f;
    private const float W = 1920f, H = 1080f;

    [MenuItem("Tools/Rain/Apply Dialogue Box Background")]
    public static void Apply()
    {
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexPath);
        if (tex == null) { EditorUtility.DisplayDialog("Dialogue", "Cannot find " + TexPath, "OK"); return; }

        DialogueUI ui = Object.FindAnyObjectByType<DialogueUI>();
        if (ui == null) { EditorUtility.DisplayDialog("Dialogue", "There is no DialogueUI in this scene.", "OK"); return; }

        SerializedObject so = new SerializedObject(ui);
        GameObject panel = so.FindProperty("panel").objectReferenceValue as GameObject;
        TMP_Text text = so.FindProperty("dialogueText").objectReferenceValue as TMP_Text;
        if (panel == null || text == null) { EditorUtility.DisplayDialog("Dialogue", "DialogueUI has no panel / text assigned.", "OK"); return; }

        RectTransform prt = panel.GetComponent<RectTransform>();
        Undo.RegisterFullObjectHierarchyUndo(panel, "Dialogue background");

        // 1) panel = the drawn box
        float x0 = BoxL / W, x1 = BoxR / W;
        float y0 = 1f - BoxBottom / H, y1 = 1f - BoxTop / H;
        prt.anchorMin = new Vector2(x0, y0);
        prt.anchorMax = new Vector2(x1, y1);
        prt.offsetMin = Vector2.zero;
        prt.offsetMax = Vector2.zero;

        // old flat background of the panel -> invisible
        Image old = panel.GetComponent<Image>();
        if (old != null) { Undo.RecordObject(old, "hide"); old.enabled = false; }

        // 2) picture, stretched so that the WHOLE frame covers the screen
        Transform existing = panel.transform.Find(BgName);
        GameObject bg = existing != null ? existing.gameObject : new GameObject(BgName, typeof(RectTransform), typeof(RawImage));
        bg.transform.SetParent(panel.transform, false);
        bg.transform.SetAsFirstSibling();
        RectTransform brt = bg.GetComponent<RectTransform>();
        float pw = x1 - x0, ph = y1 - y0;
        brt.anchorMin = new Vector2(-x0 / pw, -y0 / ph);
        brt.anchorMax = new Vector2((1f - x0) / pw, (1f - y0) / ph);
        brt.offsetMin = Vector2.zero;
        brt.offsetMax = Vector2.zero;
        RawImage raw = bg.GetComponent<RawImage>();
        raw.texture = tex;
        raw.color = Color.white;
        raw.raycastTarget = false;

        // 3) text inside the box (below the little name tab at the top-left)
        RectTransform trt = text.rectTransform;
        Undo.RecordObject(trt, "text");
        Undo.RecordObject(text, "text");
        if (!text.transform.IsChildOf(panel.transform)) text.transform.SetParent(panel.transform, false);
        text.transform.SetAsLastSibling();
        trt.anchorMin = new Vector2(0.04f, 0.06f);
        trt.anchorMax = new Vector2(0.96f, 0.76f);
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.color = new Color(0.96f, 0.96f, 0.96f, 1f);
        text.overflowMode = TextOverflowModes.Overflow;

        // 4) name tab (the dark little rectangle at the top-left of the picture)
        Transform tagT = panel.transform.Find("SpeakerTag");
        GameObject tag;
        if (tagT != null) tag = tagT.gameObject;
        else
        {
            tag = new GameObject("SpeakerTag", typeof(RectTransform), typeof(TextMeshProUGUI));
            tag.transform.SetParent(panel.transform, false);
        }
        tag.transform.SetAsLastSibling();
        RectTransform srt = tag.GetComponent<RectTransform>();
        float tabR = (631f - BoxL) / (BoxR - BoxL);
        float tabLow = 1f - (750f - BoxTop) / (BoxBottom - BoxTop);
        srt.anchorMin = new Vector2(0.005f, tabLow + 0.02f);
        srt.anchorMax = new Vector2(tabR - 0.005f, 0.99f);
        srt.offsetMin = Vector2.zero;
        srt.offsetMax = Vector2.zero;
        TextMeshProUGUI st = tag.GetComponent<TextMeshProUGUI>();
        st.font = text.font;
        st.fontSharedMaterial = text.fontSharedMaterial;
        st.fontStyle = FontStyles.Bold;
        st.color = new Color(1f, 0.82f, 0.5f, 1f);
        st.alignment = TextAlignmentOptions.Center;
        st.enableAutoSizing = true;
        st.fontSizeMin = 10;
        st.fontSizeMax = Mathf.Max(12f, text.fontSize);
        st.raycastTarget = false;
        st.text = "Name";
        so.FindProperty("speakerText").objectReferenceValue = st;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(panel);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Dialogue box background applied. Press Ctrl+S, then test with the anon NPC.");
    }
}
