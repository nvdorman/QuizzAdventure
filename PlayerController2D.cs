using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerController2D : MonoBehaviour
{
    [Header("Character Selection")]
    public CharacterColor characterColor = CharacterColor.Beige;
    
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;
    public float duckSpeedMultiplier = 0.5f;

    [Header("Shooting Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 15f;
    public int bulletDamage = 1;
    public float fireRate = 0.3f;
    public int maxAmmo = 30;
    public float reloadTime = 2f;
    
    [Header("Shooting Modes")]
    public bool autoFire = false;
    public bool singleFire = true;
    
    [Header("Shooting Effects")]
    public GameObject muzzleFlash;
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip emptyClipSound;
    
    [Header("Hit Animation Settings")]
    public float hitAnimationDuration = 0.15f;
    public Color hitColor = Color.white;
    
    [Header("Animation Settings")]
    public float walkAnimationSpeed = 7.5f;
    
    [Header("Collider Settings")]
    [Range(0.5f, 1.0f)]
    public float colliderWidthMultiplier = 0.8f;
    [Range(0.5f, 1.0f)]
    public float colliderHeightMultiplier = 0.9f;
    
    [Header("Ground Check")]
    public LayerMask groundLayerMask = 1;
    public float groundCheckDistance = 0.1f;
    
    [Header("Tags & Layers")]
    public string groundTag = "Ground";

    // Enum untuk pilihan character
    public enum CharacterColor
    {
        Beige,
        Green,
        Pink,
        Purple,
        Yellow
    }

    // Struct untuk menyimpan sprite data
    [System.Serializable]
    public struct CharacterSprites
    {
        public Sprite idle;
        public Sprite walkA;
        public Sprite walkB;
        public Sprite jump;
        public Sprite duck;
        public Sprite hit;
        public Sprite front;
        public Sprite climbA;
        public Sprite climbB;
    }

    // Components
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private Camera playerCamera;
    private CapsuleCollider2D capsuleCollider;

    // Movement variables
    private bool isGrounded;
    private bool isDucking = false;
    private float movementInput;
    private float originalMoveSpeed;
    private Color originalColor;
    
    // Character data
    private CharacterSprites currentCharacterSprites;
    private string currentAnimationState = "";
    
    // Shooting variables
    private float nextFireTime = 0f;
    private int currentAmmo;
    private bool isReloading = false;
    private float reloadProgress = 0f;
    private Vector2 mousePosition;
    private Vector2 shootDirection;
    private bool lastMouseButtonState = false;

    // Animation States
    private const string IDLE = "Idle";
    private const string WALK_A = "WalkA";
    private const string WALK_B = "WalkB";
    private const string JUMP = "Jump";
    private const string DUCK = "Duck";
    private const string HIT = "Hit";

    void Awake()
    {
        InitializeComponents();
        LoadCharacterSprites();
        SetupInitialState();
    }

    void Start()
    {
        // Set initial sprite dan warna
        if (spriteRenderer != null && currentCharacterSprites.idle != null)
        {
            spriteRenderer.sprite = currentCharacterSprites.idle;
            originalColor = spriteRenderer.color;
            UpdateColliderToFitSprite(currentCharacterSprites.idle);
        }
    }

    void InitializeComponents()
    {
        // Get atau create Rigidbody2D
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        // Setup Rigidbody2D properties
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        // Get atau create SpriteRenderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        
        // Get atau create AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Get atau create CapsuleCollider2D
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        if (capsuleCollider == null)
        {
            capsuleCollider = gameObject.AddComponent<CapsuleCollider2D>();
        }
        
        // Setup collider properties
        capsuleCollider.direction = CapsuleDirection2D.Vertical;
        
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = FindObjectOfType<Camera>();
        }
        
        originalMoveSpeed = moveSpeed;
        currentAmmo = maxAmmo;
        
        // Setup fire point if not assigned
        if (firePoint == null)
        {
            GameObject firePointObj = new GameObject("FirePoint");
            firePointObj.transform.SetParent(transform);
            firePointObj.transform.localPosition = new Vector3(0.5f, 0, 0);
            firePoint = firePointObj.transform;
        }
    }

    void LoadCharacterSprites()
    {
        string colorName = characterColor.ToString().ToLower();
        
        currentCharacterSprites = new CharacterSprites
        {
            idle = LoadSpriteFromResources($"character_{colorName}_idle"),
            walkA = LoadSpriteFromResources($"character_{colorName}_walk_a"),
            walkB = LoadSpriteFromResources($"character_{colorName}_walk_b"),
            jump = LoadSpriteFromResources($"character_{colorName}_jump"),
            duck = LoadSpriteFromResources($"character_{colorName}_duck"),
            hit = LoadSpriteFromResources($"character_{colorName}_hit"),
            front = LoadSpriteFromResources($"character_{colorName}_front"),
            climbA = LoadSpriteFromResources($"character_{colorName}_climb_a"),
            climbB = LoadSpriteFromResources($"character_{colorName}_climb_b")
        };
        
        Debug.Log($"Loaded character sprites for: {characterColor}");
    }

    Sprite LoadSpriteFromResources(string spriteName)
    {
        // Try loading from Resources/character/ folder
        Sprite sprite = Resources.Load<Sprite>($"character/{spriteName}");
        
        if (sprite == null)
        {
            // Fallback: try loading from Resources root
            sprite = Resources.Load<Sprite>(spriteName);
        }
        
        if (sprite == null)
        {
            Debug.LogWarning($"Could not load sprite: {spriteName}. Make sure it's in Resources/character/ folder.");
        }
        
        return sprite;
    }

    void SetupInitialState()
    {
        currentAnimationState = IDLE;
    }

    void UpdateColliderToFitSprite(Sprite sprite)
    {
        if (capsuleCollider == null || sprite == null) return;
        
        // Get sprite bounds in world units
        Bounds spriteBounds = sprite.bounds;
        
        // Calculate collider size berdasarkan sprite bounds
        Vector2 colliderSize = new Vector2(
            spriteBounds.size.x * colliderWidthMultiplier,
            spriteBounds.size.y * colliderHeightMultiplier
        );
        
        // Set collider size
        capsuleCollider.size = colliderSize;
        
        // Set offset (biasanya sedikit ke bawah untuk grounded check yang baik)
        Vector2 colliderOffset = new Vector2(0f, -spriteBounds.size.y * 0.05f);
        capsuleCollider.offset = colliderOffset;
        
        Debug.Log($"Updated collider for {sprite.name}: Size={colliderSize}, Offset={colliderOffset}");
    }

    void Update()
    {
        HandleInput();
        HandleShooting();
        UpdateAnimation();
        UpdateAiming();
        CheckGrounded();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    void HandleInput()
    {
        // Get horizontal input
        movementInput = Input.GetAxisRaw("Horizontal");

        // Jump input (tidak bisa jump saat jongkok)
        if (Input.GetButtonDown("Jump") && isGrounded && !isDucking)
        {
            Jump();
        }
        
        // Duck/Crouch input (Ctrl key)
        HandleDuckInput();
        
        // Reload input
        if (Input.GetKeyDown(KeyCode.R) && !isReloading)
        {
            StartReload();
        }
    }
    
    void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        Debug.Log("🦘 Player jumped!");
    }
    
    void HandleDuckInput()
    {
        bool duckInput = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        
        if (duckInput && !isDucking && isGrounded)
        {
            StartDuck();
        }
        else if (!duckInput && isDucking)
        {
            StopDuck();
        }
    }
    
    void StartDuck()
    {
        isDucking = true;
        moveSpeed = originalMoveSpeed * duckSpeedMultiplier;
        Debug.Log("🦆 Player is ducking!");
    }
    
    void StopDuck()
    {
        isDucking = false;
        moveSpeed = originalMoveSpeed;
        Debug.Log("🚶 Player stopped ducking!");
    }
    
    void CheckGrounded()
    {
        if (capsuleCollider == null) return;
        
        // Cast down from collider bottom
        Vector2 colliderBottom = (Vector2)transform.position + capsuleCollider.offset - new Vector2(0, capsuleCollider.size.y * 0.5f);
        
        RaycastHit2D hit = Physics2D.Raycast(colliderBottom, Vector2.down, groundCheckDistance, groundLayerMask);
        
        // Also check with tag system as fallback
        bool wasGrounded = isGrounded;
        isGrounded = hit.collider != null;
        
        // Debug ray
        Debug.DrawRay(colliderBottom, Vector2.down * groundCheckDistance, isGrounded ? Color.green : Color.red);
        
        if (!wasGrounded && isGrounded)
        {
            Debug.Log("Player landed");
        }
    }
    
    void HandleShooting()
    {
        if (isReloading) return;
        
        bool currentMouseButton = Input.GetMouseButton(0);
        bool mouseButtonDown = Input.GetMouseButtonDown(0);
        
        // Single fire mode
        if (singleFire)
        {
            if (mouseButtonDown && Time.time >= nextFireTime)
            {
                if (currentAmmo > 0)
                {
                    Shoot();
                    nextFireTime = Time.time + fireRate;
                }
                else
                {
                    PlayEmptyClipSound();
                    StartReload();
                }
            }
        }
        // Auto fire mode
        else if (autoFire)
        {
            if (currentMouseButton && Time.time >= nextFireTime)
            {
                if (currentAmmo > 0)
                {
                    Shoot();
                    nextFireTime = Time.time + fireRate;
                }
                else
                {
                    PlayEmptyClipSound();
                    StartReload();
                }
            }
        }
        
        lastMouseButtonState = currentMouseButton;
    }
    
    void PlayEmptyClipSound()
    {
        if (audioSource != null && emptyClipSound != null)
        {
            audioSource.PlayOneShot(emptyClipSound);
        }
    }
    
    void UpdateAiming()
    {
        if (playerCamera != null)
        {
            mousePosition = playerCamera.ScreenToWorldPoint(Input.mousePosition);
            shootDirection = (mousePosition - (Vector2)transform.position).normalized;
            
            if (firePoint != null)
            {
                float angle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;
                firePoint.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
                
                // Adjust fire point position based on current animation
                float firePointY = isDucking ? shootDirection.y * 0.2f : shootDirection.y * 0.3f;
                
                firePoint.localPosition = new Vector3(
                    Mathf.Abs(shootDirection.x) * 0.5f, 
                    firePointY, 
                    0
                );
            }
        }
    }
    
    void Shoot()
    {
        if (bulletPrefab == null || firePoint == null) return;
        
        currentAmmo--;
        
        // Create bullet
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        
        if (bulletScript != null)
        {
            bulletScript.Initialize(shootDirection, true, bulletDamage);
        }
        else
        {
            Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
            if (bulletRb != null)
            {
                bulletRb.velocity = shootDirection * bulletSpeed;
            }
        }
        
        // Play shoot sound
        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
        
        // Show muzzle flash
        if (muzzleFlash != null)
        {
            StartCoroutine(ShowMuzzleFlash());
        }
        
        // Hit animation saat menembak
        StartCoroutine(PlayHitAnimationOnShoot());
        
        Debug.Log($"💥 Shot fired! Ammo: {currentAmmo}/{maxAmmo}");
    }
    
    System.Collections.IEnumerator PlayHitAnimationOnShoot()
    {
        if (spriteRenderer != null && currentCharacterSprites.hit != null)
        {
            Color currentColor = spriteRenderer.color;
            Sprite originalSprite = spriteRenderer.sprite;
            
            // Flash color and change sprite
            spriteRenderer.color = hitColor;
            spriteRenderer.sprite = currentCharacterSprites.hit;
            UpdateColliderToFitSprite(currentCharacterSprites.hit);
            
            yield return new WaitForSeconds(hitAnimationDuration);
            
            // Restore original
            spriteRenderer.color = currentColor;
            spriteRenderer.sprite = originalSprite;
            UpdateColliderToFitSprite(originalSprite);
        }
    }
    
    System.Collections.IEnumerator ShowMuzzleFlash()
    {
        if (muzzleFlash != null)
        {
            GameObject flash = Instantiate(muzzleFlash, firePoint.position, firePoint.rotation);
            yield return new WaitForSeconds(0.1f);
            if (flash != null) Destroy(flash);
        }
    }
    
    void StartReload()
    {
        if (currentAmmo >= maxAmmo || isReloading) return;
        
        StartCoroutine(ReloadCoroutine());
    }
    
    System.Collections.IEnumerator ReloadCoroutine()
    {
        isReloading = true;
        reloadProgress = 0f;
        
        if (audioSource != null && reloadSound != null)
        {
            audioSource.PlayOneShot(reloadSound);
        }
        
        Debug.Log("🔄 Reloading...");
        
        float elapsedTime = 0f;
        while (elapsedTime < reloadTime)
        {
            elapsedTime += Time.deltaTime;
            reloadProgress = elapsedTime / reloadTime;
            yield return null;
        }
        
        currentAmmo = maxAmmo;
        isReloading = false;
        reloadProgress = 1f;
        
        Debug.Log("✅ Reload complete!");
    }

    void HandleMovement()
    {
        // Apply horizontal movement
        float targetVelocityX = movementInput * moveSpeed;
        rb.velocity = new Vector2(targetVelocityX, rb.velocity.y);
        
        // Handle sprite flipping
        if (spriteRenderer != null)
        {
            if (movementInput != 0)
            {
                spriteRenderer.flipX = movementInput < 0;
            }
            else if (playerCamera != null)
            {
                Vector2 mousePos = playerCamera.ScreenToWorldPoint(Input.mousePosition);
                spriteRenderer.flipX = mousePos.x < transform.position.x;
            }
        }
        
        // Debug movement
        if (movementInput != 0)
        {
            Debug.Log($"Moving: input={movementInput}, velocity={rb.velocity.x}, speed={moveSpeed}");
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(groundTag))
        {
            isGrounded = true;
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(groundTag))
        {
            isGrounded = true;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(groundTag))
        {
            isGrounded = false;
        }
    }

    public void SetSlowMotion(float slowFactor)
    {
        moveSpeed = originalMoveSpeed * slowFactor;
    }

    void UpdateAnimation()
    {
        if (spriteRenderer == null) return;
        
        string newAnimationState = "";
        Sprite newSprite = null;
        
        // Determine animation state priority: Duck -> Jump -> Walk -> Idle
        if (isDucking)
        {
            newAnimationState = DUCK;
            newSprite = currentCharacterSprites.duck;
        }
        else if (!isGrounded)
        {
            newAnimationState = JUMP;
            newSprite = currentCharacterSprites.jump;
        }
        else if (Mathf.Abs(movementInput) > 0.1f) // Walking
        {
            // Alternate between walk A and B
            float walkAnimationTime = Mathf.PingPong(Time.time * walkAnimationSpeed, 1);
            
            if (walkAnimationTime < 0.5f)
            {
                newAnimationState = WALK_A;
                newSprite = currentCharacterSprites.walkA;
            }
            else
            {
                newAnimationState = WALK_B;
                newSprite = currentCharacterSprites.walkB;
            }
        }
        else
        {
            newAnimationState = IDLE;
            newSprite = currentCharacterSprites.idle;
        }
        
        // Update sprite and collider if animation changed
        if (newAnimationState != currentAnimationState && newSprite != null)
        {
            currentAnimationState = newAnimationState;
            spriteRenderer.sprite = newSprite;
            UpdateColliderToFitSprite(newSprite);
            
            Debug.Log($"Animation changed to: {newAnimationState}");
        }
    }
    
    // Public methods
    public int GetCurrentAmmo() { return currentAmmo; }
    public int GetMaxAmmo() { return maxAmmo; }
    public bool IsReloading() { return isReloading; }
    public float GetReloadProgress() { return reloadProgress; }
    public bool IsDucking() { return isDucking; }
    public bool IsGrounded() { return isGrounded; }
    public CharacterColor GetCharacterColor() { return characterColor; }
    
    public void SetAutoFire(bool auto)
    {
        autoFire = auto;
        singleFire = !auto;
    }
    
    public void SetSingleFire(bool single)
    {
        singleFire = single;
        autoFire = !single;
    }
    
    public void ChangeCharacter(CharacterColor newColor)
    {
        characterColor = newColor;
        LoadCharacterSprites();
        
        // Update current sprite
        if (spriteRenderer != null && currentCharacterSprites.idle != null)
        {
            spriteRenderer.sprite = currentCharacterSprites.idle;
            UpdateColliderToFitSprite(currentCharacterSprites.idle);
        }
        
        Debug.Log($"Changed character to: {characterColor}");
    }
    
    public void ResetSpriteColor()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }
    
    public void TriggerHitAnimation()
    {
        StartCoroutine(PlayHitAnimationOnShoot());
    }

    // OnValidate untuk update character di editor
    void OnValidate()
    {
        if (Application.isPlaying && gameObject.activeInHierarchy)
        {
            LoadCharacterSprites();
            if (spriteRenderer != null && currentCharacterSprites.idle != null)
            {
                spriteRenderer.sprite = currentCharacterSprites.idle;
                UpdateColliderToFitSprite(currentCharacterSprites.idle);
            }
        }
    }

    // Debug method untuk testing di Scene view
    void OnDrawGizmosSelected()
    {
        if (capsuleCollider != null)
        {
            // Draw collider bounds
            Gizmos.color = Color.green;
            Vector3 colliderCenter = transform.position + (Vector3)capsuleCollider.offset;
            Gizmos.DrawWireCube(colliderCenter, capsuleCollider.size);
        }
        
        // Draw ground check
        if (Application.isPlaying)
        {
            Vector2 colliderBottom = (Vector2)transform.position + capsuleCollider.offset - new Vector2(0, capsuleCollider.size.y * 0.5f);
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawLine(colliderBottom, colliderBottom + Vector2.down * groundCheckDistance);
        }
    }
}