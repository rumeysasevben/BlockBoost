using UnityEngine;
using TMPro;
using DG.Tweening;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;

    [Header("Count Animation")]
    [Tooltip("Sayının hedefe ulaşma süresi (saniye)")]
    [SerializeField] private float countDuration = 0.4f;
    [Tooltip("Her artışta yazının hafif zıplaması")]
    [SerializeField] private bool punchOnChange = true;
    [SerializeField] private float punchScale = 0.15f;

    private int displayedScore = 0;   // ekranda o an gösterilen değer
    private Tween countTween;

    private void Start()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += UpdateUI;
            // baslangicta animasyonsuz set et
            displayedScore = ScoreManager.Instance.CurrentScore;
            SetText(displayedScore);
        }
    }

    private void OnDestroy()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreChanged -= UpdateUI;

        countTween?.Kill();
    }

    private void UpdateUI(int targetScore)
    {
        if (scoreText == null) return;

        // onceki sayma animasyonunu durdur, kaldigi yerden devam etsin
        countTween?.Kill();

        countTween = DOTween.To(
            () => displayedScore,
            x => { displayedScore = x; SetText(x); },
            targetScore,
            countDuration
        ).SetEase(Ease.OutQuad);

        if (punchOnChange)
        {
            scoreText.transform.DOKill();
            scoreText.transform.localScale = Vector3.one;
            scoreText.transform.DOPunchScale(Vector3.one * punchScale, countDuration, 6, 0.6f);
        }
    }

    private void SetText(int score)
    {
        scoreText.text = $"{score:N0}";
    }
}