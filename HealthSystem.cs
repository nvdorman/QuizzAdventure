using UnityEngine;
using System.Collections;

public class HealthSystem : MonoBehaviour
{
    [Header("One Hit Kill Settings")]
    public bool oneHitKillMode = true; // New setting for instant death
    
    [Header("Health Settings")]
    public int maxHealth = 1; // Set to 1 for one hit kill
    public int currentHealth;
    
    [Header("Audio")]
    public AudioClip deathSound;
    
    [Header("Game Over Settings")]
    public Canvas gameOverCanvas;
    
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private bool isInvulnerable = false; // Add this field
    
    // Events - Add this event
    public System.Action<int, int> OnHealthChanged; // current, max
    public System.Action OnDeath;
    
    void Start()
    {
        currentHealth = oneHitKillMode ? 1 : maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
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
        
        // Notify UI of initial health
        OnHealthChanged?.Invoke(currentHealth, GetMaxHealth());
        
        Debug.Log("💀 One Hit Kill Mode ACTIVATED - Be careful!");
    }
    
    public void TakeDamage(int damage)
    {
        if (isInvulnerable || currentHealth <= 0) return;
        
        if (oneHitKillMode)
        {
            Debug.Log("💀 ONE HIT KILL - INSTANT DEATH!");
            currentHealth = 0;
            OnHealthChanged?.Invoke(currentHealth, GetMaxHealth()); // Notify UI
            Die();
        }
        else
        {
            currentHealth -= damage;
            currentHealth = Mathf.Max(0, currentHealth);
            
            OnHealthChanged?.Invoke(currentHealth, GetMaxHealth()); // Notify UI
            
            Debug.Log($"Health: {currentHealth}/{maxHealth}");
            
            if (currentHealth <= 0)
            {
                Die();
            }
        }
    }
    
    void Die()
    {
        // Play death sound
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
        
        Debug.Log("💀 Player died! Game Over!");
        OnDeath?.Invoke();
        
        // Disable player controls immediately
        PlayerController2D playerController = GetComponent<PlayerController2D>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        // Show game over immediately
        StartCoroutine(GameOverDelay());
    }
    
    System.Collections.IEnumerator GameOverDelay()
    {
        yield return new WaitForSeconds(1f); // Short delay for death sound
        
        // Show game over canvas
        if (gameOverCanvas != null)
        {
            gameOverCanvas.gameObject.SetActive(true);
            Debug.Log("✅ Game Over Canvas activated!");
        }
        else
        {
            // Fallback - try to find GameOverManager
            GameOverManager gameOverManager = FindObjectOfType<GameOverManager>();
            if (gameOverManager != null)
            {
                gameOverManager.ActivateGameOver();
                Debug.Log("✅ GameOverManager activated!");
            }
            else
            {
                Debug.LogError("❌ No Game Over system found!");
            }
        }
    }
    
    // Add IsInvulnerable method for compatibility
    public bool IsInvulnerable()
    {
        return isInvulnerable;
    }
    
    // Helper methods
    public int GetCurrentHealth() { return currentHealth; }
    public int GetMaxHealth() { return oneHitKillMode ? 1 : maxHealth; }
    public bool IsDead() { return currentHealth <= 0; }
    
    // Reset for new game
    public void ResetHealth()
    {
        currentHealth = oneHitKillMode ? 1 : maxHealth;
        isInvulnerable = false;
        
        // Re-enable player controller
        PlayerController2D playerController = GetComponent<PlayerController2D>();
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        
        // Notify UI
        OnHealthChanged?.Invoke(currentHealth, GetMaxHealth());
        
        Debug.Log("🔄 Health system reset - ready for new game");
    }
    
    // Additional compatibility methods
    public void SetInvulnerable(bool invulnerable)
    {
        isInvulnerable = invulnerable;
    }
    
    public void Heal(int amount)
    {
        if (oneHitKillMode) return; // No healing in one hit kill mode
        
        currentHealth += amount;
        currentHealth = Mathf.Min(GetMaxHealth(), currentHealth);
        OnHealthChanged?.Invoke(currentHealth, GetMaxHealth());
        Debug.Log($"Healed {amount}! Health: {currentHealth}/{GetMaxHealth()}");
    }
}