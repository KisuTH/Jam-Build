using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Text box at the bottom of the screen. Types text out letter by letter.
/// Click once to show all the text, click again to close (or it closes by itself).
///
/// Setup: put this script on an object that stays ACTIVE (e.g. the Canvas),
/// and assign "panel" to a child Panel that holds the TMP text.
/// </summary>
public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private float charsPerSecond = 40f;
    [SerializeField] private float autoCloseDelay = 3f;

    /// <summary>
    /// ON = a DialogueSequence is in charge: this box ignores clicks and never closes by itself.
    /// (Set by DialogueSequence. Normal one-line messages are not affected.)
    /// </summary>
    public bool ManualMode { get; set; }

    /// <summary>The text is still being typed out.</summary>
    public bool IsTyping => typing;

    private Coroutine routine;
    private bool typing;
    private int shownFrame = -1;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
        FitText();
    }

    // Long texts (e.g. a 4-step notice) must wrap and shrink to fit the box instead of running off it
    private void FitText()
    {
        if (dialogueText == null) return;
        dialogueText.textWrappingMode = TextWrappingModes.Normal;
        dialogueText.overflowMode = TextOverflowModes.Overflow;
        if (!dialogueText.enableAutoSizing)
        {
            dialogueText.fontSizeMax = dialogueText.fontSize;
            dialogueText.fontSizeMin = Mathf.Max(12f, dialogueText.fontSize * 0.5f);
            dialogueText.enableAutoSizing = true;
        }
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf || ManualMode) return;

        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

        // Ignore the same click that opened the box
        if (Time.frameCount == shownFrame) return;

        if (typing)
        {
            // First click: show all the text right away
            if (routine != null) StopCoroutine(routine);
            dialogueText.maxVisibleCharacters = int.MaxValue;
            typing = false;
            routine = StartCoroutine(AutoClose());
        }
        else
        {
            // Second click: close
            Hide();
        }
    }

    public void Show(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (routine != null) StopCoroutine(routine);

        text = Loc.T(text);
        shownFrame = Time.frameCount;
        panel.SetActive(true);
        routine = StartCoroutine(Typewriter(text));
    }

    /// <summary>Show all the text right now (skip the typing).</summary>
    public void SkipTyping()
    {
        if (!typing) return;
        if (routine != null) StopCoroutine(routine);
        dialogueText.maxVisibleCharacters = int.MaxValue;
        typing = false;
    }

    public void Hide()
    {
        if (routine != null) StopCoroutine(routine);
        typing = false;
        panel.SetActive(false);
    }

    private IEnumerator Typewriter(string text)
    {
        typing = true;
        dialogueText.text = text;
        dialogueText.ForceMeshUpdate();

        int total = dialogueText.textInfo.characterCount;
        float delay = 1f / charsPerSecond;

        for (int i = 0; i <= total; i++)
        {
            dialogueText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(delay);
        }

        dialogueText.maxVisibleCharacters = int.MaxValue;
        typing = false;
        if (!ManualMode) routine = StartCoroutine(AutoClose());
    }

    private IEnumerator AutoClose()
    {
        float len = dialogueText != null ? dialogueText.text.Length : 0;
        yield return new WaitForSeconds(Mathf.Max(autoCloseDelay, len / 14f));   // longer text stays longer
        panel.SetActive(false);
    }
}
