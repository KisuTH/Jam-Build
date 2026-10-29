using UnityEngine;

/// <summary>
/// Hides one item in a RANDOM drawer out of several (a new random choice every time the scene starts,
/// so every life / loop the key can be somewhere else). Put it on an object that is always active
/// (the builder puts it on the Room object) and list the drawers.
/// </summary>
public class RandomKeyPlacer : MonoBehaviour
{
    public DrawerSpot[] drawers;
    public string itemId = "";
    public string itemName = "";
    [TextArea] public string itemDescription = "";
    public string iconResource = "";

    private void Awake()
    {
        if (drawers == null || drawers.Length == 0) return;

        int pick = Random.Range(0, drawers.Length);
        for (int i = 0; i < drawers.Length; i++)
        {
            DrawerSpot d = drawers[i];
            if (d == null) continue;
            bool has = i == pick;
            d.itemId = has ? itemId : "";
            d.itemName = has ? itemName : "";
            d.itemDescription = has ? itemDescription : "";
            d.iconResource = has ? iconResource : "";
        }
    }
}
