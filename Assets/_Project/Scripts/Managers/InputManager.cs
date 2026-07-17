using System.Collections;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Header("Settings")]
    public float minSwipeDistance = 0.3f;  // Dünya birimi cinsinden minimum swipe mesafesi

    private Fish selectedFish;
    private Vector2 startTouchWorldPos;
    private bool isDragging;
    private bool isSwapping;

    private Camera mainCam;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        mainCam = Camera.main;
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        if (isSwapping) return;

        // Grid clear/gravity/refill VEYA swap animasyonu sürerken input alma
        if (GridManager.Instance != null && GridManager.Instance.IsBusy) return;

        // Level bittiyse de input alma
        if (LevelManager.Instance != null && !LevelManager.Instance.IsLevelActive) return;

        // Mouse/Touch başlangıcı
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 worldPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
            startTouchWorldPos = worldPos;

            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
            if (hit.collider != null)
            {
                Fish fish = hit.collider.GetComponent<Fish>();
                if (fish != null)
                {
                    selectedFish = fish;
                    isDragging = true;
                }
            }
        }

        // Sürüklenirken yön belirlendiğinde swap dene
        if (isDragging && Input.GetMouseButton(0))
        {
            Vector2 currentWorldPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 delta = currentWorldPos - startTouchWorldPos;

            if (delta.magnitude >= minSwipeDistance)
            {
                Vector2Int direction = GetSwipeDirection(delta);
                TrySwap(selectedFish, direction);

                // swap başlatıldı, sürüklemeyi bitir
                isDragging = false;
                selectedFish = null;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            selectedFish = null;
        }
    }

    private Vector2Int GetSwipeDirection(Vector2 delta)
    {
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            return delta.x > 0 ? Vector2Int.right : Vector2Int.left;
        else
            return delta.y > 0 ? Vector2Int.up : Vector2Int.down;
    }

    private void TrySwap(Fish fish, Vector2Int direction)
    {
        if (fish == null) return;

        // Grid meşgulse veya zaten swap sürüyorsa hiç başlatma
        if (isSwapping) return;
        if (GridManager.Instance != null && GridManager.Instance.IsBusy) return;

        int targetX = fish.gridX + direction.x;
        int targetY = fish.gridY + direction.y;

        Fish targetFish = GridManager.Instance.GetFishAt(targetX, targetY);
        if (targetFish == null) return;

        StartCoroutine(SwapRoutine(fish, targetFish));
    }

    private IEnumerator SwapRoutine(Fish a, Fish b)
    {
        // Çifte giriş koruması: zaten meşgulse başlama
        if (isSwapping) yield break;
        if (GridManager.Instance != null && GridManager.Instance.IsBusy) yield break;

        // Grid'i HEMEN kilitle — hiçbir input/işlem araya giremesin
        isSwapping = true;
        GridManager.Instance.IsBusy = true;

        // Swap animasyonu
        yield return StartCoroutine(GridManager.Instance.SwapFishAnimated(a, b));

        // ── İKİ SPECIAL SWAP (büyük combo) ──
        if (a.IsSpecial && b.IsSpecial)
        {
            yield return StartCoroutine(GridManager.Instance.HandleSpecialCombo(a, b));
            LevelManager.Instance.UseMove();
            FinishSwap();
            yield break;
        }

        // ── ColorBomb + normal balık ──
        if (a.specialType == SpecialType.ColorBomb && !b.IsSpecial)
        {
            yield return StartCoroutine(GridManager.Instance.ActivateColorBombOnType(a, b.data.fishType));
            LevelManager.Instance.UseMove();
            FinishSwap();
            yield break;
        }
        if (b.specialType == SpecialType.ColorBomb && !a.IsSpecial)
        {
            yield return StartCoroutine(GridManager.Instance.ActivateColorBombOnType(b, a.data.fishType));
            LevelManager.Instance.UseMove();
            FinishSwap();
            yield break;
        }

        // ── ROCKET / BOMB + normal balık → match aranmadan aktive et ──
        Fish singleSpecial = null;
        if (IsRocketOrBomb(a) && !b.IsSpecial) singleSpecial = a;
        else if (IsRocketOrBomb(b) && !a.IsSpecial) singleSpecial = b;

        if (singleSpecial != null)
        {
            yield return StartCoroutine(GridManager.Instance.ActivateSpecialAt(singleSpecial));
            LevelManager.Instance.UseMove();
            FinishSwap();
            yield break;
        }

        // ── NORMAL MATCH ──
        bool hasMatch =
            MatchFinder.Instance.HasMatchAt(a.gridX, a.gridY) ||
            MatchFinder.Instance.HasMatchAt(b.gridX, b.gridY);

        if (hasMatch)
        {
            yield return StartCoroutine(GridManager.Instance.ProcessMatches(a, b));
            LevelManager.Instance.UseMove();
        }
        else
        {
            // Geçersiz swap — geri al (swap sesi calmasin, invalid sesi calsin)
            AudioManager.Instance?.PlayInvalidSwap();
            yield return StartCoroutine(GridManager.Instance.SwapFishAnimated(a, b, 0.2f, false));
        }

        FinishSwap();
    }

    // Swap bittiğinde HER durumda çağrılır — kilitleri güvenle açar
    private void FinishSwap()
    {
        isSwapping = false;
        if (GridManager.Instance != null)
            GridManager.Instance.IsBusy = false;
    }

    private bool IsRocketOrBomb(Fish f)
    {
        return f.specialType == SpecialType.RocketH
            || f.specialType == SpecialType.RocketV
            || f.specialType == SpecialType.Bomb;
    }
}