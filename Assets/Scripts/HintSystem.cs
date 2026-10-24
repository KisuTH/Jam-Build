using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Hints: the player may ask for a hint (button or H key) a limited number of times per life.
/// Every time, the AI phones, scolds the player and then gives the hint.
/// Asking when no hints are left = the AI phones and the player dies.
///
/// A new life (after dying) gets the hints back, because the scene reloads.
/// Put ONE in the scene (the builder adds it to GameSystems) and connect the button's label.
/// </summary>
public class HintSystem : MonoBehaviour
{
    public static HintSystem Instance { get; private set; }

    [SerializeField] private int maxHints = 2;
    [Tooltip("Text on the hint button. It shows how many hints are left.")]
    [SerializeField] private TMP_Text buttonLabel;
    [SerializeField] private bool openWithHKey = true;

    [Header("Where each code spot is (slot 0-6 of CodeClue)")]
    [TextArea] public string[] clueLocations =
    {
        "the calendar in the Waiting Hall",
        "the clock in the Ward",
        "the stain on the floor in the Records Room",
        "the writing on the wall in the Records Room",
        "the faded poster at the far left of the Waiting Hall",
        "the note on the wall near the hospital bed in the Ward",
        "the tag between the filing cabinets in the Records Room"
    };
    [TextArea] public string allFoundHint =
        "You already have every piece. Open the Case Book, put the digits in order and use the keypad by the exit.";

    private int used;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        UpdateLabel();
    }

    private void Update()
    {
        if (!openWithHKey) return;
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.hKey.wasPressedThisFrame) AskForHint();
    }

    /// <summary>Hook this into the hint button's OnClick.</summary>
    public void AskForHint()
    {
        LoopManager loop = LoopManager.Ensure();
        if (loop == null)
        {
            Debug.LogWarning("HintSystem: there is no LoopManager in the scene.", this);
            return;
        }

        // Not while a call is running, the player is dying or the keypad is open
        if (loop.IsDying) return;
        if (AICaller.Instance != null && AICaller.Instance.IsCalling) return;
        if (PasscodeUI.Instance != null && PasscodeUI.Instance.IsOpen) return;

        // No hints left: the AI loses patience
        if (used >= maxHints)
        {
            loop.FailPuzzle(CallSituation.HintsUsedUp);
            return;
        }

        used++;
        UpdateLabel();

        string hint = "HINT: " + BuildHint();
        if (used >= maxHints) hint += " That was your LAST hint. Ask again and you die.";

        if (CaseNotebook.Instance != null)
            CaseNotebook.Instance.AddClue("hint_" + used, "Hint " + used, hint);

        if (AICaller.Instance != null) AICaller.Instance.Call(CallSituation.HintScolding, 1, hint);
        else if (DialogueUI.Instance != null) DialogueUI.Instance.Show(hint);
    }

    // A clue the player has not found yet; if all are found, a general hint
    private string BuildHint()
    {
        int slot = LoopManager.Ensure().PickUnfoundSlot();
        if (slot < 0) return allFoundHint;
        if (clueLocations == null || slot >= clueLocations.Length || string.IsNullOrEmpty(clueLocations[slot]))
            return "Search again. Something has not been checked yet.";
        return "Look at " + clueLocations[slot] + ".";
    }

    private void UpdateLabel()
    {
        if (buttonLabel != null)
            buttonLabel.text = "Hint [H] (" + Mathf.Max(0, maxHints - used) + " left)";
    }
}
