using UnityEngine;

public class CameraController : MonoBehaviour
{
    private Transform target;
    
    [Header("Camera Settings")]
    public Vector3 offset = new Vector3(0, 15, -10);
    public float smoothSpeed = 5f;
    
    void LateUpdate()
    {
        // Keep searching for player until found (handles spawn timing)
        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                Debug.Log("Camera found player instance at: " + target.position);
            }
            return;
        }
        
        // Calculate desired position
        Vector3 desiredPosition = target.position + offset;
        
        // Smoothly move camera to desired position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
        
        // Look at the target
        transform.LookAt(target);
    }
}
