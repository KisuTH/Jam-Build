using System;
using System.Linq;
using JamBuild.Options;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace JamBuild.Pause.Editor
{
    public static class PauseMenuBuilder
    {
        public const string PrefabPath = "Assets/UI/PauseMenu/PauseMenu.prefab";
        private static readonly Color Ink = new Color32(36, 36, 29, 255);
        private static readonly Color Paper = new Color32(216, 210, 173, 255);
        private static readonly Color Red = new Color32(168, 39, 32, 255);
        private static TMP_FontAsset font;

        [MenuItem("Tools/Pause Menu/Create in Current Scene")]
        public static void Install()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || string.IsNullOrEmpty(scene.path))
                throw new InvalidOperationException("Open a saved gameplay scene in Edit Mode first.");
            if (scene.GetRootGameObjects().Any(r => r.GetComponentInChildren<PauseMenu>(true) != null))
                throw new InvalidOperationException("This scene already has a PauseMenu. Edit the existing instance.");
            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existingPrefab != null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(existingPrefab, scene);
                Undo.RegisterCreatedObjectUndo(instance, "Add Pause Menu");
                EnsureEventSystem(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                Selection.activeGameObject = instance;
                return;
            }
            var notebookPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/NotebookOptions/NotebookOptions.prefab");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/NotebookOptions/Fonts/NotebookThai SDF.asset");
            if (notebookPrefab == null || font == null) throw new InvalidOperationException("Notebook options prefab or Thai font is missing.");

            var root = new GameObject("PauseMenu", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            root.layer = 5;
            Undo.RegisterCreatedObjectUndo(root, "Create Pause Menu");
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
            var controller = root.AddComponent<PauseMenu>();

            var pauseButton = Button(root.transform, "PauseButton", "II   PAUSE", 176, 62, Paper, Ink, 25);
            Corner((RectTransform)pauseButton.transform, new Vector2(1, 1), new Vector2(-36, -30));

            var overlay = Rect(root.transform, "PausePanel", Vector2.zero);
            Stretch(overlay);
            var dimmer = overlay.gameObject.AddComponent<UnityEngine.UI.Image>();
            dimmer.color = new Color(.035f, .045f, .035f, .78f);
            dimmer.raycastTarget = true;
            var card = Rect(overlay, "NotebookCard", new Vector2(590, 660));
            Shape(card, "Shadow", new Vector2(606, 676), new Vector2(10, -14), new Color(0, 0, 0, .27f));
            Shape(card, "Cover", new Vector2(600, 670), new Vector2(0, -5), new Color32(130, 125, 96, 255));
            Shape(card, "Paper", new Vector2(590, 660), Vector2.zero, Paper);
            Shape(card, "RedMargin", new Vector2(2, 594), new Vector2(-240, 0), new Color(Red.r, Red.g, Red.b, .35f));
            var title = Text(card, "Title", "PAUSED", new Vector2(450, 78), 55, Ink);
            title.rectTransform.anchoredPosition = new Vector2(10, 228);
            var subtitle = Text(card, "Subtitle", "หยุดพักเกม", new Vector2(450, 50), 27, Ink);
            subtitle.rectTransform.anchoredPosition = new Vector2(10, 170);
            Shape(card, "TitleUnderline", new Vector2(144, 4), new Vector2(10, 130), Red);
            var buttons = Rect(card, "Actions", new Vector2(436, 264));
            buttons.anchoredPosition = new Vector2(10, -32);
            var layout = buttons.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.spacing = 18;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var resume = Button(buttons, "ResumeButton", "เล่นต่อ  /  RESUME", 436, 76, Ink, Paper, 29);
            var settings = Button(buttons, "SettingsButton", "ตั้งค่า  /  SETTINGS", 436, 76, new Color32(187, 182, 146, 255), Ink, 29);
            var menu = Button(buttons, "MainMenuButton", "กลับเมนูหลัก  /  MAIN MENU", 436, 76, new Color32(187, 182, 146, 255), Red, 26);
            var hint = Text(card, "Hint", "ESC  /  เล่นต่อ", new Vector2(450, 42), 22, new Color(Ink.r, Ink.g, Ink.b, .75f));
            hint.rectTransform.anchoredPosition = new Vector2(10, -257);

            var notebook = (GameObject)PrefabUtility.InstantiatePrefab(notebookPrefab, scene);
            // Root Canvas transforms in the source prefab are driven by Unity and saved with zero scale.
            // Disable it before parenting, then give this nested canvas an explicit layout.
            notebook.SetActive(false);
            notebook.transform.SetParent(root.transform, false);
            notebook.name = "PauseSettings";
            notebook.transform.localScale = Vector3.one;
            ((RectTransform)notebook.transform).pivot = new Vector2(.5f, .5f);
            Stretch((RectTransform)notebook.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(notebook.transform);
            var notebookCanvas = notebook.GetComponent<Canvas>();
            notebookCanvas.overrideSorting = true;
            notebookCanvas.sortingOrder = 110;
            notebook.GetComponent<UnityEngine.UI.CanvasScaler>().enabled = false;
            var notebookController = notebook.GetComponent<NotebookOptions>();
            var notebookSO = new SerializedObject(notebookController);
            var bookBack = (UnityEngine.UI.Button)notebookSO.FindProperty("backButton").objectReferenceValue;
            bookBack.GetComponentInChildren<TextMeshProUGUI>(true).text = "BACK TO PAUSE";
            PrefabUtility.RecordPrefabInstancePropertyModifications(bookBack.GetComponentInChildren<TextMeshProUGUI>(true));
            var back = Button(notebook.transform, "BackToPauseButton", "กลับ  /  BACK", 226, 62, Paper, Ink, 25);
            Corner((RectTransform)back.transform, new Vector2(1, 1), new Vector2(-36, -30));

            var so = new SerializedObject(controller);
            Ref(so, "pausePanel", overlay.gameObject);
            Ref(so, "settings", notebookController);
            Ref(so, "pauseButton", pauseButton);
            Ref(so, "resumeButton", resume);
            Ref(so, "settingsButton", settings);
            Ref(so, "mainMenuButton", menu);
            Ref(so, "settingsBackButton", back);
            Ref(so, "settingsFirstSelection", notebookSO.FindProperty("pages").GetArrayElementAtIndex(0).FindPropertyRelative("tab").objectReferenceValue);
            so.ApplyModifiedPropertiesWithoutUndo();
            notebook.SetActive(false);
            overlay.gameObject.SetActive(false);
            EnsureEventSystem(scene);
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.AutomatedAction);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = root;
        }

        // Only used for the initial integration into this project's known scenes.
        public static void InstallInTestgame()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/Testgame.unity" || scene.isDirty)
                throw new InvalidOperationException("Testgame must be the active, saved scene before installation.");
            Install();
            var scenes = EditorBuildSettings.scenes.ToList();
            int index = scenes.FindIndex(s => s.path == scene.path);
            if (index < 0) scenes.Add(new EditorBuildSettingsScene(scene.path, true));
            else scenes[index].enabled = true;
            EditorBuildSettings.scenes = scenes.ToArray();
            EditorSceneManager.SaveScene(scene);

            var main = SceneManager.GetSceneByPath("Assets/Scenes/MainScene.unity");
            bool opened = !main.isLoaded;
            if (opened) main = EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity", OpenSceneMode.Additive);
            try
            {
                if (main.isDirty) throw new InvalidOperationException("MainScene has unsaved changes; start button was not modified.");
                var components = main.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MainMenu>(true)).ToArray();
                var starts = main.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UnityEngine.UI.Button>(true)).Where(b => b.name == "StartButton").ToArray();
                if (components.Length != 1 || starts.Length != 1) throw new InvalidOperationException("Expected one MainMenu controller and StartButton.");
                if (starts[0].onClick.GetPersistentEventCount() == 0)
                {
                    Undo.RecordObject(starts[0], "Connect Start Game");
                    UnityEventTools.AddPersistentListener(starts[0].onClick, components[0].StartGame);
                    EditorSceneManager.MarkSceneDirty(main);
                    EditorSceneManager.SaveScene(main);
                }
            }
            finally { if (opened) EditorSceneManager.CloseScene(main, true); }
            SceneManager.SetActiveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Pause menu installed in Testgame, with notebook settings and MainScene navigation.");
        }

        private static void EnsureEventSystem(Scene scene)
        {
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            SceneManager.MoveGameObjectToScene(go, scene);
            go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
        }

        private static UnityEngine.UI.Button Button(Transform parent, string name, string label, float width, float height, Color background, Color foreground, float size)
        {
            var rect = Rect(parent, name, new Vector2(width, height));
            var graphic = rect.gameObject.AddComponent<NotebookPaper>();
            graphic.Configure(NotebookPaper.Shape.Rounded, background, false, 8);
            graphic.raycastTarget = true;
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = graphic;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(.8f, .8f, .8f, 1);
            colors.fadeDuration = .08f;
            button.colors = colors;
            var layout = rect.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            layout.preferredHeight = height;
            var text = Text(rect, "Label", label, new Vector2(width - 24, height), size, foreground);
            Stretch(text.rectTransform, 12);
            return button;
        }

        private static TextMeshProUGUI Text(Transform parent, string name, string value, Vector2 size, float fontSize, Color color)
        {
            var rect = Rect(parent, name, size);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        private static void Shape(Transform parent, string name, Vector2 size, Vector2 position, Color color)
        {
            var rect = Rect(parent, name, size);
            rect.anchoredPosition = position;
            rect.gameObject.AddComponent<NotebookPaper>().Configure(NotebookPaper.Shape.Rounded, color, false, 12);
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            return rect;
        }

        private static void Corner(RectTransform rect, Vector2 anchor, Vector2 position)
        { rect.anchorMin = rect.anchorMax = rect.pivot = anchor; rect.anchoredPosition = position; }
        private static void Stretch(RectTransform rect, float margin = 0)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.one * margin; rect.offsetMax = Vector2.one * -margin; }
        private static void Ref(SerializedObject so, string field, UnityEngine.Object value)
        { so.FindProperty(field).objectReferenceValue = value; }
    }
}
