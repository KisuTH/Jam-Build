using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InventoryItem
{
    public string id;
    public string displayName;
    public string description;
    public Sprite icon;

    public InventoryItem(string id, string displayName, string description, Sprite icon)
    {
        this.id = id;
        this.displayName = displayName;
        this.description = description;
        this.icon = icon;
    }
}

/// <summary>
/// Holds the items the player has collected. Put ONE in the scene.
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
        if (HasItem(item.id)) return;
        items.Add(item);
        OnInventoryChanged?.Invoke();
    }

    public bool HasItem(string id)
    {
        return items.Exists(i => i.id == id);
    }

    public void RemoveItem(string id)
    {
        if (items.RemoveAll(i => i.id == id) > 0)
            OnInventoryChanged?.Invoke();
    }
}
