using System.Collections;
using System.Collections.Generic;
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
    public Transform gridParent;     // Tüm balıklar bunun child'ı olacak

    [Header("State")]
    public bool IsBusy { get; private set; }

    // Grid'in kendisi: 2D dizi
    private Fish[,] grid;

    private void Awake()
    {
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

    // ─────────────────────────────────────────────
    // GRID OLUŞTURMA
    // ─────────────────────────────────────────────

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
        FishData safeData = GetSafeRandomFishData(x, y);
        fish.Initialize(safeData, x, y);

        grid[x, y] = fish;
    }

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

        return fishDataPool[0];
    }

    /// <summary>
    /// Sol/alt komşulara bakarak 3'lü match oluşturacak tipleri hariç tutar.
    /// </summary>
    private FishData GetSafeRandomFishData(int x, int y)
    {
        FishType? forbiddenH = null;
        if (x >= 2)
        {
            Fish a = grid[x - 1, y];
            Fish b = grid[x - 2, y];
            if (a != null && b != null && a.data.fishType == b.data.fishType)
                forbiddenH = a.data.fishType;
        }

        FishType? forbiddenV = null;
        if (y >= 2)
        {
            Fish a = grid[x, y - 1];
            Fish b = grid[x, y - 2];
            if (a != null && b != null && a.data.fishType == b.data.fishType)
                forbiddenV = a.data.fishType;
        }

        for (int i = 0; i < 10; i++)
        {
            FishData picked = GetRandomFishData();
            if (picked.fishType != forbiddenH && picked.fishType != forbiddenV)
                return picked;
        }
        return GetRandomFishData();
    }

    // ─────────────────────────────────────────────
    // POZİSYON HELPERS
    // ─────────────────────────────────────────────

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

    public Fish GetFishAt(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return null;
        return grid[x, y];
    }

    // ─────────────────────────────────────────────
    // SWAP
    // ─────────────────────────────────────────────

    /// <summary>
    /// Anlık swap (data + pozisyon ışınlama). Animasyonsuz, internal kullanımlar için.
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

    /// <summary>
    /// Animasyonlu swap: data anlık değişir, görsel DOTween ile akıcı hareket eder.
    /// </summary>
    public IEnumerator SwapFishAnimated(Fish a, Fish b, float duration = 0.2f)
    {
        int ax = a.gridX, ay = a.gridY;
        int bx = b.gridX, by = b.gridY;

        grid[ax, ay] = b;
        grid[bx, by] = a;

        a.SetGridPosition(bx, by);
        b.SetGridPosition(ax, ay);

        a.MoveTo(GridToWorldPosition(bx, by), duration);
        b.MoveTo(GridToWorldPosition(ax, ay), duration);

        yield return new WaitForSeconds(duration);
    }

    // ─────────────────────────────────────────────
    // CLEAR + GRAVITY + REFILL + CASCADE
    // ─────────────────────────────────────────────

    /// <summary>
    /// Ana döngü: match bul → patlat → düşür → doldur → tekrar match var mı?
    /// </summary>
    public IEnumerator ProcessMatches()
    {
        IsBusy = true;
        int comboLevel = 0;

        while (true)
        {
            HashSet<Fish> matches = MatchFinder.Instance.FindAllMatches();
            if (matches.Count == 0) break;

            comboLevel++;
            if (comboLevel > 1)
                Debug.Log($"<color=magenta>★ COMBO x{comboLevel}! ★</color>");

            // --- CLEAR ---
            float sizeMultiplier = matches.Count >= 5 ? 2f : matches.Count >= 4 ? 1.5f : 1f;
            float totalMultiplier = sizeMultiplier * comboLevel;
            int totalScore = 0;

            foreach (Fish f in matches)
            {
                if (f == null) continue;
                totalScore += Mathf.RoundToInt(f.data.scoreValue * totalMultiplier);
                grid[f.gridX, f.gridY] = null;
                f.PopAndDestroy();
            }
            ScoreManager.Instance.AddScore(totalScore);

            yield return new WaitForSeconds(0.3f);

            // --- GRAVITY ---
            yield return StartCoroutine(ApplyGravity());

            // --- REFILL ---
            yield return StartCoroutine(RefillGrid());
        }

        IsBusy = false;
    }

    private IEnumerator ApplyGravity()
    {
        const float fallDuration = 0.3f;
        bool anyMoved = false;

        for (int x = 0; x < width; x++)
        {
            int writeY = 0;
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] != null)
                {
                    if (y != writeY)
                    {
                        Fish f = grid[x, y];
                        grid[x, writeY] = f;
                        grid[x, y] = null;
                        f.SetGridPosition(x, writeY);
                        f.MoveTo(GridToWorldPosition(x, writeY), fallDuration);
                        anyMoved = true;
                    }
                    writeY++;
                }
            }
        }

        if (anyMoved)
            yield return new WaitForSeconds(fallDuration);
    }

    private IEnumerator RefillGrid()
    {
        const float fallDuration = 0.4f;
        bool anySpawned = false;

        for (int x = 0; x < width; x++)
        {
            int spawnOffset = 0;
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == null)
                {
                    Vector3 spawnPos = GridToWorldPosition(x, height + spawnOffset);
                    Vector3 targetPos = GridToWorldPosition(x, y);

                    GameObject obj = Instantiate(fishPrefab, spawnPos, Quaternion.identity, gridParent);
                    Fish fish = obj.GetComponent<Fish>();
                    fish.Initialize(GetSafeRandomFishData(x, y), x, y);
                    fish.MoveTo(targetPos, fallDuration);

                    grid[x, y] = fish;
                    spawnOffset++;
                    anySpawned = true;
                }
            }
        }

        if (anySpawned)
            yield return new WaitForSeconds(fallDuration);
    }
}