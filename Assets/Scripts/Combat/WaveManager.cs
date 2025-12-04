using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class WaveManager : MonoBehaviour
{
    [Header("Wave Settings")]
    [SerializeField] private int enemiesPerWave = 4;
    [SerializeField] private float spawnDelay = 0.5f;
    [SerializeField] private bool autoStartWave = true;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Enemy Prefabs (can be 1)")]
    [SerializeField] private GameObject[] enemyPrefabs;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI enemiesLeftText;
    [SerializeField] private string uiFormat = "Enemies Left: {0}";

    [Header("Exit Trigger")]
    [SerializeField] private string exitTriggerTag = "ExitTrigger";
    [SerializeField] private GameObject exitTrigger;

    [Header("Events")]
    [SerializeField] private UnityEvent onWaveCleared;

    // --- Lists ---
    private Queue<GameObject> enemyPool = new();
    private List<GameObject> activeEnemies = new();
    // Maps a spawn point to the enemy currently spawned there
    private readonly Dictionary<Transform, GameObject> _activeSpawnPoints = new();
    // Queue for enemies that are waiting to be spawned when all points are busy.
    private readonly Queue<int> _pendingSpawns = new();

    // --- Private Fields ---
    private int enemiesToKill;
    private bool waveInProgress = false;
    private bool _watcherRunning = false;

    private void Awake()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            Debug.LogError("WaveManager: Assign at least one spawn point!", this);

        if (exitTrigger == null)
        {
            exitTrigger = GameObject.FindGameObjectWithTag(exitTriggerTag);
        }

        InitializePool();
        UpdateUI();
    }

    private void Start()
    {
        if (autoStartWave) StartWave();

        exitTrigger.SetActive(false);
    }

    public void StartWave()
    {
        if (waveInProgress) return;

        waveInProgress = true;
        enemiesToKill = enemiesPerWave;
        UpdateUI();

        // 1. Spawn the initial enemies immediately
        int initialSpawnCount = Mathf.Min(enemiesPerWave, spawnPoints.Length);
        for (int i = 0; i < initialSpawnCount; i++)
            SpawnEnemyAtFreePoint();

        // 2. Queeue the rest that need to be spawned
        for (int i = initialSpawnCount; i < enemiesPerWave; i++)
            _pendingSpawns.Enqueue(i);   // we just need a placeholder

        // 3. Start the corouting that watches the points
        if (!_watcherRunning)
            StartCoroutine(MonitorFreePoints());
    }

    IEnumerator SpawnEnemies()
    {
        for (int i = 0; i < enemiesPerWave; i++)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    void OnEnemyKilled(GameObject enemy)
    {
        // Find which spawn point it was using
        Transform usedSp = null;
        foreach (var kvp in _activeSpawnPoints)
            if (kvp.Value == enemy) { usedSp = kvp.Key; break; }

        if (usedSp != null)
            _activeSpawnPoints.Remove(usedSp);

        activeEnemies.Remove(enemy);
        enemiesToKill--;
        UpdateUI();

        ReturnToPool(enemy);

        // If we have queued spawns, try to spawn one now
        if (_pendingSpawns.Count > 0)
            SpawnEnemyAtFreePoint();   // this will also dequeue automatically

        if (enemiesToKill <= 0 && waveInProgress) WaveCleared();
    }

    void WaveCleared()
    {
        waveInProgress = false;
        UpdateUI();
        onWaveCleared?.Invoke();
        Debug.Log("Wave Cleared!");

        if (exitTrigger != null)
        {
            exitTrigger.SetActive(true);
        }
    }

    void SpawnEnemy()
    {
        // Get Enemy from pool
        GameObject enemy = GetPooledEnemy();
        if (enemy == null) return;

        // Activate enemy
        enemy.SetActive(true);
        activeEnemies.Add(enemy);

        // Hook death event
        if (enemy.TryGetComponent(out Health health))
        {
            health.onEnemyDeath.AddListener(() => OnEnemyKilled(enemy));
        }
        else
        {
            Debug.LogWarning($"Enemy {enemy.name} has no Health component!");
        }
    }

    private void SpawnEnemyAtFreePoint()
    {
        // Consume one queued enemy (if any)
        if (_pendingSpawns.Count > 0)
            _pendingSpawns.Dequeue();

        // Find a free spawn point
        Transform freeSp = null;

        foreach (var sp in spawnPoints)
        {
            if (!_activeSpawnPoints.ContainsKey(sp))
            {
                freeSp = sp;
                break;
            }
        }

        if (freeSp == null) return; // not a free point should be handled by the watcher

        GameObject enemy = GetPooledEnemy();
        if (!enemy) return;

        // Position & activate
        Vector3 safePos = freeSp.position;
        if (NavMesh.SamplePosition(safePos, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
            safePos = hit.position;
        enemy.transform.SetPositionAndRotation(safePos, freeSp.rotation);
        enemy.SetActive(true);

        // Register death callback
        if (enemy.TryGetComponent(out Health h))
            h.onEnemyDeath.AddListener(() => OnEnemyKilled(enemy));

        _activeSpawnPoints[freeSp] = enemy;
    }

    void InitializePool()
    {
        Vector3 dummyPos = new(0, -100f, 0); // Dummy position to initialize the whole pool

        int poolSize = enemiesPerWave * 3; // Always have buffer
        for (int i = 0; i < poolSize; i++)
        {
            // 1. Pick a random prefab
            GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

            // 2. Instantiate on the safe position
            GameObject obj = Instantiate(prefab, dummyPos, Quaternion.identity, transform);

            // 3. Disable NavMesh Agent to avoid warnings
            if (obj.TryGetComponent(out NavMeshAgent agent))
            {
                agent.enabled = false;
            }

            // 4. Set enemy object to false
            obj.SetActive(false);
            enemyPool.Enqueue(obj);
        }
    }

    GameObject GetPooledEnemy()
    {
        if (enemyPool.Count == 0)
        {
            Debug.LogWarning("Pool empty – creating fallback enemy.");
            return CreateFallbackEnemy();
        }

        GameObject enemy = enemyPool.Dequeue();

        // Re-enable NavMeshAgent now that we know the position is valid
        var agent = enemy.GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = true;

        return enemy;
    }

    // Fallback if pool runs dry
    private GameObject CreateFallbackEnemy()
    {
        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Vector3 pos = sp.position;
        NavMesh.SamplePosition(pos, out NavMeshHit hit, 1.5f, NavMesh.AllAreas);
        return Instantiate(prefab, hit.position, sp.rotation, transform);
    }

    void ReturnToPool(GameObject enemy)
    {
        // 1. Disable NavMesh Agent to avoid warnings
        if (enemy.TryGetComponent(out NavMeshAgent agent))
        {
            agent.enabled = false;
        }

        // 2. Move to a new random spawn point
        Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Vector3 safePos = sp.position;
        if (NavMesh.SamplePosition(safePos, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
        {
            safePos = hit.position;
        }

        enemy.transform.SetPositionAndRotation(safePos, sp.rotation);

        // 3. Deactivate
        enemy.SetActive(false);
        enemyPool.Enqueue(enemy);
    }

    void UpdateUI()
    {
        if (enemiesLeftText != null)
        {
            enemiesLeftText.text = string.Format(uiFormat, enemiesToKill);
        }
    }

    #region Helper Methods
    private IEnumerator MonitorFreePoints()
    {
        _watcherRunning = true;
        while (_pendingSpawns.Count > 0)
        {
            // Wait until at least one spawn point becomes free
            yield return new WaitUntil(() => _activeSpawnPoints.Count < spawnPoints.Length);

            // Spawn the next queued enemy
            SpawnEnemyAtFreePoint();

            Debug.Log($"!--- Wait before new enemy is spawned. Remaining {_pendingSpawns.Count}");
            // Wait before a new enemy can be spawned
            yield return new WaitForSeconds(spawnDelay);
        }
        _watcherRunning = false;
    }
    #endregion
}
