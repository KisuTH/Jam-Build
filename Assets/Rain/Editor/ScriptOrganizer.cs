using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu: Tools > Rain > Organize Scripts Into Folders.
/// Moves the scripts in Assets/Scripts into category folders with AssetDatabase.MoveAsset,
/// which keeps the .meta files (and so every reference in scenes and prefabs).
/// Safe to run more than once: scripts that are already in place are skipped.
/// </summary>
public static class ScriptOrganizer
{
    private static readonly Dictionary<string, string[]> Folders = new Dictionary<string, string[]>
    {
        { "Player&Camera",     new[] { "PlayerMovement2D", "PointAndClickPlayer", "CameraFollow2D", "CameraShake2D" } },
        { "Interaction",       new[] { "ClickInteractor2D", "Interactable", "Interactable2D", "ItemPickup", "CodeClue", "WrongChoice" } },
        { "Inventory&CaseBook", new[] { "Inventory", "InventoryUI", "CaseNotebook", "CaseNotebookUI", "StoryFlags" } },
        { "Rooms",             new[] { "Room", "RoomDoor", "RoomTransition" } },
        { "Dialogue",          new[] { "DialogueUI", "DialogueSequence" } },
        { "Loop&AI",           new[] { "LoopManager", "AICaller", "PasscodeUI", "HintSystem", "DeathMonster" } },
        { "Story&Effects",     new[] { "ScareEvent", "ChapterEnd", "ScreenFlash" } },
        { "Localization",      new[] { "Loc" } },
    };

    [MenuItem("Tools/Rain/Organize Scripts Into Folders")]
    public static void Organize()
    {
        const string root = "Assets/Scripts";
        int moved = 0;
        List<string> problems = new List<string>();

        // 1) make the folders (and let Unity see them) BEFORE moving anything
        foreach (string folderName in Folders.Keys)
        {
            if (!AssetDatabase.IsValidFolder(root + "/" + folderName)) AssetDatabase.CreateFolder(root, folderName);
        }
        AssetDatabase.Refresh();

        // 2) move the scripts one by one
        foreach (KeyValuePair<string, string[]> folder in Folders)
        {
            string dir = root + "/" + folder.Key;
            foreach (string name in folder.Value)
            {
                string from = root + "/" + name + ".cs";
                string to = dir + "/" + name + ".cs";
                if (AssetDatabase.LoadMainAssetAtPath(from) == null) continue;   // not here (already moved)
                string error = AssetDatabase.MoveAsset(from, to);
                if (string.IsNullOrEmpty(error)) moved++;
                else problems.Add(name + ": " + error);
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string message = "Moved " + moved + " script(s).";
        if (problems.Count > 0) message += "\nProblems:\n" + string.Join("\n", problems);
        Debug.Log("ScriptOrganizer: " + message);
        EditorUtility.DisplayDialog("Organize scripts", message, "OK");
    }
}
