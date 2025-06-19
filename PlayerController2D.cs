using UnityEngine;

public class PlayerController2D : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 5f;

    [Header("Tags & Layers")]
    public string groundTag = "Ground";

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private bool isGrounded;
    private float movementInput;
    private float originalMoveSpeed;

    // Animation States
    private readonly int idleHash = Animator.StringToHash("Idle");
    private readonly int walkAHash = Animator.StringToHash("WalkA");
    private readonly int walkBHash = Animator.StringToHash("WalkB");
    private readonly int jumpHash = Animator.StringToHash("Jump");

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        originalMoveSpeed = moveSpeed;
    }

    void Update()
    {
        HandleInput();
        UpdateAnimation();
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
    }

    void HandleMovement()
    {
        rb.velocity = new Vector2(movementInput * moveSpeed, rb.velocity.y);

        if (movementInput != 0)
        {
            spriteRenderer.flipX = movementInput < 0;
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
}