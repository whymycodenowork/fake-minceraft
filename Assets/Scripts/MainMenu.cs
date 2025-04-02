using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject main;
    public GameObject settings;
    public GameObject singleplayer;
    public GameObject back;
    void Start()
    {
        settings.SetActive(false);
        singleplayer.SetActive(false);
    }

    void Update()
    {
        if (!main.activeSelf)
        {
            back.gameObject.SetActive(true);
        }
        else
        {
            back.gameObject.SetActive(false);
        }
    }

    public void Back()
    {
        settings.SetActive(false);
        singleplayer.SetActive(false);
        main.SetActive(true);
    }

    public void Singleplayer()
    {
        main.SetActive(false);
        singleplayer.SetActive(true);
    }

    public void Play()
    {
        main.SetActive(false);
        SceneManager.LoadScene("GameScene");
    }

    public void OpenSettings()
    {
        main.SetActive(false);
        settings.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
