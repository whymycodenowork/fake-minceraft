using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public GameObject main;
    public GameObject settings;
    public GameObject loadGame;
    public GameObject newGame;
    public GameObject back;
    void Start()
    {
        settings.SetActive(false);
        loadGame.SetActive(false);
        newGame.SetActive(false);
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
        loadGame.SetActive(false);
        newGame.SetActive(false);
        main.SetActive(true);
    }

    public void NewGame()
    {
        main.SetActive(false);
        newGame.SetActive(true);
    }

    public void LoadGame()
    {
        main.SetActive(false);
        loadGame.SetActive(true);
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
