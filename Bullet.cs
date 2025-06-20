using UnityEngine;
using System.Collections;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float speed = 20f; // Increased speed
    public int damage = 1;
    public float lifetime = 5f; // Increased lifetime
    public bool piercing = false;
    public int maxHits = 1;
    
    [Header("Effects")]
    public GameObject hitEffect;
    public GameObject trailEffect;
    public AudioClip hitSound;
    
    [Header("Target Settings")]
    public string[] targetTags = {"Enemy"};
    public LayerMask obstacleLayer = 1;
    
    [Header("Collision Settings")]
    public float ignoreOriginTime = 0.2f; // Time to ignore collisions near spawn point
    
    private Rigidbody2D rb;
    private CircleCollider2D bulletCollider;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private Vector2 direction;
    private int hitCount = 0;
    private bool isPlayerBullet = true;
    private GameObject shooter; // Reference to who shot this bullet
    private Vector3 spawnPosition;
    private float spawnTime;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        bulletCollider = GetComponent<CircleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        spawnPosition = transform.position;
        spawnTime = Time.time;
        
        // Destroy bullet after lifetime
        Destroy(gameObject, lifetime);
        
        // Add trail effect
        if (trailEffect != null)
        {
            Instantiate(trailEffect, transform);
        }
        
        // Disable collision with shooter initially
        if (shooter != null)
        {
            Collider2D shooterCollider = shooter.GetComponent<Collider2D>();
            if (shooterCollider != null && bulletCollider != null)
            {
                Physics2D.IgnoreCollision(bulletCollider, shooterCollider, true);
            }
        }
    }
    
    void FixedUpdate()
    {
        // Move bullet
        if (rb != null)
        {
            rb.velocity = direction * speed;
        }
    }
    
    public void Initialize(Vector2 shootDirection, bool fromPlayer = true, int bulletDamage = 1, GameObject bulletShooter = null)
    {
        direction = shootDirection.normalized;
        isPlayerBullet = fromPlayer;
        damage = bulletDamage;
        shooter = bulletShooter;
        
        // Rotate bullet to face direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        
        // Set target tags based on shooter
        if (isPlayerBullet)
        {
            targetTags = new string[] {"Enemy"};
        }
        else
        {
            targetTags = new string[] {"Player"};
        }
        
        // Move bullet slightly forward from spawn to avoid immediate collision
        transform.position += (Vector3)(direction * 0.5f);
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // Ignore collision with shooter
        if (shooter != null && other.gameObject == shooter)
        {
            return;
        }
        
        // Ignore collisions too close to spawn point and too soon after spawning
        float distanceFromSpawn = Vector3.Distance(transform.position, spawnPosition);
        float timeSinceSpawn = Time.time - spawnTime;
        
        if (distanceFromSpawn < 1f && timeSinceSpawn < ignoreOriginTime)
        {
            return;
        }
        
        // Check if hit target
        bool hitTarget = false;
        foreach (string tag in targetTags)
        {
            if (other.CompareTag(tag))
            {
                hitTarget = true;
                break;
            }
        }
        
        if (hitTarget)
        {
            HitTarget(other.gameObject);
        }
        else if (IsObstacle(other))
        {
            HitObstacle();
        }
    }
    
    void HitTarget(GameObject target)
    {
        hitCount++;
        
        // Apply damage
        if (isPlayerBullet)
        {
            // Try to find EnemyHealth component (if it exists)
            Component enemyHealthComponent = target.GetComponent("EnemyHealth");
            if (enemyHealthComponent != null)
            {
                // Use SendMessage to call TakeDamage method
                target.SendMessage("TakeDamage", (float)damage, SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                // Alternative: Destroy enemy directly or handle differently
                Debug.Log($"Hit enemy {target.name} for {damage} damage");
                // You can add your own enemy damage logic here
            }
        }
        else
        {
            // Apply damage to player
            PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
        }
        
        // Show hit effect
        ShowHitEffect();
        
        // Destroy bullet if not piercing or max hits reached
        if (!piercing || hitCount >= maxHits)
        {
            DestroyBullet();
        }
    }
    
    void HitObstacle()
    {
        ShowHitEffect();
        DestroyBullet();
    }
    
    void ShowHitEffect()
    {
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
            Destroy(effect, 1f);
        }
        
        if (audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound);
        }
    }
    
    void DestroyBullet()
    {
        // Disable collider and renderer but keep audio playing
        if (bulletCollider != null) bulletCollider.enabled = false;
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        
        // Destroy after sound finishes
        Destroy(gameObject, hitSound != null ? hitSound.length : 0.1f);
    }
    
    bool IsObstacle(Collider2D collider)
    {
        // Don't consider player as obstacle for player bullets
        if (isPlayerBullet && collider.CompareTag("Player"))
        {
            return false;
        }
        
        // Don't consider enemies as obstacles for enemy bullets
        if (!isPlayerBullet && collider.CompareTag("Enemy"))
        {
            return false;
        }
        
        return ((1 << collider.gameObject.layer) & obstacleLayer) != 0;
    }
}