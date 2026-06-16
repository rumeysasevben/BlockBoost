using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class WinPanelUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;       // WinPanel kendisi
    [SerializeField] private RectTransform card;         // Card (pop animation için)

    [Header("Stars")]
    [SerializeField] private Image[] starImages;         // 3 yıldız sırayla
    [SerializeField] private Sprite starFilled;
    [SerializeField] private Sprite starEmpty;

    [Header("Texts")]
    [SerializeField] private TMP_Text scoreText;

    [Header("Buttons")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button restartButton;

    [Header("Audio (opsiyonel)")]
    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip starPopSound;

    private void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (nextButton    != null) nextButton.onClick.AddListener(OnNext);
        if (restartButton != null) restartButton.onClick.AddListener(OnRestart);
    }

    private void Start()
    {
        if (LevelManager.Instance != null)
            LevelManager.Instance.OnLevelWon += Show;
    }

    private void OnDestroy()
    {
        if (LevelManager.Instance != null)
            LevelManager.Instance.OnLevelWon -= Show;
    }

    public void Show(int stars)
    {
        Debug.Log($"<color=cyan>>>> WinPanel.Show() çağrıldı! Stars: {stars}</color>");

        if (panelRoot == null) return;

        panelRoot.SetActive(true);

        // Skoru göster
        int finalScore = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
        if (scoreText != null) scoreText.text = $"Score: {finalScore:N0}";

        // Yıldızları başta hepsini boş yap
        for (int i = 0; i < starImages.Length; i++)
        {
            starImages[i].sprite = starEmpty;
            starImages[i].transform.localScale = Vector3.one;
        }

        // Card pop animation
        if (card != null)
        {
            card.localScale = Vector3.zero;
            card.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
        }

        // Yıldız pop animasyonu (gecikmeli)
        StartCoroutine(AnimateStars(stars));

        // Win sesi
        //if (winSound != null && AudioManager.Instance != null)
        //    AudioManager.Instance.PlaySFX(winSound);
    }

    private IEnumerator AnimateStars(int starsEarned)
    {
        yield return new WaitForSeconds(0.4f); // Card pop bitsin

        for (int i = 0; i < starsEarned && i < starImages.Length; i++)
        {
            var s = starImages[i];
            s.sprite = starFilled;
            s.transform.localScale = Vector3.zero;
            s.transform.DOScale(1.3f, 0.25f).SetEase(Ease.OutBack)
                .OnComplete(() => s.transform.DOScale(1f, 0.15f));

            //if (starPopSound != null && AudioManager.Instance != null)
            //    AudioManager.Instance.PlaySFX(starPopSound);

            yield return new WaitForSeconds(0.3f);
        }
    }

    private void OnNext()
    {
        panelRoot.SetActive(false);
        LevelManager.Instance.LoadNextLevel();
    }

    private void OnRestart()
    {
        panelRoot.SetActive(false);
        LevelManager.Instance.RestartLevel();
    }
}