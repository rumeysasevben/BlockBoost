using UnityEngine;
using TMPro;
using DG.Tweening;

[RequireComponent(typeof(TMP_Text))]
public class ScorePopup : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float floatHeight = 1.2f;
    [SerializeField] private float lifetime = 0.9f;

    [Header("Combo Text Animation")]
    [Tooltip("0 = yerinde dursun (flash ile hizali kalir)")]
    [SerializeField] private float comboFloatHeight = 0f;
    [SerializeField] private float comboLifetime = 1.8f;
    [SerializeField] private float comboScale = 2.2f;
    [Tooltip("Combo yazisi tile'larin ustunde kalsin diye yuksek sorting order")]
    [SerializeField] private int comboSortingOrder = 50;

    private TMP_Text txt;

    private void Awake()
    {
        txt = GetComponent<TMP_Text>();
    }

    public void Show(int score, Color color)
    {
        if (txt == null) txt = GetComponent<TMP_Text>();
        txt.text = $"+{score}";
        txt.color = color;
        transform.localScale = Vector3.zero;

        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.up * floatHeight;

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack));
        seq.Join(transform.DOMove(endPos, lifetime).SetEase(Ease.OutCubic));
        seq.Insert(lifetime * 0.5f, txt.DOFade(0f, lifetime * 0.5f));
        seq.OnComplete(() => Destroy(gameObject));
    }

    /// <summary>
    /// Combo / special feedback yazisi ("Fin-tastic!", "Torpedo!" gibi).
    /// Verilen dunya pozisyonunda, buyuk ve tile'larin ustunde gosterilir.
    /// </summary>
    public void ShowTextAt(string message, Color color, Vector3 worldPos)
    {
        if (txt == null) txt = GetComponent<TMP_Text>();

        // RectTransform kullaniliyorsa anchor/pivot'u merkeze sabitle,
        // yoksa pozisyon layout tarafindan eziliyor
        RectTransform rt = transform as RectTransform;
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
        }

        txt.text = message;
        txt.color = color;
        txt.alignment = TextAlignmentOptions.Center;

        // Tile'larin ustunde kalsin
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = comboSortingOrder;

        // Pozisyonu kesin olarak zorla
        transform.position = worldPos;
        transform.localScale = Vector3.zero;

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(comboScale, 0.25f).SetEase(Ease.OutBack));
        if (comboFloatHeight > 0.01f)
            seq.Join(transform.DOMove(worldPos + Vector3.up * comboFloatHeight, comboLifetime).SetEase(Ease.OutCubic));
        seq.Insert(0.25f, transform.DOPunchScale(Vector3.one * 0.25f, 0.35f, 6, 0.6f));
        seq.Insert(comboLifetime * 0.55f, txt.DOFade(0f, comboLifetime * 0.45f));
        seq.OnComplete(() => Destroy(gameObject));
    }

    /// <summary>
    /// Geriye donuk uyumluluk: mevcut pozisyonda goster.
    /// </summary>
    public void ShowText(string message, Color color)
    {
        ShowTextAt(message, color, transform.position);
    }
}