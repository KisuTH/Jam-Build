using UnityEngine;
using UnityEngine.EventSystems;

public class BagButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public UnityEngine.UI.Image bagImage;
    public Sprite closedBag;
    public Sprite openedBag;
    public GameObject hoverBorder;
    public GameObject inventoryPanel;

    private UnityEngine.UI.Button button;
    private BagIconOutline outline;
    private bool ownsClick;

    private void Awake()
    {
        button = GetComponent<UnityEngine.UI.Button>();
        if (bagImage == null) bagImage = GetComponent<UnityEngine.UI.Image>();
        outline = GetComponent<BagIconOutline>();
        if (hoverBorder != null) hoverBorder.SetActive(false);
        SetHovered(false);
    }

    private void Start()
    {
        // InventoryUI already handles this button and the I key. Register only
        // when this component is being used on its own, avoiding a double toggle.
        InventoryUI inventory = GetComponentInParent<InventoryUI>();
        ownsClick = inventory == null || !inventory.ControlsBagButton(button);
        if (ownsClick)
        {
            if (inventoryPanel != null) inventoryPanel.SetActive(false);
            if (button != null) button.onClick.AddListener(ToggleBag);
        }
        RefreshIcon();
    }

    private void LateUpdate()
    {
        // Also follow keyboard toggles and other code that closes the panel.
        RefreshIcon();
    }

    private void OnDisable()
    {
        SetHovered(false);
    }

    private void OnDestroy()
    {
        if (ownsClick && button != null) button.onClick.RemoveListener(ToggleBag);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetHovered(button != null && button.IsInteractable());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHovered(false);
    }

    private void SetHovered(bool hovered)
    {
        if (outline != null) outline.enabled = hovered;
    }

    public void RefreshIcon()
    {
        if (bagImage == null) return;
        Sprite sprite = inventoryPanel != null && inventoryPanel.activeSelf ? openedBag : closedBag;
        if (sprite != null && bagImage.sprite != sprite) bagImage.sprite = sprite;
    }

    private void ToggleBag()
    {
        if (inventoryPanel == null) return;
        inventoryPanel.SetActive(!inventoryPanel.activeSelf);
        RefreshIcon();
    }
}
