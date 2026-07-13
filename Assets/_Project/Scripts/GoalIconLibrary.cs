using System;
using UnityEngine;

[CreateAssetMenu(fileName = "GoalIconLibrary", menuName = "BlockBoost/Goal Icon Library")]
public class GoalIconLibrary : ScriptableObject
{
    [Serializable] public class FishIcon        { public FishType type;        public Sprite sprite; }
    [Serializable] public class ObstacleIcon    { public ObstacleType type;    public Sprite sprite; }
    [Serializable] public class CollectibleIcon { public CollectibleType type; public Sprite sprite; }

    public FishIcon[] fishIcons;
    public ObstacleIcon[] obstacleIcons;
    public CollectibleIcon[] collectibleIcons;
    public Sprite netIcon;

    public Sprite GetIcon(LevelGoal goal)
    {
        switch (goal.goalType)
        {
            case GoalType.CollectFish:
                foreach (var f in fishIcons)
                    if (f.type == goal.targetFish) return f.sprite;
                break;

            case GoalType.ClearObstacle:
                foreach (var o in obstacleIcons)
                    if (o.type == goal.targetObstacle) return o.sprite;
                break;

            case GoalType.DeliverCollectible:
                foreach (var c in collectibleIcons)
                    if (c.type == goal.targetCollectible) return c.sprite;
                break;

            case GoalType.ClearNet:
                return netIcon;
        }
        return null;
    }
}