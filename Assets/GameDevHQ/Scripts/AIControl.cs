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
    [SerializeField] private float _stoppingDistance = 0.5f;
    [SerializeField] private float _hideDuration = 6f;
    [SerializeField] private float _hideRadius = 3.5f;
    [SerializeField] private float _hideChance = 0.35f;
    [SerializeField] private float _hideCooldown = 10f;
    [SerializeField] private float _deathDelay = 2.5f;

    private NavMeshAgent _agent;
    private Animator _anim;
    private Transform[] _points;
    private int _index;
    private AIState _state = AIState.Running;

    private float _timer;
    private float _nextHide;
    private Transform _column;

    static readonly HashSet<Transform> _usedColumns = new HashSet<Transform>();

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponentInChildren<Animator>();
    }

    void OnEnable()
    {
        FreeColumn();
        if (_anim != null)
        {
            _anim.SetBool("Hiding", false);
            _anim.ResetTrigger("Death");
        }
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = true;
        ChangeState(AIState.Running);
    }

    void OnDisable()
    {
        FreeColumn();
    }

    void Update()
    {
        if (_agent == null) return;

        switch (_state)
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

    void ChangeState(AIState newState)
    {
        _state = newState;

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

    // ---- Running ----

    void EnterRunning()
    {
        _agent.isStopped = false;
        if (_anim != null) _anim.SetBool("Hiding", false);
        if (_points != null && _points.Length > 0)
            _agent.SetDestination(_points[_index].position);
    }

    void Running()
    {
        if (_points == null || _points.Length == 0) return;

        if (!_agent.hasPath && !_agent.pathPending)
            _agent.SetDestination(_points[_index].position);

        if (_anim != null)
        {
            _anim.SetBool("Hiding", false);
            _anim.SetFloat("Speed", _agent.velocity.magnitude);
        }

        if (Time.time >= _nextHide)
        {
            Transform col = NearestColumn();
            if (col != null && Random.value <= _hideChance && _usedColumns.Add(col))
            {
                _column = col;
                ChangeState(AIState.Hide);
                return;
            }
            if (col != null)
                _nextHide = Time.time + 2f;
        }

        if (!_agent.pathPending && _agent.hasPath && _agent.remainingDistance <= _stoppingDistance)
        {
            if (_index >= _points.Length - 1)
            {
                Despawn();
                return;
            }
            _index++;
            _agent.SetDestination(_points[_index].position);
        }
    }

    // ---- Hide ----

    void EnterHide()
    {
        _timer = _hideDuration;

        // stand on far side of column from the player/camera
        if (_column != null)
        {
            Vector3 fromPlayer = transform.position - _column.position;
            Camera cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (cam != null)
                fromPlayer = _column.position - cam.transform.position;

            fromPlayer.y = 0f;
            if (fromPlayer.sqrMagnitude > 0.01f)
            {
                fromPlayer.Normalize();
                Vector3 hidePos = _column.position + fromPlayer * 1.25f;
                hidePos.y = transform.position.y;
                _agent.Warp(hidePos);
                transform.rotation = Quaternion.LookRotation(fromPlayer);
            }
        }

        _agent.isStopped = true;
        _agent.ResetPath();
        _agent.velocity = Vector3.zero;

        if (_anim != null)
        {
            _anim.SetFloat("Speed", 0f);
            _anim.SetBool("Hiding", true);
            _anim.Play("Cover_idle", 0, 0f);
        }
    }

    void Hide()
    {
        // stay stopped the whole hide
        _agent.isStopped = true;
        _agent.velocity = Vector3.zero;

        if (_anim != null)
        {
            _anim.SetFloat("Speed", 0f);
            _anim.SetBool("Hiding", true);
        }

        _timer -= Time.deltaTime;
        if (_timer > 0f) return;

        FreeColumn();
        _nextHide = Time.time + _hideCooldown;
        ChangeState(AIState.Running);
    }

    // ---- Death ----

    void EnterDeath()
    {
        _timer = _deathDelay;
        FreeColumn();
        _agent.isStopped = true;
        _agent.ResetPath();

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (_anim != null)
        {
            _anim.SetBool("Hiding", false);
            _anim.SetFloat("Speed", 0f);
            _anim.SetTrigger("Death");
        }

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayDeathSound();
    }

    void Death()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
            Despawn();
    }

    // ---- Public ----

    public void Init(Transform[] points, int spawnIndex)
    {
        _points = points;
        _index = Mathf.Clamp(spawnIndex + 1, 0, points.Length - 1);
        FreeColumn();
        ChangeState(AIState.Running);
    }

    public void Die()
    {
        if (_state == AIState.Death) return;
        ChangeState(AIState.Death);
    }

    // ---- Helpers ----

    Transform NearestColumn()
    {
        Transform best = null;
        float bestDist = _hideRadius;

        foreach (var go in GameObject.FindGameObjectsWithTag("Column"))
        {
            if (_usedColumns.Contains(go.transform)) continue;

            Vector3 d = transform.position - go.transform.position;
            d.y = 0f;
            float dist = d.magnitude;
            if (dist < bestDist)
            {
                bestDist = dist;
                best = go.transform;
            }
        }
        return best;
    }

    void FreeColumn()
    {
        if (_column == null) return;
        _usedColumns.Remove(_column);
        _column = null;
    }

    void Despawn()
    {
        if (SpawnManager.Instance != null)
            SpawnManager.Instance.ReturnEnemy(gameObject);
        else
            gameObject.SetActive(false);
    }
}
