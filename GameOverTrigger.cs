using UnityEngine;
using System.Collections;

public class GameOverTrigger : MonoBehaviour
{
    [Header("Game Over Settings")]
    public Canvas gameOverCanvas;
    public GameOverManager gameOverManager;
    public string playerTag = "Player";
    
    [Header("Dangerous Tags")]
    public string[] dangerousTags = {"Water", "Lava", "Spike", "Poison"};
    
    [Header("Delay Settings")]
    public float gameOverDelay = 1.0f;
    public bool disablePlayerImmediately = false;
    
    [Header("Visual Effects")]
    public GameObject warningEffect;
    public Color[] flashColors = {Color.red, Color.yellow, Color.white};
    public float flashSpeed = 8f;
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
        if (gameOverManager == null)
        {
            gameOverManager = FindObjectOfType<GameOverManager>();
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
            try
            {
                if (obj.CompareTag(tag))
                {
                    return true;
                }
            }
            catch (UnityException e)
            {
                Debug.LogWarning($"⚠️ Tag '{tag}' is not defined! Error: {e.Message}");
                // Fallback: cek berdasarkan nama
                if (obj.name.ToLower().Contains(tag.ToLower()))
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    private IEnumerator TriggerGameOverWithDelay(GameObject player)
    {
        Debug.Log($"💀 Player hit dangerous area! Game Over in {gameOverDelay} seconds...");
        
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
        
        yield return new WaitForSeconds(gameOverDelay);
        
        StopCoroutine(EnhancedFlashPlayer());
        StartCoroutine(FadeOutPlayer());
        
        yield return new WaitForSeconds(fadeOutDuration);
        
        TriggerGameOver(player);
    }
    
    private IEnumerator EnhancedFlashPlayer()
    {
        if (playerRenderer == null) yield break;
        
        int colorIndex = 0;
        while (true)
        {
            playerRenderer.color = flashColors[colorIndex % flashColors.Length];
            yield return new WaitForSeconds(1f / flashSpeed);
            
            playerRenderer.color = originalPlayerColor;
            yield return new WaitForSeconds(1f / flashSpeed);
            
            colorIndex++;
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
        
        // Langsung trigger game over tanpa health system
        if (gameOverManager != null)
        {
            Debug.Log("💀 Triggering Game Over via GameOverManager");
            gameOverManager.ActivateGameOver();
        }
        else
        {
            Debug.LogError("❌ GameOverManager tidak ditemukan!");
        }
    }
    
    private void DisablePlayer(GameObject player)
    {
        PlayerController2D playerController = player.GetComponent<PlayerController2D>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerRb.velocity = Vector2.zero;
            playerRb.isKinematic = true;
        }
    }
}