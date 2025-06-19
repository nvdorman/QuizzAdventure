using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    public int healAmount = 1;
    public bool destroyOnPickup = true;
    public string playerTag = "Player";
    
    [Header("Effects")]
    public GameObject pickupEffect;
    public AudioClip pickupSound;
    
    [Header("Animation")]
    public float bobSpeed = 2f;
    public float bobHeight = 0.5f;
    public float rotateSpeed = 90f;
    
    private Vector3 startPosition;
    private AudioSource audioSource;
    
    void Start()
    {
        startPosition = transform.position;
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    void Update()
    {
        // Floating animation
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
        
        // Rotation animation
        transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null && playerHealth.GetCurrentHealth() < playerHealth.GetMaxHealth())
            {
                // Heal player
                playerHealth.Heal(healAmount);
                
                // Play pickup sound
                if (audioSource != null && pickupSound != null)
                {
                    audioSource.PlayOneShot(pickupSound);
                }
                
                // Show pickup effect
                if (pickupEffect != null)
                {
                    Instantiate(pickupEffect, transform.position, Quaternion.identity);
                }
                
                if (destroyOnPickup)
                {
                    // Disable renderer and collider
                    GetComponent<SpriteRenderer>().enabled = false;
                    GetComponent<Collider2D>().enabled = false;
                    
                    // Destroy after sound finishes
                    Destroy(gameObject, pickupSound != null ? pickupSound.length : 0.1f);
                }
            }
        }
    }
}