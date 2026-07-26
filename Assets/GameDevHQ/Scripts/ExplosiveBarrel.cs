using System.Collections;
using UnityEngine;

public class ExplosiveBarrel : MonoBehaviour
{
    [SerializeField] private GameObject _explosion;
    [SerializeField] private float _killRadius = 3f;

    [Tooltip("How long barrel stays visible after explosion starts (so blast and barrel overlap).")]
    [SerializeField] private float _barrelHideDelay = 0.25f;

    [Tooltip("How long the explosion VFX stays in the world before it is destroyed.")]
    [SerializeField] private float _explosionLifetime = 3f;

    private bool _exploded;

    void Awake()
    {
        if (_explosion == null)
            _explosion = FindExplosionChild();
    }

    public void Explode()
    {
        if (_exploded)
            return;

        _exploded = true;
        StartCoroutine(ExplodeRoutine());
    }

    IEnumerator ExplodeRoutine()
    {
        if (_explosion == null)
            _explosion = FindExplosionChild();

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayExplosion();

        KillNearbyEnemies();

        // Stop further shots, but keep the barrel mesh visible for now
        DisableColliders();

        if (_explosion != null)
        {
            // worldPositionStays = true so it stays on the barrel spot
            _explosion.transform.SetParent(null, true);
            _explosion.SetActive(true);

            ParticleSystem[] particles = _explosion.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
                particles[i].Play(true);

            Destroy(_explosion, _explosionLifetime);
        }

        // Wait so the player sees explosion ON the barrel, then remove barrel
        yield return new WaitForSeconds(_barrelHideDelay);

        HideBarrelVisuals();
        Destroy(gameObject);
    }

    void KillNearbyEnemies()
    {
        AIControl[] enemies = FindObjectsByType<AIControl>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Vector3 origin = transform.position;

        for (int i = 0; i < enemies.Length; i++)
        {
            AIControl ai = enemies[i];
            if (ai == null || !ai.gameObject.activeInHierarchy)
                continue;

            if (Vector3.Distance(origin, ai.transform.position) <= _killRadius)
                ai.Die();
        }
    }

    void DisableColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;
    }

    void HideBarrelVisuals()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = false;
    }

    GameObject FindExplosionChild()
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform t = children[i];
            if (t == transform)
                continue;

            if (t.CompareTag("Explosion") || t.name.StartsWith("Explosion"))
                return t.gameObject;
        }

        return null;
    }
}
