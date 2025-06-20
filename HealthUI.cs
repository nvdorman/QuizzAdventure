using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [Header("Health Bar Components")]
    public Slider healthSlider;
    public Image healthFill;
    public Text healthText; // Optional
    
    [Header("Health Bar Colors")]
    public Color fullHealthColor = Color.green;
    public Color mediumHealthColor = Color.yellow;
    public Color lowHealthColor = Color.red;
    
    [Header("Animation")]
    public bool animateHealthBar = true;
    public float animationSpeed = 5f;
    
    private HealthSystem playerHealth;
    private float targetHealth;
    private float currentDisplayHealth;
    
    void Start()
    {
        // Cari player health system
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                // Subscribe ke health change events
                playerHealth.OnHealthChanged += UpdateHealthDisplay;
                
                // Set initial values
                SetupHealthBar(playerHealth.currentHealth, playerHealth.maxHealth);
            }
            else
            {
                Debug.LogWarning("HealthSystem tidak ditemukan pada Player!");
            }
        }
        else
        {
            Debug.LogWarning("Player dengan tag 'Player' tidak ditemukan!");
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
            
            if (!animateHealthBar)
            {
                healthSlider.value = currentHealth;
                currentDisplayHealth = currentHealth;
            }
        }
        
        UpdateHealthColor();
        UpdateHealthText();
    }
    
    void Update()
    {
        if (animateHealthBar && healthSlider != null)
        {
            // Smooth animation untuk health bar
            currentDisplayHealth = Mathf.Lerp(currentDisplayHealth, targetHealth, Time.deltaTime * animationSpeed);
            healthSlider.value = currentDisplayHealth;
            
            if (Mathf.Abs(currentDisplayHealth - targetHealth) < 0.1f)
            {
                currentDisplayHealth = targetHealth;
                healthSlider.value = targetHealth;
            }
        }
    }
    
    void UpdateHealthColor()
    {
        if (healthFill == null || healthSlider == null) return;
        
        float healthPercentage = healthSlider.value / healthSlider.maxValue;
        
        Color targetColor;
        if (healthPercentage > 0.6f)
        {
            targetColor = fullHealthColor;
        }
        else if (healthPercentage > 0.3f)
        {
            targetColor = mediumHealthColor;
        }
        else
        {
            targetColor = lowHealthColor;
        }
        
        healthFill.color = targetColor;
    }
    
    void UpdateHealthText()
    {
        if (healthText != null && playerHealth != null)
        {
            healthText.text = $"{Mathf.Ceil(currentDisplayHealth)}/{playerHealth.maxHealth}";
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe dari events
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthDisplay;
        }
    }
}