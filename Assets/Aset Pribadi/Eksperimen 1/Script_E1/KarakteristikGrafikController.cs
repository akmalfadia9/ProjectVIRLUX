using UnityEngine;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// KarakteristikGrafikController - Fixed Version
/// Grafik intensitas vs waktu menggunakan LineRenderer.
/// Posisi grafik otomatis mengikuti posisi Canvas (tidak perlu setting manual).
/// </summary>
public class KarakteristikGrafikController : MonoBehaviour
{
    [Header("=== Panel Grafik Karakteristik ===")]
    public GameObject grafikPanelRoot;
    public XRSimpleInteractable showGrafikButton;
    public TMPro.TextMeshProUGUI showGrafikButtonLabel;

    [Header("=== LineRenderer Grafik ===")]
    public LineRenderer lineGrafikCahaya;
    public LineRenderer lineGrafikBunyi;
    public LineRenderer lineAxisX;
    public LineRenderer lineAxisY;

    [Header("=== Label Grafik ===")]
    public TMPro.TextMeshProUGUI labelJudulGrafik;
    public TMPro.TextMeshProUGUI labelAxisX;
    public TMPro.TextMeshProUGUI labelAxisY;
    public TMPro.TextMeshProUGUI labelGarisKuning;
    public TMPro.TextMeshProUGUI labelGarisMerah;
    public TMPro.TextMeshProUGUI labelNilaiCahayaLive;
    public TMPro.TextMeshProUGUI labelNilaiBunyiLive;

    [Header("=== Konfigurasi Grafik ===")]
    [Tooltip("Lebar area grafik dalam world units")]
    public float grafikWidth = 0.5f;

    [Tooltip("Tinggi area grafik dalam world units")]
    public float grafikHeight = 0.25f;

    [Tooltip("Offset dari pusat Canvas ke pojok kiri bawah grafik")]
    public Vector3 grafikOffset = new Vector3(-0.22f, -0.1f, -0.01f);

    public int maxDataPoints = 150;
    public float sampleInterval = 0.2f;
    public float maxIntensitasCahaya = 850f;
    public float maxIntensitasBunyi = 80f;

    [Header("=== Warna ===")]
    public Color warnaCahaya = new Color(1f, 0.85f, 0.1f);
    public Color warnaBunyi = new Color(1f, 0.3f, 0.3f);
    public Color warnaAxis = new Color(0.7f, 0.7f, 0.7f);

    [Header("=== Debug ===")]
    [Tooltip("Aktifkan untuk lihat titik origin grafik di Scene view")]
    public bool showDebugGizmo = true;

    // ─── Data ─────────────────────────────────────────────────────────────────
    private Queue<float> dataCahaya = new Queue<float>();
    private Queue<float> dataBunyi = new Queue<float>();
    private float sampleTimer = 0f;
    private bool isPanelVisible = false;

    // Origin grafik di world space (dihitung otomatis dari Canvas)
    private Vector3 worldOrigin;
    private Vector3 worldRight;
    private Vector3 worldUp;

    // ─────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        if (grafikPanelRoot != null)
            grafikPanelRoot.SetActive(false);

        if (showGrafikButtonLabel != null)
            showGrafikButtonLabel.text = "TAMPILKAN\nGRAFIK\nEKSPERIMEN";

        SetupLabels();
        SetupLineRenderers();

        if (showGrafikButton != null)
            showGrafikButton.selectEntered.AddListener(OnShowGrafikPressed);

        for (int i = 0; i < 10; i++)
        {
            dataCahaya.Enqueue(0f);
            dataBunyi.Enqueue(0f);
        }
    }

    private void OnDestroy()
    {
        if (showGrafikButton != null)
            showGrafikButton.selectEntered.RemoveListener(OnShowGrafikPressed);
    }

    private void Update()
    {
        sampleTimer += Time.deltaTime;
        if (sampleTimer >= sampleInterval)
        {
            sampleTimer = 0f;
            SampleData();
        }

        if (isPanelVisible && grafikPanelRoot != null && grafikPanelRoot.activeSelf)
        {
            UpdateGrafikOrigin();
            DrawAxis();
            DrawGrafik();
            UpdateLiveLabels();
        }
    }

    // ─── Hitung origin grafik dari posisi Canvas secara otomatis ──────────────
    private void UpdateGrafikOrigin()
    {
        if (grafikPanelRoot == null) return;

        Transform canvasTF = grafikPanelRoot.transform;

        worldRight = canvasTF.right;
        worldUp = canvasTF.up;

        worldOrigin = canvasTF.position
                    + canvasTF.right * grafikOffset.x
                    + canvasTF.up * grafikOffset.y
                    + canvasTF.forward * grafikOffset.z;
    }

    // ─── Sampling Data ────────────────────────────────────────────────────────
    private void SampleData()
    {
        var mgr = KarakteristikExperimentManager.Instance;
        if (mgr == null) return;

        dataCahaya.Enqueue(mgr.LightIntensity);
        dataBunyi.Enqueue(mgr.SoundIntensity);

        while (dataCahaya.Count > maxDataPoints) dataCahaya.Dequeue();
        while (dataBunyi.Count > maxDataPoints) dataBunyi.Dequeue();
    }

    // ─── Draw Grafik ──────────────────────────────────────────────────────────
    private void DrawGrafik()
    {
        float[] arrCahaya = new List<float>(dataCahaya).ToArray();
        float[] arrBunyi = new List<float>(dataBunyi).ToArray();
        int n = Mathf.Min(arrCahaya.Length, arrBunyi.Length);
        if (n == 0) return;

        Vector3[] ptsCahaya = new Vector3[n];
        Vector3[] ptsBunyi = new Vector3[n];

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / (maxDataPoints - 1);
            float tC = Mathf.Clamp01(arrCahaya[i] / maxIntensitasCahaya);
            float tB = Mathf.Clamp01(arrBunyi[i] / maxIntensitasBunyi);

            ptsCahaya[i] = worldOrigin
                         + worldRight * (t * grafikWidth)
                         + worldUp * (tC * grafikHeight);

            ptsBunyi[i] = worldOrigin
                         + worldRight * (t * grafikWidth)
                         + worldUp * (tB * grafikHeight);
        }

        if (lineGrafikCahaya != null)
        {
            lineGrafikCahaya.positionCount = n;
            lineGrafikCahaya.SetPositions(ptsCahaya);
        }
        if (lineGrafikBunyi != null)
        {
            lineGrafikBunyi.positionCount = n;
            lineGrafikBunyi.SetPositions(ptsBunyi);
        }
    }

    // ─── Draw Axis ────────────────────────────────────────────────────────────
    private void DrawAxis()
    {
        if (lineAxisX != null)
        {
            lineAxisX.positionCount = 2;
            lineAxisX.SetPosition(0, worldOrigin);
            lineAxisX.SetPosition(1, worldOrigin + worldRight * grafikWidth);
        }

        if (lineAxisY != null)
        {
            lineAxisY.positionCount = 2;
            lineAxisY.SetPosition(0, worldOrigin);
            lineAxisY.SetPosition(1, worldOrigin + worldUp * grafikHeight);
        }
    }

    // ─── Setup Helpers ────────────────────────────────────────────────────────
    private void SetupLineRenderers()
    {
        SetupLine(lineGrafikCahaya, warnaCahaya, 0.004f);
        SetupLine(lineGrafikBunyi, warnaBunyi, 0.004f);
        SetupLine(lineAxisX, warnaAxis, 0.002f);
        SetupLine(lineAxisY, warnaAxis, 0.002f);
    }

    private void SetupLine(LineRenderer lr, Color color, float width)
    {
        if (lr == null) return;
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.useWorldSpace = true;
        lr.positionCount = 0;

        if (lr.material == null || lr.material.name.Contains("Default-Line"))
            lr.material = new Material(Shader.Find("Sprites/Default"));

        lr.material.color = color;
    }

    private void SetupLabels()
    {
        if (labelJudulGrafik != null) labelJudulGrafik.text = "GRAFIK INTENSITAS vs WAKTU";
        if (labelAxisX != null) labelAxisX.text = "Waktu (s)";
        if (labelAxisY != null) labelAxisY.text = "Intensitas";
        if (labelGarisKuning != null) labelGarisKuning.text = "— Cahaya (lux)";
        if (labelGarisMerah != null) labelGarisMerah.text = "— Bunyi (dB)";
    }

    private void UpdateLiveLabels()
    {
        var mgr = KarakteristikExperimentManager.Instance;
        if (mgr == null) return;
        if (labelNilaiCahayaLive != null)
            labelNilaiCahayaLive.text = $"Cahaya: {mgr.LightIntensity:F1} lux";
        if (labelNilaiBunyiLive != null)
            labelNilaiBunyiLive.text = $"Bunyi: {mgr.SoundIntensity:F1} dB";
    }

    // ─── Toggle Panel ─────────────────────────────────────────────────────────
    private void OnShowGrafikPressed(SelectEnterEventArgs args) => ToggleGrafikPanel();
    public void OnShowGrafikClick() => ToggleGrafikPanel();

    private void ToggleGrafikPanel()
    {
        isPanelVisible = !isPanelVisible;
        if (grafikPanelRoot != null)
            grafikPanelRoot.SetActive(isPanelVisible);

        if (showGrafikButtonLabel != null)
            showGrafikButtonLabel.text = isPanelVisible
                ? "SEMBUNYIKAN\nGRAFIK"
                : "TAMPILKAN\nGRAFIK\nEKSPERIMEN";

        if (isPanelVisible)
        {
            UpdateGrafikOrigin();
            DrawAxis();
        }

        Debug.Log($"[Karakteristik] Grafik Panel: {(isPanelVisible ? "TAMPIL" : "TERSEMBUNYI")}");
    }

    // ─── Reset Grafik ─────────────────────────────────────────────────────────
    /// <summary>
    /// Dipanggil oleh KarakteristikResetController.
    /// Menghapus semua data history grafik dan menyembunyikan panel.
    /// </summary>
    public void ResetGrafik()
    {
        // Hapus semua data history
        dataCahaya.Clear();
        dataBunyi.Clear();

        // Seed ulang dengan nol
        for (int i = 0; i < 10; i++)
        {
            dataCahaya.Enqueue(0f);
            dataBunyi.Enqueue(0f);
        }

        // Kosongkan LineRenderer
        if (lineGrafikCahaya != null) lineGrafikCahaya.positionCount = 0;
        if (lineGrafikBunyi != null) lineGrafikBunyi.positionCount = 0;

        // Sembunyikan panel
        isPanelVisible = false;
        if (grafikPanelRoot != null)
            grafikPanelRoot.SetActive(false);

        if (showGrafikButtonLabel != null)
            showGrafikButtonLabel.text = "TAMPILKAN\nGRAFIK\nEKSPERIMEN";

        // Reset timer sampling
        sampleTimer = 0f;

        Debug.Log("[Karakteristik] Grafik direset.");
    }

    // ─── Gizmo debug ─────────────────────────────────────────────────────────
    private void OnDrawGizmos()
    {
        if (!showDebugGizmo || !isPanelVisible) return;
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(worldOrigin, 0.02f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(worldOrigin, worldOrigin + worldRight * grafikWidth);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(worldOrigin, worldOrigin + worldUp * grafikHeight);
    }
}