using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(SpriteRenderer))]
public class Collectible : MonoBehaviour
{
    [Header("Data")]
    public CollectibleType type;
    public int gridX;
    public int gridY;

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void Initialize(CollectibleType t, int x, int y)
    {
        type = t;
        gridX = x;
        gridY = y;
        UpdateVisual();
        name = $"Collectible_{type}_({x},{y})";
    }

    private void UpdateVisual()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        switch (type)
        {
            case CollectibleType.Chest: sr.color = new Color(1f, 0.85f, 0.2f); break; // gold
            case CollectibleType.Key:   sr.color = new Color(0.6f, 0.8f, 1f); break;  // blue-silver
        }
    }

    public void SetGridPosition(int x, int y)
    {
        gridX = x;
        gridY = y;
    }

    public void MoveTo(Vector3 worldPos, float duration)
    {
        transform.DOKill();
        transform.DOMove(worldPos, duration).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// Teslim animasyonu (büyür, sonra kaybolur).
    /// </summary>
    public void DeliverAndDestroy()
    {
        transform.DOKill();
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(Vector3.one * 1.5f, 0.2f).SetEase(Ease.OutBack));
        seq.Append(transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack));
        seq.OnComplete(() => Destroy(gameObject));
    }
}