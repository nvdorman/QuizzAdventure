using UnityEngine;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    [Header("Enemy Type")]
    [SerializeField] private EnemyType enemyType = EnemyType.Slime_Normal;
    
    [Header("Guardian Settings")]
    public GuardianType guardianType = GuardianType.Patrol;
    public float guardAreaRadius = 5f; // Radius area yang dijaga
    public bool stayInGuardArea = true; // Apakah musuh harus tetap di area
    public Vector2 guardCenter; // Pusat area yang dijaga
    [SerializeField] private bool showGuardArea = true; // Show guard area in editor
    
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
    public bool useGravity = true;
    
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
    public GameObject attackEffect;
    public GameObject alertEffect;
    public GameObject damageEffect;
    public GameObject deathEffect;
    
    [Header("Audio")]
    public AudioClip detectSound;
    public AudioClip alertSound;
    public AudioClip attackSound;
    public AudioClip hurtSound;
    public AudioClip deathSound;
    public AudioClip walkSound;
    
    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color alertColor = Color.yellow;
    public Color chaseColor = Color.red;
    public Color guardAreaColor = Color.cyan;
    
    // Components
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private Transform player;
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
    
    public enum GuardianType
    {
        Patrol,      // Bergerak patrol seperti biasa
        Stationary,  // Diam di tempat seperti penjaga
        Flying,      // Terbang di area tertentu seperti lebah
        Ground       // Bergerak di ground saja
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
        SetupGuardianBehavior();
        InitializeAI();
        currentHealth = maxHealth;
    }
    
    void InitializeComponents()
    {
        // Auto-create Rigidbody2D if missing
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            Debug.Log($"🔧 Auto-created Rigidbody2D for {gameObject.name}");
        }
        
        // Auto-create SpriteRenderer if missing
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            Debug.Log($"🔧 Auto-created SpriteRenderer for {gameObject.name}");
        }
        
        // Auto-create Collider2D if missing
        enemyCollider = GetComponent<Collider2D>();
        if (enemyCollider == null)
        {
            // Create BoxCollider2D as default
            BoxCollider2D boxCollider = gameObject.AddComponent<BoxCollider2D>();
            boxCollider.size = new Vector2(1f, 1f);
            boxCollider.isTrigger = true; // Set as trigger for collision detection
            enemyCollider = boxCollider;
            Debug.Log($"🔧 Auto-created BoxCollider2D for {gameObject.name}");
        }
        
        // Setup AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            Debug.Log($"🔧 Auto-created AudioSource for {gameObject.name}");
        }
        
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogWarning($"⚠️ Player not found for {gameObject.name}!");
        }
        
        // Get optional components
        groundDetection = GetComponent<EnemyGroundDetection>();
        enemyCollisionScript = GetComponent<EnemyCollision>();
        
        // Setup rigidbody properties
        if (rb != null)
        {
            rb.gravityScale = useGravity ? 1f : 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            
            // Set constraints based on enemy type
            if (!useGravity && guardianType == GuardianType.Flying)
            {
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
            else
            {
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
        }
        
        Debug.Log($"🤖 All components initialized for {gameObject.name}");
    }

    void SafeSetVelocity(Vector2 velocity)
    {
        if (rb != null)
        {
            rb.velocity = velocity;
        }
        else
        {
            Debug.LogWarning($"⚠️ Rigidbody2D is null for {gameObject.name}!");
        }
    }

    Vector2 SafeGetVelocity()
    {
        if (rb != null)
        {
            return rb.velocity;
        }
        else
        {
            Debug.LogWarning($"⚠️ Rigidbody2D is null for {gameObject.name}!");
            return Vector2.zero;
        }
    }
    
    void SetupGuardianBehavior()
    {
        guardCenter = transform.position; // Set guard center ke posisi awal
        
        switch (guardianType)
        {
            case GuardianType.Stationary:
                // Musuh diam di tempat, hanya detect dan attack/shoot
                canPatrol = false;
                canChase = false;
                canAttack = true;
                canShoot = true;
                moveSpeed = 0f; // No movement
                Debug.Log($"🛡️ {gameObject.name} setup as STATIONARY guardian");
                break;
                
            case GuardianType.Flying:
                // Musuh terbang seperti lebah, bergerak tapi tetap di area
                useGravity = false; // Tidak terpengaruh gravitasi
                if (rb != null) rb.gravityScale = 0f;
                canPatrol = true;
                canChase = true;
                canAttack = true;
                canShoot = true;
                moveSpeed = 2f; // Slower flying speed
                chaseSpeed = 3f;
                Debug.Log($"🐝 {gameObject.name} setup as FLYING guardian");
                break;
                
            case GuardianType.Ground:
                // Musuh ground normal
                useGravity = true;
                if (rb != null) rb.gravityScale = 1f;
                canPatrol = true;
                canChase = true;
                canAttack = true;
                Debug.Log($"🐛 {gameObject.name} setup as GROUND guardian");
                break;
                
            case GuardianType.Patrol:
            default:
                // Behavior normal seperti sebelumnya
                Debug.Log($"🚶 {gameObject.name} setup as PATROL guardian");
                break;
        }
        
        // Special case untuk Barnacle
        if (isBarnacle || enemyType == EnemyType.Barnacle)
        {
            guardianType = GuardianType.Stationary;
            canShootOnly = true;
            canChase = false;
            canPatrol = false;
            canShoot = true;
            Debug.Log($"🦪 {gameObject.name} force setup as Barnacle (Stationary)");
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
                walkSprites = LoadSpritesForAnimation(baseName, "move");
            }
            
            if (attackSprites.Length == 0)
            {
                attackSprites = LoadSpritesForAnimation(baseName, "attack");
            }
            
            if (alertSprites.Length == 0)
            {
                alertSprites = LoadSpritesForAnimation(baseName, "idle");
            }
            
            if (deathSprites.Length == 0)
            {
                deathSprites = LoadSpritesForAnimation(baseName, "flat");
            }
        }
        
        // Set initial sprite
        if (spriteRenderer != null && idleSprites.Length > 0)
        {
            spriteRenderer.sprite = idleSprites[0];
        }
        
        Debug.Log($"✅ Sprite setup complete for {baseName}");
    }
    
    string GetEnemyBaseName()
    {
        return enemyType.ToString().ToLower().Replace("_", "-");
    }
    
    Sprite[] LoadSpritesForAnimation(string baseName, string animationType)
    {
        System.Collections.Generic.List<Sprite> sprites = new System.Collections.Generic.List<Sprite>();
        
        Debug.Log($"🔍 Loading {animationType} sprites for {baseName}...");
        
        // Multiple path attempts for better compatibility
        string[] pathAttempts = {
            $"Enemies/{baseName}",
            $"enemies/{baseName}",
            $"{baseName}",
            $"Sprites/Enemies/{baseName}",
            $"Sprites/enemies/{baseName}"
        };
        
        switch (animationType.ToLower())
        {
            case "rest":
                LoadSpriteVariants(sprites, pathAttempts, new string[] { "_rest_a", "_rest_b", "_idle", "_rest" });
                break;
                
            case "move":
                LoadSpriteVariants(sprites, pathAttempts, new string[] { "_move_a", "_move_b", "_walk_a", "_walk_b" });
                break;
                
            case "attack":
                LoadSpriteVariants(sprites, pathAttempts, new string[] { "_attack_a", "_attack_b", "_attack_rest", "_jump", "_fly", "_attack" });
                break;
                
            case "idle":
                LoadSpriteVariants(sprites, pathAttempts, new string[] { "_idle", "_rest_a", "_rest" });
                break;
                
            case "flat":
                LoadSpriteVariants(sprites, pathAttempts, new string[] { "_flat", "_shell", "_death", "_dead" });
                break;
        }
        
        // If still no sprites found, try to load any sprite with the base name
        if (sprites.Count == 0)
        {
            Debug.LogWarning($"⚠️ No specific sprites found for {baseName} {animationType}, trying fallback...");
            
            // Try to load any sprite that contains the base name
            foreach (string path in pathAttempts)
            {
                Sprite fallbackSprite = Resources.Load<Sprite>(path);
                if (fallbackSprite != null)
                {
                    sprites.Add(fallbackSprite);
                    Debug.Log($"   ✅ Found fallback sprite: {fallbackSprite.name}");
                    break;
                }
            }
        }
        
        if (sprites.Count == 0)
        {
            Debug.LogWarning($"⚠️ No sprites found for {baseName} {animationType}");
            
            // Create a temporary colored sprite as absolute fallback
            Sprite tempSprite = CreateTemporarySprite();
            if (tempSprite != null)
            {
                sprites.Add(tempSprite);
                Debug.Log($"   🔧 Created temporary sprite for {baseName}");
            }
        }
        
        return sprites.ToArray();
    }

    void LoadSpriteVariants(System.Collections.Generic.List<Sprite> sprites, string[] basePaths, string[] suffixes)
    {
        foreach (string basePath in basePaths)
        {
            foreach (string suffix in suffixes)
            {
                Sprite sprite = Resources.Load<Sprite>(basePath + suffix);
                if (sprite != null)
                {
                    sprites.Add(sprite);
                    Debug.Log($"   ✅ Found sprite: {sprite.name}");
                }
            }
            
            // If we found sprites from this path, don't try other paths
            if (sprites.Count > 0) break;
        }
    }

    Sprite CreateTemporarySprite()
    {
        try
        {
            // Create a simple 32x32 texture
            Texture2D tempTexture = new Texture2D(32, 32);
            Color spriteColor = GetEnemyColor();
            
            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    tempTexture.SetPixel(x, y, spriteColor);
                }
            }
            
            tempTexture.Apply();
            
            Sprite tempSprite = Sprite.Create(tempTexture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
            tempSprite.name = $"temp_{enemyType}";
            
            return tempSprite;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to create temporary sprite: {e.Message}");
            return null;
        }
    }

    Color GetEnemyColor()
    {
        switch (enemyType)
        {
            case EnemyType.Bee: return Color.yellow;
            case EnemyType.Slime_Normal: return Color.green;
            case EnemyType.Slime_Fire: return Color.red;
            case EnemyType.Fish_Blue: return Color.blue;
            case EnemyType.Frog: return Color.green;
            default: return Color.white;
        }
    }
    
    void InitializeAI()
    {
        startPosition = transform.position;
        
        if (guardianType == GuardianType.Stationary)
        {
            currentState = EnemyState.Idle;
        }
        else if (canPatrol && patrolPoints.Length > 0)
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
        
        Debug.Log($"🤖 AI initialized for {gameObject.name} - State: {currentState}, Guardian Type: {guardianType}");
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
    
    void DetectPlayer()
    {
        if (player == null) return;
        
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        // Check if player is in detection range
        if (distanceToPlayer <= detectionRange)
        {
            Vector2 directionToPlayer = (player.position - transform.position).normalized;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer, detectionRange, obstacleLayer);
            
            if (hit.collider == null || hit.collider.CompareTag("Player"))
            {
                // For area guardians, check if player is in guard area
                if (stayInGuardArea && guardianType != GuardianType.Patrol)
                {
                    float distanceFromCenter = Vector2.Distance(player.position, guardCenter);
                    if (distanceFromCenter > guardAreaRadius)
                    {
                        // Player is outside guard area
                        if (playerDetected)
                        {
                            Debug.Log($"🛡️ Player left {gameObject.name}'s guard area");
                            OnPlayerLost();
                        }
                        return;
                    }
                }
                
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
        playerInShootRange = distanceToPlayer <= shootRange;
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
        if (guardianType == GuardianType.Stationary)
        {
            // Stationary guardian - don't move, just detect and attack
            if (useGravity)
            {
                Vector2 currentVel = SafeGetVelocity();
                SafeSetVelocity(new Vector2(0, currentVel.y));
            }
            else
            {
                SafeSetVelocity(Vector2.zero);
            }
            
            PlayAnimation(AnimationState.Idle);
            
            if (playerDetected)
            {
                if (playerInRange && canAttack)
                {
                    ChangeState(EnemyState.Attack);
                }
                else if (playerInShootRange && canShoot && HasClearShot())
                {
                    ChangeState(EnemyState.Shoot);
                }
            }
            return;
        }
        
        // Original idle behavior for non-stationary
        if (useGravity)
        {
            Vector2 currentVel = SafeGetVelocity();
            SafeSetVelocity(new Vector2(0, currentVel.y));
        }
        else
        {
            SafeSetVelocity(Vector2.zero);
        }
        
        PlayAnimation(AnimationState.Idle);
        
        if (playerDetected)
        {
            if (isBarnacle && canShoot)
            {
                ChangeState(EnemyState.Shoot);
            }
            else if (canChase && guardianType != GuardianType.Stationary)
            {
                ChangeState(EnemyState.Chase);
            }
        }
        else if (canPatrol && patrolPoints.Length > 0 && guardianType != GuardianType.Stationary)
        {
            ChangeState(EnemyState.Patrol);
        }
    }
    
    void HandlePatrolState()
    {
        PlayAnimation(AnimationState.Walk);
        
        if (playerDetected && canChase && guardianType != GuardianType.Stationary)
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
                if (guardianType == GuardianType.Stationary)
                {
                    if (playerInRange && canAttack)
                    {
                        ChangeState(EnemyState.Attack);
                    }
                    else if (playerInShootRange && canShoot)
                    {
                        ChangeState(EnemyState.Shoot);
                    }
                    else
                    {
                        ChangeState(EnemyState.Idle);
                    }
                }
                else if (canChase)
                {
                    ChangeState(EnemyState.Chase);
                }
                else
                {
                    ChangeState(EnemyState.Idle);
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
        
        // Check if player is still in guard area for area guardians
        if (stayInGuardArea && guardianType != GuardianType.Patrol)
        {
            float distanceFromCenter = Vector2.Distance(player.position, guardCenter);
            if (distanceFromCenter > guardAreaRadius)
            {
                // Player left guard area, return to guard position
                Debug.Log($"🛡️ {gameObject.name}: Player left guard area, returning to position");
                ChangeState(EnemyState.Return);
                return;
            }
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
        
        // Only chase if not stationary
        if (guardianType != GuardianType.Stationary)
        {
            MoveTowardsTarget(player.position, chaseSpeed);
        }
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
            if (playerDetected && guardianType != GuardianType.Stationary)
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
            if (isBarnacle || guardianType == GuardianType.Stationary)
            {
                ChangeState(EnemyState.Idle);
            }
            else
            {
                ChangeState(EnemyState.Chase);
            }
            return;
        }
        
        if (playerInRange && canAttack && !isBarnacle && guardianType != GuardianType.Stationary)
        {
            ChangeState(EnemyState.Attack);
            return;
        }
        
        if (!playerInShootRange)
        {
            if (!isBarnacle && guardianType != GuardianType.Stationary)
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
        
        if (playerDetected && canChase && guardianType != GuardianType.Stationary)
        {
            // Check if player is in guard area for area guardians
            if (stayInGuardArea && guardianType != GuardianType.Patrol)
            {
                float distanceFromCenter = Vector2.Distance(player.position, guardCenter);
                if (distanceFromCenter <= guardAreaRadius)
                {
                    ChangeState(EnemyState.Chase);
                    return;
                }
            }
            else if (guardianType == GuardianType.Patrol)
            {
                ChangeState(EnemyState.Chase);
                return;
            }
        }
        
        Vector2 targetPosition;
        if (guardianType == GuardianType.Stationary || stayInGuardArea)
        {
            targetPosition = guardCenter; // Return to guard center
        }
        else
        {
            targetPosition = patrolPoints.Length > 0 ? patrolPoints[currentPatrolIndex].position : startPosition;
        }
        
        MoveTowardsTarget(targetPosition, moveSpeed);
        
        if (Vector2.Distance(transform.position, targetPosition) < 0.5f)
        {
            if (guardianType == GuardianType.Stationary)
            {
                ChangeState(EnemyState.Idle);
            }
            else if (canPatrol && patrolPoints.Length > 0)
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
            Vector2 currentVel = SafeGetVelocity();
            SafeSetVelocity(new Vector2(direction.x * speed, currentVel.y));
        }
        else
        {
            SafeSetVelocity(direction * speed);
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
        Debug.Log($"🎯 {gameObject.name} shot at player!");
    }
    
    void PerformAttack()
    {
        if (player == null) return;
        
        lastAttackTime = Time.time;
        
        // Try both HealthSystem and PlayerHealth for compatibility
        HealthSystem playerHealth = player.GetComponent<HealthSystem>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
        }
        else
        {
            PlayerHealth playerHealthAlt = player.GetComponent<PlayerHealth>();
            if (playerHealthAlt != null)
            {
                playerHealthAlt.TakeDamage(attackDamage);
            }
        }
        
        PlaySound(attackSound);
        StartCoroutine(ShowAttackEffect());
        Debug.Log($"⚔️ {gameObject.name} attacked player for {attackDamage} damage!");
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
        
        Debug.Log($"👁️ {gameObject.name} detected player!");
    }
    
    void OnPlayerLost()
    {
        playerDetected = false;
        
        if (alertEffect != null)
        {
            alertEffect.SetActive(false);
        }
        
        if (returnToPatrolAfterLose && guardianType != GuardianType.Stationary)
        {
            ChangeState(EnemyState.Return);
        }
        else
        {
            ChangeState(EnemyState.Idle);
        }
        
        Debug.Log($"👁️‍🗨️ {gameObject.name} lost player!");
    }
    
    void ChangeState(EnemyState newState)
    {
        if (currentState == newState) return;
        
        Debug.Log($"🔄 {gameObject.name} state: {currentState} → {newState}");
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
    
    void UpdateAnimation()
    {
        if (currentAnimationSprites == null || currentAnimationSprites.Length == 0) return;
        
        animationTimer += Time.deltaTime;
        
        if (animationTimer >= animationSpeed)
        {
            animationTimer = 0f;
            currentSpriteIndex++;
            
            if (currentSpriteIndex >= currentAnimationSprites.Length)
            {
                if (loopAnimations)
                {
                    currentSpriteIndex = 0;
                }
                else
                {
                    currentSpriteIndex = currentAnimationSprites.Length - 1;
                }
            }
            
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = currentAnimationSprites[currentSpriteIndex];
            }
        }
    }
    
    void PlayAnimation(AnimationState animationState)
    {
        if (currentAnimationState == animationState) return;
        
        currentAnimationState = animationState;
        currentSpriteIndex = 0;
        animationTimer = 0f;
        
        switch (animationState)
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
                break;
        }
        
        if (currentAnimationSprites != null && currentAnimationSprites.Length > 0 && spriteRenderer != null)
        {
            spriteRenderer.sprite = currentAnimationSprites[0];
        }
    }
    
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    // Public methods untuk external use
    public void TakeDamage(int damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        PlaySound(hurtSound);
        
        if (damageEffect != null)
        {
            GameObject effect = Instantiate(damageEffect, transform.position, Quaternion.identity);
            Destroy(effect, 1f);
        }
        
        if (currentHealth <= 0)
        {
            Die();
        }
        
        Debug.Log($"💥 {gameObject.name} took {damage} damage! Health: {currentHealth}/{maxHealth}");
    }
    
    public void Die()
    {
        if (isDead) return;
        
        isDead = true;
        ChangeState(EnemyState.Death);
        
        PlaySound(deathSound);
        
        if (deathEffect != null)
        {
            GameObject effect = Instantiate(deathEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // Disable collider
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }
        
        // Destroy after death animation
        Destroy(gameObject, 3f);
        
        Debug.Log($"💀 {gameObject.name} died!");
    }
    
    public void Stun(float duration)
    {
        if (isDead) return;
        
        ChangeState(EnemyState.Stunned);
        StartCoroutine(StunCoroutine(duration));
        
        Debug.Log($"😵 {gameObject.name} stunned for {duration} seconds!");
    }
    
    IEnumerator StunCoroutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        
        if (!isDead)
        {
            ChangeState(EnemyState.Idle);
        }
    }
    
    // Gizmos untuk visualisasi di editor
    void OnDrawGizmosSelected()
    {
        // Detection range
        Gizmos.color = Color.yellow;
        DrawWireCircle(transform.position, detectionRange);
        
        // Attack range
        Gizmos.color = Color.red;
        DrawWireCircle(transform.position, attackRange);
        
        // Shoot range
        Gizmos.color = Color.blue;
        DrawWireCircle(transform.position, shootRange);
        
        // Guard area
        if (showGuardArea && guardianType != GuardianType.Patrol)
        {
            Gizmos.color = guardAreaColor;
            Vector3 guardPos = Application.isPlaying ? (Vector3)guardCenter : transform.position;
            DrawWireCircle(guardPos, guardAreaRadius);
            
            // Draw line to guard center
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, guardPos);
        }
        
        // Patrol points
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawWireCube(patrolPoints[i].position, Vector3.one * 0.5f);
                    
                    // Draw path between patrol points
                    if (i < patrolPoints.Length - 1 && patrolPoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                    }
                    else if (i == patrolPoints.Length - 1 && patrolPoints[0] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[0].position);
                    }
                }
            }
        }
    }

    private void DrawWireCircle(Vector3 center, float radius)
    {
        int segments = 32;
        float angle = 0f;
        Vector3 lastPos = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            angle = (float)i / segments * Mathf.PI * 2f;
            Vector3 newPos = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            Gizmos.DrawLine(lastPos, newPos);
            lastPos = newPos;
        }
    }
    
    // Get/Set methods untuk external access
    public EnemyState GetCurrentState() { return currentState; }
    public GuardianType GetGuardianType() { return guardianType; }
    public bool IsPlayerDetected() { return playerDetected; }
    public bool IsDead() { return isDead; }
    public int GetCurrentHealth() { return currentHealth; }
    public int GetMaxHealth() { return maxHealth; }
    public Vector2 GetGuardCenter() { return guardCenter; }
    public float GetGuardRadius() { return guardAreaRadius; }
    
    public void SetGuardCenter(Vector2 newCenter)
    {
        guardCenter = newCenter;
        Debug.Log($"🛡️ {gameObject.name} guard center updated to {guardCenter}");
    }
    
    public void SetGuardRadius(float newRadius)
    {
        guardAreaRadius = newRadius;
        Debug.Log($"🛡️ {gameObject.name} guard radius updated to {guardAreaRadius}");
    }
}