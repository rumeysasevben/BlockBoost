using System;
using UnityEngine;

public enum GoalType
{
    CollectFish,
    ClearObstacle,
    DeliverCollectible,
    ClearNet
}

[Serializable]
public class LevelGoal
{
    [Tooltip("Goal tipi")]
    public GoalType goalType = GoalType.CollectFish;

    [Tooltip("CollectFish için: hangi balık")]
    public FishType targetFish;

    [Tooltip("ClearObstacle için: hangi obstacle")]
    public ObstacleType targetObstacle;

    [Tooltip("DeliverCollectible için: hangi collectible")]
    public CollectibleType targetCollectible;

    [Tooltip("Toplam hedef sayı")]
    public int targetCount = 15;

    [HideInInspector] public int currentCount;

    public bool IsComplete => currentCount >= targetCount;

    public void AddProgress(int amount = 1)
    {
        currentCount = Mathf.Min(currentCount + amount, targetCount);
    }

    public void Reset() => currentCount = 0;
}