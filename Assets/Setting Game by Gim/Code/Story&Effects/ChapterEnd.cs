using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ends the chapter: fades the screen to black, shows a message, and (if a scene name is set)
/// loads the next scene. Hook Finish() into an Interactable2D "On Interact" event (e.g. the exit door).
/// The next scene must be added in File > Build Profiles (Build Settings).
/// </summary>
public class ChapterEnd : MonoBehaviour
{
    [Tooltip("Name of the next scene. Leave empty to just fade out and stay.")]
    public string nextSceneName = "";
    [TextArea] public string endMessage = "To be continued...";
    public float fadeDuration = 2f;

    private bool finished;

    public void Finish()
    {
        if (finished) return;
        finished = true;
        StartCoroutine(FinishRoutine());
    }

    private IEnumerator FinishRoutine()
    {
        if (ScreenFlash.Instance != null)
            ScreenFlash.Instance.FadeToColor(Color.black, fadeDuration);

        if (DialogueUI.Instance != null)
            DialogueUI.Instance.Show(endMessage);

        yield return new WaitForSeconds(fadeDuration + 1f);

        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }
}
