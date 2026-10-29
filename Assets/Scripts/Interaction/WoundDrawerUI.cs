using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Close-up of the drawer in the dressing room (pictures: Assets/Resources/Drawer).
///   - before the wound-care puzzle is solved: the CLOSED drawer picture, "the drawer is shut".
///   - after the puzzle (story flag "wound_solved"): the OPEN drawer picture; click inside the drawer to take
///     the "Treatment report" (inventory + Case Book + flag "wound_report_taken").
/// Opened by DrawerSpot.Open (the builder wires it to each drawer in the rooms).
/// </summary>
public class WoundDrawerUI : MonoBehaviour
{
    private static WoundDrawerUI instance;
    private DrawerSpot spot;

    private GameObject root;
    private RawImage picture;
    private Texture2D closedTex, openTex;
    private GameObject hotspot;
    private Image itemImage;
    private readonly System.Collections.Generic.Dictionary<string, Sprite> cutouts = new System.Collections.Generic.Dictionary<string, Sprite>();
    private TMP_Text messageText;
    private readonly System.Collections.Generic.Dictionary<TMP_Text, string[]> bound =
        new System.Collections.Generic.Dictionary<TMP_Text, string[]>();

    public static void OpenStatic(DrawerSpot spot)
    {
        if (instance == null)
        {
            GameObject go = new GameObject("WoundDrawerUI");
            instance = go.AddComponent<WoundDrawerUI>();
        }
        instance.Open(spot);
    }

    private void OnDestroy() { Loc.Changed -= RefreshAll; if (instance == this) instance = null; }

    private bool Has(string flag) { return StoryFlags.Instance != null && StoryFlags.Instance.Has(flag); }

    private void Open(DrawerSpot s)
    {
        spot = s;
        if (root == null) Build();
        Show();
        root.SetActive(true);
        LoopManager.SetPlayerLocked(true);
        if (DialogueUI.Instance != null) DialogueUI.Instance.Hide();
    }

    // Draws the right state: closed / open with something inside / open and empty
    private void Show()
    {
        bool unlocked = string.IsNullOrEmpty(spot.requiredFlag) || Has(spot.requiredFlag);
        bool hasItem = !string.IsNullOrEmpty(spot.itemId);
        bool taken = Has(spot.TakenFlag);
        picture.texture = unlocked ? openTex : closedTex;
        hotspot.SetActive(unlocked && hasItem && !taken);
        if (hotspot.activeSelf)
        {
            itemImage.sprite = Cutout(spot.itemId, spot.GetIcon());
            itemImage.enabled = itemImage.sprite != null;
        }

        if (!unlocked)
            SetMessage(spot.lockedEnglish, spot.lockedThai);
        else if (hasItem && !taken)
            SetMessage("The drawer is open. There is something inside: " + spot.itemName + ". Click it to take it.",
                       "ลิ้นชักเปิดอยู่ มีบางอย่างอยู่ข้างใน: " + Loc.T(spot.itemName) + " คลิกเพื่อหยิบ");
        else if (hasItem)
            SetMessage("The drawer is empty now.", "ตอนนี้ลิ้นชักว่างเปล่าแล้ว");
        else
            SetMessage(spot.emptyEnglish, spot.emptyThai);
    }

    private void Take()
    {
        if (spot == null || Has(spot.TakenFlag) || string.IsNullOrEmpty(spot.itemId)) return;
        // Stored in English: the language system (Loc.cs) translates it when it is shown
        if (Inventory.Instance != null)
            Inventory.Instance.AddItem(new InventoryItem(spot.itemId, spot.itemName, spot.itemDescription, spot.GetIcon()));
        if (CaseNotebook.Instance != null) CaseNotebook.Instance.AddClue("item_" + spot.itemId, spot.itemName, spot.itemDescription);
        if (StoryFlags.Instance != null)
        {
            StoryFlags.Instance.Set(spot.TakenFlag);
            StoryFlags.Instance.Set("got_" + spot.itemId);
        }
        hotspot.SetActive(false);
        SetMessage("You took: " + spot.itemName, "หยิบแล้ว: " + Loc.T(spot.itemName));
    }

    public void Close()
    {
        if (root != null) root.SetActive(false);
        LoopManager.SetPlayerLocked(false);
    }

    private void Update()
    {
        if (root != null && root.activeSelf && Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    // Makes a copy of the item picture where the dark background is see-through (so it lies naturally in the drawer)
    private Sprite Cutout(string key, Sprite src)
    {
        if (src == null) return null;
        Sprite cached;
        if (cutouts.TryGetValue(key, out cached)) return cached;
        Sprite result = src;
        try
        {
            Texture2D tex = src.texture;
            Rect r = src.textureRect;
            RenderTexture rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(tex, rt);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D copy = new Texture2D((int)r.width, (int)r.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(r.x, r.y, r.width, r.height), 0, 0);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            Color32[] px = copy.GetPixels32();
            for (int i = 0; i < px.Length; i++)
            {
                float m = Mathf.Max(px[i].r, Mathf.Max(px[i].g, px[i].b)) / 255f;
                px[i].a = (byte)(Mathf.Clamp01((m - 0.06f) / 0.22f) * 255f);
            }
            copy.SetPixels32(px);
            copy.Apply();
            result = Sprite.Create(copy, new Rect(0, 0, copy.width, copy.height), new Vector2(0.5f, 0.5f), 100f);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("WoundDrawerUI: could not cut out the item picture: " + e.Message);
        }
        cutouts[key] = result;
        return result;
    }

    // ---------- texts that follow the language button ----------

    private static string L(string en, string th) { return Loc.Thai ? Loc.Shape(th) : en; }

    private void Bind(TMP_Text t, string en, string th)
    {
        if (t == null) return;
        bound[t] = new[] { en, th };
        t.text = L(en, th);
    }

    private void SetMessage(string en, string th) { Bind(messageText, en, th); }

    private void RefreshAll()
    {
        foreach (var kv in bound) if (kv.Key != null) kv.Key.text = L(kv.Value[0], kv.Value[1]);
    }

    // ---------- building the window ----------

    private void Build()
    {
        closedTex = Resources.Load<Texture2D>("Drawer/Drawer closed");
        openTex = Resources.Load<Texture2D>("Drawer/Drawer opened");
        if (closedTex == null || openTex == null)
            Debug.LogWarning("WoundDrawerUI: pictures not found in Assets/Resources/Drawer ('Drawer closed', 'Drawer opened').");

        if (EventSystem.current == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        root = new GameObject("WoundDrawerCanvas");
        root.transform.SetParent(transform, false);
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();

        Image dim = NewImage("Dim", root.transform, new Color(0.02f, 0.02f, 0.03f, 1f));
        Stretch(dim.rectTransform);

        // the picture (keeps its 16:9 shape, centred)
        GameObject picGO = new GameObject("Picture", typeof(RectTransform));
        picGO.transform.SetParent(root.transform, false);
        picture = picGO.AddComponent<RawImage>();
        picture.texture = closedTex;
        RectTransform pr = picture.rectTransform;
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.pivot = new Vector2(0.5f, 0.5f);
        pr.anchoredPosition = new Vector2(0f, 40f);
        pr.sizeDelta = new Vector2(1440f, 810f);

        // click area on the open drawer (normalised on the picture)
        GameObject hs = new GameObject("TakeReport", typeof(RectTransform));
        hs.transform.SetParent(picGO.transform, false);
        Image hsImg = hs.AddComponent<Image>();
        hsImg.color = new Color(1f, 0.95f, 0.6f, 0.05f);   // faint glow behind the item
        RectTransform hr = hsImg.rectTransform;
        hr.anchorMin = new Vector2(0.42f, 0.20f);
        hr.anchorMax = new Vector2(0.76f, 0.46f);
        hr.offsetMin = hr.offsetMax = Vector2.zero;
        Button b = hs.AddComponent<Button>();
        b.targetGraphic = hsImg;
        b.onClick.AddListener(Take);
        hotspot = hs;

        // the item lying in the drawer
        GameObject ig = new GameObject("Item", typeof(RectTransform));
        ig.transform.SetParent(hs.transform, false);
        itemImage = ig.AddComponent<Image>();
        itemImage.preserveAspect = true;
        itemImage.raycastTarget = false;
        Stretch(itemImage.rectTransform);
        itemImage.rectTransform.offsetMin = new Vector2(30f, 12f);
        itemImage.rectTransform.offsetMax = new Vector2(-30f, -12f);

        messageText = NewText("Message", root.transform, 34, TextAlignmentOptions.Center);
        messageText.color = new Color(0.95f, 0.9f, 0.7f);
        RectTransform mr = messageText.rectTransform;
        mr.anchorMin = new Vector2(0.1f, 0f);
        mr.anchorMax = new Vector2(0.9f, 0f);
        mr.pivot = new Vector2(0.5f, 0f);
        mr.anchoredPosition = new Vector2(0f, 40f);
        mr.sizeDelta = new Vector2(0f, 100f);

        // close button
        Image cb = NewImage("CloseButton", root.transform, new Color(0.35f, 0.15f, 0.15f));
        RectTransform cr = cb.rectTransform;
        cr.anchorMin = cr.anchorMax = new Vector2(1, 1);
        cr.pivot = new Vector2(1, 1);
        cr.anchoredPosition = new Vector2(-60, -40);
        cr.sizeDelta = new Vector2(260, 70);
        Button cbtn = cb.gameObject.AddComponent<Button>();
        cbtn.targetGraphic = cb;
        cbtn.onClick.AddListener(Close);
        TMP_Text cl = NewText("Label", cb.transform, 32, TextAlignmentOptions.Center);
        Stretch(cl.rectTransform);
        Bind(cl, "Close (Esc)", "ปิด (Esc)");

        Loc.Changed += RefreshAll;
        root.SetActive(false);
    }

    private static Image NewImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private static TMP_Text NewText(string name, Transform parent, float size, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size;
        t.alignment = align;
        t.color = new Color(0.92f, 0.92f, 0.92f);
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        return t;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
