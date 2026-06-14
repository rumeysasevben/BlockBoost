using System.Collections.Generic;
using UnityEngine;

public class MatchFinder : MonoBehaviour
{
    public static MatchFinder Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// Tüm grid'i tarar, 3+ aynı tip yatay/dikey eşleşmeleri bulur.
    /// Aynı balık birden fazla match'te olabilir (T/L şekiller) — HashSet ile dedupe edilir.
    /// </summary>
    public HashSet<Fish> FindAllMatches()
    {
        HashSet<Fish> matched = new HashSet<Fish>();
        GridManager gm = GridManager.Instance;
        int w = gm.width;
        int h = gm.height;

        // YATAY tarama
        for (int y = 0; y < h; y++)
        {
            int runStart = 0;
            for (int x = 1; x <= w; x++)
            {
                bool endOfRun = (x == w) || !SameType(gm.GetFishAt(x, y), gm.GetFishAt(x - 1, y));
                if (endOfRun)
                {
                    int runLength = x - runStart;
                    if (runLength >= 3)
                    {
                        for (int i = runStart; i < x; i++)
                            matched.Add(gm.GetFishAt(i, y));
                    }
                    runStart = x;
                }
            }
        }

        // DİKEY tarama
        for (int x = 0; x < w; x++)
        {
            int runStart = 0;
            for (int y = 1; y <= h; y++)
            {
                bool endOfRun = (y == h) || !SameType(gm.GetFishAt(x, y), gm.GetFishAt(x, y - 1));
                if (endOfRun)
                {
                    int runLength = y - runStart;
                    if (runLength >= 3)
                    {
                        for (int i = runStart; i < y; i++)
                            matched.Add(gm.GetFishAt(x, i));
                    }
                    runStart = y;
                }
            }
        }

        return matched;
    }

    /// <summary>
    /// Belirli bir balığın bulunduğu konumdaki match'i kontrol eder (swap sonrası lokal kontrol için).
    /// Tüm grid taramasından daha hızlı.
    /// </summary>
    public bool HasMatchAt(int x, int y)
    {
        GridManager gm = GridManager.Instance;
        Fish center = gm.GetFishAt(x, y);
        if (center == null) return false;

        // Yatay: sol + sağ doğrultusunda aynı tip kaç tane?
        int hCount = 1;
        for (int i = x - 1; i >= 0 && SameType(gm.GetFishAt(i, y), center); i--) hCount++;
        for (int i = x + 1; i < gm.width && SameType(gm.GetFishAt(i, y), center); i++) hCount++;
        if (hCount >= 3) return true;

        // Dikey
        int vCount = 1;
        for (int i = y - 1; i >= 0 && SameType(gm.GetFishAt(x, i), center); i--) vCount++;
        for (int i = y + 1; i < gm.height && SameType(gm.GetFishAt(x, i), center); i++) vCount++;
        return vCount >= 3;
    }

    private bool SameType(Fish a, Fish b)
    {
        if (a == null || b == null) return false;
        return a.data.fishType == b.data.fishType;
    }
}