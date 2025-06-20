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
    public LayerMask playerLayer = 1;
    public LayerMask obstacleLayer = 1;
    
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
    
    // Private variables
    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private Collider2D enemyCollider;
    
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
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        enemyCollider = GetComponent<Collider2D>();
        
        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        
        // Setup fire point if not assigned
        if (firePoint == null && canShoot)
        {
            GameObject firePointObj = new GameObject("EnemyFirePoint");
            firePointObj.transform.SetParent(transform);
            firePointObj.transform.localPosition = new Vector3(0.5f, 0, 0);
            firePoint = firePointObj.transform;
        }
    }
    
    void SetupEnemySprites()
    {
        string baseName = GetEnemyBaseName();
        
        // Special handling for Barnacle
        if (enemyType == EnemyType.Barnacle)
        {
            isBarnacle = true;
            canShootOnly = true;
            canChase = false;
            canPatrol = false;
            canShoot = true;
            
            // Setup animasi khusus barnacle
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
            // Setup normal untuk enemy lain
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
        
        PlayAnimation(AnimationState.Idle);
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
        
        switch (animationType)
        {
            case "rest":
                // Cari di folder Enemies
                Sprite restSprite = Resources.Load<Sprite>("Enemies/" + baseName + "_rest");
                if (restSprite == null)
                {
                    // Fallback: cari di root Resources
                    restSprite = Resources.Load<Sprite>(baseName + "_rest");
                }
                if (restSprite != null) sprites.Add(restSprite);
                break;
                
            case "walk":
                // FIXED: Renamed variables to avoid scope conflict
                Sprite walkSpriteA = Resources.Load<Sprite>("Enemies/" + baseName + "_walk_a");
                Sprite walkSpriteB = Resources.Load<Sprite>("Enemies/" + baseName + "_walk_b");
                
                // Fallback ke root jika tidak ada di Enemies folder
                if (walkSpriteA == null) walkSpriteA = Resources.Load<Sprite>(baseName + "_walk_a");
                if (walkSpriteB == null) walkSpriteB = Resources.Load<Sprite>(baseName + "_walk_b");
                
                if (walkSpriteA != null) sprites.Add(walkSpriteA);
                if (walkSpriteB != null) sprites.Add(walkSpriteB);
                
                // Fallback ke move jika walk tidak ada
                if (sprites.Count == 0)
                {
                    Sprite moveSpriteA = Resources.Load<Sprite>("Enemies/" + baseName + "_move_a");
                    Sprite moveSpriteB = Resources.Load<Sprite>("Enemies/" + baseName + "_move_b");
                    
                    if (moveSpriteA == null) moveSpriteA = Resources.Load<Sprite>(baseName + "_move_a");
                    if (moveSpriteB == null) moveSpriteB = Resources.Load<Sprite>(baseName + "_move_b");
                    
                    if (moveSpriteA != null) sprites.Add(moveSpriteA);
                    if (moveSpriteB != null) sprites.Add(moveSpriteB);
                }
                break;
                
            case "move":
                // Khusus untuk barnacle dan enemy yang menggunakan move animation
                Sprite moveA = Resources.Load<Sprite>("Enemies/" + baseName + "_a");
                Sprite moveB = Resources.Load<Sprite>("Enemies/" + baseName + "_b");
                
                if (moveA == null) moveA = Resources.Load<Sprite>(baseName + "_a");
                if (moveB == null) moveB = Resources.Load<Sprite>(baseName + "_b");
                
                if (moveA != null) sprites.Add(moveA);
                if (moveB != null) sprites.Add(moveB);
                
                // Alternatif naming
                if (sprites.Count == 0)
                {
                    moveA = Resources.Load<Sprite>("Enemies/" + baseName + "_move_a");
                    moveB = Resources.Load<Sprite>("Enemies/" + baseName + "_move_b");
                    
                    if (moveA == null) moveA = Resources.Load<Sprite>(baseName + "_move_a");
                    if (moveB == null) moveB = Resources.Load<Sprite>(baseName + "_move_b");
                    
                    if (moveA != null) sprites.Add(moveA);
                    if (moveB != null) sprites.Add(moveB);
                }
                break;
                
            case "attack":
                Sprite attackA = Resources.Load<Sprite>("Enemies/" + baseName + "_attack_a");
                Sprite attackB = Resources.Load<Sprite>("Enemies/" + baseName + "_attack_b");
                Sprite attackRest = Resources.Load<Sprite>("Enemies/" + baseName + "_attack_rest");
                Sprite jump = Resources.Load<Sprite>("Enemies/" + baseName + "_jump");
                Sprite fly = Resources.Load<Sprite>("Enemies/" + baseName + "_fly");
                
                // Fallback
                if (attackA == null) attackA = Resources.Load<Sprite>(baseName + "_attack_a");
                if (attackB == null) attackB = Resources.Load<Sprite>(baseName + "_attack_b");
                if (attackRest == null) attackRest = Resources.Load<Sprite>(baseName + "_attack_rest");
                if (jump == null) jump = Resources.Load<Sprite>(baseName + "_jump");
                if (fly == null) fly = Resources.Load<Sprite>(baseName + "_fly");
                
                if (attackA != null) sprites.Add(attackA);
                if (attackB != null) sprites.Add(attackB);
                if (attackRest != null) sprites.Add(attackRest);
                if (jump != null) sprites.Add(jump);
                if (fly != null) sprites.Add(fly);
                break;
                
            case "idle":
                Sprite idle = Resources.Load<Sprite>("Enemies/" + baseName + "_idle");
                if (idle == null) idle = Resources.Load<Sprite>(baseName + "_idle");
                if (idle != null) sprites.Add(idle);
                break;
                
            case "flat":
                Sprite flat = Resources.Load<Sprite>("Enemies/" + baseName + "_flat");
                Sprite shell = Resources.Load<Sprite>("Enemies/" + baseName + "_shell");
                
                if (flat == null) flat = Resources.Load<Sprite>(baseName + "_flat");
                if (shell == null) shell = Resources.Load<Sprite>(baseName + "_shell");
                
                if (flat != null) sprites.Add(flat);
                if (shell != null) sprites.Add(shell);
                break;
        }
        
        // Debug log untuk troubleshooting
        Debug.Log($"Loaded {sprites.Count} sprites for {baseName} - {animationType}");
        
        return sprites.ToArray();
    }
    
    void InitializeAI()
    {
        startPosition = transform.position;
        
        // Set initial state
        if (canPatrol && patrolPoints.Length > 0)
        {
            currentState = EnemyState.Patrol;
        }
        else
        {
            currentState = EnemyState.Idle;
        }
        
        // Setup effects
        if (alertEffect != null)
        {
            alertEffect.SetActive(false);
        }
        
        if (attackEffect != null)
        {
            attackEffect.SetActive(false);
        }
        
        // Set initial color
        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
        }
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
                spriteRenderer.sprite = currentAnimationSprites[currentSpriteIndex];
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
        
        // Set first frame immediately
        if (currentAnimationSprites != null && currentAnimationSprites.Length > 0 && spriteRenderer != null)
        {
            spriteRenderer.sprite = currentAnimationSprites[0];
        }
    }
    
    void DetectPlayer()
    {
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
        rb.velocity = Vector2.zero;
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
            rb.velocity = Vector2.zero;
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
        rb.velocity = Vector2.zero;
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
        rb.velocity = Vector2.zero;
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
        rb.velocity = Vector2.zero;
        
        // Untuk barnacle, gunakan walk animation saat menembak
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
        rb.velocity = Vector2.zero;
        PlayAnimation(AnimationState.Idle);
    }
    
    void HandleDeathState()
    {
        rb.velocity = Vector2.zero;
        PlayAnimation(AnimationState.Death);
    }
    
    void MoveTowardsTarget(Vector2 target, float speed)
    {
        Vector2 direction = (target - (Vector2)transform.position).normalized;
        rb.velocity = direction * speed;
        
        if (spriteRenderer != null)
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
        
        // Damage player
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
            // Flash red when damaged
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
}