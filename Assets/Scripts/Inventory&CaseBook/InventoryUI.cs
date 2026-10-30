using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Shows collected items as icons. Click an icon to read its description.
///
/// Bag mode (optional): assign "bagButton" and "panel". The panel (the item slots) starts CLOSED;
/// clicking the bag button (or pressing I) opens / closes it.
/// Without those two fields it works as before: slots are always visible.
///
/// slotContainer: the object the slots are created under (a Panel with a Grid / Horizontal Layout Group).
/// slotPrefab: a UI object with an Image (and a Button, if you want click-to-read).
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotPrefab;

    [Header("Bag (optional)")]
    [Tooltip("The panel that is shown / hidden. Usually the same object as Slot Container.")]
    [SerializeField] private GameObject panel;
    [Tooltip("Click this (the bag picture) to open / close the panel.")]
    [SerializeField] private Button bagButton;
    [Tooltip("Optional: shows how many items you have, e.g. \"Bag (2)\".")]
    [SerializeField] private TMP_Text bagLabel;
    [SerializeField] private bool openWithIKey = true;

    private void Awake()
    {
        StretchOverCanvas();
    }

    // Also runs in the Editor (when the script reloads or a value changes), so the fix shows without pressing Play
    private void OnValidate()
    {
        StretchOverCanvas();
    }

    // In bag mode this object must cover the whole Canvas, so the bag button sits in the
    // screen's top-left corner (fixes scenes built before this was added).
    private void StretchOverCanvas()
    {
        RectTransform rt = transform as RectTransform;
        if (bagButton == null || rt == null || bagButton.GetComponent<BagButtonUI>() != null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void Start()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged += Refresh;

        if (bagButton != null) bagButton.onClick.AddListener(Toggle);
        if (panel != null) panel.SetActive(false);   // bag starts closed
        Refresh();
    }

    private void Update()
    {
        if (!openWithIKey || bagButton == null) return;
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.iKey.wasPressedThisFrame) Toggle();
    }

    private void OnDestroy()
    {
        if (bagButton != null) bagButton.onClick.RemoveListener(Toggle);
        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged -= Refresh;
    }

    public bool ControlsBagButton(UnityEngine.UI.Button button) => bagButton == button;

    public void Toggle()
    {
        if (panel == null) return;
        panel.SetActive(!panel.activeSelf);
        if (bagButton != null) bagButton.GetComponent<BagButtonUI>()?.RefreshIcon();
    }

    private void Refresh()
    {
        if (Inventory.Instance == null) return;

        if (bagLabel != null) bagLabel.text = "Bag [I] (" + Inventory.Instance.Items.Count + ")";

        foreach (Transform child in slotContainer)
            Destroy(child.gameObject);

        foreach (InventoryItem item in Inventory.Instance.Items)
        {
            GameObject slot = Instantiate(slotPrefab, slotContainer);

            Image img = slot.GetComponent<Image>();
            if (img == null) img = slot.GetComponentInChildren<Image>();
            if (img != null) img.sprite = item.icon;

            Button btn = slot.GetComponent<Button>();
            if (btn != null)
            {
                InventoryItem captured = item;
                btn.onClick.AddListener(() =>
                {
                    if (DialogueUI.Instance != null)
                        DialogueUI.Instance.Show(captured.displayName + ": " + captured.description);
                });
            }
        }
    }
}
