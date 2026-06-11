using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletBehavior : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private int damage;
    private bool isInitialized = false;
    
    [Header("Settings")]
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private GameObject hitEffect;
    
    void Start()
    {
        Destroy(gameObject, lifetime);
    }
    
    void Update()
    {
        if (isInitialized)
        {
            transform.position += direction * speed * Time.deltaTime;
        }
    }
    
    public void Initialize(Vector3 shootDirection, float bulletSpeed, int bulletDamage)
    {
        direction = shootDirection.normalized;
        speed = bulletSpeed;
        damage = bulletDamage;
        isInitialized = true;
        
        // Rotasi bullet mengikuti arah tembakan
        transform.rotation = Quaternion.LookRotation(direction);
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Check collision with player
        PlayerBehavior player = other.GetComponent<PlayerBehavior>();
        if (player != null)
        {
            // Check if player is parrying
            if (player.IsParrying())
            {
                Debug.Log("✦ BULLET PARRIED! Bullet destroyed, player takes no damage");
                DestroyBullet();
                return;
            }
            
            player.TakeDamage(damage);
            Debug.Log($"💥 BULLET HIT! Player took {damage} damage");
            DestroyBullet();
            return;
        }
        
        // Destroy bullet when hitting environment (walls, ground)
        if (!other.isTrigger && other.gameObject.layer != LayerMask.NameToLayer("Enemy"))
        {
            Debug.Log("Bullet hit environment, destroyed");
            DestroyBullet();
        }
    }
    
    void DestroyBullet()
    {
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }
        
        Destroy(gameObject);
    }
}