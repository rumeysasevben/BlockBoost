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

    [Header("Special Sprites")]
    public Sprite bombSprite;
    public Sprite colorBombSprite;
    public Sprite rocketSprite; // Sprite dikey cizili. RocketH icin 90 derece yatirilir, RocketV dik kalir

    [Header("Sizing")]
    [Range(0.5f, 1f)] public float fillRatio = 0.85f; // Hucrenin ne kadarini kaplasin

    public bool IsSpecial => specialType != SpecialType.None;

    private SpriteRenderer sr;
    private Vector3 baseScale = Vector3.one;
    private float cellSize = 0.6f; // GridManager tarafindan Initialize'da set edilir

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    // GridManager cellSize'i gecirir; eski cagrilar icin varsayilan overload da var
    public void Initialize(FishData fishData, int x, int y, float gridCellSize)
    {
        cellSize = gridCellSize;
        Initialize(fishData, x, y);
    }

    public void Initialize(FishData fishData, int x, int y)
    {
        data = fishData;
        gridX = x;
        gridY = y;
        specialType = SpecialType.None;
        sr.sprite = fishData.sprite;
        sr.color = Color.white;
        transform.rotation = Quaternion.identity; // onceki special rotasyonunu sifirla

        // Sprite'i hucre boyutuna normalize et (buyuk PNG'ler tasmasin)
        FitToCell(fishData.sprite);

        name = $"Fish_{fishData.fishType}_({x},{y})";

        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null && fishData.sprite != null)
            col.size = fishData.sprite.bounds.size;
    }

    private void FitToCell(Sprite sprite)
    {
        if (sprite == null) { baseScale = Vector3.one; transform.localScale = baseScale; return; }
        float target = cellSize * fillRatio;
        float spriteSize = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
        float scaleFactor = spriteSize > 0f ? target / spriteSize : 1f;
        baseScale = Vector3.one * scaleFactor;
        transform.localScale = baseScale;
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
    /// Bu baligi bir special tile'a donusturur. Sprite + boyut + efekt degisir.
    /// </summary>
    public void MakeSpecial(SpecialType type)
    {
        specialType = type;
        SetGridPosition(gridX, gridY);

        // Hedef boyut: baliklarla ayni referans (cellSize * fillRatio)
        float targetWorldSize = cellSize * fillRatio;

        transform.rotation = Quaternion.identity;
        sr.color = Color.white;

        Sprite chosen = null;
        bool rotate = false;

        switch (type)
        {
            case SpecialType.RocketH:
                chosen = rocketSprite;
                rotate = true; // sprite dikey cizili, yatay temizlik icin 90 dereceye yatir
                if (chosen == null) sr.color = new Color(1f, 0.85f, 0.2f);
                break;

            case SpecialType.RocketV:
                chosen = rocketSprite;
                // sprite zaten dikey cizili, dik kalsin (dondurme)
                if (chosen == null) sr.color = new Color(0.2f, 0.85f, 1f);
                break;

            case SpecialType.Bomb:
                chosen = bombSprite;
                if (chosen == null) sr.color = new Color(0.85f, 0.3f, 1f);
                break;

            case SpecialType.ColorBomb:
                chosen = colorBombSprite;
                if (chosen == null) sr.color = new Color(1f, 0.4f, 0.7f);
                break;

            default:
                sr.color = Color.white;
                break;
        }

        if (chosen != null)
        {
            sr.sprite = chosen;
            float specialSize = Mathf.Max(chosen.bounds.size.x, chosen.bounds.size.y);
            float scaleFactor = specialSize > 0f ? targetWorldSize / specialSize : 1f;
            baseScale = Vector3.one * scaleFactor;
        }
        else
        {
            FitToCell(data != null ? data.sprite : null);
        }

        if (rotate) transform.rotation = Quaternion.Euler(0f, 0f, 90f);

        transform.DOKill();
        transform.localScale = baseScale;
        transform.DOPunchScale(baseScale * 0.3f, 0.4f, 8, 0.5f);
    }

    public void PopAndDestroy(float duration = 0.25f)
    {
        transform.DOKill();
        sr.DOColor(Color.white, duration * 0.4f);

        Vector3 s = transform.localScale;
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(s * 1.2f, duration * 0.3f).SetEase(Ease.OutQuad));
        seq.Append(transform.DOScale(0f, duration * 0.7f).SetEase(Ease.InBack));
        seq.OnComplete(() => Destroy(gameObject));
    }

    public void MoveTo(Vector3 worldPos, float duration = 0.3f)
    {
        transform.DOKill();
        transform.DOMove(worldPos, duration).SetEase(Ease.OutQuad);
    }
    // Idle sallanma — hareketsizken hafifçe döner (canlı görünüm)
    public void PlayIdle()
    {
        // special taşları veya hareket halindekileri sallamayalım
        if (IsSpecial) return;
        transform.DOKill();
        transform.localScale = baseScale;
        transform.rotation = Quaternion.identity;

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DORotate(new Vector3(0, 0, 6f), 0.5f).SetEase(Ease.InOutSine));
        seq.Append(transform.DORotate(new Vector3(0, 0, -6f), 1f).SetEase(Ease.InOutSine));
        seq.Append(transform.DORotate(Vector3.zero, 0.5f).SetEase(Ease.InOutSine));
    }
}