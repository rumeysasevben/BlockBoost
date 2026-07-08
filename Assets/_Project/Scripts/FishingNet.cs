using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class FishingNet : MonoBehaviour
{
    [Header("Data")]
    public int gridX;
    public int gridY;

    [Header("Sprite")]
    public Sprite netSprite; // net
    [Range(0.5f, 1f)] public float fillRatio = 0.95f;
    [Range(0f, 1f)] public float netAlpha = 0.85f;
    public int sortingOrder = 10; // Baligin ustunde gorunmesi icin yuksek

    [Header("Grid Settings")]
    public float cellSize = 1f; // GridManager tarafindan Initialize'da otomatik set edilir

    [Header("State")]
    public int currentHP = 1;

    private SpriteRenderer sr;
    private Sprite defaultSprite;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        defaultSprite = sr.sprite;
    }

    // GridManager cellSize'i gecirir; eski cagrilar icin varsayilan overload da var
    public void Initialize(int x, int y, float gridCellSize)
    {
        cellSize = gridCellSize;
        Initialize(x, y);
    }

    public void Initialize(int x, int y)
    {
        gridX = x;
        gridY = y;
        currentHP = 1;
        UpdateVisual();
        name = $"FishingNet_({x},{y})";
    }

    public bool TakeDamage()
    {
        currentHP--;
        transform.DOKill();
        transform.DOShakePosition(0.2f, 0.1f, 20);

        if (currentHP <= 0)
        {
            BreakAndDestroy();
            return true;
        }
        return false;
    }

    private void UpdateVisual()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        if (netSprite != null)
        {
            sr.sprite = netSprite;
            FitSpriteToCell();
            sr.color = new Color(1f, 1f, 1f, netAlpha);
        }
        else
        {
            sr.sprite = defaultSprite;
            sr.color = new Color(0.9f, 0.9f, 0.95f, 0.7f);
        }

        sr.sortingOrder = sortingOrder;
    }

    private void FitSpriteToCell()
    {
        Vector2 nativeSize = sr.sprite.bounds.size;
        float scaleX = cellSize / nativeSize.x;
        float scaleY = cellSize / nativeSize.y;
        float uniformScale = Mathf.Min(scaleX, scaleY) * fillRatio;
        transform.localScale = new Vector3(uniformScale, uniformScale, 1f);
    }

    private void BreakAndDestroy()
    {
        transform.DOKill();
        Sequence seq = DOTween.Sequence();
        Vector3 s = transform.localScale;
        seq.Append(transform.DOScale(s * 1.3f, 0.15f));
        seq.Append(transform.DOScale(0f, 0.25f).SetEase(Ease.InBack));
        seq.OnComplete(() => Destroy(gameObject));
    }
}