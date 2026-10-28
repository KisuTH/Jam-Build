// Menu:  Tools > Rain > Add Wound-Care Room (Room 4)
//
// Adds "Room 4: Dressing Room" to the open scene (Gameplay), reached from the Ward through a new door.
//   - a paper on the wall with the wound-care order  (the clue)
//   - the dressing table: click it to open the tool-arranging puzzle (WoundPuzzleUI)
//   - a drawer cabinet: shut until the puzzle is solved, then it holds the evidence
//   - a door back to the Ward
// Needs the existing "LongTestLevel" (Room 2 = Ward). Everything new is under "WoundRoom" / "Content: Room 4"
// and the new door pieces under the Ward's content, so you can delete them to undo.
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WoundRoomBuilder
{
    private const float CX = 60f;        // centre of the new room (local x). Rooms 1-3 end at 36.
    private const float WardDoorX = -3.75f;
    private const float EntryDoorX = 50f;

    [MenuItem("Tools/Rain/Add Wound-Care Room (Room 4)")]
    public static void Build()
    {
        const string title = "Wound-care room";
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(title, "Stop Play mode first.", "OK");
            return;
        }

        GameObject rootGO = GameObject.Find("LongTestLevel");
        if (rootGO == null)
        {
            EditorUtility.DisplayDialog(title, "There is no 'LongTestLevel' in this scene. Open the Gameplay scene first.", "OK");
            return;
        }
        RemoveOldBuild();

        Transform root = rootGO.transform;
        Transform wardContent = root.Find("Content: Room 2");
        Room ward = null;
        foreach (Room r in Object.FindObjectsByType<Room>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (r.roomName.Contains("Ward")) ward = r;
        if (wardContent == null || ward == null)
        {
            EditorUtility.DisplayDialog(title, "Could not find the Ward ('Content: Room 2' and its Room).", "OK");
            return;
        }

        if (!AssetDatabase.IsValidFolder(BlockoutSceneBuilder.Folder))
            AssetDatabase.CreateFolder("Assets", "BlockoutTest");
        Sprite sprite = BlockoutSceneBuilder.CreateWhiteSquareSprite();
        EnsureTableImport();

        // --- room shell ---
        GameObject holderGO = new GameObject("Content: Room 4");
        holderGO.transform.SetParent(root, false);
        Transform h = holderGO.transform;
        Undo.RegisterCreatedObjectUndo(holderGO, "Add Room 4");

        Room room4 = MakeRoom(root, "Room 4: Dressing Room", CX - 12.6f, CX + 12.6f);
        room4.content = holderGO;

        Color dark = new Color(0.08f, 0.08f, 0.10f);
        Block(h, "Room 4 Wall", CX, 1.25f, 24f, 7.5f, new Color(0.14f, 0.17f, 0.18f), sprite, -3, false);
        Block(h, "Room 4 Floor", CX, -4f, 25f, 3f, new Color(0.10f, 0.10f, 0.12f), sprite, -2, true);
        Block(h, "Room 4 Wall Left", CX - 12.25f, 0f, 0.5f, 10f, dark, sprite, -1, true);
        Block(h, "Room 4 Wall Right", CX + 12.25f, 0f, 0.5f, 10f, dark, sprite, -1, true);
        Label(h, "ROOM 4: DRESSING ROOM", CX, 4f);

        // --- furniture / scenery ---
        Block(h, "Curtain", CX - 8f, 0.2f, 2.2f, 4.6f, new Color(0.16f, 0.30f, 0.34f), sprite, 0, false);
        Block(h, "Side Cabinet", CX + 9f, -1.5f, 2.2f, 2f, new Color(0.22f, 0.24f, 0.26f), sprite, 0, false);
        Block(h, "Stool", CX + 6.5f, -2.0f, 0.9f, 1f, new Color(0.10f, 0.12f, 0.16f), sprite, 0, false);

        // --- the clue paper ---
        Interactable2D paper = Inspect(h, "Wound Care Instructions", CX - 4f, 0.6f, 1.6f, 2.2f,
            new Color(0.78f, 0.72f, 0.58f), sprite,
            "An old notice: WOUND CARE ORDER. 1. Prepare the tray. 2. Place the bandage. 3. Prepare the wound tools (scissors / forceps). 4. Prepare the medicine / disinfectant. - Public Health Officer",
            "wound_order", "Wound care order", 4f);
        paper.descriptionThai = Loc.Shape("เอกสารเก่า: คำสั่งการทำแผล 1. เตรียมถาด 2. ใส่ผ้าพันแผล 3. เตรียมอุปกรณ์ทำแผล (กรรไกร / คีม) 4. เตรียมยา / น้ำยาฆ่าเชื้อ - เจ้าหน้าที่สาธารณสุข");
        paper.clueText = "Wound care order: 1 tray, 2 bandage, 3 tools (scissors), 4 medicine / disinfectant.";

        // --- the dressing table with the puzzle ---
        Interactable2D table = Inspect(h, "Dressing Table", CX + 1f, -1.9f, 4f, 1.4f,
            new Color(0.34f, 0.35f, 0.37f), sprite,
            "A steel dressing table. The tools on it are all scattered.", "", "", 3.5f);
        table.descriptionThai = Loc.Shape("โต๊ะทำแผลสแตนเลส อุปกรณ์บนโต๊ะกระจัดกระจายไปหมด");
        // replace the "say description" action with "open the puzzle"
        table.onInteract = new UnityEngine.Events.UnityEvent();
        WoundPuzzleOpener opener = table.gameObject.AddComponent<WoundPuzzleOpener>();
        UnityEventTools.AddVoidPersistentListener(table.onInteract, opener.Open);
        Label(h, "DRESSING TABLE", CX + 1f, -0.9f, 2.6f);

        // --- the drawer cabinet: shut until the puzzle is solved, then it holds the cursed red file ---
        DrawerSpot redDrawer = MakeDrawer(h, "Drawer Cabinet", CX + 5f, -1.6f, 1.6f, 1.8f, sprite, "wound_drawer",
            WoundPuzzleUI.SolvedFlag, "cursed_red_file", "Cursed Red File",
            "A red file with a smeared handprint on the cover. It feels cold to hold. Inside, a name has been scratched out.");
        redDrawer.lockedEnglish = "The drawer is shut tight. Something in this room must unlock it...";
        redDrawer.lockedThai = "ลิ้นชักปิดแน่น คงมีอะไรบางอย่างในห้องนี้ที่ปลดล็อกมันได้...";
        Label(h, "DRAWER CABINET", CX + 5f, -0.4f, 2.6f);

        // The Bedside Note sat exactly where the door goes: slide it left, above the bed (still "near the bed")
        Transform note = wardContent.Find("Bedside Note");
        if (note != null && note.localPosition.x > -8.1f)
        {
            Undo.RecordObject(note, "Move Bedside Note");
            note.localPosition = new Vector3(-8.2f, note.localPosition.y, note.localPosition.z);
        }

        // --- doors: Ward <-> Room 4 ---
        Color col = new Color(0.30f, 0.45f, 0.40f);
        Interactable2D toRoom = Door(wardContent, "Door C (Ward side) Dressing", WardDoorX, sprite, col, 1.8f);
        LinkDoor(root, toRoom, new Vector2(EntryDoorX, -1.7f), room4, 1f, "The door to the dressing room creaks open...");
        toRoom.requiredItemId = "wound_room_key";
        toRoom.consumeRequiredItem = false;
        toRoom.missingItemMessage = "The dressing room door is locked. I need a key.";
        Label(wardContent, "DRESSING ROOM", WardDoorX, 1.5f, 3.2f);

        Interactable2D back = Door(h, "Door C (Dressing Room side)", EntryDoorX, sprite, col, 1.8f);
        LinkDoor(root, back, new Vector2(WardDoorX, -1.7f), ward, 1f, "");
        Label(h, "BACK", EntryDoorX, 1.5f);

        BuildPatientRoom(root, wardContent, ward, sprite);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = holderGO;
        Debug.Log("Room 4 (Dressing Room) and Room 5 (Patient Room) added. Save the scene (Ctrl+S), press Play, go to the Ward and use the new door at x = -3.75.");
    }

    // Room 5: three patient beds, each with a drawer. One drawer holds the dressing room key.
    private static void BuildPatientRoom(Transform root, Transform wardContent, Room ward, Sprite sprite)
    {
        const float PX = 100f;      // centre of the patient room
        const float WardPatientDoorX = -6.5f;   // above the Ward bed, between the Bedside Note and the dressing room door
        const float EntryX = 90f;

        GameObject holderGO = new GameObject("Content: Room 5");
        holderGO.transform.SetParent(root, false);
        Transform h = holderGO.transform;
        Room room5 = MakeRoom(root, "Room 5: Patient Room", PX - 12.6f, PX + 12.6f);
        room5.content = holderGO;

        Color dark = new Color(0.08f, 0.08f, 0.10f);
        Block(h, "Room 5 Wall", PX, 1.25f, 24f, 7.5f, new Color(0.17f, 0.17f, 0.15f), sprite, -3, false);
        Block(h, "Room 5 Floor", PX, -4f, 25f, 3f, new Color(0.10f, 0.10f, 0.12f), sprite, -2, true);
        Block(h, "Room 5 Wall Left", PX - 12.25f, 0f, 0.5f, 10f, dark, sprite, -1, true);
        Block(h, "Room 5 Wall Right", PX + 12.25f, 0f, 0.5f, 10f, dark, sprite, -1, true);
        Label(h, "ROOM 5: PATIENT ROOM", PX, 4f);

        string[] bedText =
        {
            "A patient bed. The sheets are torn, as if someone clawed at them.",
            "A patient bed. The mattress is still warm, but nobody is here.",
            "A patient bed. A name tag at the foot has been scratched off."
        };
        float[] bedX = { PX - 6f, PX, PX + 6f };
        for (int i = 0; i < 3; i++)
        {
            Inspect(h, "Patient Bed " + (i + 1), bedX[i], -2.0f, 3f, 1.0f,
                new Color(0.50f, 0.50f, 0.55f), sprite, bedText[i], "", "", 3f);

            // the drawer (bedside cabinet) next to each bed. The middle one hides the key.
            bool hasKey = i == 1;
            DrawerSpot d = MakeDrawer(h, "Bedside Drawer " + (i + 1), bedX[i] + 2.3f, -1.75f, 1.2f, 1.5f, sprite,
                "bed_drawer_" + (i + 1), "",
                hasKey ? "wound_room_key" : "", hasKey ? "Dressing Room Key" : "",
                hasKey ? "A small brass key with a paper tag: 'Dressing Room'." : "");
            if (hasKey) d.iconResource = "Items/Key";
            Label(h, "BEDSIDE DRAWER", bedX[i] + 2.3f, -0.6f, 2.6f);
        }

        // doors: Ward <-> Patient Room
        Color col = new Color(0.45f, 0.40f, 0.30f);
        Interactable2D toRoom = Door(wardContent, "Door D (Ward side) Patients", WardPatientDoorX, sprite, col, 1.8f);
        LinkDoor(root, toRoom, new Vector2(EntryX, -1.7f), room5, 1f, "The door to the patient room creaks open...");
        Label(wardContent, "PATIENT ROOM", WardPatientDoorX, 1.5f, 3.2f);

        Interactable2D back = Door(h, "Door D (Patient Room side)", EntryX, sprite, col, 1.8f);
        LinkDoor(root, back, new Vector2(WardPatientDoorX, -1.7f), ward, -1f, "");
        Label(h, "BACK", EntryX, 1.5f);
    }

    // A clickable cabinet that opens the drawer close-up
    private static DrawerSpot MakeDrawer(Transform parent, string name, float x, float y, float w, float h, Sprite sprite,
        string id, string requiredFlag, string itemId, string itemName, string itemDesc)
    {
        Interactable2D cab = Inspect(parent, name, x, y, w, h, new Color(0.45f, 0.37f, 0.30f), sprite,
            "A small cabinet with a drawer.", "", "", 3.5f);
        cab.onInteract = new UnityEngine.Events.UnityEvent();
        DrawerSpot spot = cab.gameObject.AddComponent<DrawerSpot>();
        spot.drawerId = id;
        spot.requiredFlag = requiredFlag;
        spot.itemId = itemId;
        spot.itemName = itemName;
        spot.itemDescription = itemDesc;
        spot.icon = sprite;
        UnityEventTools.AddVoidPersistentListener(cab.onInteract, spot.Open);
        return spot;
    }

    // Running the menu again replaces the previous Room 4 (so you can re-run after a change)
    private static void RemoveOldBuild()
    {
        string[] names =
        {
            "Content: Room 4", "Room 4: Dressing Room",
            "Door C (Ward side) Dressing", "Door C (Ward side) Dressing Opening", "Label: DRESSING ROOM",
            "Arrive: Door C (Ward side) Dressing", "Arrive: Door C (Dressing Room side)",
            "Content: Room 5", "Room 5: Patient Room",
            "Door D (Ward side) Patients", "Door D (Ward side) Patients Opening", "Label: PATIENT ROOM",
            "Arrive: Door D (Ward side) Patients", "Arrive: Door D (Patient Room side)"
        };
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null) continue;
            foreach (string n in names)
                if (t.gameObject.name == n) { Object.DestroyImmediate(t.gameObject); break; }
        }
    }

    // The picture used by the puzzle must be importable as a Sprite
    private static void EnsureTableImport()
    {
        const string path = "Assets/Resources/WoundPuzzle/wound_table.png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { Debug.LogWarning("wound_table.png not found at " + path); return; }
        if (ti.textureType != TextureImporterType.Sprite || ti.spriteImportMode != SpriteImportMode.Single)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.maxTextureSize = 2048;
            ti.SaveAndReimport();
        }
    }

    // ---------------- helpers (copies of the private ones in LongLevelBuilder) ----------------

    private static GameObject Block(Transform parent, string name, float x, float y, float w, float h,
        Color color, Sprite sprite, int order, bool solid)
    {
        return BlockoutSceneBuilder.MakeBlock(name, parent, new Vector2(x, y), new Vector2(w, h), color, sprite, order, solid, false);
    }

    private static void Label(Transform parent, string text, float x, float y, float size = 5f)
    {
        GameObject go = new GameObject("Label: " + text);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(x, y, 0f);
        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.65f, 0.65f, 0.70f, 0.8f);
    }

    private static Interactable2D Inspect(Transform parent, string name, float x, float y, float w, float h,
        Color color, Sprite sprite, string description, string clueId, string clueTitle, float maxDistance)
    {
        GameObject go = BlockoutSceneBuilder.MakeBlock(name, parent, new Vector2(x, y), new Vector2(w, h), color, sprite, 0, true, true);
        Interactable2D item = go.AddComponent<Interactable2D>();
        item.displayName = name;
        item.description = description;
        item.maxDistance = maxDistance;
        item.clueId = clueId;
        item.clueTitle = clueTitle;
        UnityEventTools.AddVoidPersistentListener(item.onInteract, item.SayDescription);
        return item;
    }

    private static Interactable2D Door(Transform parent, string name, float x, Sprite sprite, Color color, float width)
    {
        const float height = 3.6f;
        const float y = -0.7f;
        BlockoutSceneBuilder.MakeBlock(name + " Opening", parent, new Vector2(x, y), new Vector2(width - 0.3f, height - 0.2f),
            new Color(0.03f, 0.03f, 0.05f), sprite, 0, false, false);
        GameObject go = BlockoutSceneBuilder.MakeBlock(name, parent, new Vector2(x, y), new Vector2(width, height),
            color, sprite, -1, true, true);
        Interactable2D door = go.AddComponent<Interactable2D>();
        door.displayName = name;
        door.maxDistance = 3.5f;
        return door;
    }

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
}
