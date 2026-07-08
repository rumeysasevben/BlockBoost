using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(SpriteRenderer))]
public class Obstacle : MonoBehaviour
{
    [Header("Data")]
    public ObstacleType type;
    public int gridX;
    public int gridY;

    [Header("Sprites")]
    public Sprite seaweedSprite; // moss
    public Sprite coralSprite;
    public Sprite iceSprite;
    public Sprite cageSprite;

    [Header("Grid Settings")]
    public float cellSize = 1f; // GridManager tarafindan Initialize'da otomatik set edilir
    [Range(0.5f, 1f)] public float fillRatio = 0.9f;

    [Header("State")]
    public int currentHP;

    private SpriteRenderer sr;
    private Sprite defaultSprite;
    private Vector3 baseScale = Vector3.one;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        defaultSprite = sr.sprite;
        baseScale = transform.localScale;
    }

    // GridManager cellSize'i gecirir; eski cagrilar icin varsayilan overload da var
    public void Initialize(ObstacleType obstacleType, int x, int y, float gridCellSize)
    {
        cellSize = gridCellSize;
        Initialize(obstacleType, x, y);
    }

    public void Initialize(ObstacleType obstacleType, int x, int y)
    {
        type = obstacleType;
        gridX = x;
        gridY = y;
        currentHP = GetMaxHP();
        UpdateVisual();
        name = $"Obstacle_{type}_({x},{y})";
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

        UpdateVisual();
        return false;
    }

    // Bu tip icin atanmis sprite'i dondurur (yoksa null)
    private Sprite GetSpriteForType()
    {
        switch (type)
        {
            case ObstacleType.Seaweed: return seaweedSprite;
            case ObstacleType.Coral:   return coralSprite;
            case ObstacleType.Ice:     return iceSprite;
            case ObstacleType.Cage:    return cageSprite;
            default: return null;
        }
    }

    private void UpdateVisual()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        Sprite typeSprite = GetSpriteForType();

        if (typeSprite != null)
        {
            sr.sprite = typeSprite;
            FitSpriteToCell();
            float alpha = Mathf.Lerp(0.55f, 1f, (float)currentHP / GetMaxHP());
            sr.color = new Color(1f, 1f, 1f, alpha);
            return;
        }

        // Sprite atanmamissa eski renkli placeholder
        sr.sprite = defaultSprite;
        transform.localScale = baseScale;
        Color color = Color.white;
        switch (type)
        {
            case ObstacleType.Seaweed: color = new Color(0.4f, 0.7f, 0.3f); break;
            case ObstacleType.Coral:   color = new Color(1f, 0.5f, 0.4f);   break;
            case ObstacleType.Ice:     color = new Color(0.7f, 0.9f, 1f);   break;
            case ObstacleType.Cage:    color = new Color(0.55f, 0.4f, 0.2f); break;
        }
        float a = Mathf.Lerp(0.5f, 1f, (float)currentHP / GetMaxHP());
        color = Color.Lerp(Color.gray, color, a);
        sr.color = color;
    }

    // Sprite'in piksel boyutu ne olursa olsun, orani bozmadan tek hucreye sigdirir
    private void FitSpriteToCell()
    {
        Vector2 nativeSize = sr.sprite.bounds.size;
        float scaleX = cellSize / nativeSize.x;
        float scaleY = cellSize / nativeSize.y;
        float uniformScale = Mathf.Min(scaleX, scaleY) * fillRatio;
        baseScale = new Vector3(uniformScale, uniformScale, 1f);
        transform.localScale = baseScale;
    }

    private int GetMaxHP()
    {
        switch (type)
        {
            case ObstacleType.Seaweed: return 1;
            case ObstacleType.Coral:   return 2;
            case ObstacleType.Ice:     return 3;
            case ObstacleType.Cage:    return 2;
            default: return 1;
        }
    }

    private void BreakAndDestroy()
    {
        transform.DOKill();
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(baseScale * 1.3f, 0.15f));
        seq.Append(transform.DOScale(0f, 0.25f).SetEase(Ease.InBack));
        seq.OnComplete(() => Destroy(gameObject));
    }
}