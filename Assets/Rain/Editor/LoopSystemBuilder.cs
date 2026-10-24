// Adds the "Return by Death" puzzle to the open scene (menu: Tools > Point and Click > Add Loop + AI Call + Passcode).
// Best used on a scene that already has the Long Test Level (it uses its rooms and exit door), but it also
// works alone (it then puts the objects under one object called "LoopPuzzle").
//
// What it adds:
//   - LoopManager (random code + death + restart), AICaller (the phone call box), PasscodeUI (keypad window)
//   - 7 code spots (Calendar, Poster, Wall Clock, Bedside Note, Stain, Wall Writing, Cabinet Tag). Each loop 4 of them
//     really show a digit, the others are decoys, so after dying the digits are in new places. A Keypad next to the exit door
//   - a "Basement" door in room 1: the wrong way (the AI warns you once, then you die)
//   - the Exit Door now also needs the code
//   - a Hint button (2 hints per life; the AI scolds, then gives the hint; a 3rd request = death)
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class LoopSystemBuilder
{
    [MenuItem("Tools/Point and Click/Add Loop + AI Call + Passcode To Current Scene")]
    public static void Build()
    {
        string title = "Loop + AI + Passcode";

        if (Object.FindAnyObjectByType<LoopManager>() != null)
        {
            EditorUtility.DisplayDialog(title, "This scene already has a LoopManager. Delete the old loop objects first.", "OK");
            return;
        }
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog(title, "There is no Canvas in this scene. Run the Blockout or Long Level builder first.", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog(title,
                "This adds the AI phone call, the random passcode puzzle and the death loop to the open scene.\n\n" +
                "After this: SAVE the scene (Ctrl+S) before Play, because dying reloads the scene from disk.\n" +
                "The scene is NOT saved automatically.", "Add", "Cancel"))
            return;

        try
        {
            Run(canvas);
        }
        catch (System.Exception e)
        {
            Debug.LogError("LoopSystemBuilder failed: " + e);
            EditorUtility.DisplayDialog(title, "Something went wrong:\n" + e.Message + "\n\nSee the Console.", "OK");
        }
    }

    // Adds ONLY the hint button + HintSystem to a scene that already has the loop system
    [MenuItem("Tools/Point and Click/Add Hint Button To Current Scene")]
    public static void AddHintOnly()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null || Object.FindAnyObjectByType<LoopManager>() == null)
        {
            EditorUtility.DisplayDialog("Hint button", "Run \"Add Loop + AI Call + Passcode\" first (the hints need the loop system and a Canvas).", "OK");
            return;
        }
        if (Object.FindAnyObjectByType<HintSystem>() != null)
        {
            EditorUtility.DisplayDialog("Hint button", "This scene already has a HintSystem.", "OK");
            return;
        }
        GameObject systems = GameObject.Find("GameSystems");
        if (systems == null) systems = new GameObject("GameSystems");
        BuildHintButton(canvas.transform, systems);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Hint button added (top right, under the Case Book button). Save the scene.");
    }

    private static void Run(Canvas canvas)
    {
        Sprite sprite = BlockoutSceneBuilder.CreateWhiteSquareSprite();

        // Where to put the world objects: inside each room's Content (so rooms hide / show), if it exists
        Transform fallback = new GameObject("LoopPuzzle").transform;
        Transform room1 = Find("Content: Room 1", fallback);
        Transform room2 = Find("Content: Room 2", fallback);
        Transform room3 = Find("Content: Room 3", fallback);

        // ---------- Systems ----------
        GameObject systems = GameObject.Find("GameSystems");
        if (systems == null) systems = new GameObject("GameSystems");
        LoopManager loop = systems.AddComponent<LoopManager>();

        // ---------- UI ----------
        Transform ui = canvas.transform;
        TMP_Text deathText = BuildDeathText(ui);
        BuildCallBox(ui);
        PasscodeUI keypadUI = BuildKeypad(ui);
        BuildHintButton(ui, systems);

        SerializedObject loopSO = new SerializedObject(loop);
        loopSO.FindProperty("deathText").objectReferenceValue = deathText;
        loopSO.ApplyModifiedPropertiesWithoutUndo();

        // ---------- 7 places that can hide a digit (each loop only 4 of them really do) ----------
        AddSpots(room1, room2, room3, sprite, 0, 7);

        // ---------- Keypad next to the exit door ----------
        GameObject keypad = BlockoutSceneBuilder.MakeBlock("Keypad", room3, new Vector2(32.4f, -0.8f), new Vector2(0.6f, 0.9f),
            new Color(0.15f, 0.55f, 0.35f), sprite, 1, true, true);
        Interactable2D keypadItem = keypad.AddComponent<Interactable2D>();
        keypadItem.displayName = "Keypad";
        keypadItem.maxDistance = 3f;
        UnityEventTools.AddVoidPersistentListener(keypadItem.onInteract, keypadUI.Open);
        Label(room3, "KEYPAD", 32.4f, 0.1f);

        // The exit door now also needs the code
        GameObject exit = GameObject.Find("Exit Door");
        if (exit != null)
        {
            Interactable2D exitItem = exit.GetComponent<Interactable2D>();
            if (exitItem != null)
            {
                exitItem.requiredFlags = new string[] { "filing_cabinet", PasscodeUI.SolvedFlag };
                exitItem.missingFlagsMessage = "The exit is sealed. There is a keypad next to it, and I should look around first.";
            }
        }
        else Debug.LogWarning("No 'Exit Door' found. The keypad sets the flag 'code_solved'; use it as a requirement on any door.");

        // ---------- The wrong way (room 1) ----------
        BlockoutSceneBuilder.MakeBlock("Basement Door Opening", room1, new Vector2(-20.5f, -0.7f), new Vector2(1.7f, 3.4f),
            new Color(0.02f, 0.0f, 0.0f), sprite, 0, false, false);
        GameObject wrong = BlockoutSceneBuilder.MakeBlock("Basement Door", room1, new Vector2(-20.5f, -0.7f), new Vector2(2f, 3.6f),
            new Color(0.45f, 0.15f, 0.15f), sprite, -1, true, true);
        Interactable2D wrongItem = wrong.AddComponent<Interactable2D>();
        wrongItem.displayName = "Basement Door";
        wrongItem.maxDistance = 3.5f;
        WrongChoice choice = wrong.AddComponent<WrongChoice>();
        choice.warningsBeforeDeath = 1;
        UnityEventTools.AddVoidPersistentListener(wrongItem.onInteract, choice.Choose);
        Label(room1, "BASEMENT", -20.5f, 1.5f);

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Loop + AI + Passcode added. SAVE the scene (Ctrl+S), then Play. " +
                  "Find the 4 code clues (calendar, clock, stain, wall writing), type the code on the keypad by the exit. " +
                  "A wrong code (or the Basement door, twice) = the AI calls and you die and restart with a NEW code.");
        if (fallback.childCount == 0) Object.DestroyImmediate(fallback.gameObject);
    }

    // The code spots: slot number, name, room (1-3), position, size, color, text, where the hint says it is
    private static readonly string[] SpotName =
        { "Calendar", "Wall Clock", "Stain on the Floor", "Wall Writing", "Faded Poster", "Bedside Note", "Cabinet Tag" };
    private static readonly int[] SpotRoom = { 1, 2, 3, 3, 1, 2, 3 };
    private static readonly Vector2[] SpotPos =
    {
        new Vector2(-23f, 0.9f), new Vector2(1.2f, 1.4f), new Vector2(26.7f, -2.3f), new Vector2(30.9f, 0.6f),
        new Vector2(-33.2f, 1.3f), new Vector2(-3.8f, 0.4f), new Vector2(21.4f, -0.8f)
    };
    private static readonly Vector2[] SpotSize =
    {
        new Vector2(1.2f, 1.4f), new Vector2(1f, 1f), new Vector2(0.9f, 0.4f), new Vector2(1.6f, 1f),
        new Vector2(1.2f, 1.6f), new Vector2(0.6f, 0.5f), new Vector2(0.5f, 0.4f)
    };
    private static readonly Color[] SpotColor =
    {
        new Color(0.75f, 0.70f, 0.55f), new Color(0.60f, 0.62f, 0.66f), new Color(0.45f, 0.10f, 0.10f), new Color(0.35f, 0.40f, 0.36f),
        new Color(0.55f, 0.45f, 0.35f), new Color(0.85f, 0.85f, 0.75f), new Color(0.70f, 0.60f, 0.30f)
    };
    private static readonly string[] SpotText =
    {
        "A calendar. Some dates are circled in red:",
        "The clock stopped long ago. Something is written on its glass:",
        "A dark stain. Someone dragged a finger through it:",
        "Scratched into the wall, over and over:",
        "A faded poster. Someone wrote numbers in the margin:",
        "A crumpled note stuck to the wall:",
        "A tag hangs between the cabinets:"
    };
    private static readonly string[] SpotWhere =
    {
        "the calendar in the Waiting Hall", "the clock in the Ward", "the stain on the floor in the Records Room",
        "the writing on the wall in the Records Room", "the faded poster at the far left of the Waiting Hall",
        "the note on the wall near the hospital bed in the Ward", "the tag between the filing cabinets in the Records Room"
    };

    private static void AddSpots(Transform room1, Transform room2, Transform room3, Sprite sprite, int from, int to)
    {
        Transform[] rooms = { room1, room2, room3 };
        for (int i = from; i < to; i++)
            MakeClue(rooms[SpotRoom[i] - 1], SpotName[i], SpotPos[i], SpotSize[i], SpotColor[i], sprite, i, SpotText[i]);
    }

    // For a scene that already has the first 4 code spots: adds the other 3 (Poster, Bedside Note, Cabinet Tag)
    [MenuItem("Tools/Point and Click/Add Extra Code Spots To Current Scene")]
    public static void AddExtraSpots()
    {
        if (Object.FindAnyObjectByType<LoopManager>() == null)
        {
            EditorUtility.DisplayDialog("Extra code spots", "Run \"Add Loop + AI Call + Passcode\" first.", "OK");
            return;
        }
        if (GameObject.Find("Faded Poster") != null)
        {
            EditorUtility.DisplayDialog("Extra code spots", "The extra spots are already in this scene.", "OK");
            return;
        }

        Sprite sprite = BlockoutSceneBuilder.CreateWhiteSquareSprite();
        Transform fallback = new GameObject("LoopPuzzle").transform;
        AddSpots(Find("Content: Room 1", fallback), Find("Content: Room 2", fallback), Find("Content: Room 3", fallback), sprite, 4, 7);
        if (fallback.childCount == 0) Object.DestroyImmediate(fallback.gameObject);

        // Hint texts for all 7 spots
        HintSystem hints = Object.FindAnyObjectByType<HintSystem>();
        if (hints != null)
        {
            SerializedObject so = new SerializedObject(hints);
            SerializedProperty arr = so.FindProperty("clueLocations");
            arr.arraySize = SpotWhere.Length;
            for (int i = 0; i < SpotWhere.Length; i++) arr.GetArrayElementAtIndex(i).stringValue = SpotWhere[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("3 extra code spots added (7 in total). Each loop 4 of them show a digit. Save the scene.");
    }

    // ===================================================================
    //  World objects
    // ===================================================================

    private static Transform Find(string name, Transform fallback)
    {
        GameObject go = GameObject.Find(name);
        return go != null ? go.transform : fallback;
    }

    private static void MakeClue(Transform parent, string name, Vector2 pos, Vector2 size, Color color, Sprite sprite, int slot, string flavor)
    {
        GameObject go = BlockoutSceneBuilder.MakeBlock(name, parent, pos, size, color, sprite, 0, true, true);
        Interactable2D item = go.AddComponent<Interactable2D>();
        item.displayName = name;
        item.maxDistance = 3f;
        CodeClue clue = go.AddComponent<CodeClue>();
        clue.slot = slot;
        clue.flavorText = flavor;
        UnityEventTools.AddVoidPersistentListener(item.onInteract, clue.Reveal);
    }

    private static void Label(Transform parent, string text, float x, float y)
    {
        GameObject go = new GameObject("Label: " + text);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(x, y, 0f);
        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = 4f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.65f, 0.65f, 0.70f, 0.8f);
    }

    // ===================================================================
    //  UI
    // ===================================================================

    private static TMP_Text BuildDeathText(Transform ui)
    {
        GameObject go = BlockoutSceneBuilder.UIObject("DeathText", ui);
        Stretch((RectTransform)go.transform);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = 110f;
        t.alignment = TextAlignmentOptions.Center;
        t.color = new Color(0.85f, 0.1f, 0.1f, 1f);
        t.raycastTarget = false;
        t.text = "";
        return t;
    }

    // The phone call box (top middle). The script lives on an always-active parent; the box starts hidden.
    private static void BuildCallBox(Transform ui)
    {
        GameObject root = BlockoutSceneBuilder.UIObject("AI Caller UI", ui);
        Stretch((RectTransform)root.transform);

        GameObject box = BlockoutSceneBuilder.UIObject("CallBox", root.transform);
        RectTransform brt = (RectTransform)box.transform;
        brt.anchorMin = new Vector2(0.5f, 1f);
        brt.anchorMax = new Vector2(0.5f, 1f);
        brt.pivot = new Vector2(0.5f, 1f);
        brt.anchoredPosition = new Vector2(0f, -190f);
        brt.sizeDelta = new Vector2(1100f, 200f);
        box.AddComponent<Image>().color = new Color(0.12f, 0.02f, 0.02f, 0.92f);

        TextMeshProUGUI caller = MakeText(box.transform, "CallerText", "UNKNOWN CALLER", 34f, new Color(1f, 0.35f, 0.35f),
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(-40f, 50f));
        TextMeshProUGUI line = MakeText(box.transform, "LineText", "", 38f, Color.white,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -25f), new Vector2(-60f, -90f));
        line.alignment = TextAlignmentOptions.TopLeft;

        AICaller ai = root.AddComponent<AICaller>();
        SerializedObject so = new SerializedObject(ai);
        so.FindProperty("panel").objectReferenceValue = box;
        so.FindProperty("callerText").objectReferenceValue = caller;
        so.FindProperty("lineText").objectReferenceValue = line;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Rebuilds ONLY the keypad window (the new one with Look Left / Look Right, the phone box and the addict).
    // Use it on a scene that was built with the older keypad.
    [MenuItem("Tools/Point and Click/Rebuild Keypad (Look Left-Right View)")]
    public static void RebuildKeypad()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Rebuild keypad", "There is no Canvas in this scene.", "OK");
            return;
        }

        Transform old = canvas.transform.Find("Passcode UI");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        PasscodeUI keypadUI = BuildKeypad(canvas.transform);

        // The world keypad must open the NEW window
        GameObject keypad = GameObject.Find("Keypad");
        if (keypad != null)
        {
            Interactable2D item = keypad.GetComponent<Interactable2D>();
            if (item != null)
            {
                for (int i = item.onInteract.GetPersistentEventCount() - 1; i >= 0; i--)
                    UnityEventTools.RemovePersistentListener(item.onInteract, i);
                UnityEventTools.AddVoidPersistentListener(item.onInteract, keypadUI.Open);
            }
        }
        else Debug.LogWarning("No object named 'Keypad' found: connect its Interactable2D > On Interact to PasscodeUI.Open yourself.");

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Keypad rebuilt. Save the scene (Ctrl+S).");
    }

    // The keypad window. Three views inside one dim full-screen panel:
    //   center = the keypad (+ phone box, Look Left / Look Right arrows, warning),
    //   left / right = the dark sides where the addict may stand.
    private static PasscodeUI BuildKeypad(Transform ui)
    {
        GameObject root = BlockoutSceneBuilder.UIObject("Passcode UI", ui);
        Stretch((RectTransform)root.transform);
        PasscodeUI codeUI = root.AddComponent<PasscodeUI>();

        // The whole window: a dim layer over the game (it also stops clicks reaching the world)
        GameObject panel = BlockoutSceneBuilder.UIObject("PasscodePanel", root.transform);
        Stretch((RectTransform)panel.transform);
        panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        // ---------- Center view: the keypad ----------
        GameObject center = BlockoutSceneBuilder.UIObject("View Center", panel.transform);
        Stretch((RectTransform)center.transform);

        GameObject box = BlockoutSceneBuilder.UIObject("KeypadBox", center.transform);
        RectTransform prt = (RectTransform)box.transform;
        prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = new Vector2(0f, 20f);
        prt.sizeDelta = new Vector2(540f, 760f);
        box.AddComponent<Image>().color = new Color(0.07f, 0.09f, 0.08f, 0.97f);

        TextMeshProUGUI display = MakeText(box.transform, "Display", "_ _ _ _", 90f, new Color(0.4f, 1f, 0.6f),
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -25f), new Vector2(-40f, 120f));
        TextMeshProUGUI message = MakeText(box.transform, "Message", "", 30f, new Color(0.85f, 0.85f, 0.85f),
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(-40f, 50f));

        GameObject pad = BlockoutSceneBuilder.UIObject("Pad", box.transform);
        RectTransform padRT = (RectTransform)pad.transform;
        padRT.anchorMin = padRT.anchorMax = padRT.pivot = new Vector2(0.5f, 1f);
        padRT.anchoredPosition = new Vector2(0f, -215f);
        padRT.sizeDelta = new Vector2(470f, 440f);
        GridLayoutGroup grid = pad.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(140f, 100f);
        grid.spacing = new Vector2(15f, 13f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;

        string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "CLR", "0", "OK" };
        foreach (string k in keys)
        {
            Button b = MakeButton(pad.transform, "Key " + k, k, 44f);
            if (k == "CLR") UnityEventTools.AddVoidPersistentListener(b.onClick, codeUI.Clear);
            else if (k == "OK") UnityEventTools.AddVoidPersistentListener(b.onClick, codeUI.Submit);
            else UnityEventTools.AddStringPersistentListener(b.onClick, codeUI.AddDigit, k);
        }

        Button close = MakeButton(box.transform, "Close", "Close [Esc]", 30f);
        RectTransform crt = (RectTransform)close.transform;
        crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0.5f, 0f);
        crt.anchoredPosition = new Vector2(0f, 20f);
        crt.sizeDelta = new Vector2(260f, 70f);
        UnityEventTools.AddVoidPersistentListener(close.onClick, codeUI.Close);

        // The phone call box (top left): the digits found so far
        GameObject phone = BlockoutSceneBuilder.UIObject("PhoneBox", center.transform);
        RectTransform phrt = (RectTransform)phone.transform;
        phrt.anchorMin = phrt.anchorMax = phrt.pivot = new Vector2(0f, 1f);
        phrt.anchoredPosition = new Vector2(30f, -30f);
        phrt.sizeDelta = new Vector2(430f, 150f);
        phone.AddComponent<Image>().color = new Color(0.1f, 0.03f, 0.03f, 0.95f);
        TextMeshProUGUI phoneText = MakeText(phone.transform, "PhoneText", "DOOR PASSCODE\n? - ? - ? - ?", 54f, new Color(0.4f, 1f, 0.6f),
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        // Look Left / Look Right (left and right middle of the screen)
        Button lookL = MakeSideArrow(center.transform, "LookLeft", "<  LOOK LEFT", true);
        Button lookR = MakeSideArrow(center.transform, "LookRight", "LOOK RIGHT  >", false);
        UnityEventTools.AddVoidPersistentListener(lookL.onClick, codeUI.LookLeft);
        UnityEventTools.AddVoidPersistentListener(lookR.onClick, codeUI.LookRight);
        MakeWarning(center.transform);

        // ---------- Left view ----------
        GameObject left = MakeSideView(panel.transform, "View Left", "You look to the left. Only darkness...", true, codeUI, out GameObject leftThreat);
        GameObject right = MakeSideView(panel.transform, "View Right", "You look to the right. Only darkness...", false, codeUI, out GameObject rightThreat);

        // The cue text ("...footsteps...") is on top of everything, top middle
        TextMeshProUGUI cue = MakeText(panel.transform, "CueText", "", 44f, new Color(1f, 0.5f, 0.3f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1500f, 80f));

        SerializedObject so = new SerializedObject(codeUI);
        so.FindProperty("panel").objectReferenceValue = panel;
        so.FindProperty("keypadView").objectReferenceValue = center;
        so.FindProperty("leftView").objectReferenceValue = left;
        so.FindProperty("rightView").objectReferenceValue = right;
        so.FindProperty("leftThreat").objectReferenceValue = leftThreat;
        so.FindProperty("rightThreat").objectReferenceValue = rightThreat;
        so.FindProperty("displayText").objectReferenceValue = display;
        so.FindProperty("messageText").objectReferenceValue = message;
        so.FindProperty("phoneText").objectReferenceValue = phoneText;
        so.FindProperty("cueText").objectReferenceValue = cue;
        so.ApplyModifiedPropertiesWithoutUndo();

        return codeUI;
    }

    // A big arrow button on the left or right side of the screen
    private static Button MakeSideArrow(Transform parent, string name, string label, bool leftSide)
    {
        Button b = MakeButton(parent, name, label, 38f);
        RectTransform rt = (RectTransform)b.transform;
        float x = leftSide ? 0f : 1f;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(x, 0.5f);
        rt.anchoredPosition = new Vector2(leftSide ? 40f : -40f, 0f);
        rt.sizeDelta = new Vector2(330f, 150f);
        b.GetComponent<Image>().color = new Color(0.3f, 0.12f, 0.12f, 0.95f);
        return b;
    }

    // The warning box at the bottom of the screen
    private static void MakeWarning(Transform parent)
    {
        GameObject warn = BlockoutSceneBuilder.UIObject("Warning", parent);
        RectTransform rt = (RectTransform)warn.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 25f);
        rt.sizeDelta = new Vector2(1250f, 90f);
        warn.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.05f, 0.9f);
        MakeText(warn.transform, "WarningText", "! Careful: a drug addict may attack while you enter the code. Keep checking left and right!", 30f,
            new Color(1f, 0.85f, 0.2f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-30f, 0f));
    }

    // A dark side view with a "back" arrow and a hidden threat (a black figure with red eyes)
    private static GameObject MakeSideView(Transform parent, string name, string text, bool isLeft, PasscodeUI codeUI, out GameObject threat)
    {
        GameObject view = BlockoutSceneBuilder.UIObject(name, parent);
        Stretch((RectTransform)view.transform);
        view.AddComponent<Image>().color = new Color(0.03f, 0.03f, 0.05f, 0.96f);

        MakeText(view.transform, "Text", text, 40f, new Color(0.6f, 0.6f, 0.65f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -260f), new Vector2(1400f, 80f));

        // The addict (hidden until he is really there)
        threat = BlockoutSceneBuilder.UIObject("Threat", view.transform);
        RectTransform trt = (RectTransform)threat.transform;
        trt.anchorMin = trt.anchorMax = trt.pivot = new Vector2(0.5f, 0.5f);
        trt.anchoredPosition = new Vector2(isLeft ? -250f : 250f, 20f);
        trt.sizeDelta = new Vector2(240f, 560f);
        threat.AddComponent<Image>().color = new Color(0f, 0f, 0f, 1f);
        for (int i = 0; i < 2; i++)
        {
            GameObject eye = BlockoutSceneBuilder.UIObject("Eye " + i, threat.transform);
            RectTransform ert = (RectTransform)eye.transform;
            ert.anchorMin = ert.anchorMax = ert.pivot = new Vector2(0.5f, 1f);
            ert.anchoredPosition = new Vector2(i == 0 ? -45f : 45f, -80f);
            ert.sizeDelta = new Vector2(34f, 22f);
            eye.AddComponent<Image>().color = new Color(1f, 0.1f, 0.1f, 1f);
        }
        threat.SetActive(false);

        // The way back to the keypad
        Button back = MakeSideArrow(view.transform, "Back", isLeft ? "BACK  >" : "<  BACK", !isLeft);
        UnityEventTools.AddVoidPersistentListener(back.onClick, isLeft ? (UnityAction)codeUI.LookRight : codeUI.LookLeft);
        MakeWarning(view.transform);

        view.SetActive(false);
        return view;
    }

    // The hint button (top right, under the Case Book button) + the HintSystem script on GameSystems
    private static void BuildHintButton(Transform ui, GameObject systems)
    {
        Button b = MakeButton(ui, "HintButton", "Hint [H] (2 left)", 30f);
        RectTransform rt = (RectTransform)b.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-20f, -110f);
        rt.sizeDelta = new Vector2(380f, 70f);
        b.GetComponent<Image>().color = new Color(0.25f, 0.12f, 0.12f, 0.95f);

        HintSystem hints = systems.AddComponent<HintSystem>();
        SerializedObject so = new SerializedObject(hints);
        so.FindProperty("buttonLabel").objectReferenceValue = b.GetComponentInChildren<TMP_Text>();
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddVoidPersistentListener(b.onClick, hints.AskForHint);
    }

    // ----- small UI helpers -----

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static TextMeshProUGUI MakeText(Transform parent, string name, string text, float size, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 sizeDelta)
    {
        GameObject go = BlockoutSceneBuilder.UIObject(name, parent);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }

    private static Button MakeButton(Transform parent, string name, string label, float fontSize)
    {
        GameObject go = BlockoutSceneBuilder.UIObject(name, parent);
        Image img = go.AddComponent<Image>();
        img.color = new Color(0.22f, 0.27f, 0.24f, 1f);
        Button b = go.AddComponent<Button>();
        b.targetGraphic = img;

        GameObject lbl = BlockoutSceneBuilder.UIObject("Label", go.transform);
        Stretch((RectTransform)lbl.transform);
        TextMeshProUGUI t = lbl.AddComponent<TextMeshProUGUI>();
        t.text = label;
        t.fontSize = fontSize;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.raycastTarget = false;
        return b;
    }
}
