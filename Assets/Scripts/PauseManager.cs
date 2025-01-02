using UnityEngine;
using UnityEngine.SceneManagement; // For scene management if needed
using UnityEngine.UI; // For UI elements

public class PauseManager : MonoBehaviour
{
    public GameObject pauseMenuUI; // Reference to the pause menu UI
    public GameObject pauseMain;
    public GameObject pauseSettings;
    public bool isPaused = false; // Track whether the game is paused

    void Start()
    {
        Resume();
        pauseSettings.SetActive(false);
    }

    void Update()
    {
        // use this update cuz im not using it for much else
        SaveSystem.Update(Time.deltaTime);
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false); // Hide the pause menu
        Time.timeScale = 1f; // Resume game time
        isPaused = false; // Update the pause state
    }

    void Pause()
    {
        pauseMenuUI.SetActive(true); // Show the pause menu
        Time.timeScale = 0f; // Pause game time
        isPaused = true; // Update the pause state
    }

    public void ExitToMainMenu()
    {
        Time.timeScale = 1f; // Ensure game time resumes
        // Load the main menu or exit game
        SceneManager.LoadScene("Main Menu"); // Adjust scene name as needed
    }

    public void OpenSettings()
    {
        pauseMain.SetActive(false);
        pauseSettings.SetActive(true);
    }

    public void Back()
    {
        pauseSettings.SetActive(false);
        pauseMain.SetActive(true);
    }

    private void OnApplicationQuit()
    {
        SaveSystem.SaveAllChunksToDisk();
    }
}
