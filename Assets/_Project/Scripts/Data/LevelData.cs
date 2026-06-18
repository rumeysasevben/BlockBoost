using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Level_", menuName = "BlockBoost/Level Data", order = 1)]
public class LevelData : ScriptableObject
{
    [Header("Identity")]
    public int levelNumber = 1;
    public string levelName = "Level 1";

    [Header("Rules")]
    public int moveLimit = 25;
    public int targetScore = 1000;

    [Header("Star Thresholds")]
    public int twoStarScore = 2000;
    public int threeStarScore = 3500;

    [Header("Grid")]
    [Tooltip("Bu level için grid genişliği")]
    public int gridWidth = 8;
    [Tooltip("Bu level için grid yüksekliği")]
    public int gridHeight = 8;

    [Header("Fish Pool")]
    [Tooltip("Bu level'da kullanılacak balıklar. Boşsa GridManager'ın default pool'u kullanılır.")]
    public FishData[] levelFishPool;

    [Header("Goals")]
    public List<LevelGoal> collectGoals = new List<LevelGoal>();

    [Header("Visual (opsiyonel, sonra)")]
    public Color backgroundTint = Color.white;
}