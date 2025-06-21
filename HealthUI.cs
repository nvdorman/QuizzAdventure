using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [Header("Health UI Components")]
    public Slider healthSlider;
    public Text healthText;
    public Image healthFill;
    
    [Header("Health Colors")]
    public Color fullHealthColor = Color.green;
    public Color lowHealthColor = Color.red;
    
    private HealthSystem playerHealth;
    private float targetHealth;
    private float currentDisplayHealth;
    
    void Start()
    {
        // In one-hit kill mode, hide health UI
        if (ShouldHideHealthUI())
        {
            HideHealthUI();
            return;
        }
        
        // Cari player health system
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                // Check if OnHealthChanged event exists
                if (playerHealth.OnHealthChanged != null)
                {
                    // Subscribe ke health change events
                    playerHealth.OnHealthChanged += UpdateHealthDisplay;
                }
                else
                {
                    Debug.LogWarning("⚠️ HealthSystem.OnHealthChanged event not found! Using fallback update method.");
                    // Use fallback method
                    InvokeRepeating(nameof(UpdateHealthFallback), 0f, 0.1f);
                }
                
                // Set initial values
                SetupHealthBar(playerHealth.GetCurrentHealth(), playerHealth.GetMaxHealth());
            }
            else
            {
                Debug.LogWarning("HealthSystem tidak ditemukan pada Player!");
                HideHealthUI();
            }
        }
        else
        {
            Debug.LogWarning("Player dengan tag 'Player' tidak ditemukan!");
            HideHealthUI();
        }
    }
    
    bool ShouldHideHealthUI()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            HealthSystem health = player.GetComponent<HealthSystem>();
            if (health != null)
            {
                // Check if it's one-hit kill mode (max health = 1)
                return health.GetMaxHealth() <= 1;
            }
        }
        return false;
    }
    
    void HideHealthUI()
    {
        // Hide health UI components since it's one-hit kill
        if (healthSlider != null)
        {
            healthSlider.gameObject.SetActive(false);
        }
        
        if (healthText != null)
        {
            healthText.gameObject.SetActive(false);
        }
        
        if (healthFill != null)
        {
            healthFill.gameObject.SetActive(false);
        }
        
        // Hide heart container if it exists
        Transform heartContainer = transform.Find("HeartContainer");
        if (heartContainer != null)
        {
            heartContainer.gameObject.SetActive(false);
        }
        
        Debug.Log("💀 Health UI hidden - One Hit Kill Mode");
    }
    
    void UpdateHealthFallback()
    {
        if (playerHealth != null)
        {
            UpdateHealthDisplay(playerHealth.GetCurrentHealth(), playerHealth.GetMaxHealth());
        }
    }
    
    void SetupHealthBar(int currentHealth, int maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
        
        targetHealth = currentHealth;
        currentDisplayHealth = currentHealth;
        
        UpdateHealthColor();
        UpdateHealthText();
    }
    
    void UpdateHealthDisplay(int currentHealth, int maxHealth)
    {
        targetHealth = currentHealth;
        
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
        }
        
        UpdateHealthColor();
        UpdateHealthText();
    }
    
    void Update()
    {
        if (playerHealth == null) return;
        
        // Smooth health bar animation
        if (Mathf.Abs(currentDisplayHealth - targetHealth) > 0.1f)
        {
            currentDisplayHealth = Mathf.Lerp(currentDisplayHealth, targetHealth, Time.deltaTime * 5f);
            
            if (healthSlider != null)
            {
                healthSlider.value = currentDisplayHealth;
            }
        }
    }
    
    void UpdateHealthColor()
    {
        if (healthFill != null && playerHealth != null)
        {
            float healthPercentage = (float)playerHealth.GetCurrentHealth() / playerHealth.GetMaxHealth();
            healthFill.color = Color.Lerp(lowHealthColor, fullHealthColor, healthPercentage);
        }
    }
    
    void UpdateHealthText()
    {
        if (healthText != null && playerHealth != null)
        {
            healthText.text = $"{playerHealth.GetCurrentHealth()}/{playerHealth.GetMaxHealth()}";
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (playerHealth != null && playerHealth.OnHealthChanged != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthDisplay;
        }
        
        // Cancel invoke if using fallback
        CancelInvoke(nameof(UpdateHealthFallback));
    }
}