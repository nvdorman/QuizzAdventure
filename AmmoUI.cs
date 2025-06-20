using UnityEngine;
using UnityEngine.UI;

public class AmmoUI : MonoBehaviour
{
    [Header("Ammo Display Components")]
    public Text ammoText;
    public Slider ammoSlider; // Optional
    public Image reloadProgress; // Optional reload progress bar
    
    [Header("Ammo Display Settings")]
    public Color normalAmmoColor = Color.white;
    public Color lowAmmoColor = Color.red;
    public Color reloadingColor = Color.yellow;
    public int lowAmmoThreshold = 5;
    
    private PlayerController2D playerController;
    
    void Start()
    {
        // Cari player controller
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerController = player.GetComponent<PlayerController2D>();
            if (playerController == null)
            {
                Debug.LogWarning("PlayerController2D tidak ditemukan pada Player!");
            }
        }
        else
        {
            Debug.LogWarning("Player dengan tag 'Player' tidak ditemukan!");
        }
        
        // Hide reload progress initially
        if (reloadProgress != null)
        {
            reloadProgress.gameObject.SetActive(false);
        }
    }
    
    void Update()
    {
        if (playerController == null) return;
        
        UpdateAmmoDisplay();
        UpdateReloadProgress();
    }
    
    void UpdateAmmoDisplay()
    {
        int currentAmmo = playerController.GetCurrentAmmo();
        int maxAmmo = playerController.GetMaxAmmo();
        bool isReloading = playerController.IsReloading();
        
        // Update ammo text
        if (ammoText != null)
        {
            if (isReloading)
            {
                ammoText.text = "RELOADING...";
                ammoText.color = reloadingColor;
            }
            else
            {
                ammoText.text = $"{currentAmmo}/{maxAmmo}";
                
                // Change color based on ammo count
                if (currentAmmo <= lowAmmoThreshold)
                {
                    ammoText.color = lowAmmoColor;
                }
                else
                {
                    ammoText.color = normalAmmoColor;
                }
            }
        }
        
        // Update ammo slider
        if (ammoSlider != null)
        {
            ammoSlider.maxValue = maxAmmo;
            ammoSlider.value = currentAmmo;
        }
    }
    
    void UpdateReloadProgress()
    {
        if (reloadProgress == null || playerController == null) return;
        
        bool isReloading = playerController.IsReloading();
        
        if (isReloading)
        {
            reloadProgress.gameObject.SetActive(true);
            // Ini memerlukan modification pada PlayerController2D untuk expose reload progress
            // Untuk sementara, kita bisa menggunakan animasi sederhana
        }
        else
        {
            reloadProgress.gameObject.SetActive(false);
        }
    }
}