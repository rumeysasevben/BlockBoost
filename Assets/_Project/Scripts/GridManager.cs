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
    public FishData[] fishDataPool;

    [Header("Layout")]
    public Transform gridParent;

    [Header("State")]
    public bool IsBusy { get; private set; }

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
        // Grid artık LevelManager tarafından LoadLevel'da kurulur
    }

    private void CreateGrid()
    {
        grid = new Fish[width, height];

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                SpawnFishAt(x, y);

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
        foreach (var data in fishDataPool) totalWeight += data.spawnWeight;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var data in fishDataPool)
        {
            cumulative += data.spawnWeight;
            if (roll <= cumulative) return data;
        }
        return fishDataPool[0];
    }

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

    public Vector3 GridToWorldPosition(int x, int y)
    {
        float offsetX = -(width - 1) * cellSize / 2f;
        float offsetY = -(height - 1) * cellSize / 2f;
        return new Vector3(x * cellSize + offsetX, y * cellSize + offsetY, 0f);
    }

    public Fish GetFishAt(int x, int y)
    {
        if (grid == null) return null;
        if (x < 0 || x >= width || y < 0 || y >= height) return null;
        return grid[x, y];
    }

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

    public IEnumerator ProcessMatches(Fish swappedA = null, Fish swappedB = null)
    {
        IsBusy = true;
        int comboLevel = 0;
        bool firstIteration = true;

        while (true)
        {
            List<MatchGroup> groups = MatchFinder.Instance.FindAllMatchGroups();
            if (groups.Count == 0) break;

            comboLevel++;
            if (comboLevel > 1)
                Debug.Log($"<color=magenta>★ COMBO x{comboLevel}! ★</color>");

            Dictionary<Fish, List<MatchGroup>> fishGroups = new Dictionary<Fish, List<MatchGroup>>();
            foreach (var g in groups)
                foreach (var f in g.fish)
                {
                    if (!fishGroups.ContainsKey(f)) fishGroups[f] = new List<MatchGroup>();
                    fishGroups[f].Add(g);
                }

            HashSet<Fish> intersectionFishes = new HashSet<Fish>();
            foreach (var kvp in fishGroups)
            {
                MatchGroup hG = null, vG = null;
                foreach (var g in kvp.Value)
                {
                    if (g.isHorizontal && (hG == null || g.Length > hG.Length)) hG = g;
                    else if (!g.isHorizontal && (vG == null || g.Length > vG.Length)) vG = g;
                }
                if (hG != null && vG != null && (hG.Length + vG.Length - 1) >= 5)
                    intersectionFishes.Add(kvp.Key);
            }

            HashSet<Fish> promotedSpecials = new HashSet<Fish>();

            foreach (var fish in intersectionFishes)
            {
                if (fish.IsSpecial || promotedSpecials.Contains(fish)) continue;
                fish.MakeSpecial(SpecialType.Bomb);
                promotedSpecials.Add(fish);
                Debug.Log($"<color=yellow>★ Bomb (T/L) spawn @ ({fish.gridX},{fish.gridY})</color>");
            }

            foreach (var g in groups)
            {
                bool hasIntersection = false;
                foreach (var f in g.fish)
                    if (intersectionFishes.Contains(f)) { hasIntersection = true; break; }
                if (hasIntersection) continue;

                SpecialType specialFor = g.GetSpecialType();
                if (specialFor == SpecialType.None) continue;

                Fish promoter = null;
                if (firstIteration)
                {
                    if (swappedA != null && g.fish.Contains(swappedA)) promoter = swappedA;
                    else if (swappedB != null && g.fish.Contains(swappedB)) promoter = swappedB;
                }
                if (promoter == null) promoter = g.GetMiddleFish();

                if (promoter == null || promotedSpecials.Contains(promoter)) continue;
                if (promoter.IsSpecial) continue;

                promoter.MakeSpecial(specialFor);
                promotedSpecials.Add(promoter);
                Debug.Log($"<color=yellow>★ SPECIAL spawn: {specialFor} @ ({promoter.gridX},{promoter.gridY})</color>");
            }

            HashSet<Fish> toClear = new HashSet<Fish>();
            Queue<Fish> queue = new Queue<Fish>();

            int maxGroupLength = 0;
            foreach (var g in groups)
            {
                foreach (var f in g.fish)
                    if (!promotedSpecials.Contains(f))
                        queue.Enqueue(f);
                if (g.Length > maxGroupLength) maxGroupLength = g.Length;
            }

            while (queue.Count > 0)
            {
                Fish f = queue.Dequeue();
                if (f == null) continue;
                if (toClear.Contains(f)) continue;
                toClear.Add(f);

                if (f.IsSpecial)
                {
                    Debug.Log($"<color=orange>💥 ACTIVATE {f.specialType} @ ({f.gridX},{f.gridY})</color>");
                    List<Fish> activated = GetActivationArea(f);
                    foreach (var a in activated)
                        if (a != null && !toClear.Contains(a))
                            queue.Enqueue(a);
                }
            }

            float sizeMultiplier = maxGroupLength >= 5 ? 2f : maxGroupLength >= 4 ? 1.5f : 1f;
            float totalMultiplier = sizeMultiplier * comboLevel;
            int totalScore = 0;

            foreach (Fish f in toClear)
            {
                if (f == null) continue;
                totalScore += Mathf.RoundToInt(f.data.scoreValue * totalMultiplier);
                LevelManager.Instance?.ReportFishCollected(f.data.fishType, 1);
                grid[f.gridX, f.gridY] = null;
                f.PopAndDestroy();
            }
            ScoreManager.Instance.AddScore(totalScore);

            yield return new WaitForSeconds(0.3f);
            yield return StartCoroutine(ApplyGravity());
            yield return StartCoroutine(RefillGrid());

            firstIteration = false;
        }

        if (HasAnyValidMove() == false)
            yield return StartCoroutine(ShuffleGrid());

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

        if (anyMoved) yield return new WaitForSeconds(fallDuration);
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

        if (anySpawned) yield return new WaitForSeconds(fallDuration);
    }

    private List<Fish> GetActivationArea(Fish special)
    {
        List<Fish> affected = new List<Fish>();
        int x = special.gridX, y = special.gridY;

        switch (special.specialType)
        {
            case SpecialType.RocketH:
                for (int i = 0; i < width; i++)
                {
                    Fish f = GetFishAt(i, y);
                    if (f != null && f != special) affected.Add(f);
                }
                break;

            case SpecialType.RocketV:
                for (int i = 0; i < height; i++)
                {
                    Fish f = GetFishAt(x, i);
                    if (f != null && f != special) affected.Add(f);
                }
                break;

            case SpecialType.Bomb:
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        Fish f = GetFishAt(x + dx, y + dy);
                        if (f != null && f != special) affected.Add(f);
                    }
                break;

            case SpecialType.ColorBomb:
                Dictionary<FishType, int> counts = new Dictionary<FishType, int>();
                for (int gx = 0; gx < width; gx++)
                    for (int gy = 0; gy < height; gy++)
                    {
                        Fish ff = GetFishAt(gx, gy);
                        if (ff == null || ff == special || ff.IsSpecial) continue;
                        if (!counts.ContainsKey(ff.data.fishType)) counts[ff.data.fishType] = 0;
                        counts[ff.data.fishType]++;
                    }
                FishType target = FishType.Clownfish;
                int maxCount = 0;
                foreach (var kvp in counts)
                    if (kvp.Value > maxCount) { target = kvp.Key; maxCount = kvp.Value; }

                for (int gx = 0; gx < width; gx++)
                    for (int gy = 0; gy < height; gy++)
                    {
                        Fish ff = GetFishAt(gx, gy);
                        if (ff != null && ff != special && !ff.IsSpecial && ff.data.fishType == target)
                            affected.Add(ff);
                    }
                Debug.Log($"<color=cyan>🎨 ColorBomb temizliyor: {target} ({maxCount} tane)</color>");
                break;
        }
        return affected;
    }

    // ─────────────────────────────────────────────
    // SPECIAL COMBOS
    // ─────────────────────────────────────────────

    private bool IsRocket(SpecialType t)
    {
        return t == SpecialType.RocketH || t == SpecialType.RocketV;
    }

    public IEnumerator HandleSpecialCombo(Fish a, Fish b)
    {
        IsBusy = true;

        HashSet<Fish> toClear = new HashSet<Fish>();
        toClear.Add(a);
        toClear.Add(b);

        int ax = a.gridX, ay = a.gridY;
        SpecialType ta = a.specialType, tb = b.specialType;

        Debug.Log($"<color=cyan>🌟 COMBO: {ta} + {tb}</color>");

        if (ta == SpecialType.ColorBomb && tb == SpecialType.ColorBomb)
        {
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    Fish f = GetFishAt(x, y);
                    if (f != null) toClear.Add(f);
                }
        }
        else if (ta == SpecialType.ColorBomb || tb == SpecialType.ColorBomb)
        {
            Dictionary<FishType, int> counts = new Dictionary<FishType, int>();
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    Fish f = GetFishAt(x, y);
                    if (f == null || f.IsSpecial) continue;
                    if (!counts.ContainsKey(f.data.fishType)) counts[f.data.fishType] = 0;
                    counts[f.data.fishType]++;
                }
            FishType target = FishType.Clownfish;
            int max = 0;
            foreach (var kvp in counts)
                if (kvp.Value > max) { target = kvp.Key; max = kvp.Value; }

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    Fish f = GetFishAt(x, y);
                    if (f != null && f.data.fishType == target) toClear.Add(f);
                }
        }
        else if (IsRocket(ta) && IsRocket(tb))
        {
            for (int i = 0; i < width; i++) { Fish f = GetFishAt(i, ay); if (f != null) toClear.Add(f); }
            for (int i = 0; i < height; i++) { Fish f = GetFishAt(ax, i); if (f != null) toClear.Add(f); }
        }
        else if ((IsRocket(ta) && tb == SpecialType.Bomb) || (ta == SpecialType.Bomb && IsRocket(tb)))
        {
            for (int dy = -1; dy <= 1; dy++)
                for (int i = 0; i < width; i++)
                {
                    Fish f = GetFishAt(i, ay + dy);
                    if (f != null) toClear.Add(f);
                }
            for (int dx = -1; dx <= 1; dx++)
                for (int i = 0; i < height; i++)
                {
                    Fish f = GetFishAt(ax + dx, i);
                    if (f != null) toClear.Add(f);
                }
        }
        else if (ta == SpecialType.Bomb && tb == SpecialType.Bomb)
        {
            for (int dx = -2; dx <= 2; dx++)
                for (int dy = -2; dy <= 2; dy++)
                {
                    Fish f = GetFishAt(ax + dx, ay + dy);
                    if (f != null) toClear.Add(f);
                }
        }

        Queue<Fish> queue = new Queue<Fish>(toClear);
        HashSet<Fish> expanded = new HashSet<Fish>();
        while (queue.Count > 0)
        {
            Fish f = queue.Dequeue();
            if (f == null || expanded.Contains(f)) continue;
            expanded.Add(f);

            if (f.IsSpecial && f != a && f != b)
            {
                List<Fish> area = GetActivationArea(f);
                foreach (var x in area)
                    if (!expanded.Contains(x)) queue.Enqueue(x);
            }
        }

        int totalScore = 0;
        foreach (Fish f in expanded)
        {
            if (f == null) continue;
            totalScore += f.data.scoreValue * 2;
            LevelManager.Instance?.ReportFishCollected(f.data.fishType, 1);
            grid[f.gridX, f.gridY] = null;
            f.PopAndDestroy();
        }
        ScoreManager.Instance.AddScore(totalScore);

        yield return new WaitForSeconds(0.4f);
        yield return StartCoroutine(ApplyGravity());
        yield return StartCoroutine(RefillGrid());
        yield return StartCoroutine(ProcessMatches());

        IsBusy = false;
    }

    public IEnumerator ActivateColorBombOnType(Fish colorBomb, FishType target)
    {
        IsBusy = true;

        Debug.Log($"<color=cyan>🎨 ColorBomb activated → {target}</color>");

        HashSet<Fish> toClear = new HashSet<Fish>();
        toClear.Add(colorBomb);

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                Fish f = GetFishAt(x, y);
                if (f != null && f.data.fishType == target) toClear.Add(f);
            }

        Queue<Fish> queue = new Queue<Fish>(toClear);
        HashSet<Fish> expanded = new HashSet<Fish>();
        while (queue.Count > 0)
        {
            Fish f = queue.Dequeue();
            if (f == null || expanded.Contains(f)) continue;
            expanded.Add(f);

            if (f.IsSpecial && f != colorBomb)
            {
                List<Fish> area = GetActivationArea(f);
                foreach (var x in area)
                    if (!expanded.Contains(x)) queue.Enqueue(x);
            }
        }

        int totalScore = 0;
        foreach (Fish f in expanded)
        {
            if (f == null) continue;
            totalScore += Mathf.RoundToInt(f.data.scoreValue * 1.5f);
            LevelManager.Instance?.ReportFishCollected(f.data.fishType, 1);
            grid[f.gridX, f.gridY] = null;
            f.PopAndDestroy();
        }
        ScoreManager.Instance.AddScore(totalScore);

        yield return new WaitForSeconds(0.3f);
        yield return StartCoroutine(ApplyGravity());
        yield return StartCoroutine(RefillGrid());
        yield return StartCoroutine(ProcessMatches());

        IsBusy = false;
    }

    // ─────────────────────────────────────────────
    // NO-MOVES DETECTION + SHUFFLE
    // ─────────────────────────────────────────────

    public bool HasAnyValidMove()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x + 1 < width && WouldSwapCreateMatch(x, y, x + 1, y)) return true;
                if (y + 1 < height && WouldSwapCreateMatch(x, y, x, y + 1)) return true;
            }
        }
        return false;
    }

    private bool WouldSwapCreateMatch(int ax, int ay, int bx, int by)
    {
        Fish a = grid[ax, ay];
        Fish b = grid[bx, by];
        if (a == null || b == null) return false;

        if (a.IsSpecial || b.IsSpecial) return true;

        grid[ax, ay] = b;
        grid[bx, by] = a;
        int oldAx = a.gridX, oldAy = a.gridY;
        int oldBx = b.gridX, oldBy = b.gridY;
        a.gridX = bx; a.gridY = by;
        b.gridX = ax; b.gridY = ay;

        bool hasMatch = MatchFinder.Instance.HasMatchAt(bx, by)
                     || MatchFinder.Instance.HasMatchAt(ax, ay);

        grid[ax, ay] = a;
        grid[bx, by] = b;
        a.gridX = oldAx; a.gridY = oldAy;
        b.gridX = oldBx; b.gridY = oldBy;

        return hasMatch;
    }

    public IEnumerator ShuffleGrid()
    {
        Debug.Log("<color=yellow>🔀 SHUFFLE: Hamle yok, karıştırılıyor...</color>");

        List<Fish> allFish = new List<Fish>();
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (grid[x, y] != null) allFish.Add(grid[x, y]);

        int attempts = 0;
        while (attempts < 5)
        {
            for (int i = allFish.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (allFish[i], allFish[j]) = (allFish[j], allFish[i]);
            }

            int idx = 0;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    if (grid[x, y] != null)
                    {
                        Fish f = allFish[idx++];
                        grid[x, y] = f;
                        f.SetGridPosition(x, y);
                        f.MoveTo(GridToWorldPosition(x, y), 0.4f);
                    }
                }

            yield return new WaitForSeconds(0.5f);

            attempts++;
            if (HasAnyValidMove()) break;
        }

        Debug.Log("<color=lime>🔀 SHUFFLE: Tamamlandı</color>");
    }

    // ─────────────────────────────────────────────
    // LEVEL-BASED GRID RESET
    // ─────────────────────────────────────────────

    /// <summary>
    /// Level yüklendiğinde çağrılır. Grid'i temizler, yeni boyutla yeniden oluşturur.
    /// </summary>
    public void ResetForLevel(LevelData level)
    {
        ClearGrid();

        width = level.gridWidth;
        height = level.gridHeight;

        // Level'in kendi balık havuzu varsa kullan
        if (level.levelFishPool != null && level.levelFishPool.Length > 0)
            fishDataPool = level.levelFishPool;

        // Cell size'ı otomatik ölçekle
        cellSize = 5.5f / Mathf.Max(width, height);

        CreateGrid();

        Debug.Log($"<color=cyan>[Grid] Reset for {level.levelName}: {width}x{height}, {fishDataPool.Length} fish types</color>");
    }

    private void ClearGrid()
    {
        if (grid == null) return;

        int oldW = grid.GetLength(0);
        int oldH = grid.GetLength(1);
        for (int x = 0; x < oldW; x++)
            for (int y = 0; y < oldH; y++)
            {
                if (grid[x, y] != null)
                {
                    Destroy(grid[x, y].gameObject);
                    grid[x, y] = null;
                }
            }
    }
}