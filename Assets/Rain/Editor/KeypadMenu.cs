#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;

public static class KeypadMenu
{
    // ----- helpers (also used by BoardClueMenu) -----

    /// <summary>The "Content" object of the Room whose bounds contain this point (so the new object hides with its room).</summary>
    public static Transform RoomParentAt(Vector3 p)
    {
        foreach (Room r in Object.FindObjectsByType<Room>(FindObjectsInactive.Include))
        {
            if (r.content == null) continue;
            if (p.x >= r.boundsMin.x && p.x <= r.boundsMax.x && p.y >= r.boundsMin.y && p.y <= r.boundsMax.y)
                return r.content.transform;
        }
        return null;
    }

    public static Sprite FindSprite(string name)
    {
        foreach (string guid in AssetDatabase.FindAssets(name + " t:Sprite"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
                if (o is Sprite s && s.name == name) return s;
        }
        return null;
    }

    // Finds a texture by file name and returns its sprite. forceSingle: re-import as ONE sprite (old sprite-sheet slices were wrong)
    private static Sprite FindTextureSprite(string fileName, bool forceSingle)
    {
        foreach (string guid in AssetDatabase.FindAssets(System.IO.Path.GetFileNameWithoutExtension(fileName) + " t:Texture2D"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith("/" + fileName)) continue;
            if (forceSingle)
            {
                TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti != null && (ti.spriteImportMode != SpriteImportMode.Single || ti.textureType != TextureImporterType.Sprite))
                {
                    ti.textureType = TextureImporterType.Sprite;
                    ti.spriteImportMode = SpriteImportMode.Single;
                    ti.SaveAndReimport();
                }
            }
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
                if (o is Sprite s) return s;
        }
        return null;
    }

    private static Vector3 Pivot()
    {
        Vector3 p = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
        return new Vector3(p.x, p.y, 0f);
    }

    private static Transform ParentFor(Vector3 p)
    {
        return Selection.activeTransform != null ? Selection.activeTransform : RoomParentAt(p);
    }

    // ----- menus -----

    [MenuItem("Tools/Rain/Add Keypad (at Scene view)")]
    public static void AddKeypad()
    {
        Vector3 p = Pivot();
        GameObject go = new GameObject("Keypad");
        Undo.RegisterCreatedObjectUndo(go, "Add Keypad");
        Transform parent = ParentFor(p);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position = p;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.6f, 0.8f);
        Interactable2D it = go.AddComponent<Interactable2D>();
        it.displayName = "Keypad";
        it.maxDistance = 3f;
        KeypadLock kl = go.AddComponent<KeypadLock>();
        kl.keypadSprite = FindTextureSprite("Pass Code.png", false);
        UnityEventTools.AddPersistentListener(it.onInteract, kl.Open);
        Selection.activeGameObject = go;
        Debug.Log("Added Keypad. Move it over the little panel next to the door and resize the collider. Then select the DOOR and run Tools > Rain > Lock Selected Door With Keypad.");
    }

    // Wall things (keypad, panels) must be drawn BEHIND the player, but above the room picture
    public static void PutBehindPlayer(SpriteRenderer sr)
    {
        int order = -5;
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            int min = int.MaxValue;
            foreach (SpriteRenderer p in player.GetComponentsInChildren<SpriteRenderer>(true)) min = Mathf.Min(min, p.sortingOrder);
            if (min != int.MaxValue) order = min - 1;
        }
        Undo.RecordObject(sr, "sorting");
        sr.sortingOrder = order;
        EditorUtility.SetDirty(sr);
        Debug.Log("'" + sr.name + "' sorting order = " + order + " (behind the player).");
    }

    [MenuItem("Tools/Rain/Put Selected Sprite Behind Player")]
    public static void PutSelectedBehindPlayer()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr != null) PutBehindPlayer(sr);
        }
    }

    [MenuItem("Tools/Rain/Setup Keypad On Selected Sprite")]
    public static void SetupOnSprite()
    {
        GameObject go = Selection.activeGameObject;
        SpriteRenderer sr = go != null ? go.GetComponent<SpriteRenderer>() : null;
        if (sr == null || sr.sprite == null) { Debug.LogWarning("Select the keypad sprite object (e.g. 'Pass Code') first."); return; }

        // remove the leftover empty 'Keypad' child made by Add Keypad
        foreach (KeypadLock old in go.GetComponentsInChildren<KeypadLock>(true))
            if (old.gameObject != go) Undo.DestroyObjectImmediate(old.gameObject);

        // move into the room it stands in, so it hides with that room
        Transform room = RoomParentAt(go.transform.position);
        if (room != null && go.transform.parent != room) Undo.SetTransformParent(go.transform, room, "Keypad into room");

        PutBehindPlayer(sr);

        BoxCollider2D col = go.GetComponent<BoxCollider2D>();
        if (col == null) col = Undo.AddComponent<BoxCollider2D>(go);
        col.isTrigger = true;
        col.offset = sr.sprite.bounds.center;
        col.size = sr.sprite.bounds.size;

        Interactable2D it = go.GetComponent<Interactable2D>();
        if (it == null) it = Undo.AddComponent<Interactable2D>(go);
        it.displayName = "Keypad";
        it.maxDistance = 3f;
        KeypadLock kl = go.GetComponent<KeypadLock>();
        bool fresh = kl == null;
        if (fresh) kl = Undo.AddComponent<KeypadLock>(go);
        kl.keypadSprite = sr.sprite;
        if (fresh) UnityEventTools.AddPersistentListener(it.onInteract, kl.Open);
        EditorUtility.SetDirty(go);
        Debug.Log("Keypad ready on '" + go.name + "'. Now select the door and run Lock Selected Door With Keypad.");
    }

    [MenuItem("Tools/Rain/Lock Selected Door With Keypad")]
    public static void LockDoor()
    {
        GameObject go = Selection.activeGameObject;
        if (go == null) { Debug.LogWarning("Select the door object first."); return; }
        Interactable2D it = go.GetComponent<Interactable2D>();
        if (it == null)
        {
            if (go.GetComponent<Collider2D>() == null) go.AddComponent<BoxCollider2D>().isTrigger = true;
            it = Undo.AddComponent<Interactable2D>(go);
            RoomDoor rd = go.GetComponent<RoomDoor>();
            if (rd != null) UnityEventTools.AddPersistentListener(it.onInteract, rd.Enter);
        }
        Undo.RecordObject(it, "Lock door");
        it.requiredFlags = new[] { KeypadLock.DefaultFlag };
        it.missingFlagsMessage = "The door is locked. There is a keypad beside it.";
        EditorUtility.SetDirty(it);
        Debug.Log("Door '" + go.name + "' now needs the keypad code.");
    }

    [MenuItem("Tools/Rain/Add Red Folder (doc) (at Scene view)")]
    public static void AddFolder()
    {
        Vector3 p = Pivot();
        Sprite sprite = FindTextureSprite("doc.png", false);
        GameObject go = new GameObject("RedFolder");
        Undo.RegisterCreatedObjectUndo(go, "Add Red Folder");
        Transform parent = ParentFor(p);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position = p;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 25;

        // size: about 22% of the player's height (in world units, whatever the scene scale is)
        float target = 0.6f;
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            SpriteRenderer psr = player.GetComponentInChildren<SpriteRenderer>();
            if (psr != null && psr.bounds.size.y > 0.1f) target = psr.bounds.size.y * 0.22f;
        }
        if (sprite != null)
        {
            float k = target / Mathf.Max(0.01f, sprite.bounds.size.y);
            Vector3 ps = parent != null ? parent.lossyScale : Vector3.one;
            go.transform.localScale = new Vector3(k / Mathf.Max(0.0001f, ps.x), k / Mathf.Max(0.0001f, ps.y), 1f);
        }

        go.AddComponent<BoxCollider2D>().isTrigger = true;
        Interactable2D it = go.AddComponent<Interactable2D>();
        it.displayName = "Red folder";
        ItemPickup ip = go.AddComponent<ItemPickup>();
        ip.itemId = "red_folder";
        ip.itemName = "Red folder";
        ip.itemDescription = "A red folder. The label is torn off.";
        ip.icon = sprite;
        ip.pickupMessage = "Picked up a red folder.";
        UnityEventTools.AddPersistentListener(it.onInteract, ip.Pickup);
        Selection.activeGameObject = go;
        Debug.Log("Added a red folder. Move it where you want it and scale it if needed.");
    }
}
#endif
