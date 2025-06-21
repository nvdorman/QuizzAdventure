using UnityEngine;
using System.Collections;

public class EnemyDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    public bool killOnTouch = true;
    public string playerTag = "Player";
    
    [Header("Knockback")]
    public float knockbackForce = 10f;
    public bool applyKnockback = true;
    
    [Header("Effects")]
    public GameObject hitEffect;
    public AudioClip hitSound;
    
    [Header("Game Over")]
    public GameOverManager gameOverManager;
    
    private AudioSource audioSource;
    private bool hasTriggered = false;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Auto-find GameOverManager jika tidak diassign
        if (gameOverManager == null)
        {
            gameOverManager = FindObjectOfType<GameOverManager>();
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (killOnTouch && other.CompareTag(playerTag) && !hasTriggered)
        {
            KillPlayer(other.gameObject);
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (killOnTouch && collision.gameObject.CompareTag(playerTag) && !hasTriggered)
        {
            KillPlayer(collision.gameObject);
        }
    }
    
    public void KillPlayer(GameObject player)
    {
        if (hasTriggered) return;
        
        hasTriggered = true;
        
        Debug.Log($"💀 Player killed by {gameObject.name}!");
        
        // Get PlayerController2D dan trigger death
        PlayerController2D playerController = player.GetComponent<PlayerController2D>();
        if (playerController != null)
        {
            playerController.TriggerDeath($"Killed by {gameObject.name}");
        }
        else
        {
            // Fallback jika PlayerController2D tidak ada
            Debug.LogWarning("PlayerController2D tidak ditemukan, trigger GameOver langsung");
            if (gameOverManager != null)
            {
                gameOverManager.ActivateGameOver();
            }
        }
        
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