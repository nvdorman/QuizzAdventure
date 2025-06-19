using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;

public class GameOverTrigger : MonoBehaviour
{
    [Header("Game Over Settings")]
    public Canvas gameOverCanvas;
    public GameOverManager gameOverManager; // Reference ke GameOverManager
    public string playerTag = "Player";
    public bool pauseGameOnGameOver = true;
    
    [Header("Dangerous Tags")]
    public string[] dangerousTags = {"Water", "Lava", "Spike", "Poison"};
    
    [Header("Delay Settings")]
    public float gameOverDelay = 2.0f;
    public bool disablePlayerImmediately = false;
    public bool showCountdown = true;
    
    [Header("Visual Effects")]
    public GameObject warningEffect;
    public Color playerFlashColor = Color.red;
    public float flashSpeed = 8f;
    
    [Header("Enhanced Visual Effects")]
    public Color[] flashColors = {Color.red, Color.yellow, Color.white};
    public float fadeOutDuration = 1.0f;
    public AudioClip deathSound;
    
    private bool gameOverTriggered = false;
    private SpriteRenderer playerRenderer;
    private Color originalPlayerColor;
    private AudioSource audioSource;
    
    private void Start()
    {
        if (gameOverCanvas != null)
        {
            gameOverCanvas.gameObject.SetActive(false);
        }
        
        // Auto-find GameOverManager jika tidak diassign
        if (gameOverManager == null && gameOverCanvas != null)
        {
            gameOverManager = gameOverCanvas.GetComponent<GameOverManager>();
        }
        
        // Setup audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && deathSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag) && !gameOverTriggered)
        {
            if (HasDangerousTag(gameObject))
            {
                gameOverTriggered = true;
                
                playerRenderer = other.GetComponent<SpriteRenderer>();
                if (playerRenderer != null)
                {
                    originalPlayerColor = playerRenderer.color;
                }
                
                if (audioSource != null && deathSound != null)
                {
                    audioSource.PlayOneShot(deathSound);
                }
                
                StartCoroutine(TriggerGameOverWithDelay(other.gameObject));
            }
        }
    }
    
    private bool HasDangerousTag(GameObject obj)
    {
        foreach (string tag in dangerousTags)
        {
            if (obj.CompareTag(tag))
            {
                return true;
            }
        }
        return false;
    }
    
    private IEnumerator TriggerGameOverWithDelay(GameObject player)
    {
        Debug.Log("Player hit dangerous area! Game Over in " + gameOverDelay + " seconds...");
        
        if (warningEffect != null)
        {
            warningEffect.SetActive(true);
        }
        
        if (disablePlayerImmediately)
        {
            DisablePlayer(player);
        }
        else
        {
            PlayerController2D playerController = player.GetComponent<PlayerController2D>();
            if (playerController != null)
            {
                playerController.SetSlowMotion(0.3f);
            }
        }
        
        StartCoroutine(EnhancedFlashPlayer());
        
        if (showCountdown)
        {
            for (int i = Mathf.FloorToInt(gameOverDelay); i > 0; i--)
            {
                Debug.Log("Game Over in: " + i);
                yield return new WaitForSeconds(1f);
            }
        }
        else
        {
            yield return new WaitForSeconds(gameOverDelay);
        }
        
        StopCoroutine(EnhancedFlashPlayer());
        StartCoroutine(FadeOutPlayer());
        
        yield return new WaitForSeconds(fadeOutDuration);
        
        TriggerGameOver(player);
    }
    
    private IEnumerator EnhancedFlashPlayer()
    {
        int colorIndex = 0;
        while (true)
        {
            if (playerRenderer != null)
            {
                playerRenderer.color = flashColors[colorIndex % flashColors.Length];
                yield return new WaitForSeconds(1f / flashSpeed);
                
                playerRenderer.color = originalPlayerColor;
                yield return new WaitForSeconds(1f / flashSpeed);
                
                colorIndex++;
            }
            else
            {
                yield break;
            }
        }
    }
    
    private IEnumerator FadeOutPlayer()
    {
        if (playerRenderer != null)
        {
            float elapsed = 0f;
            Color startColor = playerRenderer.color;
            
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
                playerRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }
        }
    }
    
    private void TriggerGameOver(GameObject player)
    {
        if (warningEffect != null)
        {
            warningEffect.SetActive(false);
        }
        
        DisablePlayer(player);
        
        if (gameOverCanvas != null)
        {
            gameOverCanvas.gameObject.SetActive(true);
            
            // Panggil ActivateGameOver() untuk pause game
            if (gameOverManager != null)
            {
                gameOverManager.ActivateGameOver();
            }
        }
        
        Debug.Log("Game Over! Player touched dangerous area!");
    }
    
    private void DisablePlayer(GameObject player)
    {
        MonoBehaviour[] playerScripts = player.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in playerScripts)
        {
            if (script != this && script.GetType().Name != "Transform")
            {
                script.enabled = false;
            }
        }
        
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerRb.velocity = Vector2.zero;
            playerRb.isKinematic = true;
        }
    }
    
    public void ResetGameOverState()
    {
        gameOverTriggered = false;
        StopAllCoroutines();
        
        if (playerRenderer != null)
        {
            playerRenderer.color = originalPlayerColor;
        }
        
        if (warningEffect != null)
        {
            warningEffect.SetActive(false);
        }
        
        Time.timeScale = 1f;
    }
}