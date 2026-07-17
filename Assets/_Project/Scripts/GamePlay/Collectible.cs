using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(SpriteRenderer))]
public class Collectible : MonoBehaviour
{
    [Header("Data")]
    public CollectibleType type;
    public int gridX;
    public int gridY;

    [Header("Sprites")]
    public Sprite chestSprite; // chest
    public Sprite keySprite;   // key
    [Range(0.5f, 1f)] public float fillRatio = 0.85f;

    [Header("Grid Settings")]
    public float cellSize = 1f; // GridManager tarafindan Initialize'da otomatik set edilir

    private SpriteRenderer sr;
    private Sprite defaultSprite;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        defaultSprite = sr.sprite;
    }

    // GridManager cellSize'i gecirir; eski cagrilar icin varsayilan overload da var
    public void Initialize(CollectibleType t, int x, int y, float gridCellSize)
    {
        cellSize = gridCellSize;
        Initialize(t, x, y);
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

        Sprite chosen = null;
        switch (type)
        {
            case CollectibleType.Chest: chosen = chestSprite; break;
            case CollectibleType.Key:   chosen = keySprite;   break;
        }

        if (chosen != null)
        {
            sr.sprite = chosen;
            FitSpriteToCell();
            sr.color = Color.white;
        }
        else
        {
            sr.sprite = defaultSprite;
            transform.localScale = Vector3.one;
            switch (type)
            {
                case CollectibleType.Chest: sr.color = new Color(1f, 0.85f, 0.2f); break;
                case CollectibleType.Key:   sr.color = new Color(0.6f, 0.8f, 1f);  break;
            }
        }
    }

    private void FitSpriteToCell()
    {
        Vector2 nativeSize = sr.sprite.bounds.size;
        float scaleX = cellSize / nativeSize.x;
        float scaleY = cellSize / nativeSize.y;
        float uniformScale = Mathf.Min(scaleX, scaleY) * fillRatio;
        transform.localScale = new Vector3(uniformScale, uniformScale, 1f);
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

    public void DeliverAndDestroy()
    {
        transform.DOKill();
        Vector3 s = transform.localScale;
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(s * 1.5f, 0.2f).SetEase(Ease.OutBack));
        seq.Append(transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack));
        seq.OnComplete(() => Destroy(gameObject));
    }
}