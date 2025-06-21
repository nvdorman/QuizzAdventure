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
    
    [Header("Collision Settings")]
    public bool useCollisionDamage = true;
    public bool useTriggerDamage = true;
    
    private bool hasDealtDamage = false;
    private AudioSource audioSource;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        Collider2D mainCollider = GetComponent<Collider2D>();
        if (mainCollider != null)
        {
            Debug.Log($"EnemyCollision pada {gameObject.name} menggunakan collider: {mainCollider.GetType().Name}");
        }
        
        if (useTriggerDamage && mainCollider != null && !mainCollider.isTrigger)
        {
            GameObject triggerObj = new GameObject("DamageTrigger");
            triggerObj.transform.SetParent(transform);
            triggerObj.transform.localPosition = Vector3.zero;
            triggerObj.transform.localScale = Vector3.one;
            
            CircleCollider2D triggerCollider = triggerObj.AddComponent<CircleCollider2D>();
            triggerCollider.isTrigger = true;
            triggerCollider.radius = 0.6f;
            
            EnemyCollisionTrigger triggerScript = triggerObj.AddComponent<EnemyCollisionTrigger>();
            triggerScript.parentCollision = this;
            
            Debug.Log($"Trigger collider ditambahkan ke {gameObject.name}");
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            DamagePlayer(other.gameObject);
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!useCollisionDamage) return;
        
        Debug.Log($"EnemyCollision Collision: {gameObject.name} vs {collision.gameObject.name}");
        
        if (collision.gameObject.CompareTag("Player") && canDamagePlayer)
        {
            if (damageOnlyOnce && hasDealtDamage) return;
            
            DamagePlayer(collision.gameObject);
        }
    }
    
    public void DamagePlayer(GameObject player)
    {
        if (damageOnlyOnce && hasDealtDamage) return;
        
        // Kirim damage message ke player object
        // Player harus memiliki method untuk menerima damage
        player.SendMessage("TakeDamage", contactDamage, SendMessageOptions.DontRequireReceiver);
        
        hasDealtDamage = true;
        
        if (audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound);
        }
        
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        Debug.Log($"💥 Enemy {gameObject.name} memberikan {contactDamage} damage kepada player!");
    }
    
    public void ResetDamage()
    {
        hasDealtDamage = false;
    }
}

public class EnemyCollisionTrigger : MonoBehaviour
{
    [HideInInspector]
    public EnemyCollision parentCollision;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (parentCollision != null && other.CompareTag("Player") && parentCollision.canDamagePlayer)
        {
            if (!parentCollision.damageOnlyOnce || !HasDealtDamage())
            {
                parentCollision.DamagePlayer(other.gameObject);
            }
        }
    }
    
    private bool HasDealtDamage()
    {
        var field = parentCollision.GetType().GetField("hasDealtDamage", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            return (bool)field.GetValue(parentCollision);
        }
        
        return false;
    }
}