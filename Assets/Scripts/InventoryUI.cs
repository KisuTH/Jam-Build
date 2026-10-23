using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// แสดงไอเท็มในกระเป๋าเป็นแถบไอคอน
/// slotContainer = Panel ที่มี Horizontal Layout Group
/// slotPrefab = Prefab ที่มี Image (ไอคอน) อย่างเดียวก็พอ
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotPrefab;

    private void OnEnable()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged -= Refresh;
    }

    private void Refresh()
    {
        if (Inventory.Instance == null) return;

        foreach (Transform child in slotContainer)
            Destroy(child.gameObject);

        foreach (var item in Inventory.Instance.Items)
        {
            GameObject slot = Instantiate(slotPrefab, slotContainer);
            Image img = slot.GetComponentInChildren<Image>();
            if (img != null) img.sprite = item.icon;
        }
    }
}
