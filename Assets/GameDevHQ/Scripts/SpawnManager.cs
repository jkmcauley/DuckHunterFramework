using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [SerializeField] private Transform[] _waypoints;
    [SerializeField] private GameObject _enemyPrefab;
    [SerializeField] private int _poolSize = 20;
    [SerializeField] private float _spawnInterval = 6f;

    private readonly List<GameObject> _pool = new List<GameObject>();
    public int TotalEnemiesSpawned { get; private set; }

    public int EnemiesHit { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        for (int i = 0; i < _poolSize; i++)
        {
            GameObject enemy = Instantiate(_enemyPrefab);
            enemy.SetActive(false);
            _pool.Add(enemy);
        }

        StartCoroutine(SpawnEnemyRoutine(_spawnInterval));
    }

    public GameObject GetFromPool()
    {
        foreach (GameObject enemy in _pool)
        {
            if (!enemy.activeInHierarchy)
                return enemy;
        }

        return null;
    }

    public int ActiveEnemyCount()
    {
        int count = 0;
        foreach (GameObject enemy in _pool)
        {
            if (enemy.activeInHierarchy)
                count++;
        }
        return count;
    }

    public GameObject SpawnEnemy()
    {
        if (ActiveEnemyCount() >= _poolSize)
            return null;

        if (_waypoints == null || _waypoints.Length == 0 || _waypoints[0] == null)
            return null;

        GameObject enemy = GetFromPool();
        if (enemy == null)
            return null;

        Transform spawn = _waypoints[0];
        enemy.transform.SetPositionAndRotation(spawn.position, spawn.rotation);

        var agent = enemy.GetComponent<NavMeshAgent>();
        if (agent != null)
            agent.Warp(spawn.position);

        var ai = enemy.GetComponent<AIControl>();
        if (ai != null)
        {
            enemy.SetActive(true);
            ai.Init(_waypoints);
            EnemySpawned();
        }
        else
        {
            enemy.SetActive(true);
        }

        return enemy;
    }

    public void ReturnEnemy(GameObject enemy)
    {
        if (enemy == null)
            return;

        var agent = enemy.GetComponent<NavMeshAgent>();
        if (agent != null)
            agent.ResetPath();

        enemy.SetActive(false);
    }

    IEnumerator SpawnEnemyRoutine(float spawnInterval)
    {
        while (true)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    public void EnemySpawned()
    {
        TotalEnemiesSpawned++;
    }

    public void EnemyHit()
    {
        EnemiesHit++;
    }

    public float HitPercentage
    {
        get
        {
            if (TotalEnemiesSpawned == 0)
                return 0f;

            return (float)EnemiesHit / TotalEnemiesSpawned * 100f;
        }
    }
}
