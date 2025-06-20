using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverManager : MonoBehaviour
{
    [Header("UI References")]
    public Button restartButton;
    public Button exitButton;
    public GameObject gameOverPanel; // Add this reference
    
    [Header("Scene Settings")]
    public string mainMenuSceneName = "MainMenu";
    public bool useCurrentScene = true;
    public string specificSceneName = "";
    
    [Header("Audio")]
    public AudioClip buttonClickSound;
    public AudioClip gameOverSound;
    
    private AudioSource audioSource;
    private bool gameOverActivated = false;
    
    void Start()
    {
        // Setup audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Setup button listeners
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }
        
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(ExitToMainMenu);
        }
        
        // Hide game over panel initially
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        // Don't pause game at start
        Time.timeScale = 1f;
    }
    
    // Method yang dipanggil dari HealthSystem
    public void ActivateGameOver()
    {
        if (!gameOverActivated)
        {
            gameOverActivated = true;
            
            // Show game over panel
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
            
            // Play game over sound
            if (audioSource != null && gameOverSound != null)
            {
                audioSource.PlayOneShot(gameOverSound);
            }
            
            // Pause game
            Time.timeScale = 0f;
            
            Debug.Log("Game Over Activated!");
        }
    }
    
    // Alternative method name for compatibility
    public void GameOver()
    {
        ActivateGameOver();
    }
    
    public void RestartGame()
    {
        PlayButtonSound();
        Time.timeScale = 1f;
        
        if (useCurrentScene)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        else
        {
            if (!string.IsNullOrEmpty(specificSceneName))
            {
                SceneManager.LoadScene(specificSceneName);
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
    }
    
    public void ExitToMainMenu()
    {
        PlayButtonSound();
        Time.timeScale = 1f;
        
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.Log("No main menu scene specified. Quitting application...");
            Application.Quit();
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }
    }
    
    private void PlayButtonSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }
    
    void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}