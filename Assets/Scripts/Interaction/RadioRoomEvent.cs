using System.Collections;
using UnityEngine;

/// <summary>
/// The radio room. Put it on a trigger box that covers the room (Tools > Rain > Setup Radio Room does it all).
///
/// When the player walks in (first time):
///   1. the room locks (every door inside needs the flag "radio_unlocked"; the keypad on the wall sets it),
///   2. after a short pause the radio plays "Clip" (the voice reading the 6-digit code),
///   3. when the audio ends the code is written in the Case Book, together with a short story note.
/// No audio assigned yet? Then it waits "Fallback Seconds" instead, so everything can be tested already.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class RadioRoomEvent : MonoBehaviour
{
    public const string HeardFlag = "radio_heard";
    public const string UnlockFlag = "radio_unlocked";

    [Header("Drag the voice recording here")]
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    [Tooltip("Pause between the door locking and the radio starting (seconds).")]
    public float delayBeforeAudio = 1.5f;
    [Tooltip("Used only when no Clip is assigned yet (seconds the 'radio' lasts).")]
    public float fallbackSeconds = 6f;

    [Header("The code")]
    public string code = "021604";

    private bool started;
    private AudioSource source;

    private static bool Has(string flag) { return StoryFlags.Instance != null && StoryFlags.Instance.Has(flag); }
    private static string L(string en, string th) { return Loc.Thai ? Loc.Shape(th) : en; }
    private static void Say(string text) { if (DialogueUI.Instance != null) DialogueUI.Instance.Show(text); }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (started || Has(HeardFlag)) return;
        if (!other.CompareTag("Player") && other.GetComponentInParent<PlayerMovement2D>() == null) return;
        started = true;
        StartCoroutine(Run());
    }

    // The player left the room: the radio stops at once. If the code was not finished yet,
    // walking back in starts the radio again from the beginning.
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!started || Has(HeardFlag)) return;
        if (!other.CompareTag("Player") && other.GetComponentInParent<PlayerMovement2D>() == null) return;
        StopAllCoroutines();
        if (source != null) source.Stop();
        started = false;
    }

    private IEnumerator Run()
    {
        // 1) the door locks behind the player
        CameraShake2D.Shake(0.3f, 0.08f);
        Say(L("Click. The door locked behind me.", "แกร๊ก... ประตูล็อกอยู่ข้างหลังฉัน"));
        yield return new WaitForSeconds(delayBeforeAudio);

        // 2) the radio
        float wait;
        if (clip != null)
        {
            if (source == null)
            {
                source = GetComponent<AudioSource>();
                if (source == null) source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;   // 2D: the same loudness everywhere in the room
            }
            source.clip = clip;
            source.volume = volume;
            source.Play();
            wait = clip.length;
        }
        else
        {
            Say(L("...the old radio crackles to life.", "...วิทยุเก่าส่งเสียงซ่าขึ้นมา"));
            wait = fallbackSeconds;
        }
        // wait until the audio has really finished (also works if the game was paused for a while)
        float t = 0f;
        while (t < wait + 0.3f && (clip == null || (source != null && source.isPlaying)))
        {
            t += Time.deltaTime;
            yield return null;
        }

        // 3) the Case Book
        if (CaseNotebook.Instance != null)
            CaseNotebook.Instance.AddClue("radio_code", "The Radio Room Code",
                "The moment I stepped into the radio room, the door locked behind me. Then the old radio crackled to life, " +
                "and a voice slowly read out six numbers. That is the code that opens the room: " + code + ".");
        if (StoryFlags.Instance != null) StoryFlags.Instance.Set(HeardFlag);
        Say(L(code + ". That must be the code. I wrote it in my Case Book.", code + "... นั่นต้องเป็นรหัสแน่ ฉันจดลงสมุดเคสไว้แล้ว"));
    }
}
