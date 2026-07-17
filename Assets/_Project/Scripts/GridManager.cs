using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

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
    public GameObject collectiblePrefab;
    public GameObject fishingNetPrefab;
    public FishData[] fishDataPool = new FishData[0];

    [Header("Layout")]
    public Transform gridParent;

    [Header("State")]
    public bool IsBusy { get; set; }

    private Fish[,] grid;
    private float idleTimer = 0f;
    private const float idleDelay = 3.5f;
    private Dictionary<Vector2Int, Obstacle> obstacles = new Dictionary<Vector2Int, Obstacle>();
    private Dictionary<Vector2Int, Collectible> collectibles = new Dictionary<Vector2Int, Collectible>();
    private Dictionary<Vector2Int, FishingNet> nets = new Dictionary<Vector2Int, FishingNet>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start() { }

    private void Update()
    {
        // Oyun meşgulse (swap/patlama/düşme) idle sayacı çalışmaz
        if (IsBusy || grid == null)
        {
            idleTimer = 0f;
            return;
        }

        idleTimer += Time.deltaTime;
        if (idleTimer >= idleDelay)
        {
            idleTimer = 0f;   // sayacı sıfırla, tekrar saymaya başlasın
            PlayIdleWave();
        }
    }

    // Tüm balıkları hafif gecikmeli, dalga gibi sallar
    private void PlayIdleWave()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                Fish f = grid[x, y];
                if (f == null) continue;
                // hafif gecikme: soldan sağa dalga etkisi
                float delay = (x + y) * 0.05f;
                DOTween.Sequence().AppendInterval(delay).AppendCallback(() =>
                {
                    if (f != null) f.PlayIdle();
                });
            }
    }

    private void SpawnFishAt(int x, int y)
    {
        Vector3 worldPos = GridToWorldPosition(x, y);
        GameObject obj = Instantiate(fishPrefab, worldPos, Quaternion.identity, gridParent);
        Fish fish = obj.GetComponent<Fish>();
        FishData safeData = GetSafeRandomFishData(x, y);
        fish.Initialize(safeData, x, y, cellSize);
        grid[x, y] = fish;
    }

    private FishData GetRandomFishData()
    {
        if (fishDataPool == null || fishDataPool.Length == 0) return null;
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
        if (HasNetAt(a.gridX, a.gridY) || HasNetAt(b.gridX, b.gridY)) return;
        int ax = a.gridX, ay = a.gridY;
        int bx = b.gridX, by = b.gridY;
        grid[ax, ay] = b;
        grid[bx, by] = a;
        a.SetGridPosition(bx, by);
        b.SetGridPosition(ax, ay);
        a.transform.position = GridToWorldPosition(bx, by);
        b.transform.position = GridToWorldPosition(ax, ay);
    }

    public IEnumerator SwapFishAnimated(Fish a, Fish b, float duration = 0.2f, bool playSound = true)
    {
        idleTimer = 0f;
        if (HasNetAt(a.gridX, a.gridY) || HasNetAt(b.gridX, b.gridY)) yield break;
        int ax = a.gridX, ay = a.gridY;
        int bx = b.gridX, by = b.gridY;
        grid[ax, ay] = b;
        grid[bx, by] = a;
        a.SetGridPosition(bx, by);
        b.SetGridPosition(ax, ay);

        if (playSound) AudioManager.Instance?.PlaySwap();

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
                AudioManager.Instance?.PlaySpecialCreate();
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
                AudioManager.Instance?.PlaySpecialCreate();
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
                Vector3 wpos = GridToWorldPosition(f.gridX, f.gridY);
                int fishScore = Mathf.RoundToInt(f.data.scoreValue * totalMultiplier);
                Color burstColor = f.IsSpecial ? new Color(1f, 0.85f, 0.2f) : new Color(0.6f, 0.9f, 1f);

                MatchVFXManager.Instance?.SpawnBurst(wpos, burstColor);
                MatchVFXManager.Instance?.SpawnScorePopup(wpos, fishScore, burstColor);

                clearedPositions.Add(new Vector2Int(f.gridX, f.gridY));
                totalScore += fishScore;
                LevelManager.Instance?.ReportFishCollected(f.data.fishType, 1);
                grid[f.gridX, f.gridY] = null;
                f.PopAndDestroy();
            }
            ScoreManager.Instance.AddScore(totalScore);
            AudioManager.Instance?.PlayMatch(comboLevel);
            DamageAdjacentObstaclesAndNets(clearedPositions);

            // Camera shake — büyük match veya combo'da
            if (comboLevel >= 2 || toClear.Count >= 6)
                CameraShake.Instance?.Shake(0.15f + comboLevel * 0.02f, 0.04f + comboLevel * 0.008f);

            // Combo feedback yazisi (Fin-tastic! / Splash-tastic! / ...) — grid merkezinde
            if (comboLevel >= 2)
                MatchVFXManager.Instance?.SpawnComboText(comboLevel);

            yield return new WaitForSeconds(0.3f);
            yield return StartCoroutine(FillBoard());
            yield return StartCoroutine(DeliverCollectiblesAtBottom());

            firstIteration = false;
        }

        if (HasAnyValidMove() == false)
            yield return StartCoroutine(ShuffleGrid());

        IsBusy = false;
    }

    // ─── BOOSTER ACTIONS ─────────────────────────

    public IEnumerator HammerCellAt(int x, int y)
    {
        IsBusy = true;
        AudioManager.Instance?.PlayHammer();

        Fish f = GetFishAt(x, y);
        if (f != null)
        {
            Vector3 wpos = GridToWorldPosition(x, y);
            Color burstColor = new Color(1f, 0.7f, 0.2f);

            MatchVFXManager.Instance?.SpawnBurst(wpos, burstColor);
            MatchVFXManager.Instance?.SpawnScorePopup(wpos, f.data.scoreValue, burstColor);
            CameraShake.Instance?.Shake(0.15f, 0.04f);

            List<Vector2Int> cleared = new List<Vector2Int> { new Vector2Int(x, y) };
            ScoreManager.Instance.AddScore(f.data.scoreValue);
            LevelManager.Instance?.ReportFishCollected(f.data.fishType, 1);
            grid[x, y] = null;
            f.PopAndDestroy();
            DamageAdjacentObstaclesAndNets(cleared);
            yield return new WaitForSeconds(0.2f);
            yield return StartCoroutine(FillBoard());
            yield return StartCoroutine(DeliverCollectiblesAtBottom());
            yield return StartCoroutine(ProcessMatches());
        }
        IsBusy = false;
    }

    public IEnumerator RocketCellAt(int x, int y)
    {
        IsBusy = true;

        // Booster roket feedback yazisi
        MatchVFXManager.Instance?.SpawnSpecialText(SpecialType.RocketH);
        AudioManager.Instance?.PlaySpecial(SpecialType.RocketH);

        HashSet<Fish> toClear = new HashSet<Fish>();
        for (int i = 0; i < width; i++)  { Fish f = GetFishAt(i, y); if (f != null) toClear.Add(f); }
        for (int i = 0; i < height; i++) { Fish f = GetFishAt(x, i); if (f != null) toClear.Add(f); }

        Queue<Fish> queue = new Queue<Fish>(toClear);
        HashSet<Fish> expanded = new HashSet<Fish>();
        while (queue.Count > 0)
        {
            Fish f = queue.Dequeue();
            if (f == null || expanded.Contains(f)) continue;
            expanded.Add(f);
            if (f.IsSpecial)
            {
                List<Fish> area = GetActivationArea(f);
                foreach (var aa in area)
                    if (!expanded.Contains(aa)) queue.Enqueue(aa);
            }
        }

        int totalScore = 0;
        List<Vector2Int> clearedPositions = new List<Vector2Int>();
        foreach (Fish f in expanded)
        {
            if (f == null) continue;
            Vector3 wpos = GridToWorldPosition(f.gridX, f.gridY);
            int fishScore = f.data.scoreValue * 2;
            Color burstColor = new Color(1f, 0.6f, 0.2f);

            MatchVFXManager.Instance?.SpawnBurst(wpos, burstColor);
            MatchVFXManager.Instance?.SpawnScorePopup(wpos, fishScore, burstColor);

            clearedPositions.Add(new Vector2Int(f.gridX, f.gridY));
            totalScore += fishScore;
            LevelManager.Instance?.ReportFishCollected(f.data.fishType, 1);
            grid[f.gridX, f.gridY] = null;
            f.PopAndDestroy();
        }
        ScoreManager.Instance.AddScore(totalScore);
        DamageAdjacentObstaclesAndNets(clearedPositions);

        CameraShake.Instance?.Shake(0.2f, 0.06f);

        yield return new WaitForSeconds(0.4f);
        yield return StartCoroutine(FillBoard());
        yield return StartCoroutine(DeliverCollectiblesAtBottom());
        yield return StartCoroutine(ProcessMatches());
        IsBusy = false;
    }

    // Bir special tile'ı (Rocket/Bomb) bulunduğu konumda aktive eder.
    // Swap ile tetiklendiğinde çağrılır (match gerektirmez).
    public IEnumerator ActivateSpecialAt(Fish special)
    {
        IsBusy = true;

        // Special feedback yazisi (Torpedo! / Boom-arine! / Reef Wrecker!)
        MatchVFXManager.Instance?.SpawnSpecialText(special.specialType);
        AudioManager.Instance?.PlaySpecial(special.specialType);

        HashSet<Fish> toClear = new HashSet<Fish>();
        toClear.Add(special);

        // Special'ın kendi aktivasyon alanını al
        List<Fish> initialArea = GetActivationArea(special);
        foreach (var f in initialArea)
            if (f != null) toClear.Add(f);

        // Zincirleme: alan içinde başka special varsa onları da patlat
        Queue<Fish> queue = new Queue<Fish>(toClear);
        HashSet<Fish> expanded = new HashSet<Fish>();
        while (queue.Count > 0)
        {
            Fish f = queue.Dequeue();
            if (f == null || expanded.Contains(f)) continue;
            expanded.Add(f);
            if (f.IsSpecial && f != special)
            {
                List<Fish> area = GetActivationArea(f);
                foreach (var aa in area)
                    if (aa != null && !expanded.Contains(aa)) queue.Enqueue(aa);
            }
        }

        int totalScore = 0;
        List<Vector2Int> clearedPositions = new List<Vector2Int>();
        foreach (Fish f in expanded)
        {
            if (f == null) continue;
            Vector3 wpos = GridToWorldPosition(f.gridX, f.gridY);
            int fishScore = f.data.scoreValue * 2;
            Color burstColor = new Color(1f, 0.6f, 0.2f);

            MatchVFXManager.Instance?.SpawnBurst(wpos, burstColor);
            MatchVFXManager.Instance?.SpawnScorePopup(wpos, fishScore, burstColor);

            clearedPositions.Add(new Vector2Int(f.gridX, f.gridY));
            totalScore += fishScore;
            LevelManager.Instance?.ReportFishCollected(f.data.fishType, 1);
            grid[f.gridX, f.gridY] = null;
            f.PopAndDestroy();
        }
        ScoreManager.Instance.AddScore(totalScore);
        DamageAdjacentObstaclesAndNets(clearedPositions);

        CameraShake.Instance?.Shake(0.2f, 0.06f);

        yield return new WaitForSeconds(0.4f);
        yield return StartCoroutine(FillBoard());
        yield return StartCoroutine(DeliverCollectiblesAtBottom());
        yield return StartCoroutine(ProcessMatches());

        IsBusy = false;
    }

    // ─── REST OF METHODS ─────────────────────────

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
                    else if (HasCollectibleAt(x, y))
                    {
                        if (y != writeY)
                        {
                            Collectible c = collectibles[new Vector2Int(x, y)];
                            collectibles.Remove(new Vector2Int(x, y));
                            collectibles[new Vector2Int(x, writeY)] = c;
                            c.SetGridPosition(x, writeY);
                            c.MoveTo(GridToWorldPosition(x, writeY), fallDuration);
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
                    if (HasCollectibleAt(x, y)) continue;

                    bool canVerticalFill = false;
                    bool obstructed = false;
                    for (int up = y + 1; up < height; up++)
                    {
                        if (IsCellBlocked(x, up) || HasCollectibleAt(x, up)) { obstructed = true; break; }
                        if (grid[x, up] != null) { canVerticalFill = true; break; }
                    }
                    if (canVerticalFill) continue;
                    if (!obstructed) continue;

                    int firstDir = Random.value < 0.5f ? -1 : 1;
                    int[] dirs = { firstDir, -firstDir };
                    foreach (int d in dirs)
                    {
                        int srcX = x + d;
                        int srcY = y + 1;
                        if (srcX < 0 || srcX >= width || srcY >= height) continue;
                        if (IsCellBlocked(srcX, srcY)) continue;
                        if (HasCollectibleAt(srcX, srcY)) continue;
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
                if (HasCollectibleAt(x, y)) continue;
                if (grid[x, y] != null) continue;
                bool blockedAbove = false;
                for (int oy = y + 1; oy < height; oy++)
                    if (IsCellBlocked(x, oy) || HasCollectibleAt(x, oy)) { blockedAbove = true; break; }
                if (blockedAbove) continue;

                Vector3 spawnPos = GridToWorldPosition(x, height + spawnOffset);
                Vector3 targetPos = GridToWorldPosition(x, y);
                GameObject obj = Instantiate(fishPrefab, spawnPos, Quaternion.identity, gridParent);
                Fish fish = obj.GetComponent<Fish>();
                fish.Initialize(GetSafeRandomFishData(x, y), x, y, cellSize);
                fish.MoveTo(targetPos, fallDuration);
                grid[x, y] = fish;
                spawnOffset++;
                spawnedTotal++;
            }
        }
        return spawnedTotal;
    }

    private IEnumerator DeliverCollectiblesAtBottom()
    {
        bool anyDelivered = false;
        int keysDelivered = 0;

        List<Vector2Int> toDeliver = new List<Vector2Int>();
        for (int x = 0; x < width; x++)
        {
            Vector2Int key = new Vector2Int(x, 0);
            if (collectibles.ContainsKey(key)) toDeliver.Add(key);
        }

        foreach (var key in toDeliver)
        {
            Collectible c = collectibles[key];
            collectibles.Remove(key);
            LevelManager.Instance?.ReportCollectibleDelivered(c.type, 1);

            if (c.type == CollectibleType.Key) keysDelivered++;

            AudioManager.Instance?.PlayCollectibleDeliver();
            c.DeliverAndDestroy();
            anyDelivered = true;
        }

        // Teslim edilen her anahtar rastgele bir kafesi acar
        for (int i = 0; i < keysDelivered; i++)
        {
            yield return new WaitForSeconds(0.25f);
            UnlockRandomCage();
        }

        if (anyDelivered)
        {
            yield return new WaitForSeconds(0.5f);
            yield return StartCoroutine(FillBoard());
        }
    }

    // Rastgele bir Cage'i kirar; icindeki baligi serbest birakir.
    private void UnlockRandomCage()
    {
        List<Vector2Int> cages = new List<Vector2Int>();
        foreach (var kvp in obstacles)
            if (kvp.Value != null && kvp.Value.type == ObstacleType.Cage)
                cages.Add(kvp.Key);

        if (cages.Count == 0) return;

        Vector2Int target = cages[Random.Range(0, cages.Count)];
        Obstacle cage = obstacles[target];

        // HP ne olursa olsun tamamen kir
        obstacles.Remove(target);
        LevelManager.Instance?.ReportObstacleCleared(ObstacleType.Cage, 1);
        AudioManager.Instance?.PlayObstacleBreak(ObstacleType.Cage);

        Vector3 wpos = GridToWorldPosition(target.x, target.y);
        MatchVFXManager.Instance?.SpawnBurst(wpos, new Color(1f, 0.9f, 0.3f));
        CameraShake.Instance?.Shake(0.15f, 0.04f);

        if (cage != null)
        {
            cage.transform.DOKill();
            Vector3 s = cage.transform.localScale;
            Sequence seq = DOTween.Sequence();
            seq.Append(cage.transform.DOScale(s * 1.4f, 0.15f));
            seq.Append(cage.transform.DOScale(0f, 0.25f).SetEase(Ease.InBack));
            seq.OnComplete(() => { if (cage != null) Destroy(cage.gameObject); });
        }

        // Kafesteki balık yüzerek kaçsın, sonra yerine yeni balık düşsün
        SpawnEscapingFishThenRefill(target.x, target.y);
    }

    // Kafes kırılınca içindeki balık yüzerek ekrandan kaçar, sonra yerine yeni balık düşer
    private void SpawnEscapingFishThenRefill(int x, int y)
    {
        Vector3 startPos = GridToWorldPosition(x, y);

        // Geçici bir kaçan balık oluştur (grid'e KAYDEDİLMEZ, sadece görsel)
        GameObject obj = Instantiate(fishPrefab, startPos, Quaternion.identity, gridParent);
        Fish escaper = obj.GetComponent<Fish>();
        escaper.Initialize(GetRandomFishData(), x, y, cellSize);

        // Collider/etkileşimi kapat (sadece animasyon objesi)
        var col = obj.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Rastgele bir yöne yüzerek kaç
        // Yukarı doğru rastgele bir yöne yüz (su yüzeyine kaçar gibi)
        float angle = Random.Range(30f, 150f) * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
        Vector3 escapeTarget = startPos + dir * (cellSize * 4f);

        obj.transform.DOKill();
        // hafif dönerek + solarak yüzsün
        obj.transform.DOMove(escapeTarget, 0.8f).SetEase(Ease.InQuad);
        obj.transform.DORotate(new Vector3(0, 0, Random.Range(-90f, 90f)), 0.8f);
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null) sr.DOFade(0f, 0.8f).SetEase(Ease.InQuad);
        Destroy(obj, 0.85f);

        // Kaçış başladıktan kısa süre sonra yerine yeni balık düşür
        DOVirtual.DelayedCall(0.4f, () =>
        {
            if (grid == null) return;
            if (x < 0 || x >= width || y < 0 || y >= height) return;
            if (grid[x, y] != null) return; // zaten doluysa dokunma

            SpawnFishAt(x, y);
            Fish spawned = GetFishAt(x, y);
            if (spawned != null)
            {
                Vector3 correctScale = spawned.transform.localScale;
                spawned.transform.localScale = Vector3.zero;
                spawned.transform.DOScale(correctScale, 0.3f).SetEase(Ease.OutBack);
            }
        });
    }

    private List<Fish> GetActivationArea(Fish special)
    {
        List<Fish> affected = new List<Fish>();
        int x = special.gridX, y = special.gridY;
        switch (special.specialType)
        {
            case SpecialType.RocketH:
                for (int i = 0; i < width; i++) { Fish f = GetFishAt(i, y); if (f != null && f != special) affected.Add(f); }
                break;
            case SpecialType.RocketV:
                for (int i = 0; i < height; i++) { Fish f = GetFishAt(x, i); if (f != null && f != special) affected.Add(f); }
                break;
            case SpecialType.Bomb:
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    { Fish f = GetFishAt(x + dx, y + dy); if (f != null && f != special) affected.Add(f); }
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
        toClear.Add(a); toClear.Add(b);
        int ax = a.gridX, ay = a.gridY;
        SpecialType ta = a.specialType, tb = b.specialType;

        // Iki special birlesti — en guclu feedback
        MatchVFXManager.Instance?.SpawnKrakenText();
        AudioManager.Instance?.PlayKraken();

        if (ta == SpecialType.ColorBomb && tb == SpecialType.ColorBomb)
        {
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                { Fish f = GetFishAt(x, y); if (f != null) toClear.Add(f); }
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
            if (counts.Count == 0) { IsBusy = false; yield break; }
            FishType target = FishType.Clownfish;
            int max = 0;
            foreach (var kvp in counts) if (kvp.Value > max) { target = kvp.Key; max = kvp.Value; }
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    Fish f = GetFishAt(x, y);
                    if (f != null && !f.IsSpecial && f.data.fishType == target) toClear.Add(f);
                }
        }
        else if (IsRocket(ta) && IsRocket(tb))
        {
            for (int i = 0; i < width; i++)  { Fish f = GetFishAt(i, ay); if (f != null) toClear.Add(f); }
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
                { Fish f = GetFishAt(ax + dx, ay + dy); if (f != null) toClear.Add(f); }
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
                foreach (var x in area) if (!expanded.Contains(x)) queue.Enqueue(x);
            }
        }

        int totalScore = 0;
        List<Vector2Int> clearedPositions = new List<Vector2Int>();
        foreach (Fish f in expanded)
        {
            if (f == null) continue;
            Vector3 wpos = GridToWorldPosition(f.gridX, f.gridY);
            int fishScore = f.data.scoreValue * 2;
            Color burstColor = new Color(1f, 0.6f, 0.2f);

            MatchVFXManager.Instance?.SpawnBurst(wpos, burstColor);
            MatchVFXManager.Instance?.SpawnScorePopup(wpos, fishScore, burstColor);

            clearedPositions.Add(new Vector2Int(f.gridX, f.gridY));
            totalScore += fishScore;
            LevelManager.Instance?.ReportFishCollected(f.data.fishType, 1);
            grid[f.gridX, f.gridY] = null;
            f.PopAndDestroy();
        }
        ScoreManager.Instance.AddScore(totalScore);
        DamageAdjacentObstaclesAndNets(clearedPositions);

        CameraShake.Instance?.Shake(0.2f, 0.06f);

        yield return new WaitForSeconds(0.4f);
        yield return StartCoroutine(FillBoard());
        yield return StartCoroutine(DeliverCollectiblesAtBottom());
        yield return StartCoroutine(ProcessMatches());

        IsBusy = false;
    }

    public IEnumerator ActivateColorBombOnType(Fish colorBomb, FishType target)
    {
        IsBusy = true;

        // ColorBomb feedback yazisi
        MatchVFXManager.Instance?.SpawnSpecialText(SpecialType.ColorBomb);
        AudioManager.Instance?.PlaySpecial(SpecialType.ColorBomb);

        HashSet<Fish> toClear = new HashSet<Fish>();
        toClear.Add(colorBomb);
        bool hasTarget = false;
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                Fish f = GetFishAt(x, y);
                if (f != null && !f.IsSpecial && f.data.fishType == target)
                {
                    toClear.Add(f); hasTarget = true;
                }
            }
        if (!hasTarget) { IsBusy = false; yield break; }

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
                foreach (var x in area) if (!expanded.Contains(x)) queue.Enqueue(x);
            }
        }

        int totalScore = 0;
        List<Vector2Int> clearedPositions = new List<Vector2Int>();
        foreach (Fish f in expanded)
        {
            if (f == null) continue;
            Vector3 wpos = GridToWorldPosition(f.gridX, f.gridY);
            int fishScore = Mathf.RoundToInt(f.data.scoreValue * 1.5f);
            Color burstColor = new Color(1f, 0.4f, 0.7f);

            MatchVFXManager.Instance?.SpawnBurst(wpos, burstColor);
            MatchVFXManager.Instance?.SpawnScorePopup(wpos, fishScore, burstColor);

            clearedPositions.Add(new Vector2Int(f.gridX, f.gridY));
            totalScore += fishScore;
            LevelManager.Instance?.ReportFishCollected(f.data.fishType, 1);
            grid[f.gridX, f.gridY] = null;
            f.PopAndDestroy();
        }
        ScoreManager.Instance.AddScore(totalScore);
        DamageAdjacentObstaclesAndNets(clearedPositions);

        CameraShake.Instance?.Shake(0.2f, 0.05f);

        yield return new WaitForSeconds(0.3f);
        yield return StartCoroutine(FillBoard());
        yield return StartCoroutine(DeliverCollectiblesAtBottom());
        yield return StartCoroutine(ProcessMatches());

        IsBusy = false;
    }

    public bool HasAnyValidMove()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                if (HasNetAt(x, y)) continue;
                if (x + 1 < width && !HasNetAt(x + 1, y) && WouldSwapCreateMatch(x, y, x + 1, y)) return true;
                if (y + 1 < height && !HasNetAt(x, y + 1) && WouldSwapCreateMatch(x, y, x, y + 1)) return true;
            }
        return false;
    }

    private bool WouldSwapCreateMatch(int ax, int ay, int bx, int by)
    {
        Fish a = grid[ax, ay];
        Fish b = grid[bx, by];
        if (a == null || b == null) return false;
        if (a.IsSpecial || b.IsSpecial) return true;
        grid[ax, ay] = b; grid[bx, by] = a;
        int oldAx = a.gridX, oldAy = a.gridY;
        int oldBx = b.gridX, oldBy = b.gridY;
        a.gridX = bx; a.gridY = by;
        b.gridX = ax; b.gridY = ay;
        bool hasMatch = MatchFinder.Instance.HasMatchAt(bx, by) || MatchFinder.Instance.HasMatchAt(ax, ay);
        grid[ax, ay] = a; grid[bx, by] = b;
        a.gridX = oldAx; a.gridY = oldAy;
        b.gridX = oldBx; b.gridY = oldBy;
        return hasMatch;
    }

   public IEnumerator ShuffleGrid()
    {
        AudioManager.Instance?.PlayShuffle();

        // idle animasyonunu durdur (çakışmayı önle)
        idleTimer = 0f;

        int attempts = 0;
        while (attempts < 10)
        {
            attempts++;

            // Her turda grid'i TAZE topla (eski listeyi tekrar kullanma!)
            List<Fish> allFish = new List<Fish>();
            List<Vector2Int> slots = new List<Vector2Int>();
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (grid[x, y] != null && !HasNetAt(x, y))
                    {
                        allFish.Add(grid[x, y]);
                        slots.Add(new Vector2Int(x, y));
                    }

            if (allFish.Count == 0) yield break;

            // Karıştır
            for (int i = allFish.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (allFish[i], allFish[j]) = (allFish[j], allFish[i]);
            }

            // Karışık balıkları slotlara yerleştir
            for (int i = 0; i < slots.Count; i++)
            {
                Vector2Int s = slots[i];
                Fish f = allFish[i];
                grid[s.x, s.y] = f;
                f.SetGridPosition(s.x, s.y);
                f.transform.DOKill();  // önceki tween'i temizle
                f.MoveTo(GridToWorldPosition(s.x, s.y), 0.4f);
            }

            yield return new WaitForSeconds(0.5f);

            // Geçerli hamle varsa dur
            if (HasAnyValidMove()) break;
        }

        // Shuffle sonrası tesadüfen match oluştuysa patlat
        if (HasAnyMatchNow())
            yield return StartCoroutine(ProcessMatches());
    }

    // Faz 1: kalan hamle kadar rastgele balığı special yap
    // Faz 2: hepsini sırayla patlat
    public IEnumerator ConvertMovesToBonus(int remainingMoves)
    {
        IsBusy = true;

        // ── FAZ 1: Dönüştürme ──
        List<Fish> converted = new List<Fish>();

        for (int m = 0; m < remainingMoves; m++)
        {
            List<Fish> candidates = new List<Fish>();
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    Fish f = grid[x, y];
                    if (f != null && !f.IsSpecial && !HasNetAt(x, y))
                        candidates.Add(f);
                }

            if (candidates.Count == 0) break;

            Fish chosen = candidates[Random.Range(0, candidates.Count)];

            int roll = Random.Range(0, 3);
            SpecialType type = roll == 0 ? SpecialType.RocketH
                             : roll == 1 ? SpecialType.RocketV
                             : SpecialType.Bomb;

            chosen.MakeSpecial(type);
            AudioManager.Instance?.PlaySpecialCreate();
            converted.Add(chosen);

            LevelManager.Instance?.DecrementMoveForBonus();

            yield return new WaitForSeconds(0.2f);
        }

        yield return new WaitForSeconds(0.4f);

        // ── FAZ 2: Sırayla patlatma ──
        // Her special'ı TEK TEK, sadece kendi etki alanıyla patlat.
        // Diğer special'lara zincirleme dokunma → hepsi sırayla patlar, boşta kalmaz.
        // Henüz patlamamış tüm converted balıkları takip et (kendi sırası gelene kadar dokunma)
        HashSet<Fish> notYetExploded = new HashSet<Fish>(converted);

        foreach (Fish special in converted)
        {
            if (special == null) continue;
            if (special.gridX < 0 || special.gridX >= width || special.gridY < 0 || special.gridY >= height) continue;
            if (grid[special.gridX, special.gridY] != special) continue;

            notYetExploded.Remove(special);  // bu bomba artık patlıyor

            // Sadece bu special'ın kendi etki alanı, henüz sırası gelmemiş diğer bombalar HARİÇ
            HashSet<Fish> toClear = new HashSet<Fish>();
            toClear.Add(special);
            foreach (var a in GetActivationArea(special))
            {
                if (a == null) continue;
                if (notYetExploded.Contains(a)) continue;  // sırası gelmemiş bombayı atla
                toClear.Add(a);
            }

            AudioManager.Instance?.PlaySpecial(special.specialType);
            CameraShake.Instance?.Shake(0.18f, 0.05f);

            int totalScore = 0;
            foreach (Fish f in toClear)
            {
                if (f == null) continue;
                if (f.gridX < 0 || f.gridX >= width || f.gridY < 0 || f.gridY >= height) continue;
                if (grid[f.gridX, f.gridY] != f) continue;

                Vector3 wpos = GridToWorldPosition(f.gridX, f.gridY);
                int fishScore = f.data.scoreValue * 2;
                Color burstColor = new Color(1f, 0.6f, 0.2f);

                MatchVFXManager.Instance?.SpawnBurst(wpos, burstColor);
                MatchVFXManager.Instance?.SpawnScorePopup(wpos, fishScore, burstColor);

                totalScore += fishScore;
                grid[f.gridX, f.gridY] = null;
                f.PopAndDestroy();
            }
            ScoreManager.Instance.AddScore(totalScore);

            // Patlama görünsün
            yield return new WaitForSeconds(0.35f);

            // Boşalan yerleri doldur ki sonraki special boş ekranda kalmasın
            yield return StartCoroutine(FillBoard());
            yield return new WaitForSeconds(0.2f);
        }

        yield return new WaitForSeconds(0.3f);
        IsBusy = false;
    }

    // Su anki dizilimde patlamayi bekleyen bir eslesme var mi?
    private bool HasAnyMatchNow()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (grid[x, y] != null && MatchFinder.Instance.HasMatchAt(x, y))
                    return true;
        return false;
    }

    // ─── LEVEL RESET ─────────────────────────

    public void ResetForLevel(LevelData level)
    {
        ClearGrid();
        ClearObstacles();
        ClearCollectibles();
        ClearNets();

        width = level.gridWidth;
        height = level.gridHeight;
        if (level.levelFishPool != null && level.levelFishPool.Length > 0)
            fishDataPool = level.levelFishPool;
        cellSize = 5.5f / Mathf.Max(width, height);
        grid = new Fish[width, height];

        SpawnObstacles(level.obstacles);
        SpawnRandomObstacles(level.randomObstacles);
        SpawnCollectibles(level.collectibles);
        SpawnRandomCollectibles(level.randomCollectibles);

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (!IsCellBlocked(x, y) && !HasCollectibleAt(x, y))
                    SpawnFishAt(x, y);

        SpawnFishingNets(level.nets);
        SpawnRandomNets(level.randomNetCount);

        IsBusy = false;
        Debug.Log($"<color=cyan>[Grid] {level.levelName}: {width}x{height}, {obstacles.Count} obs, {collectibles.Count} col, {nets.Count} nets</color>");
    }

    private void ClearGrid()
    {
        if (grid == null) return;
        int oldW = grid.GetLength(0);
        int oldH = grid.GetLength(1);
        for (int x = 0; x < oldW; x++)
            for (int y = 0; y < oldH; y++)
                if (grid[x, y] != null) { Destroy(grid[x, y].gameObject); grid[x, y] = null; }
    }

    // ─── OBSTACLES ─────────────────────────

    public bool IsCellBlocked(int x, int y) { return obstacles.ContainsKey(new Vector2Int(x, y)); }

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
                if (!obstacles.ContainsKey(pos)) candidates.Add(pos);
            }
        foreach (var spec in specs)
            for (int i = 0; i < spec.count; i++)
            {
                if (candidates.Count == 0) return;
                int idx = Random.Range(0, candidates.Count);
                Vector2Int pos = candidates[idx];
                candidates.RemoveAt(idx);
                SpawnSingleObstacle(pos.x, pos.y, spec.type);
            }
    }

    private void SpawnSingleObstacle(int x, int y, ObstacleType type)
    {
        Vector3 worldPos = GridToWorldPosition(x, y);
        GameObject obj = Instantiate(obstaclePrefab, worldPos, Quaternion.identity, gridParent);
        Obstacle obs = obj.GetComponent<Obstacle>();
        obs.Initialize(type, x, y, cellSize);
        obstacles[new Vector2Int(x, y)] = obs;
    }

    private void ClearObstacles()
    {
        foreach (var kvp in obstacles) if (kvp.Value != null) Destroy(kvp.Value.gameObject);
        obstacles.Clear();
    }

    public void DamageObstacleAt(int x, int y)
    {
        Vector2Int key = new Vector2Int(x, y);
        if (!obstacles.TryGetValue(key, out Obstacle obs)) return;
        if (obs == null) return;
        ObstacleType brokenType = obs.type;
        bool broken = obs.TakeDamage();
        if (broken)
        {
            obstacles.Remove(key);
            LevelManager.Instance?.ReportObstacleCleared(brokenType, 1);
            AudioManager.Instance?.PlayObstacleBreak(brokenType);

            if (brokenType == ObstacleType.Cage)
            {
                Vector3 wpos = GridToWorldPosition(x, y);
                MatchVFXManager.Instance?.SpawnBurst(wpos, new Color(0.6f, 0.9f, 1f));
                SpawnEscapingFishThenRefill(x, y);
            }
        }
    }

    public void DamageNetAt(int x, int y)
    {
        Vector2Int key = new Vector2Int(x, y);
        if (!nets.TryGetValue(key, out FishingNet net)) return;
        if (net == null) return;
        bool broken = net.TakeDamage();
        if (broken)
        {
            nets.Remove(key);
            LevelManager.Instance?.ReportNetCleared(1);
            AudioManager.Instance?.PlayNetBreak();
        }
    }

    private void DamageAdjacentObstaclesAndNets(IEnumerable<Vector2Int> clearedPositions)
    {
        HashSet<Vector2Int> obsToDmg = new HashSet<Vector2Int>();
        HashSet<Vector2Int> netToDmg = new HashSet<Vector2Int>();
        int[] dx = { 0, 0, -1, 1 };
        int[] dy = { 1, -1, 0, 0 };
        foreach (var pos in clearedPositions)
            for (int i = 0; i < 4; i++)
            {
                int nx = pos.x + dx[i], ny = pos.y + dy[i];
                if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                Vector2Int key = new Vector2Int(nx, ny);
                if (IsCellBlocked(nx, ny)) obsToDmg.Add(key);
                if (HasNetAt(nx, ny)) netToDmg.Add(key);
            }
        foreach (var p in obsToDmg) DamageObstacleAt(p.x, p.y);
        foreach (var p in netToDmg) DamageNetAt(p.x, p.y);
    }

    // ─── COLLECTIBLES ─────────────────────────

    public bool HasCollectibleAt(int x, int y) { return collectibles.ContainsKey(new Vector2Int(x, y)); }

    private void SpawnCollectibles(List<CollectiblePlacement> placements)
    {
        if (placements == null || collectiblePrefab == null) return;
        foreach (var p in placements)
        {
            if (p.gridX < 0 || p.gridX >= width || p.gridY < 0 || p.gridY >= height) continue;
            Vector2Int pos = new Vector2Int(p.gridX, p.gridY);
            if (obstacles.ContainsKey(pos) || collectibles.ContainsKey(pos)) continue;
            SpawnSingleCollectible(p.gridX, p.gridY, p.type);
        }
    }

    private void SpawnRandomCollectibles(List<RandomCollectibleSpec> specs)
    {
        if (specs == null || collectiblePrefab == null) return;
        int minY = Mathf.Max(2, height / 2);
        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int x = 0; x < width; x++)
            for (int y = minY; y < height; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (obstacles.ContainsKey(pos) || collectibles.ContainsKey(pos)) continue;
                candidates.Add(pos);
            }
        foreach (var spec in specs)
            for (int i = 0; i < spec.count; i++)
            {
                if (candidates.Count == 0) return;
                int idx = Random.Range(0, candidates.Count);
                Vector2Int pos = candidates[idx];
                candidates.RemoveAt(idx);
                SpawnSingleCollectible(pos.x, pos.y, spec.type);
            }
    }

    private void SpawnSingleCollectible(int x, int y, CollectibleType type)
    {
        Vector3 worldPos = GridToWorldPosition(x, y);
        GameObject obj = Instantiate(collectiblePrefab, worldPos, Quaternion.identity, gridParent);
        Collectible c = obj.GetComponent<Collectible>();
        c.Initialize(type, x, y, cellSize);
        collectibles[new Vector2Int(x, y)] = c;
    }

    private void ClearCollectibles()
    {
        foreach (var kvp in collectibles) if (kvp.Value != null) Destroy(kvp.Value.gameObject);
        collectibles.Clear();
    }

    // ─── FISHING NETS ─────────────────────────

    public bool HasNetAt(int x, int y) { return nets.ContainsKey(new Vector2Int(x, y)); }

    private void SpawnFishingNets(List<NetPlacement> placements)
    {
        if (placements == null || fishingNetPrefab == null) return;
        foreach (var p in placements)
        {
            if (p.gridX < 0 || p.gridX >= width || p.gridY < 0 || p.gridY >= height) continue;
            Vector2Int pos = new Vector2Int(p.gridX, p.gridY);
            if (obstacles.ContainsKey(pos) || collectibles.ContainsKey(pos)) continue;
            if (nets.ContainsKey(pos)) continue;
            if (grid[p.gridX, p.gridY] == null) continue;
            SpawnSingleNet(p.gridX, p.gridY);
        }
    }

    private void SpawnRandomNets(int count)
    {
        if (count <= 0 || fishingNetPrefab == null) return;
        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (obstacles.ContainsKey(pos) || collectibles.ContainsKey(pos)) continue;
                if (nets.ContainsKey(pos)) continue;
                if (grid[x, y] == null) continue;
                candidates.Add(pos);
            }
        for (int i = 0; i < count; i++)
        {
            if (candidates.Count == 0) return;
            int idx = Random.Range(0, candidates.Count);
            Vector2Int pos = candidates[idx];
            candidates.RemoveAt(idx);
            SpawnSingleNet(pos.x, pos.y);
        }
    }

    private void SpawnSingleNet(int x, int y)
    {
        Vector3 worldPos = GridToWorldPosition(x, y);
        GameObject obj = Instantiate(fishingNetPrefab, worldPos, Quaternion.identity, gridParent);
        FishingNet net = obj.GetComponent<FishingNet>();
        net.Initialize(x, y, cellSize);
        nets[new Vector2Int(x, y)] = net;
    }

    private void ClearNets()
    {
        foreach (var kvp in nets) if (kvp.Value != null) Destroy(kvp.Value.gameObject);
        nets.Clear();
    }
}