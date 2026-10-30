using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ClueEntry
{
    public string id;     // unique name, e.g. "family_portrait"
    public string title;  // shown in bold
    public string text;   // the note
    public Sprite image;  // optional: a document picture, opened by the "View document" button

    public ClueEntry(string id, string title, string text, Sprite image = null)
    {
        this.id = id;
        this.title = title;
        this.text = text;
        this.image = image;
    }
}

/// <summary>
/// The player's case book. Clues are added automatically when the player inspects
/// something with a Clue Id, or picks up an item. Put ONE in the scene (on "GameSystems").
/// </summary>
public class CaseNotebook : MonoBehaviour
{
    public static CaseNotebook Instance { get; private set; }

    /// <summary>Fires whenever a clue is added.</summary>
    public event Action OnChanged;

    private readonly List<ClueEntry> entries = new List<ClueEntry>();
    public IReadOnlyList<ClueEntry> Entries => entries;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public bool HasClue(string id)
    {
        return entries.Exists(e => e.id == id);
    }

    /// <summary>Adds a clue. Returns false if it has no id or was already added.</summary>
    public bool AddClue(string id, string title, string text, Sprite image = null)
    {
        if (string.IsNullOrEmpty(id) || HasClue(id)) return false;

        entries.Add(new ClueEntry(id, title, text, image));
        OnChanged?.Invoke();
        return true;
    }
}
