using UnityEngine;
using System.Collections;

public class EnemyDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damageAmount = 1;
    public float damageCooldown = 1f;
    public bool damageOnTouch = true;
    public string playerTag = "Player";
    
    [Header("Knockback")]
    public float knockbackForce = 10f;
    public bool applyKnockback = true;
    
    [Header("Effects")]
    public GameObject hitEffect;
    public AudioClip hitSound;
    
    private AudioSource audioSource;
    private float lastDamageTime = 0f;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (damageOnTouch && other.CompareTag(playerTag))
        {
            DamagePlayer(other.gameObject);
        }
    }
    
    void OnTriggerStay2D(Collider2D other)
    {
        if (damageOnTouch && other.CompareTag(playerTag))
        {
            if (Time.time - lastDamageTime >= damageCooldown)
            {
                DamagePlayer(other.gameObject);
            }
        }
    }
    
    public void DamagePlayer(GameObject player)
    {
        if (Time.time - lastDamageTime < damageCooldown) return;
        
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null && !playerHealth.IsInvulnerable())
        {
            lastDamageTime = Time.time;
            
            // Apply damage
            playerHealth.TakeDamage(damageAmount);
            
            // Apply knockback
            if (applyKnockback)
            {
                Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    Vector2 knockbackDirection = (player.transform.position - transform.position).normalized;
                    playerRb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
                }
            }
            
            // Show hit effect
            if (hitEffect != null)
            {
                GameObject effect = Instantiate(hitEffect, player.transform.position, Quaternion.identity);
                Destroy(effect, 1f);
            }
            
            // Play hit sound
            if (audioSource != null && hitSound != null)
            {
                audioSource.PlayOneShot(hitSound);
            }
        }
    }
}