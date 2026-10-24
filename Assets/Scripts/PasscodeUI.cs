using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The keypad window. Put it on an object that stays ACTIVE and give it the panel (the builder does this).
/// A keypad object in the world has an Interactable2D whose "On Interact" calls PasscodeUI.Open.
///
/// Right code  -> sets the story flag "code_solved".
/// Wrong code  -> the AI phones, then the black monster kills the player (and the loop restarts).
///
/// While the keypad is open, a drug addict may ATTACK from the left or the right every few seconds.
/// The player must keep looking left and right (the Look Left / Look Right buttons) and stare at him
/// for a moment to drive him off. If the player does not look at the right side in time, the monster
/// attacks and the player dies.
///
/// The "phone call" box (top left) shows the digits the player has already found, like "0 - 9 - ? - ?".
/// </summary>
public class PasscodeUI : MonoBehaviour
{
    public static PasscodeUI Instance { get; private set; }

    public const string SolvedFlag = "code_solved";

    [Header("Windows")]
    [Tooltip("The whole keypad window (everything below is inside it).")]
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject keypadView;   // the keypad (looking straight ahead)
    [SerializeField] private GameObject leftView;     // what you see when you look left
    [SerializeField] private GameObject rightView;    // what you see when you look right
    [SerializeField] private GameObject leftThreat;   // the addict, shown in the left view when he is there
    [SerializeField] private GameObject rightThreat;

    [Header("Texts")]
    [SerializeField] private TMP_Text displayText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text phoneText;      // "PHONE CALL 0 - 9 - ? - ?"
    [SerializeField] private TMP_Text cueText;        // "...footsteps..." / "He backs away"

    [Header("The addict's attacks")]
    [SerializeField] private bool enableAttacks = true;
    [Tooltip("Seconds after opening the keypad until the first attack.")]
    [SerializeField] private float firstAttackDelay = 6f;
    [SerializeField] private float minInterval = 7f;
    [SerializeField] private float maxInterval = 12f;
    [Tooltip("Seconds the player has to look at the right side before the attack lands.")]
    [SerializeField] private float reactionTime = 4f;
    [Tooltip("Seconds the player must keep looking at him to drive him off.")]
    [SerializeField] private float stareTime = 0.6f;

    public bool IsOpen => panel != null && panel.activeSelf;

    private string entered = "";
    private bool busy;

    private int view;              // -1 = looking left, 0 = keypad, 1 = looking right
    private int attackSide;        // 0 = nobody is coming, -1 = from the left, 1 = from the right
    private float nextAttackIn;
    private float attackTimer;
    private float stare;
    private float cueClearTime;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf || busy) return;

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            if (view != 0) SetView(0);
            else Close();
            return;
        }
        if (kb != null && kb.leftArrowKey.wasPressedThisFrame) LookLeft();
        if (kb != null && kb.rightArrowKey.wasPressedThisFrame) LookRight();

        // The cue text goes away after a moment
        if (cueClearTime > 0f && Time.time > cueClearTime)
        {
            cueClearTime = 0f;
            if (attackSide == 0 && cueText != null) cueText.text = "";
        }

        if (enableAttacks) UpdateAttacks();
    }

    // ===== Opening and closing =====

    /// <summary>Hook this into the keypad's "On Interact".</summary>
    public void Open()
    {
        if (StoryFlags.Instance != null && StoryFlags.Instance.Has(SolvedFlag))
        {
            if (DialogueUI.Instance != null) DialogueUI.Instance.Show("The lock is already open.");
            return;
        }

        entered = "";
        busy = false;
        attackSide = 0;
        nextAttackIn = firstAttackDelay;
        stare = 0f;
        if (cueText != null) cueText.text = "";
        if (messageText != null) messageText.text = Loc.Thai ? "ใส่รหัส " + LoopManager.CodeLength + " หลัก" : "Enter the " + LoopManager.CodeLength + "-digit code";
        UpdateDisplay();
        panel.SetActive(true);
        SetView(0);
        LoopManager.SetPlayerLocked(true);
    }

    public void Close()
    {
        if (busy) return;
        attackSide = 0;
        panel.SetActive(false);
        LoopManager.SetPlayerLocked(false);
    }

    // ===== Looking left and right =====

    public void LookLeft()
    {
        if (busy) return;
        SetView(view == 1 ? 0 : -1);
    }

    public void LookRight()
    {
        if (busy) return;
        SetView(view == -1 ? 0 : 1);
    }

    private void SetView(int newView)
    {
        view = newView;
        stare = 0f;
        if (keypadView != null) keypadView.SetActive(view == 0);
        if (leftView != null) leftView.SetActive(view == -1);
        if (rightView != null) rightView.SetActive(view == 1);
        UpdatePhone();
        UpdateThreats();
    }

    private void UpdateAttacks()
    {
        if (attackSide == 0)
        {
            // Waiting for the next attack
            nextAttackIn -= Time.deltaTime;
            if (nextAttackIn <= 0f)
            {
                attackSide = Random.value < 0.5f ? -1 : 1;
                attackTimer = reactionTime;
                stare = 0f;
                if (cueText != null) cueText.text = "...footsteps. Someone is coming. Look around!";
                UpdateThreats();
            }
            return;
        }

        // Someone is coming from attackSide
        if (view == attackSide)
        {
            // The player is looking right at him: stare him down
            stare += Time.deltaTime;
            if (stare >= stareTime) DriveOff();
            return;
        }

        stare = 0f;
        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f) AttackLands();
    }

    private void DriveOff()
    {
        attackSide = 0;
        stare = 0f;
        nextAttackIn = Random.Range(minInterval, maxInterval);
        if (cueText != null) cueText.text = Loc.T("He backs away into the dark. Keep going...");
        cueClearTime = Time.time + 2.5f;
        UpdateThreats();
    }

    private void AttackLands()
    {
        int side = attackSide;
        attackSide = 0;
        busy = false;
        panel.SetActive(false);
        if (cueText != null) cueText.text = "";
        // The monster comes from the side the addict was on
        LoopManager.Ensure().Die(side);
    }

    private void UpdateThreats()
    {
        if (leftThreat != null) leftThreat.SetActive(attackSide == -1);
        if (rightThreat != null) rightThreat.SetActive(attackSide == 1);
    }

    // ===== Typing the code =====

    public void AddDigit(string digit)
    {
        if (busy || view != 0 || entered.Length >= LoopManager.CodeLength) return;
        entered += digit;
        UpdateDisplay();
    }

    public void Clear()
    {
        if (busy) return;
        entered = "";
        UpdateDisplay();
    }

    public void Submit()
    {
        if (busy) return;
        if (entered.Length < LoopManager.CodeLength)
        {
            if (messageText != null) messageText.text = Loc.Thai ? "ใส่ให้ครบ " + LoopManager.CodeLength + " หลัก" : "Enter all " + LoopManager.CodeLength + " digits";
            return;
        }

        busy = true;
        if (LoopManager.IsCorrect(entered)) StartCoroutine(Correct());
        else Wrong();
    }

    private IEnumerator Correct()
    {
        if (messageText != null) messageText.text = Loc.T("CLICK. Unlocked.");
        if (StoryFlags.Instance != null) StoryFlags.Instance.Set(SolvedFlag);
        attackSide = 0;
        yield return new WaitForSeconds(0.8f);
        busy = false;
        Close();
        if (DialogueUI.Instance != null) DialogueUI.Instance.Show("The lock clicks open.");
    }

    private void Wrong()
    {
        if (messageText != null) messageText.text = Loc.T("WRONG.");
        attackSide = 0;
        panel.SetActive(false);
        busy = false;
        LoopManager.Ensure().FailPuzzle(CallSituation.WrongCode);
    }

    private void UpdateDisplay()
    {
        if (displayText == null) return;
        string s = "";
        for (int i = 0; i < LoopManager.CodeLength; i++)
            s += (i < entered.Length ? entered[i].ToString() : "_") + " ";
        displayText.text = s.Trim();
    }

    // The digits the player has found so far, like "0 - 9 - ? - ?"
    private void UpdatePhone()
    {
        if (phoneText == null) return;
        LoopManager loop = LoopManager.Ensure();
        string s = "";
        for (int i = 0; i < LoopManager.CodeLength; i++)
        {
            bool known = loop.IsPositionFound(i) && LoopManager.Code != null;
            s += (known ? LoopManager.Code[i].ToString() : "?");
            if (i < LoopManager.CodeLength - 1) s += " - ";
        }
        phoneText.text = "<size=60%><color=#FF6060>PHONE CALL</color></size>\n" + s;
    }
}
