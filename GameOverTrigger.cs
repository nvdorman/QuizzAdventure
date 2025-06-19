using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;

public class GameOverTrigger : MonoBehaviour
{
    [Header("Game Over Settings")]
    public Canvas gameOverCanvas;
    public string playerTag = "Player";
    public bool pauseGameOnGameOver = true;
    
    [Header("Delay Settings")]
    public float gameOverDelay = 1.0f;
    public bool disablePlayerImmediately = true;
    public bool showCountdown = true;
    
    [Header("Visual Effects")]
    public GameObject warningEffect; // Optional warning effect
    public Color playerFlashColor = Color.red;
    public float flashSpeed = 5f;
    
    private bool gameOverTriggered = false;
    private SpriteRenderer playerRenderer;
    private Color originalPlayerColor;
    
    private void Start()
    {
        if (gameOverCanvas != null)
        {
            gameOverCanvas.gameObject.SetActive(false);
        }
        
        TilemapCollider2D tilemapCollider = GetComponent<TilemapCollider2D>();
        if (tilemapCollider != null)
        {
            tilemapCollider.isTrigger = true;
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag) && !gameOverTriggered)
        {
            gameOverTriggered = true;
            
            // Get player renderer untuk visual effect
            playerRenderer = other.GetComponent<SpriteRenderer>();
            if (playerRenderer != null)
            {
                originalPlayerColor = playerRenderer.color;
            }
            
            StartCoroutine(TriggerGameOverWithDelay(other.gameObject));
        }
    }
    
    private IEnumerator TriggerGameOverWithDelay(GameObject player)
    {
        Debug.Log("Player hit! Game Over in " + gameOverDelay + " seconds...");
        
        // Aktifkan warning effect jika ada
        if (warningEffect != null)
        {
            warningEffect.SetActive(true);
        }
        
        // Disable player immediately jika diinginkan
        if (disablePlayerImmediately)
        {
            DisablePlayer(player);
        }
        
        // Start visual effects (player flashing)
        StartCoroutine(FlashPlayer());
        
        // Countdown dengan debug log
        if (showCountdown)
        {
            for (int i = Mathf.FloorToInt(gameOverDelay); i > 0; i--)
            {
                Debug.Log("Game Over in: " + i);
                yield return new WaitForSeconds(1f);
            }
        }
        else
        {
            yield return new WaitForSeconds(gameOverDelay);
        }
        
        // Stop flashing
        StopAllCoroutines();
        
        // Reset player color
        if (playerRenderer != null)
        {
            playerRenderer.color = originalPlayerColor;
        }
        
        // Trigger game over
        TriggerGameOver(player);
    }
    
    private IEnumerator FlashPlayer()
    {
        while (true)
        {
            if (playerRenderer != null)
            {
                playerRenderer.color = playerFlashColor;
                yield return new WaitForSeconds(1f / flashSpeed);
                playerRenderer.color = originalPlayerColor;
                yield return new WaitForSeconds(1f / flashSpeed);
            }
            else
            {
                yield break;
            }
        }
    }
    
    private void TriggerGameOver(GameObject player)
    {
        // Matikan warning effect
        if (warningEffect != null)
        {
            warningEffect.SetActive(false);
        }
        
        // Aktifkan canvas game over
        if (gameOverCanvas != null)
        {
            gameOverCanvas.gameObject.SetActive(true);
        }
        
        // Pause game jika diinginkan
        if (pauseGameOnGameOver)
        {
            Time.timeScale = 0f;
        }
        
        // Disable player movement jika belum di-disable
        if (!disablePlayerImmediately)
        {
            DisablePlayer(player);
        }
        
        Debug.Log("Game Over! Player touched dangerous tilemap!");
    }
    
    private void DisablePlayer(GameObject player)
    {
        MonoBehaviour[] playerScripts = player.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in playerScripts)
        {
            if (script != this && script.GetType().Name != "Transform")
            {
                script.enabled = false;
            }
        }
        
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerRb.velocity = Vector2.zero;
            playerRb.isKinematic = true;
        }
    }
    
    public void ResetGameOverState()
    {
        gameOverTriggered = false;
        StopAllCoroutines();
        
        if (playerRenderer != null)
        {
            playerRenderer.color = originalPlayerColor;
        }
        
        if (warningEffect != null)
        {
            warningEffect.SetActive(false);
        }
    }
}