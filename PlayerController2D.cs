using UnityEngine;
using System.Collections;

public class PlayerController2D : MonoBehaviour
{
    [Header("Character Selection")]
    public CharacterColor characterColor = CharacterColor.Beige;
    
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;
    public float duckSpeedMultiplier = 0.5f;

    [Header("Shooting Settings")]
    public bool shootingDisabled = true; // Shooting completely disabled in dodge mode
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
    public float jumpAnimationMinTime = 0.3f;
    
    [Header("Collider Settings")]
    [Range(0.5f, 1.0f)]
    public float colliderWidthMultiplier = 0.8f;
    [Range(0.5f, 1.0f)]
    public float colliderHeightMultiplier = 0.9f;
    
    [Header("Ground Check - IMPROVED")]
    public LayerMask groundLayerMask = 1;
    public float groundCheckDistance = 0.2f;
    [Range(0.5f, 1.0f)]
    public float groundStabilityThreshold = 0.6f; // Minimum ground coverage required
    public float coyoteTime = 0.1f; // Grace period after leaving ground
    public float jumpCooldown = 0.15f; // Minimum time between jumps
    
    [Header("Tags & Layers")]
    public string groundTag = "Ground";
    
    [Header("Game Over Integration")]
    public GameOverManager gameOverManager;
    public string[] dangerousTags = {"Water", "Lava", "Spike", "Poison", "Enemy", "Trap"};
    public float gameOverDelay = 1.0f;
    public bool enableDeathByFalling = true;
    public float fallDeathY = -10f; // Y position below which player dies
    
    [Header("Death Effects")]
    public AudioClip deathSound;
    public Color deathFlashColor = Color.red;
    public float deathFlashSpeed = 10f;

    public enum CharacterColor
    {
        Beige,
        Green,
        Pink,
        Purple,
        Yellow
    }

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
    
    // IMPROVED JUMP SYSTEM
    private bool canJump = true;
    private float jumpTime = 0f;
    private float lastJumpTime = 0f; // For jump cooldown
    private float lastGroundedTime = 0f; // For coyote time
    private float groundStability = 0f; // How much ground is under player (0-1)
    
    // Slow motion system (for GameOverTrigger compatibility)
    private float currentSlowMotionFactor = 1f;
    private bool isInSlowMotion = false;
    
    // Character data
    private CharacterSprites currentCharacterSprites;
    private string currentAnimationState = "";
    
    // Shooting variables
    private int currentAmmo;
    private bool isReloading = false;
    private float reloadProgress = 0f;
    private Vector2 mousePosition;
    private Vector2 shootDirection = Vector2.right; // Initialize with default direction
    
    // Death system variables
    private bool isDead = false;
    private bool deathTriggered = false;

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
        // PERBAIKAN SPRITE TRANSPARAN - Pastikan sprite dan warna di-set dengan benar
        if (spriteRenderer != null && currentCharacterSprites.idle != null)
        {
            spriteRenderer.sprite = currentCharacterSprites.idle;
            spriteRenderer.color = Color.white; // PASTIKAN TIDAK TRANSPARAN
            originalColor = spriteRenderer.color;
            UpdateColliderToFitSprite(currentCharacterSprites.idle);
            Debug.Log($"✅ Sprite berhasil di-set: {currentCharacterSprites.idle.name} dengan warna: {spriteRenderer.color}");
        }
        else
        {
            Debug.LogError("❌ SpriteRenderer atau sprite idle tidak ditemukan! Cek apakah sprite ada di Resources folder!");
            // Fallback - buat sprite renderer dengan warna solid jika sprite tidak ada
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.cyan; // Warna fallback yang terlihat
                Debug.LogWarning("⚠️ Menggunakan warna fallback untuk sprite");
            }
        }

        // Auto-find GameOverManager if not assigned
        if (gameOverManager == null)
        {
            gameOverManager = FindObjectOfType<GameOverManager>();
            if (gameOverManager == null)
            {
                Debug.LogWarning("⚠️ GameOverManager not found! Player death won't trigger game over.");
            }
        }

        // Enable shooting after a short delay to prevent accidental shots on start
        StartCoroutine(EnableShootingAfterDelay());
        Debug.Log("🚫 Shooting system disabled - dodge only mode!");
    }
    
    IEnumerator EnableShootingAfterDelay()
    {
        yield return new WaitForSeconds(0.1f); // Small delay
        Debug.Log("🔫 Shooting enabled!");
    }

    void InitializeComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        
        // PERBAIKAN SPRITE TRANSPARAN - Pastikan sprite renderer tidak transparan dari awal
        spriteRenderer.color = Color.white;
        Debug.Log($"🎨 SpriteRenderer initialized dengan warna: {spriteRenderer.color}");
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        if (capsuleCollider == null)
        {
            capsuleCollider = gameObject.AddComponent<CapsuleCollider2D>();
        }
        
        capsuleCollider.direction = CapsuleDirection2D.Vertical;
        
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = FindObjectOfType<Camera>();
        }
        
        originalMoveSpeed = moveSpeed;
        currentAmmo = maxAmmo;
        
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
        
        Debug.Log($"🎭 Loaded character sprites for: {characterColor}");
        
        // Debug info untuk sprite yang berhasil/gagal dimuat
        if (currentCharacterSprites.idle != null)
            Debug.Log($"✅ Idle sprite loaded: {currentCharacterSprites.idle.name}");
        else
            Debug.LogWarning($"❌ Idle sprite gagal dimuat untuk {colorName}");
    }

    Sprite LoadSpriteFromResources(string spriteName)
    {
        // Coba beberapa path yang mungkin
        Sprite sprite = Resources.Load<Sprite>($"character/{spriteName}");
        
        if (sprite == null)
        {
            sprite = Resources.Load<Sprite>($"Characters/{spriteName}");
        }
        
        if (sprite == null)
        {
            sprite = Resources.Load<Sprite>(spriteName);
        }
        
        if (sprite == null)
        {
            // Coba dengan format nama yang berbeda
            string altName = spriteName.Replace("character_", "p1_").Replace("_", "_");
            sprite = Resources.Load<Sprite>($"Characters/{altName}");
        }
        
        if (sprite == null)
        {
            Debug.LogWarning($"⚠️ Could not load sprite: {spriteName}. Cek apakah sprite ada di Resources folder!");
        }
        else
        {
            Debug.Log($"✅ Successfully loaded sprite: {spriteName}");
        }
        
        return sprite;
    }

    void SetupInitialState()
    {
        currentAnimationState = IDLE;
        isGrounded = true;
        canJump = true;
        lastGroundedTime = Time.time;
        currentSlowMotionFactor = 1f;
        isInSlowMotion = false;
        isDead = false;
        deathTriggered = false;
    }

    void UpdateColliderToFitSprite(Sprite sprite)
    {
        if (capsuleCollider == null || sprite == null) return;
        
        Bounds spriteBounds = sprite.bounds;
        
        Vector2 colliderSize = new Vector2(
            spriteBounds.size.x * colliderWidthMultiplier,
            spriteBounds.size.y * colliderHeightMultiplier
        );
        
        capsuleCollider.size = colliderSize;
        
        Vector2 colliderOffset = new Vector2(0f, -spriteBounds.size.y * 0.05f);
        capsuleCollider.offset = colliderOffset;
        
        Debug.Log($"🔧 Updated collider for {sprite.name}: Size={colliderSize}, Offset={colliderOffset}");
    }

    void Update()
    {
        // Don't process input if player is dead
        if (isDead) return;
        
        HandleInput();
        UpdateAiming();
        HandleShooting();
        UpdateAnimation();
        CheckGrounded();
        
        // Check for fall death
        CheckFallDeath();
    }

    void FixedUpdate()
    {
        // Don't move if player is dead
        if (isDead) return;
        
        HandleMovement();
    }
    
    // NEW: Check if player has fallen too far
    void CheckFallDeath()
    {
        if (enableDeathByFalling && !deathTriggered && transform.position.y < fallDeathY)
        {
            Debug.Log($"💀 Player fell too far! Y position: {transform.position.y}");
            TriggerDeath("Fell too far");
        }
    }
    
    // PERBAIKAN TAG - Collision detection untuk objek berbahaya dengan penanganan tag yang aman
    void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead || deathTriggered) return;
        
        if (IsDangerousObject(other.gameObject))
        {
            Debug.Log($"💀 Player hit dangerous object: {other.name} with tag: {other.tag}");
            TriggerDeath($"Hit {other.tag}");
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead || deathTriggered) return;
        
        if (IsDangerousObject(collision.gameObject))
        {
            Debug.Log($"💀 Player collided with dangerous object: {collision.gameObject.name} with tag: {collision.gameObject.tag}");
            TriggerDeath($"Collided with {collision.gameObject.tag}");
        }
    }
    
    // PERBAIKAN TAG - Fungsi yang aman untuk mengecek objek berbahaya
    bool IsDangerousObject(GameObject obj)
    {
        foreach (string dangerousTag in dangerousTags)
        {
            // PERBAIKAN: Gunakan try-catch untuk menghindari error "Tag is not defined"
            try
            {
                if (obj.CompareTag(dangerousTag))
                {
                    return true;
                }
            }
            catch (UnityException e)
            {
                Debug.LogWarning($"⚠️ Tag '{dangerousTag}' is not defined in Tag Manager! Error: {e.Message}");
                Debug.LogWarning($"💡 Please add '{dangerousTag}' to Tag Manager (Window > Tags and Layers > Tags)");
                
                // Alternatif: cek berdasarkan nama object jika tag tidak ada
                if (obj.name.ToLower().Contains(dangerousTag.ToLower()))
                {
                    Debug.Log($"🔍 Found dangerous object by name: {obj.name} contains '{dangerousTag}'");
                    return true;
                }
            }
        }
        return false;
    }
    
    // NEW: Trigger death sequence
    public void TriggerDeath(string cause = "Unknown")
    {
        if (deathTriggered) return;
        
        deathTriggered = true;
        Debug.Log($"💀💀💀 PLAYER DEATH TRIGGERED! Cause: {cause}");
        
        // Play death sound
        if (deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
        
        // Start death sequence
        StartCoroutine(DeathSequence());
    }
    
    // NEW: Death sequence coroutine
    IEnumerator DeathSequence()
    {
        // Disable player controls
        isDead = true;
        
        // Apply slow motion
        SetSlowMotion(0.3f);
        
        // Start death flash effect
        StartCoroutine(DeathFlashEffect());
        
        // Wait for death delay
        yield return new WaitForSeconds(gameOverDelay);
        
        // Stop flash effect
        StopAllCoroutines();
        
        // Reset sprite color
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        
        // Trigger Game Over
        if (gameOverManager != null)
        {
            Debug.Log("💀 Calling GameOverManager.ActivateGameOver()");
            gameOverManager.ActivateGameOver();
        }
        else
        {
            Debug.LogError("❌ GameOverManager is null! Cannot trigger game over!");
        }
    }
    
    // NEW: Death flash effect
    IEnumerator DeathFlashEffect()
    {
        if (spriteRenderer == null) yield break;
        
        Color originalColor = spriteRenderer.color;
        
        while (true)
        {
            // Flash to death color
            spriteRenderer.color = deathFlashColor;
            yield return new WaitForSeconds(1f / deathFlashSpeed);
            
            // Flash back to original
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(1f / deathFlashSpeed);
        }
    }

    void HandleInput()
    {
        movementInput = Input.GetAxisRaw("Horizontal");

        // IMPROVED JUMP INPUT with all anti-spam measures
        if (Input.GetButtonDown("Jump"))
        {
            Debug.Log($"🎮 Jump input! Grounded: {isGrounded}, CanJump: {canJump}, Stability: {groundStability:F2}, " +
                     $"Cooldown: {Time.time - lastJumpTime:F2}s, VelY: {rb.velocity.y:F2}");
            
            TryJump();
        }
        
        HandleDuckInput();
        
        if (Input.GetKeyDown(KeyCode.R) && !isReloading)
        {
            StartReload();
        }
    }
    
    void TryJump()
    {
        // STRICT CONDITIONS - All must be met to jump
        bool cooldownPassed = (Time.time - lastJumpTime) >= jumpCooldown;
        bool withinCoyoteTime = (Time.time - lastGroundedTime) <= coyoteTime;
        bool hasGroundStability = groundStability >= groundStabilityThreshold;
        bool notMovingUpTooFast = rb.velocity.y <= 0.5f;
        bool basicConditions = canJump && !isDucking;
        
        // Must be either grounded OR within coyote time (but not both conditions relaxed)
        bool groundCondition = isGrounded || (withinCoyoteTime && hasGroundStability);
        
        if (!cooldownPassed)
        {
            Debug.Log($"❌ Jump denied: Cooldown not passed ({Time.time - lastJumpTime:F2}s < {jumpCooldown}s)");
            return;
        }
        
        if (!groundCondition)
        {
            Debug.Log($"❌ Jump denied: Not grounded ({isGrounded}) and not in coyote time ({Time.time - lastGroundedTime:F2}s > {coyoteTime}s)");
            return;
        }
        
        if (!hasGroundStability)
        {
            Debug.Log($"❌ Jump denied: Insufficient ground stability ({groundStability:F2} < {groundStabilityThreshold})");
            return;
        }
        
        if (!notMovingUpTooFast)
        {
            Debug.Log($"❌ Jump denied: Moving up too fast (velY={rb.velocity.y:F2})");
            return;
        }
        
        if (!basicConditions)
        {
            Debug.Log($"❌ Jump denied: Basic conditions failed (canJump={canJump}, ducking={isDucking})");
            return;
        }
        
        // All conditions passed - Execute jump
        ExecuteJump();
    }
    
    void ExecuteJump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        
        // IMMEDIATE STATE CHANGES
        canJump = false;
        isGrounded = false;
        jumpTime = Time.time;
        lastJumpTime = Time.time;
        groundStability = 0f; // Reset stability immediately
        
        Debug.Log($"🚀 JUMP EXECUTED! All locks applied. VelY: {rb.velocity.y:F2}");
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
        moveSpeed = originalMoveSpeed * duckSpeedMultiplier * currentSlowMotionFactor;
        canJump = false; // Can't jump while ducking
        Debug.Log("Started ducking");
    }

    void StopDuck()
    {
        isDucking = false;
        moveSpeed = originalMoveSpeed * currentSlowMotionFactor;
        // Don't immediately restore canJump - let ground check handle it
        Debug.Log("Stopped ducking");
    }

    // COMPLETELY REWRITTEN CheckGrounded with strict validation
    void CheckGrounded()
    {
        if (capsuleCollider == null) return;
        
        // Calculate raycast positions - more points for better accuracy
        Vector2 centerBottom = (Vector2)transform.position + capsuleCollider.offset - new Vector2(0, capsuleCollider.size.y * 0.5f);
        Vector2 leftBottom = centerBottom - new Vector2(capsuleCollider.size.x * 0.35f, 0);
        Vector2 rightBottom = centerBottom + new Vector2(capsuleCollider.size.x * 0.35f, 0);
        Vector2 farLeftBottom = centerBottom - new Vector2(capsuleCollider.size.x * 0.45f, 0);
        Vector2 farRightBottom = centerBottom + new Vector2(capsuleCollider.size.x * 0.45f, 0);
        
        // Perform 5 raycasts for better ground detection
        RaycastHit2D centerHit = Physics2D.Raycast(centerBottom, Vector2.down, groundCheckDistance, groundLayerMask);
        RaycastHit2D leftHit = Physics2D.Raycast(leftBottom, Vector2.down, groundCheckDistance, groundLayerMask);
        RaycastHit2D rightHit = Physics2D.Raycast(rightBottom, Vector2.down, groundCheckDistance, groundLayerMask);
        RaycastHit2D farLeftHit = Physics2D.Raycast(farLeftBottom, Vector2.down, groundCheckDistance, groundLayerMask);
        RaycastHit2D farRightHit = Physics2D.Raycast(farRightBottom, Vector2.down, groundCheckDistance, groundLayerMask);
        
        // Count hits and calculate stability
        int hitCount = 0;
        if (centerHit.collider != null) hitCount++;
        if (leftHit.collider != null) hitCount++;
        if (rightHit.collider != null) hitCount++;
        if (farLeftHit.collider != null) hitCount++;
        if (farRightHit.collider != null) hitCount++;
        
        // Calculate ground stability (0.0 to 1.0)
        groundStability = hitCount / 5f;
        
        bool wasGrounded = isGrounded;
        bool hasMinimumGroundContact = groundStability >= groundStabilityThreshold;
        bool isMovingDownward = rb.velocity.y <= 1f; // More lenient for grounding
        
        // STRICT GROUNDING RULES
        if (hasMinimumGroundContact && isMovingDownward)
        {
            if (!wasGrounded)
            {
                // Just landed
                isGrounded = true;
                canJump = true;
                lastGroundedTime = Time.time;
                Debug.Log($"✅ LANDED! Stability: {groundStability:F2}, VelY: {rb.velocity.y:F2}");
            }
            else
            {
                // Still grounded
                isGrounded = true;
                if (!isDucking) canJump = true;
                lastGroundedTime = Time.time;
            }
        }
        else if (!hasMinimumGroundContact)
        {
            // Lost ground contact
            if (wasGrounded)
            {
                Debug.Log($"⚠️ LEFT GROUND! Stability dropped to: {groundStability:F2}");
            }
            isGrounded = false;
            // Don't immediately disable canJump - let coyote time handle it
        }
        
        // Visual debug
        Color rayColor = hasMinimumGroundContact ? Color.green : Color.red;
        Debug.DrawRay(centerBottom, Vector2.down * groundCheckDistance, rayColor);
        Debug.DrawRay(leftBottom, Vector2.down * groundCheckDistance, rayColor);
        Debug.DrawRay(rightBottom, Vector2.down * groundCheckDistance, rayColor);
        Debug.DrawRay(farLeftBottom, Vector2.down * groundCheckDistance, Color.cyan);
        Debug.DrawRay(farRightBottom, Vector2.down * groundCheckDistance, Color.cyan);
        
        // Debug stability info
        if (Time.frameCount % 30 == 0) // Every 30 frames
        {
            Debug.Log($"Ground Debug - Stability: {groundStability:F2}, Grounded: {isGrounded}, CanJump: {canJump}, " +
                     $"TimeSinceGrounded: {Time.time - lastGroundedTime:F2}s");
        }
    }

    void HandleMovement()
    {
        float targetVelocityX = movementInput * moveSpeed;
        rb.velocity = new Vector2(targetVelocityX, rb.velocity.y);
        
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
    }

    void UpdateAnimation()
    {
        if (spriteRenderer == null) return;
        
        string newAnimationState = "";
        Sprite newSprite = null;
        
        bool isInJumpAnimation = (Time.time - jumpTime) < jumpAnimationMinTime;
        
        if (isDucking)
        {
            newAnimationState = DUCK;
            newSprite = currentCharacterSprites.duck;
        }
        else if (!isGrounded || isInJumpAnimation)
        {
            newAnimationState = JUMP;
            newSprite = currentCharacterSprites.jump;
        }
        else if (Mathf.Abs(movementInput) > 0.1f)
        {
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
        
        if (newAnimationState != currentAnimationState && newSprite != null)
        {
            currentAnimationState = newAnimationState;
            spriteRenderer.sprite = newSprite;
            // PERBAIKAN SPRITE TRANSPARAN - Pastikan warna tetap tidak transparan
            if (spriteRenderer.color.a < 1f)
            {
                Color fixedColor = spriteRenderer.color;
                fixedColor.a = 1f;
                spriteRenderer.color = fixedColor;
                Debug.Log($"🔧 Fixed transparent sprite! Color set to: {spriteRenderer.color}");
            }
            UpdateColliderToFitSprite(newSprite);
        }
    }

    void HandleShooting()
    {
        // SHOOTING DISABLED - Player can only dodge
        if (shootingDisabled)
        {
            return;
        }
        
        // Original shooting code would go here, but it's disabled
        Debug.Log("🚫 Shooting is disabled in dodge-only mode!");
    }

    void Shoot()
    {
        Debug.Log("🚫 Shooting is disabled in this game mode!");
        return;
    }

    void UpdateAiming()
    {
        if (playerCamera == null) return;
        
        Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePosition = new Vector2(mouseWorldPos.x, mouseWorldPos.y);
        shootDirection = (mousePosition - (Vector2)firePoint.position).normalized;
        
        // Ensure we have a valid direction
        if (shootDirection.magnitude < 0.1f)
        {
            shootDirection = Vector2.right; // Default to right if no valid direction
        }
        
        float angle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;
        firePoint.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        
        // Debug aiming every few frames
        if (Time.frameCount % 60 == 0) // Every 60 frames
        {
            Debug.Log($"🎯 Mouse: {mousePosition}, Direction: {shootDirection}, Angle: {angle:F1}°");
        }
    }

    void StartReload()
    {
        if (currentAmmo >= maxAmmo) return;
        
        StartCoroutine(ReloadCoroutine());
    }

    IEnumerator ReloadCoroutine()
    {
        isReloading = true;
        reloadProgress = 0f;
        
        if (reloadSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(reloadSound);
        }
        
        float elapsedTime = 0f;
        while (elapsedTime < reloadTime)
        {
            elapsedTime += Time.deltaTime;
            reloadProgress = elapsedTime / reloadTime;
            yield return null;
        }
        
        currentAmmo = maxAmmo;
        isReloading = false;
        reloadProgress = 0f;
    }

    IEnumerator PlayHitAnimationOnShoot()
    {
        if (spriteRenderer != null && hitColor != Color.clear)
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = hitColor;
            
            yield return new WaitForSeconds(hitAnimationDuration);
            
            spriteRenderer.color = originalColor;
        }
    }

    void PlayEmptyClipSound()
    {
        if (emptyClipSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(emptyClipSound);
        }
    }

    // ==== PUBLIC METHODS FOR EXTERNAL SCRIPTS ====
    
    // Basic state getters
    public bool IsGrounded() { return isGrounded; }
    public bool IsJumping() { return !isGrounded; }
    public bool IsDucking() { return isDucking; }
    public bool IsMoving() { return Mathf.Abs(movementInput) > 0.1f; }
    public bool CanJump() { return canJump; }
    public bool IsDead() { return isDead; }
    
    // Shooting system getters
    public int GetCurrentAmmo() { return currentAmmo; }
    public int GetMaxAmmo() { return maxAmmo; }
    public bool IsReloading() { return isReloading; }
    public float GetReloadProgress() { return reloadProgress; }
    
    // Character system getters
    public CharacterColor GetCharacterColor() { return characterColor; }
    
    // New ground stability getter
    public float GetGroundStability() { return groundStability; }
    
    // Jump system getters (for compatibility)
    public bool HasJumped() { return !canJump; }
    public bool IsJumpLocked() { return !canJump; }
    
    // Legacy compatibility methods
    public bool IsAgainstWall() { return false; }
    public bool IsWallHanging() { return false; }
    public float GetGroundedTime() { return isGrounded ? 1f : 0f; }
    
    // ==== SLOW MOTION SYSTEM (for GameOverTrigger compatibility) ====
    public void SetSlowMotion(float slowFactor)
    {
        currentSlowMotionFactor = Mathf.Clamp01(slowFactor);
        isInSlowMotion = currentSlowMotionFactor < 1f;
        
        // Update movement speed based on current state
        if (isDucking)
        {
            moveSpeed = originalMoveSpeed * duckSpeedMultiplier * currentSlowMotionFactor;
        }
        else
        {
            moveSpeed = originalMoveSpeed * currentSlowMotionFactor;
        }
        
        Debug.Log($"🐌 Slow motion set to: {currentSlowMotionFactor:F2}x speed");
    }
    
    public void ResetSlowMotion()
    {
        SetSlowMotion(1f);
    }
    
    public bool IsInSlowMotion()
    {
        return isInSlowMotion;
    }
    
    public float GetSlowMotionFactor()
    {
        return currentSlowMotionFactor;
    }
    
    // ==== SHOOTING MODE SETTERS ====
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
    
    // ==== CHARACTER SYSTEM ====
    public void ChangeCharacter(CharacterColor newColor)
    {
        characterColor = newColor;
        LoadCharacterSprites();
        
        if (spriteRenderer != null && currentCharacterSprites.idle != null)
        {
            spriteRenderer.sprite = currentCharacterSprites.idle;
            spriteRenderer.color = Color.white; // PASTIKAN TIDAK TRANSPARAN
            UpdateColliderToFitSprite(currentCharacterSprites.idle);
        }
        
        Debug.Log($"Changed character to: {characterColor}");
    }
    
    public void ResetSpriteColor()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white; // PASTIKAN TIDAK TRANSPARAN
            originalColor = spriteRenderer.color;
        }
    }
    
    public void TriggerHitAnimation()
    {
        StartCoroutine(PlayHitAnimationOnShoot());
    }
    
    // NEW: Public method to trigger death from external scripts
    public void Die(string cause = "External")
    {
        TriggerDeath(cause);
    }

    // ==== UNITY LIFECYCLE METHODS ====
    void OnValidate()
    {
        if (Application.isPlaying && gameObject.activeInHierarchy)
        {
            LoadCharacterSprites();
            if (spriteRenderer != null && currentCharacterSprites.idle != null)
            {
                spriteRenderer.sprite = currentCharacterSprites.idle;
                spriteRenderer.color = Color.white; // PASTIKAN TIDAK TRANSPARAN
                UpdateColliderToFitSprite(currentCharacterSprites.idle);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (capsuleCollider != null)
        {
            // Draw collider bounds
            Gizmos.color = Color.green;
            Vector3 colliderCenter = transform.position + (Vector3)capsuleCollider.offset;
            Gizmos.DrawWireCube(colliderCenter, capsuleCollider.size);
            
            if (Application.isPlaying)
            {
                // Draw ground check rays
                Vector2 centerBottom = (Vector2)transform.position + capsuleCollider.offset - new Vector2(0, capsuleCollider.size.y * 0.5f);
                Vector2 leftBottom = centerBottom - new Vector2(capsuleCollider.size.x * 0.35f, 0);
                Vector2 rightBottom = centerBottom + new Vector2(capsuleCollider.size.x * 0.35f, 0);
                Vector2 farLeftBottom = centerBottom - new Vector2(capsuleCollider.size.x * 0.45f, 0);
                Vector2 farRightBottom = centerBottom + new Vector2(capsuleCollider.size.x * 0.45f, 0);
                
                Gizmos.color = groundStability >= groundStabilityThreshold ? Color.green : Color.red;
                Gizmos.DrawLine(centerBottom, centerBottom + Vector2.down * groundCheckDistance);
                Gizmos.DrawLine(leftBottom, leftBottom + Vector2.down * groundCheckDistance);
                Gizmos.DrawLine(rightBottom, rightBottom + Vector2.down * groundCheckDistance);
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(farLeftBottom, farLeftBottom + Vector2.down * groundCheckDistance);
                Gizmos.DrawLine(farRightBottom, farRightBottom + Vector2.down * groundCheckDistance);
                
                // Draw jump ability indicator
                Gizmos.color = canJump ? Color.green : Color.red;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.3f);
                
                // Draw ground stability indicator
                Gizmos.color = Color.yellow;
                Vector3 stabilityPos = transform.position + Vector3.up * 2.5f;
                float stabilitySize = groundStability * 0.5f;
                Gizmos.DrawWireSphere(stabilityPos, stabilitySize);
                
                // Draw slow motion indicator
                if (isInSlowMotion)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireSphere(transform.position + Vector3.up * 3f, currentSlowMotionFactor * 0.3f);
                }
                
                // Draw death zone indicator
                if (enableDeathByFalling)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(new Vector3(-1000, fallDeathY, 0), new Vector3(1000, fallDeathY, 0));
                }
                
                // Draw death state indicator
                if (isDead)
                {
                    Gizmos.color = Color.black;
                    Gizmos.DrawWireSphere(transform.position + Vector3.up * 3.5f, 0.5f);
                }
            }
        }
    }
}