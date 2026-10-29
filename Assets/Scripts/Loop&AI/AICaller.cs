using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum CallSituation { WrongCode, WrongWayWarning, WrongWayDeath, LoopStart, HintScolding, HintsUsedUp }

/// <summary>
/// The AI that phones the player. It rings, shows a call box and "types" a few insulting lines.
/// Put it on an object that stays ACTIVE (the builder makes "AI Caller UI"), and give it the call box (panel).
///
/// The lines are lists you can edit in the Inspector: each call picks a few RANDOM lines from the list.
/// {loop} in a line becomes the loop number.
/// (The default font has no Thai letters: for Thai lines you need a Thai TMP font.)
/// </summary>
public class AICaller : MonoBehaviour
{
    public static AICaller Instance { get; private set; }

    [Header("Call box")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text callerText;
    [SerializeField] private TMP_Text lineText;
    [SerializeField] private string callerName = "UNKNOWN CALLER";

    [Header("Timing")]
    [SerializeField] private float ringTime = 1.6f;
    [SerializeField] private float charsPerSecond = 40f;
    [SerializeField] private float holdAfterLine = 1.3f;

    [Header("Sound (optional)")]
    [Tooltip("A ringing sound. Leave empty for silence.")]
    [SerializeField] private AudioClip ringSound;

    [Header("What the AI says")]
    [TextArea] public string[] wrongCodeLines =
    {
        "Wrong. I gave you four digits to find and you still managed to fail.",
        "Did you even LOOK at the notes? Pathetic.",
        "A child could have opened that lock. Goodnight.",
        "That was the wrong code. You know what happens now, right?"
    };
    [TextArea] public string[] wrongWayWarningLines =
    {
        "Hey! Where do you think you're going? Not that way, idiot.",
        "Stop wandering. The way out is NOT down there.",
        "Do you want to die? Because that is where you die."
    };
    [TextArea] public string[] wrongWayDeathLines =
    {
        "I told you. I TOLD you. Not that way.",
        "That is what you get for ignoring me.",
        "...and now you are dead. Again. Impressive."
    };
    [TextArea] public string[] loopStartLines =
    {
        "Loop {loop}. Welcome back. Try to be less useless this time.",
        "The code has changed. Everything you remembered about it is garbage.",
        "Do not die again. It is getting boring."
    };

    [TextArea] public string[] hintScoldingLines =
    {
        "Really? You need help ALREADY?",
        "Fine. I will hold your hand. Again.",
        "How are you this slow? Listen carefully.",
        "Ugh. Do I have to do everything myself?"
    };
    [TextArea] public string[] hintsUsedUpLines =
    {
        "I gave you two hints. TWO. And you want a third?",
        "No more help. You used it all. Goodbye.",
        "Begging for hints again? That is the end of the line."
    };

    public bool IsCalling { get; private set; }

    private AudioSource audioSource;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    /// <summary>Starts a call (does nothing if one is already running).</summary>
    public void Call(CallSituation situation, int lineCount = 2, string finalLine = null)
    {
        if (IsCalling) return;
        StartCoroutine(CallRoutine(situation, lineCount, false, finalLine));
    }

    /// <summary>The whole call as a coroutine. keepLocked = do not give the keys back afterwards.</summary>
    public IEnumerator CallRoutine(CallSituation situation, int lineCount, bool keepLocked, string finalLine = null)
    {
        IsCalling = true;
        LoopManager.SetPlayerLocked(true);
        if (DialogueUI.Instance != null) DialogueUI.Instance.Hide();

        // Ringing
        if (panel != null) panel.SetActive(true);
        if (callerText != null) callerText.text = Loc.T("INCOMING CALL...");
        if (lineText != null) lineText.text = "";
        if (ringSound != null)
        {
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.PlayOneShot(ringSound);
        }
        if (ScreenFlash.Instance != null) ScreenFlash.Instance.Flash(new Color(1f, 1f, 1f, 0.35f), 0.25f);
        CameraShake2D.Shake(ringTime, 0.05f);
        yield return new WaitForSeconds(ringTime);

        if (callerText != null) callerText.text = Loc.T(callerName);

        // The lines
        List<string> lines = PickLines(situation, lineCount);
        if (!string.IsNullOrEmpty(finalLine)) lines.Add(finalLine);   // e.g. the hint itself
        foreach (string line in lines)
        {
            yield return Type(Loc.T(line).Replace("{loop}", LoopManager.LoopCount.ToString()));
            yield return new WaitForSeconds(holdAfterLine);
        }

        if (panel != null) panel.SetActive(false);
        IsCalling = false;
        if (!keepLocked) LoopManager.SetPlayerLocked(false);
    }

    private List<string> PickLines(CallSituation situation, int count)
    {
        string[] pool;
        switch (situation)
        {
            case CallSituation.WrongCode: pool = wrongCodeLines; break;
            case CallSituation.WrongWayWarning: pool = wrongWayWarningLines; break;
            case CallSituation.WrongWayDeath: pool = wrongWayDeathLines; break;
            case CallSituation.HintScolding: pool = hintScoldingLines; break;
            case CallSituation.HintsUsedUp: pool = hintsUsedUpLines; break;
            default: pool = loopStartLines; break;
        }

        // Shuffle a copy, take the first few
        List<string> list = new List<string>(pool);
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            string tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
        if (list.Count > count) list.RemoveRange(count, list.Count - count);
        return list;
    }

    private IEnumerator Type(string text)
    {
        if (lineText == null) yield break;
        lineText.text = text;
        lineText.ForceMeshUpdate();
        int total = lineText.textInfo.characterCount;
        float delay = 1f / charsPerSecond;
        for (int i = 0; i <= total; i++)
        {
            lineText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(delay);
        }
        lineText.maxVisibleCharacters = int.MaxValue;
    }
}
