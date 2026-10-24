// Put this file in Assets/Rain/Editor/ (next to BlockoutSceneBuilder.cs).
//
// Menu:  Tools > Point and Click > Add Long Test Level To Current Scene
//
// Builds a 72-unit-wide blockout level with 3 rooms and tries out the story features:
//   Room 1 "Waiting Hall":  notice board, desk, Patient Register (item), Old Key (item), locked door A
//   Room 2 "Ward":          phone that rings, falling pot, portrait that changes, Torn Photo (item),
//                           door B (needs the Register AND the Torn Photo = "putting clues together")
//   Room 3 "Records Room":  lights flicker, filing cabinets, mirror, exit door (needs the cabinet clue)
//
// Doors between rooms are PAIRS (one half on each side of the wall). Click a door: the player walks into
// it, the screen fades to black, and the player comes out of the other door in the next room (RoomDoor).
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class LongLevelBuilder
{
    private const float HalfWidth = 36f;   // the level runs from x = -36 to +36

    [MenuItem("Tools/Point and Click/Add Long Test Level To Current Scene")]
    public static void Build()
    {
        try
        {
            BuildInner();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Long level builder FAILED part-way, so the level is incomplete. " +
                           "Delete the 'LongTestLevel' object and fix the error below, then run it again.");
            Debug.LogException(e);
            EditorUtility.DisplayDialog("Long Test Level",
                "The builder failed part-way (the level is incomplete).\n\nOpen the Console and copy the red error.",
                "OK");
        }
    }

    // =====================================================================

    private static void BuildInner()
    {
        const string title = "Long Test Level";

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

        if (GameObject.Find("LongTestLevel") != null)
        {
            EditorUtility.DisplayDialog(title,
                "There is already a 'LongTestLevel' object in this scene.\nDelete it first, then run this again.",
                "OK");
            return;
        }

        string sceneName = SceneManager.GetActiveScene().name;
        if (string.IsNullOrEmpty(sceneName)) sceneName = "(untitled)";

        string warn = GameObject.Find("BlockoutTest") != null
            ? "\n\nWARNING: this scene still has the small 'BlockoutTest'. It uses the same item names " +
              "(old_key). Delete 'BlockoutTest' first for a clean test."
            : "";

        if (!EditorUtility.DisplayDialog(title,
                "This adds the long level to the open scene:  " + sceneName + "\n\n" +
                "Everything is put under one object called \"LongTestLevel\", so you can delete it later.\n" +
                "The existing Player (tag 'Player') is MOVED to the start of the level.\n" +
                "The scene is NOT saved automatically." + warn,
                "Add", "Cancel"))
            return;

        if (!AssetDatabase.IsValidFolder(BlockoutSceneBuilder.Folder))
            AssetDatabase.CreateFolder("Assets", "BlockoutTest");

        Sprite sprite = BlockoutSceneBuilder.CreateWhiteSquareSprite();
        Camera cam = BlockoutSceneBuilder.SetupCamera(false);

        Vector2 offset = (Vector2)cam.transform.position;
        GameObject rootGO = new GameObject("LongTestLevel");
        rootGO.transform.position = offset;
        Transform root = rootGO.transform;

        BuildRooms(root, sprite);
        GameObject player = EnsurePlayer(root, sprite, offset);
        // Camera limits for each room (the doors sit on the boundaries)
        Room room1 = MakeRoom(root, "Room 1: Waiting Hall", -36.5f, -11.4f);
        Room room2 = MakeRoom(root, "Room 2: Ward", -12.6f, 12.6f);
        Room room3 = MakeRoom(root, "Room 3: Records Room", 11.4f, 36.5f);

        BuildRoom1(root, sprite);
        BuildRoom2(root, sprite);
        BuildRoom3(root, sprite);
        BuildDoors(root, sprite, room1, room2, room3);

        GroupIntoRooms(root, room1, room2, room3);

        EnsureSystems(root, cam, player);
        EnsureCamera(cam, player, room1);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = rootGO;
        Debug.Log("Long test level added under 'LongTestLevel'. Save the scene, then press Play! " +
                  "Find the key, click the door at the right end of the room: you walk in, the screen fades, and you come out in the next room.");
    }

    // =====================================================================
    //  Level shell: walls, floor, doors' lintels, room names
    // =====================================================================

    private static void BuildRooms(Transform root, Sprite sprite)
    {
        // Three room backgrounds, each 24 units wide
        Block(root, "Room 1 Wall", -24f, 1.25f, 24f, 7.5f, new Color(0.16f, 0.16f, 0.20f), sprite, -3, false);
        Block(root, "Room 2 Wall", 0f, 1.25f, 24f, 7.5f, new Color(0.19f, 0.14f, 0.14f), sprite, -3, false);
        Block(root, "Room 3 Wall", 24f, 1.25f, 24f, 7.5f, new Color(0.12f, 0.16f, 0.17f), sprite, -3, false);

        // Solid floor (top is at y = -2.5) and solid end walls
        Block(root, "Floor", 0f, -4f, 74f, 3f, new Color(0.10f, 0.10f, 0.12f), sprite, -2, true);
        Block(root, "Wall Left", -HalfWidth - 0.25f, 0f, 0.5f, 10f, new Color(0.08f, 0.08f, 0.10f), sprite, -1, true);
        Block(root, "Wall Right", HalfWidth + 0.25f, 0f, 0.5f, 10f, new Color(0.08f, 0.08f, 0.10f), sprite, -1, true);

        // Solid dividers between the rooms (the player can only change room through a door).
        // The doors themselves are doorways painted on the back wall of each room.
        Color dark = new Color(0.08f, 0.08f, 0.10f);
        Block(root, "Divider A", -12f, 0f, 0.5f, 10f, dark, sprite, -1, true);
        Block(root, "Divider B", 12f, 0f, 0.5f, 10f, dark, sprite, -1, true);

        // Room names (3D text in the world, just to find your way around while testing)
        Label(root, "ROOM 1: WAITING HALL", -24f, 4f);
        Label(root, "ROOM 2: WARD", 0f, 4f);
        Label(root, "ROOM 3: RECORDS ROOM", 24f, 4f);
    }

    private static GameObject EnsurePlayer(Transform root, Sprite sprite, Vector2 offset)
    {
        Vector2 startLocal = new Vector2(-33f, -1.7f);
        GameObject player = GameObject.FindWithTag("Player");

        if (player == null)
        {
            player = BlockoutSceneBuilder.MakeBlock("Player", root, startLocal, new Vector2(0.8f, 1.6f),
                new Color(0.3f, 0.8f, 0.4f), sprite, 5, true);
            player.tag = "Player";
            Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            player.AddComponent<PlayerMovement2D>();
        }
        else
        {
            // Use the existing player and move it to the start of the level
            player.transform.position = new Vector3(offset.x + startLocal.x, offset.y + startLocal.y,
                player.transform.position.z);
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
            Debug.Log("Using the existing Player '" + player.name + "' and moving it to the start of the level.");
        }
        return player;
    }

    // =====================================================================
    //  Room 1: Waiting Hall
    // =====================================================================

    private static void BuildRoom1(Transform root, Sprite sprite)
    {
        Inspect(root, "Bench", -34f, -2.0f, 2.2f, 1.0f, new Color(0.40f, 0.28f, 0.16f), sprite,
            "A wooden bench. Someone carved 'HELP' into the seat.");

        Inspect(root, "Notice Board", -30f, 0.8f, 2.2f, 1.4f, new Color(0.60f, 0.55f, 0.40f), sprite,
            "A notice pinned to the wall: 'Clinic closed until further notice. Do not remove any records.'",
            "notice_board", "Closure notice", 6f);

        Inspect(root, "Reception Desk", -26f, -1.9f, 3f, 1.2f, new Color(0.45f, 0.30f, 0.15f), sprite,
            "A reception desk covered in dust. Something was dragged across it a long time ago.");

        Pickup(root, "Patient Register", -26f, -1.2f, 0.8f, 0.25f, new Color(0.85f, 0.85f, 0.75f), sprite,
            "register", "Patient Register",
            "The last entry is dated three years ago. One name is circled in red: 'Mali'.");

        Pickup(root, "Old Key", -18f, -2.25f, 0.5f, 0.5f, new Color(0.95f, 0.80f, 0.20f), sprite,
            "old_key", "Old Key", "A rusty old key. It looks like it fits a heavy door.");

    }

    // =====================================================================
    //  Room 2: Ward
    // =====================================================================

    private static void BuildRoom2(Transform root, Sprite sprite)
    {
        // Scare 1: a phone rings as soon as you step through door A
        Inspect(root, "Phone", -6.5f, -1.15f, 0.6f, 0.4f, new Color(0.20f, 0.20f, 0.20f), sprite,
            "The phone is dead. The cord was cut years ago.", "dead_phone", "Dead phone", 2.5f);

        ScareEvent ring = Zone(root, "Scare: Phone Rings", -10.5f, 1.5f);
        ring.message = "(Somewhere in the room, a phone starts to ring...)";
        ring.flashScreen = true;
        ring.flashColor = new Color(0f, 0f, 0f, 0.85f);
        ring.flashDuration = 0.5f;
        ring.shakeCamera = true;
        ring.shakeDuration = 0.4f;
        ring.shakeStrength = 0.12f;
        // Tip: drag a ringing sound into the "Sound" field of this object

        Inspect(root, "Hospital Bed", -6.5f, -2.0f, 3f, 1.0f, new Color(0.50f, 0.50f, 0.55f), sprite,
            "A hospital bed. The sheets are crumpled, as if someone just got up.");

        Inspect(root, "Medicine Cabinet", -1f, 0.6f, 2f, 2.2f, new Color(0.70f, 0.75f, 0.70f), sprite,
            "The medicine bottles are empty. Every label has been scratched off.",
            "cabinet", "Medicine cabinet", 4f);

        // Scare 2: a pot falls off the shelf when you walk past
        Block(root, "Shelf", 3.5f, 0.3f, 3f, 0.3f, new Color(0.40f, 0.28f, 0.16f), sprite, 0, true);
        GameObject pot = Block(root, "Pot", 3.8f, 0.75f, 0.5f, 0.6f, new Color(0.55f, 0.35f, 0.25f), sprite, 1, true);
        Rigidbody2D potBody = pot.AddComponent<Rigidbody2D>();
        potBody.bodyType = RigidbodyType2D.Kinematic;   // stays on the shelf until the scare
        potBody.gravityScale = 2f;

        ScareEvent crash = Zone(root, "Scare: Pot Falls", 2.2f, 1f);
        crash.message = "Something shatters on the floor nearby!";
        crash.shakeCamera = true;
        crash.shakeDuration = 0.35f;
        crash.shakeStrength = 0.2f;
        crash.dropBodies = new Rigidbody2D[] { potBody };

        // Scare 3: after you pick up the torn photo, the portrait changes by itself
        Interactable2D portraitA = Inspect(root, "Portrait", 6f, 0.9f, 1.8f, 1.2f, new Color(0.60f, 0.55f, 0.40f), sprite,
            "A faded family portrait. Every face has been scratched out.",
            "family_portrait", "Family portrait", 5f);

        Interactable2D portraitB = Inspect(root, "Portrait (changed)", 6f, 0.9f, 1.8f, 1.2f, new Color(0.55f, 0.20f, 0.20f), sprite,
            "The faces are not scratched out anymore. Now they all stare at you.",
            "portrait_changed", "The portrait changed", 5f);
        portraitB.gameObject.SetActive(false);

        GameObject changer = new GameObject("Scare: Portrait Changes");
        changer.transform.SetParent(root, false);
        ScareEvent change = changer.AddComponent<ScareEvent>();
        change.triggerMode = ScareEvent.TriggerMode.WhenFlagIsSet;
        change.flagToWaitFor = "got_torn_photo";
        change.delay = 0.8f;
        change.message = "...Something about the portrait on the wall looks different now.";
        change.flashScreen = true;
        change.flashColor = new Color(0f, 0f, 0f, 1f);
        change.flashDuration = 0.4f;
        change.shakeCamera = true;
        change.shakeDuration = 0.3f;
        change.shakeStrength = 0.1f;
        change.turnOff = new GameObject[] { portraitA.gameObject };
        change.turnOn = new GameObject[] { portraitB.gameObject };

        Pickup(root, "Torn Photo", 8f, -2.3f, 0.6f, 0.4f, new Color(0.90f, 0.50f, 0.70f), sprite,
            "torn_photo", "Torn Photo", "A photo of a girl. Someone tore away the other half.");

    }

    // =====================================================================
    //  Room 3: Records Room
    // =====================================================================

    private static void BuildRoom3(Transform root, Sprite sprite)
    {
        ScareEvent flicker = Zone(root, "Scare: Lights Flicker", 16.5f, 1.5f);
        flicker.message = "The lights flicker. Then everything goes silent.";
        flicker.flashScreen = true;
        flicker.flashColor = new Color(0f, 0f, 0f, 0.95f);
        flicker.flashDuration = 1.2f;

        Inspect(root, "Filing Cabinets", 19f, -1.5f, 2.5f, 2f, new Color(0.45f, 0.48f, 0.50f), sprite,
            "Rows of filing cabinets. Every drawer is open and empty.");

        Inspect(root, "Cabinet 'M'", 24f, -1.5f, 2.5f, 2f, new Color(0.50f, 0.52f, 0.54f), sprite,
            "A cabinet labelled 'M'. The folder for 'Mali' has been cut out with scissors.",
            "filing_cabinet", "Missing folder", 3.5f);

        Inspect(root, "Mirror", 29f, 0.8f, 1.5f, 2.2f, new Color(0.50f, 0.60f, 0.70f), sprite,
            "The mirror shows the room behind you. But you are not in it.",
            "mirror", "The mirror", 6f);

        // Exit door: needs the filing cabinet clue, then ends the chapter
        Interactable2D exit = Door(root, "Exit Door", 34f, sprite, new Color(0.35f, 0.20f, 0.20f));
        exit.requiredFlags = new string[] { "filing_cabinet" };
        exit.missingFlagsMessage = "I should look around this room before I leave.";
        ChapterEnd end = exit.gameObject.AddComponent<ChapterEnd>();
        end.endMessage = "To be continued...";
        UnityEventTools.AddVoidPersistentListener(exit.onInteract, end.Finish);
    }

    // =====================================================================
    //  Doors between rooms (two halves per wall: one for each side)
    // =====================================================================

    private static void BuildDoors(Transform root, Sprite sprite, Room room1, Room room2, Room room3)
    {
        Color colorA = new Color(0.30f, 0.38f, 0.60f);
        Color colorB = new Color(0.34f, 0.32f, 0.58f);

        // Door A (room 1 <-> room 2). Going forward needs the old key (the key is kept, so coming back works).
        Interactable2D a1 = Door(root, "Door A (Room 1 side)", -15f, sprite, colorA);
        a1.requiredItemId = "old_key";
        a1.consumeRequiredItem = false;
        a1.missingItemMessage = "The door is locked. I need a key.";
        LinkDoor(root, a1, new Vector2(-10f, -1.7f), room2, 1f, "The key turns. The door creaks open...");
        Label(root, "TO THE WARD", -15f, 1.5f);

        Interactable2D a2 = Door(root, "Door A (Room 2 side)", -10f, sprite, colorA);
        LinkDoor(root, a2, new Vector2(-15f, -1.7f), room1, -1f, "");
        Label(root, "BACK", -10f, 1.5f);

        // Door B (room 2 <-> room 3). Going forward needs the register AND the torn photo.
        Interactable2D b1 = Door(root, "Door B (Room 2 side)", 10f, sprite, colorB);
        b1.requiredFlags = new string[] { "got_register", "got_torn_photo" };
        b1.missingFlagsMessage = "The door will not open. I feel like I'm missing something I should have found...";
        b1.clueId = "deduction_1";
        b1.clueTitle = "Connection";
        b1.clueText = "The name circled in the register is 'Mali'. The girl in the torn photo must be her.";
        LinkDoor(root, b1, new Vector2(15f, -1.7f), room3, 1f, "The two clues fit together. The lock gives way...");
        Label(root, "TO THE RECORDS ROOM", 10f, 1.5f);

        Interactable2D b2 = Door(root, "Door B (Room 3 side)", 15f, sprite, colorB);
        LinkDoor(root, b2, new Vector2(10f, -1.7f), room2, -1f, "");
        Label(root, "BACK", 15f, 1.5f);
    }

    // Puts everything of each room under one "Content" object, so only the room the player is in is shown.
    // Always visible: the floor, the outer walls, the dividers, the Player, room markers, arrive points.
    private static void GroupIntoRooms(Transform root, Room room1, Room room2, Room room3)
    {
        Transform[] holders = new Transform[3];
        string[] names = { "Content: Room 1", "Content: Room 2", "Content: Room 3" };
        Room[] rooms = { room1, room2, room3 };
        for (int i = 0; i < 3; i++)
        {
            GameObject h = new GameObject(names[i]);
            h.transform.SetParent(root, false);
            holders[i] = h.transform;
            rooms[i].content = h;
        }

        // Copy the list first (we change the parents while looping)
        System.Collections.Generic.List<Transform> kids = new System.Collections.Generic.List<Transform>();
        foreach (Transform t in root) kids.Add(t);

        foreach (Transform t in kids)
        {
            string n = t.name;
            if (t.GetComponent<Room>() != null) continue;
            if (n.StartsWith("Content:") || n.StartsWith("Arrive:") || n.StartsWith("Divider")) continue;
            if (n == "GameSystems" || n == "Floor" || n == "Wall Left" || n == "Wall Right") continue;
            if (t.CompareTag("Player")) continue;

            float localX = t.position.x - root.position.x;
            int index = localX < -12f ? 0 : (localX < 12f ? 1 : 2);
            t.SetParent(holders[index], true);
        }
    }

    // Makes a Room marker. minX / maxX are local to the level root; the height matches the camera view.
    private static Room MakeRoom(Transform root, string name, float minX, float maxX)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(root, false);

        Room room = go.AddComponent<Room>();
        room.roomName = name;
        Vector3 origin = root.position;
        room.boundsMin = new Vector2(origin.x + minX, origin.y - 5f);
        room.boundsMax = new Vector2(origin.x + maxX, origin.y + 5f);
        return room;
    }

    // Turns a door into a room door: where the player appears, which room it is, and the click action
    private static void LinkDoor(Transform root, Interactable2D door, Vector2 arriveLocal, Room arriveRoom,
        float stepOutDirection, string firstTimeMessage)
    {
        GameObject arrive = new GameObject("Arrive: " + door.gameObject.name);
        arrive.transform.SetParent(root, false);
        arrive.transform.localPosition = new Vector3(arriveLocal.x, arriveLocal.y, 0f);

        RoomDoor roomDoor = door.gameObject.AddComponent<RoomDoor>();
        roomDoor.arrivePoint = arrive.transform;
        roomDoor.arriveRoom = arriveRoom;
        roomDoor.stepOutDirection = stepOutDirection;
        roomDoor.firstTimeMessage = firstTimeMessage;

        UnityEventTools.AddVoidPersistentListener(door.onInteract, roomDoor.Enter);
    }

    // =====================================================================
    //  Scene systems (only what the scene does not have yet)
    // =====================================================================

    private static void EnsureSystems(Transform root, Camera cam, GameObject player)
    {
        GameObject systems = new GameObject("GameSystems");
        systems.transform.SetParent(root, false);

        if (Object.FindAnyObjectByType<Inventory>() == null) systems.AddComponent<Inventory>();
        if (Object.FindAnyObjectByType<StoryFlags>() == null) systems.AddComponent<StoryFlags>();
        if (Object.FindAnyObjectByType<CaseNotebook>() == null) systems.AddComponent<CaseNotebook>();
        if (Object.FindAnyObjectByType<RoomTransition>() == null) systems.AddComponent<RoomTransition>();

        ClickInteractor2D clicker = Object.FindAnyObjectByType<ClickInteractor2D>();
        if (clicker == null) clicker = systems.AddComponent<ClickInteractor2D>();

        SerializedObject so = new SerializedObject(clicker);
        SerializedProperty camProp = so.FindProperty("cam");
        SerializedProperty playerProp = so.FindProperty("player");
        if (camProp.objectReferenceValue == null) camProp.objectReferenceValue = cam;
        if (playerProp.objectReferenceValue == null) playerProp.objectReferenceValue = player.transform;
        so.ApplyModifiedPropertiesWithoutUndo();

        // UI: dialogue box + inventory bar, case book, and the full-screen flash layer
        if (Object.FindAnyObjectByType<DialogueUI>() == null)
            BlockoutSceneBuilder.BuildUI(root,
                "A/D = walk  |  W = jump  |  S = crouch  |  Tab = Case Book  |  Click objects to inspect  |  Explore the clinic");

        if (Object.FindAnyObjectByType<CaseNotebookUI>() == null)
            BlockoutSceneBuilder.BuildNotebookUI();

        if (Object.FindAnyObjectByType<ScreenFlash>() == null)
            BuildScreenFlash();
    }

    private static void BuildScreenFlash()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("No Canvas in the scene, so the ScreenFlash layer was not created (scares will have no flash).");
            return;
        }

        GameObject go = BlockoutSceneBuilder.UIObject("ScreenFlash", canvas.transform);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = go.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = false;
        go.AddComponent<ScreenFlash>();

        // Behind the other UI, so the dialogue box and Case Book stay readable on top of a flash
        go.transform.SetAsFirstSibling();
    }

    private static void EnsureCamera(Camera cam, GameObject player, Room startRoom)
    {
        CameraFollow2D follow = cam.GetComponent<CameraFollow2D>();

        if (follow == null)
        {
            // The project already has its own simple follow script ("CameraFollow"): do not stack two.
            if (cam.GetComponent("CameraFollow") != null)
            {
                Debug.Log("Main Camera already has the 'CameraFollow' script, so CameraFollow2D was not added. " +
                          "It follows the player but has no room limits: near the doors the camera can show the " +
                          "next room. Swap it for CameraFollow2D if you want limits per room.");
                return;
            }
            follow = cam.gameObject.AddComponent<CameraFollow2D>();
        }

        SerializedObject so = new SerializedObject(follow);
        so.FindProperty("target").objectReferenceValue = player.transform;
        so.FindProperty("followY").boolValue = false;
        so.FindProperty("useBounds").boolValue = true;
        so.FindProperty("boundsMin").vector2Value = startRoom.boundsMin;
        so.FindProperty("boundsMax").vector2Value = startRoom.boundsMax;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // =====================================================================
    //  Small helpers
    // =====================================================================

    private static GameObject Block(Transform root, string name, float x, float y, float w, float h,
        Color color, Sprite sprite, int order, bool solid)
    {
        return BlockoutSceneBuilder.MakeBlock(name, root, new Vector2(x, y), new Vector2(w, h), color, sprite, order, solid, false);
    }

    private static void Label(Transform root, string text, float x, float y)
    {
        GameObject go = new GameObject("Label: " + text);
        go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3(x, y, 0f);

        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = 5f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.65f, 0.65f, 0.70f, 0.8f);
    }

    // A clickable object that shows a description (and optionally adds a clue to the Case Book)
    private static Interactable2D Inspect(Transform root, string name, float x, float y, float w, float h,
        Color color, Sprite sprite, string description, string clueId = "", string clueTitle = "", float maxDistance = 2.5f)
    {
        GameObject go = BlockoutSceneBuilder.MakeBlock(name, root, new Vector2(x, y), new Vector2(w, h), color, sprite, 0, true, true);
        Interactable2D item = go.AddComponent<Interactable2D>();
        item.displayName = name;
        item.description = description;
        item.maxDistance = maxDistance;
        item.clueId = clueId;
        item.clueTitle = clueTitle;
        UnityEventTools.AddVoidPersistentListener(item.onInteract, item.SayDescription);
        return item;
    }

    // An item the player can pick up (goes in the inventory and the Case Book)
    private static ItemPickup Pickup(Transform root, string name, float x, float y, float w, float h,
        Color color, Sprite sprite, string itemId, string itemName, string itemDescription)
    {
        GameObject go = BlockoutSceneBuilder.MakeBlock(name, root, new Vector2(x, y), new Vector2(w, h), color, sprite, 1, true, true);
        Interactable2D item = go.AddComponent<Interactable2D>();
        item.displayName = name;

        ItemPickup pickup = go.AddComponent<ItemPickup>();
        pickup.itemId = itemId;
        pickup.itemName = itemName;
        pickup.itemDescription = itemDescription;
        pickup.pickupMessage = "Picked up: " + itemName;
        pickup.icon = sprite;
        UnityEventTools.AddVoidPersistentListener(item.onInteract, pickup.Pickup);
        return pickup;
    }

    // A doorway painted on the BACK wall (like the reference pictures): a frame with a dark opening.
    // The player walks in front of it; clicking it opens it. Not solid (it is behind the player).
    private static Interactable2D Door(Transform root, string name, float x, Sprite sprite, Color color, float width = 2f)
    {
        const float height = 3.6f;
        const float y = -0.7f;   // bottom of the doorway sits on the floor (top at y = -2.5)

        // Dark opening (visual only)
        BlockoutSceneBuilder.MakeBlock(name + " Opening", root, new Vector2(x, y), new Vector2(width - 0.3f, height - 0.2f),
            new Color(0.03f, 0.03f, 0.05f), sprite, 0, false, false);

        // Frame = the clickable part (trigger), drawn behind the opening
        GameObject go = BlockoutSceneBuilder.MakeBlock(name, root, new Vector2(x, y), new Vector2(width, height),
            color, sprite, -1, true, true);
        Interactable2D door = go.AddComponent<Interactable2D>();
        door.displayName = name;
        door.maxDistance = 3.5f;
        return door;
    }

    // An invisible trigger zone with a ScareEvent that fires when the player walks into it
    private static ScareEvent Zone(Transform root, string name, float x, float width)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3(x, -0.2f, 0f);

        BoxCollider2D zone = go.AddComponent<BoxCollider2D>();
        zone.isTrigger = true;
        zone.size = new Vector2(width, 4.6f);

        return go.AddComponent<ScareEvent>();   // default mode: PlayerEntersZone
    }
}
