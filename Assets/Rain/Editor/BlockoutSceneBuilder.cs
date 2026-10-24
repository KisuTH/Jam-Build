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
    private const string Folder = "Assets/BlockoutTest";

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
            Light dirLight = Object.FindFirstObjectByType<Light>();
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
        GameObject door = MakeBlock("Door", root, new Vector2(7f, -0.2f), new Vector2(2f, 4.6f), new Color(0.25f, 0.35f, 0.6f), sprite, 0, true, true);
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

        if (Object.FindFirstObjectByType<Inventory>() == null)
            systems.AddComponent<Inventory>();

        ClickInteractor2D clicker = Object.FindFirstObjectByType<ClickInteractor2D>();
        if (clicker == null) clicker = systems.AddComponent<ClickInteractor2D>();

        SerializedObject clickerSO = new SerializedObject(clicker);
        SerializedProperty camProp = clickerSO.FindProperty("cam");
        SerializedProperty playerProp = clickerSO.FindProperty("player");
        if (camProp.objectReferenceValue == null) camProp.objectReferenceValue = cam;
        if (playerProp.objectReferenceValue == null) playerProp.objectReferenceValue = player.transform;
        clickerSO.ApplyModifiedPropertiesWithoutUndo();

        // ---------- UI ----------
        if (Object.FindFirstObjectByType<DialogueUI>() == null)
            BuildUI(root);
        else
            Debug.Log("Found an existing DialogueUI in the scene, so the test UI was not created.");

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

    private static Camera SetupCamera(bool configure)
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

    private static void BuildUI(Transform root)
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
        if (Object.FindFirstObjectByType<EventSystem>() == null)
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

        // Inventory bar
        GameObject bar = UIObject("InventoryBar", canvasGO.transform);
        RectTransform brt = (RectTransform)bar.transform;
        brt.anchorMin = new Vector2(0f, 1f);
        brt.anchorMax = new Vector2(1f, 1f);
        brt.pivot = new Vector2(0.5f, 1f);
        brt.sizeDelta = new Vector2(0f, 120f);
        brt.anchoredPosition = Vector2.zero;
        bar.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);

        HorizontalLayoutGroup hlg = bar.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(15, 15, 15, 15);
        hlg.spacing = 15f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        GameObject slotPrefab = CreateSlotPrefab();

        InventoryUI invUI = bar.AddComponent<InventoryUI>();
        SerializedObject invSO = new SerializedObject(invUI);
        invSO.FindProperty("slotContainer").objectReferenceValue = bar.transform;
        invSO.FindProperty("slotPrefab").objectReferenceValue = slotPrefab;
        invSO.ApplyModifiedPropertiesWithoutUndo();

        // Hint text
        GameObject hintGO = UIObject("HintText", canvasGO.transform);
        RectTransform hrt = (RectTransform)hintGO.transform;
        hrt.anchorMin = new Vector2(0f, 1f);
        hrt.anchorMax = new Vector2(0f, 1f);
        hrt.pivot = new Vector2(0f, 1f);
        hrt.anchoredPosition = new Vector2(20f, -135f);
        hrt.sizeDelta = new Vector2(1400f, 50f);
        TextMeshProUGUI hint = hintGO.AddComponent<TextMeshProUGUI>();
        hint.fontSize = 28f;
        hint.color = new Color(0.8f, 0.8f, 0.8f, 0.9f);
        hint.raycastTarget = false;
        hint.text = "A/D = walk  |  W = jump  |  S = crouch  |  Click objects to inspect  |  Find the key, open the door";
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

    private static GameObject UIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject MakeBlock(string name, Transform parent, Vector2 localPos, Vector2 scale, Color color, Sprite sprite, int order, bool withCollider, bool isTrigger = false)
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
    private static Sprite CreateWhiteSquareSprite()
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
