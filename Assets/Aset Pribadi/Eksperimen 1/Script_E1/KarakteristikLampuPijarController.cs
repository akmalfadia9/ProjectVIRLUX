using UnityEngine;

/// <summary>
/// KarakteristikLampuPijarController
/// Mengontrol visual dan cahaya dari lampu pijar di dalam bell jar.
/// Cahaya TIDAK terpengaruh vacuum (sifat gelombang elektromagnetik).
/// </summary>
public class KarakteristikLampuPijarController : MonoBehaviour
{
    [Header("=== Komponen Lampu Pijar ===")]
    [Tooltip("Light component utama (Point Light)")]
    public Light pointLight;

    [Tooltip("Renderer filamen/bohlam untuk emisi material")]
    public Renderer bulbRenderer;

    [Tooltip("Nama property emission di material (biasanya _EmissionColor)")]
    public string emissionColorProperty = "_EmissionColor";

    [Header("=== Parameter Karakteristik Cahaya ===")]
    [Tooltip("Intensitas light saat menyala penuh")]
    public float maxLightIntensity = 3f;

    [Tooltip("Range point light")]
    public float lightRange = 1.5f;

    [Tooltip("Warna cahaya lampu pijar (warm white)")]
    public Color lampColor = new Color(1f, 0.9f, 0.7f);

    [Tooltip("Warna emisi material bohlam saat ON")]
    public Color emissionColorOn  = new Color(1f, 0.85f, 0.5f) * 2f;

    [Tooltip("Warna emisi material bohlam saat OFF")]
    public Color emissionColorOff = Color.black;

    [Header("=== Animasi Nyala/Mati ===")]
    [Tooltip("Kecepatan transisi ON/OFF")]
    public float transitionSpeed = 4f;

    // ─── State internal ───────────────────────────────────────────────────────
    private bool  isOn           = false;
    private float currentIntensity = 0f;

    // ─────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        // Setup light
        if (pointLight != null)
        {
            pointLight.color     = lampColor;
            pointLight.range     = lightRange;
            pointLight.intensity = 0f;
            pointLight.enabled   = true;
        }

        // Subscribe events
        if (KarakteristikExperimentManager.Instance != null)
            KarakteristikExperimentManager.Instance.OnLampToggled += SetLampState;
    }

    private void OnDestroy()
    {
        if (KarakteristikExperimentManager.Instance != null)
            KarakteristikExperimentManager.Instance.OnLampToggled -= SetLampState;
    }

    private void Update()
    {
        float targetIntensity = isOn ? maxLightIntensity : 0f;
        currentIntensity = Mathf.MoveTowards(currentIntensity, targetIntensity, transitionSpeed * Time.deltaTime);

        // Update light
        if (pointLight != null)
            pointLight.intensity = currentIntensity;

        // Update material emission
        if (bulbRenderer != null && bulbRenderer.material.HasProperty(emissionColorProperty))
        {
            float t = currentIntensity / Mathf.Max(maxLightIntensity, 0.001f);
            Color emissionColor = Color.Lerp(emissionColorOff, emissionColorOn, t);
            bulbRenderer.material.SetColor(emissionColorProperty, emissionColor);
        }
    }

    private void SetLampState(bool state)
    {
        isOn = state;
        Debug.Log($"[Karakteristik] Lampu Pijar visual: {(isOn ? "ON" : "OFF")}");
    }
}
