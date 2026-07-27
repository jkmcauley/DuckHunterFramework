using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD")]
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private TMP_Text _timerText;
    [SerializeField] private TMP_Text _accuracyText;

    [Header("End Screen")]
    [SerializeField] private TMP_Text _restartText;
    [SerializeField] private TMP_Text _gameOverText;
    [SerializeField] private TMP_Text _winText;
    [SerializeField] private float _winHitPercent = 90f;
    [SerializeField] private float _startDelay = 2f;

    private int _score;
    private float _timer = 90f;
    private float _delayLeft;
    private bool _ended;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _delayLeft = _startDelay;
        HideEndScreen();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (_ended)
            return;

        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            return;

        // Brief pause after load / restart before timer ticks
        if (_delayLeft > 0f)
        {
            _delayLeft -= Time.deltaTime;
            UpdateUI();
            return;
        }

        UpdateTimer();
        UpdateUI();
    }

    void UpdateTimer()
    {
        if (_timer > 0f)
            _timer -= Time.deltaTime;

        if (_timer > 0f)
            return;

        // Time up: win only if hits >= 90%, otherwise game over
        bool won = SpawnManager.Instance != null &&
                   SpawnManager.Instance.HitPercentage >= _winHitPercent;
        EndGame(won);
    }

    void EndGame(bool won)
    {
        if (_ended)
            return;

        _ended = true;

        if (_gameOverText != null)
            _gameOverText.gameObject.SetActive(true);

        if (_winText != null)
            _winText.gameObject.SetActive(won);

        if (_restartText != null)
            _restartText.gameObject.SetActive(true);

        if (GameManager.Instance != null)
            GameManager.Instance.GameOver();
    }

    void HideEndScreen()
    {
        if (_gameOverText != null)
            _gameOverText.gameObject.SetActive(false);
        if (_winText != null)
            _winText.gameObject.SetActive(false);
        if (_restartText != null)
            _restartText.gameObject.SetActive(false);
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
