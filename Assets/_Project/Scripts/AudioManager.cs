using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music")]
    public AudioClip ambientMusic;
    [Range(0f, 1f)] public float musicVolume = 0.35f;

    [Header("Match / Fish")]
    public AudioClip matchPop;
    [Tooltip("Cascade combo'da her seviyede pitch yukselir")]
    public float comboPitchStep = 0.08f;
    public float maxComboPitch = 1.6f;

    [Header("Special Tiles")]
    public AudioClip specialCreate;   // özel taş oluşurken "power up" sesi
    public AudioClip rocketWhoosh;
    public AudioClip bombExplode;
    public AudioClip colorBombSparkle;
    public AudioClip krakenCombo;

    [Header("Obstacles")]
    public AudioClip seaweedBreak;
    public AudioClip coralBreak;
    public AudioClip iceBreak;
    public AudioClip cageBreak;
    public AudioClip netBreak;

    [Header("Collectibles")]
    public AudioClip collectibleDeliver;

    [Header("Boosters")]
    public AudioClip hammerHit;
    public AudioClip shuffleSwoosh;

    [Header("UI")]
    public AudioClip buttonClick;
    public AudioClip levelWin;
    public AudioClip levelLose;
    public AudioClip swapSound;
    public AudioClip invalidSwap;
    public AudioClip starPop;

    [Header("SFX Settings")]
    [Range(0f, 1f)] public float sfxVolume = 0.7f;
    [Tooltip("Ayni anda calabilecek maksimum SFX sayisi")]
    public int sfxPoolSize = 12;

    private AudioSource musicSource;
    private List<AudioSource> sfxPool = new List<AudioSource>();
    private int poolIndex = 0;

    private const string MUSIC_KEY = "BlockBoost_MusicOn";
    private const string SFX_KEY = "BlockBoost_SfxOn";

    public bool MusicEnabled { get; private set; } = true;
    public bool SfxEnabled { get; private set; } = true;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Music source
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = musicVolume;

        // SFX pool
        for (int i = 0; i < sfxPoolSize; i++)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            sfxPool.Add(src);
        }

        MusicEnabled = PlayerPrefs.GetInt(MUSIC_KEY, 1) == 1;
        SfxEnabled = PlayerPrefs.GetInt(SFX_KEY, 1) == 1;
    }

    private void Start()
    {
        PlayMusic();
    }

    // ─── CORE ─────────────────────────

    private void PlaySFX(AudioClip clip, float volumeScale = 1f, float pitch = 1f)
    {
        if (!SfxEnabled || clip == null) return;

        AudioSource src = sfxPool[poolIndex];
        poolIndex = (poolIndex + 1) % sfxPool.Count;

        src.clip = clip;
        src.volume = sfxVolume * volumeScale;
        src.pitch = pitch;
        src.Play();
    }

    public void PlayMusic()
    {
        if (ambientMusic == null) return;
        musicSource.clip = ambientMusic;
        musicSource.volume = musicVolume;
        if (MusicEnabled) musicSource.Play();
    }

    public void ToggleMusic()
    {
        MusicEnabled = !MusicEnabled;
        PlayerPrefs.SetInt(MUSIC_KEY, MusicEnabled ? 1 : 0);
        PlayerPrefs.Save();

        if (MusicEnabled) musicSource.Play();
        else musicSource.Stop();
    }

    public void ToggleSFX()
    {
        SfxEnabled = !SfxEnabled;
        PlayerPrefs.SetInt(SFX_KEY, SfxEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    // ─── MATCH ─────────────────────────

    // comboLevel 1 = normal, 2+ = cascade. Pitch yukselir.
    public void PlayMatch(int comboLevel = 1)
    {
        float pitch = Mathf.Min(1f + (comboLevel - 1) * comboPitchStep, maxComboPitch);
        PlaySFX(matchPop, 1f, pitch);
    }

    public void PlaySwap() => PlaySFX(swapSound, 0.6f);
    public void PlayInvalidSwap() => PlaySFX(invalidSwap, 0.5f);

    // ─── SPECIAL TILES ─────────────────────────

    public void PlaySpecialCreate() => PlaySFX(specialCreate, 0.9f);

    public void PlaySpecial(SpecialType type)
    {
        switch (type)
        {
            case SpecialType.RocketH:
            case SpecialType.RocketV:
                PlaySFX(rocketWhoosh);
                break;
            case SpecialType.Bomb:
                PlaySFX(bombExplode);
                break;
            case SpecialType.ColorBomb:
                PlaySFX(colorBombSparkle);
                break;
        }
    }

    public void PlayKraken() => PlaySFX(krakenCombo, 1f);

    // ─── OBSTACLES ─────────────────────────

    public void PlayObstacleBreak(ObstacleType type)
    {
        switch (type)
        {
            case ObstacleType.Seaweed: PlaySFX(seaweedBreak, 0.8f); break;
            case ObstacleType.Coral:   PlaySFX(coralBreak, 0.8f);   break;
            case ObstacleType.Ice:     PlaySFX(iceBreak, 0.9f);     break;
            case ObstacleType.Cage:    PlaySFX(cageBreak, 0.9f);    break;
        }
    }

    public void PlayNetBreak() => PlaySFX(netBreak, 0.8f);

    // ─── COLLECTIBLES ─────────────────────────

    public void PlayCollectibleDeliver() => PlaySFX(collectibleDeliver);

    // ─── BOOSTERS ─────────────────────────

    public void PlayHammer() => PlaySFX(hammerHit);
    public void PlayShuffle() => PlaySFX(shuffleSwoosh);

    // ─── UI ─────────────────────────

    public void PlayButtonClick() => PlaySFX(buttonClick, 0.6f);
    public void PlayWin() => PlaySFX(levelWin, 1f);
    public void PlayLose() => PlaySFX(levelLose, 0.9f);
    public void PlayStarPop(float pitch = 1f) => PlaySFX(starPop, 0.8f, pitch);
}