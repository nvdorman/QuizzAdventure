using UnityEngine;

public class EnemyCollision : MonoBehaviour
{
    [Header("Damage Settings")]
    public int contactDamage = 50;
    public bool canDamagePlayer = true;
    public bool damageOnlyOnce = false;
    
    [Header("Effects")]
    public GameObject hitEffect;
    public AudioClip hitSound;
    
    private bool hasDealtDamage = false;
    private AudioSource audioSource;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && canDamagePlayer)
        {
            if (damageOnlyOnce && hasDealtDamage) return;
            
            DamagePlayer(other.gameObject);
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && canDamagePlayer)
        {
            if (damageOnlyOnce && hasDealtDamage) return;
            
            DamagePlayer(collision.gameObject);
        }
    }
    
    private void DamagePlayer(GameObject player)
    {
        HealthSystem playerHealth = player.GetComponent<HealthSystem>();
        if (playerHealth != null && !playerHealth.IsInvulnerable())
        {
            playerHealth.TakeDamage(contactDamage);
            hasDealtDamage = true;
            
            // Play hit sound
            if (audioSource != null && hitSound != null)
            {
                audioSource.PlayOneShot(hitSound);
            }
            
            // Show hit effect
            if (hitEffect != null)
            {
                GameObject effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }
            
            Debug.Log($"Enemy dealt {contactDamage} damage to player!");
        }
    }
    
    public void ResetDamage()
    {
        hasDealtDamage = false;
    }
}