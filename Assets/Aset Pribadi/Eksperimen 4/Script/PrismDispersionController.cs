using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Komponen utama yang di-attach pada setiap GameObject Prisma.
/// Mendeteksi tumbukan laser, menjalankan kalkulasi dispersi,
/// dan menampilkan LineRenderer spectrum + label TMP.
///
/// SETUP INSPECTOR:
///   - Prisma harus memiliki Collider (MeshCollider atau BoxCollider)
///   - Assign PrismMaterial ScriptableObject sesuai jenis prisma
///   - Isi field LineRenderer dan prefab label di Inspector
/// </summary>
public class PrismDispersionController : MonoBehaviour
{
    // =====================================================================
    // INSPECTOR FIELDS
    // =====================================================================

    [Header("=== MATERIAL PRISMA ===")]
    [Tooltip("ScriptableObject berisi indeks bias material prisma ini")]
    public PrismMaterial prismMaterial;

    [Tooltip("Sudut pembias (apex angle) prisma dalam derajat. Default 60° untuk prisma equilateral.")]
    [Range(10f, 85f)]
    public float apexAngleDeg = 60f;

    [Tooltip("Layer mask. Set ke layer 'Prism' saja agar tidak interference objek lain.")]
    public LayerMask prismLayerMask = ~0;

    [Header("=== GEOMETRI PERMUKAAN ===")]
    [Tooltip("Transform yang mewakili permukaan masuk sinar (Surface 1). Arah forward = normal permukaan.")]
    public Transform surface1;

    [Tooltip("Transform yang mewakili permukaan keluar sinar (Surface 2). Arah forward = normal permukaan.")]
    public Transform surface2;

    [Header("=== LINE RENDERER SPEKTRUM ===")]
    [Tooltip("LineRenderer untuk sinar datang (putih). Sudah ada di scene.")]
    public LineRenderer incomingRayRenderer;

    [Tooltip("LineRenderers untuk 6 warna spektrum keluar. Urutan: Merah, Jingga, Kuning, Hijau, Biru, Ungu")]
    public LineRenderer[] spectrumRenderers = new LineRenderer[6];

    [Tooltip("Panjang sinar spektrum yang keluar (meter)")]
    [Range(0.1f, 10f)]
    public float exitRayLength = 3f;

    [Header("=== LABEL DATA FISIKA ===")]
    [Tooltip("Prefab TextMeshPro World Space untuk menampilkan data kalkulasi real-time")]
    public GameObject physicsLabelPrefab;

    [Tooltip("Offset posisi label dari titik tumbukan")]
    public Vector3 labelOffset = new Vector3(0.1f, 0.3f, 0);


    [Header("=== DEBUG ===")]
    public bool showDebugGizmos = true;

    // =====================================================================
    // PRIVATE FIELDS
    // =====================================================================

    // Warna Unity untuk setiap enum SpectrumColor
    private static readonly Color[] SpectrumColors = new Color[]
    {
        new Color(1.0f, 0.1f, 0.1f),   // Red
        new Color(1.0f, 0.5f, 0.0f),   // Orange
        new Color(1.0f, 0.9f, 0.0f),   // Yellow
        new Color(0.1f, 0.9f, 0.1f),   // Green
        new Color(0.1f, 0.4f, 1.0f),   // Blue
        new Color(0.7f, 0.0f, 1.0f),   // Violet
    };

    private GameObject _labelInstance;
    private TextMeshPro _labelTMP;

    // Hasil kalkulasi terakhir — disimpan agar dapat di-update
    private DispersionPhysics.DispersionResult[] _lastResults =
        new DispersionPhysics.DispersionResult[6];

    private bool _isActive = false;           // Apakah ada sinar yang sedang mengenai prisma?
    private Vector3 _lastHitPoint;            // Titik tumbukan terakhir
    private Vector3 _lastIncomingDir;         // Arah sinar datang terakhir

    // =====================================================================
    // UNITY LIFECYCLE
    // =====================================================================

    private void Start()
    {
        ValidateSetup();
        InitializeLabel();
        HideAllSpectrum();
    }

    private void Update()
    {
        // Jika tidak ada tumbukan aktif, sembunyikan semua
        if (!_isActive)
        {
            HideAllSpectrum();
            if (_labelInstance != null) _labelInstance.SetActive(false);
        }
        // Reset flag setiap frame — LaserEmitter akan mengeset ulang via OnRayHit()
        _isActive = false;
    }

    // =====================================================================
    // PUBLIC API — dipanggil oleh LaserEmitter
    // =====================================================================

    /// <summary>
    /// Dipanggil oleh LaserEmitter setiap frame ketika sinar mengenai prisma ini.
    /// Menjalankan kalkulasi fisika dan memperbarui visualisasi.
    /// </summary>
    /// <param name="hitPoint">Titik tumbukan dalam world space</param>
    /// <param name="incomingDirection">Arah sinar datang (normalized)</param>
    /// <param name="hitNormal">Normal permukaan di titik tumbukan (world space)</param>
    public void OnRayHit(Vector3 hitPoint, Vector3 incomingDirection, Vector3 hitNormal)
    {
        _isActive = true;
        _lastHitPoint = hitPoint;
        _lastIncomingDir = incomingDirection;


        // 2. Calculate Internal Angle (theta2) using Yellow as the "average" index
        float n1 = 1.0f; // Air
        float n2 = prismMaterial.GetRefractiveIndex(SpectrumColor.Yellow);
        float eta = n1 / n2;

        // Calculate the incidence angle
        //float theta1 = Vector3.Angle(-hitNormal, incomingDirection);
        // Calculate refraction angle (theta2)
        //float theta2 = DispersionPhysics.SnellRefraction(theta1, n1, n2);

        //if (float.IsNaN(theta2)) return; // Total Internal Reflection at entry (rare)

        // 3. Create the Internal Direction Vector
        // We rotate the -hitNormal by theta2 relative to the incoming direction
        //Vector3 rotationAxis = Vector3.Cross(-hitNormal, incomingDirection).normalized;
        //Vector3 internalDir = Quaternion.AngleAxis(theta2, rotationAxis) * -hitNormal;
        Vector3 internalDir = Refract(incomingDirection.normalized, hitNormal.normalized, eta);

        if (internalDir == Vector3.zero) return;

        // 2. Find the Exit Point using a raycast from INSIDE
        // Start slightly inside to avoid hitting the entry wall again
        Vector3 internalOrigin = hitPoint + (internalDir * 0.001f);

        // Enable "Queries Hit Backfaces" so we can hit the inside of the mesh
        bool oldBackfaceSetting = Physics.queriesHitBackfaces;
        Physics.queriesHitBackfaces = true;

        if (Physics.Raycast(internalOrigin, internalDir, out RaycastHit exitHit, 2f, prismLayerMask))
        {
            Vector3 exitPoint = exitHit.point;
            Vector3 exitNormal = exitHit.normal; // This is the normal of the back wall

            Debug.DrawLine(hitPoint, exitPoint, Color.yellow);

            // 3. Calculate dispersion for all colors based on this specific exitNormal
            for (int i = 0; i < 6; i++)
            {
                //SpectrumColor col = (SpectrumColor)i;
                //_lastResults[i] = DispersionPhysics.CalculateDispersion(
                //    color: col,
                //    material: prismMaterial,
                //    incidenceAngleDeg: theta1,
                //    apexAngleDeg: apexAngleDeg, // Note: You might need to adjust this math if apex isn't constant
                //    surfaceNormal1: hitNormal,
                //    surfaceNormal2: -exitNormal, // Flip because RaycastHit normal points OUT
                //    incomingDirection: incomingDirection
                //);

                float nExit1 = prismMaterial.GetRefractiveIndex((SpectrumColor)i);
                float nExit2 = 1.0f;
                float etaExit = nExit1 / nExit2;

                // Note: Use -exitNormal because the ray hits the "back" of the face
                Vector3 finalExitDir = Refract(internalDir, -exitNormal, etaExit);

                // Update individual line renderer positions
                if (spectrumRenderers[i] != null && !_lastResults[i].totalInternalReflection)
                {
                    spectrumRenderers[i].enabled = true;
                    spectrumRenderers[i].SetPosition(0, exitPoint);
                    //spectrumRenderers[i].SetPosition(1, exitPoint + _lastResults[i].exitDirection * exitRayLength);
                    spectrumRenderers[i].SetPosition(1, exitPoint + finalExitDir * exitRayLength);
                }
            }
        }

        Physics.queriesHitBackfaces = oldBackfaceSetting; // Restore setting
    }


    public void OnRayHit2(Vector3 hitPoint, Vector3 incomingDirection, Vector3 hitNormal)
    {
        _isActive = true;

        bool oldBackface = Physics.queriesHitBackfaces;
        Physics.queriesHitBackfaces = true;

        for (int i = 0; i < 6; i++)
        {
            SpectrumColor color = (SpectrumColor)i;
            if (spectrumRenderers[i] == null) continue;

            // --- STEP 1: ENTRY REFRACTION (Air to Glass) ---
            float n1 = 1.0f;
            float n2 = prismMaterial.GetRefractiveIndex(color);
            Vector3 internalDir = Refract(incomingDirection.normalized, hitNormal.normalized, n1 / n2);

            if (internalDir == Vector3.zero) { spectrumRenderers[i].enabled = false; continue; }

            // --- STEP 2: INTERNAL PATH (Finding unique exit point) ---
            Vector3 internalOrigin = hitPoint + (internalDir * 0.001f);
            if (Physics.Raycast(internalOrigin, internalDir, out RaycastHit exitHit, 2f, prismLayerMask))
            {
                Vector3 exitPoint = exitHit.point;
                Vector3 exitNormal = exitHit.normal;

                // --- STEP 3: EXIT REFRACTION (Glass to Air) ---
                // Use -exitNormal because the ray hits the back face from inside
                Vector3 finalExitDir = Refract(internalDir, -exitNormal, n2 / 1.0f);

                // --- STEP 4: VISUALS (3 Points) ---
                spectrumRenderers[i].enabled = true;
                spectrumRenderers[i].positionCount = 3;

                // Draw light curve
                AnimationCurve curve = new AnimationCurve();

                // Key 0: Start of the line (Entry Point) -> Width 0.005
                curve.AddKey(0.0f, 0.002f);

                // Key 1: Middle of the line (Exit Point) -> Width 0.01
                // (0.5 is the middle of the position array, not physical distance)
                curve.AddKey(0.5f, 0.005f);

                // Key 2: End of the line (Outer Space) -> Width 0.05
                curve.AddKey(1.0f, 0.03f);

                // Apply the curve to the LineRenderer
                spectrumRenderers[i].widthCurve = curve;
                spectrumRenderers[i].widthMultiplier = 1.0f;

                // Point 0: Where the white light hits the prism
                spectrumRenderers[i].SetPosition(0, hitPoint);
                // Point 1: Where this specific color hits the back wall (Inside)
                spectrumRenderers[i].SetPosition(1, exitPoint);
                // Point 2: Where the light goes after leaving (Outside)
                spectrumRenderers[i].SetPosition(2, exitPoint + finalExitDir * exitRayLength);

                // Gradient: White at entry -> Color at exit
                spectrumRenderers[i].startColor = Color.white;
                spectrumRenderers[i].endColor = SpectrumColors[i];
            }
            else
            {
                spectrumRenderers[i].enabled = false;
            }
        }

        Physics.queriesHitBackfaces = oldBackface;
    }


    Vector3 Refract(Vector3 incoming, Vector3 normal, float eta)
    {
        float dot = Vector3.Dot(normal, incoming);
        float k = 1.0f - eta * eta * (1.0f - dot * dot);

        if (k < 0.0f)
            return Vector3.zero; // Total Internal Reflection
        else
            return eta * incoming - (eta * dot + Mathf.Sqrt(k)) * normal;
    }

    // =====================================================================
    // VISUALISASI SPEKTRUM
    // =====================================================================

    /// <summary>
    /// Memperbarui posisi dan warna seluruh LineRenderer spektrum.
    /// Titik awal = titik keluar (surface2 hit), titik akhir = arah bias × length.
    ///
    /// Untuk menyederhanakan, kita gunakan hitPoint di surface2 sebagai
    /// titik origin semua sinar keluar, lalu pisahkan dengan offset kecil
    /// agar tidak overlap secara visual.
    /// </summary>
    private void UpdateSpectrumVisualForIndex(int i, Vector3 exitPoint, Vector3 exitDirection)
    {
            if (exitDirection == Vector3.zero)
            {
                spectrumRenderers[i].enabled = false;
            }

            spectrumRenderers[i].enabled = true;

            // Posisi: dari exitPoint ke arah exitDirection × panjang
            Vector3 endPoint = exitPoint + exitDirection * exitRayLength;

            spectrumRenderers[i].SetPosition(0, exitPoint);
            spectrumRenderers[i].SetPosition(1, endPoint);

            // Warna gradien dari putih (masuk) ke warna spektrum (keluar)
            spectrumRenderers[i].startColor = Color.white;
            spectrumRenderers[i].endColor = SpectrumColors[i];
    }

    /// <summary>
    /// Estimasi titik keluar sinar dari prisma.
    /// Melakukan Raycast dari titik masuk ke arah dalam prisma,
    /// dan mencari titik tumbukan kedua dengan permukaan prisma.
    /// </summary>
    private Vector3 EstimateExitPoint(Vector3 entryPoint)
    {
        if (_lastResults.Length == 0) return entryPoint + transform.forward * 0.1f;

        // Gunakan hasil warna kuning (tengah spektrum) sebagai representasi
        var refResult = _lastResults[(int)SpectrumColor.Yellow];

        // Hitung arah dalam prisma setelah refraksi permukaan 1
        Vector3 normal1 = (surface1 != null) ? surface1.forward : transform.forward;
        Vector3 insideDir = DispersionPhysics.CalculateExitDirection(
            _lastIncomingDir, -normal1, refResult.theta2);

        // Raycast dari titik masuk ke arah dalam prisma untuk menemukan permukaan 2
        Ray insideRay = new Ray(entryPoint + insideDir * 0.001f, insideDir);
        if (Physics.Raycast(insideRay, out RaycastHit hit2, 2f))
        {
            // Pastikan hanya mendeteksi prisma ini sendiri
            if (hit2.collider.gameObject == gameObject ||
                hit2.collider.transform.IsChildOf(transform))
            {
                return hit2.point;
            }
        }

        // Fallback: gunakan posisi surface2 jika raycast gagal
        return surface2 != null ? surface2.position : entryPoint + transform.forward * 0.15f;
    }

    // =====================================================================
    // LABEL FISIKA (TextMeshPro World Space)
    // =====================================================================

    /// <summary>
    /// Inisialisasi instance label TMP. Dibuat sekali dan di-reuse.
    /// </summary>
    private void InitializeLabel()
    {
        if (physicsLabelPrefab == null) return;

        _labelInstance = Instantiate(physicsLabelPrefab, transform);
        _labelTMP = _labelInstance.GetComponentInChildren<TextMeshPro>();
        _labelInstance.SetActive(false);
    }

    /// <summary>
    /// Memperbarui konten label dengan data kalkulasi terbaru.
    /// Menampilkan sudut-sudut kunci dan variasi per-warna.
    /// </summary>
    private void UpdatePhysicsLabel(
        Vector3 hitPoint,
        DispersionPhysics.DispersionResult[] results)
    {
        if (_labelInstance == null || _labelTMP == null) return;

        _labelInstance.SetActive(true);

        // Posisikan label di dekat titik tumbukan
        _labelInstance.transform.position = hitPoint + labelOffset;

        // Arahkan label agar selalu menghadap kamera (billboard)
        if (Camera.main != null)
            _labelInstance.transform.LookAt(Camera.main.transform);

        // Ambil hasil referensi (kuning = tengah spektrum)
        var yellow = results[(int)SpectrumColor.Yellow];
        var red    = results[(int)SpectrumColor.Red];
        var violet = results[(int)SpectrumColor.Violet];

        // -------------------------------------------------------
        // FORMAT TEKS LABEL
        // -------------------------------------------------------
        string labelText =
            $"<b><color=#FFD700>[ {prismMaterial.materialName} ]</color></b>\n" +
            $"<size=80%><color=#AAAAAA>──────────────────</color></size>\n" +
            $"<b>θ₁</b> Sudut Datang Pertama : <color=#00FFFF>{yellow.theta1:F1}°</color>\n" +
            $"<b>θ₂</b> Sudut Bias Pertama   : <color=#00FFFF>{yellow.theta2:F1}°</color>\n" +
            $"<b>A</b>  Sudut Pembias (Apex) : <color=#FFAA00>{apexAngleDeg:F1}°</color>\n" +
            $"<b>θ₃</b> Sudut Datang Akhir   : <color=#00FFFF>{yellow.theta3:F1}°</color>\n" +
            $"<size=80%><color=#AAAAAA>──── Dispersi ────</color></size>\n" +
            $"<color=#FF4444>Merah</color>  θ₄ = {(float.IsNaN(red.theta4) ? "TIR" : $"{red.theta4:F1}°")}" +
            $"   D = {(float.IsNaN(red.deviationAngle) ? "TIR" : $"{red.deviationAngle:F1}°")}\n" +
            $"<color=#CC44FF>Ungu</color>   θ₄ = {(float.IsNaN(violet.theta4) ? "TIR" : $"{violet.theta4:F1}°")}" +
            $"   D = {(float.IsNaN(violet.deviationAngle) ? "TIR" : $"{violet.deviationAngle:F1}°")}\n" +
            $"<size=75%><color=#888888>Δθ₄ = {(float.IsNaN(violet.theta4) || float.IsNaN(red.theta4) ? "N/A" : $"{Mathf.Abs(violet.theta4 - red.theta4):F2}°")} (ungu − merah)</color></size>";

        _labelTMP.text = labelText;
    }

    // =====================================================================
    // HELPER
    // =====================================================================

    private void HideAllSpectrum()
    {
        foreach (var lr in spectrumRenderers)
            if (lr != null) lr.enabled = false;
    }

    private void ValidateSetup()
    {
        if (prismMaterial == null)
            Debug.LogError($"[PrismDispersion] {name}: PrismMaterial belum di-assign!");

        if (surface1 == null || surface2 == null)
            Debug.LogWarning($"[PrismDispersion] {name}: Surface1/Surface2 belum di-assign. " +
                             "Perhitungan arah mungkin tidak akurat.");

        if (spectrumRenderers.Length != 6)
            Debug.LogWarning($"[PrismDispersion] {name}: Dibutuhkan tepat 6 LineRenderer spektrum.");
    }

    // =====================================================================
    // GIZMOS (Editor Visualization)
    // =====================================================================

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Tampilkan normal permukaan
        if (surface1 != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(surface1.position, surface1.forward * 0.3f);
            Gizmos.DrawWireSphere(surface1.position, 0.03f);
        }
        if (surface2 != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(surface2.position, surface2.forward * 0.3f);
            Gizmos.DrawWireSphere(surface2.position, 0.03f);
        }

        // Tampilkan arah sinar keluar jika ada kalkulasi
        if (Application.isPlaying && _isActive)
        {
            Vector3 exitPt = EstimateExitPoint(_lastHitPoint);
            for (int i = 0; i < 6; i++)
            {
                if (_lastResults[i].totalInternalReflection) continue;
                Gizmos.color = SpectrumColors[i];
                Gizmos.DrawRay(exitPt, _lastResults[i].exitDirection * 0.5f);
            }
        }
    }
}
