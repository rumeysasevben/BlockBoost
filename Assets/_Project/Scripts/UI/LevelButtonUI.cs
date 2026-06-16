using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class LevelButtonUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text numberText;
    [SerializeField] private Image[] starImages;        // 3 yıldız
    [SerializeField] private Sprite starFilled;
    [SerializeField] private Sprite starEmpty;
    [SerializeField] private GameObject lockIcon;
    [SerializeField] private Button button;

    private int levelIndex;       // allLevels[] içindeki index
    private Action<int> onClick;

    public void Setup(int index, int levelNumber, int starsEarned, bool unlocked, Action<int> onClickCallback)
    {
        levelIndex = index;
        onClick = onClickCallback;

        if (numberText) numberText.text = levelNumber.ToString();

        // Yıldızlar
        if (starImages != null)
        {
            for (int i = 0; i < starImages.Length; i++)
                starImages[i].sprite = (i < starsEarned) ? starFilled : starEmpty;
        }

        // Kilit
        if (lockIcon) lockIcon.SetActive(!unlocked);

        // Buton tıklanır mı?
        if (button)
        {
            button.interactable = unlocked;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(levelIndex));
        }

        // Kilitli olanları soldurabiliriz (opsiyonel)
        var img = GetComponent<Image>();
        if (img != null)
            img.color = unlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
    }
}