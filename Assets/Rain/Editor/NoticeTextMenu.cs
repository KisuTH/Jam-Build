#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class NoticeTextMenu
{
    [MenuItem("Tools/Rain/Fix Wound Notice Text")]
    public static void Fix()
    {
        const string full = "An old notice: WOUND CARE ORDER. 1. Prepare the tray. 2. Place the bandage. 3. Prepare the wound tools (scissors / forceps). 4. Prepare the medicine / disinfectant. - Public Health Officer";
        int n = 0;
        foreach (Interactable2D it in Object.FindObjectsByType<Interactable2D>(FindObjectsInactive.Include))
        {
            if (it.displayName != "Wound Care Instructions") continue;
            Undo.RecordObject(it, "Notice text");
            it.description = full;
            EditorUtility.SetDirty(it);
            n++;
        }
        Debug.Log("Notice text fixed on " + n + " object(s). Save the scene (Ctrl+S).");
    }
}
#endif
