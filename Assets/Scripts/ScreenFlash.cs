using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A full-screen colour layer for scares: Flash() blinks a colour and fades out,
/// FadeToColor() fades the screen to a colour and keeps it (e.g. black at the end of a chapter).
/// Put it on a full-screen UI Image (stretched over the Canvas, Raycast Target off).
/// The Level builder creates this for you.
/// </summary>
[RequireComponent(typeof(Image))]
public class ScreenFlash : MonoBehaviour
{
    public static ScreenFlash Instance { get; private set; }

    private Image image;
    private Coroutine routine;

    private void Awake()
    {
        Instance = this;
        image = GetComponent<Image>();
        image.raycastTarget = false;
        SetAlpha(Color.black, 0f);
    }

    /// <summary>Shows the colour at once, then fades it out over "duration" seconds.</summary>
    public void Flash(Color color, float duration)
    {
        Run(FlashRoutine(color, Mathf.Max(0.01f, duration)));
    }

    /// <summary>Fades from clear to the colour and stays there.</summary>
    public void FadeToColor(Color color, float duration)
    {
        Run(FadeInRoutine(color, Mathf.Max(0.01f, duration)));
    }

    /// <summary>Fades whatever colour is on screen back to clear.</summary>
    public void FadeToClear(float duration)
    {
        Run(FadeOutRoutine(Mathf.Max(0.01f, duration)));
    }

    private void Run(IEnumerator r)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(r);
    }

    private IEnumerator FlashRoutine(Color color, float duration)
    {
        float startAlpha = color.a;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            SetAlpha(color, Mathf.Lerp(startAlpha, 0f, t / duration));
            yield return null;
        }
        SetAlpha(color, 0f);
    }

    private IEnumerator FadeInRoutine(Color color, float duration)
    {
        float targetAlpha = color.a;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            SetAlpha(color, Mathf.Lerp(0f, targetAlpha, t / duration));
            yield return null;
        }
        SetAlpha(color, targetAlpha);
    }

    private IEnumerator FadeOutRoutine(float duration)
    {
        Color color = image.color;
        float startAlpha = color.a;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            SetAlpha(color, Mathf.Lerp(startAlpha, 0f, t / duration));
            yield return null;
        }
        SetAlpha(color, 0f);
    }

    private void SetAlpha(Color color, float alpha)
    {
        image.color = new Color(color.r, color.g, color.b, alpha);
    }
}
