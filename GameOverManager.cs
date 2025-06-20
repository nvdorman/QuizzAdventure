using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverManager : MonoBehaviour
{
    [Header("UI References")]
    public Button restartButton;
    public Button exitButton;
    public GameObject gameOverPanel;
    
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
        // RESET FLAG SETIAP KALI START
        gameOverActivated = false;
        
        // Setup audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Setup button listeners
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners(); // Clear existing listeners
            restartButton.onClick.AddListener(RestartGame);
        }
        
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners(); // Clear existing listeners
            exitButton.onClick.AddListener(ExitToMainMenu);
        }
        
        // Hide game over panel initially
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
            Debug.Log("🎮 GameOverManager: Panel hidden on start");
        }
        else
        {
            Debug.LogWarning("⚠️ GameOverManager: gameOverPanel is NULL! Please assign in inspector!");
        }
        
        // Don't pause game at start
        Time.timeScale = 1f;
        
        Debug.Log("🎮 GameOverManager initialized - gameOverActivated reset to FALSE");
    }
    
    // Method yang dipanggil dari HealthSystem
    public void ActivateGameOver()
    {
        Debug.Log($"💀 ActivateGameOver called! gameOverActivated = {gameOverActivated}");
        
        // HAPUS PENGECEKAN gameOverActivated AGAR BISA DIPANGGIL BERULANG
        // if (!gameOverActivated) // <-- INI YANG MENYEBABKAN BUG!
        // {
            gameOverActivated = true;
            
            // Show game over panel
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                Debug.Log("✅ Game Over panel activated!");
            }
            else
            {
                Debug.LogError("❌ gameOverPanel is NULL! Cannot show game over!");
            }
            
            // Play game over sound
            if (audioSource != null && gameOverSound != null)
            {
                audioSource.PlayOneShot(gameOverSound);
            }
            
            // Pause game
            Time.timeScale = 0f;
            
            Debug.Log("💀 Game Over Activated!");
        // }
    }
    
    // Alternative method name for compatibility
    public void GameOver()
    {
        ActivateGameOver();
    }
    
    public void RestartGame()
    {
        Debug.Log("🔄 RestartGame called!");
        
        PlayButtonSound();
        
        // RESET FLAG SEBELUM RESTART
        gameOverActivated = false;
        
        // Reset time scale
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
        Debug.Log("🚪 ExitToMainMenu called!");
        
        PlayButtonSound();
        
        // Reset time scale
        Time.timeScale = 1f;
        
        // Reset flag
        gameOverActivated = false;
        
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
    
    // PUBLIC METHOD UNTUK RESET FLAG DARI LUAR
    public void ResetGameOverState()
    {
        gameOverActivated = false;
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        Time.timeScale = 1f;
        
        Debug.Log("🔄 GameOverManager state reset");
    }
    
    void OnDestroy()
    {
        Time.timeScale = 1f;
    }
    
    // Method untuk debug
    public bool IsGameOverActivated()
    {
        return gameOverActivated;
    }
}