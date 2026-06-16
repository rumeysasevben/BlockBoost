using UnityEngine;
using UnityEngine.UI;

public class LevelSelectUI : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private Transform buttonsContainer;
    [SerializeField] private GameObject levelButtonPrefab;

    [Header("Navigation")]
    [SerializeField] private Button backButton;

    private void Awake()
    {
        if (backButton) backButton.onClick.AddListener(OnBack);
    }

    private void OnEnable()
    {
        // Her açıldığında butonları yeniden çiz (save güncellenmiş olabilir)
        RefreshButtons();
    }

    private void RefreshButtons()
    {
        // Önce eski butonları temizle
        foreach (Transform child in buttonsContainer)
            Destroy(child.gameObject);

        var allLevels = LevelManager.Instance?.allLevels;
        if (allLevels == null || allLevels.Length == 0)
        {
            Debug.LogWarning("LevelSelectUI: allLevels bulunamadı");
            return;
        }

        for (int i = 0; i < allLevels.Length; i++)
        {
            var level = allLevels[i];
            int index = i;  // closure için

            GameObject buttonObj = Instantiate(levelButtonPrefab, buttonsContainer);
            var btnUI = buttonObj.GetComponent<LevelButtonUI>();

            int stars = SaveManager.Instance != null
                ? SaveManager.Instance.GetStars(level.levelNumber)
                : 0;

            bool unlocked = SaveManager.Instance != null
                ? SaveManager.Instance.IsLevelUnlocked(level.levelNumber)
                : (i == 0);  // Save yoksa sadece ilk seviye açık

            btnUI.Setup(index, level.levelNumber, stars, unlocked, OnLevelClicked);
        }
    }

    private void OnLevelClicked(int levelIndex)
    {
        Debug.Log($"<color=cyan>[LevelSelect] Level {levelIndex + 1} seçildi</color>");
        GameFlowManager.Instance.GoToGameplay();
        LevelManager.Instance.LoadLevel(levelIndex);
    }

    private void OnBack()
    {
        GameFlowManager.Instance.GoToMainMenu();
    }
}