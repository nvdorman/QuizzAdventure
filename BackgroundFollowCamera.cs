using UnityEngine;

public class BackgroundFollowCamera : MonoBehaviour
{
    public Transform mainCamera; // Drag Main Camera di sini
    private Vector3 lastCameraPosition;
    
    void Start()
    {
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera belum diassign!");
            enabled = false;
        }

        lastCameraPosition = mainCamera.position;
    }

    void LateUpdate()
    {
        // Hanya ikuti pergerakan horizontal (sumbu X)
        float deltaX = mainCamera.position.x - lastCameraPosition.x;

        transform.position += new Vector3(deltaX, 0, 0);

        lastCameraPosition = mainCamera.position;
    }
}