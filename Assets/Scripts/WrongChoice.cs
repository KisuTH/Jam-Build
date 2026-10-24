using UnityEngine;

/// <summary>
/// A wrong path. The first time(s) the player goes there the AI phones with a WARNING.
/// After "Warnings Before Death" warnings, the next time the AI phones and the player dies.
///
/// Use it two ways:
///  - on a clickable object: Interactable2D "On Interact" -> WrongChoice.Choose
///  - on an invisible trigger zone: tick "Trigger On Enter" (the Player needs the tag 'Player')
/// </summary>
public class WrongChoice : MonoBehaviour
{
    [Tooltip("How many warning calls before the next mistake kills the player. 0 = die right away.")]
    public int warningsBeforeDeath = 1;
    [Tooltip("On a trigger zone: act when the Player walks in.")]
    public bool triggerOnEnter = false;

    private int mistakes;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnEnter && other.CompareTag("Player")) Choose();
    }

    public void Choose()
    {
        if (AICaller.Instance != null && AICaller.Instance.IsCalling) return;
        if (LoopManager.Ensure() == null)
        {
            Debug.LogWarning("WrongChoice: no LoopManager in the scene.", this);
            return;
        }

        mistakes++;
        if (mistakes > warningsBeforeDeath)
            LoopManager.Ensure().FailPuzzle(CallSituation.WrongWayDeath);
        else if (AICaller.Instance != null)
            AICaller.Instance.Call(CallSituation.WrongWayWarning, 2);
    }
}
