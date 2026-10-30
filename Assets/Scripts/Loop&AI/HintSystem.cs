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
        StyleButton();
        UpdateLabel();
    }

    // The hint button in the prefab is a plain white box with white text, so it looks blank.
    // Give it the dark-red look of the other HUD buttons, find its label and make sure a click asks for a hint.
    private void StyleButton()
    {
        UnityEngine.UI.Button btn = null;
        foreach (UnityEngine.UI.Button b in FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include))
            if (b.gameObject.name.IndexOf("Hint", System.StringComparison.OrdinalIgnoreCase) >= 0) { btn = b; break; }
        if (btn == null) return;

        UnityEngine.UI.Image img = btn.GetComponent<UnityEngine.UI.Image>();
        if (img != null)
        {
            if (img.sprite == null) img.color = new Color(0.32f, 0.12f, 0.12f, 0.95f);
            else { img.color = Color.white; img.preserveAspect = true; }
        }

        if (buttonLabel == null) buttonLabel = btn.GetComponentInChildren<TMP_Text>(true);
        if (buttonLabel != null)
        {
            buttonLabel.color = Color.white;
            buttonLabel.enableAutoSizing = true;
            buttonLabel.fontSizeMin = 12;
            buttonLabel.fontSizeMax = 26;
            MatchCaseBookText(btn, buttonLabel);
            buttonLabel.alignment = TextAlignmentOptions.Center;
            buttonLabel.raycastTarget = false;
        }

        bool wired = false;
        for (int i = 0; i < btn.onClick.GetPersistentEventCount(); i++)
            if (btn.onClick.GetPersistentTarget(i) == this) wired = true;
        if (!wired) { btn.onClick.RemoveListener(AskForHint); btn.onClick.AddListener(AskForHint); }
    }

    // Make the hint label the same size / font as the Case Book button's label
    private static void MatchCaseBookText(UnityEngine.UI.Button hint, TMP_Text label)
    {
        foreach (UnityEngine.UI.Button b in FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include))
        {
            if (b == hint) continue;
            TMP_Text t = b.GetComponentInChildren<TMP_Text>(true);
            string n = b.gameObject.name.ToLower();
            string tx = t != null ? t.text.ToLower() : "";
            bool isCase = n.Contains("case") || n.Contains("notebook") || tx.Contains("case") || tx.Contains("notebook");
            if (!isCase || t == null) continue;
            label.enableAutoSizing = t.enableAutoSizing;
            if (t.enableAutoSizing) { label.fontSizeMin = t.fontSizeMin; label.fontSizeMax = t.fontSizeMax; }
            label.fontSize = t.fontSize;
            label.font = t.font;
            label.fontStyle = t.fontStyle;
            return;
        }
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
        else if (DialogueUI.Instance != null) DialogueUI.Instance.ShowHint(hint);
    }

    // A clue the player has not found yet; if all are found, a general hint
    private string BuildHint()
    {
        int slot = LoopManager.Ensure().PickUnfoundSlot();
        if (slot < 0) return allFoundHint;
        string where = LoopManager.SlotLocation[slot];
        if (!string.IsNullOrEmpty(where)) return "Look at " + where + ".";
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
