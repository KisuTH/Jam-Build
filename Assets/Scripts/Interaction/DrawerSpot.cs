using UnityEngine;

/// <summary>
/// A drawer the player can open (put it on the same object as an Interactable2D and add DrawerSpot.Open to "On Interact").
/// It shows the close-up window (WoundDrawerUI) with the pictures in Assets/Resources/Drawer.
///   Required Flag : empty = the drawer opens right away; otherwise it stays shut until that story flag is on.
///   Item          : what is inside (empty Item Id = the drawer is empty). Texts are written in English;
///                   Loc.cs translates them (add the Thai text there).
/// </summary>
public class DrawerSpot : MonoBehaviour
{
    [Tooltip("Unique name of this drawer (used to remember it was emptied).")]
    public string drawerId = "drawer";
    [Tooltip("Story flag that must be on before the drawer opens. Empty = not locked.")]
    public string requiredFlag = "";

    [Header("Inside (leave Item Id empty for an empty drawer)")]
    public string itemId = "";
    public string itemName = "";
    [TextArea] public string itemDescription = "";
    public Sprite icon;
    [Tooltip("Optional picture from a Resources folder (e.g. Items/Key) used as the inventory icon instead of Icon.")]
    public string iconResource = "";

    [Header("Texts (English / Thai)")]
    [TextArea] public string lockedEnglish = "The drawer is shut tight. It will not budge. Maybe it unlocks some other way...";
    [TextArea] public string lockedThai = "ลิ้นชักปิดแน่น ดึงเท่าไรก็ไม่ขยับ บางทีอาจต้องปลดล็อกด้วยวิธีอื่น...";
    [TextArea] public string emptyEnglish = "The drawer is empty. Only dust.";
    [TextArea] public string emptyThai = "ลิ้นชักว่างเปล่า มีแต่ฝุ่น";

    public string TakenFlag { get { return "drawer_taken_" + drawerId; } }

    // The inventory icon: the Resources picture if given, otherwise the Icon field
    public Sprite GetIcon()
    {
        if (!string.IsNullOrEmpty(iconResource))
        {
            Sprite s = Resources.Load<Sprite>(iconResource);
            if (s == null)
            {
                Sprite[] all = Resources.LoadAll<Sprite>(iconResource);
                if (all != null && all.Length > 0) s = all[0];
            }
            if (s == null)
            {
                Texture2D t = Resources.Load<Texture2D>(iconResource);
                if (t != null) s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
            }
            if (s != null) return s;
        }
        return icon;
    }

    public void Open()
    {
        WoundDrawerUI.OpenStatic(this);
    }
}
