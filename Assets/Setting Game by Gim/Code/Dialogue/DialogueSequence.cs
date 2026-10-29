using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>One line of a conversation. Everything is written in the Inspector.</summary>
[Serializable]
public class DialogueLine
{
    [Tooltip("Who is talking, e.g. Detective, Nurse, ??? . Leave empty for narration.")]
    public string speaker = "";

    [TextArea(2, 6)]
    [Tooltip("What is said. You can use rich text: <b>bold</b>, <i>italic</i>, <color=red>red</color>.")]
    public string text = "";

    [Tooltip("0 = wait for the player to click / press Space. More than 0 = go on by itself after this many seconds.")]
    public float autoAdvanceAfter = 0f;

    [Header("Thai (optional - used when the language is Thai)")]
    public string speakerThai = "";
    [TextArea(1, 4)] public string textThai = "";

    [Tooltip("Optional: runs when this line appears (shake the camera, play a sound, turn something on...).")]
    public UnityEvent onLineShown = new UnityEvent();
}

/// <summary>
/// A conversation the dev writes in the Inspector. Add it to any object, then press "+" under Lines
/// and type who speaks and what they say.
///
/// How it starts (pick one or more):
///  - hook an Interactable2D "On Interact" to  DialogueSequence.Play   (click an NPC / object)
///  - "Play On Start"            (an opening monologue)
///  - "Play When Player Enters"  (the object needs a Collider2D with Is Trigger ON, the Player needs the tag 'Player')
///  - "Play When Flag Is Set"    (when a story flag turns on)
///  - call Play() from any UnityEvent / script
///
/// While it plays: click or Space / Enter / E = show all the text, then go to the next line.
/// Needs the DialogueUI of the scene (the builders add it).
/// </summary>
public class DialogueSequence : MonoBehaviour
{
    /// <summary>True while any conversation is on screen (the click interactor waits).</summary>
    public static bool IsAnyPlaying { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPlaybackState()
    {
        // Static fields survive Play Mode when domain reload is disabled.
        IsAnyPlaying = false;
    }

    [Header("Lines (write the conversation here)")]
    public DialogueLine[] lines = new DialogueLine[]
    {
        new DialogueLine { speaker = "Detective", text = "Write the first line here." }
    };

    // Preserve old scene data; ChatboxView now lays out the speaker separately.
    [HideInInspector] public string speakerFormat = "<b><color=#FFD27F>{0}</color></b>\n{1}";

    [Header("When it plays")]
    public bool playOnStart = false;
    public bool playWhenPlayerEnters = false;
    [Tooltip("Plays when this story flag turns on, e.g. got_register. Empty = not used.")]
    public string playWhenFlagIsSet = "";
    [Tooltip("Needs ALL of these story flags before it plays. Empty = no condition.")]
    public string[] requiredFlags = new string[0];
    [Tooltip("Play only the first time. After that, nothing happens.")]
    public bool playOnlyOnce = false;

    [Header("While it plays")]
    [Tooltip("The Player cannot walk during the conversation.")]
    public bool lockPlayer = true;
    [Tooltip("Click / Space / Enter / E to read on.")]
    public bool allowSkipWithKeys = true;

    [Header("When it ends")]
    [Tooltip("Turn this story flag on at the end. Empty = none.")]
    public string setsFlagWhenDone = "";
    public UnityEvent onFinished = new UnityEvent();

    private bool playing;
    private bool played;
    private int startFrame;
    private DialogueUI activeUI;

    private void Start()
    {
        if (playOnStart) Play();

        if (!string.IsNullOrEmpty(playWhenFlagIsSet) && StoryFlags.Instance != null)
        {
            StoryFlags.Instance.OnFlagSet += OnFlag;
            if (StoryFlags.Instance.Has(playWhenFlagIsSet)) Play();
        }
    }

    private void OnDisable()
    {
        if (!playing) return;
        StopAllCoroutines();
        // Unity may stop the iterator without running its finally block.
        ReleaseControl();
    }

    private void OnDestroy()
    {
        if (StoryFlags.Instance != null) StoryFlags.Instance.OnFlagSet -= OnFlag;
    }

    private void OnFlag(string flag)
    {
        if (flag == playWhenFlagIsSet) Play();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (playWhenPlayerEnters && other.CompareTag("Player")) Play();
    }

    /// <summary>Starts the conversation. Hook this into "On Interact" or any UnityEvent.</summary>
    [ContextMenu("Test: Play")]
    public void Play()
    {
        if (!isActiveAndEnabled || playing || IsAnyPlaying) return;
        if (playOnlyOnce && played) return;
        if (lines == null || lines.Length == 0) return;
        if (DialogueUI.Instance == null)
        {
            Debug.LogWarning("DialogueSequence: there is no DialogueUI in the scene.", this);
            return;
        }
        if (!HasRequiredFlags()) return;

        StartCoroutine(Run());
    }

    private bool HasRequiredFlags()
    {
        if (requiredFlags == null) return true;
        foreach (string flag in requiredFlags)
        {
            if (string.IsNullOrEmpty(flag)) continue;
            if (StoryFlags.Instance == null || !StoryFlags.Instance.Has(flag)) return false;
        }
        return true;
    }

    private IEnumerator Run()
    {
        playing = true;
        IsAnyPlaying = true;
        played = true;
        DialogueUI ui = DialogueUI.Instance;
        activeUI = ui;
        ui.ManualMode = true;
        if (lockPlayer) SetPlayerLocked(true);

        try
        {
            foreach (DialogueLine line in lines)
            {
                if (line == null) continue;

                startFrame = Time.frameCount;
                string speaker = Loc.Thai && !string.IsNullOrEmpty(line.speakerThai)
                    ? Loc.Shape(line.speakerThai) : Loc.T(line.speaker);
                string text = Loc.Thai && !string.IsNullOrEmpty(line.textThai)
                    ? Loc.Shape(line.textThai) : Loc.T(line.text);
                ui.ShowLine(speaker, text);
                if (line.onLineShown != null) line.onLineShown.Invoke();

                float waited = 0f;
                while (true)
                {
                    yield return null;
                    waited += Time.deltaTime;

                    // Auto-advance lines
                    if (line.autoAdvanceAfter > 0f && !ui.IsTyping && waited >= line.autoAdvanceAfter) break;

                    if (PressedAdvance())
                    {
                        if (ui.IsTyping) ui.SkipTyping();   // first press: show all the text
                        else break;                          // second press: next line
                    }
                }
            }
        }
        finally
        {
            ReleaseControl();
        }

        if (!string.IsNullOrEmpty(setsFlagWhenDone) && StoryFlags.Instance != null)
            StoryFlags.Instance.Set(setsFlagWhenDone);
        if (onFinished != null) onFinished.Invoke();
    }

    private void ReleaseControl()
    {
        // Only the active sequence may release the shared interaction lock.
        if (!playing) return;
        playing = false;
        IsAnyPlaying = false;
        if (activeUI != null)
        {
            activeUI.ManualMode = false;
            activeUI.Hide();
        }
        activeUI = null;
        if (lockPlayer) SetPlayerLocked(false);
    }

    private bool PressedAdvance()
    {
        if (!allowSkipWithKeys) return false;
        if (Time.frameCount == startFrame) return false;   // not the click that started the line

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;

        Keyboard kb = Keyboard.current;
        if (kb == null) return false;
        return kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame;
    }

    private static void SetPlayerLocked(bool locked)
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;
        PlayerMovement2D move = player.GetComponent<PlayerMovement2D>();
        if (move != null) move.SetControlLocked(locked);
    }
}
