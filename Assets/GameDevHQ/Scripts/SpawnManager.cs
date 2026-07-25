using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private Transform[] _waypoints;
    [SerializeField] private GameObject _enemyPrefab;
    [SerializeField] private int _poolSize = 20;
    [SerializeField] private float _spawnInterval = 4f;

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

        // Cap at pool size — do not grow
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

        GameObject enemy = GetFromPool();
        if (enemy == null)
            return null;

        enemy.transform.SetPositionAndRotation(_spawnPoint.position, _spawnPoint.rotation);

        var agent = enemy.GetComponent<NavMeshAgent>();
        if (agent != null)
            agent.Warp(_spawnPoint.position);

        var ai = enemy.GetComponent<AIControl>();
        if (ai != null)
        {
            ai.SetWaypoints(_waypoints);
            enemy.SetActive(true);
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
