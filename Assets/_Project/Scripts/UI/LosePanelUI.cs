using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class LosePanelUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private RectTransform card;

    [Header("Texts")]
    [SerializeField] private TMP_Text scoreText;

    [Header("Buttons")]
    [SerializeField] private Button restartButton;

    private void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (restartButton != null) restartButton.onClick.AddListener(OnRestart);
    }

    private void Start()
    {
        if (LevelManager.Instance != null)
            LevelManager.Instance.OnLevelLost += Show;
    }

    private void OnDestroy()
    {
        if (LevelManager.Instance != null)
            LevelManager.Instance.OnLevelLost -= Show;
    }

    public void Show()
    {
        Debug.Log("<color=red>>>> LosePanel.Show() çağrıldı!</color>");

        if (panelRoot == null) return;

        panelRoot.SetActive(true);

        int finalScore = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
        if (scoreText != null) scoreText.text = $"Score: {finalScore:N0}";

        if (card != null)
        {
            card.localScale = Vector3.zero;
            card.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
        }
    }

    private void OnRestart()
    {
        panelRoot.SetActive(false);
        LevelManager.Instance.RestartLevel();
    }
}