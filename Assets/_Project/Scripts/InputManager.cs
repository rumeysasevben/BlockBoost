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
        // Grid clear/gravity/refill VEYA swap animasyonu sürerken input alma
        if (GridManager.Instance.IsBusy || isSwapping) return;

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
        int targetX = fish.gridX + direction.x;
        int targetY = fish.gridY + direction.y;

        Fish targetFish = GridManager.Instance.GetFishAt(targetX, targetY);
        if (targetFish == null) return;

        StartCoroutine(SwapRoutine(fish, targetFish));
    }

    private IEnumerator SwapRoutine(Fish a, Fish b)
    {
        isSwapping = true;

        yield return StartCoroutine(GridManager.Instance.SwapFishAnimated(a, b));

        bool hasMatch =
            MatchFinder.Instance.HasMatchAt(a.gridX, a.gridY) ||
            MatchFinder.Instance.HasMatchAt(b.gridX, b.gridY);

        if (hasMatch)
        {
            yield return StartCoroutine(GridManager.Instance.ProcessMatches());
        }
        else
        {
            // Geçersiz swap — animasyonlu geri al
            yield return StartCoroutine(GridManager.Instance.SwapFishAnimated(a, b));
        }

        isSwapping = false;
    }
}