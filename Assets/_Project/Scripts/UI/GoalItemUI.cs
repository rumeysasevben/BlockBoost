using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GoalItemUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text countText;

    public void Setup(Sprite iconSprite, int current, int target)
    {
        if (icon)
        {
            icon.sprite = iconSprite;
            icon.enabled = iconSprite != null;
        }
        Refresh(current, target);
    }

    public void Refresh(int current, int target)
    {
        if (!countText) return;

        bool complete = current >= target;
        countText.text = complete ? "OK" : $"{current}/{target}";
        countText.color = complete ? new Color(0.2f, 1f, 0.4f) : Color.white;
    }
}