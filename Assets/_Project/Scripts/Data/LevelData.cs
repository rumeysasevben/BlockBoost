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
    public int gridWidth = 8;
    public int gridHeight = 8;

    [Header("Fish Pool")]
    public FishData[] levelFishPool;

    [Header("Goals")]
    public List<LevelGoal> collectGoals = new List<LevelGoal>();

    [Header("Obstacles (Sabit)")]
    public List<ObstaclePlacement> obstacles = new List<ObstaclePlacement>();

    [Header("Obstacles (Random)")]
    public List<RandomObstacleSpec> randomObstacles = new List<RandomObstacleSpec>();

    [Header("Collectibles (Sabit)")]
    [Tooltip("Sabit konuma yerleştirilecek collectible'lar")]
    public List<CollectiblePlacement> collectibles = new List<CollectiblePlacement>();

    [Header("Collectibles (Random)")]
    [Tooltip("Random konuma yerleştirilecek collectible'lar (üst yarıda spawn olur)")]
    public List<RandomCollectibleSpec> randomCollectibles = new List<RandomCollectibleSpec>();

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
    public int count = 1;
}

[System.Serializable]
public class CollectiblePlacement
{
    public int gridX;
    public int gridY;
    public CollectibleType type;
}

[System.Serializable]
public class RandomCollectibleSpec
{
    public CollectibleType type;
    public int count = 1;
}