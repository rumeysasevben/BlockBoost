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
    public List<CollectiblePlacement> collectibles = new List<CollectiblePlacement>();

    [Header("Collectibles (Random)")]
    public List<RandomCollectibleSpec> randomCollectibles = new List<RandomCollectibleSpec>();

    [Header("Fishing Nets (Sabit)")]
    [Tooltip("Sabit konumdaki balık ağları (üzerindeki balık swap edilemez)")]
    public List<NetPlacement> nets = new List<NetPlacement>();

    [Header("Fishing Nets (Random)")]
    [Tooltip("Random konumda spawn olacak balık ağları")]
    public int randomNetCount = 0;

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

[System.Serializable]
public class NetPlacement
{
    public int gridX;
    public int gridY;
}