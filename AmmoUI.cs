using UnityEngine;
using UnityEngine.UI;

public class AmmoUI : MonoBehaviour
{
    [Header("UI References")]
    public Text ammoText;
    public Slider ammoSlider;
    public Text reloadText;
    public Image reloadFill;
    
    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color lowAmmoColor = Color.red;
    public Color reloadingColor = Color.yellow;
    
    private PlayerController2D playerController;
    
    void Start()
    {
        playerController = FindObjectOfType<PlayerController2D>();
        
        if (reloadText != null)
        {
            reloadText.gameObject.SetActive(false);
        }
    }
    
    void Update()
    {
        if (playerController == null) return;
        
        UpdateAmmoDisplay();
        UpdateReloadDisplay();
    }
    
    void UpdateAmmoDisplay()
    {
        int currentAmmo = playerController.GetCurrentAmmo();
        int maxAmmo = playerController.GetMaxAmmo();
        
        // Update text
        if (ammoText != null)
        {
            ammoText.text = $"{currentAmmo}/{maxAmmo}";
            
            // Change color based on ammo
            if (currentAmmo <= maxAmmo * 0.3f)
            {
                ammoText.color = lowAmmoColor;
            }
            else
            {
                ammoText.color = normalColor;
            }
        }
        
        // Update slider
        if (ammoSlider != null)
        {
            ammoSlider.maxValue = maxAmmo;
            ammoSlider.value = currentAmmo;
        }
    }
    
    void UpdateReloadDisplay()
    {
        bool isReloading = playerController.IsReloading();
        
        if (reloadText != null)
        {
            reloadText.gameObject.SetActive(isReloading);
            if (isReloading)
            {
                reloadText.text = "RELOADING...";
                reloadText.color = reloadingColor;
            }
        }
        
        // You can add reload progress bar here if needed
    }
}