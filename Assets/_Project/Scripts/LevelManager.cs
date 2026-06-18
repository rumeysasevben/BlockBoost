using System;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Levels")]
    public LevelData[] allLevels;
    public int startLevelIndex = 0;

    [Header("Runtime State")]
    public LevelData CurrentLevel { get; private set; }
    public int MovesRemaining { get; private set; }
    public bool IsLevelActive { get; private set; }

    public event Action<LevelData> OnLevelLoaded;
    public event Action<int> OnMovesChanged;
    public event Action<LevelGoal> OnGoalProgress;
    public event Action<int> OnLevelWon;
    public event Action OnLevelLost;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        OnMovesChanged += LogMoves;
        OnLevelWon     += LogWon;
        OnLevelLost    += LogLost;
        OnGoalProgress += LogGoal;
    }

    private void OnDisable()
    {
        OnMovesChanged -= LogMoves;
        OnLevelWon     -= LogWon;
        OnLevelLost    -= LogLost;
        OnGoalProgress -= LogGoal;
    }

    private void LogMoves(int m) => Debug.Log($"[Level] Moves: {m}");
    private void LogWon(int s)   => Debug.Log($"<color=lime>[Level] WON! Stars: {s}</color>");
    private void LogLost()       => Debug.Log("<color=red>[Level] LOST!</color>");
    private void LogGoal(LevelGoal g)
    {
        string label;
        switch (g.goalType)
        {
            case GoalType.CollectFish:        label = g.targetFish.ToString(); break;
            case GoalType.ClearObstacle:      label = g.targetObstacle.ToString(); break;
            case GoalType.DeliverCollectible: label = g.targetCollectible.ToString(); break;
            case GoalType.ClearNet:           label = "Net"; break;
            default: label = "?"; break;
        }
        Debug.Log($"[Goal] {label}: {g.currentCount}/{g.targetCount}");
    }

    public void LoadLevel(int index)
    {
        if (index < 0 || index >= allLevels.Length) return;

        CurrentLevel = allLevels[index];
        GridManager.Instance?.ResetForLevel(CurrentLevel);
        MovesRemaining = CurrentLevel.moveLimit;
        IsLevelActive = true;

        foreach (var g in CurrentLevel.collectGoals) g.Reset();
        ScoreManager.Instance.ResetScore();

        OnLevelLoaded?.Invoke(CurrentLevel);
        OnMovesChanged?.Invoke(MovesRemaining);
    }

    public void UseMove()
    {
        if (!IsLevelActive) return;
        MovesRemaining--;
        OnMovesChanged?.Invoke(MovesRemaining);
        if (MovesRemaining <= 0) EndLevel();
    }

    public void ReportFishCollected(FishType fish, int amount = 1)
    {
        if (!IsLevelActive || CurrentLevel == null) return;
        foreach (var g in CurrentLevel.collectGoals)
            if (g.goalType == GoalType.CollectFish && g.targetFish == fish && !g.IsComplete)
            {
                g.AddProgress(amount);
                OnGoalProgress?.Invoke(g);
            }
        if (AllGoalsComplete()) EndLevel();
    }

    public void ReportObstacleCleared(ObstacleType obstacle, int amount = 1)
    {
        if (!IsLevelActive || CurrentLevel == null) return;
        foreach (var g in CurrentLevel.collectGoals)
            if (g.goalType == GoalType.ClearObstacle && g.targetObstacle == obstacle && !g.IsComplete)
            {
                g.AddProgress(amount);
                OnGoalProgress?.Invoke(g);
            }
        if (AllGoalsComplete()) EndLevel();
    }

    public void ReportCollectibleDelivered(CollectibleType collectible, int amount = 1)
    {
        if (!IsLevelActive || CurrentLevel == null) return;
        foreach (var g in CurrentLevel.collectGoals)
            if (g.goalType == GoalType.DeliverCollectible && g.targetCollectible == collectible && !g.IsComplete)
            {
                g.AddProgress(amount);
                OnGoalProgress?.Invoke(g);
            }
        if (AllGoalsComplete()) EndLevel();
    }

    public void ReportNetCleared(int amount = 1)
    {
        if (!IsLevelActive || CurrentLevel == null) return;
        foreach (var g in CurrentLevel.collectGoals)
            if (g.goalType == GoalType.ClearNet && !g.IsComplete)
            {
                g.AddProgress(amount);
                OnGoalProgress?.Invoke(g);
            }
        if (AllGoalsComplete()) EndLevel();
    }

    private bool AllGoalsComplete()
    {
        if (CurrentLevel == null) return false;
        if (CurrentLevel.collectGoals == null || CurrentLevel.collectGoals.Count == 0) return false;
        foreach (var g in CurrentLevel.collectGoals)
            if (!g.IsComplete) return false;
        return true;
    }

    private void EndLevel()
    {
        IsLevelActive = false;
        int score = ScoreManager.Instance.CurrentScore;
        if (AllGoalsComplete())
        {
            int stars = CalculateStars(score);
            SaveManager.Instance?.SaveLevelResult(CurrentLevel.levelNumber, stars);
            OnLevelWon?.Invoke(stars);
        }
        else OnLevelLost?.Invoke();
    }

    private int CalculateStars(int score)
    {
        if (score >= CurrentLevel.threeStarScore) return 3;
        if (score >= CurrentLevel.twoStarScore) return 2;
        if (score >= CurrentLevel.targetScore) return 1;
        return 0;
    }

    public void LoadNextLevel()
    {
        int currentIndex = Array.IndexOf(allLevels, CurrentLevel);
        if (currentIndex + 1 < allLevels.Length) LoadLevel(currentIndex + 1);
    }

    public void RestartLevel()
    {
        int currentIndex = Array.IndexOf(allLevels, CurrentLevel);
        LoadLevel(currentIndex);
    }
}