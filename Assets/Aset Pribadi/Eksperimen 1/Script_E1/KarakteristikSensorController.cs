using UnityEngine;

/// <summary>
/// KarakteristikSensorController
/// Script untuk 3D model sensor yang ditempatkan di titik pengukuran.
/// Sensor ini membaca intensitas cahaya dan bunyi secara real-time dari ExperimentManager.
/// Dapat ditempatkan di dalam atau di luar bell jar untuk simulasi pengukuran.
/// </summary>
public class KarakteristikSensorController : MonoBehaviour
{
    [Header("=== Identitas Sensor Karakteristik ===")]
    [Tooltip("Nama/ID sensor untuk identifikasi")]
    public string sensorName = "Sensor-A";

    public enum KarakteristikSensorType
    {
        CahayaDanBunyi,    // Sensor gabungan (default)
        KhususCahaya,      // Hanya baca intensitas cahaya
        KhususBunyi        // Hanya baca intensitas bunyi
    }
    public KarakteristikSensorType sensorType = KarakteristikSensorType.CahayaDanBunyi;

    [Header("=== Nilai Sensor Real-time ===")]
    [SerializeField, ReadOnly] private float bacaanIntensitasCahaya = 0f;  // lux
    [SerializeField, ReadOnly] private float bacaanIntensitasBunyi  = 0f;  // dB

    [Header("=== Visual Sensor ===")]
    [Tooltip("Renderer LED indikator sensor aktif")]
    public Renderer sensorLedRenderer;

    [Tooltip("Material LED saat sensor mendeteksi sinyal")]
    public Material ledDetectingMaterial;

    [Tooltip("Material LED saat sensor idle")]
    public Material ledIdleMaterial;

    [Header("=== Label Floating ===")]
    [Tooltip("TextMeshPro untuk label floating di atas sensor (opsional)")]
    public TMPro.TextMeshPro floatingLabel;

    [Tooltip("Tampilkan label floating?")]
    public bool showFloatingLabel = true;

    // ─── Properties publik untuk diakses Panel Data ───────────────────────────
    public float BacaanCahayaLux => bacaanIntensitasCahaya;
    public float BacaanBunyiDb  => bacaanIntensitasBunyi;

    private bool isDetecting = false;

    // ─────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        if (KarakteristikExperimentManager.Instance != null)
        {
            KarakteristikExperimentManager.Instance.OnLightIntensityChanged += OnCahayaUpdate;
            KarakteristikExperimentManager.Instance.OnSoundIntensityChanged += OnBunyiUpdate;
        }
        UpdateFloatingLabel();
    }

    private void OnDestroy()
    {
        if (KarakteristikExperimentManager.Instance != null)
        {
            KarakteristikExperimentManager.Instance.OnLightIntensityChanged -= OnCahayaUpdate;
            KarakteristikExperimentManager.Instance.OnSoundIntensityChanged -= OnBunyiUpdate;
        }
    }

    private void Update()
    {
        // Update setiap frame untuk smooth display
        if (KarakteristikExperimentManager.Instance != null)
        {
            bacaanIntensitasCahaya = KarakteristikExperimentManager.Instance.LightIntensity;
            bacaanIntensitasBunyi  = KarakteristikExperimentManager.Instance.SoundIntensity;
        }

        bool newDetecting = bacaanIntensitasCahaya > 0.1f || bacaanIntensitasBunyi > 0.1f;
        if (newDetecting != isDetecting)
        {
            isDetecting = newDetecting;
            RefreshSensorLed();
        }

        if (showFloatingLabel) UpdateFloatingLabel();
    }

    private void OnCahayaUpdate(float value) => bacaanIntensitasCahaya = value;
    private void OnBunyiUpdate(float value)  => bacaanIntensitasBunyi  = value;

    private void RefreshSensorLed()
    {
        if (sensorLedRenderer == null) return;
        sensorLedRenderer.material = isDetecting ? ledDetectingMaterial : ledIdleMaterial;
    }

    private void UpdateFloatingLabel()
    {
        if (floatingLabel == null) return;

        string text = $"[{sensorName}]\n";
        switch (sensorType)
        {
            case KarakteristikSensorType.CahayaDanBunyi:
                text += $"Cahaya: {bacaanIntensitasCahaya:F1} lux\n";
                text += $"Bunyi:  {bacaanIntensitasBunyi:F1} dB";
                break;
            case KarakteristikSensorType.KhususCahaya:
                text += $"Cahaya: {bacaanIntensitasCahaya:F1} lux";
                break;
            case KarakteristikSensorType.KhususBunyi:
                text += $"Bunyi: {bacaanIntensitasBunyi:F1} dB";
                break;
        }
        floatingLabel.text = text;
    }
}
