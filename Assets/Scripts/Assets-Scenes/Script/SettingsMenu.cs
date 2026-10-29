
using UnityEngine;
using UnityEngine.SceneManagement;

public class SettingsMenu : MonoBehaviour
{
    public GameObject videoPanel;
    public GameObject soundPanel;
    public GameObject controlPanel;

    void Start()
    {
        ShowVideo();
    }

    public void ShowVideo()
    {
        OpenPanel(videoPanel);
    }

    public void ShowSound()
    {
        OpenPanel(soundPanel);
    }

    public void ShowControl()
    {
        OpenPanel(controlPanel);
    }

    void OpenPanel(GameObject selected)
    {
        videoPanel.SetActive(selected == videoPanel);
        soundPanel.SetActive(selected == soundPanel);
        controlPanel.SetActive(selected == controlPanel);
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene("MaingameScene");
    }
}
