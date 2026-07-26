using System.Threading;
using TMPro;
using UnityEditor.Build.Content;
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

    private void Update()

    {

        UpdateTimer();

        UpdateUI();

    }

    public void UpdateTimer()

    {

        if (_timer > 0f)
        {
            _timer -= Time.deltaTime;
        }


        if (_timer <= 0)
        {
            // GameManagerDependencyInfo>Instance.Gameover();
            Debug.Log("Gameover");
        }

    }

    public void UpdateUI()

    {

        _scoreText.text = $"Score: {_score}";

        _timerText.text = $"Time: {Mathf.CeilToInt(_timer)}";

        _accuracyText.text =

            $"Hits: {SpawnManager.Instance.HitPercentage:F1}%";

    }

    public void AddScore(int amount)

    {

        _score += amount;

    }

}
