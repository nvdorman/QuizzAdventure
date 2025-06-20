using UnityEngine;
using System.Collections;

public class PlayerController2D : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 5f;

    [Header("Shooting Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 15f;
    public int bulletDamage = 1;
    public float fireRate = 0.3f;
    public int maxAmmo = 30;
    public float reloadTime = 2f;
    
    [Header("Shooting Effects")]
    public GameObject muzzleFlash;
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip emptyClipSound;
    
    [Header("Tags & Layers")]
    public string groundTag = "Ground";

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private Camera playerCamera;

    private bool isGrounded;
    private float movementInput;
    private float originalMoveSpeed;
    
    // Shooting variables
    private float nextFireTime = 0f;
    private int currentAmmo;
    private bool isReloading = false;
    private Vector2 mousePosition;
    private Vector2 shootDirection;

    // Animation States
    private readonly int idleHash = Animator.StringToHash("Idle");
    private readonly int walkAHash = Animator.StringToHash("WalkA");
    private readonly int walkBHash = Animator.StringToHash("WalkB");
    private readonly int jumpHash = Animator.StringToHash("Jump");
    private readonly int shootHash = Animator.StringToHash("Shoot");

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
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
        
        // Ensure default sprite is visible (index 0)
        if (spriteRenderer != null && spriteRenderer.sprite == null)
        {
            // Try to get the first sprite from animator if available
            if (animator != null)
            {
                animator.Play(idleHash);
            }
        }
    }

    void Update()
    {
        HandleInput();
        HandleShooting();
        UpdateAnimation();
        UpdateAiming();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    void HandleInput()
    {
        movementInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }
        
        // Reload input
        if (Input.GetKeyDown(KeyCode.R) && !isReloading)
        {
            StartReload();
        }
    }
    
    void HandleShooting()
    {
        if (isReloading) return;
        
        // Shooting with left mouse button
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            if (currentAmmo > 0)
            {
                Shoot();
                nextFireTime = Time.time + fireRate;
            }
            else
            {
                // Play empty clip sound
                if (audioSource != null && emptyClipSound != null)
                {
                    audioSource.PlayOneShot(emptyClipSound);
                }
                StartReload();
            }
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
                
                firePoint.localPosition = new Vector3(
                    Mathf.Abs(shootDirection.x) * 0.5f, 
                    shootDirection.y * 0.3f, 
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
            // If no Bullet script, just add velocity
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
        
        // Play shoot animation
        if (animator != null)
        {
            animator.Play(shootHash);
        }
        
        Debug.Log($"Shot fired! Ammo: {currentAmmo}/{maxAmmo}");
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
        
        // Play reload sound
        if (audioSource != null && reloadSound != null)
        {
            audioSource.PlayOneShot(reloadSound);
        }
        
        Debug.Log("Reloading...");
        
        yield return new WaitForSeconds(reloadTime);
        
        currentAmmo = maxAmmo;
        isReloading = false;
        
        Debug.Log("Reload complete!");
    }

    void HandleMovement()
    {
        rb.velocity = new Vector2(movementInput * moveSpeed, rb.velocity.y);
        
        // FIXED: Proper sprite flipping based on movement direction
        if (spriteRenderer != null)
        {
            if (movementInput != 0)
            {
                // Flip based on movement direction
                spriteRenderer.flipX = movementInput < 0;
            }
            else if (playerCamera != null)
            {
                // Flip based on mouse position when not moving
                Vector2 mousePos = playerCamera.ScreenToWorldPoint(Input.mousePosition);
                spriteRenderer.flipX = mousePos.x < transform.position.x;
            }
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
        if (!isGrounded)
        {
            animator.Play(jumpHash);
        }
        else if (movementInput != 0)
        {
            float walkSpeedMultiplier = 7.5f;
            float walkAnimationTime = Mathf.PingPong(Time.time * walkSpeedMultiplier, 1);

            if (walkAnimationTime < 0.5f)
            {
                animator.Play(walkAHash);
            }
            else
            {
                animator.Play(walkBHash);
            }
        }
        else
        {
            animator.Play(idleHash);
        }
    }
    
    // Public getters for UI
    public int GetCurrentAmmo() { return currentAmmo; }
    public int GetMaxAmmo() { return maxAmmo; }
    public bool IsReloading() { return isReloading; }
}