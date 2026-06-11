using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    [Header("Wave Settings")]
    [SerializeField] private int startingWave = 1;
    [SerializeField] private float timeBetweenWaves = 5f;
    [SerializeField] private float spawnDelayBetweenEnemies = 0.5f;
    
    [Header("Enemy Spawning")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int maxEnemiesPerWave = 20;
    
    [Header("Spawn Range (Square)")]
    [SerializeField] private float spawnRangeWidth = 20f;
    [SerializeField] private float spawnRangeLength = 20f;
    [SerializeField] private float spawnYOffset = 0f;
    [SerializeField] private Vector2 centerPoint = Vector2.zero;
    
    [Header("Wave Progression")]
    [SerializeField] private AnimationCurve enemyCountCurve = AnimationCurve.Linear(1, 1, 20, 20);
    
    private int currentWave;
    private int enemiesRemaining;
    private int enemiesSpawned;
    private int totalEnemiesInWave;
    private bool isSpawning;
    private bool isWaitingForNextWave;
    private List<GameObject> activeEnemies = new List<GameObject>();
    
    public static WaveManager Instance { get; private set; }
    
    public delegate void WaveChangedHandler(int waveNumber);
    public event WaveChangedHandler OnWaveChanged;
    
    public delegate void WaveClearedHandler(int waveNumber);
    public event WaveClearedHandler OnWaveCleared;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        currentWave = startingWave - 1;
        StartNextWave();
    }
    
    public void StartNextWave()
    {
        if (isWaitingForNextWave) return;
        
        currentWave++;
        OnWaveChanged?.Invoke(currentWave);
        
        // Calculate enemies to spawn (max 20)
        totalEnemiesInWave = Mathf.RoundToInt(enemyCountCurve.Evaluate(currentWave));
        totalEnemiesInWave = Mathf.Min(totalEnemiesInWave, maxEnemiesPerWave);
        
        enemiesRemaining = totalEnemiesInWave;
        enemiesSpawned = 0;
        
        Debug.Log($"=== WAVE {currentWave} STARTING ===");
        Debug.Log($"Total enemies in this wave: {totalEnemiesInWave}");
        
        StartCoroutine(SpawnWave());
    }
    
    IEnumerator SpawnWave()
    {
        isSpawning = true;
        Debug.Log($"Spawning {totalEnemiesInWave} enemies...");
        
        for (int i = 0; i < totalEnemiesInWave; i++)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(spawnDelayBetweenEnemies);
        }
        
        isSpawning = false;
        Debug.Log($"Spawning complete. Waiting for {enemiesRemaining} enemies to be defeated.");
        
        // If no enemies were spawned (shouldn't happen), force wave complete
        if (totalEnemiesInWave == 0)
        {
            OnWaveComplete();
        }
    }
    
    void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("Enemy prefab not assigned in WaveManager!");
            return;
        }
        
        // Generate random position within square range
        Vector3 spawnPosition = GetRandomSpawnPosition();
        
        // Raycast to ground
        RaycastHit hit;
        if (Physics.Raycast(spawnPosition + Vector3.up * 10f, Vector3.down, out hit, 20f))
        {
            spawnPosition.y = hit.point.y;
        }
        
        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        
        // Ensure enemy has EnemyBehavior and assign wave manager reference
        EnemyBehavior enemyBehavior = enemy.GetComponent<EnemyBehavior>();
        if (enemyBehavior == null)
        {
            enemyBehavior = enemy.AddComponent<EnemyBehavior>();
        }
        
        activeEnemies.Add(enemy);
        enemiesSpawned++;
        
        Debug.Log($"Enemy spawned ({enemiesSpawned}/{totalEnemiesInWave}) at {spawnPosition}");
    }
    
    Vector3 GetRandomSpawnPosition()
    {
        float halfWidth = spawnRangeWidth / 2f;
        float halfLength = spawnRangeLength / 2f;
        
        float randomX = Random.Range(centerPoint.x - halfWidth, centerPoint.x + halfWidth);
        float randomZ = Random.Range(centerPoint.y - halfLength, centerPoint.y + halfLength);
        
        return new Vector3(randomX, spawnYOffset, randomZ);
    }
    
    public void EnemyDefeated(GameObject enemy)
    {
        // Remove from active list
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
        }
        
        // Decrement remaining enemies
        enemiesRemaining--;
        Debug.Log($"Enemy defeated! {enemiesRemaining}/{totalEnemiesInWave} enemies remaining in wave {currentWave}");
        
        // Check if wave is complete
        if (enemiesRemaining <= 0 && !isSpawning && !isWaitingForNextWave)
        {
            OnWaveComplete();
        }
    }
    
    void OnWaveComplete()
    {
        if (isWaitingForNextWave) return;
        
        Debug.Log($"!!! WAVE {currentWave} COMPLETE !!!");
        OnWaveCleared?.Invoke(currentWave);
        
        isWaitingForNextWave = true;
        StartCoroutine(StartNextWaveWithDelay());
    }
    
    IEnumerator StartNextWaveWithDelay()
    {
        Debug.Log($"Next wave starting in {timeBetweenWaves} seconds...");
        yield return new WaitForSeconds(timeBetweenWaves);
        
        isWaitingForNextWave = false;
        StartNextWave();
    }
    
    public int GetCurrentWave()
    {
        return currentWave;
    }
    
    public int GetRemainingEnemies()
    {
        return enemiesRemaining;
    }
    
    public int GetTotalEnemiesInWave()
    {
        return totalEnemiesInWave;
    }
    
    public void ResetWaveSystem()
    {
        // Clear all active enemies
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }
        activeEnemies.Clear();
        
        currentWave = startingWave - 1;
        isWaitingForNextWave = false;
        isSpawning = false;
        StopAllCoroutines();
        StartNextWave();
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 center = new Vector3(centerPoint.x, spawnYOffset, centerPoint.y);
        Vector3 size = new Vector3(spawnRangeWidth, 1f, spawnRangeLength);
        Gizmos.DrawWireCube(center, size);
    }
}