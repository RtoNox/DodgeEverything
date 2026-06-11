using UnityEngine;

public class CameraBehavior : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Vector3 offset = new Vector3(0, 10, -8);
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float rotationSpeed = 100f;
    
    [Header("Angle Settings")]
    [SerializeField] private float fixedPitchAngle = 45f; // Fixed looking down angle
    
    private float currentYaw = 0f;
    
    void Start()
    {
        if (playerTarget == null)
        {
            // Try to find player automatically
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTarget = player.transform;
            else
                Debug.LogError("No player target assigned to CameraBehavior!");
        }
        
        // Initialize camera rotation with fixed pitch angle
        currentYaw = transform.eulerAngles.y;
        transform.rotation = Quaternion.Euler(fixedPitchAngle, currentYaw, 0);
    }
    
    void LateUpdate()
    {
        if (playerTarget == null) return;
        
        HandleHorizontalMouseRotation();
        FollowPlayer();
    }
    
    void HandleHorizontalMouseRotation()
    {
        // Mouse X rotates camera around Y axis ONLY (horizontal rotation)
        currentYaw += Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
        
        // Apply rotation with fixed pitch angle (no vertical movement)
        transform.rotation = Quaternion.Euler(fixedPitchAngle, currentYaw, 0);
    }
    
    void FollowPlayer()
    {
        // Calculate desired position based on camera's rotation
        Vector3 desiredPosition = playerTarget.position + 
            (transform.rotation * offset);
        
        // Smooth follow
        transform.position = Vector3.Lerp(transform.position, desiredPosition, 
            followSpeed * Time.deltaTime);
    }
    
    // Optional: Public method to set new target
    public void SetTarget(Transform newTarget)
    {
        playerTarget = newTarget;
    }
    
    // Optional: Reset camera angle to look at player's forward direction
    public void ResetCameraAngle()
    {
        currentYaw = playerTarget.eulerAngles.y;
        transform.rotation = Quaternion.Euler(fixedPitchAngle, currentYaw, 0);
    }
    
    // Optional: Set fixed pitch angle dynamically
    public void SetPitchAngle(float newAngle)
    {
        fixedPitchAngle = Mathf.Clamp(newAngle, 20f, 80f);
        transform.rotation = Quaternion.Euler(fixedPitchAngle, currentYaw, 0);
    }
}