using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using System.Collections;

public class TimerUI : MonoBehaviour
{
    [Header("UI Toolkit")]
    public UIDocument timerUI;
    public UIDocument gameoverUI;

    [Header("Input System")]
    public InputActionAsset inputActions;

    private InputAction retryAction;

    private Label scoreValueLabel;
    private Label highScoreValueLabel;
    private Label timerValueLabel;

    private VisualElement timerRoot;
    private VisualElement gameoverRoot;

    private int score = 0;
    private float timeRemaining = 30f;
    private bool timerRunning = true;
    private bool gameEnded = false;

    void Awake()
    {
        var map = inputActions.FindActionMap("GamePlay");
        if (map != null)
        {
            retryAction = map.FindAction("Retry");
            if (retryAction != null)
            {
                retryAction.performed += ctx => OnRetry();
            }
            else
            {
                Debug.LogError("Retry アクションが見つかりません。");
            }
        }
        else
        {
            Debug.LogError("GamePlay アクションマップが見つかりません。");
        }
    }

    void OnEnable()
    {
        retryAction?.Enable();
    }

    void OnDisable()
    {
        retryAction?.Disable();
    }

    void Start()
    {
        timerRoot = timerUI.rootVisualElement;
        scoreValueLabel = timerRoot.Q<Label>("ScoreValue");
        highScoreValueLabel = timerRoot.Q<Label>("HighScoreValue");
        timerValueLabel = timerRoot.Q<Label>("TimerValue");

        LoadHighScore();
        UpdateScoreText();
        UpdateTimerText();
    }

    void Update()
    {
        if (timerRunning)
        {
            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                timerRunning = false;
                EndGame();
            }
            UpdateTimerText();
        }
    }

    private void OnRetry()
    {
        if (!gameEnded) return;

        // Reset game state
        score = 0;
        timeRemaining = 30f;
        timerRunning = true;
        gameEnded = false;

        UpdateScoreText();
        UpdateTimerText();

        // Hide GameOver UI
        if (gameoverRoot == null)
            gameoverRoot = gameoverUI.rootVisualElement;

        if (gameoverRoot != null)
            gameoverRoot.style.display = DisplayStyle.None;
    }

    public void AddScore(int amount)
    {
        if (gameEnded) return;

        score += amount;
        UpdateScoreText();
        UpdateHighScoreIfNeeded();
        StartCoroutine(AnimateScoreValue());
    }

    public void AdvanceTime(float seconds)
    {
        if (gameEnded) return;

        timeRemaining -= seconds;
        if (timeRemaining < 0f)
        {
            timeRemaining = 0f;
            timerRunning = false;
            EndGame();
        }
        UpdateTimerText();
    }

    private void UpdateScoreText()
    {
        if (scoreValueLabel != null)
        {
            scoreValueLabel.text = score.ToString();
        }
    }

    private void LoadHighScore()
    {
        int highScore = PlayerPrefs.GetInt("HighScore", 0);
        if (highScoreValueLabel != null)
        {
            highScoreValueLabel.text = highScore.ToString();
        }
    }

    private void UpdateHighScoreIfNeeded()
    {
        int currentHigh = PlayerPrefs.GetInt("HighScore", 0);
        if (score > currentHigh)
        {
            PlayerPrefs.SetInt("HighScore", score);
            PlayerPrefs.Save();
            if (highScoreValueLabel != null)
            {
                highScoreValueLabel.text = score.ToString();
                StartCoroutine(AnimateHighScoreValue());
            }
        }
    }

    private void UpdateTimerText()
    {
        if (timerValueLabel != null)
        {
            int displayTime = Mathf.CeilToInt(timeRemaining);
            timerValueLabel.text = displayTime.ToString();

            if (timeRemaining <= 10f)
            {
                float t = Mathf.InverseLerp(10f, 0f, timeRemaining);
                Color interpolatedColor = Color.Lerp(Color.white, Color.red, t);
                timerValueLabel.style.color = new StyleColor(interpolatedColor);
            }
            else
            {
                timerValueLabel.style.color = new StyleColor(Color.white);
            }
        }
    }

    private IEnumerator AnimateScoreValue()
    {
        scoreValueLabel.style.scale = new Scale(new Vector3(1.4f, 1.4f, 1f));
        yield return new WaitForSeconds(0.1f);
        scoreValueLabel.style.scale = new Scale(Vector3.one);
    }

    private IEnumerator AnimateHighScoreValue()
    {
        highScoreValueLabel.style.scale = new Scale(new Vector3(1.4f, 1.4f, 1f));
        yield return new WaitForSeconds(0.1f);
        highScoreValueLabel.style.scale = new Scale(Vector3.one);
    }

    private void EndGame()
    {
        gameEnded = true;

        PlayerPrefs.SetInt("FinalScore", score);
        PlayerPrefs.Save();

        ShowGameOverUI();
    }

    private void ShowGameOverUI()
    {
        gameoverRoot = gameoverUI.rootVisualElement;
        if (gameoverRoot == null)
        {
            Debug.LogError("GameOverUI not found in UI.");
            return;
        }

        gameoverRoot.style.display = DisplayStyle.Flex;

        var highScoreLabel = gameoverRoot.Q<Label>("HighScoreLabel");
        var finalScoreLabel = gameoverRoot.Q<Label>("FinalScoreLabel");
        var retryLabel = gameoverRoot.Q<Label>("RetryLabel");

        if (highScoreLabel != null)
            highScoreLabel.text = $"High Score : {PlayerPrefs.GetInt("HighScore", 0)}";

        if (finalScoreLabel != null)
            finalScoreLabel.text = $"Your Score : {score}";

        if (retryLabel != null)
            retryLabel.text = "Press Space to retry!";
    }
}
