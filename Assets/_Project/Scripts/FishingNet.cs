using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class FishingNet : MonoBehaviour
{
    [Header("Data")]
    public int gridX;
    public int gridY;

    [Header("State")]
    public int currentHP = 1;

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void Initialize(int x, int y)
    {
        gridX = x;
        gridY = y;
        currentHP = 1;
        UpdateVisual();
        name = $"FishingNet_({x},{y})";
    }

    /// <summary>
    /// Yan komşuluğundaki match'lerden zarar alır. HP biterse temizlen, true döner.
    /// </summary>
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
        sr.color = new Color(0.9f, 0.9f, 0.95f, 0.7f); // beyaz-saydam (ağ teması)
    }

    private void BreakAndDestroy()
    {
        transform.DOKill();
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(1.3f, 0.15f));
        seq.Append(transform.DOScale(0f, 0.25f).SetEase(Ease.InBack));
        seq.OnComplete(() => Destroy(gameObject));
    }
}