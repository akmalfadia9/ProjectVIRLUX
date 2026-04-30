using UnityEngine;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// KarakteristikGrafikController
/// Menampilkan grafik intensitas vs waktu secara real-time menggunakan LineRenderer.
/// Graf cahaya: tetap konstan saat lampu ON, turun tiba-tiba saat lampu OFF.
/// Graf bunyi: turun bertahap saat vacuum aktif.
/// Panel grafik TERSEMBUNYI di awal, muncul saat player tekan tombol show graph.
/// Data grafik sinkron dengan panel data angka.
/// </summary>
public class KarakteristikGrafikController : MonoBehaviour
{
    // ─── Graph Panel ──────────────────────────────────────────────────────────
    [Header("=== Panel Grafik Karakteristik ===")]
    [Tooltip("Root panel grafik (awalnya hidden)")]
    public GameObject grafikPanelRoot;

    [Tooltip("Tombol XR untuk show/hide grafik")]
    public XRSimpleInteractable showGrafikButton;

    [Tooltip("Label tombol show grafik")]
    public TMPro.TextMeshPro showGrafikButtonLabel;

    // ─── LineRenderers ────────────────────────────────────────────────────────
    [Header("=== LineRenderer Grafik ===")]
    [Tooltip("LineRenderer untuk garis grafik cahaya (kuning)")]
    public LineRenderer lineGrafikCahaya;

    [Tooltip("LineRenderer untuk garis grafik bunyi (merah)")]
    public LineRenderer lineGrafikBunyi;

    [Tooltip("LineRenderer untuk garis axis X (horizontal)")]
    public LineRenderer lineAxisX;

    [Tooltip("LineRenderer untuk garis axis Y (vertikal)")]
    public LineRenderer lineAxisY;

    // ─── Label Grafik ─────────────────────────────────────────────────────────
    [Header("=== Label Grafik ===")]
    public TMPro.TextMeshPro labelJudulGrafik;
    public TMPro.TextMeshPro labelAxisX;          // "Waktu (s)"
    public TMPro.TextMeshPro labelAxisY;          // "Intensitas"
    public TMPro.TextMeshPro labelGarisKuning;    // "Cahaya (lux)"
    public TMPro.TextMeshPro labelGarisMerah;     // "Bunyi (dB)"
    public TMPro.TextMeshPro labelNilaiCahayaLive;
    public TMPro.TextMeshPro labelNilaiBunyiLive;

    // ─── Konfigurasi Grafik ───────────────────────────────────────────────────
    [Header("=== Konfigurasi Grafik ===")]
    [Tooltip("Lebar area grafik dalam world units")]
    public float grafikWidth    = 0.8f;

    [Tooltip("Tinggi area grafik dalam world units")]
    public float grafikHeight   = 0.4f;

    [Tooltip("Maksimum titik data yang disimpan")]
    public int   maxDataPoints  = 200;

    [Tooltip("Interval sampling data (detik)")]
    public float sampleInterval = 0.2f;

    [Tooltip("Nilai maksimum Y axis (untuk normalisasi)")]
    public float maxIntensitasCahaya = 850f;    // lux - sedikit di atas base value

    [Tooltip("Nilai maksimum Y axis bunyi")]
    public float maxIntensitasBunyi  = 80f;     // dB

    [Header("=== Warna Garis ===")]
    public Color warnaCahaya  = new Color(1f, 0.85f, 0.1f);   // kuning
    public Color warnaBunyi   = new Color(1f, 0.3f, 0.3f);    // merah
    public Color warnaAxis    = new Color(0.7f, 0.7f, 0.7f);  // abu-abu

    // ─── Data History ─────────────────────────────────────────────────────────
    private Queue<float> dataCahaya = new Queue<float>();
    private Queue<float> dataBunyi  = new Queue<float>();

    private float sampleTimer    = 0f;
    private bool  isPanelVisible = false;
    private float elapsedTime    = 0f;

    // ─── Cached values ────────────────────────────────────────────────────────
    private Vector3 grafikOrigin;  // Titik kiri-bawah area grafik (local space)

    // ─────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        grafikOrigin = Vector3.zero;

        // Setup grafik panel - hidden di awal
        if (grafikPanelRoot != null)
            grafikPanelRoot.SetActive(false);

        // Setup button label
        if (showGrafikButtonLabel != null)
            showGrafikButtonLabel.text = "TAMPILKAN\nGRAFIK\nEKSPERIMEN";

        // Setup labels
        SetupLabels();

        // Setup LineRenderer defaults
        SetupLineRenderers();

        // Setup axis lines
        DrawAxis();

        // XR Button
        if (showGrafikButton != null)
            showGrafikButton.selectEntered.AddListener(OnShowGrafikPressed);

        // Seed dengan nol agar grafik tidak kosong
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
        elapsedTime += Time.deltaTime;
        sampleTimer += Time.deltaTime;

        if (sampleTimer >= sampleInterval)
        {
            sampleTimer = 0f;
            SampleData();
        }

        if (isPanelVisible && grafikPanelRoot != null && grafikPanelRoot.activeSelf)
        {
            DrawGrafik();
            UpdateLiveLabels();
        }
    }

    // ─── Data Sampling ────────────────────────────────────────────────────────
    private void SampleData()
    {
        var mgr = KarakteristikExperimentManager.Instance;
        if (mgr == null) return;

        float cahaya = mgr.LightIntensity;
        float bunyi  = mgr.SoundIntensity;

        dataCahaya.Enqueue(cahaya);
        dataBunyi.Enqueue(bunyi);

        // Hapus data lama
        while (dataCahaya.Count > maxDataPoints) dataCahaya.Dequeue();
        while (dataBunyi.Count  > maxDataPoints) dataBunyi.Dequeue();
    }

    // ─── Draw Grafik ──────────────────────────────────────────────────────────
    private void DrawGrafik()
    {
        float[] arrCahaya = new List<float>(dataCahaya).ToArray();
        float[] arrBunyi  = new List<float>(dataBunyi).ToArray();

        int n = Mathf.Min(arrCahaya.Length, arrBunyi.Length);
        if (n == 0) return;

        Vector3[] pointsCahaya = new Vector3[n];
        Vector3[] pointsBunyi  = new Vector3[n];

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / (maxDataPoints - 1);

            float xPos = grafikOrigin.x + t * grafikWidth;
            float yCahaya = grafikOrigin.y + (arrCahaya[i] / maxIntensitasCahaya) * grafikHeight;
            float yBunyi  = grafikOrigin.y + (arrBunyi[i]  / maxIntensitasBunyi)  * grafikHeight;

            // Clamp agar tidak keluar area
            yCahaya = Mathf.Clamp(yCahaya, grafikOrigin.y, grafikOrigin.y + grafikHeight);
            yBunyi  = Mathf.Clamp(yBunyi,  grafikOrigin.y, grafikOrigin.y + grafikHeight);

            pointsCahaya[i] = transform.TransformPoint(new Vector3(xPos, yCahaya, 0f));
            pointsBunyi[i]  = transform.TransformPoint(new Vector3(xPos, yBunyi,  0f));
        }

        if (lineGrafikCahaya != null)
        {
            lineGrafikCahaya.positionCount = n;
            lineGrafikCahaya.SetPositions(pointsCahaya);
        }

        if (lineGrafikBunyi != null)
        {
            lineGrafikBunyi.positionCount = n;
            lineGrafikBunyi.SetPositions(pointsBunyi);
        }
    }

    private void DrawAxis()
    {
        // Axis X: horizontal bawah
        if (lineAxisX != null)
        {
            lineAxisX.positionCount = 2;
            lineAxisX.SetPosition(0, transform.TransformPoint(new Vector3(grafikOrigin.x, grafikOrigin.y, 0f)));
            lineAxisX.SetPosition(1, transform.TransformPoint(new Vector3(grafikOrigin.x + grafikWidth, grafikOrigin.y, 0f)));
        }

        // Axis Y: vertikal kiri
        if (lineAxisY != null)
        {
            lineAxisY.positionCount = 2;
            lineAxisY.SetPosition(0, transform.TransformPoint(new Vector3(grafikOrigin.x, grafikOrigin.y, 0f)));
            lineAxisY.SetPosition(1, transform.TransformPoint(new Vector3(grafikOrigin.x, grafikOrigin.y + grafikHeight, 0f)));
        }
    }

    // ─── Setup Helpers ────────────────────────────────────────────────────────
    private void SetupLineRenderers()
    {
        float lineWidth = 0.005f;

        SetupLine(lineGrafikCahaya, warnaCahaya, lineWidth);
        SetupLine(lineGrafikBunyi,  warnaBunyi,  lineWidth);
        SetupLine(lineAxisX,        warnaAxis,   lineWidth * 0.5f);
        SetupLine(lineAxisY,        warnaAxis,   lineWidth * 0.5f);
    }

    private void SetupLine(LineRenderer lr, Color color, float width)
    {
        if (lr == null) return;
        lr.startColor      = color;
        lr.endColor        = color;
        lr.startWidth      = width;
        lr.endWidth        = width;
        lr.useWorldSpace   = true;
        lr.material        = new Material(Shader.Find("Sprites/Default"));
    }

    private void SetupLabels()
    {
        if (labelJudulGrafik    != null) labelJudulGrafik.text    = "GRAFIK INTENSITAS vs WAKTU\nEKSPERIMEN KARAKTERISTIK GELOMBANG";
        if (labelAxisX          != null) labelAxisX.text          = "Waktu (s)";
        if (labelAxisY          != null) labelAxisY.text          = "Intensitas";
        if (labelGarisKuning    != null) labelGarisKuning.text    = "—— Cahaya (lux)";
        if (labelGarisMerah     != null) labelGarisMerah.text     = "—— Bunyi (dB)";
    }

    private void UpdateLiveLabels()
    {
        var mgr = KarakteristikExperimentManager.Instance;
        if (mgr == null) return;

        if (labelNilaiCahayaLive != null)
            labelNilaiCahayaLive.text = $"Cahaya: {mgr.LightIntensity:F1} lux";

        if (labelNilaiBunyiLive != null)
            labelNilaiBunyiLive.text  = $"Bunyi: {mgr.SoundIntensity:F1} dB";
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

        // Reset grafik saat dibuka ulang
        if (isPanelVisible) DrawAxis();

        Debug.Log($"[Karakteristik] Grafik Panel: {(isPanelVisible ? "TAMPIL" : "TERSEMBUNYI")}");
    }

    public bool IsPanelVisible => isPanelVisible;
}
