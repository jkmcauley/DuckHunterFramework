using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum AIState
{
    Running,
    Hide,
    Death
}

public class AIControl : MonoBehaviour
{
    [SerializeField] private NavMeshAgent _agent;
    [SerializeField] private Transform[] _points;
    private Animator _animator;

    [Header("Patrol")]
    [SerializeField] private float _stoppingDistance = 0.5f;
    [SerializeField] private AIState _currentState = AIState.Running;
    private int _currentPointIndex;

    [Header("Hide")]
    [SerializeField] private float _hideDuration = 3.5f;
    [SerializeField] private float _columnDetectRadius = 3.5f;
    [SerializeField] private float _hideChance = 0.2f;
    [SerializeField] private float _hideCooldown = 10f;
    [SerializeField] private float _sameFloorYTolerance = 4f;
    [SerializeField] private string _columnTag = "Column";
    private float _nextHideTime;
    private float _hideEndTime;
    private Transform _occupiedColumn;

    [Header("Death")]
    [SerializeField] private float _deathDespawnDelay = 2.5f;
    private float _deathEndTime;

    static readonly HashSet<Transform> s_OccupiedColumns = new HashSet<Transform>();

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();

        if (_agent != null)
            _agent.avoidancePriority = Random.Range(0, 99);
    }

    void OnEnable()
    {
        ReleaseColumn();
        ResetVisualsForPool();
        ChangeState(AIState.Running);
    }

    void OnDisable()
    {
        ReleaseColumn();
    }

    void Start()
    {
        BeginPatrolFromNearestPoint();
    }

    void Update()    //updates state every frame
    {
        if (!gameObject.activeInHierarchy || _agent == null)
            return;

        switch (_currentState)
        {
            case AIState.Running:
                Running();
                break;
            case AIState.Hide:
                Hide();
                break;
            case AIState.Death:
                Death();
                break;
        }
    }


    // State machine checks when state changes, not every frame


    void ChangeState(AIState newState)
    {
        _currentState = newState;

        switch (newState)
        {
            case AIState.Running:
                EnterRunning();
                break;
            case AIState.Hide:
                EnterHide();
                break;
            case AIState.Death:
                EnterDeath();
                break;
        }
    }

    void EnterRunning()
    {
        if (_animator != null)
        {
            _animator.SetBool("Hiding", false);
            _animator.ResetTrigger("Death");
        }

        if (_agent != null)
            _agent.isStopped = false;

        if (HasValidPoints() && _agent != null && _agent.isOnNavMesh)
            _agent.SetDestination(_points[_currentPointIndex].position);
    }

    void Running()
    {
        if (!HasValidPoints())
            return;

        if (_agent.isOnNavMesh && !_agent.hasPath && !_agent.pathPending)
            _agent.SetDestination(_points[_currentPointIndex].position);

        if (_animator != null)
        {
            _animator.SetBool("Hiding", false);
            _animator.SetFloat("Speed", _agent.velocity.magnitude);
        }

        TryEnterHide();
        AdvanceWaypointIfReached();
    }

    void EnterHide()
    {
        _hideEndTime = Time.time + _hideDuration;

        if (_agent != null)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }

        if (_animator != null)
        {
            _animator.SetFloat("Speed", 0f);
            _animator.SetBool("Hiding", true);
        }
    }

    void Hide()
    {
        if (_animator != null)
        {
            _animator.SetFloat("Speed", 0f);
            _animator.SetBool("Hiding", true);
        }

        if (Time.time < _hideEndTime)
            return;

        ReleaseColumn();
        _nextHideTime = Time.time + _hideCooldown;
        ChangeState(AIState.Running);
    }

    void EnterDeath()
    {
        _deathEndTime = Time.time + _deathDespawnDelay;
        ReleaseColumn();

        if (_agent != null)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }

        var col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        if (_animator != null)
        {
            _animator.SetBool("Hiding", false);
            _animator.SetFloat("Speed", 0f);
            _animator.SetTrigger("Death");
        }

        // SoundManager lives in the scene (singleton), not on the enemy prefab
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayDeathSound();
    }

    void Death()
    {
        if (Time.time < _deathEndTime)
            return;

        if (SpawnManager.Instance != null)
            SpawnManager.Instance.ReturnEnemy(gameObject);
        else
            gameObject.SetActive(false);
    }


    // Public Methods below


    public void SetWaypoints(Transform[] points)
    {
        _points = points;
    }

    /// <summary>
    /// Start patrol at the waypoint after spawnIndex. Always moves forward toward the end — never wraps to 0.
    /// </summary>
    public void BeginPatrolFromWaypointIndex(int spawnIndex)
    {
        if (!HasValidPoints() || _agent == null)
            return;

        ReleaseColumn();

        spawnIndex = Mathf.Clamp(spawnIndex, 0, _points.Length - 1);
        int next = spawnIndex + 1;
        if (next >= _points.Length)
            next = EndPointIndex;

        _currentPointIndex = next;
        ChangeState(AIState.Running);
    }

    /// <summary>
    /// Resume from the nearest waypoint, always continuing forward (never wraps back to start).
    /// </summary>
    public void BeginPatrolFromNearestPoint()
    {
        if (!HasValidPoints() || _agent == null)
            return;

        ReleaseColumn();

        int nearest = GetNearestPointIndex();
        int next = nearest + 1;
        if (next >= _points.Length)
            next = EndPointIndex;

        _currentPointIndex = next;
        ChangeState(AIState.Running);
    }

    public void Die()
    {
        if (_currentState == AIState.Death)
            return;

        ChangeState(AIState.Death);
    }


    // Running methods


    void TryEnterHide()
    {
        if (Time.time < _nextHideTime)
            return;

        Transform column = FindNearbyFreeColumn();
        if (column == null)
            return;

        if (Random.value > _hideChance)
        {
            _nextHideTime = Time.time + 2f;
            return;
        }

        if (!TryOccupyColumn(column))
            return;

        ChangeState(AIState.Hide);
    }

    void AdvanceWaypointIfReached()
    {
        if (!HasReachedDestination())
            return;

        if (_currentPointIndex == EndPointIndex)
        {
            ReleaseColumn();
            if (SpawnManager.Instance != null)
                SpawnManager.Instance.ReturnEnemy(gameObject);
            else
                gameObject.SetActive(false);
            return;
        }

        _currentPointIndex++;
        _agent.SetDestination(_points[_currentPointIndex].position);
    }





    void ResetVisualsForPool()
    {
        if (_animator != null)
        {
            _animator.SetBool("Hiding", false);
            _animator.ResetTrigger("Death");
        }

        var col = GetComponent<Collider>();
        if (col != null)
            col.enabled = true;
    }

    int EndPointIndex => _points.Length - 1;

    bool HasValidPoints()
    {
        if (_points == null || _points.Length == 0)
            return false;

        for (int i = 0; i < _points.Length; i++)
        {
            if (_points[i] == null)
                return false;
        }

        return true;
    }

    int GetNearestPointIndex()
    {
        int nearest = 0;
        float bestDist = float.MaxValue;

        for (int i = 0; i < _points.Length; i++)
        {
            float dist = Vector3.Distance(transform.position, _points[i].position);
            if (dist < bestDist)
            {
                bestDist = dist;
                nearest = i;
            }
        }

        return nearest;
    }

    bool HasReachedDestination()
    {
        if (_agent == null || !_agent.isOnNavMesh || _agent.pathPending)
            return false;

        if (_agent.pathStatus == NavMeshPathStatus.PathInvalid)
            return false;

        if (!_agent.hasPath)
            return false;

        return _agent.remainingDistance <= _stoppingDistance;
    }

    Transform FindNearbyFreeColumn()
    {
        GameObject[] columns = GameObject.FindGameObjectsWithTag(_columnTag);
        Transform nearest = null;
        float best = _columnDetectRadius;

        foreach (GameObject col in columns)
        {
            Transform t = col.transform;
            if (s_OccupiedColumns.Contains(t))
                continue;

            if (Mathf.Abs(transform.position.y - t.position.y) > _sameFloorYTolerance)
                continue;

            Vector3 delta = transform.position - t.position;
            delta.y = 0f;
            float d = delta.magnitude;
            if (d < best)
            {
                best = d;
                nearest = t;
            }
        }

        return nearest;
    }

    bool TryOccupyColumn(Transform column)
    {
        if (column == null || s_OccupiedColumns.Contains(column))
            return false;

        s_OccupiedColumns.Add(column);
        _occupiedColumn = column;
        return true;
    }

    void ReleaseColumn()
    {
        if (_occupiedColumn == null)
            return;

        s_OccupiedColumns.Remove(_occupiedColumn);
        _occupiedColumn = null;
    }
}
