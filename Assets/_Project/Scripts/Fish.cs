using UnityEngine;

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

        // Collider'ı sprite'a göre güncelle
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null && fishData.sprite != null)
        {
            col.size = fishData.sprite.bounds.size;
        }
    }

    public void SetGridPosition(int x, int y)
    {
        gridX = x;
        gridY = y;
        name = $"Fish_{data.fishType}_({x},{y})";
    }
}