using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(SpriteRenderer))]
public class Obstacle : MonoBehaviour
{
    [Header("Data")]
    public ObstacleType type;
    public int gridX;
    public int gridY;

    [Header("State")]
    public int currentHP;

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Initialize obstacle: tipe göre HP ve görsel ayarla.
    /// </summary>
    public void Initialize(ObstacleType obstacleType, int x, int y)
    {
        type = obstacleType;
        gridX = x;
        gridY = y;

        // Tipe göre max HP
        switch (type)
        {
            case ObstacleType.Seaweed: currentHP = 1; break;
            case ObstacleType.Coral:   currentHP = 2; break;
            case ObstacleType.Ice:     currentHP = 3; break;
        }

        UpdateVisual();
        name = $"Obstacle_{type}_({x},{y})";
    }

    /// <summary>
    /// Yan komşuluğunda match olduğunda zarar al.
    /// HP biterse temizlen, true döner.
    /// </summary>
    public bool TakeDamage()
    {
        currentHP--;

        // Shake animasyonu
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

    private void UpdateVisual()
    {
        // Geçici: tipe ve HP'ye göre renk
        // Sonra polish'te gerçek sprite eklenecek
        Color color = Color.white;
        switch (type)
        {
            case ObstacleType.Seaweed: color = new Color(0.4f, 0.7f, 0.3f); break;  // yeşil
            case ObstacleType.Coral:   color = new Color(1f, 0.5f, 0.4f);   break;  // mercan/turuncu
            case ObstacleType.Ice:     color = new Color(0.7f, 0.9f, 1f);   break;  // açık mavi
        }
        // HP düştükçe karart
        float alpha = Mathf.Lerp(0.5f, 1f, (float)currentHP / GetMaxHP());
        color = Color.Lerp(Color.gray, color, alpha);
        sr.color = color;
    }

    private int GetMaxHP()
    {
        switch (type)
        {
            case ObstacleType.Seaweed: return 1;
            case ObstacleType.Coral:   return 2;
            case ObstacleType.Ice:     return 3;
            default: return 1;
        }
    }

    private void BreakAndDestroy()
    {
        Debug.Log($"<color=lime>💎 {type} kırıldı @ ({gridX},{gridY})</color>");

        transform.DOKill();
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(1.3f, 0.15f));
        seq.Append(transform.DOScale(0f, 0.25f).SetEase(Ease.InBack));
        seq.OnComplete(() => Destroy(gameObject));
    }
}