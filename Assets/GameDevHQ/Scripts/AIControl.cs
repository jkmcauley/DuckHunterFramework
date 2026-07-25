using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

enum AIState
{
    Running, Hide, Death
}

public class AIControl : MonoBehaviour
{
    [SerializeField] private NavMeshAgent _agent;
    [SerializeField] private Transform[] _points;
    private Animator _animator;

    [Header("Patrol")]
    [SerializeField] private float _stoppingDistance = 0.5f;
    private int _currentPointIndex = 0;
    [SerializeField] private AIState _currentState = AIState.Running;

    [Header("Hide")]
    [SerializeField] private float _hideDuration = 3.5f;
    [SerializeField] private float _columnDetectRadius = 3.5f;
    [SerializeField] private float _hideChance = 0.2f;
    [SerializeField] private float _hideCooldown = 10f;
    [SerializeField] private float _sameFloorYTolerance = 4f; // ignore columns on other levels
    [SerializeField] private string _columnTag = "Column";

    private float _nextHideTime;
    private Coroutine _hideRoutine;
    private Transform _occupiedColumn;

    static readonly HashSet<Transform> s_OccupiedColumns = new HashSet<Transform>();

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();

        // Spread priorities so agents don't deadlock on narrow ramps
        if (_agent != null)
            _agent.avoidancePriority = Random.Range(0, 99);
    }

    void OnEnable()
    {
        _currentState = AIState.Running;
        _nextHideTime = 0f;
        ReleaseColumn();
        if (_animator != null)
            _animator.SetBool("Hiding", false);
        if (_agent != null)
            _agent.isStopped = false;
    }

    void OnDisable()
    {
        if (_hideRoutine != null)
        {
            StopCoroutine(_hideRoutine);
            _hideRoutine = null;
        }
        ReleaseColumn();
    }

    void Start()
    {
        BeginPatrolFromNearestPoint();
    }

    public void SetWaypoints(Transform[] points)
    {
        _points = points;
    }

    public void BeginPatrolFromNearestPoint()
    {
        if (!HasValidPoints() || _agent == null)
            return;

        ReleaseColumn();
        _currentState = AIState.Running;

        int nearest = GetNearestPointIndex();
        int next = (nearest + 1) % _points.Length;
        if (next == EndPointIndex)
            next = (next + 1) % _points.Length;

        _currentPointIndex = next;

        if (_agent.isOnNavMesh)
        {
            _agent.isStopped = false;
            _agent.SetDestination(_points[_currentPointIndex].position);
        }
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

    void Update()
    {
        if (!HasValidPoints() || _agent == null || !gameObject.activeInHierarchy)
            return;

        if (_currentState == AIState.Death)
            return;

        if (_currentState == AIState.Running)
            UpdateRunning();

        UpdateAnimator();
    }

    void UpdateRunning()
    {
        if (_agent.isOnNavMesh && !_agent.hasPath && !_agent.pathPending)
            _agent.SetDestination(_points[_currentPointIndex].position);

        TryStartHideNearColumn();
        GoToNextPoint();
    }

    void TryStartHideNearColumn()
    {
        if (Time.time < _nextHideTime || _hideRoutine != null)
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

        // Hide in place — don't path to the column (that caused turn-arounds)
        _hideRoutine = StartCoroutine(HideRoutine());
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

            // Same floor only — upper/lower columns share XZ and were stealing detection
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

    IEnumerator HideRoutine()
    {
        _currentState = AIState.Hide;

        _agent.isStopped = true;
        _agent.ResetPath();

        if (_animator != null)
        {
            _animator.SetFloat("Speed", 0f);
            _animator.SetBool("Hiding", true);
        }

        yield return new WaitForSeconds(_hideDuration);

        if (_animator != null)
            _animator.SetBool("Hiding", false);

        ReleaseColumn();
        _agent.isStopped = false;
        _currentState = AIState.Running;
        _nextHideTime = Time.time + _hideCooldown;
        _hideRoutine = null;

        if (_agent.isOnNavMesh && HasValidPoints())
            _agent.SetDestination(_points[_currentPointIndex].position);
    }

    void UpdateAnimator()
    {
        if (_animator == null || _agent == null)
            return;

        if (_currentState == AIState.Hide)
        {
            _animator.SetFloat("Speed", 0f);
            _animator.SetBool("Hiding", true);
            return;
        }

        _animator.SetFloat("Speed", _agent.velocity.magnitude);
        _animator.SetBool("Hiding", false);
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

    public void GoToNextPoint()
    {
        if (_currentState != AIState.Running || !HasValidPoints() || !HasReachedDestination())
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
}
