using UnityEngine;

public class CameraController : MonoBehaviour
{
    private Transform target;
    
    [Header("Camera Settings")]
    public Vector3 offset = new(0, 15, -10);
    public float smoothSpeed = 5f;
    public float rotationSpeed = 100f;
    
    [Header("Initial Rotation")]
    public float initialHorizontalAngle = 0f;  // Starting Y rotation
    public float initialVerticalAngle = 45f;   // Starting X rotation (pitch)
    
    [Header("Rotation Limits (Relative to Initial)")]
    public float minVerticalAngle = -35f;   // How far down you can rotate from initial
    public float maxVerticalAngle = 44f;    // How far up you can rotate from initial
    public float minHorizontalAngle = -180f;  // How far left you can rotate from initial
    public float maxHorizontalAngle = 180f;   // How far right you can rotate from initial
    
    private float currentRotationY = 0f;
    private float currentRotationX = 45f;  // Vertical angle (pitch)

    private void RefocusOnTarget() 
    {
        if (target != null)
        {
            // Instantly move camera to target position with offset
            Quaternion rotation = Quaternion.Euler(currentRotationX, currentRotationY, 0);
            Vector3 rotatedOffset = rotation * offset;
            transform.position = target.position + rotatedOffset;
            transform.LookAt(target);
            Debug.Log("Camera refocused on target at position: " + target.position);
        }
    }
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        RefocusOnTarget();
        Debug.Log($"Camera target changed to: {newTarget.name}");
    }
    
    void Start()
    {
        // Initialize rotation from inspector values
        currentRotationY = initialHorizontalAngle;
        currentRotationX = initialVerticalAngle;
    }
    
    void LateUpdate()
    {
        // Don't update if we don't have a target yet
        if (target == null)
            return;
            
        // Calculate desired position using rotated offset
        Quaternion rotation = Quaternion.Euler(currentRotationX, currentRotationY, 0);
        Vector3 rotatedOffset = rotation * offset;
        Vector3 desiredPosition = target.position + rotatedOffset;
        
        // Smoothly move camera to desired position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;

    }
    
    public void RotateCamera(Vector2 lookInput)
    {
        // Rotate camera around the player based on look input
        // Horizontal rotation (yaw)
        currentRotationY += lookInput.x * rotationSpeed * Time.deltaTime;
        currentRotationY = Mathf.Clamp(currentRotationY, 
            initialHorizontalAngle + minHorizontalAngle, 
            initialHorizontalAngle + maxHorizontalAngle);
        
        // Vertical rotation (pitch) - inverted so up on stick tilts camera up
        currentRotationX -= lookInput.y * rotationSpeed * Time.deltaTime;
        currentRotationX = Mathf.Clamp(currentRotationX, 
            initialVerticalAngle + minVerticalAngle, 
            initialVerticalAngle + maxVerticalAngle);
    }
}
