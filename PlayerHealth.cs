using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHearts = 3;
    public int hitsPerHeart = 2; // 2 hits per heart (full → half → empty)
    public float invulnerabilityTime = 1.5f;
    
    [Header("Health Sprites")]
    public Sprite fullHeartSprite;
    public Sprite halfHeartSprite;
    public Sprite emptyHeartSprite;
    
    [Header("UI Settings")]
    public Transform heartContainer; // Parent object untuk heart UI
    public GameObject heartPrefab; // Prefab untuk single heart UI
    public bool useWorldSpaceUI = false; // true = world space, false = screen space
    
    [Header("Damage Effects")]
    public GameObject damageEffect;
    public AudioClip damageSound;
    public AudioClip deathSound;
    public AudioClip healSound;
    
    [Header("Visual Effects")]
    public Color damageFlashColor = Color.red;
    public float flashDuration = 0.2f;
    public bool shakeOnDamage = true;
    public float shakeIntensity = 0.1f;
    public float shakeDuration = 0.3f;
    
    [Header("Death Settings")]
    public bool useGameOverManager = true;
    public GameObject gameOverCanvas;
    
    // Private variables
    private int currentHealth;
    private bool isInvulnerable = false;
    private bool isDead = false; // Add flag to prevent multiple death calls
    private Image[] heartImages;
    private AudioSource audioSource;
    private SpriteRenderer playerRenderer;
    private Color originalColor;
    private PlayerController2D playerController;
    private Camera playerCamera;
    
    // Events
    public System.Action<int, int> OnHealthChanged; // currentHealth, maxHealth
    public System.Action OnPlayerDeath;
    public System.Action<int> OnDamageTaken; // damage amount
    
    void Start()
    {
        InitializeComponents();
        InitializeHealth();
        CreateHeartUI();
    }
    
    void InitializeComponents()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        playerRenderer = GetComponent<SpriteRenderer>();
        if (playerRenderer != null)
        {
            originalColor = playerRenderer.color;
        }
        
        playerController = GetComponent<PlayerController2D>();
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = FindObjectOfType<Camera>();
        }
    }
    
    void InitializeHealth()
    {
        currentHealth = maxHearts * hitsPerHeart;
        isDead = false; // Reset death flag
        
        // Setup heart container if not assigned
        if (heartContainer == null)
        {
            SetupHeartContainer();
        }
    }
    
    void SetupHeartContainer()
    {
        if (useWorldSpaceUI)
        {
            // Create world space UI above player
            GameObject containerObj = new GameObject("HeartContainer");
            containerObj.transform.SetParent(transform);
            containerObj.transform.localPosition = Vector3.up * 2f;
            
            Canvas canvas = containerObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;
            containerObj.transform.localScale = Vector3.one * 0.01f;
            
            GameObject heartsParent = new GameObject("Hearts");
            heartsParent.transform.SetParent(containerObj.transform);
            heartsParent.transform.localPosition = Vector3.zero;
            
            // Add horizontal layout group
            HorizontalLayoutGroup layoutGroup = heartsParent.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = 10f;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            
            heartContainer = heartsParent.transform;
        }
        else
        {
            // Find or create screen space canvas
            Canvas screenCanvas = FindObjectOfType<Canvas>();
            if (screenCanvas == null)
            {
                GameObject canvasObj = new GameObject("UI Canvas");
                screenCanvas = canvasObj.AddComponent<Canvas>();
                screenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            
            GameObject containerObj = new GameObject("HeartContainer");
            containerObj.transform.SetParent(screenCanvas.transform);
            
            RectTransform rectTransform = containerObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = new Vector2(20, -20);
            
            // Add horizontal layout group
            HorizontalLayoutGroup layoutGroup = containerObj.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = 5f;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            
            heartContainer = containerObj.transform;
        }
    }
    
    void CreateHeartUI()
    {
        // Clear existing hearts
        foreach (Transform child in heartContainer)
        {
            DestroyImmediate(child.gameObject);
        }
        
        // Create heart images array
        heartImages = new Image[maxHearts];
        
        for (int i = 0; i < maxHearts; i++)
        {
            GameObject heartObj;
            
            if (heartPrefab != null)
            {
                heartObj = Instantiate(heartPrefab, heartContainer);
            }
            else
            {
                // Create default heart UI
                heartObj = new GameObject($"Heart_{i}");
                heartObj.transform.SetParent(heartContainer);
                
                Image heartImage = heartObj.AddComponent<Image>();
                heartImage.sprite = fullHeartSprite;
                heartImage.preserveAspect = true;
                
                if (useWorldSpaceUI)
                {
                    RectTransform rectTransform = heartImage.rectTransform;
                    rectTransform.sizeDelta = new Vector2(100, 100);
                }
                else
                {
                    RectTransform rectTransform = heartImage.rectTransform;
                    rectTransform.sizeDelta = new Vector2(50, 50);
                }
            }
            
            heartImages[i] = heartObj.GetComponent<Image>();
            if (heartImages[i] == null)
            {
                heartImages[i] = heartObj.AddComponent<Image>();
            }
            
            heartImages[i].sprite = fullHeartSprite;
        }
        
        UpdateHeartUI();
    }
    
    void UpdateHeartUI()
    {
        for (int i = 0; i < maxHearts; i++)
        {
            if (heartImages[i] == null) continue;
            
            int heartIndex = i;
            int healthForThisHeart = Mathf.Max(0, currentHealth - (heartIndex * hitsPerHeart));
            
            if (healthForThisHeart >= hitsPerHeart)
            {
                // Full heart
                heartImages[i].sprite = fullHeartSprite;
            }
            else if (healthForThisHeart > 0)
            {
                // Half heart
                heartImages[i].sprite = halfHeartSprite;
            }
            else
            {
                // Empty heart
                heartImages[i].sprite = emptyHeartSprite;
            }
        }
    }
    
    public void TakeDamage(int damage = 1)
    {
        if (isInvulnerable || currentHealth <= 0 || isDead) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        // Trigger events
        OnDamageTaken?.Invoke(damage);
        OnHealthChanged?.Invoke(currentHealth, maxHearts * hitsPerHeart);
        
        // Update UI
        UpdateHeartUI();
        
        // Play damage sound
        if (audioSource != null && damageSound != null)
        {
            audioSource.PlayOneShot(damageSound);
        }
        
        // Visual effects
        StartCoroutine(DamageEffects());
        
        // Start invulnerability
        StartCoroutine(InvulnerabilityCoroutine());
        
        // Check if dead
        if (currentHealth <= 0)
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
    
    void Die()
    {
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
        
        Debug.Log("Player died!");
    }
    
    IEnumerator ShowGameOverWithDelay()
    {
        yield return new WaitForSeconds(1f); // Wait for death effects
        
        // Try multiple ways to show game over
        bool gameOverShown = false;
        
        // Method 1: Try GameOverManager
        GameOverManager gameOverManager = FindObjectOfType<GameOverManager>();
        if (gameOverManager != null)
        {
            gameOverManager.ActivateGameOver();
            gameOverShown = true;
        }
        
        // Method 2: Try assigned canvas
        if (!gameOverShown && gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(true);
            GameOverManager canvasManager = gameOverCanvas.GetComponent<GameOverManager>();
            if (canvasManager != null)
            {
                canvasManager.ActivateGameOver();
            }
            else
            {
                Time.timeScale = 0f; // Pause game if no manager
            }
            gameOverShown = true;
        }
        
        // Method 3: Find any game over canvas in scene
        if (!gameOverShown)
        {
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
                    Debug.Log($"Found and activated game over canvas: {canvas.name}");
                    break;
                }
            }
        }
        
        if (!gameOverShown)
        {
            Debug.LogError("No game over system found! Please assign gameOverCanvas or add GameOverManager to scene.");
        }
    }
    
    // Public method to reset player health (useful for respawn/restart)
    public void ResetHealth()
    {
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
        
        Debug.Log("Player health reset!");
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
    }
    
    // Method untuk testing di editor
    [ContextMenu("Test Damage")]
    void TestDamage()
    {
        TakeDamage(1);
    }
    
    [ContextMenu("Test Heal")]
    void TestHeal()
    {
        Heal(1);
    }
    
    [ContextMenu("Test Death")]
    void TestDeath()
    {
        currentHealth = 1;
        TakeDamage(1);
    }
    
    [ContextMenu("Reset Health")]
    void TestResetHealth()
    {
        ResetHealth();
    }
}