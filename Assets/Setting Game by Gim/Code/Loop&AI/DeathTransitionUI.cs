using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;

/// <summary>Presentation for death, the rolling loop counter, and the covered scene reload.</summary>
public sealed class DeathTransitionUI : MonoBehaviour
{
    private const float PageSlideTime = 0.45f;
    private const float RoundHoldTime = 0.85f;
    private const float NumberSlideTime = 0.55f;
    private const float ImpactTime = 0.45f;
    private const float NumberTravel = 240f;

    private RectTransform canvasRect;
    private CanvasGroup cover;
    private RectTransform deathPage;
    private RectTransform roundPage;
    private RectTransform roundContent;
    private CanvasGroup roundContentAlpha;
    private TMP_Text roundLabel;
    private TMP_Text previousNumber;
    private TMP_Text nextNumber;

    public static DeathTransitionUI Create(Transform owner, TMP_Text style, Color deathColor)
    {
        GameObject root = new GameObject("DeathTransition", typeof(RectTransform), typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster),
            typeof(CanvasGroup), typeof(DeathTransitionUI));
        root.layer = 5;
        root.transform.SetParent(owner, false);
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 32000;
        var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        DeathTransitionUI view = root.GetComponent<DeathTransitionUI>();
        view.Build(style, deathColor);
        return view;
    }

    private void Build(TMP_Text style, Color deathColor)
    {
        canvasRect = (RectTransform)transform;
        cover = GetComponent<CanvasGroup>();
        cover.blocksRaycasts = true;
        cover.interactable = false;

        deathPage = FullPage("DeathPage", transform, new Color(deathColor.r, deathColor.g, deathColor.b, 1f));
        TMP_Text deathTitle = Text("DeathTitle", deathPage, style, 110f, new Vector2(1200f, 260f));
        deathTitle.color = new Color(0.9f, 0.07f, 0.06f, 1f);
        deathTitle.text = Loc.T("YOU DIED");

        roundPage = FullPage("RoundPage", transform, Color.black);
        roundContent = Rect("RoundContent", roundPage, new Vector2(800f, NumberTravel));
        roundContentAlpha = roundContent.gameObject.AddComponent<CanvasGroup>();
        roundLabel = Text("RoundLabel", roundContent, style, 100f, new Vector2(380f, NumberTravel));
        roundLabel.alignment = TextAlignmentOptions.MidlineRight;
        roundLabel.rectTransform.anchoredPosition = new Vector2(-160f, 0f);

        RectTransform viewport = Rect("NumberViewport", roundContent, new Vector2(330f, NumberTravel));
        viewport.anchoredPosition = new Vector2(205f, 0f);
        viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
        previousNumber = Text("PreviousRound", viewport, style, 180f, new Vector2(330f, NumberTravel));
        nextNumber = Text("NextRound", viewport, style, 180f, new Vector2(330f, NumberTravel));
        previousNumber.fontStyle = nextNumber.fontStyle = FontStyles.Bold;
        previousNumber.alignment = nextNumber.alignment = TextAlignmentOptions.MidlineLeft;
        nextNumber.rectTransform.anchoredPosition = new Vector2(0f, NumberTravel);
        roundPage.gameObject.SetActive(false);
    }

    public IEnumerator Play(int currentRound, int upcomingRound, float deathHoldTime)
    {
        // The first page deliberately contains no counter or next-life subtitle.
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, deathHoldTime));

        roundLabel.text = Loc.T("LOOP");
        previousNumber.text = currentRound.ToString(CultureInfo.InvariantCulture);
        nextNumber.text = upcomingRound.ToString(CultureInfo.InvariantCulture);
        Canvas.ForceUpdateCanvases();
        roundPage.anchoredPosition = Vector2.down * canvasRect.rect.height;
        roundPage.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < PageSlideTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / PageSlideTime);
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            roundPage.anchoredPosition = Vector2.down * canvasRect.rect.height * (1f - ease);
            yield return null;
        }
        roundPage.anchoredPosition = Vector2.zero;
        deathPage.gameObject.SetActive(false);
        yield return new WaitForSecondsRealtime(RoundHoldTime);

        elapsed = 0f;
        while (elapsed < NumberSlideTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / NumberSlideTime);
            float travel = t * t * NumberTravel;
            previousNumber.rectTransform.anchoredPosition = new Vector2(0f, -travel);
            nextNumber.rectTransform.anchoredPosition = new Vector2(0f, NumberTravel - travel);
            yield return null;
        }
        previousNumber.gameObject.SetActive(false);
        nextNumber.rectTransform.anchoredPosition = Vector2.zero;

        // A screen-space canvas is unaffected by the world camera, so shake the
        // title too; the impact remains visible against the opaque round page.
        CameraShake2D.Shake(ImpactTime, 0.4f);
        elapsed = 0f;
        while (elapsed < ImpactTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / ImpactTime);
            float decay = (1f - t) * (1f - t);
            roundContent.anchoredPosition = new Vector2(Mathf.Sin(elapsed * 95f) * 28f,
                Mathf.Cos(elapsed * 119f) * 22f) * decay;
            roundContent.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(elapsed * 81f) * 1.5f * decay);
            nextNumber.rectTransform.localScale = Vector3.one * (1f + 0.12f * decay);
            yield return null;
        }
        roundContent.anchoredPosition = Vector2.zero;
        roundContent.localRotation = Quaternion.identity;
        nextNumber.rectTransform.localScale = Vector3.one;
        yield return new WaitForSecondsRealtime(0.65f);

        elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.unscaledDeltaTime;
            roundContentAlpha.alpha = 1f - Mathf.Clamp01(elapsed / 0.3f);
            yield return null;
        }
        roundContentAlpha.alpha = 0f;
    }

    public void KeepAcrossReload()
    {
        transform.SetParent(null, false);
        DontDestroyOnLoad(gameObject);
    }

    public IEnumerator RevealGameplay()
    {
        // The opaque cover survives loading: no flash of gameplay before the new life is ready.
        yield return new WaitForSecondsRealtime(0.2f);
        float elapsed = 0f;
        while (elapsed < 0.8f)
        {
            elapsed += Time.unscaledDeltaTime;
            cover.alpha = 1f - Mathf.Clamp01(elapsed / 0.8f);
            yield return null;
        }
        cover.alpha = 0f;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        return rt;
    }

    private static RectTransform FullPage(string name, Transform parent, Color color)
    {
        RectTransform rt = Rect(name, parent, Vector2.zero);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var image = rt.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = color;
        image.raycastTarget = true;
        return rt;
    }

    private static TMP_Text Text(string name, Transform parent, TMP_Text style, float size, Vector2 bounds)
    {
        RectTransform rt = Rect(name, parent, bounds);
        var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = style != null ? style.font : TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Midline;
        text.color = new Color(0.96f, 0.94f, 0.9f, 1f);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        return text;
    }
}
