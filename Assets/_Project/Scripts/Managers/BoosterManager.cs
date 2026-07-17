using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BoosterManager : MonoBehaviour
{
    public static BoosterManager Instance { get; private set; }

    [Header("Visual Selection Cursor (optional)")]
    public GameObject selectionHighlightPrefab;  // null olabilir

    public Dictionary<BoosterType, int> available = new Dictionary<BoosterType, int>();
    public BoosterType? activeBooster { get; private set; } = null;

    public event Action OnBoostersChanged;
    public event Action<BoosterType?> OnActiveBoosterChanged;

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

    public void InitializeForLevel(LevelData level)
    {
        available[BoosterType.Hammer]  = level.hammerCount;
        available[BoosterType.Shuffle] = level.shuffleCount;
        available[BoosterType.Rocket]  = level.rocketCount;
        activeBooster = null;
        OnBoostersChanged?.Invoke();
        OnActiveBoosterChanged?.Invoke(null);
    }

    public int GetAvailable(BoosterType type)
    {
        return available.TryGetValue(type, out int v) ? v : 0;
    }

    /// <summary>
    /// UI butonundan çağrılır. Shuffle anında çalışır, diğerleri seçim moduna girer.
    /// </summary>
    public void RequestActivate(BoosterType type)
    {
        if (GetAvailable(type) <= 0) return;
        if (GridManager.Instance.IsBusy) return;
        if (LevelManager.Instance == null || !LevelManager.Instance.IsLevelActive) return;

        // Aynı boostera tekrar tıklarsa iptal
        if (activeBooster == type)
        {
            CancelSelection();
            return;
        }

        if (type == BoosterType.Shuffle)
        {
            available[type]--;
            StartCoroutine(GridManager.Instance.ShuffleGrid());
            OnBoostersChanged?.Invoke();
            Debug.Log("<color=cyan>🔀 Booster: Shuffle used</color>");
            return;
        }

        // Selection mode
        activeBooster = type;
        GridManager.Instance.IsBusy = true;  // input bloklanır
        OnActiveBoosterChanged?.Invoke(activeBooster);
        Debug.Log($"<color=cyan>🎯 Booster selection mode: {type}</color>");
    }

    public void CancelSelection()
    {
        if (activeBooster == null) return;
        activeBooster = null;
        GridManager.Instance.IsBusy = false;
        OnActiveBoosterChanged?.Invoke(null);
        Debug.Log("<color=cyan>❌ Booster selection canceled</color>");
    }

    private void Update()
    {
        if (activeBooster == null) return;

        // Sağ tık veya Escape ile iptal
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelSelection();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            // UI üzerindeyse görmezden gel
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            Vector3 worldPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

            int gx = -1, gy = -1;
            if (hit.collider != null)
            {
                Fish f = hit.collider.GetComponent<Fish>();
                if (f != null) { gx = f.gridX; gy = f.gridY; }
            }

            if (gx < 0) return;  // balık değil, devam et seçim modunda

            // Geçerli hedef bulundu, boosteri uygula
            BoosterType used = activeBooster.Value;
            activeBooster = null;
            available[used]--;
            GridManager.Instance.IsBusy = false;  // coroutine kendi IsBusy yönetir
            OnActiveBoosterChanged?.Invoke(null);
            OnBoostersChanged?.Invoke();

            switch (used)
            {
                case BoosterType.Hammer:
                    StartCoroutine(GridManager.Instance.HammerCellAt(gx, gy));
                    Debug.Log($"<color=cyan>🔨 Hammer @ ({gx},{gy})</color>");
                    break;
                case BoosterType.Rocket:
                    StartCoroutine(GridManager.Instance.RocketCellAt(gx, gy));
                    Debug.Log($"<color=cyan>🚀 Rocket @ ({gx},{gy})</color>");
                    break;
            }
        }
    }
}