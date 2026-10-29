using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Remembers what has happened in the story, as simple named "flags".
/// Example: "got_old_key", "family_portrait", "heard_phone".
/// A flag is either ON (it happened) or not set. Later features (locked doors, scare events,
/// unlocking rooms) just ask: "is this flag on?"
///
/// Put ONE of these in the scene (e.g. on the "GameSystems" object).
/// </summary>
public class StoryFlags : MonoBehaviour
{
    public static StoryFlags Instance { get; private set; }

    /// <summary>Fires once when a flag turns on for the first time. The string is the flag name.</summary>
    public event Action<string> OnFlagSet;

    private readonly HashSet<string> flags = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>True if the flag is on.</summary>
    public bool Has(string flag)
    {
        return !string.IsNullOrEmpty(flag) && flags.Contains(flag);
    }

    /// <summary>Turns a flag on. Also usable from a UnityEvent in the Inspector (type the flag name).</summary>
    public void Set(string flag)
    {
        if (string.IsNullOrEmpty(flag)) return;
        if (flags.Add(flag)) OnFlagSet?.Invoke(flag);
    }
}
