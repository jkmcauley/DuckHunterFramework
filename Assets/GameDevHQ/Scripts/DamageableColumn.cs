using System.Collections;
using UnityEngine;

public class DamageableColumn : MonoBehaviour
{
    [SerializeField] private int _hitsToBreak = 5;
    [SerializeField] private float _rechargeTime = 8f;
    [SerializeField] private float _dimMultiplier = 0.35f;
    [SerializeField] private bool _flickerWhileDown = true;
    [SerializeField] private float _flickerInterval = 0.12f;

    private int _hits;
    private bool _broken;
    private Collider _collider;
    private Renderer[] _renderers;
    private Color[] _originalColors;
    private Coroutine _rechargeRoutine;

    void Awake()
    {
        _collider = GetComponent<Collider>();
        _renderers = GetComponentsInChildren<Renderer>(true);
        CacheColors();
    }

    public void TakeHit()
    {
        if (_broken)
            return;

        _hits++;
        if (_hits < _hitsToBreak)
            return;

        if (_rechargeRoutine != null)
            StopCoroutine(_rechargeRoutine);

        _rechargeRoutine = StartCoroutine(BreakAndRecharge());
    }

    IEnumerator BreakAndRecharge()
    {
        _broken = true;

        if (_collider != null)
            _collider.enabled = false;

        float endTime = Time.time + _rechargeTime;

        if (_flickerWhileDown)
        {
            bool dimmed = true;
            while (Time.time < endTime)
            {
                ApplyDim(dimmed);
                dimmed = !dimmed;
                yield return new WaitForSeconds(_flickerInterval);
            }
        }
        else
        {
            ApplyDim(true);
            yield return new WaitForSeconds(_rechargeTime);
        }

        ApplyDim(false);

        if (_collider != null)
            _collider.enabled = true;

        _hits = 0;
        _broken = false;
        _rechargeRoutine = null;
    }

    void CacheColors()
    {
        _originalColors = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            Material mat = _renderers[i].material;
            if (mat.HasProperty("_BaseColor"))
                _originalColors[i] = mat.GetColor("_BaseColor");
            else if (mat.HasProperty("_Color"))
                _originalColors[i] = mat.color;
            else
                _originalColors[i] = Color.white;
        }
    }

    void ApplyDim(bool dim)
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            Material mat = _renderers[i].material;
            Color c = dim ? _originalColors[i] * _dimMultiplier : _originalColors[i];
            c.a = _originalColors[i].a;

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", c);
            else if (mat.HasProperty("_Color"))
                mat.color = c;
        }
    }
}
