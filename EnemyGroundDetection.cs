using UnityEngine;

public class EnemyGroundDetection : MonoBehaviour
{
    [Header("Ground Detection")]
    public LayerMask groundLayer = 8; // Set ke layer Ground
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public float gravityScale = 3f;
    
    private Rigidbody2D rb;
    private bool isGrounded;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Set gravity scale
        if (rb != null)
        {
            rb.gravityScale = gravityScale;
        }
        
        // Create ground check point if not assigned
        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.SetParent(transform);
            groundCheckObj.transform.localPosition = new Vector3(0, -0.5f, 0);
            groundCheck = groundCheckObj.transform;
        }
    }
    
    void Update()
    {
        CheckGrounded();
    }
    
    void CheckGrounded()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }
    
    public bool IsGrounded()
    {
        return isGrounded;
    }
    
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}