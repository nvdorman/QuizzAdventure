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
    public float shootCooldown = 2f;
    public int bulletDamage = 1;
    public float aimAccuracy = 0.8f;
    public bool canShoot = true;
    
    [Header("Shooting Effects")]
    public GameObject muzzleFlash;
    public AudioClip shootSound;
    
    [Header("Patrol Settings")]
    public Transform[] patrolPoints;
    public float waitTime = 2f;
    public bool randomPatrol = false;
    
    [Header("Combat Settings")]
    public float attackDamage = 1f;
    public float attackCooldown = 1.5f;
    public float knockbackForce = 5f;
    
    [Header("AI Behavior")]
    public bool canPatrol = true;
    public bool canChase = true;
    public bool canAttack = true;
    public bool returnToPatrolAfterLose = true;
    
    [Header("Visual Effects")]
    public GameObject alertEffect;
    public GameObject attackEffect;
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
    
    void Start()
    {
        InitializeComponents();
        SetupEnemySprites();
        InitializeAI();
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
        // Auto-setup sprites based on enemy type
        string baseName = GetEnemyBaseName();
        
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
        
        if (alertSprites.Length == 0)
        {
            alertSprites = LoadSpritesForAnimation(baseName, "idle");
        }
        
        if (deathSprites.Length == 0)
        {
            deathSprites = LoadSpritesForAnimation(baseName, "flat");
        }
        
        // Set initial sprite
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
        
        // Try to load sprites based on naming convention
        switch (animationType)
        {
            case "rest":
                Sprite restSprite = Resources.Load<Sprite>(baseName + "_rest");
                if (restSprite != null) sprites.Add(restSprite);
                break;
                
            case "walk":
                Sprite walkA = Resources.Load<Sprite>(baseName + "_walk_a");
                Sprite walkB = Resources.Load<Sprite>(baseName + "_walk_b");
                if (walkA != null) sprites.Add(walkA);
                if (walkB != null) sprites.Add(walkB);
                
                // For movement-based enemies
                Sprite moveA = Resources.Load<Sprite>(baseName + "_move_a");
                Sprite moveB = Resources.Load<Sprite>(baseName + "_move_b");
                if (moveA != null) sprites.Add(moveA);
                if (moveB != null) sprites.Add(moveB);
                
                // For swimming fish
                Sprite swimA = Resources.Load<Sprite>(baseName + "_swim_a");
                Sprite swimB = Resources.Load<Sprite>(baseName + "_swim_b");
                if (swimA != null) sprites.Add(swimA);
                if (swimB != null) sprites.Add(swimB);
                break;
                
            case "attack":
                Sprite attackA = Resources.Load<Sprite>(baseName + "_attack_a");
                Sprite attackB = Resources.Load<Sprite>(baseName + "_attack_b");
                Sprite attackRest = Resources.Load<Sprite>(baseName + "_attack_rest");
                Sprite jump = Resources.Load<Sprite>(baseName + "_jump");
                Sprite fly = Resources.Load<Sprite>(baseName + "_fly");
                
                if (attackA != null) sprites.Add(attackA);
                if (attackB != null) sprites.Add(attackB);
                if (attackRest != null) sprites.Add(attackRest);
                if (jump != null) sprites.Add(jump);
                if (fly != null) sprites.Add(fly);
                break;
                
            case "idle":
                Sprite idle = Resources.Load<Sprite>(baseName + "_idle");
                if (idle != null) sprites.Add(idle);
                break;
                
            case "flat":
                Sprite flat = Resources.Load<Sprite>(baseName + "_flat");
                Sprite shell = Resources.Load<Sprite>(baseName + "_shell");
                if (flat != null) sprites.Add(flat);
                if (shell != null) sprites.Add(shell);
                break;
        }
        
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
    
    // Rest of the AI logic (same as before but with animation calls)
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
        
        if (playerDetected && canChase)
        {
            ChangeState(EnemyState.Chase);
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
            if (playerDetected && canChase)
            {
                ChangeState(EnemyState.Chase);
            }
            else
            {
                ChangeState(EnemyState.Idle);
            }
        }
    }
    
    void HandleChaseState()
    {
        PlayAnimation(AnimationState.Walk);
        
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
        PlayAnimation(AnimationState.Attack);
        
        if (!playerDetected)
        {
            ChangeState(EnemyState.Chase);
            return;
        }
        
        if (playerInRange && canAttack)
        {
            ChangeState(EnemyState.Attack);
            return;
        }
        
        if (!playerInShootRange)
        {
            ChangeState(EnemyState.Chase);
            return;
        }
        
        if (Time.time - lastShootTime >= shootCooldown && HasClearShot())
        {
            ShootAtPlayer();
        }
    }
    
    void HandleReturnState()
    {
        PlayAnimation(AnimationState.Walk);
        
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
    
    // ... (include all other methods from previous script)
    
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
        
        Vector2 perfectAim = (player.position - firePoint.position).normalized;
        Vector2 inaccuracy = Random.insideUnitCircle * (1f - aimAccuracy);
        Vector2 shootDirection = (perfectAim + inaccuracy).normalized;
        
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        
        if (bulletScript != null)
        {
            bulletScript.Initialize(shootDirection, false, bulletDamage);
        }
        else
        {
            Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
            if (bulletRb != null)
            {
                bulletRb.velocity = shootDirection * 10f;
            }
        }
        
        PlaySound(shootSound);
        
        if (muzzleFlash != null)
        {
            StartCoroutine(ShowMuzzleFlash());
        }
        
        Debug.Log("Enemy shot at player!");
    }
    
    IEnumerator ShowMuzzleFlash()
    {
        if (muzzleFlash != null)
        {
            GameObject flash = Instantiate(muzzleFlash, firePoint.position, firePoint.rotation);
            yield return new WaitForSeconds(0.1f);
            if (flash != null) Destroy(flash);
        }
    }
    
    void UpdateAiming()
    {
        if (player == null || firePoint == null) return;
        
        if (currentState == EnemyState.Shoot || currentState == EnemyState.Chase)
        {
            Vector2 directionToPlayer = (player.position - transform.position).normalized;
            float angle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;
            firePoint.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            
            firePoint.localPosition = new Vector3(
                Mathf.Abs(directionToPlayer.x) * 0.5f, 
                directionToPlayer.y * 0.3f, 
                0
            );
        }
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
    
    void PerformAttack()
    {
        lastAttackTime = Time.time;
        
        PlaySound(attackSound);
        
        if (attackEffect != null)
        {
            StartCoroutine(ShowAttackEffect());
        }
        
        PlayerController2D playerController = player.GetComponent<PlayerController2D>();
        if (playerController != null)
        {
            Vector2 knockbackDirection = (player.position - transform.position).normalized;
            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                playerRb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
            }
            
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(1);
            }
            
            Debug.Log("Player hit by enemy melee attack!");
        }
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
    
    public void Stun(float duration)
    {
        StartCoroutine(StunCoroutine(duration));
    }
    
    IEnumerator StunCoroutine(float duration)
    {
        EnemyState previousState = currentState;
        ChangeState(EnemyState.Stunned);
        
        yield return new WaitForSeconds(duration);
        
        ChangeState(previousState);
    }
    
    public void Die()
    {
        isDead = true;
        ChangeState(EnemyState.Death);
        
        // Disable AI components
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }
        
        // Destroy after death animation
        Destroy(gameObject, 2f);
    }
    
    public void EnableAI()
    {
        enabled = true;
    }
    
    public void DisableAI()
    {
        enabled = false;
        rb.velocity = Vector2.zero;
    }
    
    public EnemyState GetCurrentState()
    {
        return currentState;
    }
    
    // Unity 2022 Compatible Gizmos
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, shootRange);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, losePlayerRange);
        
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawWireCube(patrolPoints[i].position, Vector3.one * 0.5f);
                    
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
}