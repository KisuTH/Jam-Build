#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > Rain > Hook Up Player Animation
/// Creates the child "Visual" (SpriteRenderer + Animator + PlayerAnimation) under the Player of the OPEN scene
/// and pre-fills the clip slots with the clips it finds. After that you can drag any other clip into the slots yourself.
/// The friend's art all faces LEFT, so the clips go into the Left slots (Right = mirrored automatically).
/// </summary>
public static class PlayerAnimationBuilder
{
    // How tall the standing character is drawn, in world units (the Player collider is ~1.6, doors ~4.6)
    const float CharacterHeight = 2.6f;

    [MenuItem("Tools/Rain/Hook Up Player Animation")]
    public static void Run()
    {
        PlayerMovement2D player = Object.FindFirstObjectByType<PlayerMovement2D>();
        if (player == null) { Debug.LogError("No Player (PlayerMovement2D) in the open scene. Open Gameplay first."); return; }

        var clips = new List<AnimationClip>();
        foreach (string g in AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets" }))
        {
            string cp = AssetDatabase.GUIDToAssetPath(g);
            if (cp.StartsWith("Assets/Animation/")) continue;   // clips made by this menu (Player_Run ...) are not the friend's
            var c = AssetDatabase.LoadAssetAtPath<AnimationClip>(cp);
            if (c != null) clips.Add(c);
        }

        AnimationClip crouchWalk = Pick(clips, x => x.Contains("crouch") && x.Contains("walk"));
        AnimationClip crouch = Pick(clips, x => x.Contains("crouch") && !x.Contains("walk"));
        AnimationClip jump = Pick(clips, x => x.Contains("jump"));
        AnimationClip enter = Pick(clips, x => x.Contains("enter"));
        AnimationClip exit = Pick(clips, x => x.Contains("exit"));
        AnimationClip idle = Pick(clips, x => x.Contains("idle") || x.Contains("stand"));
        AnimationClip run = Pick(clips, x => x.Contains("run") && !x.Contains("crouch"));
        AnimationClip walk = Pick(clips, x => x.Contains("walk") && !x.Contains("crouch"));

        foreach (var c in clips)
            if (!HasSprites(c)) Debug.LogWarning($"Clip '{c.name}' has MISSING sprites (its PNG is not in the project). Skipped - ask your friend to push that PNG.");
        var rest = clips.Where(c => HasSprites(c) && c != idle && c != walk && c != jump && c != crouch && c != crouchWalk && c != run && c != enter && c != exit)
                        .OrderByDescending(c => c.length).ToList();
        if (idle != null && !HasSprites(idle)) idle = null;
        if (walk != null && !HasSprites(walk)) walk = null;
        if (walk == null && rest.Count > 0) { walk = rest[0]; rest.RemoveAt(0); }
        // Idle stays empty if there is no idle art: the game then freezes the first frame of Walk

        // Run: the friend's sheet Assets/anim_jogn_walk.png (5 frames, a run cycle facing LEFT) has no clip yet: make one
        AnimationClip walkRight = null;   // (not used any more: all art faces left, Right = mirrored)
        const string runSheet = "Assets/anim_jogn_walk.png";
        var runSprites = AssetDatabase.LoadAllAssetsAtPath(runSheet).OfType<Sprite>()
            .Select(sp => new { sp, n = SpriteNumber(sp.name) }).Where(x => x.n >= 0).OrderBy(x => x.n).Select(x => x.sp).ToArray();
        if (runSprites.Length > 0)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animation")) AssetDatabase.CreateFolder("Assets", "Animation");
            run = MakeClip("Player_Run", runSprites, 10f);
        }
        else Debug.LogWarning(runSheet + " not found: no Run clip created (Walk will play faster instead).");
        // Idle (standing still, breathing): the 2 idle frames of Assets/uufgjh.png (drawn bigger than the friend's other sheets,
        // so it gets its own Size Multiplier below)
        var idleSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/uufgjh.png").OfType<Sprite>()
            .Select(sp => new { sp, n = SpriteNumber(sp.name) }).Where(x => x.n >= 0 && x.n < 8).OrderBy(x => x.n).Select(x => x.sp).ToArray();
        if (idleSprites.Length > 0)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animation")) AssetDatabase.CreateFolder("Assets", "Animation");
            idle = MakeClip("Player_Idle", idleSprites, 2f);
        }
        else { idle = null; Debug.LogWarning("Assets/uufgjh.png not found: no Idle (Walk's first frame is used)."); }

        GameObject go = player.gameObject;
        SpriteRenderer oldSr = go.GetComponent<SpriteRenderer>();
        Collider2D col = go.GetComponent<Collider2D>();
        float colHeight = col != null ? col.bounds.size.y : 1.6f;

        var oldAnim = go.GetComponent<Animator>();
        if (oldAnim != null) Undo.DestroyObjectImmediate(oldAnim);
        var oldPa = go.GetComponent<PlayerAnimation>();
        if (oldPa != null) Undo.DestroyObjectImmediate(oldPa);

        Transform vis = go.transform.Find("Visual");
        if (vis == null)
        {
            var v = new GameObject("Visual");
            Undo.RegisterCreatedObjectUndo(v, "Create Visual");
            v.transform.SetParent(go.transform, false);
            vis = v.transform;
        }
        SpriteRenderer vsr = vis.GetComponent<SpriteRenderer>();
        if (vsr == null) vsr = Undo.AddComponent<SpriteRenderer>(vis.gameObject);
        if (oldSr != null)
        {
            vsr.sortingLayerID = oldSr.sortingLayerID;
            vsr.sortingOrder = oldSr.sortingOrder;
            vsr.sharedMaterial = oldSr.sharedMaterial;
            oldSr.enabled = false;
        }

        Sprite first = null;
        AnimationClip firstClip = walk != null ? walk : idle;   // the scale is based on the friend's walk sheet
        if (firstClip != null)
        {
            var ib = AnimationUtility.GetObjectReferenceCurveBindings(firstClip);
            if (ib.Length > 0) first = AnimationUtility.GetObjectReferenceCurve(firstClip, ib[0])[0].value as Sprite;
        }
        vsr.sprite = first;

        Animator animator = vis.GetComponent<Animator>();
        if (animator == null) animator = Undo.AddComponent<Animator>(vis.gameObject);
        animator.runtimeAnimatorController = null;
        var pa = vis.GetComponent<PlayerAnimation>();
        if (pa == null) pa = Undo.AddComponent<PlayerAnimation>(vis.gameObject);

        var so = new SerializedObject(pa);
        // ALL the friend's art is drawn facing LEFT -> Left slots, Right is mirrored
        Fill(so, "idle", idle, false); Fill(so, "walk", walk, false);
        Fill(so, "jump", jump, false);
        Fill(so, "run", run, false);   // empty = Walk plays faster
        Fill(so, "crouch", crouch, false); Fill(so, "crouchWalk", crouchWalk, false);
        if (enter != null) so.FindProperty("enterDoor").objectReferenceValue = enter;
        if (exit != null) so.FindProperty("exitDoor").objectReferenceValue = exit;
        // Match the standing height of the crouch/jump/door sheets to the Walk sheet
        float mul = 1f;
        {
            float walkH = StandingHeight(walk), crouchH = StandingHeight(crouch);
            if (walkH > 0f && crouchH > 0f) mul = walkH / crouchH;
        }
        const float CrouchShrink = 1.0f;   // 1 = same drawing scale as standing (0.8 made the crouch far too small)
        // The Idle art is drawn bigger than the others: shrink it to the same body size as Walk
        float idleMul = 1f;
        {
            float walkH = StandingHeight(walk), idleH = StandingHeight(idle);
            if (walkH > 0f && idleH > 0f) idleMul = walkH / idleH;
        }
        so.FindProperty("idle.sizeMultiplier").floatValue = idleMul;
        so.FindProperty("jump.sizeMultiplier").floatValue = mul;
        so.FindProperty("crouch.sizeMultiplier").floatValue = mul * CrouchShrink;
        // Same BODY size as the squat: the head is ~6% bigger in the crouch-walk sheet (hair area 24.0k vs 21.3k px -> sqrt = 0.94).
        // (The crouch-walk pose is naturally taller than the deep squat; 0.79 would make them equally tall but the head smaller.)
        const float CrouchWalkMatch = 0.94f;
        so.FindProperty("crouchWalk.sizeMultiplier").floatValue = mul * CrouchShrink * CrouchWalkMatch;
        // where each state freezes while its key is held
        so.FindProperty("crouch.holdTime").floatValue = HoldTime(crouch, true);
        so.FindProperty("jump.holdTime").floatValue = HoldTime(jump, false);
        so.FindProperty("doorSizeMultiplier").floatValue = mul;
        FillAnchors(so, new[] { idle, walk, walkRight, run, jump, crouch, crouchWalk, enter, exit });
        Debug.Log($"Size multiplier for the friend's clips: {mul:F2}, Idle: {idleMul:F2}");
        if (first != null) so.FindProperty("spriteWorldScale").floatValue = CharacterHeight / first.bounds.size.y;
        so.ApplyModifiedProperties();

        Debug.Log($"Filled slots (Left) -> Idle: {N(idle)} | Walk: {N(walk)} | Jump: {N(jump)} | Crouch: {N(crouch)} | CrouchWalk: {N(crouchWalk)} | EnterDoor: {N(enter)} | ExitDoor: {N(exit)}. Select 'Visual' under Player to change them.");
        EditorUtility.SetDirty(go);
        EditorUtility.SetDirty(vis.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        AssetDatabase.SaveAssets();
    }

    static void Fill(SerializedObject so, string slot, AnimationClip clip, bool facesRight)
    {
        so.FindProperty(slot + ".right").objectReferenceValue = facesRight ? clip : null;
        so.FindProperty(slot + ".left").objectReferenceValue = facesRight ? null : clip;
    }

    // Time (seconds) where a held state should freeze.
    // crouch: down-and-up clips (first sprite == last sprite) freeze at the middle key = the deepest pose.
    // jump: freeze on the second to last key (the falling pose); the last one is the landing.
    static float HoldTime(AnimationClip c, bool crouchLike)
    {
        if (c == null) return 0f;
        var bs = AnimationUtility.GetObjectReferenceCurveBindings(c);
        if (bs.Length == 0) return 0f;
        var k = AnimationUtility.GetObjectReferenceCurve(c, bs[0]);
        if (k.Length < 3) return 0f;
        if (crouchLike)
            return k[0].value == k[k.Length - 1].value ? k[k.Length / 2].time : 0f;
        return k[k.Length - 2].time;
    }

    // Stable body anchor per sprite: average x of the opaque pixels in the upper half (head + torso),
    // measured from the sprite pivot. The art cuts each frame around the whole figure, so the legs
    // swinging would otherwise make the body sway.
    static void FillAnchors(SerializedObject so, AnimationClip[] clipList)
    {
        var sprites = new List<Sprite>();
        foreach (var c in clipList)
        {
            if (c == null) continue;
            foreach (var b in AnimationUtility.GetObjectReferenceCurveBindings(c))
                foreach (var key in AnimationUtility.GetObjectReferenceCurve(c, b))
                {
                    var sp = key.value as Sprite;
                    if (sp != null && !sprites.Contains(sp)) sprites.Add(sp);
                }
        }

        var prop = so.FindProperty("anchors");
        prop.ClearArray();
        int n = 0;
        foreach (var group in sprites.GroupBy(sp => AssetDatabase.GetAssetPath(sp.texture)))
        {
            string path = group.Key;
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path)) continue;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(tex, System.IO.File.ReadAllBytes(path))) { Object.DestroyImmediate(tex); continue; }
            foreach (var sp in group)
            {
                Rect r = sp.rect;
                int x0 = Mathf.Clamp(Mathf.RoundToInt(r.x), 0, tex.width - 1);
                int w = Mathf.Clamp(Mathf.RoundToInt(r.width), 1, tex.width - x0);
                int h = Mathf.Max(1, Mathf.RoundToInt(r.height * 0.5f));
                int y0 = Mathf.Clamp(Mathf.RoundToInt(r.yMax) - h, 0, tex.height - h);
                var px = tex.GetPixels(x0, y0, w, h);
                double sum = 0; long cnt = 0;
                for (int i = 0; i < px.Length; i++)
                    if (px[i].a > 0.5f) { sum += i % w; cnt++; }
                if (cnt == 0) continue;
                float meanPx = (float)(sum / cnt) + (x0 - r.x);          // from the left edge of the sprite rect
                float local = (meanPx - sp.pivot.x) / sp.pixelsPerUnit;     // from the pivot, in world units of the sprite
                prop.InsertArrayElementAtIndex(n);
                var el = prop.GetArrayElementAtIndex(n);
                el.FindPropertyRelative("sprite").objectReferenceValue = sp;
                el.FindPropertyRelative("x").floatValue = local;
                n++;
            }
            Object.DestroyImmediate(tex);
        }
        Debug.Log($"Body anchors computed for {n} sprites.");
    }

    // Height (world units) of the first sprite of a clip (the crouch clip starts standing up)
    static float StandingHeight(AnimationClip c)
    {
        if (c == null) return 0f;
        var bs = AnimationUtility.GetObjectReferenceCurveBindings(c);
        if (bs.Length == 0) return 0f;
        var sp = AnimationUtility.GetObjectReferenceCurve(c, bs[0])[0].value as Sprite;
        return sp != null ? sp.bounds.size.y : 0f;
    }

    static int SpriteNumber(string name)
    {
        int i = name.LastIndexOf('_');
        int n;
        return i >= 0 && int.TryParse(name.Substring(i + 1), out n) ? n : -1;
    }

    // Creates (or refreshes) a looping sprite-swap clip in Assets/Animation
    static AnimationClip MakeClip(string name, Sprite[] sprites, float fps)
    {
        string path = "Assets/Animation/" + name + ".anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
        clip.frameRate = fps;

        var keys = new ObjectReferenceKeyframe[sprites.Length + 1];
        for (int i = 0; i < sprites.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprites[i] };
        keys[sprites.Length] = new ObjectReferenceKeyframe { time = sprites.Length / fps, value = sprites[sprites.Length - 1] };

        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
        return clip;
    }

    static AnimationClip Pick(List<AnimationClip> clips, System.Func<string, bool> match)
    {
        return clips.FirstOrDefault(c => match(c.name.ToLower().Replace(" ", "_")));
    }

    static bool HasSprites(AnimationClip c)
    {
        var bs = AnimationUtility.GetObjectReferenceCurveBindings(c);
        if (bs.Length == 0) return false;
        var keys = AnimationUtility.GetObjectReferenceCurve(c, bs[0]);
        return keys.Length > 0 && keys.All(k => k.value != null);
    }

    static string N(AnimationClip c) { return c == null ? "(none)" : c.name; }
}
#endif
