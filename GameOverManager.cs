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
    
    // PERBAIKAN: Static reference untuk persistent audio
    private static AudioSource persistentAudioSource;
    private static GameObject persistentAudioObject;
    
    void Start()
    {
        // PERBAIKAN: Reset semua flags saat Start
        gameOverActivated = false;
        
        Debug.Log("🎮 GameOverManager Start - Setting up...");
        
        // PERBAIKAN: Force reset semua health systems di scene saat start
        ForceResetAllHealthSystems();
        
        // Setup audio source
        SetupAudioSource();
        
        // Setup button listeners
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartGame);
            Debug.Log("✅ Restart button setup complete");
        }
        else
        {
            Debug.LogWarning("⚠️ Restart button not assigned!");
        }
        
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(ExitToMainMenu);
            Debug.Log("✅ Exit button setup complete");
        }
        else
        {
            Debug.LogWarning("⚠️ Exit button not assigned!");
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
        
        Debug.Log("🎮 GameOverManager initialized - ready for audio-safe restarts");
    }
    
    // PERBAIKAN: Method untuk force reset SEMUA health systems di scene
    void ForceResetAllHealthSystems()
    {
        Debug.Log("🔧🔧 GameOverManager - Force resetting all health systems in scene...");
        
        // Reset PlayerHealth components
        PlayerHealth[] playerHealths = FindObjectsOfType<PlayerHealth>();
        foreach (PlayerHealth ph in playerHealths)
        {
            Debug.Log($"🔧🔧 Force resetting PlayerHealth: {ph.name}");
            ph.ResetHealth();
        }
        
        // Reset HealthSystem components
        HealthSystem[] healthSystems = FindObjectsOfType<HealthSystem>();
        foreach (HealthSystem hs in healthSystems)
        {
            Debug.Log($"🔧🔧 Force resetting HealthSystem: {hs.name}");
            hs.ResetHealth();
        }
        
        // Force reset static variables
        HealthSystem.ResetGameOverState();
        
        Debug.Log("🔧🔧 GameOverManager - All health systems force reset complete!");
    }
    
    // PERBAIKAN: Setup audio source yang persistent
    void SetupAudioSource()
    {
        // Setup local audio source untuk UI sounds
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Setup persistent audio source untuk game over sound
        if (persistentAudioSource == null)
        {
            CreatePersistentAudioSource();
        }
    }
    
    // PERBAIKAN: Buat audio source yang tidak akan ter-destroy
    void CreatePersistentAudioSource()
    {
        // Buat GameObject khusus untuk persistent audio
        persistentAudioObject = new GameObject("PersistentGameOverAudio");
        persistentAudioSource = persistentAudioObject.AddComponent<AudioSource>();
        
        // Setup audio source
        persistentAudioSource.playOnAwake = false;
        persistentAudioSource.spatialBlend = 0f; // 2D sound
        persistentAudioSource.volume = gameOverSoundVolume;
        
        // PENTING: Jangan hancurkan saat scene berubah
        DontDestroyOnLoad(persistentAudioObject);
        
        Debug.Log("🎵 Persistent audio source created");
    }
    
    // Method yang dipanggil dari HealthSystem
    public void ActivateGameOver()
    {
        Debug.Log($"💀 GameOverManager ActivateGameOver called! gameOverActivated = {gameOverActivated}");

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

        // PERBAIKAN: Play game over sound dengan persistent audio source
        PlayGameOverSound();

        // Pause game
        Time.timeScale = 0f;

        Debug.Log("💀 Game Over Activated with persistent audio!");
    }
    
    // PERBAIKAN: Method khusus untuk play game over sound
    void PlayGameOverSound()
    {
        if (gameOverSound != null)
        {
            // Pastikan persistent audio source ada
            if (persistentAudioSource == null)
            {
                CreatePersistentAudioSource();
            }
            
            if (persistentAudioSource != null)
            {
                persistentAudioSource.volume = gameOverSoundVolume;
                persistentAudioSource.PlayOneShot(gameOverSound);
                Debug.Log("🎵 Game over sound played with persistent audio");
            }
            else
            {
                // Fallback ke audio source biasa
                if (audioSource != null)
                {
                    audioSource.PlayOneShot(gameOverSound, gameOverSoundVolume);
                    Debug.Log("🎵 Game over sound played with fallback audio");
                }
            }
        }
    }
    
    // Alternative method name for compatibility
    public void GameOver()
    {
        ActivateGameOver();
    }
    
    // PERBAIKAN UTAMA: RestartGame yang benar-benar restart scene dengan force reset
    public void RestartGame()
    {
        Debug.Log("🔄🔄 GameOverManager RestartGame called!");
        
        // PERBAIKAN: Reset global state SEBELUM scene reload
        Debug.Log("🔧🔧 Calling HealthSystem.ResetGameOverState()...");
        HealthSystem.ResetGameOverState();
        
        // PERBAIKAN: Reset semua PlayerHealth dan HealthSystem di scene SEBELUM reload
        Debug.Log("🔧🔧 Finding and resetting all health systems before reload...");
        
        PlayerHealth[] playerHealths = FindObjectsOfType<PlayerHealth>();
        foreach (PlayerHealth ph in playerHealths)
        {
            Debug.Log($"🔧🔧 Resetting PlayerHealth before reload: {ph.name}");
            ph.ResetHealth();
        }
        
        HealthSystem[] healthSystems = FindObjectsOfType<HealthSystem>();
        foreach (HealthSystem hs in healthSystems)
        {
            Debug.Log($"🔧🔧 Resetting HealthSystem before reload: {hs.name}");
            hs.ResetHealth();
        }
        
        // Play button sound
        PlayButtonSound();
        
        // PERBAIKAN: Panggil coroutine untuk restart dengan delay
        StartCoroutine(RestartWithAudioDelay());
    }
    
    // PERBAIKAN: Coroutine untuk restart dengan delay audio
    System.Collections.IEnumerator RestartWithAudioDelay()
    {
        Debug.Log("⏰ RestartWithAudioDelay started...");
        
        // PERBAIKAN: Reset semua flags dulu
        gameOverActivated = false;
        
        // Reset time scale untuk memungkinkan coroutine berjalan
        Time.timeScale = 1f;
        
        // PERBAIKAN: Hide panel sebelum reload
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
            Debug.Log("🎮 GameOver panel hidden before scene reload");
        }
        
        if (gameOverCanvas != null)
        {
            gameOverCanvas.gameObject.SetActive(false);
            Debug.Log("🎮 GameOver canvas hidden before scene reload");
        }
        
        // Wait for button sound to finish
        yield return new WaitForSeconds(0.2f);
        
        // Get current scene name
        string currentSceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"🔄 Reloading scene: {currentSceneName}");
        
        // Reload scene
        if (useCurrentScene)
        {
            SceneManager.LoadScene(currentSceneName);
        }
        else if (!string.IsNullOrEmpty(specificSceneName))
        {
            SceneManager.LoadScene(specificSceneName);
        }
        else
        {
            SceneManager.LoadScene(currentSceneName);
        }
    }
    
    public void ExitToMainMenu()
    {
        Debug.Log("🚪 ExitToMainMenu called!");
        
        PlayButtonSound();
        
        Time.timeScale = 1f;
        gameOverActivated = false;
        
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
        gameOverActivated = false;
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        Time.timeScale = 1f;
        
        Debug.Log("🔄 GameOverManager state reset");
    }
    
    void OnApplicationQuit()
    {
        CleanupPersistentAudio();
    }
    
    void OnDestroy()
    {
        Time.timeScale = 1f;
    }
    
    public static void CleanupPersistentAudio()
    {
        if (persistentAudioObject != null)
        {
            if (Application.isPlaying)
            {
                Destroy(persistentAudioObject);
            }
            persistentAudioSource = null;
            persistentAudioObject = null;
            Debug.Log("🧹 Persistent audio cleaned up");
        }
    }
    
    public bool IsGameOverActivated()
    {
        return gameOverActivated;
    }
}