using UnityEngine;
using UnityEngine.AI;

public class EnemyBehavior : MonoBehaviour
{
    [Header("NavMesh Settings")]
    private NavMeshAgent _agent;
    
    [Header("Movement Settings")]
    [SerializeField] private float idleWaitTime = 2f;
    [SerializeField] private float wanderRadius = 10f;
    
    [Header("Combat Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float shootingRange = 15f;
    [SerializeField] private float shootingCooldown = 1.5f;
    [SerializeField] private float bulletSpeed = 20f;
    [SerializeField] private int damagePerBullet = 1;
    
    private Transform _player;
    private float lastShootTime;
    private bool isPlayerInRange = false;
    private bool isWandering = true;
    private float idleTimer = 0f;
    private Vector3 wanderTarget;
    private bool isDead = false;
    
    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        
        if (_agent == null)
        {
            _agent = gameObject.AddComponent<NavMeshAgent>();
        }
        
        _player = GameObject.FindGameObjectWithTag("Player").transform;
        
        if (_player == null)
        {
            _player = GameObject.Find("Player")?.transform;
        }
        
        // Start wandering
        SetRandomWanderTarget();
    }
    
    void Update()
    {
        if (isDead || _player == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, _player.position);
        
        if (distanceToPlayer <= shootingRange)
        {
            // Player in range - stop moving and shoot
            if (!isPlayerInRange)
            {
                isPlayerInRange = true;
                _agent.isStopped = true;
                _agent.ResetPath();
            }
            
            // Rotate to face player
            Vector3 directionToPlayer = (_player.position - transform.position).normalized;
            directionToPlayer.y = 0;
            if (directionToPlayer != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            }
            
            // Shoot if cooldown is ready
            if (Time.time >= lastShootTime + shootingCooldown)
            {
                Shoot();
            }
        }
        else
        {
            // Player out of range - wander
            if (isPlayerInRange)
            {
                isPlayerInRange = false;
                _agent.isStopped = false;
            }
            
            Wander();
        }
    }
    
    void Wander()
    {
        if (_agent.remainingDistance < 0.5f && !_agent.pathPending)
        {
            if (isWandering)
            {
                // Wait at current position
                isWandering = false;
                idleTimer = idleWaitTime;
                _agent.isStopped = true;
            }
            else
            {
                // Start wandering to new position
                if (idleTimer <= 0)
                {
                    SetRandomWanderTarget();
                    isWandering = true;
                    _agent.isStopped = false;
                }
                else
                {
                    idleTimer -= Time.deltaTime;
                }
            }
        }
    }
    
    void SetRandomWanderTarget()
    {
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += transform.position;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, -1))
        {
            wanderTarget = hit.position;
            _agent.destination = wanderTarget;
        }
    }
    
    void Shoot()
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("Bullet prefab not assigned on Enemy!");
            return;
        }
        
        if (firePoint == null)
        {
            Debug.LogError("Fire point not assigned on Enemy!");
            return;
        }
        
        lastShootTime = Time.time;
        
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        BulletBehavior bulletBehavior = bullet.GetComponent<BulletBehavior>();
        
        if (bulletBehavior != null)
        {
            Vector3 direction = (_player.position - firePoint.position).normalized;
            bulletBehavior.Initialize(direction, bulletSpeed, damagePerBullet);
        }
        else
        {
            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 direction = (_player.position - firePoint.position).normalized;
                rb.velocity = direction * bulletSpeed;
            }
        }
        
        Debug.Log($"Enemy {gameObject.name} shoots!");
    }
    
    public void TakeDamage()
    {
        if (isDead) return;
        
        isDead = true;
        Debug.Log($"Enemy {gameObject.name} defeated!");
        
        // Notify WaveManager
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.EnemyDefeated(gameObject);
        }
        
        Destroy(gameObject);
    }
    
    void OnDestroy()
    {
        // Cleanup in case enemy is destroyed without calling TakeDamage
        if (!isDead && WaveManager.Instance != null)
        {
            WaveManager.Instance.EnemyDefeated(gameObject);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, shootingRange);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);
        
        if (firePoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(firePoint.position, firePoint.forward * 2f);
        }
    }
}