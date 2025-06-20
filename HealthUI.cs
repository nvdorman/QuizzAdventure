using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [Header("UI References")]
    public Slider healthSlider;
    public Text healthText;
    public Image healthFill;

    [Header("Color Settings")]
    public Color fullHealthColor = Color.green;
    public Color midHealthColor = Color.yellow;
    public Color lowHealthColor = Color.red;

    [Header("Thresholds")]
    public float lowHealthThreshold = 0.3f;
    public float midHealthThreshold = 0.7f;

    private HealthSystem playerHealth;

    void Start()
    {
        // Find player health system
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += UpdateHealthUI;
                // Initialize UI
                UpdateHealthUI(playerHealth.currentHealth, playerHealth.maxHealth);
            }
        }
    }

    void UpdateHealthUI(int currentHealth, int maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (healthText != null)
        {
            healthText.text = $"{currentHealth}/{maxHealth}";
        }

        if (healthFill != null)
        {
            float healthPercentage = (float)currentHealth / maxHealth;

            if (healthPercentage <= lowHealthThreshold)
            {
                healthFill.color = lowHealthColor;
            }
            else if (healthPercentage <= midHealthThreshold)
            {
                healthFill.color = midHealthColor;
            }
            else
            {
                healthFill.color = fullHealthColor;
            }
        }
    }

    void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthUI;
        }
    }
}
