using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private TMP_Text _timerText;
    [SerializeField] private TMP_Text _accuracyText;

    private int _score;
    private float _timer = 90f;

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

    void Update()
    {
        UpdateTimer();
        UpdateUI();
    }

    public void UpdateTimer()
    {
        if (_timer > 0f)
            _timer -= Time.deltaTime;

        if (_timer <= 0f)
            Debug.Log("Gameover");
    }

    public void UpdateUI()
    {
        if (_scoreText != null)
            _scoreText.text = $"Score: {_score}";

        if (_timerText != null)
            _timerText.text = $"Time: {Mathf.CeilToInt(Mathf.Max(0f, _timer))}";

        float hits = 0f;
        if (SpawnManager.Instance != null)
            hits = SpawnManager.Instance.HitPercentage;

        if (_accuracyText != null)
            _accuracyText.text = $"Hits: {hits:F1}%";
    }

    public void AddScore(int amount)
    {
        _score += amount;
    }
}
