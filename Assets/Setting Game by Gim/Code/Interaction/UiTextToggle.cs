using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Setting: hide / show the TEXT of the HUD (the words on the Bag, Case Book and Hint buttons and the
/// "A/D = walk | W = jump ..." line). The icons stay. Press T or click the small "Aa" button (bottom right).
/// The choice is remembered (PlayerPrefs). Creates itself, nothing to set up in the scene.
/// </summary>
public class UiTextToggle : MonoBehaviour
{
    private const string PrefKey = "ui_text_hidden";

    private bool hidden;
    private float nextScan;
    private TMP_Text buttonLabel;
    private Image buttonImage;
    private readonly List<TMP_Text> hudTexts = new List<TMP_Text>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (Object.FindAnyObjectByType<UiTextToggle>() != null) return;
        GameObject go = new GameObject("UiTextToggle");
        go.AddComponent<UiTextToggle>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        hidden = PlayerPrefs.GetInt(PrefKey, 0) == 1;
        BuildButton();
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.tKey.wasPressedThisFrame) Toggle();

        // HUD objects appear / reload with scenes: look again now and then
        if (Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime + 0.5f;
            Scan();
        }
        Apply();
    }

    private void Toggle()
    {
        hidden = !hidden;
        PlayerPrefs.SetInt(PrefKey, hidden ? 1 : 0);
        PlayerPrefs.Save();
        Apply();
    }

    private void Scan()
    {
        hudTexts.Clear();
        foreach (TMP_Text t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include))
        {
            string n = t.gameObject.name;
            string p = t.transform.parent != null ? t.transform.parent.name : "";
            bool isButtonLabel = n == "Label" && (p == "BagButton" || p == "CaseBookButton" || p == "HintButton");
            if (isButtonLabel || n == "HintText") hudTexts.Add(t);
        }
    }

    private void Apply()
    {
        foreach (TMP_Text t in hudTexts)
            if (t != null && t.enabled == hidden) t.enabled = !hidden;
        if (buttonLabel != null) buttonLabel.text = hidden ? "Aa" : "Aa";
        if (buttonImage != null) buttonImage.color = hidden ? new Color(0.35f, 0.35f, 0.35f, 0.85f) : new Color(0.1f, 0.1f, 0.1f, 0.85f);
    }

    private void BuildButton()
    {
        GameObject cv = new GameObject("UiTextToggleCanvas", typeof(Canvas), typeof(GraphicRaycaster));
        cv.transform.SetParent(transform, false);
        Canvas c = cv.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 95;

        GameObject b = new GameObject("Aa", typeof(RectTransform), typeof(Image), typeof(Button));
        b.transform.SetParent(cv.transform, false);
        RectTransform rt = (RectTransform)b.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-12f, 12f);
        rt.sizeDelta = new Vector2(44f, 30f);
        buttonImage = b.GetComponent<Image>();
        b.GetComponent<Button>().onClick.AddListener(Toggle);

        GameObject l = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        l.transform.SetParent(b.transform, false);
        RectTransform lr = (RectTransform)l.transform;
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.offsetMin = lr.offsetMax = Vector2.zero;
        buttonLabel = l.GetComponent<TextMeshProUGUI>();
        buttonLabel.text = "Aa";
        buttonLabel.fontSize = 16;
        buttonLabel.alignment = TextAlignmentOptions.Center;
        buttonLabel.color = Color.white;
        buttonLabel.raycastTarget = false;
        // never touched by the toggle itself (its name is not in the list)

        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }
}
