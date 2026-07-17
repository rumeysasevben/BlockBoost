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
    /// Her run ayrı bir MatchGroup olarak döner.
    /// </summary>
    public List<MatchGroup> FindAllMatchGroups()
    {
        List<MatchGroup> groups = new List<MatchGroup>();
        GridManager gm = GridManager.Instance;
        int w = gm.width;
        int h = gm.height;

        // YATAY
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
                        MatchGroup g = new MatchGroup { isHorizontal = true };
                        for (int i = runStart; i < x; i++)
                            g.fish.Add(gm.GetFishAt(i, y));
                        groups.Add(g);
                    }
                    runStart = x;
                }
            }
        }

        // DİKEY
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
                        MatchGroup g = new MatchGroup { isHorizontal = false };
                        for (int i = runStart; i < y; i++)
                            g.fish.Add(gm.GetFishAt(x, i));
                        groups.Add(g);
                    }
                    runStart = y;
                }
            }
        }

        return groups;
    }

    /// <summary>
    /// Eski API ile uyumluluk için: tüm match'leri tek HashSet olarak verir.
    /// GridManager refactor edilene kadar var.
    /// </summary>
    public HashSet<Fish> FindAllMatches()
    {
        HashSet<Fish> all = new HashSet<Fish>();
        foreach (var g in FindAllMatchGroups())
            foreach (var f in g.fish)
                all.Add(f);
        return all;
    }

    /// <summary>
    /// Belirli bir konumda match var mı? (Swap geçerliliği için lokal kontrol.)
    /// </summary>
    public bool HasMatchAt(int x, int y)
    {
        GridManager gm = GridManager.Instance;
        Fish center = gm.GetFishAt(x, y);
        if (center == null) return false;

        // Yatay
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
        if (a.data == null || b.data == null) return false;
        return a.data.fishType == b.data.fishType;
    }
}