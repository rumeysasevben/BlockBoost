using UnityEngine;
using UnityEngine.UI;

public class SfxToggleButton : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite sfxOnSprite;
    [SerializeField] private Sprite sfxOffSprite;

    private Image img;

    private void Start()
    {
        img = GetComponent<Image>();
        UpdateIcon();
    }

    public void ToggleSfx()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.ToggleSFX();

        UpdateIcon();
    }

    private void UpdateIcon()
    {
        if (img == null) img = GetComponent<Image>();
        if (AudioManager.Instance == null) return;

        bool isOn = AudioManager.Instance.SfxEnabled;
        img.sprite = isOn ? sfxOnSprite : sfxOffSprite;
    }
}