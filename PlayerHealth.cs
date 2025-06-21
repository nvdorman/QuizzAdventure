using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHearts = 3;
    public int hitsPerHeart = 4;
    
    [Header("UI")]
    public Transform heartContainer;
    public GameObject heartPrefab;
    public Canvas gameOverCanvas;
    
    [Header("Effects")]
    public GameObject damageEffect;
    public Color damageFlashColor = Color.red;
    public float flashDuration = 0.1f;
    public float invulnerabilityTime = 1.5f;
    
    [Header("Camera Shake")]
    public bool shakeOnDamage = true;
    public float shakeDuration = 0.2f;
    public float shakeIntensity = 0.3f;
    public Camera playerCamera;
    
    [Header("Audio")]
    public AudioClip damageSound;
    public AudioClip healSound;
    public AudioClip deathSound;
    [Range(0f, 1f)]
    public float soundVolume = 0.7f;
    
    // Private variables
    private int currentHealth;
    private bool isDead = false;
    private bool isInvulnerable = false;
    private SpriteRenderer playerRenderer;
    private Color originalColor;
    private PlayerController2D playerController;
    private AudioSource audioSource;
    
    // Events
    public System.Action<int, int> OnHealthChanged;
    public System.Action OnPlayerDeath;
    
    void Start()
    {
        currentHealth = maxHearts * hitsPerHeart;
        
        playerRenderer = GetComponent<SpriteRenderer>();
        if (playerRenderer != null)
        {
            originalColor = playerRenderer.color;
        }
        
        playerController = GetComponent<PlayerController2D>();
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.volume = soundVolume;
        
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        
        CreateHeartUI();
        Debug.Log($"PlayerHealth initialized - Health: {currentHealth}/{maxHearts * hitsPerHeart}");
    }
    
    // PERBAIKAN: OnEnable untuk safety reset
    void OnEnable()
    {
        Debug.Log($"🔧🔧 PlayerHealth OnEnable - Force reset isDead flag from {isDead} to false");
        isDead = false; // Safety reset setiap kali object enabled
        isInvulnerable = false;
    }
    
    void CreateHeartUI()
    {
        if (heartContainer == null || heartPrefab == null) return;
        
        // Clear existing hearts
        foreach (Transform child in heartContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Create heart UI
        for (int i = 0; i < maxHearts; i++)
        {
            GameObject heart = Instantiate(heartPrefab, heartContainer);
            // Heart prefab should have multiple sprites for different states
        }
        
        UpdateHeartUI();
    }
    
    void UpdateHeartUI()
    {
        if (heartContainer == null) return;
        
        for (int i = 0; i < heartContainer.childCount; i++)
        {
            Transform heart = heartContainer.GetChild(i);
            Image heartImage = heart.GetComponent<Image>();
            
            if (heartImage != null)
            {
                int heartValue = (i + 1) * hitsPerHeart;
                int remainingHits = Mathf.Max(0, currentHealth - (i * hitsPerHeart));
                
                if (remainingHits <= 0)
                {
                    heartImage.color = Color.black; // Empty heart
                }
                else if (remainingHits < hitsPerHeart)
                {
                    heartImage.color = Color.yellow; // Damaged heart
                }
                else
                {
                    heartImage.color = Color.red; // Full heart
                }
            }
        }
    }
    
    public void TakeDamage(int damage = 1)
    {
        Debug.Log($"⚔️⚔️ PlayerHealth TakeDamage called - Damage: {damage}, isDead: {isDead}, isInvulnerable: {isInvulnerable}");
        
        if (isInvulnerable || isDead)
        {
            Debug.Log($"🚫 Damage ignored - Invulnerable: {isInvulnerable}, Dead: {isDead}");
            return;
        }
        
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        UpdateHeartUI();
        OnHealthChanged?.Invoke(currentHealth, maxHearts * hitsPerHeart);
        
        // Play damage sound
        if (audioSource != null && damageSound != null)
        {
            audioSource.PlayOneShot(damageSound);
        }
        
        // Start damage effects
        StartCoroutine(DamageEffects());
        
        // Start invulnerability
        if (!isDead)
        {
            StartCoroutine(InvulnerabilityCoroutine());
        }
        
        // Check for death
        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
        
        Debug.Log($"Player took {damage} damage! Health: {currentHealth}/{maxHearts * hitsPerHeart}");
    }
    
    IEnumerator DamageEffects()
    {
        // Damage effect particle
        if (damageEffect != null)
        {
            GameObject effect = Instantiate(damageEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // Flash effect
        if (playerRenderer != null)
        {
            playerRenderer.color = damageFlashColor;
            yield return new WaitForSeconds(flashDuration);
            playerRenderer.color = originalColor;
        }
        
        // Camera shake
        if (shakeOnDamage && playerCamera != null)
        {
            StartCoroutine(CameraShake());
        }
    }
    
    IEnumerator CameraShake()
    {
        Vector3 originalPosition = playerCamera.transform.position;
        float elapsed = 0f;
        
        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeIntensity;
            float y = Random.Range(-1f, 1f) * shakeIntensity;
            
            playerCamera.transform.position = originalPosition + new Vector3(x, y, 0);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        playerCamera.transform.position = originalPosition;
    }
    
    IEnumerator InvulnerabilityCoroutine()
    {
        isInvulnerable = true;
        
        // Flashing effect during invulnerability
        float flashInterval = 0.1f;
        float elapsed = 0f;
        
        while (elapsed < invulnerabilityTime)
        {
            if (playerRenderer != null)
            {
                playerRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.5f);
                yield return new WaitForSeconds(flashInterval);
                playerRenderer.color = originalColor;
                yield return new WaitForSeconds(flashInterval);
            }
            
            elapsed += flashInterval * 2;
        }
        
        if (playerRenderer != null)
        {
            playerRenderer.color = originalColor;
        }
        
        isInvulnerable = false;
    }
    
    public void Heal(int healAmount = 1)
    {
        if (currentHealth >= maxHearts * hitsPerHeart || isDead) return;
        
        currentHealth += healAmount;
        currentHealth = Mathf.Min(maxHearts * hitsPerHeart, currentHealth);
        
        UpdateHeartUI();
        OnHealthChanged?.Invoke(currentHealth, maxHearts * hitsPerHeart);
        
        // Play heal sound
        if (audioSource != null && healSound != null)
        {
            audioSource.PlayOneShot(healSound);
        }
        
        Debug.Log($"Player healed {healAmount}! Health: {currentHealth}/{maxHearts * hitsPerHeart}");
    }
    
    // PERBAIKAN: Tambah debug log untuk track Die() calls
    void Die()
    {
        Debug.Log($"💀💀 PlayerHealth Die() called - isDead check: {isDead}");
        if (isDead) return; // Prevent multiple death calls
        
        isDead = true;
        OnPlayerDeath?.Invoke();
        
        // Play death sound
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
        
        // Disable player controls
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        // Disable rigidbody
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
        }
        
        // Show game over with delay to allow death sound/animation
        StartCoroutine(ShowGameOverWithDelay());
        
        Debug.Log("💀 PlayerHealth - Player died!");
    }
    
    IEnumerator ShowGameOverWithDelay()
    {
        yield return new WaitForSeconds(1f); // Wait for death effects
    
        Debug.Log("💀 PlayerHealth - Starting game over sequence...");
        
        // PERBAIKAN: Multi-layered approach untuk memastikan game over muncul
        bool gameOverShown = false;
        
        // Method 1: Prioritas utama - GameOverManager
        GameOverManager gameOverManager = FindObjectOfType<GameOverManager>();
        if (gameOverManager != null)
        {
            Debug.Log("✅ PlayerHealth - GameOverManager found, activating...");
            gameOverManager.ActivateGameOver();
            gameOverShown = true;
        }
        
        // Method 2: Canvas yang di-assign
        if (!gameOverShown && gameOverCanvas != null)
        {
            Debug.Log("🔄 PlayerHealth - Using assigned canvas...");
            gameOverCanvas.gameObject.SetActive(true); // PERBAIKAN: Tambah .gameObject
            
            GameOverManager canvasManager = gameOverCanvas.GetComponent<GameOverManager>();
            if (canvasManager != null)
            {
                canvasManager.ActivateGameOver();
            }
            else
            {
                Time.timeScale = 0f; // Pause jika tidak ada manager
            }
            gameOverShown = true;
        }
        
        // Method 3: Cari canvas game over di scene
        if (!gameOverShown)
        {
            Debug.Log("🔍 PlayerHealth - Searching for game over canvas...");
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            foreach (Canvas canvas in canvases)
            {
                if (canvas.name.ToLower().Contains("gameover") || 
                    canvas.name.ToLower().Contains("game_over") ||
                    canvas.name.ToLower().Contains("gameovercanvas"))
                {
                    canvas.gameObject.SetActive(true);
                    Time.timeScale = 0f;
                    gameOverShown = true;
                    Debug.Log($"✅ PlayerHealth - Found and activated canvas: {canvas.name}");
                    break;
                }
            }
        }
        
        if (!gameOverShown)
        {
            Debug.LogError("❌ PlayerHealth - CRITICAL ERROR: No game over system found!");
        }
        else
        {
            Debug.Log("✅ PlayerHealth - Game over displayed successfully!");
        }
    }
    
    // PERBAIKAN: ResetHealth dengan debug logging
    public void ResetHealth()
    {
        Debug.Log($"🔧🔧 PlayerHealth ResetHealth called - BEFORE: isDead = {isDead}, Health = {currentHealth}");
        
        isDead = false;
        currentHealth = maxHearts * hitsPerHeart;
        isInvulnerable = false;
        
        UpdateHeartUI();
        OnHealthChanged?.Invoke(currentHealth, maxHearts * hitsPerHeart);
        
        // Re-enable player controls
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        
        // Re-enable rigidbody
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }
        
        // Reset sprite color
        if (playerRenderer != null)
        {
            playerRenderer.color = originalColor;
        }
        
        Debug.Log($"🔧🔧 PlayerHealth ResetHealth complete - AFTER: isDead = {isDead}, Health = {currentHealth}");
        Debug.Log("✅ PlayerHealth reset complete!");
    }
    
    // Public methods for external use
    public int GetCurrentHealth() { return currentHealth; }
    public int GetMaxHealth() { return maxHearts * hitsPerHeart; }
    public float GetHealthPercentage() { return (float)currentHealth / (maxHearts * hitsPerHeart); }
    public bool IsInvulnerable() { return isInvulnerable; }
    public bool IsDead() { return isDead; }
    
    public void SetMaxHearts(int newMaxHearts)
    {
        maxHearts = newMaxHearts;
        currentHealth = Mathf.Min(currentHealth, maxHearts * hitsPerHeart);
        CreateHeartUI();
    }
    
    public void FullHeal()
    {
        if (isDead) return;
        
        currentHealth = maxHearts * hitsPerHeart;
        UpdateHeartUI();
        OnHealthChanged?.Invoke(currentHealth, maxHearts * hitsPerHeart);
        
        Debug.Log("Player fully healed!");
    }
}