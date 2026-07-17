using UnityEngine;
using UnityEngine.UI;

public class MusicToggleButton : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite musicOnSprite;
    [SerializeField] private Sprite musicOffSprite;

    private Image img;

    private void Start()
    {
        img = GetComponent<Image>();
        UpdateIcon();
    }

    public void ToggleMusic()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.ToggleMusic();

        UpdateIcon();
    }

    private void UpdateIcon()
    {
        if (img == null) img = GetComponent<Image>();
        if (AudioManager.Instance == null) return;

        bool isOn = AudioManager.Instance.MusicEnabled;
        img.sprite = isOn ? musicOnSprite : musicOffSprite;
    }
}