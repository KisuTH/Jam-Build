using UnityEngine;

/// <summary>
/// Used by the dressing room's objects (next to their Interactable2D, in "On Interact"):
///   Open       = the dressing table  -> the tool-arranging puzzle
/// </summary>
public class WoundPuzzleOpener : MonoBehaviour
{
    public void Open()
    {
        WoundPuzzleUI.OpenStatic();
    }
}
