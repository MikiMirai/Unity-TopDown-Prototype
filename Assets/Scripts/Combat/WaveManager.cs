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

    private Queue<GameObject> enemyPool = new();
    private List<GameObject> activeEnemies = new();
    private int enemiesToKill;
    private bool waveInProgress = false;

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

        StartCoroutine(SpawnEnemies());
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
        activeEnemies.Remove(enemy);
        enemiesToKill--;
        UpdateUI();

        ReturnToPool(enemy);

        if (enemiesToKill <= 0 && waveInProgress)
        {
            WaveCleared();
        }
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

    void InitializePool()
    {
        int poolSize = enemiesPerWave * 3; // Always have buffer
        for (int i = 0; i < poolSize; i++)
        {
            // 1. Pick a random prefab
            GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

            // 2. Pick a random spawn point
            Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];

            // 3. Snap to a NavMesh
            Vector3 safePos = sp.position;
            if (NavMesh.SamplePosition(safePos, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
                safePos = hit.position;

            // 4. Instantiate on the safe position
            GameObject obj = Instantiate(prefab, safePos, sp.rotation, transform);

            // Disable NavMesh Agent to avoid warnings
            if (obj.TryGetComponent(out NavMeshAgent agent))
            {
                agent.enabled = false;
            }

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

        enemy.transform.position = safePos;
        enemy.transform.rotation = sp.rotation;

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
}
