using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;

    private void Awake()
    {
        if (playButton) playButton.onClick.AddListener(OnPlay);
        if (quitButton) quitButton.onClick.AddListener(OnQuit);
    }

    private void OnPlay()
    {
        GameFlowManager.Instance.GoToLevelSelect();
    }

    private void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}