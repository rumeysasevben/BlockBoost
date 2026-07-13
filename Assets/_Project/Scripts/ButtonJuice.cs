using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
public class ButtonJuice : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [Header("Scale")]
    [SerializeField] private float hoverScale = 1.06f;
    [SerializeField] private float pressScale = 0.90f;
    [SerializeField] private float duration   = 0.12f;

    [Header("Click Pop")]
    [SerializeField] private float popScale   = 1.12f;
    [SerializeField] private float popDuration = 0.18f;

    [Header("Options")]
    [SerializeField] private bool useHover = true;
    [SerializeField] private bool playSfx  = true;

    private Vector3 baseScale;
    private bool isPressed;
    private bool isPointerInside;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    void OnDisable()
    {
        transform.DOKill();
        transform.localScale = baseScale;
        isPressed = false;
        isPointerInside = false;
    }

    public void OnPointerEnter(PointerEventData e)
    {
        isPointerInside = true;
        if (useHover && !isPressed)
            ScaleTo(hoverScale);
    }

    public void OnPointerExit(PointerEventData e)
    {
        isPointerInside = false;
        if (!isPressed)
            ScaleTo(1f);
    }

    public void OnPointerDown(PointerEventData e)
    {
        isPressed = true;
        ScaleTo(pressScale);
    }

    public void OnPointerUp(PointerEventData e)
    {
        isPressed = false;
        ScaleTo(isPointerInside && useHover ? hoverScale : 1f);
    }

    public void OnPointerClick(PointerEventData e)
    {
        transform.DOKill();
        transform.localScale = baseScale * pressScale;
        transform.DOScale(baseScale * (isPointerInside && useHover ? hoverScale : 1f), popDuration)
                 .SetEase(Ease.OutBack)
                 .SetUpdate(true);

        if (playSfx && AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();
    }

    private void ScaleTo(float mult)
    {
        transform.DOKill();
        transform.DOScale(baseScale * mult, duration)
                 .SetEase(Ease.OutQuad)
                 .SetUpdate(true);
    }
}