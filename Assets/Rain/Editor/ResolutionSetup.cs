// Menu: Tools > Point and Click > Resolution > 1920x1080 (Full HD)  /  2560x1440 (QHD)
//
// Does two things:
//   1. Game view: adds a FIXED size (1920x1080 or 2560x1440) and selects it, so what you see while testing is that size.
//   2. Player Settings: sets the default window size of a build to that size, windowed and resizable.
// The UI (Canvas Scaler, reference 1920x1080) and the camera (Orthographic Size 5) are the same on both sizes
// (both are 16:9), so the game looks the same, only sharper at 2560x1440.
// Note: step 2 changes ProjectSettings/ProjectSettings.asset (it is shared with the team).
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class ResolutionSetup
{
    [MenuItem("Tools/Point and Click/Resolution/1920x1080 (Full HD)")]
    public static void FullHD() { Apply(1920, 1080); }

    [MenuItem("Tools/Point and Click/Resolution/2560x1440 (QHD)")]
    public static void QHD() { Apply(2560, 1440); }

    private static void Apply(int w, int h)
    {
        // Build settings
        PlayerSettings.defaultIsNativeResolution = false;
        PlayerSettings.defaultScreenWidth = w;
        PlayerSettings.defaultScreenHeight = h;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;

        // Game view (the part that can differ between Unity versions: if it fails, pick the size by hand)
        bool ok = TrySetGameView(w, h);

        Debug.Log("Resolution set to " + w + "x" + h + " (build window size). " +
                  (ok ? "Game view switched too." : "The Game view could not be switched by script: choose it in the Game tab's size dropdown."));
    }

    private static bool TrySetGameView(int w, int h)
    {
        try
        {
            Type sizesType = Type.GetType("UnityEditor.GameViewSizes,UnityEditor");
            Type singletonType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            object instance = singletonType.GetProperty("instance").GetValue(null, null);
            object group = sizesType.GetMethod("GetGroup").Invoke(instance, new object[] { GameViewSizeGroupType.Standalone });
            Type groupType = group.GetType();

            int count = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
            MethodInfo getSize = groupType.GetMethod("GetGameViewSize");

            // Is the size already in the list?
            int index = -1;
            for (int i = 0; i < count; i++)
            {
                object size = getSize.Invoke(group, new object[] { i });
                Type sizeType = size.GetType();
                int sw = (int)sizeType.GetProperty("width").GetValue(size, null);
                int sh = (int)sizeType.GetProperty("height").GetValue(size, null);
                string kind = sizeType.GetProperty("sizeType").GetValue(size, null).ToString();
                if (sw == w && sh == h && kind == "FixedResolution") { index = i; break; }
            }

            // No: add it
            if (index < 0)
            {
                Type gvSizeType = Type.GetType("UnityEditor.GameViewSize,UnityEditor");
                Type gvKindType = Type.GetType("UnityEditor.GameViewSizeType,UnityEditor");
                object fixedKind = Enum.Parse(gvKindType, "FixedResolution");
                object newSize = Activator.CreateInstance(gvSizeType, new object[] { fixedKind, w, h, w + "x" + h });
                groupType.GetMethod("AddCustomSize").Invoke(group, new object[] { newSize });
                index = count;
            }

            // Select it in the Game view
            Type gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
            EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
            PropertyInfo selected = gameViewType.GetProperty("selectedSizeIndex",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            selected.SetValue(gameView, index, null);
            gameView.Repaint();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("Resolution: could not change the Game view automatically (" + e.Message + ").");
            return false;
        }
    }
}
