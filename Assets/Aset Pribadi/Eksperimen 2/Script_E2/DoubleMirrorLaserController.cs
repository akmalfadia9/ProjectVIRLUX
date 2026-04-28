using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// DoubleMirrorLaserController
/// ─────────────────────────────────────────────────────────────
/// Laser untuk eksperimen 2 cermin bersiku (sudut 90°).
/// - Sinar memantul pada cermin 1 → cermin 2 (maks. maxReflections pantulan)
/// - Setiap pantulan: garis normal, busur θ datang & θ pantul
/// - Label θ1=θ2 (cermin 1) dan θ3=θ4 (cermin 2), nilai dinamis real-time
/// - Semua label selalu menghadap kamera (billboard)
/// - Sinar TIDAK menembus cermin (pengecekan dot product sisi belakang)
/// ─────────────────────────────────────────────────────────────
/// CARA PASANG:
/// 1. Pasang script ini ke GameObject laser (objek hitam).
/// 2. Buat child "LaserTip" di ujung laser → assign ke Laser Origin.
/// 3. Kedua cermin: set Layer = "Mirror", pasang ReflectionMirrorSurface + Box Collider.
/// 4. Di Inspector: set Mirror Layer & Reflectable Layer ke "Mirror".
/// </summary>
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class DoubleMirrorLaserController : MonoBehaviour
{
    // ══════════════════════════════════════════════════════
    // INSPECTOR FIELDS
    // ══════════════════════════════════════════════════════

    [Header("── Laser ──")]
    [Tooltip("Transform ujung laser (arah forward = arah tembak)")]
    public Transform laserOrigin;

    [Tooltip("Layer mask cermin")]
    public LayerMask mirrorLayer;

    [Tooltip("Layer mask semua objek yang bisa kena laser (termasuk cermin)")]
    public LayerMask reflectableLayer;

    [Tooltip("Warna sinar laser")]
    public Color laserColor = Color.red;

    [Tooltip("Lebar sinar laser")]
    public float laserWidth = 0.005f;

    [Tooltip("Jarak maksimal sinar laser")]
    public float maxLaserDistance = 10f;

    [Tooltip("Maksimal jumlah pantulan (rekomendasi: 4)")]
    public int maxReflections = 4;

    [Header("── Garis Normal ──")]
    public Color normalColor = Color.white;
    public float normalWidth = 0.003f;

    [Tooltip("Panjang garis normal ke atas dari titik pantul")]
    public float normalLengthUp = 0.25f;

    [Tooltip("Panjang garis normal ke bawah (ke dalam cermin)")]
    public float normalLengthDown = 0.08f;

    [Header("── Busur Sudut ──")]
    [Tooltip("Warna busur sudut datang (θ ganjil: θ1, θ3)")]
    public Color incidentArcColor = Color.yellow;

    [Tooltip("Warna busur sudut pantul (θ genap: θ2, θ4)")]
    public Color reflectedArcColor = Color.cyan;

    public float arcRadius = 0.10f;
    public int arcSegments = 40;
    public float arcWidth = 0.003f;

    [Header("── Label Sudut ──")]
    [Tooltip("Ukuran karakter label")]
    public float labelCharSize = 0.007f;

    [Tooltip("Font size label")]
    public int labelFontSize = 28;

    [Tooltip("Jarak label dari titik pantul (relatif terhadap arcRadius)")]
    public float labelRadiusMult = 1.8f;

    [Tooltip("Warna label sudut datang (θ1, θ3)")]
    public Color incidentLabelColor = Color.yellow;

    [Tooltip("Warna label sudut pantul (θ2, θ4)")]
    public Color reflectedLabelColor = Color.cyan;

    // ══════════════════════════════════════════════════════
    // PRIVATE
    // ══════════════════════════════════════════════════════

    private bool isLaserOn = false;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private Camera mainCamera;

    // Pools
    private List<LineRenderer> laserLines = new List<LineRenderer>();
    private List<LineRenderer> normalLines = new List<LineRenderer>();
    private List<LineRenderer> arcLines = new List<LineRenderer>();
    private List<TextMesh> labelPool = new List<TextMesh>();
    private int activeLabelCount;

    // Containers (untuk hierarchy yang rapi)
    private GameObject laserContainer;
    private GameObject normalContainer;
    private GameObject arcContainer;
    private GameObject labelContainer;

    // Counter global sudut (θ1, θ2, θ3, θ4, ...)
    private int thetaIndex;

    // ══════════════════════════════════════════════════════
    // LIFECYCLE
    // ══════════════════════════════════════════════════════

    void Awake()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        laserContainer = new GameObject("[DoubleM] LaserLines");
        normalContainer = new GameObject("[DoubleM] NormalLines");
        arcContainer = new GameObject("[DoubleM] ArcLines");
        labelContainer = new GameObject("[DoubleM] AngleLabels");
    }

    void Start()
    {
        grabInteractable.activated.AddListener(OnActivated);
        grabInteractable.deactivated.AddListener(OnDeactivated);

        if (laserOrigin == null)
        {
            laserOrigin = transform;
            Debug.LogWarning("[DoubleMirrorLaserController] laserOrigin tidak di-assign!");
        }

        mainCamera = Camera.main;
        if (mainCamera == null)
            mainCamera = FindAnyObjectByType<Camera>();
    }

    void OnDestroy()
    {
        grabInteractable.activated.RemoveListener(OnActivated);
        grabInteractable.deactivated.RemoveListener(OnDeactivated);
    }

    void OnActivated(ActivateEventArgs args)
    {
        isLaserOn = !isLaserOn;
        if (!isLaserOn) ClearAllVisuals();
    }

    void OnDeactivated(DeactivateEventArgs args) { }

    void Update()
    {
        if (isLaserOn) ShootLaser();

        // Billboard: semua label selalu menghadap kamera
        if (mainCamera != null)
        {
            foreach (var tm in labelPool)
            {
                if (tm == null || !tm.gameObject.activeSelf) continue;
                tm.transform.LookAt(
                    tm.transform.position + mainCamera.transform.rotation * Vector3.forward,
                    mainCamera.transform.rotation * Vector3.up);
            }
        }
    }

    // ══════════════════════════════════════════════════════
    // CORE: SHOOT LASER
    // ══════════════════════════════════════════════════════

    void ShootLaser()
    {
        ClearAllVisuals();
        activeLabelCount = 0;
        thetaIndex = 1; // mulai dari θ1

        Vector3 origin = laserOrigin.position;
        Vector3 direction = laserOrigin.forward;

        List<Vector3> laserPoints = new List<Vector3>();
        laserPoints.Add(origin);

        int reflectionCount = 0;

        while (reflectionCount <= maxReflections)
        {
            RaycastHit hit;
            bool didHit = Physics.Raycast(origin, direction, out hit, maxLaserDistance, reflectableLayer);

            if (!didHit)
            {
                // Sinar bebas hingga max distance
                laserPoints.Add(origin + direction * maxLaserDistance);
                break;
            }

            bool isMirror = ((1 << hit.collider.gameObject.layer) & mirrorLayer) != 0;

            if (isMirror)
            {
                // Tolak sisi belakang cermin
                if (Vector3.Dot(direction, hit.normal) >= 0f)
                {
                    laserPoints.Add(hit.point);
                    break;
                }

                laserPoints.Add(hit.point);

                Vector3 normal = hit.normal;
                Vector3 incomingDir = direction;
                Vector3 reflectedDir = Vector3.Reflect(incomingDir, normal);

                float angleIn = Vector3.Angle(-incomingDir, normal);
                float angleOut = Vector3.Angle(reflectedDir, normal);

                // ── Garis normal ──
                DrawNormalLine(hit.point, normal);

                // ── Busur sudut datang ──
                DrawArc(hit.point, normal, -incomingDir, arcRadius, incidentArcColor, "ArcIn");

                // ── Busur sudut pantul ──
                DrawArc(hit.point, normal, reflectedDir, arcRadius, reflectedArcColor, "ArcOut");

                // ── Label θ_in ──
                Vector3 midIn = ((normal + (-incomingDir).normalized) * 0.5f).normalized;
                DrawLabel(
                    hit.point + midIn * (arcRadius * labelRadiusMult),
                    $"\u03b8{thetaIndex} = {angleIn:F1}\u00b0",
                    incidentLabelColor);
                thetaIndex++;

                // ── Label θ_out ──
                Vector3 midOut = ((normal + reflectedDir.normalized) * 0.5f).normalized;
                DrawLabel(
                    hit.point + midOut * (arcRadius * labelRadiusMult),
                    $"\u03b8{thetaIndex} = {angleOut:F1}\u00b0",
                    reflectedLabelColor);
                thetaIndex++;

                // Lanjut dari titik pantul
                origin = hit.point + reflectedDir * 0.002f;
                direction = reflectedDir;
                reflectionCount++;
            }
            else
            {
                // Kena bukan cermin → berhenti
                laserPoints.Add(hit.point);
                break;
            }
        }

        DrawLaserSegments(laserPoints);
    }

    // ══════════════════════════════════════════════════════
    // DRAWING HELPERS
    // ══════════════════════════════════════════════════════

    void DrawLaserSegments(List<Vector3> points)
    {
        if (points.Count < 2) return;
        LineRenderer lr = GetOrCreateLR(laserLines, laserContainer, "LaserBeam");
        lr.positionCount = points.Count;
        lr.SetPositions(points.ToArray());
        ApplyLR(lr, laserWidth, laserColor);
    }

    void DrawNormalLine(Vector3 hitPoint, Vector3 normal)
    {
        LineRenderer lr = GetOrCreateLR(normalLines, normalContainer, "Normal");
        lr.positionCount = 2;
        lr.SetPosition(0, hitPoint - normal * normalLengthDown);
        lr.SetPosition(1, hitPoint + normal * normalLengthUp);
        ApplyLR(lr, normalWidth, normalColor);
    }

    void DrawArc(Vector3 center, Vector3 fromDir, Vector3 toDir, float radius, Color color, string name)
    {
        fromDir = fromDir.normalized;
        toDir = toDir.normalized;

        Vector3 planeNormal = Vector3.Cross(fromDir, toDir);
        if (planeNormal.magnitude < 0.001f) return;
        planeNormal = planeNormal.normalized;

        float angle = Vector3.Angle(fromDir, toDir);
        if (angle < 0.5f) return;

        LineRenderer lr = GetOrCreateLR(arcLines, arcContainer, name);
        lr.positionCount = arcSegments + 1;
        ApplyLR(lr, arcWidth, color);

        for (int i = 0; i <= arcSegments; i++)
        {
            float t = (float)i / arcSegments;
            Vector3 d = Quaternion.AngleAxis(t * angle, planeNormal) * fromDir;
            lr.SetPosition(i, center + d * radius);
        }
    }

    void DrawLabel(Vector3 worldPos, string text, Color color)
    {
        TextMesh tm;

        if (activeLabelCount < labelPool.Count)
        {
            tm = labelPool[activeLabelCount];
            tm.gameObject.SetActive(true);
        }
        else
        {
            GameObject go = new GameObject("AngleLabel");
            go.transform.SetParent(labelContainer.transform);
            tm = go.AddComponent<TextMesh>();
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            labelPool.Add(tm);
        }

        tm.transform.position = worldPos;
        tm.text = text;
        tm.color = color;
        tm.fontSize = labelFontSize;
        tm.characterSize = labelCharSize;

        activeLabelCount++;
    }

    // ══════════════════════════════════════════════════════
    // UTILITIES
    // ══════════════════════════════════════════════════════

    void ApplyLR(LineRenderer lr, float width, Color color)
    {
        lr.startWidth = width;
        lr.endWidth = width;
        lr.startColor = color;
        lr.endColor = color;
        lr.useWorldSpace = true;
        lr.material = new Material(Shader.Find("Unlit/Color")) { color = color };
    }

    LineRenderer GetOrCreateLR(List<LineRenderer> pool, GameObject container, string objName)
    {
        foreach (var lr in pool)
        {
            if (lr != null && !lr.gameObject.activeSelf)
            {
                lr.gameObject.SetActive(true);
                return lr;
            }
        }

        GameObject go = new GameObject(objName);
        go.transform.SetParent(container.transform);
        var newLr = go.AddComponent<LineRenderer>();
        newLr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        newLr.receiveShadows = false;
        pool.Add(newLr);
        return newLr;
    }

    void ClearAllVisuals()
    {
        foreach (var lr in laserLines) if (lr != null) lr.gameObject.SetActive(false);
        foreach (var lr in normalLines) if (lr != null) lr.gameObject.SetActive(false);
        foreach (var lr in arcLines) if (lr != null) lr.gameObject.SetActive(false);
        foreach (var tm in labelPool) if (tm != null) tm.gameObject.SetActive(false);
    }
}