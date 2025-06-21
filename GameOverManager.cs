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

    [Header("Audio Objects Management")]
    public GameObject[] persistentAudioObjects; // Input dari Inspector
    public bool cleanupPersistentAudio = true;
    
    private AudioSource audioSource;
    private bool gameOverActivated = false;
    
    void Start()
    {
        // Force reset semua flags saat Start
        gameOverActivated = false;
        
        Debug.Log("🎮 GameOverManager Start - Setting up...");
        
        // Cleanup any leftover persistent audio objects
        CleanupAllPersistentAudio();
        
        // Force reset semua systems di scene saat start
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
        
        // Reset EnemyDamage components
        EnemyDamage[] enemyDamages = FindObjectsOfType<EnemyDamage>();
        foreach (EnemyDamage ed in enemyDamages)
        {
            Debug.Log($"🔧🔧 Force resetting EnemyDamage: {ed.name}");
            // Reset trigger flag menggunakan reflection
            var field = ed.GetType().GetField("hasTriggered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(ed, false);
            }
        }
        
        // Reset GameOverTrigger components
        GameOverTrigger[] gameOverTriggers = FindObjectsOfType<GameOverTrigger>();
        foreach (GameOverTrigger got in gameOverTriggers)
        {
            Debug.Log($"🔧🔧 Force resetting GameOverTrigger: {got.name}");
            // Reset trigger flag menggunakan reflection
            var field = got.GetType().GetField("gameOverTriggered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(got, false);
            }
        }
        
        Debug.Log("🔧🔧 All systems reset complete!");
    }
    
    void SetupAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        
        Debug.Log("🔊 AudioSource setup complete");
    }
    
    void CleanupAllPersistentAudio()
    {
        if (!cleanupPersistentAudio) return;
        
        // PERBAIKAN: Gunakan array dari Inspector, bukan tag
        if (persistentAudioObjects != null && persistentAudioObjects.Length > 0)
        {
            foreach (GameObject audioObj in persistentAudioObjects)
            {
                if (audioObj != null)
                {
                    Destroy(audioObj);
                    Debug.Log($"🧹 Cleaned up persistent audio object: {audioObj.name}");
                }
            }
        }
        
        // Alternatif: Cari berdasarkan nama object (tanpa tag)
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Persistent") && obj.GetComponent<AudioSource>() != null)
            {
                Destroy(obj);
                Debug.Log($"🧹 Cleaned up persistent audio by name: {obj.name}");
            }
        }
    }
    
    // PUBLIC METHOD - Dipanggil dari script lain untuk trigger game over
    public void ActivateGameOver()
    {
        if (gameOverActivated) 
        {
            Debug.Log("⚠️ Game Over sudah diaktivasi sebelumnya, skip");
            return;
        }

        gameOverActivated = true;
        Debug.Log("💀💀💀 GAME OVER ACTIVATED!");
        
        // PERBAIKAN: Pastikan Canvas aktif SEBELUM StartCoroutine
        if (gameOverCanvas != null && !gameOverCanvas.gameObject.activeInHierarchy)
        {
            gameOverCanvas.gameObject.SetActive(true);
            Debug.Log("🎮 Canvas activated before starting coroutine");
        }
        
        StartCoroutine(GameOverSequence());
    }
    
    // Alias untuk kompatibilitas
    public void TriggerGameOver()
    {
        ActivateGameOver();
    }
    
    IEnumerator GameOverSequence()
    {
        // Play game over sound
        if (audioSource != null && gameOverSound != null)
        {
            audioSource.volume = gameOverSoundVolume;
            audioSource.PlayOneShot(gameOverSound);
            Debug.Log("🔊 Playing game over sound");
        }
        
        // Wait sedikit untuk effect
        yield return new WaitForSeconds(0.5f);
        
        // Show game over panel
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Debug.Log("🎮 Game Over panel activated");
        }
        else if (gameOverCanvas != null)
        {
            gameOverCanvas.gameObject.SetActive(true);
            Debug.Log("🎮 Game Over canvas activated");
        }
        else
        {
            Debug.LogError("❌ CRITICAL: No game over UI found!");
        }
        
        // Pause game
        if (pauseGameOnGameOver)
        {
            Time.timeScale = 0f;
            Debug.Log("⏸️ Game paused");
        }
        
        // Enable buttons
        if (restartButton != null) restartButton.interactable = true;
        if (exitButton != null) exitButton.interactable = true;
        
        Debug.Log("🎮 Game Over sequence complete");
    }
    
    [Header("Game Over Behavior")]
    public bool pauseGameOnGameOver = true;
    
    public void RestartGame()
    {
        Debug.Log("🔄 Restarting game...");
        
        PlayButtonSound();
        
        // Reset time scale
        Time.timeScale = 1f;
        
        // Determine scene to load
        string sceneToLoad = useCurrentScene ? SceneManager.GetActiveScene().name : 
                            (string.IsNullOrEmpty(specificSceneName) ? SceneManager.GetActiveScene().name : specificSceneName);
        
        Debug.Log($"🔄 Loading scene: {sceneToLoad}");
        
        // Load scene
        SceneManager.LoadScene(sceneToLoad);
    }
    
    public void ExitToMainMenu()
    {
        Debug.Log("🚪 Exiting to main menu...");
        
        PlayButtonSound();
        
        // Reset time scale
        Time.timeScale = 1f;
        
        // Load main menu
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.Log($"🚪 Loading main menu: {mainMenuSceneName}");
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogWarning("⚠️ Main menu scene name not set, quitting application");
            Application.Quit();
        }
    }
    
    void PlayButtonSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.volume = buttonSoundVolume;
            audioSource.PlayOneShot(buttonClickSound);
        }
    }
    
    // Method untuk debugging
    public bool IsGameOverActive()
    {
        return gameOverActivated;
    }
    
    public void ResetGameOverState()
    {
        gameOverActivated = false;
        Debug.Log("🔄 Game Over state reset");
    }
}