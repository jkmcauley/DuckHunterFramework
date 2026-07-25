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

    private float _stoppingDistance = 0.5f;
    private int _currentPointIndex = 0;
    [SerializeField] private AIState _currentState = AIState.Running;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
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

        int nearest = GetNearestPointIndex();
        // Don't start by targeting the end point — go to the next one after nearest
        int next = (nearest + 1) % _points.Length;
        if (next == EndPointIndex)
            next = (next + 1) % _points.Length;

        _currentPointIndex = next;

        if (_agent.isOnNavMesh)
            _agent.SetDestination(_points[_currentPointIndex].position);
    }

    int EndPointIndex => _points.Length - 1; // waypoint[10] if you have 11 points (0–10)

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

        if (_agent.isOnNavMesh && !_agent.hasPath && !_agent.pathPending)
            _agent.SetDestination(_points[_currentPointIndex].position);

        GoToNextPoint();
        UpdateAnimator();

        switch (_currentState)
        {
            case AIState.Running:
                break;
            case AIState.Hide:
                if (_animator != null)
                    _animator.SetBool("Hiding", true);
                break;
            case AIState.Death:
                if (_animator != null)
                    _animator.SetTrigger("Death");
                break;
        }
    }

    void UpdateAnimator()
    {
        if (_animator == null || _agent == null)
            return;

        _animator.SetFloat("Speed", _agent.velocity.magnitude);

        if (_currentState != AIState.Hide)
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
        if (!HasValidPoints())
            return;

        if (!HasReachedDestination())
            return;

        // Arrived at final waypoint → return to pool via Singleton
        if (_currentPointIndex == EndPointIndex)
        {
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
