using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BoosterUI : MonoBehaviour
{
    [Header("Hammer")]
    [SerializeField] private Button hammerButton;
    [SerializeField] private TMP_Text hammerCountText;
    [SerializeField] private Image hammerHighlight;

    [Header("Shuffle")]
    [SerializeField] private Button shuffleButton;
    [SerializeField] private TMP_Text shuffleCountText;

    [Header("Rocket")]
    [SerializeField] private Button rocketButton;
    [SerializeField] private TMP_Text rocketCountText;
    [SerializeField] private Image rocketHighlight;

    private void Start()
    {
        if (hammerButton)  hammerButton.onClick.AddListener(()  => BoosterManager.Instance?.RequestActivate(BoosterType.Hammer));
        if (shuffleButton) shuffleButton.onClick.AddListener(() => BoosterManager.Instance?.RequestActivate(BoosterType.Shuffle));
        if (rocketButton)  rocketButton.onClick.AddListener(()  => BoosterManager.Instance?.RequestActivate(BoosterType.Rocket));

        if (BoosterManager.Instance != null)
        {
            BoosterManager.Instance.OnBoostersChanged += Refresh;
            BoosterManager.Instance.OnActiveBoosterChanged += OnActiveChanged;
        }

        Refresh();
        OnActiveChanged(null);
    }

    private void OnDestroy()
    {
        if (BoosterManager.Instance != null)
        {
            BoosterManager.Instance.OnBoostersChanged -= Refresh;
            BoosterManager.Instance.OnActiveBoosterChanged -= OnActiveChanged;
        }
    }

    private void Refresh()
    {
        if (BoosterManager.Instance == null) return;
        var bm = BoosterManager.Instance;

        int h = bm.GetAvailable(BoosterType.Hammer);
        int s = bm.GetAvailable(BoosterType.Shuffle);
        int r = bm.GetAvailable(BoosterType.Rocket);

        if (hammerCountText)  hammerCountText.text = h.ToString();
        if (shuffleCountText) shuffleCountText.text = s.ToString();
        if (rocketCountText)  rocketCountText.text = r.ToString();

        if (hammerButton)  hammerButton.interactable = h > 0;
        if (shuffleButton) shuffleButton.interactable = s > 0;
        if (rocketButton)  rocketButton.interactable = r > 0;
    }

    private void OnActiveChanged(BoosterType? active)
    {
        if (hammerHighlight) hammerHighlight.enabled = (active == BoosterType.Hammer);
        if (rocketHighlight) rocketHighlight.enabled = (active == BoosterType.Rocket);
    }
}