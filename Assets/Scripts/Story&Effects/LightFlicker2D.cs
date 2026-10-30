using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Makes a 2D light blink on and off at random. Put it on an object that has a Light2D
/// (Global Light 2D or any spot / point light). Nothing else needs setting up.
///
///   Min / Max On Time   : how long the light stays ON each time (a random time between the two)
///   Min / Max Off Time  : how long the light stays OFF each time (a random time between the two)
///   On Intensity        : brightness when on (0 = the light's own value from the scene)
///   Off Intensity       : brightness when off (0 = completely dark)
///
/// Other scripts can call  SetFlicker(false)  to stop it (the light goes back to normal) and  SetFlicker(true)  to start it,
/// or  FlickerFor(seconds)  for a flicker that stops by itself.
/// </summary>
[RequireComponent(typeof(Light2D))]
public class LightFlicker2D : MonoBehaviour
{
    public bool flickerOnStart = true;

    [Header("On time (seconds)")]
    public float minOnTime = 0.3f;
    public float maxOnTime = 2.5f;

    [Header("Off time (seconds)")]
    public float minOffTime = 0.05f;
    public float maxOffTime = 0.6f;

    [Header("Brightness")]
    [Tooltip("0 = use the light's own intensity from the scene.")]
    public float onIntensity = 0f;
    public float offIntensity = 0f;

    private Light2D lamp;
    private float normalIntensity;
    private bool flickering;
    private bool isOn = true;
    private float switchAt;
    private float stopAt = -1f;

    private void Awake()
    {
        lamp = GetComponent<Light2D>();
        normalIntensity = lamp.intensity;
        if (onIntensity <= 0f) onIntensity = normalIntensity;
    }

    private void OnEnable()
    {
        flickering = flickerOnStart;
        if (flickering) BeginCycle();
    }

    private void OnDisable()
    {
        if (lamp != null) lamp.intensity = normalIntensity;
    }

    /// <summary>Start or stop the flicker. Stopping puts the light back to its normal brightness.</summary>
    public void SetFlicker(bool on)
    {
        flickering = on;
        stopAt = -1f;
        if (on) BeginCycle();
        else if (lamp != null) lamp.intensity = normalIntensity;
    }

    /// <summary>Flicker for a number of seconds, then go back to normal.</summary>
    public void FlickerFor(float seconds)
    {
        SetFlicker(true);
        stopAt = Time.time + seconds;
    }

    private void BeginCycle()
    {
        isOn = true;
        if (lamp != null) lamp.intensity = onIntensity;
        switchAt = Time.time + RandomTime(minOnTime, maxOnTime);
    }

    private void Update()
    {
        if (!flickering || lamp == null) return;

        if (stopAt > 0f && Time.time >= stopAt) { SetFlicker(false); return; }

        if (Time.time < switchAt) return;

        isOn = !isOn;
        lamp.intensity = isOn ? onIntensity : offIntensity;
        switchAt = Time.time + (isOn ? RandomTime(minOnTime, maxOnTime) : RandomTime(minOffTime, maxOffTime));
    }

    private static float RandomTime(float min, float max)
    {
        float lo = Mathf.Max(0.01f, Mathf.Min(min, max));
        float hi = Mathf.Max(lo, Mathf.Max(min, max));
        return Random.Range(lo, hi);
    }
}
