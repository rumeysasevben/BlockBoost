using UnityEngine;

public class BackToLevelSelectButton : MonoBehaviour
{
    [Tooltip("Kapatılacak: şu an aktif olan oyun ekranı Canvas'ı")]
    [SerializeField] private GameObject gameCanvas;

    [Tooltip("Açılacak: Level Select Canvas'ı")]
    [SerializeField] private GameObject levelSelectCanvas;

    public void GoBackToLevelSelect()
    {
        if (gameCanvas != null) gameCanvas.SetActive(false);
        if (levelSelectCanvas != null) levelSelectCanvas.SetActive(true);
    }
}