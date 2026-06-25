using UnityEngine;

public class MatchVFXManager : MonoBehaviour
{
    public static MatchVFXManager Instance { get; private set; }

    [Header("Prefabs")]
    [Tooltip("ParticleSystem prefab - match patlamasında spawn olur")]
    public GameObject particleBurstPrefab;
    [Tooltip("ScorePopup prefab - TMP text ile +50 gibi yazı uçar")]
    public GameObject scorePopupPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// Belirtilen pozisyonda renkli particle burst spawn et.
    /// </summary>
    public void SpawnBurst(Vector3 worldPos, Color color)
    {
        if (particleBurstPrefab == null) return;
        GameObject obj = Instantiate(particleBurstPrefab, worldPos, Quaternion.identity);
        ParticleSystem ps = obj.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.startColor = color;
        }
        Destroy(obj, 2f);
    }

    /// <summary>
    /// Belirtilen pozisyonda +X yazısı yukarı uçar.
    /// </summary>
    public void SpawnScorePopup(Vector3 worldPos, int score, Color color)
    {
        if (scorePopupPrefab == null) return;
        GameObject obj = Instantiate(scorePopupPrefab, worldPos, Quaternion.identity);
        ScorePopup popup = obj.GetComponent<ScorePopup>();
        if (popup != null) popup.Show(score, color);
    }
}