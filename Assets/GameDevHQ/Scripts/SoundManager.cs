using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Source")]
    [SerializeField] private AudioSource _sfxSource;

    [Header("Clips")]
    [SerializeField] private AudioClip _gunShot;
    [SerializeField] private AudioClip _explosionClip;
    [SerializeField] private AudioClip _deathSound;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_sfxSource == null)
            _sfxSource = GetComponent<AudioSource>();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void PlayGunShot()
    {
        PlayClip(_gunShot);
    }

    public void PlayExplosion()
    {
        PlayClip(_explosionClip);
    }

    public void PlayDeathSound()
    {
        PlayClip(_deathSound);
    }

    void PlayClip(AudioClip clip)
    {
        if (_sfxSource == null || clip == null)
            return;

        _sfxSource.PlayOneShot(clip);
    }
}
