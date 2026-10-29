using System.Collections.Generic;
using UnityEngine;

namespace JamBuild.Options
{
    /// <summary>Display-mode choices and persisted dimensions shared by startup and the settings UI.</summary>
    public static class NotebookDisplaySettings
    {
        private const string Prefix = "NotebookOptions.";
        private static readonly Vector2Int[] LegacySizes =
        {
            new Vector2Int(1280,720), new Vector2Int(1600,900),
            new Vector2Int(1920,1080), new Vector2Int(2560,1440)
        };

        public static FullScreenMode ModeFromIndex(int index)
        {
            if (index == 0) return FullScreenMode.Windowed;
            if (index == 1) return FullScreenMode.FullScreenWindow;
            return Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor
                ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.FullScreenWindow;
        }

        public static int ModeIndex(FullScreenMode mode)
            => mode == FullScreenMode.Windowed ? 0 : mode == FullScreenMode.ExclusiveFullScreen ? 2 : 1;

        public static Vector2Int[] AvailableResolutions(FullScreenMode mode)
        {
            var sizes = new List<Vector2Int>();
            foreach (var resolution in Screen.resolutions)
                Add(sizes, new Vector2Int(resolution.width, resolution.height));
            Vector2Int desktop = new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);
            if (sizes.Count == 0) Add(sizes, desktop);
            if (mode != FullScreenMode.ExclusiveFullScreen)
            {
                int maxWidth = desktop.x, maxHeight = desktop.y;
                foreach (var size in sizes) { maxWidth = Mathf.Max(maxWidth, size.x); maxHeight = Mathf.Max(maxHeight, size.y); }
                foreach (var size in LegacySizes)
                    if (size.x <= maxWidth && size.y <= maxHeight) Add(sizes, size);
                Add(sizes, new Vector2Int(Screen.width, Screen.height));
            }
            if (sizes.Count == 0) sizes.Add(new Vector2Int(1280,720));
            sizes.Sort((a,b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            return sizes.ToArray();
        }

        private static void Add(List<Vector2Int> sizes, Vector2Int size)
        { if (size.x > 0 && size.y > 0 && !sizes.Contains(size)) sizes.Add(size); }

        public static Vector2Int ClosestResolution(Vector2Int requested, FullScreenMode mode)
        {
            var choices = AvailableResolutions(mode);
            if (mode != FullScreenMode.ExclusiveFullScreen)
            {
                int maxWidth=320,maxHeight=180;
                foreach(var size in choices) {maxWidth=Mathf.Max(maxWidth,size.x);maxHeight=Mathf.Max(maxHeight,size.y);}
                return new Vector2Int(Mathf.Clamp(requested.x,320,maxWidth),Mathf.Clamp(requested.y,180,maxHeight));
            }
            Vector2Int best = choices[0];
            double bestDistance = double.MaxValue;
            foreach (var size in choices)
            {
                double dx = (double)size.x-requested.x, dy = (double)size.y-requested.y;
                double distance = dx*dx+dy*dy;
                if (distance < bestDistance) { best = size; bestDistance = distance; }
            }
            return best;
        }

        public static Vector2Int SavedResolution(Vector2Int fallback)
        {
            int width = PlayerPrefs.GetInt(Prefix+"ResolutionWidth",0);
            int height = PlayerPrefs.GetInt(Prefix+"ResolutionHeight",0);
            if (width > 0 && height > 0) return new Vector2Int(width,height);
            if (PlayerPrefs.HasKey(Prefix+"Resolution"))
                return LegacySizes[Mathf.Clamp(PlayerPrefs.GetInt(Prefix+"Resolution"),0,LegacySizes.Length-1)];
            return fallback;
        }

        public static void Store(Vector2Int size, FullScreenMode mode)
        {
            PlayerPrefs.SetInt(Prefix+"ResolutionWidth",size.x);
            PlayerPrefs.SetInt(Prefix+"ResolutionHeight",size.y);
            PlayerPrefs.SetInt(Prefix+"WindowMode",ModeIndex(mode));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RestoreAtStartup()
        {
            if (Application.isEditor) return;
            if (!PlayerPrefs.HasKey(Prefix+"WindowMode") && !PlayerPrefs.HasKey(Prefix+"Resolution") &&
                !PlayerPrefs.HasKey(Prefix+"ResolutionWidth")) return;
            var mode = ModeFromIndex(Mathf.Clamp(PlayerPrefs.GetInt(Prefix+"WindowMode",ModeIndex(Screen.fullScreenMode)),0,2));
            var size = ClosestResolution(SavedResolution(new Vector2Int(Screen.width,Screen.height)),mode);
            Screen.SetResolution(size.x,size.y,mode);
        }
    }
}
