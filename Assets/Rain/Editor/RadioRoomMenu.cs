#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Tools > Rain > Setup Radio Room
/// Select ANY object inside the radio room (its door, the keypad, a wall...) in the Hierarchy, then run it:
///   - a trigger box covering the room, with RadioRoomEvent (the audio clip slot is on it),
///   - the keypad on the wall (6 digits, code 021604, no death when wrong),
///   - every door in the room needs the keypad's unlock flag.
/// It is safe to run twice: things that already exist are reused.
/// </summary>
public static class RadioRoomMenu
{
    private const string Code = "021604";

    private const string DoorMessage = "The door is locked. The keypad on the wall wants a 6-digit code.";

    /// <summary>Removes everything Setup Radio Room made (trigger, keypad, door locks), so it can be set up again in another room.</summary>
    [MenuItem("Tools/Rain/Remove Radio Room Setup")]
    public static void Remove()
    {
        int n = 0;
        foreach (RadioRoomEvent e in Object.FindObjectsByType<RadioRoomEvent>(FindObjectsInactive.Include))
        { Undo.DestroyObjectImmediate(e.gameObject); n++; }
        foreach (KeypadLock k in Object.FindObjectsByType<KeypadLock>(FindObjectsInactive.Include))
        {
            if (k == null || k.unlockFlag != RadioRoomEvent.UnlockFlag) continue;
            if (k.gameObject.name == "Radio Keypad") Undo.DestroyObjectImmediate(k.gameObject);
            else { Undo.RecordObject(k, "Reset radio keypad"); k.fixedCode = ""; k.unlockFlag = KeypadLock.DefaultFlag; k.enableAttacks = true; k.killOnWrong = true; }
            n++;
        }
        foreach (Interactable2D it in Object.FindObjectsByType<Interactable2D>(FindObjectsInactive.Include))
        {
            if (it.requiredFlags == null) continue;
            var flags = new System.Collections.Generic.List<string>(it.requiredFlags);
            if (!flags.Remove(RadioRoomEvent.UnlockFlag)) continue;
            Undo.RecordObject(it, "Unlock radio door");
            it.requiredFlags = flags.ToArray();
            if (it.missingFlagsMessage == DoorMessage) it.missingFlagsMessage = "I still don't understand enough to do this...";
            EditorUtility.SetDirty(it);
            n++;
        }
        if (n > 0) EditorSceneManager.MarkAllScenesDirty();
        Debug.Log("Radio room setup removed (" + n + " change(s)).");
    }

    [MenuItem("Tools/Rain/Setup Radio Room")]
    public static void Setup()
    {
        Transform sel = Selection.activeTransform;
        if (sel == null) { Debug.LogWarning("Select the room picture of the radio room first (e.g. 'room_4'), then run this again."); return; }
        Remove();   // start clean, so running it again on another room moves the setup there

        // The room = the picture of the room (the biggest sprite of the selected object), e.g. select "room_3".
        // Falls back to a Room component only if there is no picture.
        Vector2 min, max;
        Transform parent;
        SpriteRenderer art = null;
        float bestArea = 0f;
        foreach (SpriteRenderer s in sel.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (s.sprite == null) continue;
            float a = s.bounds.size.x * s.bounds.size.y;
            if (a > bestArea) { bestArea = a; art = s; }
        }
        string roomLabel;
        if (art != null)
        {
            min = art.bounds.min; max = art.bounds.max;
            parent = sel.GetComponent<SpriteRenderer>() != null && sel.parent != null ? sel.parent : sel;
            roomLabel = art.name;
        }
        else
        {
            Room room = FindRoom(sel);
            if (room == null) { Debug.LogWarning("Select the room picture (e.g. 'room_3') of the radio room, then run this again."); return; }
            min = room.boundsMin; max = room.boundsMax;
            parent = room.content != null ? room.content.transform : room.transform;
            roomLabel = room.roomName;
        }
        Debug.Log("Radio room area: '" + roomLabel + "'  x " + min.x.ToString("0.0") + ".." + max.x.ToString("0.0") + "  y " + min.y.ToString("0.0") + ".." + max.y.ToString("0.0"));

        // ---- trigger box + audio ----
        RadioRoomEvent ev = null;
        foreach (RadioRoomEvent e in Object.FindObjectsByType<RadioRoomEvent>(FindObjectsInactive.Include))
            if (Inside(e.transform.position, min, max)) { ev = e; break; }
        if (ev == null)
        {
            GameObject go = new GameObject("RadioRoom Trigger");
            Undo.RegisterCreatedObjectUndo(go, "Radio room trigger");
            go.transform.SetParent(parent, false);
            Vector3 c = (min + max) * 0.5f;
            go.transform.position = new Vector3(c.x, c.y, 0f);
            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            // the collider is in the object's local space: undo the parent's scale so it covers the room in the world
            Vector3 ls = go.transform.lossyScale;
            box.size = new Vector2((max.x - min.x) / Mathf.Max(0.0001f, Mathf.Abs(ls.x)),
                                   (max.y - min.y) / Mathf.Max(0.0001f, Mathf.Abs(ls.y)));
            AudioSource src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            ev = go.AddComponent<RadioRoomEvent>();
        }
        ev.code = Code;

        // ---- keypad ----
        KeypadLock kl = null;
        foreach (KeypadLock k in Object.FindObjectsByType<KeypadLock>(FindObjectsInactive.Include))
            if (Inside(k.transform.position, min, max)) { kl = k; break; }
        bool madeKeypad = false;
        if (kl == null)
        {
            SpriteRenderer sr = null;
            foreach (SpriteRenderer s in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include))
            {
                if (s.sprite == null || !Inside(s.transform.position, min, max)) continue;
                string n = (s.name + " " + s.sprite.name).ToLower();
                if (n.Contains("pass code") || n.Contains("passcode") || n.Contains("keypad")) { sr = s; break; }
            }
            GameObject kg;
            if (sr != null) kg = sr.gameObject;
            else
            {
                kg = new GameObject("Radio Keypad");
                Undo.RegisterCreatedObjectUndo(kg, "Radio keypad");
                kg.transform.SetParent(parent, false);
                Vector3 c = (min + max) * 0.5f;
                kg.transform.position = new Vector3(c.x, c.y, 0f);
                sr = kg.AddComponent<SpriteRenderer>();
                sr.sprite = FindPassCode();
                ScaleToPlayer(kg.transform, sr.sprite, parent);
                madeKeypad = true;
            }
            KeypadMenu.PutBehindPlayer(sr);

            BoxCollider2D col = kg.GetComponent<BoxCollider2D>();
            if (col == null) col = Undo.AddComponent<BoxCollider2D>(kg);
            col.isTrigger = true;
            if (sr.sprite != null) { col.offset = sr.sprite.bounds.center; col.size = sr.sprite.bounds.size; }
            Interactable2D it = kg.GetComponent<Interactable2D>();
            if (it == null) it = Undo.AddComponent<Interactable2D>(kg);
            it.displayName = "Keypad";
            it.maxDistance = 3f;
            kl = kg.GetComponent<KeypadLock>();
            if (kl == null) kl = Undo.AddComponent<KeypadLock>(kg);
            kl.keypadSprite = sr.sprite;
            if (it.onInteract.GetPersistentEventCount() == 0) UnityEventTools.AddPersistentListener(it.onInteract, kl.Open);
        }
        Undo.RecordObject(kl, "Radio keypad settings");
        kl.fixedCode = Code;
        kl.unlockFlag = RadioRoomEvent.UnlockFlag;
        kl.enableAttacks = false;
        kl.killOnWrong = false;
        EditorUtility.SetDirty(kl);

        // ---- doors ----
        int locked = 0;
        foreach (Interactable2D it in Object.FindObjectsByType<Interactable2D>(FindObjectsInactive.Include))
        {
            if (!Inside(it.transform.position, min, max)) continue;
            if (it.GetComponent<KeypadLock>() != null) continue;
            string nm = (it.name + " " + it.displayName).ToLower();
            if (it.GetComponent<RoomDoor>() == null && !nm.Contains("door")) continue;
            Debug.Log("Radio room: locking door '" + it.name + "' (" + it.displayName + ")");
            Undo.RecordObject(it, "Lock radio door");
            var flags = new System.Collections.Generic.List<string>(it.requiredFlags ?? new string[0]);
            if (!flags.Contains(RadioRoomEvent.UnlockFlag)) flags.Add(RadioRoomEvent.UnlockFlag);
            it.requiredFlags = flags.ToArray();
            it.missingFlagsMessage = DoorMessage;
            EditorUtility.SetDirty(it);
            locked++;
        }

        EditorSceneManager.MarkSceneDirty(ev.gameObject.scene);
        Selection.activeGameObject = ev.gameObject;
        Debug.Log("Radio room '" + roomLabel + "' is set up: trigger box" + (madeKeypad ? ", NEW keypad (move it onto the panel on the wall)" : ", keypad found") +
                  ", " + locked + " door(s) locked until the code " + Code + " is typed. " +
                  "Drag your audio onto  Radio Room Event > Clip  of '" + ev.name + "'. Then save the scene (Ctrl+S).");
    }

    private static Room FindRoom(Transform t)
    {
        Room r = t.GetComponentInParent<Room>();
        if (r != null) return r;
        foreach (Room room in Object.FindObjectsByType<Room>(FindObjectsInactive.Include))
            if (Inside(t.position, room.boundsMin, room.boundsMax)) return room;
        return null;
    }

    private static bool Inside(Vector3 p, Vector2 min, Vector2 max)
    {
        return p.x >= min.x && p.x <= max.x && p.y >= min.y && p.y <= max.y;
    }

    private static Sprite FindPassCode()
    {
        foreach (string guid in AssetDatabase.FindAssets("Pass Code t:Texture2D"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith("Pass Code.png")) continue;
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
                if (o is Sprite s) return s;
        }
        return null;
    }

    // a wall keypad is about half as tall as the player
    private static void ScaleToPlayer(Transform t, Sprite sprite, Transform parent)
    {
        float target = 0.8f;
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            SpriteRenderer psr = player.GetComponentInChildren<SpriteRenderer>();
            if (psr != null && psr.bounds.size.y > 0.1f) target = psr.bounds.size.y * 0.45f;
        }
        if (sprite == null) return;
        float k = target / Mathf.Max(0.01f, sprite.bounds.size.y);
        Vector3 ps = parent != null ? parent.lossyScale : Vector3.one;
        t.localScale = new Vector3(k / Mathf.Max(0.0001f, ps.x), k / Mathf.Max(0.0001f, ps.y), 1f);
    }
}
#endif
