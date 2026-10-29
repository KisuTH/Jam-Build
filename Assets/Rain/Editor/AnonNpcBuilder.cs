// Menu:  Tools > Rain > Hook Up Anon NPC Animation
// Puts the animation from Assets/test_anim/anon (anim_anon sprites + anim_anon_0 controller with anon_idle)
// onto the "Dialogue NPC" object of the open scene:
//   - a child "Visual" gets the SpriteRenderer + Animator (the clip animates the sprite of that object)
//   - the old yellow block is hidden, the click box (Collider) is resized to the character
// Safe to run again. Save the scene afterwards (Ctrl+S).
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AnonNpcBuilder
{
    private const string SheetPath = "Assets/test_anim/anon/anim_anon.png";
    private const string ControllerPath = "Assets/test_anim/anon/anim_anon_0.controller";
    private const float CharacterHeight = 2.6f;   // same height as the player (world units)

    [MenuItem("Tools/Rain/Hook Up Anon NPC Animation")]
    public static void Build()
    {
        const string title = "Anon NPC";
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(title, "Stop Play mode first.", "OK");
            return;
        }

        GameObject npc = null;
        foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (go.name == "Dialogue NPC" && go.scene.IsValid()) { npc = go; break; }
        if (npc == null)
        {
            EditorUtility.DisplayDialog(title, "There is no 'Dialogue NPC' in the open scene. Open the Gameplay scene first.", "OK");
            return;
        }

        Sprite first = null;
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(SheetPath))
        {
            Sprite sp = o as Sprite;
            if (sp != null && (first == null || sp.name == "anim_anon_0")) first = sp;
        }
        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
        if (first == null || controller == null)
        {
            EditorUtility.DisplayDialog(title, "Could not load " + SheetPath + " or " + ControllerPath + ".", "OK");
            return;
        }

        Transform p = npc.transform;
        BoxCollider2D col = npc.GetComponent<BoxCollider2D>();
        // Where the feet go: the bottom-middle of the old yellow block. It is read from the block's sprite and
        // transform (NOT from the collider: its bounds are not reliable in edit mode, and the collider is resized below).
        SpriteRenderer oldBlock = npc.GetComponent<SpriteRenderer>();
        Vector3 bottomCenter;
        if (oldBlock != null && oldBlock.sprite != null)
        {
            Bounds sb = oldBlock.sprite.bounds;
            bottomCenter = p.TransformPoint(new Vector3(sb.center.x, sb.min.y, 0f));
        }
        else if (col != null)
        {
            bottomCenter = p.TransformPoint(new Vector3(col.offset.x, col.offset.y - col.size.y * 0.5f, 0f));
        }
        else bottomCenter = p.position;
        bottomCenter.z = p.position.z;

        // child "Visual"
        Transform visual = p.Find("Visual");
        if (visual == null)
        {
            GameObject v = new GameObject("Visual");
            Undo.RegisterCreatedObjectUndo(v, "Add Anon visual");
            v.transform.SetParent(p, false);
            visual = v.transform;
        }

        SpriteRenderer sr = visual.GetComponent<SpriteRenderer>();
        if (sr == null) sr = visual.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = first;
        sr.color = Color.white;
        sr.sortingOrder = 2;

        Animator anim = visual.GetComponent<Animator>();
        if (anim == null) anim = visual.gameObject.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;

        // size: the sprite is huge (about 28 units tall), so shrink it to the character height.
        float ppu = first.pixelsPerUnit;
        float spriteH = first.rect.height / ppu;
        float spriteW = first.rect.width / ppu;
        float s = CharacterHeight / spriteH;
        Vector3 lossy = p.lossyScale;
        visual.localScale = new Vector3(s / Mathf.Abs(lossy.x), s / Mathf.Abs(lossy.y), 1f);
        // the sprite's pivot is its bottom-left corner: put the feet in the middle of the old block's bottom
        visual.position = new Vector3(bottomCenter.x - spriteW * s * 0.5f, bottomCenter.y, p.position.z);

        // hide the yellow block, resize the click box to the character
        SpriteRenderer block = npc.GetComponent<SpriteRenderer>();
        if (block != null) { Undo.RecordObject(block, "Hide block"); block.enabled = false; }
        if (col != null)
        {
            Undo.RecordObject(col, "Resize NPC collider");
            float w = spriteW * s * 0.6f;
            col.size = new Vector2(w / Mathf.Abs(lossy.x), CharacterHeight / Mathf.Abs(lossy.y));
            Vector3 centerWorld = new Vector3(bottomCenter.x, bottomCenter.y + CharacterHeight * 0.5f, p.position.z);
            col.offset = p.InverseTransformPoint(centerWorld);
        }

        EditorUtility.SetDirty(npc);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = npc;
        Debug.Log("Anon animation hooked up to 'Dialogue NPC' (child 'Visual'). Press Play to see it idle. Save the scene (Ctrl+S).");
    }
}
