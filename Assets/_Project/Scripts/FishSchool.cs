using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class FishSchool : MonoBehaviour
{
    public static FishSchool Instance { get; private set; }

    [Header("Fish Sprites (mevcut balık sprite'ları)")]
    public Sprite[] fishSprites;

    [Header("Timing")]
    [Tooltip("Sürüler arası minimum bekleme (saniye)")]
    public float minInterval = 8f;
    public float maxInterval = 15f;

    [Header("School Settings")]
    [Tooltip("Bir sürüdeki balık sayısı")]
    public int minFishPerSchool = 4;
    public int maxFishPerSchool = 8;
    public float fishScale = 0.35f;         // grid balıklarından küçük
    [Range(0f, 1f)] public float fishAlpha = 0.35f;  // yarı saydam
    public float swimDuration = 6f;         // ekranı geçme süresi
    public int sortingOrder = -5;           // grid'in arkasında

    private Camera cam;
    private readonly List<GameObject> activeFish = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        cam = Camera.main;
    }

    private void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        // ilk sürü için kısa bekleme
        yield return new WaitForSeconds(Random.Range(3f, 6f));
        while (true)
        {
            SpawnSchool();
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
        }
    }

    private void SpawnSchool()
    {
        if (fishSprites == null || fishSprites.Length == 0 || cam == null) return;

        // Kameranın dünya sınırlarını hesapla
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        // Sürü soldan mı sağdan mı gelsin
        bool fromLeft = Random.value < 0.5f;
        float startX = fromLeft ? -halfW - 1f : halfW + 1f;
        float endX   = fromLeft ?  halfW + 1f : -halfW - 1f;

        // Dikey konum: ekranın orta-üst bandında rastgele
        float baseY = Random.Range(-halfH * 0.3f, halfH * 0.8f);

        int count = Random.Range(minFishPerSchool, maxFishPerSchool + 1);
        Sprite sprite = fishSprites[Random.Range(0, fishSprites.Length)];

        for (int i = 0; i < count; i++)
        {
            GameObject fish = new GameObject("SchoolFish");
            fish.transform.SetParent(transform);

            SpriteRenderer sr = fish.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            sr.color = new Color(1f, 1f, 1f, fishAlpha);

            // sürü dağınıklığı: hafif rastgele offset
            float offsetX = Random.Range(-1.2f, 1.2f);
            float offsetY = Random.Range(-0.8f, 0.8f);
            Vector3 startPos = new Vector3(startX + offsetX, baseY + offsetY, 0f);
            fish.transform.position = startPos;
            fish.transform.localScale = Vector3.one * fishScale;

            // yön: sağa gidiyorsa sprite'ı çevir
            if (!fromLeft) fish.transform.localScale = new Vector3(-fishScale, fishScale, 1f);

            activeFish.Add(fish);

            Vector3 endPos = new Vector3(endX + offsetX, baseY + offsetY, 0f);
            float dur = swimDuration * Random.Range(0.85f, 1.15f);

            fish.transform.DOMove(endPos, dur).SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    activeFish.Remove(fish);
                    if (fish != null) Destroy(fish);
                });

            // hafif dikey dalgalanma (yüzme hissi)
            fish.transform.DOMoveY(baseY + offsetY + 0.3f, 1.5f)
                .SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }
    }

    // Match olunca çağrılır — tüm sürü balıkları ürküp hızla dağılır
    public void ScatterAll()
    {
        foreach (var fish in new List<GameObject>(activeFish))
        {
            if (fish == null) continue;
            fish.transform.DOKill();

            // rastgele yukarı/aşağı bir yöne fırla
            Vector3 dir = new Vector3(Random.Range(-1f, 1f), Random.Range(0.5f, 1.5f), 0f).normalized;
            Vector3 target = fish.transform.position + dir * 3f;

            GameObject f = fish;
            f.transform.DOMove(target, 0.5f).SetEase(Ease.OutQuad);
            SpriteRenderer sr = f.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.DOFade(0f, 0.5f).OnComplete(() =>
                {
                    activeFish.Remove(f);
                    if (f != null) Destroy(f);
                });
        }
    }
}