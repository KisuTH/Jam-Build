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
    [Tooltip("Optional: small label for the speaker's name (the tab of the box). Made by Tools > Rain > Apply Dialogue Box Background.")]
    [SerializeField] private TMP_Text speakerText;
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
        if (speakerText == null && panel != null)
        {
            Transform t = panel.transform.Find("SpeakerTag");
            if (t != null) speakerText = t.GetComponent<TMP_Text>();
        }
        if (panel != null) panel.SetActive(false);
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

    /// <summary>True when the box has a separate name tab.</summary>
    public bool HasSpeakerTag { get { return speakerText != null; } }

    /// <summary>Show a line with the speaker's name in the name tab (falls back to one text if there is no tab).</summary>
    public void ShowWithSpeaker(string speaker, string text)
    {
        if (speakerText == null)
        {
            Show(string.IsNullOrEmpty(speaker) ? text : speaker + "\n" + text);
            return;
        }
        bool has = !string.IsNullOrEmpty(speaker);
        speakerText.gameObject.SetActive(has);
        if (has) speakerText.text = speaker;
        ShowInternal(text, false);
    }

    public void Show(string text)
    {
        ShowInternal(text, true);
    }

    private void ShowInternal(string text, bool hideTag)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (hideTag && speakerText != null) speakerText.gameObject.SetActive(false);
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
        yield return new WaitForSeconds(autoCloseDelay);
        panel.SetActive(false);
    }
}
