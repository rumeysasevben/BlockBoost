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
    public GameObject obstaclePrefab;
    public FishData[] fishDataPool = new FishData[0];

    [Header("Layout")]
    public Transform gridParent;

    [Header("State")]
    public bool IsBusy { get; private set; }

    private Fish[,] grid;
    private Dictionary<Vector2Int, Obstacle> obstacles = new Dictionary<Vector2Int, Obstacle>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start() { }

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
        if (fishDataPool == null || fishDataPool.Length == 0)
        {
            Debug.LogError("[GridManager] fishDataPool is empty! Assign fish data in Inspector.");
            return null;
        }
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
                    List<Fish> activated = GetActivationArea(f);
                    foreach (var a in activated)
                        if (a != null && !toClear.Contains(a))
                            queue.Enqueue(a);
                }
            }

            float sizeMultiplier = maxGroupLength >= 5 ? 2f : maxGroupLength >= 4 ? 1.5f : 1f;
            float totalMultiplier = sizeMultiplier * comboLevel;
            int totalScore = 0;
            List<Vector2Int> clearedPositions = new List<Vector2Int>();
            foreach (Fish f in toClear)
            {
                if (f == null) continue;
                clearedPositions.Add(new Vector2Int(f.gridX, f.gridY));
                totalScore += Mathf.RoundToInt(f.data.scoreValue * totalMultiplier);
                LevelManager.Instance?.ReportFishCollected(f.data.fishType, 1);
                grid[f.gridX, f.gridY] = null;
                f.PopAndDestroy();
            }
            ScoreManager.Instance.AddScore(totalScore);
            DamageAdjacentObstacles(clearedPositions);

            yield return new WaitForSeconds(0.3f);
            yield return StartCoroutine(FillBoard());

            firstIteration = false;
        }

        if (HasAnyValidMove() == false)
            yield return StartCoroutine(ShuffleGrid());

        IsBusy = false;
    }

    private IEnumerator FillBoard()
    {
        int safety = 12;
        while (safety-- > 0)
        {
            yield return StartCoroutine(ApplyGravity());
            int spawned = SpawnTopSectionFish();
            if (spawned == 0) break;
            yield return new WaitForSeconds(0.4f);
        }
        yield return StartCoroutine(ApplyGravity());
    }

    private IEnumerator ApplyGravity()
    {
        const float fallDuration = 0.3f;
        int maxIter = 20;
        bool anyMoved;

        do
        {
            anyMoved = false;

            for (int x = 0; x < width; x++)
            {
                int writeY = 0;
                for (int y = 0; y < height; y++)
                {
                    if (IsCellBlocked(x, y)) { writeY = y + 1; continue; }
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

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (IsCellBlocked(x, y)) continue;
                    if (grid[x, y] != null) continue;

                    bool canVerticalFill = false;
                    bool obstacleAbove = false;
                    for (int up = y + 1; up < height; up++)
                    {
                        if (IsCellBlocked(x, up)) { obstacleAbove = true; break; }
                        if (grid[x, up] != null) { canVerticalFill = true; break; }
                    }
                    if (canVerticalFill) continue;
                    if (!obstacleAbove) continue;

                    int firstDir = Random.value < 0.5f ? -1 : 1;
                    int[] dirs = { firstDir, -firstDir };
                    foreach (int d in dirs)
                    {
                        int srcX = x + d;
                        int srcY = y + 1;
                        if (srcX < 0 || srcX >= width || srcY >= height) continue;
                        if (IsCellBlocked(srcX, srcY)) continue;
                        if (grid[srcX, srcY] == null) continue;

                        Fish puller = grid[srcX, srcY];
                        grid[srcX, srcY] = null;
                        grid[x, y] = puller;
                        puller.SetGridPosition(x, y);
                        puller.MoveTo(GridToWorldPosition(x, y), fallDuration);
                        anyMoved = true;
                        break;
                    }
                }
            }

            if (anyMoved) yield return new WaitForSeconds(fallDuration);
            maxIter--;
        } while (anyMoved && maxIter > 0);
    }

    private int SpawnTopSectionFish()
    {
        const float fallDuration = 0.4f;
        int spawnedTotal = 0;

        for (int x = 0; x < width; x++)
        {
            int spawnOffset = 0;
            for (int y = 0; y < height; y++)
            {
                if (IsCellBlocked(x, y)) continue;
                if (grid[x, y] != null) continue;

                bool hasObstacleAbove = false;
                for (int oy = y + 1; oy < height; oy++)
                    if (IsCellBlocked(x, oy)) { hasObstacleAbove = true; break; }
                if (hasObstacleAbove) continue;

                Vector3 spawnPos = GridToWorldPosition(x, height + spawnOffset);
                Vector3 targetPos = GridToWorldPosition(x, y);

                GameObject obj = Instantiate(fishPrefab, spawnPos, Quaternion.identity, gridParent);
                Fish fish = obj.GetComponent<Fish>();
                fish.Initialize(GetSafeRandomFishData(x, y), x, y);
                fish.MoveTo(targetPos, fallDuration);

                grid[x, y] = fish;
                spawnOffset++;
                spawnedTotal++;
            }
        }
        return spawnedTotal;
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
                if (counts.Count == 0) break;
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
                break;
        }
        return affected;
    }

    private bool IsRocket(SpecialType t) { return t == SpecialType.RocketH || t == SpecialType.RocketV; }

    public IEnumerator HandleSpecialCombo(Fish a, Fish b)
    {
        IsBusy = true;
        HashSet<Fish> toClear = new HashSet<Fish>();
        toClear.Add(a);
        toClear.Add(b);
        int ax = a.gridX, ay = a.gridY;
        SpecialType ta = a.specialType, tb = b.specialType;

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
            if (counts.Count == 0)
            {
                IsBusy = false;
                yield break;
            }
            FishType target = FishType.Clownfish;
            int max = 0;
            foreach (var kvp in counts)
                if (kvp.Value > max) { target = kvp.Key; max = kvp.Value; }
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    Fish f = GetFishAt(x, y);
                    if (f != null && !f.IsSpecial && f.data.fishType == target) toClear.Add(f);
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
                for (int i = 0; i < width; i++) { Fish f = GetFishAt(i, ay + dy); if (f != null) toClear.Add(f); }
            for (int dx = -1; dx <= 1; dx++)
                for (int i = 0; i < height; i++) { Fish f = GetFishAt(ax + dx, i); if (f != null) toClear.Add(f); }
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
        List<Vector2Int> clearedPositions = new List<Vector2Int>();
        foreach (Fish f in expanded)
        {
            if (f == null) continue;
            clearedPositions.Add(new Vector2Int(f.gridX, f.gridY));
            totalScore += f.data.scoreValue * 2;
            LevelManager.Instance?.ReportFishCollected(f.data.fishType, 1);
            grid[f.gridX, f.gridY] = null;
            f.PopAndDestroy();
        }
        ScoreManager.Instance.AddScore(totalScore);
        DamageAdjacentObstacles(clearedPositions);

        yield return new WaitForSeconds(0.4f);
        yield return StartCoroutine(FillBoard());
        yield return StartCoroutine(ProcessMatches());

        IsBusy = false;
    }

    public IEnumerator ActivateColorBombOnType(Fish colorBomb, FishType target)
    {
        IsBusy = true;
        HashSet<Fish> toClear = new HashSet<Fish>();
        toClear.Add(colorBomb);
        bool hasTarget = false;
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                Fish f = GetFishAt(x, y);
                if (f != null && !f.IsSpecial && f.data.fishType == target)
                {
                    toClear.Add(f);
                    hasTarget = true;
                }
            }
        if (!hasTarget)
        {
            IsBusy = false;
            yield break;
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
        List<Vector2Int> clearedPositions = new List<Vector2Int>();
        foreach (Fish f in expanded)
        {
            if (f == null) continue;
            clearedPositions.Add(new Vector2Int(f.gridX, f.gridY));
            totalScore += Mathf.RoundToInt(f.data.scoreValue * 1.5f);
            LevelManager.Instance?.ReportFishCollected(f.data.fishType, 1);
            grid[f.gridX, f.gridY] = null;
            f.PopAndDestroy();
        }
        ScoreManager.Instance.AddScore(totalScore);
        DamageAdjacentObstacles(clearedPositions);

        yield return new WaitForSeconds(0.3f);
        yield return StartCoroutine(FillBoard());
        yield return StartCoroutine(ProcessMatches());

        IsBusy = false;
    }

    public bool HasAnyValidMove()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                if (x + 1 < width && WouldSwapCreateMatch(x, y, x + 1, y)) return true;
                if (y + 1 < height && WouldSwapCreateMatch(x, y, x, y + 1)) return true;
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
                    if (grid[x, y] != null)
                    {
                        Fish f = allFish[idx++];
                        grid[x, y] = f;
                        f.SetGridPosition(x, y);
                        f.MoveTo(GridToWorldPosition(x, y), 0.4f);
                    }
            yield return new WaitForSeconds(0.5f);
            attempts++;
            if (HasAnyValidMove()) break;
        }
    }

    // ─── LEVEL RESET ─────────────────────────

    public void ResetForLevel(LevelData level)
    {
        ClearGrid();
        ClearObstacles();

        width = level.gridWidth;
        height = level.gridHeight;

        if (level.levelFishPool != null && level.levelFishPool.Length > 0)
            fishDataPool = level.levelFishPool;

        cellSize = 5.5f / Mathf.Max(width, height);

        grid = new Fish[width, height];

        SpawnObstacles(level.obstacles);
        SpawnRandomObstacles(level.randomObstacles);

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (!IsCellBlocked(x, y))
                    SpawnFishAt(x, y);

        Debug.Log($"<color=cyan>[Grid] {level.levelName}: {width}x{height}, {obstacles.Count} obstacles</color>");
    }

    private void ClearGrid()
    {
        if (grid == null) return;
        int oldW = grid.GetLength(0);
        int oldH = grid.GetLength(1);
        for (int x = 0; x < oldW; x++)
            for (int y = 0; y < oldH; y++)
                if (grid[x, y] != null)
                {
                    Destroy(grid[x, y].gameObject);
                    grid[x, y] = null;
                }
    }

    // ─── OBSTACLES ─────────────────────────

    public bool IsCellBlocked(int x, int y)
    {
        return obstacles.ContainsKey(new Vector2Int(x, y));
    }

    private void SpawnObstacles(List<ObstaclePlacement> placements)
    {
        if (placements == null || obstaclePrefab == null) return;
        foreach (var p in placements)
        {
            if (p.gridX < 0 || p.gridX >= width || p.gridY < 0 || p.gridY >= height) continue;
            SpawnSingleObstacle(p.gridX, p.gridY, p.type);
        }
    }

    private void SpawnRandomObstacles(List<RandomObstacleSpec> specs)
    {
        if (specs == null || obstaclePrefab == null) return;

        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int x = 0; x < width; x++)
            for (int y = 1; y < height; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (!obstacles.ContainsKey(pos))
                    candidates.Add(pos);
            }

        foreach (var spec in specs)
        {
            for (int i = 0; i < spec.count; i++)
            {
                if (candidates.Count == 0) return;
                int idx = Random.Range(0, candidates.Count);
                Vector2Int pos = candidates[idx];
                candidates.RemoveAt(idx);
                SpawnSingleObstacle(pos.x, pos.y, spec.type);
            }
        }
    }

    private void SpawnSingleObstacle(int x, int y, ObstacleType type)
    {
        Vector3 worldPos = GridToWorldPosition(x, y);
        GameObject obj = Instantiate(obstaclePrefab, worldPos, Quaternion.identity, gridParent);
        Obstacle obs = obj.GetComponent<Obstacle>();
        obs.Initialize(type, x, y);
        obstacles[new Vector2Int(x, y)] = obs;
    }

    private void ClearObstacles()
    {
        foreach (var kvp in obstacles)
            if (kvp.Value != null) Destroy(kvp.Value.gameObject);
        obstacles.Clear();
    }

    public void DamageObstacleAt(int x, int y)
    {
        Vector2Int key = new Vector2Int(x, y);
        if (!obstacles.TryGetValue(key, out Obstacle obs)) return;
        if (obs == null) return;
        bool broken = obs.TakeDamage();
        if (broken)
        {
            obstacles.Remove(key);
            LevelManager.Instance?.ReportObstacleCleared(obs.type, 1);
        }
    }

    /// <summary>
    /// Verilen pozisyonların 4 komşusundaki obstacle'lara birer hasar verir.
    /// Bir obstacle bir çağrıda en fazla 1 hasar alır (dedupe).
    /// </summary>
    private void DamageAdjacentObstacles(IEnumerable<Vector2Int> clearedPositions)
    {
        HashSet<Vector2Int> toDamage = new HashSet<Vector2Int>();
        int[] dx = { 0, 0, -1, 1 };
        int[] dy = { 1, -1, 0, 0 };

        foreach (var pos in clearedPositions)
        {
            for (int i = 0; i < 4; i++)
            {
                int nx = pos.x + dx[i];
                int ny = pos.y + dy[i];
                if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                if (IsCellBlocked(nx, ny)) toDamage.Add(new Vector2Int(nx, ny));
            }
        }
        foreach (var p in toDamage) DamageObstacleAt(p.x, p.y);
    }
}