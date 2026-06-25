using UnityEngine;
using TMPro;
using DG.Tweening;

[RequireComponent(typeof(TMP_Text))]
public class ScorePopup : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float floatHeight = 1.2f;
    [SerializeField] private float lifetime = 0.9f;

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
}