using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

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
    private int _direction = 1;
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

    /// <summary>
    /// Finds the closest waypoint, then heads to the next one so every enemy
    /// continues the loop in the same direction (never runs back to index 0).
    /// Call this from SpawnManager after setting position / NavMeshAgent.Warp.
    /// </summary>
    public void BeginPatrolFromNearestPoint()
    {
        if (_points == null || _points.Length == 0 || _agent == null)
            return;

        int nearest = GetNearestPointIndex();
        _currentPointIndex = (nearest + 1) % _points.Length;

        if (_agent.isOnNavMesh)
            _agent.SetDestination(_points[_currentPointIndex].position);
    }

    int GetNearestPointIndex()
    {
        int nearest = 0;
        float bestDist = float.MaxValue;

        for (int i = 0; i < _points.Length; i++)
        {
            if (_points[i] == null)
                continue;

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
        if (_agent != null && _agent.isOnNavMesh && !_agent.hasPath && !_agent.pathPending
            && _points != null && _points.Length > 0)
        {
            _agent.SetDestination(_points[_currentPointIndex].position);
        }

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

        // Robot.controller: Idle < 0.01 < Walk < 3 < Running
        float speed = _agent.velocity.magnitude;
        _animator.SetFloat("Speed", speed);

        if (_currentState != AIState.Hide)
            _animator.SetBool("Hiding", false);
    }

    bool HasReachedDestination()
    {
        if (_agent == null || !_agent.isOnNavMesh || _agent.pathPending)
            return false;

        if (_agent.pathStatus == NavMeshPathStatus.PathInvalid)
            return false;

        // remainingDistance is unreliable until a path exists
        if (!_agent.hasPath)
            return false;

        return _agent.remainingDistance <= _stoppingDistance;
    }



    public void GoToNextPoint()
    {
        if (_points == null || _points.Length == 0)
            return;

        if (!HasReachedDestination())
            return;

        _currentPointIndex = (_currentPointIndex + 1) % _points.Length;
        _agent.SetDestination(_points[_currentPointIndex].position);
    }




}
