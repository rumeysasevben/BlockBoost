using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class LevelUI : MonoBehaviour
{
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text movesText;

    [Header("Moves Warning")]
    [Tooltip("Bu sayı ve altında hamle kalınca kırmızı olur")]
    [SerializeField] private int criticalMoves = 5;
    [SerializeField] private Color normalMovesColor = Color.white;
    [SerializeField] private Color criticalMovesColor = new Color(0.9f, 0.2f, 0.2f);

    [Header("Goals")]
    [SerializeField] private Transform goalContainer;
    [SerializeField] private GameObject goalItemPrefab;
    [SerializeField] private GoalIconLibrary iconLibrary;

    private readonly List<GoalItemUI> goalItems = new List<GoalItemUI>();

    private void Start()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelLoaded += OnLevelLoaded;
            LevelManager.Instance.OnMovesChanged += OnMovesChanged;
            LevelManager.Instance.OnGoalProgress += OnGoalProgress;
        }
    }

    private void OnDestroy()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelLoaded -= OnLevelLoaded;
            LevelManager.Instance.OnMovesChanged -= OnMovesChanged;
            LevelManager.Instance.OnGoalProgress -= OnGoalProgress;
        }
    }

    private void OnLevelLoaded(LevelData level)
    {
        if (levelText) levelText.text = level.levelName.ToUpper();
        if (movesText) movesText.color = normalMovesColor;
        BuildGoalItems(level);
    }

    private void OnMovesChanged(int m)
    {
        if (!movesText) return;

        movesText.text = $"Moves: {m}";

        if (m <= criticalMoves)
        {
            // kritik: kırmızı + zıpla
            movesText.color = criticalMovesColor;
            movesText.transform.DOKill();
            movesText.transform.localScale = Vector3.one;
            movesText.transform.DOPunchScale(Vector3.one * 0.25f, 0.3f, 8, 0.7f)
                     .SetUpdate(true);
        }
        else
        {
            movesText.color = normalMovesColor;
        }
    }

    private void OnGoalProgress(LevelGoal g) => RefreshGoals();

    private void BuildGoalItems(LevelData level)
    {
        if (goalContainer == null || goalItemPrefab == null) return;

        foreach (Transform child in goalContainer)
            Destroy(child.gameObject);
        goalItems.Clear();

        if (level.collectGoals == null) return;

        foreach (var goal in level.collectGoals)
        {
            GameObject obj = Instantiate(goalItemPrefab, goalContainer);
            var item = obj.GetComponent<GoalItemUI>();
            if (item == null) continue;

            Sprite icon = iconLibrary != null ? iconLibrary.GetIcon(goal) : null;
            item.Setup(icon, goal.currentCount, goal.targetCount);
            goalItems.Add(item);
        }
    }

    private void RefreshGoals()
    {
        var level = LevelManager.Instance?.CurrentLevel;
        if (level == null || level.collectGoals == null) return;

        int count = Mathf.Min(goalItems.Count, level.collectGoals.Count);
        for (int i = 0; i < count; i++)
        {
            var g = level.collectGoals[i];
            goalItems[i].Refresh(g.currentCount, g.targetCount);
        }
    }
}