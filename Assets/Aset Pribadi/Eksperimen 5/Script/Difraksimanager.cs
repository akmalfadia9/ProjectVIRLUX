using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// DifraksiManager.cs — v3
/// Fix utama: tambah pengaliVisual agar pola difraksi terlihat di layar
/// tanpa mengubah nilai fisika yang ditampilkan di label.
/// Fisika: Fraunhofer — I(θ) = I₀·[sin(β)/β]², y_m = mλL/a
/// </summary>
public class DifraksiManager : MonoBehaviour
{
    [Header("── Objek Scene ──")]
    public Transform titikTembakLaser;
    public Transform objekCelah;   // Empty GO di TENGAH bukaan celah
    public Transform objekLayar;   // Empty GO di TENGAH permukaan layar

    [Header("── Laser Utama ──")]
    public LineRenderer laserBeamUtama;
    public float lebarLaserUtama = 0.005f;

    [Header("── Beam Difraksi ──")]
    public GameObject difraksiBeamPrefab;
    public Transform difraksiBeamsParent;

    [Range(1, 5)]
    public int jumlahOrdo = 3;

    [Header("── Label di Layar ──")]
    public GameObject labelPolaLayarPrefab;
    public Transform labelLayarParent;

    [Header("── UI ──")]
    public TMP_Dropdown dropdownCelah;
    public TextMeshProUGUI textInfoParameter;
    public TextMeshProUGUI textStatusLaser;

    [Header("── Parameter Fisika ──")]
    [Tooltip("Panjang gelombang laser merah = 650nm. Jangan diubah.")]
    public float lambda = 650e-9f;

    [Tooltip("Berapa meter fisika per 1 unit Unity di scene ini.\n" +
             "Contoh: jika 1 unit Unity = 1 cm, isi 0.01\n" +
             "Contoh: jika 1 unit Unity = 1 m,  isi 1.0")]
    public float meterPerUnit = 0.01f;

    [Header("── Skala Visual Pola ──")]
    [Tooltip("PENGALI VISUAL SAJA — tidak mempengaruhi nilai fisika di label.\n" +
             "Naikkan nilai ini jika pola difraksi tidak terlihat menyebar di layar.\n" +
             "Coba nilai: 10, 50, 100, 500, 1000 hingga pola terlihat.\n" +
             "Nilai ini mengkompensasi perbedaan skala scene dengan skala fisika nyata.")]
    public float pengaliVisual = 100f;

    [Tooltip("Sumbu arah penyebaran pola. Vector3.up = menyebar atas-bawah.")]
    public Vector3 arahSpread = Vector3.up;

    // 3 pilihan celah
    private readonly float[] nilaiCelah = { 0.0001f, 0.0003f, 0.0005f };
    private readonly string[] labelCelah =
    {
        "0.01 cm  —  Sempit",
        "0.03 cm  —  Sedang",
        "0.05 cm  —  Lebar"
    };

    private bool laserAktif = false;
    private float celahSaatIni;
    private int indexCelah = 0;

    private readonly List<GameObject> poolBeam = new List<GameObject>();
    private readonly List<GameObject> poolLabel = new List<GameObject>();

    // ─────────────────────────────────────────────
    void Start()
    {
        celahSaatIni = nilaiCelah[0];
        SetupDropdown();
        if (laserBeamUtama != null) laserBeamUtama.enabled = false;
        UpdateTextInfo();
    }

    void SetupDropdown()
    {
        if (dropdownCelah == null) return;
        dropdownCelah.ClearOptions();
        dropdownCelah.AddOptions(new List<string>(labelCelah));
        dropdownCelah.value = 0;
        dropdownCelah.onValueChanged.AddListener(OnDropdownChanged);
    }

    // ─────────────────────────────────────────────
    //  PUBLIC
    // ─────────────────────────────────────────────

    public void ToggleLaser()
    {
        laserAktif = !laserAktif;
        if (laserAktif) NyalakanLaser();
        else MatikanLaser();
        UpdateTextInfo();
    }

    public void OnDropdownChanged(int index)
    {
        indexCelah = index;
        celahSaatIni = nilaiCelah[index];
        if (laserAktif) { BersihkanDifraksi(); StartCoroutine(DelayDifraksi()); }
        UpdateTextInfo();
    }

    // ─────────────────────────────────────────────
    //  LASER
    // ─────────────────────────────────────────────

    void NyalakanLaser()
    {
        if (laserBeamUtama == null || titikTembakLaser == null || objekCelah == null) return;

        laserBeamUtama.enabled = true;
        laserBeamUtama.useWorldSpace = true;
        laserBeamUtama.positionCount = 2;
        laserBeamUtama.SetPosition(0, titikTembakLaser.position);
        laserBeamUtama.SetPosition(1, objekCelah.position);
        laserBeamUtama.startWidth = lebarLaserUtama;
        laserBeamUtama.endWidth = lebarLaserUtama;
        laserBeamUtama.startColor = new Color(1f, 0.05f, 0.05f, 1f);
        laserBeamUtama.endColor = new Color(1f, 0.05f, 0.05f, 1f);
        laserBeamUtama.material = new Material(Shader.Find("Sprites/Default"));

        StartCoroutine(DelayDifraksi());
    }

    void MatikanLaser()
    {
        if (laserBeamUtama != null) laserBeamUtama.enabled = false;
        BersihkanDifraksi();
    }

    IEnumerator DelayDifraksi()
    {
        yield return new WaitForSeconds(0.1f);
        TampilkanPolaDifraksi();
    }

    // ─────────────────────────────────────────────
    //  FISIKA
    // ─────────────────────────────────────────────

    float HitungIntensitas(float yMeter, float L, float a)
    {
        if (Mathf.Abs(yMeter) < 1e-12f) return 1f;
        float sinT = yMeter / Mathf.Sqrt(yMeter * yMeter + L * L);
        float beta = (Mathf.PI * a * sinT) / lambda;
        if (Mathf.Abs(beta) < 1e-9f) return 1f;
        float sb = Mathf.Sin(beta);
        return (sb / beta) * (sb / beta);
    }

    // Posisi gelap ke-m dalam meter fisika
    float YGelapMeter(int m, float a, float L) => (m * lambda * L) / a;
    // Posisi terang sekunder ke-m dalam meter fisika
    float YTerangMeter(int m, float a, float L) => ((m + 0.5f) * lambda * L) / a;

    // ─────────────────────────────────────────────
    //  TAMPILKAN POLA
    // ─────────────────────────────────────────────

    void TampilkanPolaDifraksi()
    {
        if (objekCelah == null || objekLayar == null)
        {
            Debug.LogError("[DifraksiManager] objekCelah atau objekLayar belum diassign!");
            return;
        }

        Vector3 posCelah = objekCelah.position;
        Vector3 posLayar = objekLayar.position;
        Vector3 spread = arahSpread.normalized;

        // Jarak dalam meter fisika
        float L_unity = Vector3.Distance(posCelah, posLayar);
        float L_meter = L_unity * meterPerUnit;
        float a = celahSaatIni;

        Debug.Log($"[Difraksi] L_unity={L_unity:F3}, L_meter={L_meter:F6}, a={a}, " +
                  $"pengaliVisual={pengaliVisual}");

        // ── TERANG PUSAT ──
        float lebarPusatMeter = 2f * YGelapMeter(1, a, L_meter);
        float lebarPusatCm = lebarPusatMeter * 100f;

        BuatBeam(posCelah, posLayar, 0.012f,
            new Color(1f, 0.1f, 0.1f, 1f),
            new Color(1f, 0.1f, 0.1f, 0.7f));
        BuatLabel(posLayar, posCelah, Vector3.zero,
            "Terang Pusat (m=0)", 1f, lebarPusatCm);

        // ── ORDO ±1, ±2, ... ──
        for (int m = 1; m <= jumlahOrdo; m++)
        {
            float yGelap_m = YGelapMeter(m, a, L_meter);
            float yGelap_m1 = YGelapMeter(m + 1, a, L_meter);
            float yTerang_m = YTerangMeter(m, a, L_meter);

            float lebarFringeCm = (yGelap_m1 - yGelap_m) * 100f;
            float intens = HitungIntensitas(yTerang_m, L_meter, a);

            // *** KUNCI FIX: offset visual menggunakan pengaliVisual ***
            // Nilai meter fisika → Unity units pakai pengaliVisual agar terlihat
            float offsetVisual = (yTerang_m / meterPerUnit) * pengaliVisual;
            float gelapVisual = (yGelap_m / meterPerUnit) * pengaliVisual;

            Debug.Log($"[Difraksi] m={m}: yTerang={yTerang_m:E3}m, " +
                      $"offsetVisual={offsetVisual:F4} Unity units");

            float alpha = Mathf.Lerp(0.85f, 0.2f, (float)(m - 1) / jumlahOrdo);
            Color warna = new Color(1f, 0.1f, 0.1f, alpha);
            float lebar = Mathf.Max(0.002f, 0.01f - m * 0.002f);

            // Atas
            Vector3 tujuanAtas = posLayar + spread * offsetVisual;
            BuatBeam(posCelah, tujuanAtas, lebar, warna,
                new Color(1f, 0.05f, 0.05f, alpha * 0.3f));
            BuatLabel(posLayar, posCelah, spread * offsetVisual,
                $"Terang ke-{m} (m={m})", intens, lebarFringeCm);

            // Bawah
            Vector3 tujuanBawah = posLayar - spread * offsetVisual;
            BuatBeam(posCelah, tujuanBawah, lebar, warna,
                new Color(1f, 0.05f, 0.05f, alpha * 0.3f));
            BuatLabel(posLayar, posCelah, -spread * offsetVisual,
                $"Terang ke-{m} (m={m})", intens, lebarFringeCm);

            // Label gelap (tanpa beam)
            BuatLabel(posLayar, posCelah, spread * gelapVisual,
                $"Gelap ke-{m} (m={m})", 0f, 0f);
            BuatLabel(posLayar, posCelah, -spread * gelapVisual,
                $"Gelap ke-{m} (m={m})", 0f, 0f);
        }
    }

    // ─────────────────────────────────────────────
    //  HELPER: BEAM
    // ─────────────────────────────────────────────

    void BuatBeam(Vector3 asal, Vector3 tujuan, float lebar,
                  Color cAsal, Color cTujuan)
    {
        if (difraksiBeamPrefab == null || difraksiBeamsParent == null) return;

        GameObject go = Instantiate(difraksiBeamPrefab, difraksiBeamsParent);
        poolBeam.Add(go);

        LineRenderer lr = go.GetComponent<LineRenderer>();
        if (lr == null) lr = go.AddComponent<LineRenderer>();

        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.SetPosition(0, asal);
        lr.SetPosition(1, tujuan);
        lr.startWidth = lebar;
        lr.endWidth = lebar * 2.5f;
        lr.startColor = cAsal;
        lr.endColor = cTujuan;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder = 2;
    }

    // ─────────────────────────────────────────────
    //  HELPER: LABEL
    // ─────────────────────────────────────────────

    void BuatLabel(Vector3 posLayar, Vector3 posCelah, Vector3 offsetVisual,
                   string namaOrdo, float intensitas, float lebarCm)
    {
        if (labelPolaLayarPrefab == null || labelLayarParent == null) return;

        GameObject go = Instantiate(labelPolaLayarPrefab, labelLayarParent);
        poolLabel.Add(go);

        // Geser label sedikit ke arah celah agar tidak tertanam di layar
        Vector3 arahKeCelah = (posCelah - posLayar).normalized;
        go.transform.position = posLayar + offsetVisual + arahKeCelah * 0.05f;

        TextMeshPro tmp = go.GetComponent<TextMeshPro>();
        if (tmp == null) tmp = go.AddComponent<TextMeshPro>();

        tmp.fontSize = 0.025f;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = Color.white;

        if (intensitas <= 0f)
        {
            tmp.text = $"<color=#888888><b>{namaOrdo}</b>\nI = 0</color>";
        }
        else
        {
            string hex = namaOrdo.Contains("Pusat") ? "#FFFFFF" : "#FFBBBB";
            string persen = (intensitas * 100f).ToString("F1");
            string lebar = lebarCm.ToString("F4");
            tmp.text =
                $"<color={hex}><b>{namaOrdo}</b></color>\n" +
                $"<size=80%>I = <b>{persen}%</b> I₀\n" +
                $"Δy = <b>{lebar} cm</b></size>";
        }
    }

    // ─────────────────────────────────────────────
    //  INFO TEXT
    // ─────────────────────────────────────────────

    void UpdateTextInfo()
    {
        if (textInfoParameter == null) return;

        float L_unity = (objekCelah != null && objekLayar != null)
            ? Vector3.Distance(objekCelah.position, objekLayar.position) : 1f;
        float L_meter = L_unity * meterPerUnit;
        float lebarPusat = 2f * YGelapMeter(1, celahSaatIni, L_meter) * 100f;

        textInfoParameter.text =
            "<b>═ Difraksi Celah Tunggal ═</b>\n" +
            $"a = <b>{celahSaatIni * 100f:F2} cm</b>\n" +
            $"λ = <b>{lambda * 1e9f:F0} nm</b>\n" +
            $"L = <b>{L_meter:F4} m</b>\n" +
            "─────────────────\n" +
            $"Lebar terang pusat:\n<b>{lebarPusat:F4} cm</b>\n" +
            "y = mλL/a\n" +
            "─────────────────\n" +
            (laserAktif
                ? "<color=#FF4444>■ LASER AKTIF</color>"
                : "<color=#888888>□ Laser Mati</color>");

        if (textStatusLaser != null)
            textStatusLaser.text = laserAktif ? "● AKTIF" : "○ Mati";
    }

    // ─────────────────────────────────────────────
    //  CLEANUP
    // ─────────────────────────────────────────────

    void BersihkanDifraksi()
    {
        foreach (var g in poolBeam) if (g) Destroy(g);
        foreach (var g in poolLabel) if (g) Destroy(g);
        poolBeam.Clear();
        poolLabel.Clear();
    }

    void OnDestroy() => BersihkanDifraksi();

    // ─────────────────────────────────────────────
    //  GIZMOS
    // ─────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        if (titikTembakLaser != null && objekCelah != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(titikTembakLaser.position, objekCelah.position);
        }
        if (objekCelah != null && objekLayar != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(objekCelah.position, objekLayar.position);

            // Visualisasi arah spread dan estimasi posisi terang pertama
            if (Application.isPlaying)
            {
                float L_m = Vector3.Distance(objekCelah.position, objekLayar.position) * meterPerUnit;
                float y1 = YTerangMeter(1, celahSaatIni, L_m);
                float off = (y1 / meterPerUnit) * pengaliVisual;
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(objekLayar.position + arahSpread.normalized * off, 0.02f);
                Gizmos.DrawSphere(objekLayar.position - arahSpread.normalized * off, 0.02f);
            }

            Gizmos.color = Color.green;
            Gizmos.DrawRay(objekLayar.position, arahSpread.normalized * 0.3f);
            Gizmos.DrawRay(objekLayar.position, -arahSpread.normalized * 0.3f);
        }
    }
}