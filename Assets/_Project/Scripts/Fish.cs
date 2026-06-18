using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class Fish : MonoBehaviour
{
    [Header("Data")]
    public FishData data;

    [Header("Grid Position")]
    public int gridX;
    public int gridY;

    [Header("Special")]
    public SpecialType specialType = SpecialType.None;

    public bool IsSpecial => specialType != SpecialType.None;

    private SpriteRenderer sr;
    private Vector3 baseScale = Vector3.one;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void Initialize(FishData fishData, int x, int y)
    {
        data = fishData;
        gridX = x;
        gridY = y;
        specialType = SpecialType.None;
        sr.sprite = fishData.sprite;
        sr.color = Color.white;
        transform.localScale = baseScale;
        name = $"Fish_{fishData.fishType}_({x},{y})";

        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null && fishData.sprite != null)
            col.size = fishData.sprite.bounds.size;
    }

    public void SetGridPosition(int x, int y)
    {
        gridX = x;
        gridY = y;
        name = (specialType == SpecialType.None
            ? $"Fish_{data.fishType}_({x},{y})"
            : $"Fish_{data.fishType}_{specialType}_({x},{y})");
    }

    /// <summary>
    /// Bu balığı bir special tile'a dönüştürür. Renk + scale değişir.
    /// </summary>
    public void MakeSpecial(SpecialType type)
    {
        specialType = type;
        SetGridPosition(gridX, gridY);

        switch (type)
        {
            case SpecialType.RocketH:
                sr.color = new Color(1f, 0.85f, 0.2f);   // sarı
                break;
            case SpecialType.RocketV:
                sr.color = new Color(0.2f, 0.85f, 1f);   // mavi
                break;
            case SpecialType.Bomb:
                sr.color = new Color(0.85f, 0.3f, 1f);   // mor
                break;
            case SpecialType.ColorBomb:
                sr.color = new Color(1f, 0.4f, 0.7f);    // pembe
                break;
            default:
                sr.color = Color.white;
                break;
        }

        transform.DOKill();
        transform.localScale = Vector3.one;
        transform.DOPunchScale(Vector3.one * 0.3f, 0.4f, 8, 0.5f);
    }

    public void PopAndDestroy(float duration = 0.25f)
    {
        transform.DOKill();
        sr.DOColor(Color.white, duration * 0.4f);

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(1.2f, duration * 0.3f).SetEase(Ease.OutQuad));
        seq.Append(transform.DOScale(0f, duration * 0.7f).SetEase(Ease.InBack));
        seq.OnComplete(() => Destroy(gameObject));
    }

    public void MoveTo(Vector3 worldPos, float duration = 0.3f)
    {
        transform.DOKill();
        transform.DOMove(worldPos, duration).SetEase(Ease.OutQuad);
    }
}