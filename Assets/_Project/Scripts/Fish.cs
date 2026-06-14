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

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void Initialize(FishData fishData, int x, int y)
    {
        data = fishData;
        gridX = x;
        gridY = y;
        sr.sprite = fishData.sprite;
        name = $"Fish_{fishData.fishType}_({x},{y})";

        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null && fishData.sprite != null)
            col.size = fishData.sprite.bounds.size;
    }

    public void SetGridPosition(int x, int y)
    {
        gridX = x;
        gridY = y;
        name = $"Fish_{data.fishType}_({x},{y})";
    }

    /// <summary>
    /// Match'lendiğinde çağrılır: scale pop + flash + destroy.
    /// </summary>
    public void PopAndDestroy(float duration = 0.25f)
    {
        // Tüm tween'leri kes (üst üste binmesin)
        transform.DOKill();

        // Flash: beyaza dön sonra geri (kısa)
        sr.DOColor(Color.white, duration * 0.4f);

        // Pop: hafif büyüsün sonra 0'a insin
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(1.2f, duration * 0.3f).SetEase(Ease.OutQuad));
        seq.Append(transform.DOScale(0f, duration * 0.7f).SetEase(Ease.InBack));
        seq.OnComplete(() => Destroy(gameObject));
    }

    /// <summary>
    /// Balığı verilen dünya pozisyonuna DOTween ile taşır (düşme animasyonu).
    /// </summary>
    public void MoveTo(Vector3 worldPos, float duration = 0.3f)
    {
        transform.DOKill();
        transform.DOMove(worldPos, duration).SetEase(Ease.OutQuad);
    }
}