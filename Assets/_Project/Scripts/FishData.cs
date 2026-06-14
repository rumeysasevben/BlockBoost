using UnityEngine;

public enum FishType
{
    Clownfish,   // Palyaço (turuncu çizgili)
    BlueTang,    // Mavi
    Pufferfish,  // Balon
    Shrimp,      // Karides (pembe/sarı)
    RedFish,     // Kırmızı
    GreenFish    // Yeşil
}

[CreateAssetMenu(fileName = "NewFishData", menuName = "BlockBoost/Fish Data", order = 0)]
public class FishData : ScriptableObject
{
    [Header("Identity")]
    public FishType fishType;
    public string displayName;

    [Header("Visual")]
    public Sprite sprite;
    public Color glowColor = Color.white;  // Eşleşme efektleri için

    [Header("Gameplay")]
    public int scoreValue = 10;             // Tek balık eşleşmesinden gelen puan
    [Range(0f, 1f)]
    public float spawnWeight = 1f;          // Grid'de rastgele spawn'da ağırlık (1 = normal)

    [Header("Audio (opsiyonel, sonra)")]
    public AudioClip matchSound;
}