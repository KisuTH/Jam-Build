using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A scary moment. It waits for something to happen, then plays a mix of effects:
/// a message, a screen flash, a camera shake, a sound, objects turning on / off,
/// and things falling.
///
/// WHEN it happens (Trigger Mode):
///   PlayerEntersZone = the player walks into this object's trigger Collider2D
///   WhenFlagIsSet    = a story flag turns on (e.g. after picking up the torn photo)
///   OnlyWhenCalled   = you call Trigger() yourself (e.g. from an Interactable2D event)
///
/// For "PlayerEntersZone" this object needs a Collider2D with Is Trigger ticked,
/// and the player must have the tag "Player".
/// </summary>
public class ScareEvent : MonoBehaviour
{
    public enum TriggerMode { PlayerEntersZone, WhenFlagIsSet, OnlyWhenCalled }

    [Header("When does it happen?")]
    public TriggerMode triggerMode = TriggerMode.PlayerEntersZone;
    [Tooltip("For WhenFlagIsSet: the flag to wait for.")]
    public string flagToWaitFor = "";
    [Tooltip("Optional: it only happens if this flag is already on.")]
    public string onlyIfFlag = "";
    public bool onlyOnce = true;
    [Tooltip("Seconds to wait before the effects play.")]
    public float delay = 0f;

    [Header("What happens?")]
    [TextArea] public string message = "";

    public bool flashScreen = false;
    public Color flashColor = Color.black;
    public float flashDuration = 0.6f;

    public bool shakeCamera = false;
    public float shakeDuration = 0.5f;
    public float shakeStrength = 0.2f;

    [Tooltip("Objects that appear.")]
    public GameObject[] turnOn = new GameObject[0];
    [Tooltip("Objects that disappear.")]
    public GameObject[] turnOff = new GameObject[0];
    [Tooltip("Rigidbody2D objects that start falling (set to Kinematic until now).")]
    public Rigidbody2D[] dropBodies = new Rigidbody2D[0];

    public AudioClip sound;
    [Tooltip("A story flag to turn on afterwards.")]
    public string setFlag = "";
    public UnityEvent onTriggered = new UnityEvent();

    private bool done;

    private void Start()
    {
        if (triggerMode == TriggerMode.WhenFlagIsSet && StoryFlags.Instance != null)
        {
            StoryFlags.Instance.OnFlagSet += OnFlagSet;
            // The flag may already be on (e.g. coming back to a scene)
            if (StoryFlags.Instance.Has(flagToWaitFor)) TryTrigger();
        }
    }

    private void OnDestroy()
    {
        if (StoryFlags.Instance != null)
            StoryFlags.Instance.OnFlagSet -= OnFlagSet;
    }

    private void OnFlagSet(string flag)
    {
        if (triggerMode == TriggerMode.WhenFlagIsSet && flag == flagToWaitFor) TryTrigger();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerMode == TriggerMode.PlayerEntersZone && other.CompareTag("Player")) TryTrigger();
    }

    /// <summary>Starts the scare (also usable from a UnityEvent).</summary>
    public void Trigger()
    {
        TryTrigger();
    }

    private void TryTrigger()
    {
        if (done && onlyOnce) return;

        if (!string.IsNullOrEmpty(onlyIfFlag) &&
            (StoryFlags.Instance == null || !StoryFlags.Instance.Has(onlyIfFlag)))
            return;

        done = true;
        StartCoroutine(Play());
    }

    private IEnumerator Play()
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        if (!string.IsNullOrEmpty(message) && DialogueUI.Instance != null)
            DialogueUI.Instance.Show(message);

        if (flashScreen && ScreenFlash.Instance != null)
            ScreenFlash.Instance.Flash(flashColor, flashDuration);

        if (shakeCamera) CameraShake2D.Shake(shakeDuration, shakeStrength);

        if (sound != null)
        {
            Vector3 where = Camera.main != null ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(sound, where);
        }

        foreach (GameObject go in turnOn) if (go != null) go.SetActive(true);
        foreach (GameObject go in turnOff) if (go != null) go.SetActive(false);
        foreach (Rigidbody2D body in dropBodies) if (body != null) body.bodyType = RigidbodyType2D.Dynamic;

        if (!string.IsNullOrEmpty(setFlag) && StoryFlags.Instance != null)
            StoryFlags.Instance.Set(setFlag);

        onTriggered?.Invoke();
    }
}
