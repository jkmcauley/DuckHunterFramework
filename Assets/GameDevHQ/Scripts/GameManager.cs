using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum GameState
{
    MainMenu,
    Playing,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (scene == "GameScene")
            CurrentState = GameState.Playing;
        else
            CurrentState = GameState.MainMenu;

        Time.timeScale = 1f;
    }

    void Update()
    {
        if (CurrentState != GameState.GameOver)
            return;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            RestartGame();
    }

    public void StartGame()
    {
        CurrentState = GameState.Playing;
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameScene");
    }

    public void LoadMainMenu()
    {
        CurrentState = GameState.MainMenu;
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void GameOver()
    {
        if (CurrentState == GameState.GameOver)
            return;

        CurrentState = GameState.GameOver;
        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        CurrentState = GameState.Playing;
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameScene");
    }
}
