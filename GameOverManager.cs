using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class GameOverManager : MonoBehaviour
{
    [Header("UI References")]
    public Button restartButton;
    public Button exitButton;
    public GameObject gameOverPanel;
    public Canvas gameOverCanvas;
    
    [Header("Scene Settings")]
    public string mainMenuSceneName = "MainMenu";
    public bool useCurrentScene = true;
    public string specificSceneName = "";
    
    [Header("Audio")]
    public AudioClip buttonClickSound;
    public AudioClip gameOverSound;
    [Range(0f, 1f)]
    public float gameOverSoundVolume = 0.7f;
    [Range(0f, 1f)]
    public float buttonSoundVolume = 0.5f;
    
    private AudioSource audioSource;
    private bool gameOverActivated = false;
    
    void Start()
    {
        // Force reset semua flags saat Start
        gameOverActivated = false;
        
        Debug.Log("🎮 GameOverManager Start - Setting up...");
        
        // Cleanup any leftover persistent audio objects
        CleanupAllPersistentAudio();
        
        // Force reset semua health systems di scene saat start
        ForceResetAllSystems();
        
        // Setup audio source
        SetupAudioSource();
        
        // Setup button listeners dengan debugging
        SetupButtons();
        
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
        
        Debug.Log("🎮 GameOverManager initialized and ready!");
    }
    
    void SetupButtons()
    {
        // Setup restart button dengan debugging detail
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(() => {
                Debug.Log("🔄🔄 RESTART BUTTON CLICKED!");
                RestartGame();
            });
            restartButton.interactable = true;
            Debug.Log("✅ Restart button setup complete");
        }
        else
        {
            Debug.LogError("❌ CRITICAL: Restart button is NULL!");
        }
        
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(() => {
                Debug.Log("🚪 EXIT BUTTON CLICKED!");
                ExitToMainMenu();
            });
            exitButton.interactable = true;
            Debug.Log("✅ Exit button setup complete");
        }
    }
    
    // Method untuk force reset SEMUA systems di scene
    void ForceResetAllSystems()
    {
        Debug.Log("🔧🔧 GameOverManager - Force resetting all systems in scene...");
        
        // Reset PlayerController2D components
        PlayerController2D[] playerControllers = FindObjectsOfType<PlayerController2D>();
        foreach (PlayerController2D pc in playerControllers)
        {
            Debug.Log($"🔧🔧 Force resetting PlayerController2D: {pc.name}");
            // Reset slow motion
            pc.ResetSlowMotion();
            // Reset sprite color
            pc.ResetSpriteColor();
        }
        
        // Reset PlayerHealth components if they exist
        PlayerHealth[] playerHealths = FindObjectsOfType<PlayerHealth>();
        foreach (PlayerHealth ph in playerHealths)
        {
            Debug.Log($"🔧🔧 Force resetting PlayerHealth: {ph.name}");
            if (ph.GetComponent<PlayerHealth>().GetType().GetMethod("ResetHealth") != null)
            {
                ph.ResetHealth();
            }
        }
        
        // Reset HealthSystem components if they exist
        HealthSystem[] healthSystems = FindObjectsOfType<HealthSystem>();
        foreach (HealthSystem hs in healthSystems)
        {
            Debug.Log($"🔧🔧 Force resetting HealthSystem: {hs.name}");
            if (hs.GetComponent<HealthSystem>().GetType().GetMethod("ResetHealth") != null)
            {
                hs.ResetHealth();
            }
        }
        
        // Reset GameOverTrigger components if they exist
        GameOverTrigger[] gameOverTriggers = FindObjectsOfType<GameOverTrigger>();
        foreach (GameOverTrigger got in gameOverTriggers)
        {
            if (got.GetComponent<GameOverTrigger>().GetType().GetMethod("ResetGameOverState") != null)
            {
                got.ResetGameOverState();
                Debug.Log($"🔧🔧 Reset GameOverTrigger: {got.name}");
            }
        }
        
        // Force reset static variables if HealthSystem exists
        try
        {
            if (System.Type.GetType("HealthSystem") != null)
            {
                var resetMethod = System.Type.GetType("HealthSystem").GetMethod("ResetGameOverState", 
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                if (resetMethod != null)
                {
                    resetMethod.Invoke(null, null);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.Log($"Note: HealthSystem static reset not available: {e.Message}");
        }
        
        Debug.Log("🔧🔧 GameOverManager - All systems force reset complete!");
    }
    
    void SetupAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.volume = gameOverSoundVolume;
    }
    
    // Method yang dipanggil dari PlayerController2D atau sistem lain
    public void ActivateGameOver()
    {
        Debug.Log($"💀💀💀 GameOverManager ActivateGameOver called!");
        Debug.Log($"Current gameOverActivated state: {gameOverActivated}");
        Debug.Log($"Current Time.timeScale: {Time.timeScale}");

        // Allow multiple activations but with proper handling
        gameOverActivated = true;

        // Show game over panel
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Debug.Log("✅ Game Over panel activated!");
            
            // Ensure buttons are interactable
            if (restartButton != null)
            {
                restartButton.interactable = true;
                Debug.Log($"🔧 Restart button interactable: {restartButton.interactable}");
            }
            if (exitButton != null)
            {
                exitButton.interactable = true;
            }
        }
        else
        {
            Debug.LogError("❌ gameOverPanel is NULL! Cannot show game over!");
        }

        // Always play game over sound with fresh audio source
        PlayGameOverSound();

        // Delay pause to allow UI to setup properly
        StartCoroutine(PauseAfterUI());

        Debug.Log("💀 Game Over Activated successfully!");
    }
    
    // Pause setelah UI siap
    IEnumerator PauseAfterUI()
    {
        yield return new WaitForEndOfFrame(); // Wait for UI to be ready
        Time.timeScale = 0f;
        Debug.Log("⏸️ Game paused after UI setup");
    }
    
    // Simplified game over sound - no persistent objects
    void PlayGameOverSound()
    {
        if (gameOverSound != null && audioSource != null)
        {
            // Stop any currently playing audio
            audioSource.Stop();
            
            // Play the game over sound
            audioSource.volume = gameOverSoundVolume;
            audioSource.PlayOneShot(gameOverSound);
            Debug.Log("🎵 Game over sound played with local audio source");
        }
        else
        {
            Debug.LogWarning("⚠️ GameOverSound or AudioSource is missing!");
        }
    }
    
    // Alternative method name for compatibility
    public void GameOver()
    {
        ActivateGameOver();
    }
    
    // RestartGame yang lebih simple dan reliable
    public void RestartGame()
    {
        Debug.Log("🔄🔄🔄 GameOverManager RestartGame() EXECUTED!");
        Debug.Log($"Current scene: {SceneManager.GetActiveScene().name}");
        
        // Play button sound
        PlayButtonSound();
        
        // IMMEDIATE: Reset time scale
        Time.timeScale = 1f;
        Debug.Log("⏰ Time scale reset to 1");
        
        // Cleanup everything before reload
        CleanupAllPersistentAudio();
        
        // Reset flags
        gameOverActivated = false;
        
        // Hide UI immediately
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
            Debug.Log("🎮 Game Over panel hidden");
        }
        
        if (gameOverCanvas != null)
        {
            gameOverCanvas.gameObject.SetActive(false);
            Debug.Log("🎮 Game Over canvas hidden");
        }
        
        // IMMEDIATE SCENE RELOAD
        string currentSceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"🔄 IMMEDIATELY reloading scene: {currentSceneName}");
        
        SceneManager.LoadScene(currentSceneName);
    }
    
    public void ExitToMainMenu()
    {
        Debug.Log("🚪 ExitToMainMenu called!");
        
        PlayButtonSound();
        
        Time.timeScale = 1f;
        gameOverActivated = false;
        
        CleanupAllPersistentAudio();
        
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.Log($"🏠 Loading main menu: {mainMenuSceneName}");
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
            audioSource.PlayOneShot(buttonClickSound, buttonSoundVolume);
            Debug.Log("🎵 Button sound played");
        }
    }
    
    public void ResetGameOverState()
    {
        Debug.Log("🔄 ResetGameOverState called");
        gameOverActivated = false;
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        Time.timeScale = 1f;
        
        Debug.Log("🔄 GameOverManager state reset complete");
    }
    
    // Cleanup semua persistent audio objects
    void CleanupAllPersistentAudio()
    {
        // Find and destroy any persistent game over audio objects
        GameObject[] persistentObjects = GameObject.FindGameObjectsWithTag("Untagged");
        foreach (GameObject obj in persistentObjects)
        {
            if (obj.name.Contains("PersistentGameOverAudio") || 
                obj.name.Contains("Persistent") && obj.GetComponent<AudioSource>() != null)
            {
                Debug.Log($"🧹 Destroying persistent audio object: {obj.name}");
                Destroy(obj);
            }
        }
        
        // Also try to find by component
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
        foreach (AudioSource source in allAudioSources)
        {
            if (source.transform.parent == null && 
                source.gameObject.name.Contains("Persistent"))
            {
                Debug.Log($"🧹 Destroying persistent audio: {source.gameObject.name}");
                Destroy(source.gameObject);
            }
        }
        
        Debug.Log("🧹 All persistent audio cleanup complete");
    }
    
    void OnApplicationQuit()
    {
        CleanupAllPersistentAudio();
    }
    
    void OnDestroy()
    {
        Time.timeScale = 1f;
        CleanupAllPersistentAudio();
    }
    
    public bool IsGameOverActivated()
    {
        return gameOverActivated;
    }
    
    // DEBUGGING: Method untuk test aktivasi game over
    [ContextMenu("Test Game Over")]
    public void TestGameOver()
    {
        Debug.Log("🧪 Manual game over test triggered!");
        ActivateGameOver();
    }
    
    // DEBUGGING: Method untuk check status
    [ContextMenu("Debug Status")]
    public void DebugStatus()
    {
        Debug.Log("=== GAME OVER MANAGER DEBUG ===");
        Debug.Log($"gameOverActivated: {gameOverActivated}");
        Debug.Log($"Time.timeScale: {Time.timeScale}");
        Debug.Log($"gameOverPanel active: {(gameOverPanel != null ? gameOverPanel.activeInHierarchy : "NULL")}");
        Debug.Log($"AudioSource: {(audioSource != null ? "Present" : "NULL")}");
        Debug.Log($"GameOverSound: {(gameOverSound != null ? "Present" : "NULL")}");
        Debug.Log("===============================");
    }
}