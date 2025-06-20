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
        // RESET FLAG SETIAP KALI START
        gameOverActivated = false;
        
        // Setup audio source
        SetupAudioSource();
        
        // Setup button listeners
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartGame);
        }
        
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
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
    
    // PERBAIKAN: RestartGame dengan delay untuk audio
    public void RestartGame()
    {
        Debug.Log("🔄 RestartGame called!");
        
        PlayButtonSound();
        
        // PERBAIKAN: Berikan waktu untuk sound selesai
        StartCoroutine(RestartWithAudioDelay());
    }
    
    // PERBAIKAN: Coroutine untuk restart dengan delay audio
    System.Collections.IEnumerator RestartWithAudioDelay()
    {
        // Reset flag
        gameOverActivated = false;
        
        // Reset time scale untuk memungkinkan coroutine berjalan
        Time.timeScale = 1f;
        
        // Wait sebentar untuk button sound selesai
        yield return new WaitForSeconds(0.3f);
        
        // Load scene
        if (useCurrentScene)
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            Debug.Log($"🔄 Reloading scene: {currentSceneName}");
            SceneManager.LoadScene(currentSceneName);
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
    
    // PERBAIKAN: Method untuk cleanup manual (untuk debugging)
    [ContextMenu("Cleanup Persistent Audio")]
    void ManualCleanupAudio()
    {
        CleanupPersistentAudio();
    }
}