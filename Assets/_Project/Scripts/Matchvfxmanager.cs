using UnityEngine;
using DG.Tweening;

public class MatchVFXManager : MonoBehaviour
{
    public static MatchVFXManager Instance { get; private set; }

    [Header("Prefabs")]
    [Tooltip("ParticleSystem prefab - match patlamasinda spawn olur")]
    public GameObject particleBurstPrefab;
    [Tooltip("ScorePopup prefab - TMP text ile +50 gibi yazi ucar")]
    public GameObject scorePopupPrefab;

    [Header("Flash Efekti (feedback yazilari icin)")]
    [Tooltip("Yumusak radial glow sprite'i")]
    public Sprite flashSprite;
    [Tooltip("Flash'in maksimum buyuklugu (dunya birimi)")]
    public float flashMaxSize = 4f;
    [Tooltip("Flash'in toplam suresi — yazi suresine yakin olmali")]
    public float flashDuration = 1.2f;
    [Range(0f, 1f)] public float flashPeakAlpha = 0.55f;
    [Tooltip("Flash en parlak halinde ne kadar beklesin")]
    public float flashHoldTime = 0.35f;
    [Tooltip("Flash tile'larin ustunde mi altinda mi (yazidan dusuk olmali)")]
    public int flashSortingOrder = 40;

    [Header("Particle Efekti (feedback yazilari icin)")]
    [Range(1, 8)] public int burstCountBehindText = 4;
    public float burstSpreadRadius = 0.8f;

    [Header("Combo Text Renkleri")]
    public Color comboColor      = new Color(1f, 0.85f, 0.2f);   // altin sari
    public Color rocketColor     = new Color(1f, 0.6f, 0.2f);    // turuncu
    public Color bombColor       = new Color(0.85f, 0.3f, 1f);   // mor
    public Color colorBombColor  = new Color(1f, 0.4f, 0.7f);    // pembe
    public Color krakenColor     = new Color(0.3f, 0.9f, 1f);    // parlak mavi
    public Color goalCompleteColor = new Color(0.3f, 1f, 0.5f);  // yesilimsi

    [Header("Goal Complete Banner")]
    [Tooltip("Claude Design'dan gelen GOAL COMPLETE! banner PNG'si")]
    public Sprite goalCompleteBanner;
    [Tooltip("Banner'in ekrandaki maksimum genisligi (dunya birimi)")]
    public float goalBannerWidth = 4.5f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    [Header("Merkez Ince Ayari")]
    [Tooltip("Feedback yazisi/efektinin grid merkezine gore kaymasi")]
    public Vector2 centerOffset = Vector2.zero;

    private Vector3 GetGridCenter()
    {
        if (GridManager.Instance == null) return Vector3.zero;
        GridManager g = GridManager.Instance;
        Vector3 center = Vector3.zero;
        if (g.gridParent != null)
            center += g.gridParent.position;
        center.x += centerOffset.x;
        center.y += centerOffset.y;
        center.z = 0f;
        return center;
    }

    public void SpawnBurst(Vector3 worldPos, Color color)
    {
        if (particleBurstPrefab == null) return;
        GameObject obj = Instantiate(particleBurstPrefab, worldPos, Quaternion.identity);
        ParticleSystem ps = obj.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.startColor = color;
        }
        Destroy(obj, 2f);
    }

    public void SpawnScorePopup(Vector3 worldPos, int score, Color color)
    {
        if (scorePopupPrefab == null) return;
        GameObject obj = Instantiate(scorePopupPrefab, worldPos, Quaternion.identity);
        ScorePopup popup = obj.GetComponent<ScorePopup>();
        if (popup != null) popup.Show(score, color);
    }

    private void SpawnFlash(Vector3 worldPos, Color color)
    {
        if (flashSprite == null) return;

        GameObject obj = new GameObject("ComboFlash");
        obj.transform.position = worldPos;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = flashSprite;
        sr.sortingOrder = flashSortingOrder;

        float spriteSize = Mathf.Max(flashSprite.bounds.size.x, flashSprite.bounds.size.y);
        float targetScale = spriteSize > 0f ? flashMaxSize / spriteSize : 1f;

        obj.transform.localScale = Vector3.one * (targetScale * 0.3f);
        sr.color = new Color(color.r, color.g, color.b, 0f);

        float fadeIn  = flashDuration * 0.15f;
        float fadeOut = Mathf.Max(0.1f, flashDuration - fadeIn - flashHoldTime);

        Sequence seq = DOTween.Sequence();
        seq.Append(sr.DOFade(flashPeakAlpha, fadeIn));
        seq.Join(obj.transform.DOScale(targetScale, fadeIn * 1.6f).SetEase(Ease.OutQuad));
        seq.AppendInterval(flashHoldTime);
        seq.Append(sr.DOFade(0f, fadeOut).SetEase(Ease.InQuad));
        seq.Join(obj.transform.DOScale(targetScale * 1.2f, fadeOut).SetEase(Ease.OutQuad));
        seq.OnComplete(() => Destroy(obj));
    }

    private void SpawnBurstCluster(Vector3 center, Color color)
    {
        if (particleBurstPrefab == null) return;
        for (int i = 0; i < burstCountBehindText; i++)
        {
            Vector2 offset = Random.insideUnitCircle * burstSpreadRadius;
            SpawnBurst(center + new Vector3(offset.x, offset.y, 0f), color);
        }
    }

    private void SpawnFeedbackText(string message, Color color)
    {
        if (scorePopupPrefab == null || string.IsNullOrEmpty(message)) return;

        Vector3 center = GetGridCenter();

        SpawnFlash(center, color);
        SpawnBurstCluster(center, color);

        GameObject obj = Instantiate(scorePopupPrefab, center, Quaternion.identity);
        ScorePopup popup = obj.GetComponent<ScorePopup>();
        if (popup != null) popup.ShowTextAt(message, color, center);
    }

    public void SpawnComboText(int comboLevel)
    {
        string msg = GetComboMessage(comboLevel);
        if (string.IsNullOrEmpty(msg)) return;
        SpawnFeedbackText(msg, comboColor);
    }

    private string GetComboMessage(int comboLevel)
    {
        switch (comboLevel)
        {
            case 2: return "Fin-tastic!";
            case 3: return "Splash-tastic!";
            case 4: return "O-fish-al!";
            default: return comboLevel >= 5 ? "Krilliant!" : null;
        }
    }

    public void SpawnSpecialText(SpecialType type)
    {
        string msg = null;
        Color color = comboColor;

        switch (type)
        {
            case SpecialType.RocketH:
            case SpecialType.RocketV:
                msg = "Torpedo!";
                color = rocketColor;
                break;
            case SpecialType.Bomb:
                msg = "Boom-arine!";
                color = bombColor;
                break;
            case SpecialType.ColorBomb:
                msg = "Reef Wrecker!";
                color = colorBombColor;
                break;
        }

        if (msg != null) SpawnFeedbackText(msg, color);
    }

    public void SpawnKrakenText()
    {
        SpawnFeedbackText("Kraken!", krakenColor);
    }

    public void SpawnGoalCompleteText()
    {
        Vector3 center = GetGridCenter();

        // Arkasında hafif parlama + parçacık
        SpawnFlash(center, goalCompleteColor);
        SpawnBurstCluster(center, goalCompleteColor);

        if (goalCompleteBanner != null)
        {
            // Görsel banner'ı göster
            GameObject obj = new GameObject("GoalCompleteBanner");
            obj.transform.position = center;

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = goalCompleteBanner;
            sr.sortingOrder = 60;

            // Genişliğe göre ölçekle
            float nativeWidth = goalCompleteBanner.bounds.size.x;
            float scale = nativeWidth > 0f ? goalBannerWidth / nativeWidth : 1f;

            obj.transform.localScale = Vector3.zero;
            Color c = sr.color;
            sr.color = new Color(c.r, c.g, c.b, 0f);

            Sequence seq = DOTween.Sequence();
            seq.Append(obj.transform.DOScale(scale, 0.4f).SetEase(Ease.OutBack));
            seq.Join(sr.DOFade(1f, 0.25f));
            seq.Append(obj.transform.DOPunchScale(Vector3.one * scale * 0.1f, 0.3f, 6, 0.6f));
            seq.AppendInterval(1.4f);
            seq.Append(sr.DOFade(0f, 0.5f));
            seq.Join(obj.transform.DOScale(scale * 1.1f, 0.5f).SetEase(Ease.OutQuad));
            seq.OnComplete(() => Destroy(obj));
        }
        else if (scorePopupPrefab != null)
        {
            // Sprite atanmamışsa eski yazı sistemine geri düş
            GameObject obj = Instantiate(scorePopupPrefab, center, Quaternion.identity);
            ScorePopup popup = obj.GetComponent<ScorePopup>();
            if (popup != null) popup.ShowBigTitle("GOAL COMPLETE!", goalCompleteColor, center);
        }
    }
}