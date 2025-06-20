using UnityEngine;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    [Header("Enemy Type")]
    [SerializeField] private EnemyType enemyType = EnemyType.Slime_Normal;
    
    [Header("Animation Sprites")]
    [SerializeField] private Sprite[] idleSprites;
    [SerializeField] private Sprite[] walkSprites;
    [SerializeField] private Sprite[] attackSprites;
    [SerializeField] private Sprite[] alertSprites;
    [SerializeField] private Sprite[] deathSprites;
    
    [Header("Animation Settings")]
    [SerializeField] private float animationSpeed = 0.5f;
    [SerializeField] private bool loopAnimations = true;
    
    [Header("Barnacle Specific Settings")]
    public bool isBarnacle = false;
    public bool canShootOnly = false;
    
    [Header("Detection Settings")]
    public float detectionRange = 5f;
    public float attackRange = 1.5f;
    public float shootRange = 8f;
    public float losePlayerRange = 10f;
    public LayerMask playerLayer = (1 << 3); // Player layer
    public LayerMask obstacleLayer = (1 << 8); // Ground layer
    
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float chaseSpeed = 5f;
    public float patrolSpeed = 2f;
    
    [Header("Shooting Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 10f;
    public int bulletDamage = 1;
    public float shootCooldown = 2f;
    
    [Header("Patrol Settings")]
    public Transform[] patrolPoints;
    public float waitTime = 2f;
    public bool randomPatrol = false;
    
    [Header("Combat Settings")]
    public float attackCooldown = 1f;
    public int attackDamage = 1;
    public int maxHealth = 3;
    
    [Header("Abilities")]
    public bool canChase = true;
    public bool canAttack = true;
    public bool canShoot = false;
    public bool canPatrol = true;
    public bool returnToPatrolAfterLose = true;
    
    [Header("Effects")]
    public GameObject alertEffect;
    public GameObject attackEffect;
    
    [Header("Visual Settings")]
    public Color normalColor = Color.white;
    public Color alertColor = Color.yellow;
    public Color chaseColor = Color.red;
    
    [Header("Audio")]
    public AudioClip detectSound;
    public AudioClip attackSound;
    public AudioClip alertSound;

    [Header("Physics")]
    public LayerMask groundLayer = (1 << 8); // Ground layer
    public bool useGravity = true;
    public float jumpForce = 5f;
    
    [Header("Auto Collider Settings")]
    public bool autoAdjustCollider = true; // Otomatis sesuaikan dengan sprite
    public float colliderSizeMultiplier = 0.8f; // Berapa persen dari ukuran sprite (0.8 = 80%)
    public bool updateColliderPerFrame = false; // Update collider setiap frame sprite berubah
    
    // Private variables
    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private Collider2D enemyCollider;

    private EnemyGroundDetection groundDetection;
    private EnemyCollision enemyCollisionScript;
    
    // Animation variables
    private int currentSpriteIndex = 0;
    private float animationTimer = 0f;
    private Sprite[] currentAnimationSprites;
    private AnimationState currentAnimationState = AnimationState.Idle;
    
    // AI States
    public enum EnemyState
    {
        Idle,
        Patrol,
        Alert,
        Chase,
        Attack,
        Shoot,
        Return,
        Stunned,
        Death
    }
    
    public enum AnimationState
    {
        Idle,
        Walk,
        Attack,
        Alert,
        Death
    }
    
    public enum EnemyType
    {
        Slime_Normal,
        Slime_Fire,
        Slime_Spike,
        Bee,
        Fish_Blue,
        Fish_Yellow,
        Fish_Purple,
        Frog,
        Ladybug,
        Mouse,
        Snail,
        Worm_Normal,
        Worm_Ring,
        Fly,
        Saw,
        Barnacle
    }
    
    [SerializeField] private EnemyState currentState = EnemyState.Idle;
    
    // Movement variables
    private Vector2 startPosition;
    private int currentPatrolIndex = 0;
    private bool movingToNextPoint = true;
    private float waitTimer = 0f;
    
    // Combat variables
    private float lastAttackTime = 0f;
    private float lastShootTime = 0f;
    private bool playerInRange = false;
    private bool playerInShootRange = false;
    private bool playerDetected = false;
    private float losePlayerTimer = 0f;
    private Vector2 lastKnownPlayerPosition;
    private bool isDead = false;
    private int currentHealth;
    
    void Start()
    {
        InitializeComponents();
        SetupEnemySprites();
        InitializeAI();
        currentHealth = maxHealth;
    }

    void InitializeComponents()
    {
        // Setup Rigidbody2D - OTOMATIS
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            Debug.Log("✅ Rigidbody2D otomatis ditambahkan ke " + gameObject.name);
        }
        
        // Setup SpriteRenderer - OTOMATIS
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            Debug.Log("✅ SpriteRenderer otomatis ditambahkan ke " + gameObject.name);
        }
        
        // Setup Collider2D - OTOMATIS berdasarkan ukuran sprite
        enemyCollider = GetComponent<Collider2D>();
        if (enemyCollider == null)
        {
            BoxCollider2D boxCollider = gameObject.AddComponent<BoxCollider2D>();
            enemyCollider = boxCollider;
            Debug.Log("✅ BoxCollider2D otomatis ditambahkan ke " + gameObject.name);
        }
        
        // Setup physics
        if (rb != null)
        {
            if (useGravity)
            {
                rb.gravityScale = 3f;
                rb.freezeRotation = true;
            }
            else
            {
                rb.gravityScale = 0f;
            }
            
            rb.mass = 1f;
            rb.drag = 2f;
            rb.angularDrag = 50f;
        }

        // Setup audio - OTOMATIS
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            Debug.Log("✅ AudioSource otomatis ditambahkan ke " + gameObject.name);
        }

        // Setup EnemyCollision script - OTOMATIS
        enemyCollisionScript = GetComponent<EnemyCollision>();
        if (enemyCollisionScript == null)
        {
            enemyCollisionScript = gameObject.AddComponent<EnemyCollision>();
            Debug.Log("✅ EnemyCollision script otomatis ditambahkan ke " + gameObject.name);
        }

        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            Debug.Log("✅ Player ditemukan: " + player.name);
        }
        else
        {
            Debug.LogWarning("⚠️ Player dengan tag 'Player' tidak ditemukan!");
        }

        // Setup fire point if not assigned
        if (firePoint == null && canShoot)
        {
            GameObject firePointObj = new GameObject("EnemyFirePoint");
            firePointObj.transform.SetParent(transform);
            firePointObj.transform.localPosition = new Vector3(0.5f, 0, 0);
            firePoint = firePointObj.transform;
        }
        
        // Setup ground detection - OTOMATIS
        groundDetection = GetComponent<EnemyGroundDetection>();
        if (groundDetection == null && useGravity)
        {
            groundDetection = gameObject.AddComponent<EnemyGroundDetection>();
            groundDetection.groundLayer = groundLayer;
            Debug.Log("✅ EnemyGroundDetection otomatis ditambahkan ke " + gameObject.name);
        }
    }
    
    void SetupEnemySprites()
    {
        string baseName = GetEnemyBaseName();
        Debug.Log($"🎨 Setting up sprites untuk {baseName}...");
        
        // Special handling for Barnacle
        if (enemyType == EnemyType.Barnacle)
        {
            isBarnacle = true;
            canShootOnly = true;
            canChase = false;
            canPatrol = false;
            canShoot = true;
            
            if (idleSprites.Length == 0)
            {
                idleSprites = LoadSpritesForAnimation(baseName, "rest");
            }
            
            if (walkSprites.Length == 0)
            {
                walkSprites = LoadSpritesForAnimation(baseName, "move");
            }
            
            if (attackSprites.Length == 0)
            {
                attackSprites = LoadSpritesForAnimation(baseName, "attack");
            }
        }
        else
        {
            if (idleSprites.Length == 0)
            {
                idleSprites = LoadSpritesForAnimation(baseName, "rest");
            }
            
            if (walkSprites.Length == 0)
            {
                walkSprites = LoadSpritesForAnimation(baseName, "walk");
            }
            
            if (attackSprites.Length == 0)
            {
                attackSprites = LoadSpritesForAnimation(baseName, "attack");
            }
        }
        
        if (alertSprites.Length == 0)
        {
            alertSprites = LoadSpritesForAnimation(baseName, "idle");
        }
        
        if (deathSprites.Length == 0)
        {
            deathSprites = LoadSpritesForAnimation(baseName, "flat");
        }
        
        // PERBAIKAN: Set sprite pertama SEBELUM PlayAnimation
        SetInitialSprite();
        
        // Kemudian play animation
        PlayAnimation(AnimationState.Idle);
        
        // OTOMATIS sesuaikan collider setelah sprite di-set
        StartCoroutine(DelayedAutoAdjustCollider());
    }
    
    // Method untuk set sprite pertama langsung
    void SetInitialSprite()
    {
        Sprite firstSprite = null;
        
        // Cari sprite pertama yang tersedia
        if (idleSprites != null && idleSprites.Length > 0 && idleSprites[0] != null)
        {
            firstSprite = idleSprites[0];
        }
        else if (walkSprites != null && walkSprites.Length > 0 && walkSprites[0] != null)
        {
            firstSprite = walkSprites[0];
        }
        else if (attackSprites != null && attackSprites.Length > 0 && attackSprites[0] != null)
        {
            firstSprite = attackSprites[0];
        }
        
        if (firstSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = firstSprite;
            spriteRenderer.color = normalColor;
            Debug.Log($"✅ Sprite pertama di-set: {firstSprite.name}");
        }
        else
        {
            Debug.LogWarning($"⚠️ Tidak ada sprite yang ditemukan untuk {gameObject.name}!");
            
            // Fallback: coba load sprite default langsung
            string baseName = GetEnemyBaseName();
            Sprite fallbackSprite = Resources.Load<Sprite>("Enemies/" + baseName + "_rest");
            if (fallbackSprite == null)
            {
                fallbackSprite = Resources.Load<Sprite>(baseName + "_rest");
            }
            
            if (fallbackSprite != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = fallbackSprite;
                spriteRenderer.color = normalColor;
                Debug.Log($"✅ Fallback sprite di-set: {fallbackSprite.name}");
            }
        }
    }
    
    // Delayed auto adjust collider untuk memastikan sprite sudah di-load
    IEnumerator DelayedAutoAdjustCollider()
    {
        yield return new WaitForEndOfFrame(); // Wait 1 frame
        AutoAdjustColliderToSprite();
    }
    
    // Method untuk otomatis menyesuaikan collider dengan ukuran sprite
    void AutoAdjustColliderToSprite()
    {
        if (!autoAdjustCollider) return;
        
        if (spriteRenderer == null || spriteRenderer.sprite == null || enemyCollider == null)
        {
            Debug.LogWarning($"⚠️ Tidak bisa auto-adjust collider untuk {gameObject.name}: SpriteRenderer atau Sprite null");
            return;
        }
        
        if (enemyCollider is BoxCollider2D)
        {
            BoxCollider2D boxCollider = (BoxCollider2D)enemyCollider;
            Sprite currentSprite = spriteRenderer.sprite;
            
            // Dapatkan ukuran sprite dalam world units
            Vector2 spriteSize = currentSprite.bounds.size;
            
            // Sesuaikan ukuran collider menggunakan multiplier yang bisa diatur
            Vector2 newColliderSize = spriteSize * colliderSizeMultiplier;
            
            // Set ukuran collider
            boxCollider.size = newColliderSize;
            
            // Auto-calculate offset untuk menempatkan collider di bagian bawah sprite (kaki enemy)
            float offsetY = -(spriteSize.y - newColliderSize.y) / 2f;
            boxCollider.offset = new Vector2(0, offsetY);
            
            Debug.Log($"🎯 Auto-adjusted collider untuk {gameObject.name}:");
            Debug.Log($"   📏 Sprite size: {spriteSize}");
            Debug.Log($"   📦 Collider size: {newColliderSize} ({colliderSizeMultiplier * 100}% dari sprite)");
            Debug.Log($"   📍 Collider offset: {boxCollider.offset}");
        }
    }

    // Method untuk update collider saat sprite berubah (saat animasi)
    void UpdateColliderForCurrentSprite()
    {
        if (!updateColliderPerFrame || !autoAdjustCollider) return;
        
        if (spriteRenderer != null && spriteRenderer.sprite != null && enemyCollider is BoxCollider2D)
        {
            BoxCollider2D boxCollider = (BoxCollider2D)enemyCollider;
            Sprite currentSprite = spriteRenderer.sprite;
            
            Vector2 newSpriteSize = currentSprite.bounds.size;
            Vector2 currentColliderSize = boxCollider.size;
            
            if (Mathf.Abs(newSpriteSize.x - currentColliderSize.x / colliderSizeMultiplier) > 0.1f ||
                Mathf.Abs(newSpriteSize.y - currentColliderSize.y / colliderSizeMultiplier) > 0.1f)
            {
                Vector2 newColliderSize = newSpriteSize * colliderSizeMultiplier;
                boxCollider.size = newColliderSize;
                
                float offsetY = -(newSpriteSize.y - newColliderSize.y) / 2f;
                boxCollider.offset = new Vector2(0, offsetY);
                
                Debug.Log($"🔄 Updated collider size untuk {gameObject.name}: {newColliderSize}");
            }
        }
    }
    
    string GetEnemyBaseName()
    {
        switch (enemyType)
        {
            case EnemyType.Slime_Normal: return "slime_normal";
            case EnemyType.Slime_Fire: return "slime_fire";
            case EnemyType.Slime_Spike: return "slime_spike";
            case EnemyType.Bee: return "bee";
            case EnemyType.Fish_Blue: return "fish_blue";
            case EnemyType.Fish_Yellow: return "fish_yellow";
            case EnemyType.Fish_Purple: return "fish_purple";
            case EnemyType.Frog: return "frog";
            case EnemyType.Ladybug: return "ladybug";
            case EnemyType.Mouse: return "mouse";
            case EnemyType.Snail: return "snail";
            case EnemyType.Worm_Normal: return "worm_normal";
            case EnemyType.Worm_Ring: return "worm_ring";
            case EnemyType.Fly: return "fly";
            case EnemyType.Saw: return "saw";
            case EnemyType.Barnacle: return "barnacle";
            default: return "slime_normal";
        }
    }
    
    Sprite[] LoadSpritesForAnimation(string baseName, string animationType)
    {
        System.Collections.Generic.List<Sprite> sprites = new System.Collections.Generic.List<Sprite>();
        
        Debug.Log($"🔍 Loading sprites: {baseName} - {animationType}");
        
        switch (animationType)
        {
            case "rest":
                Sprite restSprite = Resources.Load<Sprite>("Enemies/" + baseName + "_rest");
                if (restSprite == null)
                {
                    restSprite = Resources.Load<Sprite>(baseName + "_rest");
                }
                if (restSprite != null) 
                {
                    sprites.Add(restSprite);
                    Debug.Log($"   ✅ Found rest sprite: {restSprite.name}");
                }
                else
                {
                    Debug.LogWarning($"   ❌ Rest sprite not found for {baseName}");
                }
                break;
                
            case "walk":
                Sprite walkSpriteA = Resources.Load<Sprite>("Enemies/" + baseName + "_walk_a");
                Sprite walkSpriteB = Resources.Load<Sprite>("Enemies/" + baseName + "_walk_b");
                
                if (walkSpriteA == null) walkSpriteA = Resources.Load<Sprite>(baseName + "_walk_a");
                if (walkSpriteB == null) walkSpriteB = Resources.Load<Sprite>(baseName + "_walk_b");
                
                if (walkSpriteA != null) 
                {
                    sprites.Add(walkSpriteA);
                    Debug.Log($"   ✅ Found walk_a sprite: {walkSpriteA.name}");
                }
                if (walkSpriteB != null) 
                {
                    sprites.Add(walkSpriteB);
                    Debug.Log($"   ✅ Found walk_b sprite: {walkSpriteB.name}");
                }
                
                if (sprites.Count == 0)
                {
                    Sprite moveSpriteA = Resources.Load<Sprite>("Enemies/" + baseName + "_move_a");
                    Sprite moveSpriteB = Resources.Load<Sprite>("Enemies/" + baseName + "_move_b");
                    
                    if (moveSpriteA == null) moveSpriteA = Resources.Load<Sprite>(baseName + "_move_a");
                    if (moveSpriteB == null) moveSpriteB = Resources.Load<Sprite>(baseName + "_move_b");
                    
                    if (moveSpriteA != null) 
                    {
                        sprites.Add(moveSpriteA);
                        Debug.Log($"   ✅ Found move_a sprite: {moveSpriteA.name}");
                    }
                    if (moveSpriteB != null) 
                    {
                        sprites.Add(moveSpriteB);
                        Debug.Log($"   ✅ Found move_b sprite: {moveSpriteB.name}");
                    }
                }
                break;
                
            case "move":
                Sprite moveA = Resources.Load<Sprite>("Enemies/" + baseName + "_a");
                Sprite moveB = Resources.Load<Sprite>("Enemies/" + baseName + "_b");
                
                if (moveA == null) moveA = Resources.Load<Sprite>(baseName + "_a");
                if (moveB == null) moveB = Resources.Load<Sprite>(baseName + "_b");
                
                if (moveA != null) 
                {
                    sprites.Add(moveA);
                    Debug.Log($"   ✅ Found _a sprite: {moveA.name}");
                }
                if (moveB != null) 
                {
                    sprites.Add(moveB);
                    Debug.Log($"   ✅ Found _b sprite: {moveB.name}");
                }
                
                if (sprites.Count == 0)
                {
                    moveA = Resources.Load<Sprite>("Enemies/" + baseName + "_move_a");
                    moveB = Resources.Load<Sprite>("Enemies/" + baseName + "_move_b");
                    
                    if (moveA == null) moveA = Resources.Load<Sprite>(baseName + "_move_a");
                    if (moveB == null) moveB = Resources.Load<Sprite>(baseName + "_move_b");
                    
                    if (moveA != null) 
                    {
                        sprites.Add(moveA);
                        Debug.Log($"   ✅ Found move_a sprite: {moveA.name}");
                    }
                    if (moveB != null) 
                    {
                        sprites.Add(moveB);
                        Debug.Log($"   ✅ Found move_b sprite: {moveB.name}");
                    }
                }
                break;
                
            case "attack":
                Sprite attackA = Resources.Load<Sprite>("Enemies/" + baseName + "_attack_a");
                Sprite attackB = Resources.Load<Sprite>("Enemies/" + baseName + "_attack_b");
                Sprite attackRest = Resources.Load<Sprite>("Enemies/" + baseName + "_attack_rest");
                Sprite jump = Resources.Load<Sprite>("Enemies/" + baseName + "_jump");
                Sprite fly = Resources.Load<Sprite>("Enemies/" + baseName + "_fly");
                
                if (attackA == null) attackA = Resources.Load<Sprite>(baseName + "_attack_a");
                if (attackB == null) attackB = Resources.Load<Sprite>(baseName + "_attack_b");
                if (attackRest == null) attackRest = Resources.Load<Sprite>(baseName + "_attack_rest");
                if (jump == null) jump = Resources.Load<Sprite>(baseName + "_jump");
                if (fly == null) fly = Resources.Load<Sprite>(baseName + "_fly");
                
                if (attackA != null) 
                {
                    sprites.Add(attackA);
                    Debug.Log($"   ✅ Found attack_a sprite: {attackA.name}");
                }
                if (attackB != null) 
                {
                    sprites.Add(attackB);
                    Debug.Log($"   ✅ Found attack_b sprite: {attackB.name}");
                }
                if (attackRest != null) 
                {
                    sprites.Add(attackRest);
                    Debug.Log($"   ✅ Found attack_rest sprite: {attackRest.name}");
                }
                if (jump != null) 
                {
                    sprites.Add(jump);
                    Debug.Log($"   ✅ Found jump sprite: {jump.name}");
                }
                if (fly != null) 
                {
                    sprites.Add(fly);
                    Debug.Log($"   ✅ Found fly sprite: {fly.name}");
                }
                break;
                
            case "idle":
                Sprite idle = Resources.Load<Sprite>("Enemies/" + baseName + "_idle");
                if (idle == null) idle = Resources.Load<Sprite>(baseName + "_idle");
                if (idle != null) 
                {
                    sprites.Add(idle);
                    Debug.Log($"   ✅ Found idle sprite: {idle.name}");
                }
                break;
                
            case "flat":
                Sprite flat = Resources.Load<Sprite>("Enemies/" + baseName + "_flat");
                Sprite shell = Resources.Load<Sprite>("Enemies/" + baseName + "_shell");
                
                if (flat == null) flat = Resources.Load<Sprite>(baseName + "_flat");
                if (shell == null) shell = Resources.Load<Sprite>(baseName + "_shell");
                
                if (flat != null) 
                {
                    sprites.Add(flat);
                    Debug.Log($"   ✅ Found flat sprite: {flat.name}");
                }
                if (shell != null) 
                {
                    sprites.Add(shell);
                    Debug.Log($"   ✅ Found shell sprite: {shell.name}");
                }
                break;
        }
        
        Debug.Log($"📊 Total loaded {sprites.Count} sprites for {baseName} - {animationType}");
        
        return sprites.ToArray();
    }
    
    void InitializeAI()
    {
        startPosition = transform.position;
        
        if (canPatrol && patrolPoints.Length > 0)
        {
            currentState = EnemyState.Patrol;
        }
        else
        {
            currentState = EnemyState.Idle;
        }
        
        if (alertEffect != null)
        {
            alertEffect.SetActive(false);
        }
        
        if (attackEffect != null)
        {
            attackEffect.SetActive(false);
        }
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
        }
        
        Debug.Log($"🤖 AI initialized for {gameObject.name} - State: {currentState}");
    }
    
    void Update()
    {
        if (isDead || player == null) return;
        
        DetectPlayer();
        UpdateAI();
        UpdateVisuals();
        UpdateAnimation();
        UpdateAiming();
    }
    
    void UpdateAnimation()
    {
        if (currentAnimationSprites == null || currentAnimationSprites.Length == 0) return;
        
        animationTimer += Time.deltaTime;
        
        if (animationTimer >= animationSpeed)
        {
            animationTimer = 0f;
            
            if (loopAnimations)
            {
                currentSpriteIndex = (currentSpriteIndex + 1) % currentAnimationSprites.Length;
            }
            else
            {
                if (currentSpriteIndex < currentAnimationSprites.Length - 1)
                {
                    currentSpriteIndex++;
                }
            }
            
            if (currentSpriteIndex < currentAnimationSprites.Length && spriteRenderer != null)
            {
                Sprite previousSprite = spriteRenderer.sprite;
                spriteRenderer.sprite = currentAnimationSprites[currentSpriteIndex];
                
                // OTOMATIS update collider jika sprite berubah ukuran
                if (previousSprite != spriteRenderer.sprite)
                {
                    UpdateColliderForCurrentSprite();
                }
            }
        }
    }
    
    void PlayAnimation(AnimationState newState)
    {
        if (currentAnimationState == newState) return;
        
        currentAnimationState = newState;
        currentSpriteIndex = 0;
        animationTimer = 0f;
        
        switch (newState)
        {
            case AnimationState.Idle:
                currentAnimationSprites = idleSprites;
                break;
                
            case AnimationState.Walk:
                currentAnimationSprites = walkSprites;
                break;
                
            case AnimationState.Attack:
                currentAnimationSprites = attackSprites;
                break;
                
            case AnimationState.Alert:
                currentAnimationSprites = alertSprites;
                break;
                
            case AnimationState.Death:
                currentAnimationSprites = deathSprites;
                loopAnimations = false;
                break;
        }
        
        // PERBAIKAN: Pastikan sprite di-set dengan benar
        if (currentAnimationSprites != null && currentAnimationSprites.Length > 0 && spriteRenderer != null)
        {
            if (currentAnimationSprites[0] != null)
            {
                spriteRenderer.sprite = currentAnimationSprites[0];
                Debug.Log($"🎬 Playing animation: {newState} - Sprite: {currentAnimationSprites[0].name}");
            }
            else
            {
                Debug.LogWarning($"⚠️ Animation sprite is null for {newState}");
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ No animation sprites found for {newState}");
        }
    }
    
    void DetectPlayer()
    {
        if (player == null) return;
        
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        if (distanceToPlayer <= detectionRange)
        {
            Vector2 directionToPlayer = (player.position - transform.position).normalized;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer, detectionRange, obstacleLayer);
            
            if (hit.collider == null || hit.collider.CompareTag("Player"))
            {
                if (!playerDetected)
                {
                    OnPlayerDetected();
                }
                playerDetected = true;
                lastKnownPlayerPosition = player.position;
                losePlayerTimer = 0f;
            }
        }
        else if (distanceToPlayer > losePlayerRange)
        {
            losePlayerTimer += Time.deltaTime;
            if (losePlayerTimer > 2f && playerDetected)
            {
                OnPlayerLost();
            }
        }
        
        playerInRange = distanceToPlayer <= attackRange;
        playerInShootRange = distanceToPlayer <= shootRange && distanceToPlayer > attackRange;
    }
    
    void UpdateAI()
    {
        switch (currentState)
        {
            case EnemyState.Idle:
                HandleIdleState();
                break;
            case EnemyState.Patrol:
                HandlePatrolState();
                break;
            case EnemyState.Alert:
                HandleAlertState();
                break;
            case EnemyState.Chase:
                HandleChaseState();
                break;
            case EnemyState.Attack:
                HandleAttackState();
                break;
            case EnemyState.Shoot:
                HandleShootState();
                break;
            case EnemyState.Return:
                HandleReturnState();
                break;
            case EnemyState.Stunned:
                HandleStunnedState();
                break;
            case EnemyState.Death:
                HandleDeathState();
                break;
        }
    }
    
    void HandleIdleState()
    {
        if (useGravity)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
        else
        {
            rb.velocity = Vector2.zero;
        }
        
        PlayAnimation(AnimationState.Idle);
        
        if (playerDetected)
        {
            if (isBarnacle && canShoot)
            {
                ChangeState(EnemyState.Shoot);
            }
            else if (canChase)
            {
                ChangeState(EnemyState.Chase);
            }
        }
        else if (canPatrol && patrolPoints.Length > 0)
        {
            ChangeState(EnemyState.Patrol);
        }
    }
    
    void HandlePatrolState()
    {
        PlayAnimation(AnimationState.Walk);
        
        if (playerDetected && canChase)
        {
            ChangeState(EnemyState.Chase);
            return;
        }
        
        if (patrolPoints.Length == 0)
        {
            ChangeState(EnemyState.Idle);
            return;
        }
        
        if (movingToNextPoint)
        {
            MoveTowardsTarget(patrolPoints[currentPatrolIndex].position, patrolSpeed);
            
            if (Vector2.Distance(transform.position, patrolPoints[currentPatrolIndex].position) < 0.5f)
            {
                movingToNextPoint = false;
                waitTimer = waitTime;
                PlayAnimation(AnimationState.Idle);
            }
        }
        else
        {
            if (useGravity)
            {
                rb.velocity = new Vector2(0, rb.velocity.y);
            }
            else
            {
                rb.velocity = Vector2.zero;
            }
            
            waitTimer -= Time.deltaTime;
            
            if (waitTimer <= 0f)
            {
                GetNextPatrolPoint();
                movingToNextPoint = true;
            }
        }
    }
    
    void HandleAlertState()
    {
        if (useGravity)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
        else
        {
            rb.velocity = Vector2.zero;
        }
        
        PlayAnimation(AnimationState.Alert);
        
        waitTimer -= Time.deltaTime;
        if (waitTimer <= 0f)
        {
            if (playerDetected)
            {
                if (isBarnacle && canShoot)
                {
                    ChangeState(EnemyState.Shoot);
                }
                else if (canChase)
                {
                    ChangeState(EnemyState.Chase);
                }
            }
            else
            {
                ChangeState(EnemyState.Idle);
            }
        }
    }
    
    void HandleChaseState()
    {
        if (walkSprites.Length > 0)
        {
            PlayAnimation(AnimationState.Walk);
        }
        else
        {
            PlayAnimation(AnimationState.Idle);
        }
        
        if (!playerDetected)
        {
            if (returnToPatrolAfterLose)
            {
                ChangeState(EnemyState.Return);
            }
            else
            {
                ChangeState(EnemyState.Idle);
            }
            return;
        }
        
        if (playerInRange && canAttack)
        {
            ChangeState(EnemyState.Attack);
            return;
        }
        
        if (playerInShootRange && canShoot && HasClearShot())
        {
            ChangeState(EnemyState.Shoot);
            return;
        }
        
        MoveTowardsTarget(player.position, chaseSpeed);
    }
    
    void HandleAttackState()
    {
        if (useGravity)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
        else
        {
            rb.velocity = Vector2.zero;
        }
        
        PlayAnimation(AnimationState.Attack);
        
        if (!playerInRange)
        {
            if (playerDetected)
            {
                ChangeState(EnemyState.Chase);
            }
            else
            {
                ChangeState(EnemyState.Idle);
            }
            return;
        }
        
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            PerformAttack();
        }
    }
    
    void HandleShootState()
    {
        if (useGravity)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
        else
        {
            rb.velocity = Vector2.zero;
        }
        
        if (isBarnacle && walkSprites.Length > 0)
        {
            PlayAnimation(AnimationState.Walk);
        }
        else
        {
            PlayAnimation(AnimationState.Attack);
        }
        
        if (!playerDetected)
        {
            if (isBarnacle)
            {
                ChangeState(EnemyState.Idle);
            }
            else
            {
                ChangeState(EnemyState.Chase);
            }
            return;
        }
        
        if (playerInRange && canAttack && !isBarnacle)
        {
            ChangeState(EnemyState.Attack);
            return;
        }
        
        if (!playerInShootRange)
        {
            if (!isBarnacle)
            {
                ChangeState(EnemyState.Chase);
            }
            else
            {
                ChangeState(EnemyState.Idle);
            }
            return;
        }
        
        if (Time.time - lastShootTime >= shootCooldown && HasClearShot())
        {
            ShootAtPlayer();
        }
    }
    
    void HandleReturnState()
    {
        if (walkSprites.Length > 0)
        {
            PlayAnimation(AnimationState.Walk);
        }
        else
        {
            PlayAnimation(AnimationState.Idle);
        }
        
        if (playerDetected && canChase)
        {
            ChangeState(EnemyState.Chase);
            return;
        }
        
        Vector2 targetPosition = patrolPoints.Length > 0 ? patrolPoints[currentPatrolIndex].position : startPosition;
        MoveTowardsTarget(targetPosition, moveSpeed);
        
        if (Vector2.Distance(transform.position, targetPosition) < 0.5f)
        {
            if (canPatrol && patrolPoints.Length > 0)
            {
                ChangeState(EnemyState.Patrol);
            }
            else
            {
                ChangeState(EnemyState.Idle);
            }
        }
    }
    
    void HandleStunnedState()
    {
        if (useGravity)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
        else
        {
            rb.velocity = Vector2.zero;
        }
        
        PlayAnimation(AnimationState.Idle);
    }
    
    void HandleDeathState()
    {
        if (useGravity)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
        else
        {
            rb.velocity = Vector2.zero;
        }
        
        PlayAnimation(AnimationState.Death);
    }
    
    void MoveTowardsTarget(Vector2 target, float speed)
    {
        Vector2 direction = (target - (Vector2)transform.position).normalized;
        
        if (useGravity)
        {
            rb.velocity = new Vector2(direction.x * speed, rb.velocity.y);
        }
        else
        {
            rb.velocity = direction * speed;
        }
        
        if (spriteRenderer != null && Mathf.Abs(direction.x) > 0.1f)
        {
            spriteRenderer.flipX = direction.x < 0;
        }
    }
    
    void GetNextPatrolPoint()
    {
        if (randomPatrol)
        {
            currentPatrolIndex = Random.Range(0, patrolPoints.Length);
        }
        else
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }
    }
    
    bool HasClearShot()
    {
        if (player == null) return false;
        
        Vector2 directionToPlayer = (player.position - transform.position).normalized;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer, shootRange, obstacleLayer);
        
        return hit.collider == null || hit.collider.CompareTag("Player");
    }
    
    void ShootAtPlayer()
    {
        if (bulletPrefab == null || firePoint == null || player == null) return;
        
        lastShootTime = Time.time;
        
        Vector2 direction = (player.position - firePoint.position).normalized;
        
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        
        if (bulletScript != null)
        {
            bulletScript.Initialize(direction, false, bulletDamage);
        }
        else
        {
            Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
            if (bulletRb != null)
            {
                bulletRb.velocity = direction * bulletSpeed;
            }
        }
        
        PlaySound(attackSound);
    }
    
    void PerformAttack()
    {
        if (player == null) return;
        
        lastAttackTime = Time.time;
        
        HealthSystem playerHealth = player.GetComponent<HealthSystem>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
        }
        
        PlaySound(attackSound);
        StartCoroutine(ShowAttackEffect());
    }
    
    void UpdateAiming()
    {
        if (firePoint == null || player == null) return;
        
        Vector2 direction = (player.position - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        firePoint.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }
    
    IEnumerator ShowAttackEffect()
    {
        if (attackEffect != null)
        {
            attackEffect.SetActive(true);
            yield return new WaitForSeconds(0.3f);
            attackEffect.SetActive(false);
        }
    }
    
    void OnPlayerDetected()
    {
        PlaySound(detectSound);
        
        if (alertEffect != null)
        {
            alertEffect.SetActive(true);
        }
        
        ChangeState(EnemyState.Alert);
        waitTimer = 1f;
    }
    
    void OnPlayerLost()
    {
        playerDetected = false;
        
        if (alertEffect != null)
        {
            alertEffect.SetActive(false);
        }
        
        if (returnToPatrolAfterLose)
        {
            ChangeState(EnemyState.Return);
        }
        else
        {
            ChangeState(EnemyState.Idle);
        }
    }
    
    void ChangeState(EnemyState newState)
    {
        currentState = newState;
        
        switch (newState)
        {
            case EnemyState.Alert:
                PlaySound(alertSound);
                break;
        }
    }
    
    void UpdateVisuals()
    {
        if (spriteRenderer == null) return;
        
        Color targetColor = normalColor;
        
        switch (currentState)
        {
            case EnemyState.Alert:
                targetColor = alertColor;
                break;
            case EnemyState.Chase:
            case EnemyState.Attack:
            case EnemyState.Shoot:
                targetColor = chaseColor;
                break;
            default:
                targetColor = normalColor;
                break;
        }
        
        spriteRenderer.color = Color.Lerp(spriteRenderer.color, targetColor, Time.deltaTime * 5f);
    }
    
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    public void TakeDamage(int damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(FlashRed());
        }
    }
    
    IEnumerator FlashRed()
    {
        if (spriteRenderer != null)
        {
            Color original = spriteRenderer.color;
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = original;
        }
    }
    
    void Die()
    {
        isDead = true;
        ChangeState(EnemyState.Death);
        
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }
        
        Destroy(gameObject, 2f);
    }
    
    public void Stun(float duration)
    {
        StartCoroutine(StunCoroutine(duration));
    }
    
    IEnumerator StunCoroutine(float duration)
    {
        EnemyState previousState = currentState;
        ChangeState(EnemyState.Stunned);
        
        yield return new WaitForSeconds(duration);
        
        if (!isDead)
        {
            ChangeState(previousState);
        }
    }

    // Method untuk debugging collision
    void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"Enemy {gameObject.name} bertabrakan dengan {collision.gameObject.name} (Tag: {collision.gameObject.tag})");
        
        if (collision.gameObject.CompareTag("Ground") || ((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            Debug.Log("Enemy menyentuh ground!");
        }
        
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Enemy menyentuh player!");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Enemy {gameObject.name} trigger dengan {other.gameObject.name} (Tag: {other.gameObject.tag})");
    }
}