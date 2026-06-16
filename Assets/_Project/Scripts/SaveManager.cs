using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const string KEY_STARS_PREFIX = "Level_Stars_";   // Level_Stars_1, Level_Stars_2, ...
    private const string KEY_LAST_UNLOCKED = "LastUnlockedLevel";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // --- STARS ---

    /// <summary>
    /// O level için kazanılmış en yüksek yıldız sayısını döndürür (0 = oynanmadı veya kaybedildi).
    /// </summary>
    public int GetStars(int levelNumber)
    {
        return PlayerPrefs.GetInt(KEY_STARS_PREFIX + levelNumber, 0);
    }

    /// <summary>
    /// Yeni yıldız önceki rekordan büyükse kaydeder.
    /// </summary>
    public void SaveLevelResult(int levelNumber, int starsEarned)
    {
        int previousBest = GetStars(levelNumber);
        if (starsEarned > previousBest)
        {
            PlayerPrefs.SetInt(KEY_STARS_PREFIX + levelNumber, starsEarned);
            Debug.Log($"<color=lime>[Save] Level {levelNumber} yeni rekor: {starsEarned} yıldız</color>");
        }

        // Yeni level'ı aç
        int currentMax = PlayerPrefs.GetInt(KEY_LAST_UNLOCKED, 1);
        if (levelNumber >= currentMax)
        {
            PlayerPrefs.SetInt(KEY_LAST_UNLOCKED, levelNumber + 1);
        }

        PlayerPrefs.Save();
    }

    // --- UNLOCK ---

    /// <summary>
    /// Level oynanabilir mi?
    /// </summary>
    public bool IsLevelUnlocked(int levelNumber)
    {
        if (levelNumber <= 1) return true;  // Level 1 her zaman açık
        int lastUnlocked = PlayerPrefs.GetInt(KEY_LAST_UNLOCKED, 1);
        return levelNumber <= lastUnlocked;
    }

    public int GetLastUnlockedLevel()
    {
        return PlayerPrefs.GetInt(KEY_LAST_UNLOCKED, 1);
    }

    // --- RESET (dev için) ---

    [ContextMenu("Reset All Progress")]
    public void ResetAllProgress()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("<color=yellow>[Save] Tüm ilerleme silindi</color>");
    }
}