using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerRay : MonoBehaviour
{
    [Tooltip("Must include Default (cover/walls) and Enemy. First hit wins — cover blocks shots.")]
    [SerializeField] private LayerMask _layer;
    [SerializeField] private Camera _playerCamera;
    [Tooltip("Seconds between shots (1 = one shot per second)")]
    [SerializeField] private float _fireCooldown = 1f;
    [SerializeField] private int _ammoCount = 15;
    [SerializeField] private SoundManager _soundManager;

    private float _nextFireTime;

    void Awake()
    {
        if (_playerCamera == null)
            _playerCamera = GetComponent<Camera>();

        if (_playerCamera == null)
            _playerCamera = GetComponentInParent<Camera>();

        // New serialized fields on old components often load as 0
        if (_fireCooldown < 0.05f)
            _fireCooldown = 1f;

        // Enemy-only masks ignore columns/walls — always include Default for cover
        int defaultAndEnemy = LayerMask.GetMask("Default", "Enemy");
        if (_layer == 0)
            _layer = defaultAndEnemy;
        else
            _layer |= LayerMask.GetMask("Default");
    }

    void Update()
    {
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            Reload();
        }

        if (_playerCamera == null)
            return;

        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (Time.time < _nextFireTime)
            return;
        if (_ammoCount <= 0)
            return;

        _nextFireTime = Time.time + _fireCooldown;

        SoundManager.Instance.PlayGunShot();


        Ray rayOrigin = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // First solid hit only — columns/walls on Default block enemies behind them
        if (!Physics.Raycast(rayOrigin, out RaycastHit hitInfo, 100f, _layer))
            return;

        if (!hitInfo.collider.CompareTag("Enemy"))
            return;

        AIControl ai = hitInfo.collider.GetComponentInParent<AIControl>();
        if (ai != null)
            ai.Die();
        _ammoCount--;
    }

    void Reload()
    {
        _ammoCount = 15;
    }
}
