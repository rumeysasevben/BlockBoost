using System;
using UnityEngine;

[Serializable]
public class LevelGoal
{
    [Tooltip("Hangi balık toplanacak")]
    public FishType targetFish;

    [Tooltip("Kaç tane toplanacak")]
    public int targetCount = 15;

    [HideInInspector] public int currentCount;

    public bool IsComplete => currentCount >= targetCount;

    public void AddProgress(int amount = 1)
    {
        currentCount = Mathf.Min(currentCount + amount, targetCount);
    }

    public void Reset() => currentCount = 0;
}