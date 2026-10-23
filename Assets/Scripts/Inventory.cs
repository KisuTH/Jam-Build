using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InventoryItem
{
    public string id;
    public string displayName;
    public Sprite icon;

    public InventoryItem(string id, string displayName, Sprite icon)
    {
        this.id = id;
        this.displayName = displayName;
        this.icon = icon;
    }
}

/// <summary>
/// ระบบกระเป๋าไอเท็ม (Singleton) วางไว้ใน GameObject ว่างๆ ใน Scene
/// </summary>
public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    public event Action OnInventoryChanged;

    private readonly List<InventoryItem> items = new List<InventoryItem>();
    public IReadOnlyList<InventoryItem> Items => items;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void AddItem(InventoryItem item)
    {
        if (HasItem(item.id)) return; // ไม่เก็บซ้ำ
        items.Add(item);
        OnInventoryChanged?.Invoke();
    }

    public bool HasItem(string id)
    {
        return items.Exists(i => i.id == id);
    }

    public void RemoveItem(string id)
    {
        int removed = items.RemoveAll(i => i.id == id);
        if (removed > 0) OnInventoryChanged?.Invoke();
    }
}
