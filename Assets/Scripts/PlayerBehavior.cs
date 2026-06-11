using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PlayerBehavior : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 10f;
    [SerializeField] private float rotationSmoothTime = 0.1f;
    
    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float maxSlopeAngle = 45f;
    
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 8f;
    
    [Header("Fall Settings")]
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float terminalFallSpeed = -10f;
    
    [Header("Camera Reference")]
    [SerializeField] private Camera playerCamera;
    
    [Header("Combat Settings")]
    [SerializeField] private float slashRange = 2f;
    [SerializeField] private float slashCooldown = 0.5f;
    [SerializeField] private float parryDuration = 0.3f;
    [SerializeField] private int maxHealth = 10;

    [Header("Invincibility Settings")]
    [SerializeField] private float invincibilityDuration = 1f;
    private bool isInvincible = false;
    
    [Header("Combat Visualization")]
    [SerializeField] private bool showAttackRange = true;
    [SerializeField] private bool showParryRange = true;
    [SerializeField] private Color attackRangeColor = Color.red;
    [SerializeField] private Color parryRangeColor = Color.cyan;
    [SerializeField] private float parryRangeRadius = 1.5f;

    public delegate void PlayerDeathHandler(int wavesSurvived);
    public event PlayerDeathHandler OnPlayerDeath;

    public delegate void HealthChangedHandler(int currentHealth);
    public event HealthChangedHandler OnHealthChanged;

    public int GetCurrentHealth()
    {
        return currentHealth;
    }
    
    private CharacterController characterController;
    private Vector3 moveDirection;
    private float verticalVelocity;
    private float currentSpeed;
    private float currentRotationVelocity;
    private bool isGrounded;
    private bool canJump = true;
    
    private int currentHealth;
    private float lastSlashTime;
    private bool isParrying;
    private float parryEndTime;
    
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        currentHealth = maxHealth;
        
        if (characterController == null)
        {
            Debug.LogError("CharacterController component missing!");
            return;
        }
        
        if (playerCamera == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
                playerCamera = mainCamera;
        }
    }
    
    void Update()
    {
        HandleMovement();
        HandleGravityAndJump();
        HandleCombat();
        ApplyMovement();
        UpdateParryState();
    }
    
    void HandleCombat()
    {
        // Left Click - Slash (Attack)
        if (Input.GetMouseButtonDown(0) && Time.time >= lastSlashTime + slashCooldown)
        {
            PerformSlash();
        }
        
        // Right Click - Parry
        if (Input.GetMouseButtonDown(1) && !isParrying)
        {
            StartParry();
        }
    }
    
    void PerformSlash()
    {
        lastSlashTime = Time.time;
        Debug.Log($"⚔️ SLASH PERFORMED! Range: {slashRange}m");
        
        // Use OverlapSphere for area attack (like a melee swing)
        Collider[] hitColliders = Physics.OverlapSphere(transform.position + transform.forward * 1f, slashRange);
        bool hitDetected = false;
        
        foreach (Collider hit in hitColliders)
        {
            EnemyBehavior enemy = hit.GetComponent<EnemyBehavior>();
            if (enemy != null)
            {
                // Check if enemy is in front of player (optional - for directional attack)
                Vector3 directionToEnemy = (enemy.transform.position - transform.position).normalized;
                float dotProduct = Vector3.Dot(transform.forward, directionToEnemy);
                
                // Only hit enemies in front (dot > 0 means in front 180 degrees)
                if (dotProduct > 0.3f) // Adjust angle threshold as needed
                {
                    enemy.TakeDamage();
                    hitDetected = true;
                    Debug.Log($"✓ Enemy hit and destroyed! Distance: {Vector3.Distance(transform.position, enemy.transform.position):F2}m");
                    break; // Hit only one enemy per slash (or remove break to hit multiple)
                }
            }
        }
        
        if (!hitDetected)
        {
            Debug.Log("✗ No enemy in attack range");
        }
        
        // Optional: Add visual/audio feedback here
        // PlaySlashAnimation();
        // PlaySlashSound();
    }
    
    void StartParry()
    {
        isParrying = true;
        parryEndTime = Time.time + parryDuration;
        Debug.Log($"🛡️ PARRY ACTIVATED! Duration: {parryDuration}s, Range: {parryRangeRadius}m");
        
        // Optional: Parry can also push back or damage enemies in range
        ParryPushback();
    }
    
    void ParryPushback()
    {
        // This makes parry push back enemies in range (optional feature)
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, parryRangeRadius);
        
        foreach (Collider hit in hitColliders)
        {
            EnemyBehavior enemy = hit.GetComponent<EnemyBehavior>();
            if (enemy != null)
            {
                // Push enemy back
                Vector3 pushDirection = (enemy.transform.position - transform.position).normalized;
                Rigidbody enemyRb = enemy.GetComponent<Rigidbody>();
                if (enemyRb != null)
                {
                    enemyRb.AddForce(pushDirection * 5f, ForceMode.Impulse);
                    Debug.Log("🛡️ Enemy pushed back by parry!");
                }
            }
        }
    }
    
    void UpdateParryState()
    {
        if (isParrying && Time.time >= parryEndTime)
        {
            isParrying = false;
            Debug.Log("🛡️ Parry ended");
        }
    }
    
    public bool IsParrying()
    {
        return isParrying;
    }
    
    public float GetParryRange()
    {
        return parryRangeRadius;
    }
    
    public float GetAttackRange()
    {
        return slashRange;
    }
    
    public void TakeDamage(int damage)
    {
        // Check invincibility
        if (isInvincible)
        {
            Debug.Log("Player is invincible - ignoring damage");
            return;
        }
        
        if (isParrying)
        {
            Debug.Log("🛡️ Attack parried! No damage taken.");
            return;
        }
        
        currentHealth -= damage;
        Debug.Log($"💔 Player took {damage} damage! Health: {currentHealth}/{maxHealth}");
        
        // Start invincibility frames
        StartCoroutine(InvincibilityFrames());
        
        OnHealthChanged?.Invoke(currentHealth);
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    IEnumerator InvincibilityFrames()
    {
        isInvincible = true;
        Debug.Log("Invincibility started!");
        
        yield return new WaitForSeconds(invincibilityDuration);
        
        isInvincible = false;
        Debug.Log("Invincibility ended!");
    }
    
    public void Die()
    {
        Debug.Log("💀 Player died!");
        
        int survivedWaves = WaveManager.Instance != null ? WaveManager.Instance.GetCurrentWave() : 0;
        
        OnPlayerDeath?.Invoke(survivedWaves);
        
        enabled = false;
        
        if (characterController != null)
        {
            characterController.enabled = false;
        }
    }
    
    void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        
        Vector3 cameraForward = playerCamera.transform.forward;
        Vector3 cameraRight = playerCamera.transform.right;
        
        cameraForward.y = 0;
        cameraRight.y = 0;
        cameraForward.Normalize();
        cameraRight.Normalize();
        
        moveDirection = (cameraForward * vertical) + (cameraRight * horizontal);
        moveDirection.Normalize();
        
        if (moveDirection != Vector3.zero)
        {
            float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, 
                ref currentRotationVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }
    }
    
    void HandleGravityAndJump()
    {
        // Simplified ground check
        Vector3 spherePosition = transform.position + Vector3.down * (characterController.height / 2);
        float sphereRadius = characterController.radius * 0.9f;
        
        isGrounded = Physics.CheckSphere(spherePosition + Vector3.down * groundCheckDistance, sphereRadius, groundMask);
        
        if (isGrounded)
        {
            if (verticalVelocity <= 0)
            {
                verticalVelocity = -2f;
                canJump = true;
            }
        }
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isGrounded && canJump)
            {
                verticalVelocity = jumpForce;
                canJump = false;
                Debug.Log($"JUMP EXECUTED! Velocity set to: {verticalVelocity}");
            }
        }
        
        if (!isGrounded || verticalVelocity > 0)
        {
            verticalVelocity += gravity * Time.deltaTime;
            
            if (verticalVelocity < terminalFallSpeed)
            {
                verticalVelocity = terminalFallSpeed;
            }
        }
    }
    
    void ApplyMovement()
    {
        Vector3 horizontalMovement = moveDirection * currentSpeed * Time.deltaTime;
        Vector3 verticalMovement = new Vector3(0, verticalVelocity, 0) * Time.deltaTime;
        
        CollisionFlags flags = characterController.Move(horizontalMovement + verticalMovement);
        
        if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0)
        {
            verticalVelocity = 0;
        }
    }
    
    public void SetCamera(Camera newCamera)
    {
        playerCamera = newCamera;
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw ground check gizmo
        if (characterController != null)
        {
            Vector3 spherePosition = transform.position + Vector3.down * (characterController.height / 2);
            float sphereRadius = characterController.radius * 0.9f;
            
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(spherePosition + Vector3.down * groundCheckDistance, sphereRadius);
        }
        
        // Draw Attack Range (Slash)
        if (showAttackRange)
        {
            Gizmos.color = attackRangeColor;
            // Draw attack range sphere
            Gizmos.DrawWireSphere(transform.position + transform.forward * 1f, slashRange);
            
            // Draw attack arc (semi-circle in front)
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            int segments = 20;
            float angleStep = 90f / segments; // 90 degree arc
            
            Vector3 prevPoint = transform.position + forward * slashRange;
            for (int i = 0; i <= segments; i++)
            {
                float angle = -45f + (i * angleStep); // -45 to +45 degrees
                Vector3 dir = Quaternion.Euler(0, angle, 0) * forward;
                Vector3 point = transform.position + dir * slashRange;
                Gizmos.DrawLine(prevPoint, point);
                prevPoint = point;
            }
            
            // Draw center line
            Gizmos.DrawRay(transform.position, forward * slashRange);
        }
        
        // Draw Parry Range
        if (showParryRange)
        {
            Gizmos.color = parryRangeColor;
            Gizmos.DrawWireSphere(transform.position, parryRangeRadius);
            
            // Draw pulsing effect for parry when active
            if (isParrying)
            {
                float pulse = Mathf.PingPong(Time.time * 5f, 0.2f) + 0.8f;
                Gizmos.color = new Color(parryRangeColor.r, parryRangeColor.g, parryRangeColor.b, 0.5f);
                Gizmos.DrawWireSphere(transform.position, parryRangeRadius * pulse);
            }
        }
    }
    
    // Optional: Always show ranges in Scene view (not just when selected)
    void OnDrawGizmos()
    {
        if (!Application.isPlaying && showAttackRange)
        {
            // Show attack range even when not playing
            Gizmos.color = attackRangeColor;
            Gizmos.DrawWireSphere(transform.position + transform.forward * 1f, slashRange);
            
            // Show parry range
            Gizmos.color = parryRangeColor;
            Gizmos.DrawWireSphere(transform.position, parryRangeRadius);
        }
    }
}