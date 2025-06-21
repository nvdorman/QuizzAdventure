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
        Debug.Log($"💀 ActivateGameOver called! gameOverActivated = {gameOverActivated}");

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
    
    // PERBAIKAN UTAMA: RestartGame yang benar-benar restart scene
    public void RestartGame()
    {
        Debug.Log("🔄 RestartGame called!");
        
        // PERBAIKAN: Reset global state SEBELUM scene reload
        HealthSystem.ResetGameOverState();
        
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
            Debug.Log("🔧 Game over panel hidden before restart");
        }
        
        // Wait sebentar untuk button sound selesai
        yield return new WaitForSeconds(0.3f);
        
        Debug.Log("⏰ Audio delay completed, reloading scene...");
        
        // PERBAIKAN: Selalu reload scene untuk restart yang benar
        if (useCurrentScene)
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            Debug.Log($"🔄 Reloading current scene: {currentSceneName}");
            SceneManager.LoadScene(currentSceneName);
        }
        else
        {
            if (!string.IsNullOrEmpty(specificSceneName))
            {
                Debug.Log($"🔄 Loading specific scene: {specificSceneName}");
                SceneManager.LoadScene(specificSceneName);
            }
            else
            {
                // Fallback ke current scene
                string currentSceneName = SceneManager.GetActiveScene().name;
                Debug.Log($"🔄 Fallback - reloading current scene: {currentSceneName}");
                SceneManager.LoadScene(currentSceneName);
            }
        }
    }
    
    // PERBAIKAN: ExitToMainMenu dengan delay untuk audio
    public void ExitToMainMenu()
    {
        Debug.Log("🚪 ExitToMainMenu called!");
        
        PlayButtonSound();
        
        // PERBAIKAN: Berikan waktu untuk sound selesai
        StartCoroutine(ExitWithAudioDelay());
    }
    
    // PERBAIKAN: Coroutine untuk exit dengan delay audio
    System.Collections.IEnumerator ExitWithAudioDelay()
    {
        // Reset time scale dan flag
        Time.timeScale = 1f;
        gameOverActivated = false;
        
        // Wait sebentar untuk button sound selesai
        yield return new WaitForSeconds(0.3f);
        
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
    
    // PERBAIKAN: PlayButtonSound dengan volume control
    private void PlayButtonSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound, buttonSoundVolume);
            Debug.Log("🎵 Button sound played");
        }
    }
    
    // PUBLIC METHOD UNTUK RESET FLAG DARI LUAR (untuk restart manual tanpa scene reload)
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
    
    // PERBAIKAN: Cleanup persistent audio saat aplikasi quit
    void OnApplicationQuit()
    {
        CleanupPersistentAudio();
    }
    
    void OnDestroy()
    {
        Time.timeScale = 1f;
        
        // Jangan hapus persistent audio saat GameOverManager di-destroy
        // karena mungkin masih ada sound yang sedang diputar
    }
    
    // PERBAIKAN: Method untuk cleanup persistent audio
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
    
    // Method untuk debug
    public bool IsGameOverActivated()
    {
        return gameOverActivated;
    }
    
    // PERBAIKAN: Method untuk test audio di editor
    [ContextMenu("Test Game Over Sound")]
    void TestGameOverSound()
    {
        PlayGameOverSound();
    }
    
    [ContextMenu("Test Button Sound")]
    void TestButtonSound()
    {
        PlayButtonSound();
    }
    
    [ContextMenu("Cleanup Persistent Audio")]
    void ManualCleanupAudio()
    {
        CleanupPersistentAudio();
    }
    
    [ContextMenu("Test Full Restart")]
    void TestFullRestart()
    {
        RestartGame();
    }
    
    [ContextMenu("Debug Game Over State")]
    void DebugGameOverState()
    {
        Debug.Log("=== GAME OVER MANAGER DEBUG ===");
        Debug.Log($"gameOverActivated: {gameOverActivated}");
        Debug.Log($"gameOverPanel active: {(gameOverPanel != null ? gameOverPanel.activeInHierarchy.ToString() : "NULL")}");
        Debug.Log($"Time.timeScale: {Time.timeScale}");
        Debug.Log("==============================");
    }
}