using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows collected items as icons. Click an icon to read its description.
/// slotContainer: a Panel with a Horizontal Layout Group.
/// slotPrefab: a UI object with an Image (and a Button, if you want click-to-read).
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotPrefab;

    private void Start()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged -= Refresh;
    }

    private void Refresh()
    {
        if (Inventory.Instance == null) return;

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
