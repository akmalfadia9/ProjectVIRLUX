using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.ProBuilder;

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

    [Header("Laser Settings")]
    [Tooltip("Transform tempat sinar laser keluar (ujung laser)")]
    public Transform laserOrigin;

    [Tooltip("Layer mask untuk cermin")]
    public LayerMask mirrorLayer;

    [Tooltip("Layer mask untuk semua objek yang bisa dipantulkan")]
    public LayerMask reflectableLayer;

    [Header("Normal Line Settings")]
    [Tooltip("Warna garis normal")]
    public Color normalColor = Color.white;

    public Material normalMaterial;

    public LineRenderer entryNormalRenderer;

    [Tooltip("Panjang garis normal ke atas dari titik pantul")]
    public float normalLineLength = 3f;

    private LineRenderer _entryNormalLR;

    // Angle of incident
    private LineRenderer _arcEntry;
    private LineRenderer _incidentLine;
    private float _angleIncidence;
    private Color entryArcColor = new Color(1f, 1f, 1f, 0.5f); // Semi-transparent white


    [Tooltip("Lebar garis normal")]
    public float normalLineWidth = 0.003f;

    [Header("=== ARC SETTINGS ===")]
    public float arcRadius = 1f;
    public int arcSegments = 30;
    private LineRenderer _arcRed;
    private LineRenderer _arcYellow;
    private LineRenderer _arcPurple;


    private float _angleRed, _angleYellow, _anglePurple;


    [Tooltip("Lebar busur")]
    public float arcWidth = 1f;

    [Header("=== LINE RENDERER SPEKTRUM ===")]
    [Tooltip("LineRenderer untuk sinar datang (putih). Sudah ada di scene.")]
    public LineRenderer incomingRayRenderer;

    [Tooltip("LineRenderers untuk 6 warna spektrum keluar. Urutan: Merah, Jingga, Kuning, Hijau, Biru, Ungu")]
    public LineRenderer[] spectrumRenderers = new LineRenderer[6];

    [Tooltip("Panjang sinar spektrum yang keluar (meter)")]
    [Range(0.1f, 10f)]
    public float exitRayLength = 5f;

    [Header("=== LABEL DATA FISIKA ===")]
    [Tooltip("Prefab TextMeshPro World Space untuk menampilkan data kalkulasi real-time")]
    public GameObject physicsLabelPrefab;

    [Tooltip("Offset posisi label dari titik tumbukan")]
    public Vector3 labelOffset = new Vector3(0.1f, 0.2f, 0);


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


    public void OnRayHit(Vector3 hitPoint, Vector3 incomingDirection, Vector3 hitNormal)
    {
        _isActive = true;
        Vector3 exitPoint = new();
        Vector3 exitNormal;

        // --- AUTO-INITIALIZE ---
        if (_entryNormalLR == null) _entryNormalLR = CreateNormalLine("EntryNormal_Auto", Color.white);

        // --- DRAW ENTRY NORMAL ---
        _entryNormalLR.enabled = true;
        _entryNormalLR.SetPosition(0, hitPoint);
        //_entryNormalLR.SetPosition(1, hitPoint - (hitNormal * normalLineLength));
        _entryNormalLR.SetPosition(1, hitPoint + (incomingDirection * normalLineLength));

        // ---1.INCIDENCE ANGLE & ARC-- -
        // We use -incomingDirection because we want the angle from the "start" of the laser
        // and hitNormal which points OUT of the prism.
        _angleIncidence = Vector3.Angle(hitNormal, -incomingDirection.normalized);

        if (_arcEntry == null) _arcEntry = CreateNormalLine("Arc_Entry", entryArcColor);

        // Draw arc between Normal and the direction the laser is coming FROM
        DrawArc(_arcEntry, hitPoint, hitNormal, -incomingDirection.normalized, arcRadius);

        // Incidence Line
        if (_incidentLine == null) _incidentLine = CreateNormalLine("IncidentNormal_Auto", Color.white);
        _incidentLine.enabled = true;
        _incidentLine.SetPosition(0, hitPoint + (hitNormal * normalLineLength));
        _incidentLine.SetPosition(1, hitPoint - (hitNormal * normalLineLength));



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
                exitPoint = exitHit.point;
                exitNormal = exitHit.normal;

                // --- STEP 3: EXIT REFRACTION (Glass to Air) ---
                // Use -exitNormal because the ray hits the back face from inside
                Vector3 finalExitDir = Refract(internalDir, -exitNormal, n2 / 1.0f);

                // Final angle from normal line
                float currentAngle = Vector3.Angle(exitHit.normal, finalExitDir);

                if (i == 0) _angleRed = currentAngle;
                if (i == 2) _angleYellow = currentAngle;
                if (i == 5) _anglePurple = currentAngle;

                // --- STEP 4: VISUALS (3 Points) ---
                spectrumRenderers[i].enabled = true;
                spectrumRenderers[i].positionCount = 3;

                // Draw light curve
                AnimationCurve curve = new AnimationCurve();

                // Key 0: Start of the line (Entry Point) -> Width 0.005
                curve.AddKey(0.0f, 0.005f);

                // Key 1: Middle of the line (Exit Point) -> Width 0.01
                // (0.5 is the middle of the position array, not physical distance)
                curve.AddKey(0.5f, 0.01f);

                // Key 2: End of the line (Outer Space) -> Width 0.05
                curve.AddKey(1.0f, 0.05f);

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

                // --- DRAW SPECIFIC ARCS ---
                if (i == 0) // RED
                {
                    if (_arcRed == null) _arcRed = CreateNormalLine("Arc_Red", Color.red);
                    DrawArc(_arcRed, exitHit.point, exitHit.normal, finalExitDir, arcRadius * 0.8f);
                }
                else if (i == 2) // YELLOW
                {
                    if (_arcYellow == null) _arcYellow = CreateNormalLine("Arc_Yellow", Color.yellow);
                    DrawArc(_arcYellow, exitHit.point, exitHit.normal, finalExitDir, arcRadius);
                }
                else if (i == 5) // PURPLE
                {
                    if (_arcPurple == null) _arcPurple = CreateNormalLine("Arc_Purple", new Color(0.7f, 0, 1f));
                    DrawArc(_arcPurple, exitHit.point, exitHit.normal, finalExitDir, arcRadius * 1.2f);
                }
            }
            else
            {
                spectrumRenderers[i].enabled = false;
            }
        }

        if (_isActive)
        {
            string multiAngleText =
                $"<color=white>Incidence: {_angleIncidence:F1}°</color>\n" +
                "------------------\n" +
                $"<color=#FF1A1A>Red: {_angleRed:F2}°</color>\n" +
                $"<color=#FFE600>Yellow: {_angleYellow:F2}°</color>\n" +
                $"<color=#B300FF>Violet: {_anglePurple:F2}°</color>";

            // Use the last hitPoint and an offset for the label
            UpdateMultiPhysicsLabel(exitPoint, multiAngleText);
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
    private LineRenderer CreateNormalLine(string name, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(this.transform); // Keeps hierarchy clean
        go.layer = 2;

        LineRenderer lr = go.AddComponent<LineRenderer>();
        if (normalMaterial != null)
        {
            lr.material = normalMaterial;
        }
        else
        {
            lr.material = new Material(Shader.Find("Sprites/Default"));
        }

        lr.startColor = lr.endColor = color;
        lr.startWidth = lr.endWidth = normalLineWidth;

        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.enabled = false; // Hidden by default
        return lr;
    }

    private void DrawArc(LineRenderer lr, Vector3 center, Vector3 normalDir, Vector3 exitDir, float radius)
    {
        lr.enabled = true;
        lr.positionCount = arcSegments + 1;
        lr.startWidth = lr.endWidth = arcWidth;

        for (int i = 0; i <= arcSegments; i++)
        {
            float progress = (float)i / arcSegments;
            // Slerp creates the curved path between the Normal and the Ray
            Vector3 pointDir = Vector3.Slerp(normalDir.normalized, exitDir.normalized, progress);
            lr.SetPosition(i, center + (pointDir * radius));
        }
    }


    private void HideNormals()
    {
        if (_entryNormalLR != null) _entryNormalLR.enabled = false;
        if (_incidentLine != null) _incidentLine.enabled = false;
    }

    private void HideArcs()
    {
        if (_arcRed != null) _arcRed.enabled = false;
        if (_arcYellow != null) _arcYellow.enabled = false;
        if (_arcPurple != null) _arcPurple.enabled = false;
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
    private void UpdateMultiPhysicsLabel(Vector3 point, string text)
    {

        if (_labelInstance != null)
        {
            _labelInstance.SetActive(true);
            _labelTMP.text = text;
            _labelTMP.alignment = TextAlignmentOptions.Center;
            _labelTMP.fontSize = 4;

            // Position it slightly above the exit point
            _labelInstance.transform.position = point + labelOffset;

            // Face the user/camera
            if (Camera.main != null)
            {
                _labelInstance.transform.LookAt(_labelInstance.transform.position + Camera.main.transform.rotation * Vector3.forward,
                                             Camera.main.transform.rotation * Vector3.up);
                _labelInstance.transform.Rotate(0, 180, 0);
            }
        }
    }


    // =====================================================================
    // HELPER
    // =====================================================================

    private void HideAllSpectrum()
    {
        foreach (var lr in spectrumRenderers)
            if (lr != null) lr.enabled = false;
        HideNormals();
        HideArcs();
    }

    private void ValidateSetup()
    {
        if (prismMaterial == null)
            Debug.LogError($"[PrismDispersion] {name}: PrismMaterial belum di-assign!");

        if (spectrumRenderers.Length != 6)
            Debug.LogWarning($"[PrismDispersion] {name}: Dibutuhkan tepat 6 LineRenderer spektrum.");
    }

}
