using UnityEngine;

/// <summary>
/// One possible hiding place of a passcode digit. Put it on an object with an Interactable2D and hook
/// "On Interact" -> CodeClue.Reveal.
///
/// "Slot" (0-6) is the number of this place. The level has 7 such places, but each loop the game
/// picks only 4 of them to really show a digit (and which digit position each shows is shuffled).
/// The other places are decoys this loop ("nothing here"). So after dying, the digits are somewhere new.
/// </summary>
public class CodeClue : MonoBehaviour
{
    [Range(0, 6)] public int slot = 0;
    [TextArea] public string flavorText = "Numbers are scratched into the wall:";
    [TextArea] public string nothingText = "Just old scratches and dust. Nothing useful here.";

    private void Awake() { LoopManager.RegisterSlot(slot); }
    private void OnDestroy() { LoopManager.UnregisterSlot(slot); }

    private static readonly string[] Ordinals = { "first", "second", "third", "fourth" };

    public void Reveal()
    {
        int position = LoopManager.PositionForSlot(slot);

        // A decoy this loop
        if (position < 0)
        {
            if (DialogueUI.Instance != null) DialogueUI.Instance.Show(nothingText);
            return;
        }

        int digit = LoopManager.DigitForSlot(slot);
        string note = "The " + Ordinals[position] + " digit of the code is " + digit + ".";

        if (DialogueUI.Instance != null)
            DialogueUI.Instance.Show(flavorText + " " + note);

        if (LoopManager.Ensure() != null) LoopManager.Ensure().MarkFound(slot);

        // Written in the Case Book (a new page each loop, because the book restarts with the scene)
        if (CaseNotebook.Instance != null)
            CaseNotebook.Instance.AddClue("code_slot_" + slot, "Code: " + Ordinals[position] + " digit", note);
    }
}
