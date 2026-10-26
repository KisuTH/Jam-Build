using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menu: Tools > Rain > Apply Evidence List.
/// Puts the team's evidence list (health station case) into the objects of the OPEN scene.
/// It only changes the texts / names of existing objects (nothing is moved or deleted).
/// Run it, then save the scene (Ctrl+S). Safe to run more than once.
/// Thai text for these lines is in Loc.cs (Dict).
/// </summary>
public static class EvidenceUpdater
{
    private struct Entry
    {
        public string oldName, newName, description, clueTitle, itemDescription, pickupMessage;
        public Entry(string o, string n, string d, string ct, string idesc, string pm)
        { oldName = o; newName = n; description = d; clueTitle = ct; itemDescription = idesc; pickupMessage = pm; }
    }

    private static readonly Entry[] Entries =
    {
        // 1. Torn photo
        new Entry("Torn Photo", "Torn Photo", null, null,
            "A photo of the health station staff from years ago. Part of it has been torn away.", null),
        // 2. Document fragment
        new Entry("Cabinet 'M'", "Cabinet 'M'",
            "A cabinet labelled 'M'. A torn document fragment is stuck in the drawer. A page of the case report is missing.",
            "Document fragment", null, null),
        // 3. Medical equipment
        new Entry("Medicine Cabinet", "Medical Equipment",
            "A tray of old medical instruments. Something abnormal once happened inside this health station.",
            "Medical equipment", null, null),
        // 4. Evidence board
        new Entry("Notice Board", "Evidence Board",
            "A board pinned with photos, scraps of paper and instruments. Put them together and analyse them.",
            "Evidence board", null, null),
        // 5. Old patient records
        new Entry("Patient Register", "Old Patient Records", null, null,
            "Old medical records. Some patients' names are crossed out, next to their treatment dates. One name is circled in red: 'Mali'.",
            "Picked up: Old Patient Records"),
        // 6. Old key
        new Entry("Old Key", "Old Key", null, null,
            "An old key. It opens a locked room.", null),
        // 7. Direct line phone
        new Entry("Phone", "Direct Line",
            "A direct line phone. A call came in from inside the building, yet the outside line looks cut.",
            "Direct line", null, null),
    };

    [MenuItem("Tools/Rain/Apply Evidence List")]
    public static void Apply()
    {
        int changed = 0;
        Interactable2D[] all = Resources.FindObjectsOfTypeAll<Interactable2D>();
        foreach (Interactable2D it in all)
        {
            if (it == null || !it.gameObject.scene.IsValid()) continue;
            foreach (Entry e in Entries)
            {
                if (it.displayName != e.oldName && it.displayName != e.newName) continue;

                SerializedObject so = new SerializedObject(it);
                so.FindProperty("displayName").stringValue = e.newName;
                if (e.description != null) so.FindProperty("description").stringValue = e.description;
                if (e.clueTitle != null && !string.IsNullOrEmpty(so.FindProperty("clueId").stringValue))
                    so.FindProperty("clueTitle").stringValue = e.clueTitle;
                so.ApplyModifiedProperties();

                ItemPickup pick = it.GetComponent<ItemPickup>();
                if (pick != null)
                {
                    SerializedObject ps = new SerializedObject(pick);
                    ps.FindProperty("itemName").stringValue = e.newName;
                    if (e.itemDescription != null) ps.FindProperty("itemDescription").stringValue = e.itemDescription;
                    if (e.pickupMessage != null) ps.FindProperty("pickupMessage").stringValue = e.pickupMessage;
                    ps.ApplyModifiedProperties();
                }

                EditorUtility.SetDirty(it);
                changed++;
                break;
            }
        }

        if (changed > 0) EditorSceneManager.MarkAllScenesDirty();
        Debug.Log("EvidenceUpdater: updated " + changed + " object(s). Save the scene with Ctrl+S.");
        EditorUtility.DisplayDialog("Evidence list", "Updated " + changed + " object(s).\nPress Ctrl+S to save the scene.", "OK");
    }
}
