using UnityEngine;
using System.Collections;

public class HealthSystem : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public bool oneHitKillMode = false;
    
    [Header("Game Over")]
    public Canvas gameOverCanvas;
    
    [Header("Audio")]
    public AudioClip deathSound;
    public AudioSource audioSource;
    
    // Private variables
    private int currentHealth;
    private bool isDead = false;
    private bool gameOverTriggered = false;
    private bool isInvulnerable = false;
    
    // Static variable untuk global game over state
    private static bool globalGameOverActive = false;
    
    // Events
    public System.Action<int, int> OnHealthChanged;
    public System.Action OnDeath;
    
    void Start()
    {
        Debug.Log("🎮 HealthSystem Start called");
        
        // PERBAIKAN: Reset flags di Start()
        isDead = false;
        gameOverTriggered = false;
        globalGameOverActive = false;
        isInvulnerable = false;
        
        currentHealth = oneHitKillMode ? 1 : maxHealth;
        
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        OnHealthChanged?.Invoke(currentHealth, GetMaxHealth());
        
        Debug.Log($"🎮 HealthSystem initialized - Health: {currentHealth}/{GetMaxHealth()}, OneHitKill: {oneHitKillMode}");
        Debug.Log($"🔧 Flags reset - Dead: {isDead}, GameOver: {gameOverTriggered}, Global: {globalGameOverActive}");
    }
    
    // PERBAIKAN: OnEnable untuk safety reset
    void OnEnable()
    {
        Debug.Log($"🔧🔧 HealthSystem OnEnable - Force reset isDead flag from {isDead} to false");
        isDead = false;
        gameOverTriggered = false;
    }
    
    public void TakeDamage(int damage)
    {
        Debug.Log($"⚔️⚔️ HealthSystem TakeDamage called - Damage: {damage}");
        Debug.Log($"🔍 Current state - Invul: {isInvulnerable}, Dead: {isDead}, GameOver: {gameOverTriggered}, Global: {globalGameOverActive}");
        
        // PERBAIKAN: Lebih permissive check
        if (isInvulnerable || isDead)
        {
            Debug.Log($"🚫 Damage ignored - Invulnerable: {isInvulnerable}, Dead: {isDead}");
            return;
        }
        
        // PERBAIKAN: Tambahan check untuk global game over, tapi dengan warning
        if (globalGameOverActive)
        {
            Debug.LogWarning($"⚠️ Global game over active, but taking damage anyway (might be restart bug)");
            // Jangan return, biarkan damage berlanjut untuk debug
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
    
    // PERBAIKAN: Die method dengan debug logging
    void Die()
    {
        Debug.Log($"💀💀 HealthSystem Die() called - isDead check: {isDead}");
        Debug.Log($"🔍 Death state - Dead: {isDead}, GameOver: {gameOverTriggered}, Global: {globalGameOverActive}");
        
        // PERBAIKAN: Hanya cek isDead untuk prevent double death
        if (isDead)
        {
            Debug.Log($"🚫 Death ignored - already dead");
            return;
        }
        
        // Set flags immediately
        isDead = true;
        gameOverTriggered = true;
        globalGameOverActive = true;
        
        Debug.Log($"💀 Death flags set - Dead: {isDead}, GameOver: {gameOverTriggered}, Global: {globalGameOverActive}");
        
        // Play death sound
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
            Debug.Log("🎵 Death sound played");
        }
        
        Debug.Log("💀 HealthSystem - Player died! Game Over!");
        OnDeath?.Invoke();
        
        // Disable player controls immediately
        PlayerController2D playerController = GetComponent<PlayerController2D>();
        if (playerController != null)
        {
            playerController.enabled = false;
            Debug.Log("🎮 Player controller disabled");
        }
        
        // Show game over immediately with protection
        StartCoroutine(GameOverDelay());
    }
    
    System.Collections.IEnumerator GameOverDelay()
    {
        Debug.Log("⏰ HealthSystem - GameOverDelay started - waiting 1 second...");
        yield return new WaitForSeconds(1f); // Short delay for death sound
        
        Debug.Log("⏰ HealthSystem - Game over delay completed, showing game over panel...");
        
        // PERBAIKAN: Coba semua metode untuk show game over
        bool gameOverShown = false;
        
        // Method 1: Canvas langsung
        if (gameOverCanvas != null)
        {
            if (!gameOverCanvas.gameObject.activeInHierarchy)
            {
                gameOverCanvas.gameObject.SetActive(true);
                Debug.Log("✅ HealthSystem - Game Over Canvas activated!");
                gameOverShown = true;
            }
            else
            {
                Debug.Log("ℹ️ HealthSystem - Game Over Canvas already active");
                gameOverShown = true;
            }
        }
        
        // Method 2: GameOverManager fallback
        if (!gameOverShown)
        {
            GameOverManager gameOverManager = FindObjectOfType<GameOverManager>();
            if (gameOverManager != null)
            {
                gameOverManager.ActivateGameOver();
                Debug.Log("✅ HealthSystem - GameOverManager activated!");
                gameOverShown = true;
            }
        }
        
        // Method 3: Search for any Canvas with "GameOver" in name
        if (!gameOverShown)
        {
            Canvas[] allCanvases = FindObjectsOfType<Canvas>();
            foreach (Canvas canvas in allCanvases)
            {
                if (canvas.name.ToLower().Contains("gameover"))
                {
                    canvas.gameObject.SetActive(true);
                    Debug.Log($"✅ HealthSystem - Found and activated canvas: {canvas.name}");
                    gameOverShown = true;
                    break;
                }
            }
        }
        
        if (!gameOverShown)
        {
            Debug.LogError("❌ HealthSystem - No Game Over system found! Please check setup.");
        }
    }
    
    // PERBAIKAN: Public method to reset the game over state (for restart)
    public static void ResetGameOverState()
    {
        globalGameOverActive = false;
        Debug.Log("🔄 HealthSystem - Global game over state reset");
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
    
    // PERBAIKAN: Reset for new game dengan lebih comprehensive reset dan debug logging
    public void ResetHealth()
    {
        Debug.Log($"🔧🔧 HealthSystem ResetHealth called - BEFORE: isDead = {isDead}, Health = {currentHealth}");
        Debug.Log("🔄 HealthSystem ResetHealth called - resetting all flags and health");
        
        isDead = false;
        gameOverTriggered = false;
        globalGameOverActive = false; // PENTING: Reset static variable
        isInvulnerable = false;
        currentHealth = oneHitKillMode ? 1 : maxHealth;
        
        // Re-enable player controller
        PlayerController2D playerController = GetComponent<PlayerController2D>();
        if (playerController != null)
        {
            playerController.enabled = true;
            Debug.Log("🎮 Player controller re-enabled");
        }
        
        // Notify UI
        OnHealthChanged?.Invoke(currentHealth, GetMaxHealth());
        
        Debug.Log($"🔧🔧 HealthSystem ResetHealth complete - AFTER: isDead = {isDead}, Health = {currentHealth}");
        Debug.Log($"🔄 Health system reset - Health: {currentHealth}/{GetMaxHealth()}");
        Debug.Log($"🔧 All flags reset - Dead: {isDead}, GameOver: {gameOverTriggered}, Global: {globalGameOverActive}");
    }
    
    // Additional safety methods
    public void SetInvulnerable(bool invulnerable)
    {
        isInvulnerable = invulnerable;
        Debug.Log($"🛡️ Invulnerability set to: {invulnerable}");
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
    
    // PERBAIKAN: Method untuk force reset semua static variables (untuk debugging)
    [ContextMenu("Force Reset All States")]
    public void ForceResetAllStates()
    {
        isDead = false;
        gameOverTriggered = false;
        globalGameOverActive = false;
        isInvulnerable = false;
        currentHealth = oneHitKillMode ? 1 : maxHealth;
        
        Debug.Log("🔧 FORCE RESET - All states cleared!");
    }
    
    // Method untuk debug current state
    [ContextMenu("Debug Current State")]
    public void DebugCurrentState()
    {
        Debug.Log("=== HEALTH SYSTEM DEBUG ===");
        Debug.Log($"Health: {currentHealth}/{GetMaxHealth()}");
        Debug.Log($"isDead: {isDead}");
        Debug.Log($"gameOverTriggered: {gameOverTriggered}");
        Debug.Log($"globalGameOverActive: {globalGameOverActive}");
        Debug.Log($"isInvulnerable: {isInvulnerable}");
        Debug.Log($"oneHitKillMode: {oneHitKillMode}");
        Debug.Log("========================");
    }
}