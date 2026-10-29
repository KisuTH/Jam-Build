using JamBuild.Options;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace JamBuild.Pause
{
    [DisallowMultipleComponent]
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private NotebookOptions settings;
        [SerializeField] private UnityEngine.UI.Button pauseButton;
        [SerializeField] private UnityEngine.UI.Button resumeButton;
        [SerializeField] private UnityEngine.UI.Button settingsButton;
        [SerializeField] private UnityEngine.UI.Button mainMenuButton;
        [SerializeField] private UnityEngine.UI.Button settingsBackButton;
        [SerializeField] private UnityEngine.UI.Selectable settingsFirstSelection;
        [SerializeField] private string mainMenuScene = "MainScene";

        private bool configured;
        private float previousTimeScale;
        private bool previousAudioPause;
        private bool previousCursorVisible;
        private CursorLockMode previousCursorLock;

        public bool IsPaused { get; private set; }
        public bool IsSettingsOpen => IsPaused && settings != null && settings.gameObject.activeSelf;

        private void Awake()
        {
            configured = pausePanel != null && settings != null && pauseButton != null &&
                resumeButton != null && settingsButton != null && mainMenuButton != null &&
                settingsBackButton != null;
            if (!configured)
            {
                Debug.LogError("Pause menu references are incomplete.", this);
                enabled = false;
                return;
            }
            HidePanels();
        }

        private void OnEnable()
        {
            if (!configured) return;
            pauseButton.onClick.AddListener(PauseGame);
            resumeButton.onClick.AddListener(ResumeGame);
            settingsButton.onClick.AddListener(OpenSettings);
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
            settingsBackButton.onClick.AddListener(CloseSettings);
            settings.BackRequested += CloseSettings;
        }

        private void OnDisable()
        {
            if (!configured) return;
            pauseButton.onClick.RemoveListener(PauseGame);
            resumeButton.onClick.RemoveListener(ResumeGame);
            settingsButton.onClick.RemoveListener(OpenSettings);
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
            settingsBackButton.onClick.RemoveListener(CloseSettings);
            settings.BackRequested -= CloseSettings;
            RestoreGame();
            HidePanels();
        }

        private void Update()
        {
            bool pressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            pressed |= Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;
            if (!pressed) return;
            // The settings screen owns Escape while cancelling a key rebind, regardless of Update order.
            if (IsSettingsOpen && settings.IsHandlingRebindInput) return;
            if (IsSettingsOpen) CloseSettings();
            else if (IsPaused) ResumeGame();
            else PauseGame();
        }

        public void PauseGame()
        {
            if (!configured || !isActiveAndEnabled || IsPaused || Time.timeScale <= 0f) return;
            if (LoopManager.Instance != null && LoopManager.Instance.IsDying) return;
            previousTimeScale = Time.timeScale;
            previousAudioPause = AudioListener.pause;
            previousCursorVisible = Cursor.visible;
            previousCursorLock = Cursor.lockState;
            IsPaused = true;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            pauseButton.gameObject.SetActive(false);
            pausePanel.SetActive(true);
            Select(resumeButton.gameObject);
        }

        public void ResumeGame()
        {
            if (!IsPaused) return;
            RestoreGame();
            HidePanels();
            Select(null);
        }

        public void OpenSettings()
        {
            if (!IsPaused || IsSettingsOpen) return;
            pausePanel.SetActive(false);
            settings.gameObject.SetActive(true);
            Select(settingsFirstSelection != null ? settingsFirstSelection.gameObject : settingsBackButton.gameObject);
        }

        public void CloseSettings()
        {
            if (!IsPaused || !IsSettingsOpen) return;
            settings.gameObject.SetActive(false);
            pausePanel.SetActive(true);
            Select(settingsButton.gameObject);
        }

        public void ReturnToMainMenu()
        {
            if (!IsPaused) return;
            if (string.IsNullOrWhiteSpace(mainMenuScene) || !Application.CanStreamedLevelBeLoaded(mainMenuScene))
            {
                Debug.LogError("The main menu scene must be enabled in the build scene list: " + mainMenuScene, this);
                return;
            }
            RestoreGame();
            HidePanels();
            Select(null);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene(mainMenuScene);
        }

        private void RestoreGame()
        {
            if (!IsPaused) return;
            IsPaused = false;
            Time.timeScale = previousTimeScale;
            AudioListener.pause = previousAudioPause;
            Cursor.lockState = previousCursorLock;
            Cursor.visible = previousCursorVisible;
        }

        private void HidePanels()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (settings != null) settings.gameObject.SetActive(false);
            if (pauseButton != null) pauseButton.gameObject.SetActive(true);
        }

        private static void Select(GameObject target)
        {
            if (EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(null);
            if (target != null) EventSystem.current.SetSelectedGameObject(target);
        }
    }
}
