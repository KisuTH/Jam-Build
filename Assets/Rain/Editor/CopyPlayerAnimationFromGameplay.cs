#if UNITY_EDITOR
// Menu: Tools > Rain > Copy Player Animation From Gameplay (match Anon size)
// Open the scene that should get the animated Player (e.g. "Game main"), then run this menu.
// It reads the Player's animation setup from Assets/Scenes/Gameplay.unity (every clip slot, size multipliers,
// body anchors) and puts the same on this scene's Player ("Visual" child with SpriteRenderer + Animator + PlayerAnimation).
// The character is then scaled so that it is exactly as tall as the Anon NPC of the open scene
// (same proportion as Player vs. Anon in Gameplay). Safe to run again.
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CopyPlayerAnimationFromGameplay
{
    // 1 = jump as many body heights as in Gameplay (felt too high). Lower = lower jump.
    private const float JumpFactor = 0.4f;
    private const string GameplayPath = "Assets/Scenes/Gameplay.unity";

    [MenuItem("Tools/Rain/Copy Player Animation From Gameplay (match Anon size)")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) { Warn("Stop Play mode first."); return; }

        PlayerMovement2D player = Object.FindAnyObjectByType<PlayerMovement2D>();
        if (player == null) { Warn("No Player (PlayerMovement2D) in the open scene."); return; }
        Scene target = player.gameObject.scene;
        if (target.path == GameplayPath) { Warn("This is the Gameplay scene itself. Open the scene that needs the animation (e.g. Game main)."); return; }

        SpriteRenderer anonHere = FindAnon(target);
        if (anonHere == null) { Warn("No anon NPC (a SpriteRenderer using 'anim_anon...') in the open scene."); return; }

        Scene gp = SceneManager.GetSceneByPath(GameplayPath);
        bool openedHere = false;
        if (!gp.isLoaded) { gp = EditorSceneManager.OpenScene(GameplayPath, OpenSceneMode.Additive); openedHere = true; }

        try
        {
            PlayerAnimation src = null;
            foreach (GameObject r in gp.GetRootGameObjects())
            {
                src = r.GetComponentInChildren<PlayerAnimation>(true);
                if (src != null) break;
            }
            if (src == null) { Warn("The Gameplay scene has no PlayerAnimation (run 'Hook Up Player Animation' there and save)."); return; }
            SpriteRenderer srcSr = src.GetComponent<SpriteRenderer>();
            SpriteRenderer anonGp = FindAnon(gp);
            float ratio = 1f;
            if (anonGp != null && anonGp.bounds.size.y > 0.0001f) ratio = anonHere.bounds.size.y / anonGp.bounds.size.y;
            else Debug.LogWarning("No anon in Gameplay to compare with: using the same character size as Gameplay.");

            GameObject go = player.gameObject;
            SpriteRenderer oldSr = go.GetComponent<SpriteRenderer>();

            Transform vis = go.transform.Find("Visual");
            if (vis == null)
            {
                GameObject v = new GameObject("Visual");
                Undo.RegisterCreatedObjectUndo(v, "Create Visual");
                v.transform.SetParent(go.transform, false);
                vis = v.transform;
            }
            SpriteRenderer vsr = vis.GetComponent<SpriteRenderer>();
            if (vsr == null) vsr = Undo.AddComponent<SpriteRenderer>(vis.gameObject);
            if (oldSr != null && oldSr != vsr)
            {
                vsr.sortingLayerID = oldSr.sortingLayerID;
                vsr.sortingOrder = oldSr.sortingOrder;
                vsr.sharedMaterial = oldSr.sharedMaterial;
                oldSr.enabled = false;
            }
            if (srcSr != null) { vsr.sprite = srcSr.sprite; if (srcSr.sharedMaterial != null) vsr.sharedMaterial = srcSr.sharedMaterial; }

            Animator an = vis.GetComponent<Animator>();
            if (an == null) an = Undo.AddComponent<Animator>(vis.gameObject);
            an.runtimeAnimatorController = null;
            PlayerAnimation dst = vis.GetComponent<PlayerAnimation>();
            if (dst == null) dst = Undo.AddComponent<PlayerAnimation>(vis.gameObject);

            Undo.RecordObject(dst, "Copy player animation");
            EditorUtility.CopySerialized(src, dst);

            SerializedObject so = new SerializedObject(dst);
            SerializedProperty scale = so.FindProperty("spriteWorldScale");
            float old = scale.floatValue;
            scale.floatValue = old * ratio;
            so.ApplyModifiedProperties();

            // Jump height: same number of "body heights" as in Gameplay (a bigger character needs a bigger jump)
            PlayerMovement2D gpMove = null;
            foreach (GameObject r in gp.GetRootGameObjects())
            {
                gpMove = r.GetComponentInChildren<PlayerMovement2D>(true);
                if (gpMove != null) break;
            }
            if (gpMove != null)
            {
                SerializedObject gpSo = new SerializedObject(gpMove);
                float gpJump = gpSo.FindProperty("jumpHeight").floatValue;
                SerializedObject mv = new SerializedObject(player);
                mv.FindProperty("jumpHeight").floatValue = gpJump * ratio * JumpFactor;
                mv.ApplyModifiedProperties();
                Debug.Log("Jump height " + gpJump.ToString("F1") + " -> " + (gpJump * ratio * JumpFactor).ToString("F1") + " (Player > Player Movement 2D > Jump Height, change it there if you want it higher/lower).");
            }

            EditorUtility.SetDirty(dst);
            EditorUtility.SetDirty(vis.gameObject);
            EditorSceneManager.MarkSceneDirty(target);
            Debug.Log("Player animation copied from Gameplay. Size: spriteWorldScale " + old.ToString("F3") + " -> " + (old * ratio).ToString("F3") +
                      " (x" + ratio.ToString("F2") + " to match the Anon of this scene). Press Ctrl+S.");
        }
        finally
        {
            if (openedHere && gp.isLoaded) EditorSceneManager.CloseScene(gp, true);
        }
    }

    private static SpriteRenderer FindAnon(Scene s)
    {
        foreach (GameObject r in s.GetRootGameObjects())
            foreach (SpriteRenderer sr in r.GetComponentsInChildren<SpriteRenderer>(true))
                if (sr.sprite != null && sr.sprite.name.StartsWith("anim_anon")) return sr;
        return null;
    }

    private static void Warn(string m)
    {
        Debug.LogError("Copy Player Animation: " + m);
        EditorUtility.DisplayDialog("Copy Player Animation", m, "OK");
    }
}
#endif
