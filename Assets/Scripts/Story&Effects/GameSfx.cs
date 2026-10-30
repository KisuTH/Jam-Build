using UnityEngine;

/// <summary>
/// Shared sound effects. Put it on one object in the scene (Tools > Rain > Setup Game Sfx makes it).
/// "Tag Sounds": for each Unity Tag, the sound played when an item with that tag is picked up
/// (e.g. tag "Document" -> paper sound, tag "Key" -> key sound). Items with no matching tag use "Default Pickup Sound".
/// GameSfx.Play(clip) plays any clip once, at full volume, on every screen.
/// </summary>
public class GameSfx : MonoBehaviour
{
    public static GameSfx Instance { get; private set; }

    [System.Serializable]
    public class TagSound
    {
        [Tooltip("The Unity Tag of the item (set it in the item's Inspector, top left).")] public string tag;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    [Header("Item pickup")]
    public TagSound[] tagSounds = new TagSound[0];
    [Tooltip("Used when the item's tag is not in the list above.")]
    public AudioClip defaultPickupSound;
    [Range(0f, 1f)] public float defaultPickupVolume = 1f;

    private void Awake() { Instance = this; }
    private void OnDestroy() { if (Instance == this) Instance = null; }

    /// <summary>Plays the pickup sound for an object, chosen by its Tag.</summary>
    public void PlayPickup(GameObject item)
    {
        if (item != null && tagSounds != null)
            foreach (TagSound ts in tagSounds)
                if (ts != null && !string.IsNullOrEmpty(ts.tag) && item.CompareTag(ts.tag)) { Play(ts.clip, ts.volume); return; }
        Play(defaultPickupSound, defaultPickupVolume);
    }

    /// <summary>Plays the sound that belongs to a Tag name (e.g. "Document").</summary>
    public void PlayByTag(string tagName)
    {
        if (tagSounds != null)
            foreach (TagSound ts in tagSounds)
                if (ts != null && ts.tag == tagName) { Play(ts.clip, ts.volume); return; }
        Play(defaultPickupSound, defaultPickupVolume);
    }

    /// <summary>Play a clip once (2D). Safe to call with a null clip.</summary>
    public static void Play(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        Camera cam = Camera.main;
        Vector3 pos = cam != null ? cam.transform.position : Vector3.zero;
        AudioSource.PlayClipAtPoint(clip, pos, Mathf.Clamp01(volume));
    }
}
