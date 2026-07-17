using UnityEngine;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    public enum Screen { MainMenu, LevelSelect, Gameplay }

    [Header("Canvases")]
    [SerializeField] private GameObject mainMenuCanvas;
    [SerializeField] private GameObject levelSelectCanvas;
    [SerializeField] private GameObject gameplayCanvas;

    [Header("Gameplay Refs")]
    [SerializeField] private GameObject gridManagerObj;  // Grid göstereceğimiz/gizleyeceğimiz için

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        ShowScreen(Screen.MainMenu);
    }

    public void ShowScreen(Screen screen)
    {
        if (mainMenuCanvas)     mainMenuCanvas.SetActive(screen == Screen.MainMenu);
        if (levelSelectCanvas)  levelSelectCanvas.SetActive(screen == Screen.LevelSelect);
        if (gameplayCanvas)     gameplayCanvas.SetActive(screen == Screen.Gameplay);
        Debug.Log($"<color=cyan>[Flow] {screen} ekranı açıldı</color>");
    }

    public void GoToMainMenu()    => ShowScreen(Screen.MainMenu);
    public void GoToLevelSelect() => ShowScreen(Screen.LevelSelect);
    public void GoToGameplay()    => ShowScreen(Screen.Gameplay);
}