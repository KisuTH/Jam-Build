using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JamBuild.Options
{
    /// <summary>One saved text scale for menus, gameplay and runtime-created UI.</summary>
    public static class NotebookTextSettings
    {
        private const string PreferenceKey = "NotebookOptions.TextSize";
        private sealed class TextStyle
        {
            public float size, minimum, maximum;
            public int appliedChoice = -1;
        }

        private static readonly Dictionary<TMP_Text, TextStyle> styles = new Dictionary<TMP_Text, TextStyle>();
        private static readonly List<TMP_Text> destroyed = new List<TMP_Text>();
        public static int SelectedSize { get; private set; } = 1;
        public static float Scale => SelectedSize == 0 ? .9f : SelectedSize == 1 ? 1f : 1.15f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            styles.Clear();
            destroyed.Clear();
            SelectedSize = Mathf.Clamp(PlayerPrefs.GetInt(PreferenceKey, 1), 0, 2);
            // Enter Play Mode can preserve static subscriptions when domain reload is disabled.
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ApplyAll();
        private static void OnSceneUnloaded(Scene scene) => RemoveDestroyedTexts();

        public static void SetSize(int choice)
        {
            SelectedSize = Mathf.Clamp(choice, 0, 2);
            PlayerPrefs.SetInt(PreferenceKey, SelectedSize);
            PlayerPrefs.Save();
            ApplyAll();
        }

        public static void ApplyAll()
        {
            RemoveDestroyedTexts();
            // Include hidden panels and DontDestroyOnLoad UI, but never edit prefab assets.
            foreach (TMP_Text text in Resources.FindObjectsOfTypeAll<TMP_Text>()) Apply(text);
        }

        public static void Apply(TMP_Text text)
        {
            if (!Application.isPlaying || text == null || !text.gameObject.scene.IsValid()) return;
            if (!styles.TryGetValue(text, out TextStyle style))
            {
                style = new TextStyle { size = text.fontSize, minimum = text.fontSizeMin, maximum = text.fontSizeMax };
                styles.Add(text, style);
            }
            if (style.appliedChoice == SelectedSize) return;
            style.appliedChoice = SelectedSize;
            // Always use the authored size so reopening Settings cannot compound the scale.
            text.fontSize = style.size * Scale;
            text.fontSizeMin = style.minimum * Scale;
            text.fontSizeMax = style.maximum * Scale;
        }

        private static void RemoveDestroyedTexts()
        {
            destroyed.Clear();
            foreach (var pair in styles) if (pair.Key == null) destroyed.Add(pair.Key);
            foreach (TMP_Text text in destroyed) styles.Remove(text);
            destroyed.Clear();
        }
    }
}
