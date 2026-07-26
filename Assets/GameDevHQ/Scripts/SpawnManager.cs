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
    [Tooltip("Don't spawn on the last waypoint (end of path / despawn point).")]
    [SerializeField] private bool _excludeEndWaypoint = true;

    private readonly List<GameObject> _pool = new List<GameObject>();

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

    /// <returns>Waypoint index, or -1 if none.</returns>
    int GetRandomSpawnIndex()
    {
        if (_waypoints == null || _waypoints.Length == 0)
            return -1;

        int max = _waypoints.Length;
        if (_excludeEndWaypoint && max > 1)
            max -= 1; // skip last = path end / pool return

        for (int attempt = 0; attempt < 8; attempt++)
        {
            int index = Random.Range(0, max);
            if (_waypoints[index] != null)
                return index;
        }

        return _waypoints[0] != null ? 0 : -1;
    }

    public GameObject SpawnEnemy()
    {
        if (ActiveEnemyCount() >= _poolSize)
            return null;

        GameObject enemy = GetFromPool();
        if (enemy == null)
            return null;

        int spawnIndex = GetRandomSpawnIndex();
        Transform spawn = spawnIndex >= 0 ? _waypoints[spawnIndex] : transform;
        enemy.transform.SetPositionAndRotation(spawn.position, spawn.rotation);

        var agent = enemy.GetComponent<NavMeshAgent>();
        if (agent != null)
            agent.Warp(spawn.position);

        var ai = enemy.GetComponent<AIControl>();
        if (ai != null)
        {
            ai.SetWaypoints(_waypoints);
            enemy.SetActive(true);
            // Use spawn index so they always continue forward (not nearest, which can reverse)
            if (spawnIndex >= 0)
                ai.BeginPatrolFromWaypointIndex(spawnIndex);
            else
                ai.BeginPatrolFromNearestPoint();
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
}
