using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverManager : MonoBehaviour
{
    [Header("UI References")]
    public Button restartButton;
    public Button exitButton;
    
    [Header("Scene Settings")]
    public string mainMenuSceneName = "MainMenu"; // Nama scene menu utama
    public bool useCurrentScene = true; // Jika true, restart scene saat ini
    public string specificSceneName = ""; // Jika useCurrentScene false, gunakan scene ini
    
    [Header("Audio")]
    public AudioClip buttonClickSound;
    public AudioClip gameOverSound;
    
    private AudioSource audioSource;
    
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
        
        // Play game over sound
        if (audioSource != null && gameOverSound != null)
        {
            audioSource.PlayOneShot(gameOverSound);
        }
        
        // Pastikan time scale normal untuk UI
        Time.timeScale = 0f; // Pause game
    }
    
    public void RestartGame()
    {
        // Play button sound
        PlayButtonSound();
        
        // Reset time scale
        Time.timeScale = 1f;
        
        // Restart scene
        if (useCurrentScene)
        {
            // Restart scene yang sedang aktif
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        else
        {
            // Load scene tertentu
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
        // Play button sound
        PlayButtonSound();
        
        // Reset time scale
        Time.timeScale = 1f;
        
        // Load main menu scene
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            // Jika tidak ada main menu, keluar dari aplikasi
            Debug.Log("No main menu scene specified. Quitting application...");
            Application.Quit();
            
            // Untuk editor Unity
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
        // Pastikan time scale kembali normal
        Time.timeScale = 1f;
    }
}