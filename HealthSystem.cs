using UnityEngine;
using System.Collections;

public class HealthSystem : MonoBehaviour
{
    [Header("One Hit Kill Settings")]
    public bool oneHitKillMode = true;
    
    [Header("Health Settings")]
    public int maxHealth = 1;
    public int currentHealth;
    
    [Header("Audio")]
    public AudioClip deathSound;
    
    [Header("Game Over Settings")]
    public Canvas gameOverCanvas;
    
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private bool isInvulnerable = false;
    private bool isDead = false; // Prevent multiple death calls
    private bool gameOverTriggered = false; // Prevent multiple game over triggers
    
    // Static variable to prevent multiple game overs
    private static bool globalGameOverActive = false;
    
    // Events
    public System.Action<int, int> OnHealthChanged;
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
        
        // Reset flags
        isDead = false;
        gameOverTriggered = false;
        
        // Notify UI of initial health
        OnHealthChanged?.Invoke(currentHealth, GetMaxHealth());
        
        Debug.Log("💀 One Hit Kill Mode ACTIVATED - Be careful!");
    }
    
    void OnEnable()
    {
        // Reset death flags when object is enabled
        isDead = false;
        gameOverTriggered = false;
    }
    
    public void TakeDamage(int damage)
    {
        // Prevent multiple damage calls if already dead or game over triggered
        if (isInvulnerable || isDead || gameOverTriggered || globalGameOverActive)
        {
            Debug.Log($"🚫 Damage ignored - Dead: {isDead}, GameOver: {gameOverTriggered}, Global: {globalGameOverActive}");
            return;
        }
        
        if (oneHitKillMode)
        {
            Debug.Log("💀 ONE HIT KILL - INSTANT DEATH!");
            currentHealth = 0;
            OnHealthChanged?.Invoke(currentHealth, GetMaxHealth());
            Die();
        }
        else
        {
            currentHealth -= damage;
            currentHealth = Mathf.Max(0, currentHealth);
            
            OnHealthChanged?.Invoke(currentHealth, GetMaxHealth());
            
            Debug.Log($"Health: {currentHealth}/{maxHealth}");
            
            if (currentHealth <= 0)
            {
                Die();
            }
        }
    }
    
    void Die()
    {
        // Multiple protection against double death
        if (isDead || gameOverTriggered || globalGameOverActive)
        {
            Debug.Log($"🚫 Death ignored - already processed");
            return;
        }
        
        // Set flags immediately
        isDead = true;
        gameOverTriggered = true;
        globalGameOverActive = true;
        
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
        
        // Show game over immediately with protection
        StartCoroutine(GameOverDelay());
    }
    
    System.Collections.IEnumerator GameOverDelay()
    {
        yield return new WaitForSeconds(1f); // Short delay for death sound
        
        // Double check that game over hasn't been triggered already
        if (!gameOverTriggered)
        {
            Debug.Log("🚫 Game over delay cancelled - already triggered");
            yield break;
        }
        
        Debug.Log("⏰ Game over delay completed, showing game over panel...");
        
        // Show game over canvas
        if (gameOverCanvas != null && !gameOverCanvas.gameObject.activeInHierarchy)
        {
            gameOverCanvas.gameObject.SetActive(true);
            Debug.Log("✅ Game Over Canvas activated!");
        }
        else if (gameOverCanvas != null && gameOverCanvas.gameObject.activeInHierarchy)
        {
            Debug.Log("ℹ️ Game Over Canvas already active");
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
    
    // Public method to reset the game over state (for restart)
    public static void ResetGameOverState()
    {
        globalGameOverActive = false;
        Debug.log("🔄 Global game over state reset");
    }
    
    // Add IsInvulnerable method for compatibility
    public bool IsInvulnerable()
    {
        return isInvulnerable || isDead || gameOverTriggered;
    }
    
    // Helper methods
    public int GetCurrentHealth() { return currentHealth; }
    public int GetMaxHealth() { return oneHitKillMode ? 1 : maxHealth; }
    public bool IsDead() { return isDead; }
    
    // Reset for new game
    public void ResetHealth()
    {
        isDead = false;
        gameOverTriggered = false;
        globalGameOverActive = false;
        isInvulnerable = false;
        currentHealth = oneHitKillMode ? 1 : maxHealth;
        
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
    
    // Additional safety methods
    public void SetInvulnerable(bool invulnerable)
    {
        isInvulnerable = invulnerable;
    }
    
    public void Heal(int amount)
    {
        if (oneHitKillMode || isDead) return;
        
        currentHealth += amount;
        currentHealth = Mathf.Min(GetMaxHealth(), currentHealth);
        OnHealthChanged?.Invoke(currentHealth, GetMaxHealth());
        Debug.Log($"Healed {amount}! Health: {currentHealth}/{GetMaxHealth()}");
    }
    
    // Method untuk external scripts untuk trigger instant game over
    public void TriggerInstantGameOver()
    {
        if (!isDead && !gameOverTriggered)
        {
            currentHealth = 0;
            OnHealthChanged?.Invoke(currentHealth, GetMaxHealth());
            Die();
        }
    }
}