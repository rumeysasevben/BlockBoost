using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

[RequireComponent(typeof(Button))]
public class ButtonSFX : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Ses")]
    public bool playClickSound = true;

    [Header("Press Animasyonu")]
    public bool useScaleAnimation = true;
    [Range(0.7f, 1f)] public float pressedScale = 0.92f;
    public float pressDuration = 0.08f;
    public float releaseDuration = 0.12f;

    private Button button;
    private Vector3 originalScale;

    private void Awake()
    {
        button = GetComponent<Button>();
        originalScale = transform.localScale;

        if (playClickSound)
            button.onClick.AddListener(() => AudioManager.Instance?.PlayButtonClick());
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!useScaleAnimation || button == null || !button.interactable) return;
        transform.DOKill();
        transform.DOScale(originalScale * pressedScale, pressDuration).SetEase(Ease.OutQuad);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!useScaleAnimation || button == null) return;
        transform.DOKill();
        transform.DOScale(originalScale, releaseDuration).SetEase(Ease.OutBack);
    }

    private void OnDisable()
    {
        transform.DOKill();
        transform.localScale = originalScale;
    }
}