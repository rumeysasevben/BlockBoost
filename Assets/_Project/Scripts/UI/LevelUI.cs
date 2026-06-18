using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelUI : MonoBehaviour
{
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text movesText;
    [SerializeField] private TMP_Text goalText;

    [Header("HUD Stars")]
    [SerializeField] private Image[] hudStars;
    [SerializeField] private Sprite starFilled;
    [SerializeField] private Sprite starEmpty;

    private void Start()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelLoaded += OnLevelLoaded;
            LevelManager.Instance.OnMovesChanged += OnMovesChanged;
            LevelManager.Instance.OnGoalProgress += OnGoalProgress;
        }
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreChanged += OnScoreChanged;
    }

    private void OnDestroy()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelLoaded -= OnLevelLoaded;
            LevelManager.Instance.OnMovesChanged -= OnMovesChanged;
            LevelManager.Instance.OnGoalProgress -= OnGoalProgress;
        }
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreChanged -= OnScoreChanged;
    }

    private void OnLevelLoaded(LevelData level)
    {
        if (levelText) levelText.text = level.levelName.ToUpper();
        RefreshGoalDisplay();
        RefreshStars(0);
    }

    private void OnMovesChanged(int m) { if (movesText) movesText.text = $"Moves: {m}"; }
    private void OnGoalProgress(LevelGoal g) => RefreshGoalDisplay();
    private void OnScoreChanged(int s) => RefreshStars(s);

    private void RefreshGoalDisplay()
    {
        var level = LevelManager.Instance?.CurrentLevel;
        if (level == null || goalText == null) return;
        if (level.collectGoals == null || level.collectGoals.Count == 0) { goalText.text = ""; return; }

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < level.collectGoals.Count; i++)
        {
            var g = level.collectGoals[i];
            string label;
            switch (g.goalType)
            {
                case GoalType.CollectFish:        label = g.targetFish.ToString(); break;
                case GoalType.ClearObstacle:      label = g.targetObstacle.ToString(); break;
                case GoalType.DeliverCollectible: label = g.targetCollectible.ToString(); break;
                case GoalType.ClearNet:           label = "Net"; break;
                default: label = "?"; break;
            }
            string status = g.IsComplete ? "<color=#33FF66>OK</color>" : $"{g.currentCount}/{g.targetCount}";
            sb.Append($"{label}: {status}");
            if (i < level.collectGoals.Count - 1) sb.Append("   ");
        }
        goalText.text = sb.ToString();
    }

    private void RefreshStars(int score)
    {
        var level = LevelManager.Instance?.CurrentLevel;
        if (level == null || hudStars == null || hudStars.Length == 0) return;

        int stars = 0;
        if (score >= level.threeStarScore) stars = 3;
        else if (score >= level.twoStarScore) stars = 2;
        else if (score >= level.targetScore) stars = 1;

        for (int i = 0; i < hudStars.Length; i++)
            hudStars[i].sprite = (i < stars) ? starFilled : starEmpty;
    }
}