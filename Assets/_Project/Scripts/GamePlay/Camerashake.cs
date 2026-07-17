using UnityEngine;
using DG.Tweening;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    private Vector3 originalPos;
    private Tween activeTween;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        originalPos = transform.localPosition;
    }

    /// <summary>
    /// Kamerayı sallar. Strength büyüklüğü, duration süreyi belirler.
    /// </summary>
    public void Shake(float duration = 0.3f, float strength = 0.3f, int vibrato = 10)
    {
        if (activeTween != null && activeTween.IsActive()) activeTween.Kill();
        transform.localPosition = originalPos;
        activeTween = transform.DOShakePosition(duration, strength, vibrato, 90, false, true)
            .OnComplete(() => transform.localPosition = originalPos);
    }
}