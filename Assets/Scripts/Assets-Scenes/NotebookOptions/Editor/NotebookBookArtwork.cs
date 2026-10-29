using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JamBuild.Options.Editor
{
    /// <summary>Maps the supplied book and painted buttons onto the existing book hierarchy.</summary>
    public static class NotebookBookArtwork
    {
        public const string TexturePath = "Assets/UI/NotebookOptions/Art/booook.png";
        private const string PrefabPath = "Assets/UI/NotebookOptions/NotebookOptions.prefab";
        private const float ArtScale = .9f;
        private const float SpineX = 974f;
        private const float PaperTop = 140f;
        private const float PaperBottom = 975f;
        private static readonly Vector2 ArtPosition = new Vector2(560f - SpineX * ArtScale, -160.4f);

        [MenuItem("Tools/Notebook Options/Apply Book Artwork")]
        public static void ApplyToSettingsAndPrefab()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Apply book artwork in Edit Mode.");
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/SettingScene.unity")
                throw new InvalidOperationException("Open SettingScene before applying artwork.");

            ConfigureImport();
            GameObject prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Apply(prefab);
                PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                var menu = root.GetComponent<NotebookOptions>();
                if (menu == null) continue;
                Undo.RegisterFullObjectHierarchyUndo(root, "Apply book artwork");
                Apply(root);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Book artwork applied to SettingScene and the shared NotebookOptions prefab.");
        }

        public static void Apply(GameObject root)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (texture == null) throw new InvalidOperationException("The book artwork is missing: " + TexturePath);
            Transform book = root.transform.Find("Book");
            Transform pages = book != null ? book.Find("Pages") : null;
            if (pages == null) throw new InvalidOperationException("Expected the existing Book/Pages hierarchy.");

            // Retain old decorative objects so existing scene/prefab references stay valid.
            foreach (string name in new[] { "BookShadow", "Cover", "PaperEdge0", "PaperEdge1", "PaperEdge2", "PaperEdge3" })
            {
                Transform item = book.Find(name);
                if (item != null) item.gameObject.SetActive(false);
            }
            // The book stays in place; the supplied post-it regions belong to the existing edge tabs.
            var artwork = Image(book, "BookArtwork", texture, ArtPosition + new Vector2(280, 0) * ArtScale,
                new Vector2(1405, 1080) * ArtScale, UV(280, 0, 1405, 1080));
            artwork.transform.SetAsFirstSibling();

            foreach (Transform child in pages)
            {
                bool right = child.name.EndsWith("_Right", StringComparison.Ordinal);
                if (!right && !child.name.EndsWith("_Left", StringComparison.Ordinal)) continue;
                var paper = child.GetComponent<NotebookPaper>();
                if (paper == null) continue;
                float xMin = right ? SpineX : 365f;
                float xMax = right ? 1581f : SpineX;
                var rect = (RectTransform)child;
                rect.sizeDelta = new Vector2((xMax - xMin) * ArtScale, (PaperBottom - PaperTop) * ArtScale);
                // Rectangles share the illustration's spine, with unchanged top-left child layout.
                rect.anchoredPosition = new Vector2(0, -1.35f);
                paper.SetPaperArtwork(texture, UV(xMin, PaperTop, xMax - xMin, PaperBottom - PaperTop));

                foreach (var graphic in child.GetComponentsInChildren<NotebookPaper>(true))
                {
                    if (graphic == paper) continue;
                    if (graphic.name == "Inner" || graphic.name == "BoxInner")
                        graphic.color = new Color32(216, 199, 157, 255);
                }
            }

            // Keep the painted spine stationary above the turning leaves.
            var binding = Image(book, "BookBinding", texture,
                ArtPosition + new Vector2(925f, PaperTop) * ArtScale,
                new Vector2(95f, PaperBottom - PaperTop) * ArtScale,
                UV(925f, PaperTop, 95f, PaperBottom - PaperTop));
            binding.transform.SetAsLastSibling();
            ApplyButtons(book.Find("Tabs"), texture);
        }

        private static void ApplyButtons(Transform tabs, Texture2D texture)
        {
            if (tabs == null) throw new InvalidOperationException("The existing Tabs hierarchy is missing.");
            string[] names = { "VIDEO", "SOUND", "CONTROL", "ACCESSIBILITY", "MENU" };
            float[] tops = { 267, 399, 530, 653, 835 };
            Material opaquePostit = PostitMaterial();
            for (int i = 0; i < names.Length; i++)
            {
                var tab = tabs.Find("Tab_" + names[i]) as RectTransform;
                if (tab == null) throw new InvalidOperationException("Missing tab: " + names[i]);
                var button = tab.GetComponent<UnityEngine.UI.Button>();
                var hitArea = tab.GetComponent<UnityEngine.UI.Image>();
                if (button == null || hitArea == null) throw new InvalidOperationException("Incomplete tab: " + names[i]);
                tab.anchorMin = tab.anchorMax = new Vector2(0, 1);
                tab.pivot = new Vector2(1, .5f);
                tab.anchoredPosition = new Vector2(8, -133 - i * 99);
                tab.sizeDelta = new Vector2(i == 0 ? 148 : 62, 46);
                tab.localScale = Vector3.one;
                button.transition = UnityEngine.UI.Selectable.Transition.None;
                button.targetGraphic = hitArea;
                hitArea.sprite = PostitSprite(texture, names[i], tops[i]);
                hitArea.overrideSprite = null;
                hitArea.type = UnityEngine.UI.Image.Type.Simple;
                hitArea.preserveAspect = false;
                hitArea.color = Color.white;
                hitArea.material = opaquePostit;
                hitArea.raycastTarget = true;
                // Remove only the extra artwork children created by the previous revision.
                Transform duplicate = tab.Find("Artwork");
                if (duplicate != null) UnityEngine.Object.DestroyImmediate(duplicate.gameObject);
                var label = tab.Find("Label").GetComponent<TMPro.TextMeshProUGUI>();
                label.text = i == 0 ? names[i] : names[i].Substring(0, 2);
                label.fontStyle = TMPro.FontStyles.Normal;
                label.color = i >= 3 ? new Color32(255, 246, 221, 255) : new Color32(32, 31, 20, 255);
                label.raycastTarget = false;
            }
        }

        private static Material PostitMaterial()
        {
            const string path = "Assets/UI/NotebookOptions/Art/Postits/PostitOpaque.mat";
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/UI/NotebookOptions/Runtime/NotebookOpaquePostit.shader");
            if (shader == null || ShaderUtil.ShaderHasError(shader))
                throw new InvalidOperationException("The opaque post-it shader must compile before applying it.");
            if (!AssetDatabase.IsValidFolder("Assets/UI/NotebookOptions/Art/Postits"))
                AssetDatabase.CreateFolder("Assets/UI/NotebookOptions/Art", "Postits");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = "PostitOpaque" };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetColor("_Color", new Color(.92f, .92f, .92f, 1));
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Sprite PostitSprite(Texture2D texture, string name, float top)
        {
            // Native Sprite assets reference regions of the unchanged PNG; no new bitmap is drawn.
            const string folder = "Assets/UI/NotebookOptions/Art/Postits";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/UI/NotebookOptions/Art", "Postits");
            string path = folder + "/Postit_" + name + ".asset";
            var rect = new Rect(47, 1080 - top - 74, 213, 74);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                if (sprite.texture != texture || sprite.rect != rect)
                    throw new InvalidOperationException("Unexpected existing post-it asset: " + path);
                return sprite;
            }
            sprite = Sprite.Create(texture, rect, new Vector2(1, .5f), 100, 0, SpriteMeshType.FullRect);
            sprite.name = "Postit_" + name;
            AssetDatabase.CreateAsset(sprite, path);
            return sprite;
        }

        private static void ConfigureImport()
        {
            AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        private static Rect UV(float x, float y, float w, float h)
            => new Rect(x / 1685f, 1f - (y + h) / 1080f, w / 1685f, h / 1080f);

        private static UnityEngine.UI.RawImage Image(Transform parent, string name, Texture texture,
            Vector2 topLeft, Vector2 size, Rect uv)
        {
            Transform existing = parent.Find(name);
            var rect = existing != null ? (RectTransform)existing :
                new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.RawImage)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.gameObject.layer = parent.gameObject.layer;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(topLeft.x, -topLeft.y);
            rect.sizeDelta = size;
            var image = rect.GetComponent<UnityEngine.UI.RawImage>();
            image.texture = texture;
            image.uvRect = uv;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }
    }
}
