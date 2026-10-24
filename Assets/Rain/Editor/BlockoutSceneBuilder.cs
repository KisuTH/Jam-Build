// Put this file in a folder named exactly "Editor" (Assets/Editor/BlockoutSceneBuilder.cs).
//
// Menu:
//   Tools > Point and Click > Create Blockout Test Scene (New Scene)
//   Tools > Point and Click > Add Blockout Test To Current Scene
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BlockoutSceneBuilder
{
    internal const string Folder = "Assets/BlockoutTest";

    [MenuItem("Tools/Point and Click/Create Blockout Test Scene (New Scene)")]
    public static void BuildNewScene()
    {
        Build(true);
    }

    [MenuItem("Tools/Point and Click/Add Blockout Test To Current Scene")]
    public static void BuildInCurrentScene()
    {
        Build(false);
    }

    // =====================================================================

    // Wrapper: if anything goes wrong while building, say so clearly instead of stopping silently
    private static void Build(bool newScene)
    {
        try
        {
            BuildInner(newScene);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Blockout builder FAILED part-way, so the test scene is incomplete. " +
                           "Delete the 'BlockoutTest' object and fix the error below, then run it again.");
            Debug.LogException(e);
            EditorUtility.DisplayDialog("Blockout Test",
                "The builder failed part-way (the scene is incomplete).\n\nOpen the Console and copy the red error.",
                "OK");
        }
    }

    private static void BuildInner(bool newScene)
    {
        const string title = "Blockout Test";

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(title, "Stop Play mode first.", "OK");
            return;
        }

        if (TMP_Settings.instance == null)
        {
            EditorUtility.DisplayDialog(title,
                "TextMeshPro Essentials are not imported yet.\n\n" +
                "Go to Window > TextMeshPro > Import TMP Essential Resources, then run this again.",
                "OK");
            return;
        }

        if (newScene)
        {
            if (!EditorUtility.DisplayDialog(title,
                    "This creates a NEW scene at " + Folder + "/BlockoutTest.unity.\n" +
                    "(If it already exists it will be replaced.)",
                    "Create", "Cancel"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        }
        else
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(sceneName)) sceneName = "(untitled)";

            if (!EditorUtility.DisplayDialog(title,
                    "This adds the test objects to the open scene:  " + sceneName + "\n\n" +
                    "Everything is put under one object called \"BlockoutTest\", so you can delete it later.\n" +
                    "The scene is NOT saved automatically.",
                    "Add", "Cancel"))
                return;
        }

        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "BlockoutTest");

        Sprite sprite = CreateWhiteSquareSprite();

        Scene scene;
        if (newScene) scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        else scene = SceneManager.GetActiveScene();

        // ---------- Camera (2D) ----------
        Camera cam = SetupCamera(newScene);

        if (newScene)
        {
            Light dirLight = Object.FindAnyObjectByType<Light>();
            if (dirLight != null) Object.DestroyImmediate(dirLight.gameObject);
        }

        // Put the test in front of the camera (matters when adding to an existing scene)
        Vector2 offset = newScene ? Vector2.zero : (Vector2)cam.transform.position;

        GameObject rootGO = new GameObject("BlockoutTest");
        rootGO.transform.position = offset;
        Transform root = rootGO.transform;

        // ---------- Environment ----------
        MakeBlock("Wall", root, new Vector2(0f, 1.25f), new Vector2(30f, 7.5f), new Color(0.16f, 0.16f, 0.2f), sprite, -3, false);
        // Floor top is at y = -2.5. It has a solid collider so the player can stand on it.
        MakeBlock("Floor", root, new Vector2(0f, -4f), new Vector2(30f, 3f), new Color(0.1f, 0.1f, 0.12f), sprite, -2, true);
        // Solid walls at both ends so the player cannot walk off the floor
        MakeBlock("Wall Left", root, new Vector2(-15.25f, 0f), new Vector2(0.5f, 10f), new Color(0.08f, 0.08f, 0.1f), sprite, -1, true);
        MakeBlock("Wall Right", root, new Vector2(15.25f, 0f), new Vector2(0.5f, 10f), new Color(0.08f, 0.08f, 0.1f), sprite, -1, true);

        // ---------- Player ----------
        // If the scene already has an object tagged "Player", use that one.
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            player = MakeBlock("Player", root, new Vector2(-8f, -1.7f), new Vector2(0.8f, 1.6f), new Color(0.3f, 0.8f, 0.4f), sprite, 5, true);
            player.tag = "Player";
            Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            player.AddComponent<PlayerMovement2D>();
        }
        else
        {
            Debug.Log("Found an existing object tagged 'Player' (" + player.name + "), so a new Player was not created.");
        }

        // ---------- Interactable objects ----------
        // Table: inspect
        GameObject table = MakeBlock("Table", root, new Vector2(-4.5f, -1.9f), new Vector2(2f, 1.2f), new Color(0.45f, 0.3f, 0.15f), sprite, 0, true, true);
        Interactable2D tableI = table.AddComponent<Interactable2D>();
        tableI.displayName = "Table";
        tableI.description = "A dusty wooden table. Something was dragged across it a long time ago.";
        UnityEventTools.AddVoidPersistentListener(tableI.onInteract, tableI.SayDescription);

        // Picture on the wall: inspect
        GameObject picture = MakeBlock("Picture", root, new Vector2(-1f, 1f), new Vector2(1.8f, 1.2f), new Color(0.6f, 0.55f, 0.4f), sprite, 0, true, true);
        Interactable2D pictureI = picture.AddComponent<Interactable2D>();
        pictureI.displayName = "Picture";
        pictureI.maxDistance = 6f;
        pictureI.description = "A faded family portrait. Every face has been scratched out.";
        pictureI.clueId = "family_portrait";
        pictureI.clueTitle = "Family portrait";
        pictureI.clueText = "A faded family portrait in the room. Every face has been scratched out. Who did this?";
        UnityEventTools.AddVoidPersistentListener(pictureI.onInteract, pictureI.SayDescription);

        // Old key: pickup
        GameObject key = MakeBlock("Old Key", root, new Vector2(0.5f, -2.25f), new Vector2(0.5f, 0.5f), new Color(0.95f, 0.8f, 0.2f), sprite, 0, true, true);
        Interactable2D keyI = key.AddComponent<Interactable2D>();
        keyI.displayName = "Old Key";
        ItemPickup keyP = key.AddComponent<ItemPickup>();
        keyP.itemId = "old_key";
        keyP.itemName = "Old Key";
        keyP.itemDescription = "A rusty old key. It looks like it fits a heavy door.";
        keyP.pickupMessage = "Picked up: Old Key";
        keyP.icon = sprite;
        UnityEventTools.AddVoidPersistentListener(keyI.onInteract, keyP.Pickup);

        // Old photo: pickup (clue)
        GameObject photo = MakeBlock("Old Photo", root, new Vector2(3f, -2.3f), new Vector2(0.6f, 0.4f), new Color(0.9f, 0.5f, 0.7f), sprite, 0, true, true);
        Interactable2D photoI = photo.AddComponent<Interactable2D>();
        photoI.displayName = "Old Photo";
        ItemPickup photoP = photo.AddComponent<ItemPickup>();
        photoP.itemId = "old_photo";
        photoP.itemName = "Old Photo";
        photoP.itemDescription = "A photo of five people. On the back: 'Don't open the door at night.'";
        photoP.pickupMessage = "Picked up: Old Photo";
        photoP.icon = sprite;
        UnityEventTools.AddVoidPersistentListener(photoI.onInteract, photoP.Pickup);

        // Door: needs the key
        GameObject door = MakeBlock("Door", root, new Vector2(7f, -0.2f), new Vector2(2f, 4.6f), new Color(0.25f, 0.35f, 0.6f), sprite, 0, true, false);
        // The door is SOLID (not a trigger) so it blocks the player until it is opened.
        // Opening it hides the whole object, which removes the collider too.
        Interactable2D doorI = door.AddComponent<Interactable2D>();
        doorI.displayName = "Door";
        doorI.maxDistance = 3f;
        doorI.requiredItemId = "old_key";
        doorI.consumeRequiredItem = true;
        doorI.missingItemMessage = "The door is locked. I need a key.";
        UnityEventTools.AddStringPersistentListener(doorI.onInteract, doorI.Say, "The key turns. The door swings open...");
        UnityEventTools.AddVoidPersistentListener(doorI.onInteract, doorI.Hide);

        // ---------- Game systems (skip what the scene already has) ----------
        GameObject systems = new GameObject("GameSystems");
        systems.transform.SetParent(root, false);

        if (Object.FindAnyObjectByType<Inventory>() == null)
            systems.AddComponent<Inventory>();
        if (Object.FindAnyObjectByType<StoryFlags>() == null)
            systems.AddComponent<StoryFlags>();
        if (Object.FindAnyObjectByType<CaseNotebook>() == null)
            systems.AddComponent<CaseNotebook>();

        ClickInteractor2D clicker = Object.FindAnyObjectByType<ClickInteractor2D>();
        if (clicker == null) clicker = systems.AddComponent<ClickInteractor2D>();

        SerializedObject clickerSO = new SerializedObject(clicker);
        SerializedProperty camProp = clickerSO.FindProperty("cam");
        SerializedProperty playerProp = clickerSO.FindProperty("player");
        if (camProp.objectReferenceValue == null) camProp.objectReferenceValue = cam;
        if (playerProp.objectReferenceValue == null) playerProp.objectReferenceValue = player.transform;
        clickerSO.ApplyModifiedPropertiesWithoutUndo();

        // ---------- UI ----------
        if (Object.FindAnyObjectByType<DialogueUI>() == null)
            BuildUI(root);
        else
            Debug.Log("Found an existing DialogueUI in the scene, so the test UI was not created.");

        if (Object.FindAnyObjectByType<CaseNotebookUI>() == null)
            BuildNotebookUI();

        // ---------- Finish ----------
        if (newScene)
        {
            EditorSceneManager.SaveScene(scene, Folder + "/BlockoutTest.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("Blockout test scene created at " + Folder + "/BlockoutTest.unity. Press Play!");
        }
        else
        {
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Blockout test added to the current scene (under 'BlockoutTest'). Save the scene when you are happy, then press Play!");
        }

        Selection.activeGameObject = rootGO;
    }

    // =====================================================================

    internal static Camera SetupCamera(bool configure)
    {
        Camera cam = Camera.main;

        if (cam == null)
        {
            GameObject camGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGO.tag = "MainCamera";
            cam = camGO.GetComponent<Camera>();
            configure = true;
        }

        if (configure)
        {
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.04f, 0.07f);
            cam.transform.position = new Vector3(0f, 0f, -10f);
        }
        else if (!cam.orthographic)
        {
            Debug.LogWarning("Your Main Camera is not Orthographic. The test layout and click detection are meant for a 2D (Orthographic) camera.");
        }

        return cam;
    }

    internal static void BuildUI(Transform root, string hintText = null)
    {
        // Canvas
        GameObject canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(root, false);
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // EventSystem (New Input System), only if the scene has none
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem", typeof(EventSystem));
            es.transform.SetParent(root, false);
            InputSystemUIInputModule module = es.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        // Dialogue box
        GameObject panel = UIObject("DialoguePanel", canvasGO.transform);
        RectTransform prt = (RectTransform)panel.transform;
        prt.anchorMin = new Vector2(0.1f, 0.03f);
        prt.anchorMax = new Vector2(0.9f, 0.25f);
        prt.offsetMin = Vector2.zero;
        prt.offsetMax = Vector2.zero;
        panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

        GameObject textGO = UIObject("DialogueText", panel.transform);
        RectTransform trt = (RectTransform)textGO.transform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(30f, 20f);
        trt.offsetMax = new Vector2(-30f, -20f);
        TextMeshProUGUI dialogueText = textGO.AddComponent<TextMeshProUGUI>();
        dialogueText.fontSize = 40f;
        dialogueText.color = Color.white;
        dialogueText.alignment = TextAlignmentOptions.TopLeft;
        dialogueText.raycastTarget = false;
        dialogueText.text = "";

        DialogueUI dialogue = canvasGO.AddComponent<DialogueUI>();
        SerializedObject dialogueSO = new SerializedObject(dialogue);
        dialogueSO.FindProperty("panel").objectReferenceValue = panel;
        dialogueSO.FindProperty("dialogueText").objectReferenceValue = dialogueText;
        dialogueSO.ApplyModifiedPropertiesWithoutUndo();

        // Inventory: a bag button (top left). Clicking it opens / closes the item grid.
        GameObject invRoot = UIObject("Inventory UI", canvasGO.transform);   // always active, holds the script
        GameObject bagGO = UIObject("BagButton", invRoot.transform);
        RectTransform bagRT = (RectTransform)bagGO.transform;
        bagRT.anchorMin = new Vector2(0f, 1f);
        bagRT.anchorMax = new Vector2(0f, 1f);
        bagRT.pivot = new Vector2(0f, 1f);
        bagRT.anchoredPosition = new Vector2(20f, -20f);
        bagRT.sizeDelta = new Vector2(260f, 80f);
        Image bagImg = bagGO.AddComponent<Image>();   // tip: put your bag picture in this Image's Source Image
        bagImg.color = new Color(0.25f, 0.2f, 0.12f, 0.95f);
        Button bagBtn = bagGO.AddComponent<Button>();
        bagBtn.targetGraphic = bagImg;

        GameObject bagLblGO = UIObject("Label", bagGO.transform);
        RectTransform bagLblRT = (RectTransform)bagLblGO.transform;
        bagLblRT.anchorMin = Vector2.zero;
        bagLblRT.anchorMax = Vector2.one;
        bagLblRT.offsetMin = Vector2.zero;
        bagLblRT.offsetMax = Vector2.zero;
        TextMeshProUGUI bagLabel = bagLblGO.AddComponent<TextMeshProUGUI>();
        bagLabel.fontSize = 32f;
        bagLabel.alignment = TextAlignmentOptions.Center;
        bagLabel.color = Color.white;
        bagLabel.raycastTarget = false;
        bagLabel.text = "Bag [I] (0)";

        // The item grid (starts closed)
        GameObject bar = UIObject("InventoryPanel", invRoot.transform);
        RectTransform brt = (RectTransform)bar.transform;
        brt.anchorMin = new Vector2(0f, 1f);
        brt.anchorMax = new Vector2(0f, 1f);
        brt.pivot = new Vector2(0f, 1f);
        brt.anchoredPosition = new Vector2(20f, -110f);
        brt.sizeDelta = new Vector2(460f, 330f);
        bar.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

        GridLayoutGroup grid = bar.AddComponent<GridLayoutGroup>();
        grid.padding = new RectOffset(15, 15, 15, 15);
        grid.cellSize = new Vector2(90f, 90f);
        grid.spacing = new Vector2(15f, 15f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;

        GameObject slotPrefab = CreateSlotPrefab();

        InventoryUI invUI = invRoot.AddComponent<InventoryUI>();
        SerializedObject invSO = new SerializedObject(invUI);
        invSO.FindProperty("slotContainer").objectReferenceValue = bar.transform;
        invSO.FindProperty("slotPrefab").objectReferenceValue = slotPrefab;
        invSO.FindProperty("panel").objectReferenceValue = bar;
        invSO.FindProperty("bagButton").objectReferenceValue = bagBtn;
        invSO.FindProperty("bagLabel").objectReferenceValue = bagLabel;
        invSO.ApplyModifiedPropertiesWithoutUndo();

        // Hint text
        GameObject hintGO = UIObject("HintText", canvasGO.transform);
        RectTransform hrt = (RectTransform)hintGO.transform;
        hrt.anchorMin = new Vector2(0f, 1f);
        hrt.anchorMax = new Vector2(0f, 1f);
        hrt.pivot = new Vector2(0f, 1f);
        hrt.anchoredPosition = new Vector2(300f, -35f);
        hrt.sizeDelta = new Vector2(1400f, 50f);
        TextMeshProUGUI hint = hintGO.AddComponent<TextMeshProUGUI>();
        hint.fontSize = 28f;
        hint.color = new Color(0.8f, 0.8f, 0.8f, 0.9f);
        hint.raycastTarget = false;
        hint.text = string.IsNullOrEmpty(hintText) ? "A/D = walk  |  W = jump  |  S = crouch  |  Tab = Case Book (arrows turn pages)  |  Click objects to inspect  |  Find the key, open the door" : hintText;
    }

    // Case Book: a button (top right) and a panel with the clue pages. Added to the scene's Canvas.
    internal static void BuildNotebookUI()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("No Canvas in the scene, so the Case Book UI was not created.");
            return;
        }
        Transform parent = canvas.transform;

        // Always-active object that holds the CaseNotebookUI script
        GameObject uiGO = UIObject("CaseBookUI", parent);

        // Open / close button
        GameObject btnGO = UIObject("CaseBookButton", parent);
        RectTransform brt = (RectTransform)btnGO.transform;
        brt.anchorMin = new Vector2(1f, 1f);
        brt.anchorMax = new Vector2(1f, 1f);
        brt.pivot = new Vector2(1f, 1f);
        brt.anchoredPosition = new Vector2(-20f, -20f);
        brt.sizeDelta = new Vector2(380f, 80f);
        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.25f, 0.2f, 0.12f, 0.95f);
        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnImg;

        GameObject lblGO = UIObject("Label", btnGO.transform);
        RectTransform lrt = (RectTransform)lblGO.transform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        TextMeshProUGUI label = lblGO.AddComponent<TextMeshProUGUI>();
        label.fontSize = 32f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        label.text = "Case Book [Tab] (0)";

        // The book page (starts hidden)
        GameObject panel = UIObject("CaseBookPanel", parent);
        RectTransform prt = (RectTransform)panel.transform;
        prt.anchorMin = new Vector2(0.2f, 0.15f);
        prt.anchorMax = new Vector2(0.8f, 0.85f);
        prt.offsetMin = Vector2.zero;
        prt.offsetMax = Vector2.zero;
        panel.AddComponent<Image>().color = new Color(0.14f, 0.11f, 0.08f, 0.96f);

        GameObject titleGO = UIObject("Title", panel.transform);
        RectTransform trt = (RectTransform)titleGO.transform;
        trt.anchorMin = new Vector2(0f, 1f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -20f);
        trt.sizeDelta = new Vector2(0f, 70f);
        TextMeshProUGUI title = titleGO.AddComponent<TextMeshProUGUI>();
        title.fontSize = 48f;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(0.9f, 0.8f, 0.55f);
        title.raycastTarget = false;
        title.text = "CASE BOOK";

        // Clue title (big, one clue per page)
        GameObject clueTitleGO = UIObject("ClueTitle", panel.transform);
        RectTransform ctr = (RectTransform)clueTitleGO.transform;
        ctr.anchorMin = new Vector2(0f, 1f);
        ctr.anchorMax = new Vector2(1f, 1f);
        ctr.pivot = new Vector2(0.5f, 1f);
        ctr.anchoredPosition = new Vector2(0f, -110f);
        ctr.sizeDelta = new Vector2(-80f, 60f);
        TextMeshProUGUI clueTitle = clueTitleGO.AddComponent<TextMeshProUGUI>();
        clueTitle.fontSize = 40f;
        clueTitle.fontStyle = FontStyles.Bold;
        clueTitle.alignment = TextAlignmentOptions.Left;
        clueTitle.color = Color.white;
        clueTitle.raycastTarget = false;
        clueTitle.text = "";

        // Clue text
        GameObject entriesGO = UIObject("Entries", panel.transform);
        RectTransform ert = (RectTransform)entriesGO.transform;
        ert.anchorMin = Vector2.zero;
        ert.anchorMax = Vector2.one;
        ert.offsetMin = new Vector2(40f, 120f);
        ert.offsetMax = new Vector2(-40f, -180f);
        TextMeshProUGUI entries = entriesGO.AddComponent<TextMeshProUGUI>();
        entries.fontSize = 32f;
        entries.alignment = TextAlignmentOptions.TopLeft;
        entries.color = Color.white;
        entries.overflowMode = TextOverflowModes.Truncate;
        entries.raycastTarget = false;
        entries.text = "";

        // Page number, bottom centre
        GameObject pageGO = UIObject("PageNumber", panel.transform);
        RectTransform pgr = (RectTransform)pageGO.transform;
        pgr.anchorMin = new Vector2(0.5f, 0f);
        pgr.anchorMax = new Vector2(0.5f, 0f);
        pgr.pivot = new Vector2(0.5f, 0f);
        pgr.anchoredPosition = new Vector2(0f, 40f);
        pgr.sizeDelta = new Vector2(240f, 50f);
        TextMeshProUGUI pageNumber = pageGO.AddComponent<TextMeshProUGUI>();
        pageNumber.fontSize = 34f;
        pageNumber.alignment = TextAlignmentOptions.Center;
        pageNumber.color = new Color(0.9f, 0.8f, 0.55f);
        pageNumber.raycastTarget = false;
        pageNumber.text = "0 / 0";

        // Prev / Next buttons, bottom left and right (like turning a page)
        Button prev = MakeUIButton(panel.transform, "PrevButton", "<  Prev",
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 25f), new Vector2(190f, 70f));
        Button next = MakeUIButton(panel.transform, "NextButton", "Next  >",
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 25f), new Vector2(190f, 70f));

        CaseNotebookUI ui = uiGO.AddComponent<CaseNotebookUI>();
        SerializedObject so = new SerializedObject(ui);
        so.FindProperty("panel").objectReferenceValue = panel;
        so.FindProperty("titleText").objectReferenceValue = clueTitle;
        so.FindProperty("entriesText").objectReferenceValue = entries;
        so.FindProperty("pageText").objectReferenceValue = pageNumber;
        so.FindProperty("prevButton").objectReferenceValue = prev;
        so.FindProperty("nextButton").objectReferenceValue = next;
        so.FindProperty("openButton").objectReferenceValue = btn;
        so.FindProperty("buttonLabel").objectReferenceValue = label;
        so.ApplyModifiedPropertiesWithoutUndo();

        panel.SetActive(false);
    }

    // A simple text button (dark brown, white text)
    private static Button MakeUIButton(Transform parent, string name, string text,
        Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        GameObject go = UIObject(name, parent);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.3f, 0.23f, 0.14f, 1f);
        Button b = go.AddComponent<Button>();
        b.targetGraphic = img;

        GameObject lblGO = UIObject("Label", go.transform);
        RectTransform lrt = (RectTransform)lblGO.transform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        TextMeshProUGUI lbl = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.fontSize = 34f;
        lbl.alignment = TextAlignmentOptions.Center;
        lbl.color = Color.white;
        lbl.raycastTarget = false;
        lbl.text = text;
        return b;
    }

    private static GameObject CreateSlotPrefab()
    {
        GameObject slot = new GameObject("InventorySlot", typeof(RectTransform));
        ((RectTransform)slot.transform).sizeDelta = new Vector2(90f, 90f);
        Image img = slot.AddComponent<Image>();
        img.color = Color.white;
        Button btn = slot.AddComponent<Button>();
        btn.targetGraphic = img;

        string path = Folder + "/InventorySlot.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(slot, path);
        Object.DestroyImmediate(slot);
        return prefab;
    }

    internal static GameObject UIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    internal static GameObject MakeBlock(string name, Transform parent, Vector2 localPos, Vector2 scale, Color color, Sprite sprite, int order, bool withCollider, bool isTrigger = false)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;

        if (withCollider)
        {
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = sprite.bounds.size;
            // Interactable objects are triggers: the player walks past them, but clicks still hit them
            col.isTrigger = isTrigger;
        }
        return go;
    }

    // Makes a 1x1 unit white square sprite asset so colored blocks can be drawn
    internal static Sprite CreateWhiteSquareSprite()
    {
        string assetPath = Folder + "/WhiteSquare.png";
        string diskPath = Application.dataPath + "/BlockoutTest/WhiteSquare.png";

        if (!File.Exists(diskPath))
        {
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(diskPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 4f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }
}
