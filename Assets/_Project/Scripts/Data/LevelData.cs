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
    public FishData[] levelFishPool = new FishData[0];

    [Header("Goals")]
    public List<LevelGoal> collectGoals = new List<LevelGoal>();

    [Header("Obstacles (Sabit Konum)")]
    [Tooltip("Bu level'da grid'e yerleştirilecek sabit konumlu engeller")]
    public List<ObstaclePlacement> obstacles = new List<ObstaclePlacement>();

    [Header("Obstacles (Random)")]
    [Tooltip("Random pozisyonlara eklenecek obstacle'lar (tip + adet)")]
    public List<RandomObstacleSpec> randomObstacles = new List<RandomObstacleSpec>();

    [Header("Visual")]
    public Color backgroundTint = Color.white;
}

[System.Serializable]
public class ObstaclePlacement
{
    public int gridX;
    public int gridY;
    public ObstacleType type;
}

[System.Serializable]
public class RandomObstacleSpec
{
    public ObstacleType type;
    [Tooltip("Bu tipten kaç tane random spawn olsun")]
    public int count = 1;
}