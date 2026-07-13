using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class LevelButtonUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image backgroundImage;     // butonun kendi Image'ı
    [SerializeField] private TMP_Text numberText;
    [SerializeField] private Image[] starImages;        // 3 yıldız
    [SerializeField] private Sprite starFilled;
    [SerializeField] private Sprite starEmpty;
    [SerializeField] private GameObject lockIcon;
    [SerializeField] private Button button;

    [Header("State Sprites")]
    [SerializeField] private Sprite lockedSprite;       // level_locked
    [SerializeField] private Sprite activeSprite;       // level_active
    [SerializeField] private Sprite doneSprite;         // level_done

    private int levelIndex;
    private Action<int> onClick;

    public void Setup(int index, int levelNumber, int starsEarned, bool unlocked, Action<int> onClickCallback)
    {
        levelIndex = index;
        onClick = onClickCallback;

        if (numberText)
        {
            numberText.text = levelNumber.ToString();
            numberText.gameObject.SetActive(unlocked);
        }

        // Durum belirle
        bool completed = unlocked && starsEarned > 0;

        // Arka plan sprite'ı
        if (backgroundImage == null) backgroundImage = GetComponent<Image>();
        if (backgroundImage != null)
        {
            backgroundImage.color = Color.white;   // tint'i sıfırla, sprite kendi rengini göstersin

            if (!unlocked)      backgroundImage.sprite = lockedSprite;
            else if (completed) backgroundImage.sprite = doneSprite;
            else                backgroundImage.sprite = activeSprite;
        }

        // Yıldızlar (sadece tamamlanmışsa göster)
        if (starImages != null)
        {
            for (int i = 0; i < starImages.Length; i++)
            {
                starImages[i].gameObject.SetActive(completed);
                starImages[i].sprite = (i < starsEarned) ? starFilled : starEmpty;
            }
        }

        // Kilit
        if (lockIcon) lockIcon.SetActive(!unlocked);

        // Buton
        if (button)
        {
            button.interactable = unlocked;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(levelIndex));
        }
    }
}