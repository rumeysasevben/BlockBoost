using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Settings")]
    public int width = 8;
    public int height = 8;
    public float cellSize = 0.7f;

    [Header("References")]
    public GameObject fishPrefab;
    public FishData[] fishDataPool;  // Inspector'a 6 FishData'yı sürükleyeceksin

    [Header("Layout")]
    public Transform gridParent;     // Tüm balıklar bunun child'ı olacak (düzen için)

    // Grid'in kendisi: 2D dizi
    private Fish[,] grid;

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        CreateGrid();
    }

    /// <summary>
    /// 8x8 grid'i rastgele balıklarla doldurur.
    /// </summary>
    private void CreateGrid()
    {
        grid = new Fish[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                SpawnFishAt(x, y);
            }
        }

        Debug.Log($"Grid oluşturuldu: {width}x{height}");
    }

    private void SpawnFishAt(int x, int y)
    {
        Vector3 worldPos = GridToWorldPosition(x, y);
        GameObject obj = Instantiate(fishPrefab, worldPos, Quaternion.identity, gridParent);

        Fish fish = obj.GetComponent<Fish>();
        FishData randomData = GetRandomFishData();
        fish.Initialize(randomData, x, y);

        grid[x, y] = fish;
    }

    /// <summary>
    /// Grid koordinatını dünya pozisyonuna çevirir.
    /// Grid ekranın merkezinde olacak şekilde offset uygular.
    /// </summary>
    public Vector3 GridToWorldPosition(int x, int y)
    {
        float offsetX = -(width - 1) * cellSize / 2f;
        float offsetY = -(height - 1) * cellSize / 2f;

        return new Vector3(
            x * cellSize + offsetX,
            y * cellSize + offsetY,
            0f
        );
    }

    /// <summary>
    /// spawnWeight'lere göre ağırlıklı rastgele FishData döner.
    /// </summary>
    private FishData GetRandomFishData()
    {
        float totalWeight = 0f;
        foreach (var data in fishDataPool)
            totalWeight += data.spawnWeight;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var data in fishDataPool)
        {
            cumulative += data.spawnWeight;
            if (roll <= cumulative)
                return data;
        }

        return fishDataPool[0]; // Fallback
    }

    /// <summary>
    /// Belirli bir koordinattaki balığı döner.
    /// </summary>
    public Fish GetFishAt(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return null;
        return grid[x, y];
    }

    /// <summary>
    /// İki balığın grid pozisyonunu takas eder (henüz görsel hareket yok, sadece data).
    /// </summary>
    public void SwapFish(Fish a, Fish b)
    {
        int ax = a.gridX, ay = a.gridY;
        int bx = b.gridX, by = b.gridY;

        grid[ax, ay] = b;
        grid[bx, by] = a;

        a.SetGridPosition(bx, by);
        b.SetGridPosition(ax, ay);

        a.transform.position = GridToWorldPosition(bx, by);
        b.transform.position = GridToWorldPosition(ax, ay);
    }
}