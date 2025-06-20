using UnityEngine;
using System.Collections;

public class HealthSystem : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;
    
    [Header("Damage Settings")]
    public int contactDamage = 50; // Damage saat menyentuh enemy
    public float invulnerabilityTime = 1f;
    
    [Header("Visual Feedback")]
    public Color damageColor = Color.red;
    public float flashDuration = 0.2f;
    
    [Header("Audio")]
    public AudioClip hurtSound;
    public AudioClip deathSound;
    
    [Header("Game Over Settings")]
    public Canvas gameOverCanvas; // Reference ke game over canvas
    
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isInvulnerable = false;
    private AudioSource audioSource;
    
    // Events
    public System.Action<int, int> OnHealthChanged; // current, max
    public System.Action OnDeath;
    
    void Start()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        // Auto-find game over canvas if not assigned
        if (gameOverCanvas == null)
        {
            GameObject canvasObj = GameObject.Find("GameOverCanvas");
            if (canvasObj != null)
            {
                gameOverCanvas = canvasObj.GetComponent<Canvas>();
            }
        }
        
        // Notify UI
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    public void TakeDamage(int damage)
    {
        if (isInvulnerable || currentHealth <= 0) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        // Play hurt sound
        if (audioSource != null && hurtSound != null)
        {
            audioSource.PlayOneShot(hurtSound);
        }
        
        // Visual feedback
        if (spriteRenderer != null)
        {
            StartCoroutine(FlashDamage());
        }
        
        // Notify UI
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        Debug.Log($"Health: {currentHealth}/{maxHealth}");
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    System.Collections.IEnumerator FlashDamage()
    {
        isInvulnerable = true;
        
        // Flash effect
        float flashTime = 0f;
        while (flashTime < flashDuration)
        {
            spriteRenderer.color = Color.Lerp(originalColor, damageColor, Mathf.PingPong(flashTime * 10f, 1f));
            flashTime += Time.deltaTime;
            yield return null;
        }
        
        spriteRenderer.color = originalColor;
        
        // Invulnerability frames
        yield return new WaitForSeconds(invulnerabilityTime - flashDuration);
        isInvulnerable = false;
    }
    
    void Die()
    {
        // Play death sound
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
        
        Debug.Log("Player died!");
        OnDeath?.Invoke();
        
        // Disable player controls
        PlayerController2D playerController = GetComponent<PlayerController2D>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        // Trigger game over after a delay
        StartCoroutine(GameOverDelay());
    }
    
    System.Collections.IEnumerator GameOverDelay()
    {
        yield return new WaitForSeconds(2f);
        
        // Try to find and activate GameOverManager
        GameOverManager gameOverManager = FindObjectOfType<GameOverManager>();
        if (gameOverManager != null)
        {
            gameOverManager.ActivateGameOver();
        }
        else
        {
            // Fallback: Directly show game over canvas and pause game
            if (gameOverCanvas != null)
            {
                gameOverCanvas.gameObject.SetActive(true);
                Time.timeScale = 0f;
                Debug.Log("Game Over - Health depleted!");
            }
            else
            {
                // Last resort: Find any game over canvas in scene
                Canvas[] canvases = FindObjectsOfType<Canvas>();
                foreach (Canvas canvas in canvases)
                {
                    if (canvas.name.ToLower().Contains("gameover") || 
                        canvas.name.ToLower().Contains("game_over") ||
                        canvas.name.ToLower().Contains("gameovercanvas"))
                    {
                        canvas.gameObject.SetActive(true);
                        Time.timeScale = 0f;
                        Debug.Log("Game Over - Health depleted! (Found canvas: " + canvas.name + ")");
                        break;
                    }
                }
            }
        }
    }
    
    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"Healed {amount}! Health: {currentHealth}/{maxHealth}");
    }
    
    public void SetMaxHealth(int newMaxHealth)
    {
        maxHealth = newMaxHealth;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    public bool IsInvulnerable()
    {
        return isInvulnerable;
    }
    
    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth;
    }
    
    // Method untuk dipanggil dari script lain jika ingin langsung trigger game over
    public void TriggerInstantGameOver()
    {
        currentHealth = 0;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Die();
    }
    
    // Method untuk reset health (berguna untuk respawn/restart)
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isInvulnerable = false;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        // Re-enable player controller if it was disabled
        PlayerController2D playerController = GetComponent<PlayerController2D>();
        if (playerController != null)
        {
            playerController.enabled = true;
        }
    }
}