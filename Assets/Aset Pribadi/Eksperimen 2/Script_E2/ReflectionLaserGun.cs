using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// ReflectionLaserGun - Laser XR dengan pemantulan cermin, garis normal, busur sudut,
/// dan label θ1 / θ2 dinamis yang selalu menghadap kamera.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class ReflectionLaserGun : MonoBehaviour
{
    [Header("Laser Settings")]
    [Tooltip("Transform tempat sinar laser keluar (ujung laser)")]
    public Transform laserOrigin;

    [Tooltip("Layer mask untuk cermin")]
    public LayerMask mirrorLayer;

    [Tooltip("Layer mask untuk semua objek yang bisa dipantulkan")]
    public LayerMask reflectableLayer;

    [Tooltip("Warna sinar laser")]
    public Color laserColor = Color.red;

    [Tooltip("Lebar sinar laser")]
    public float laserWidth = 0.005f;

    [Tooltip("Panjang maksimal sinar laser")]
    public float maxLaserDistance = 10f;

    [Tooltip("Maksimal pantulan")]
    public int maxReflections = 5;

    [Header("Normal Line Settings")]
    [Tooltip("Warna garis normal")]
    public Color normalColor = Color.white;

    [Tooltip("Lebar garis normal")]
    public float normalWidth = 0.003f;

    [Tooltip("Panjang garis normal ke atas dari titik pantul")]
    public float normalLength = 0.3f;

    [Header("Arc Settings")]
    [Tooltip("Warna busur sudut datang")]
    public Color incidentArcColor = Color.yellow;

    [Tooltip("Warna busur sudut pantul")]
    public Color reflectedArcColor = Color.cyan;

    [Tooltip("Radius busur sudut")]
    public float arcRadius = 0.12f;

    [Tooltip("Jumlah segmen busur")]
    public int arcSegments = 40;

    [Tooltip("Lebar busur")]
    public float arcWidth = 0.003f;

    [Header("Angle Label Settings")]
    [Tooltip("Ukuran karakter label sudut")]
    public float labelCharSize = 0.008f;

    [Tooltip("Font size label")]
    public int labelFontSize = 28;

    [Tooltip("Warna label θ1 (sudut datang)")]
    public Color label1Color = Color.yellow;

    [Tooltip("Warna label θ2 (sudut pantul)")]
    public Color label2Color = Color.cyan;

    // ── State ──────────────────────────────────────────────
    private bool isLaserOn = false;
    private XRGrabInteractable grabInteractable;
    private Camera mainCamera;

    // ── Line Renderer Pools ────────────────────────────────
    private List<LineRenderer> laserLines = new List<LineRenderer>();
    private List<LineRenderer> normalLines = new List<LineRenderer>();
    private List<LineRenderer> arcLines = new List<LineRenderer>();

    // ── Label Pool ─────────────────────────────────────────
    private List<TextMesh> labelPool = new List<TextMesh>();

    // ── Containers ─────────────────────────────────────────
    private GameObject laserContainer;
    private GameObject normalContainer;
    private GameObject arcContainer;
    private GameObject labelContainer;

    // ── Active label count (reset per frame) ───────────────
    private int activeLabelCount = 0;

    // ══════════════════════════════════════════════════════
    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        laserContainer = new GameObject("[Laser] LaserLines");
        normalContainer = new GameObject("[Laser] NormalLines");
        arcContainer = new GameObject("[Laser] ArcLines");
        labelContainer = new GameObject("[Laser] AngleLabels");
    }

    void Start()
    {
        grabInteractable.activated.AddListener(OnActivated);
        grabInteractable.deactivated.AddListener(OnDeactivated);

        if (laserOrigin == null)
        {
            laserOrigin = this.transform;
            Debug.LogWarning("[ReflectionLaserGun] laserOrigin tidak di-assign, menggunakan transform sendiri.");
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

        // Billboard: label selalu hadap kamera
        if (mainCamera != null)
        {
            foreach (var tm in labelPool)
            {
                if (tm != null && tm.gameObject.activeSelf)
                {
                    tm.transform.LookAt(
                        tm.transform.position + mainCamera.transform.rotation * Vector3.forward,
                        mainCamera.transform.rotation * Vector3.up);
                }
            }
        }
    }

    // ══════════════════════════════════════════════════════
    void ShootLaser()
    {
        ClearAllVisuals();
        activeLabelCount = 0;

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
                laserPoints.Add(origin + direction * maxLaserDistance);
                break;
            }

            bool isMirror = ((1 << hit.collider.gameObject.layer) & mirrorLayer) != 0;

            if (isMirror)
            {
                float dot = Vector3.Dot(direction, hit.normal);

                // Sisi belakang cermin: hentikan sinar, jangan tembus
                if (dot >= 0f)
                {
                    laserPoints.Add(hit.point);
                    break;
                }

                laserPoints.Add(hit.point);

                Vector3 normal = hit.normal;
                Vector3 incomingDir = direction;
                Vector3 reflectedDir = Vector3.Reflect(incomingDir, normal);

                // Sudut terhadap garis normal
                float angleIncident = Vector3.Angle(-incomingDir, normal);
                float angleReflected = Vector3.Angle(reflectedDir, normal);

                // Gambar garis normal
                DrawNormalLine(hit.point, normal);

                // Gambar busur
                DrawAngleArcs(hit.point, normal, -incomingDir, reflectedDir);

                // Gambar label θ1 dan θ2
                Vector3 incidentMid = ((normal + (-incomingDir).normalized) * 0.5f).normalized;
                Vector3 reflectedMid = ((normal + reflectedDir.normalized) * 0.5f).normalized;

                DrawLabel(hit.point + incidentMid * (arcRadius * 1.6f), $"\u03b81 = {angleIncident:F1}\u00b0", label1Color);
                DrawLabel(hit.point + reflectedMid * (arcRadius * 1.6f), $"\u03b82 = {angleReflected:F1}\u00b0", label2Color);

                // Lanjut pantulan
                origin = hit.point + reflectedDir * 0.002f;
                direction = reflectedDir;
                reflectionCount++;
            }
            else
            {
                // Bukan cermin, berhenti
                laserPoints.Add(hit.point);
                break;
            }
        }

        DrawLaserSegments(laserPoints);
    }

    // ══════════════════════════════════════════════════════
    // DRAWING
    // ══════════════════════════════════════════════════════

    void DrawLaserSegments(List<Vector3> points)
    {
        if (points.Count < 2) return;
        LineRenderer lr = GetOrCreateLR(laserLines, laserContainer, "LaserBeam");
        lr.positionCount = points.Count;
        lr.SetPositions(points.ToArray());
        SetLR(lr, laserWidth, laserColor);
    }

    void DrawNormalLine(Vector3 hitPoint, Vector3 normal)
    {
        LineRenderer lr = GetOrCreateLR(normalLines, normalContainer, "Normal");
        // Garis normal: sedikit ke bawah (ke dalam cermin) + ke atas
        lr.positionCount = 2;
        lr.SetPosition(0, hitPoint - normal * (normalLength * 0.3f));
        lr.SetPosition(1, hitPoint + normal * normalLength);
        SetLR(lr, normalWidth, normalColor);
    }

    void DrawAngleArcs(Vector3 hitPoint, Vector3 normal, Vector3 incidentDir, Vector3 reflectedDir)
    {
        DrawArc(hitPoint, normal, incidentDir, arcRadius, incidentArcColor, "ArcIncident");
        DrawArc(hitPoint, normal, reflectedDir, arcRadius, reflectedArcColor, "ArcReflected");
    }

    void DrawArc(Vector3 center, Vector3 fromDir, Vector3 toDir, float radius, Color color, string name)
    {
        fromDir = fromDir.normalized;
        toDir = toDir.normalized;

        Vector3 planeNormal = Vector3.Cross(fromDir, toDir);
        if (planeNormal.magnitude < 0.001f) return;
        planeNormal = planeNormal.normalized;

        float angle = Vector3.Angle(fromDir, toDir);

        LineRenderer lr = GetOrCreateLR(arcLines, arcContainer, name);
        lr.positionCount = arcSegments + 1;
        SetLR(lr, arcWidth, color);

        for (int i = 0; i <= arcSegments; i++)
        {
            float t = (float)i / arcSegments;
            Vector3 d = Quaternion.AngleAxis(t * angle, planeNormal) * fromDir;
            lr.SetPosition(i, center + d * radius);
        }
    }

    void DrawLabel(Vector3 worldPos, string text, Color color)
    {
        TextMesh tm = null;

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

    void SetLR(LineRenderer lr, float width, Color color)
    {
        lr.startWidth = width;
        lr.endWidth = width;
        lr.startColor = color;
        lr.endColor = color;
        lr.material = CreateUnlitMaterial(color);
        lr.useWorldSpace = true;
    }

    LineRenderer GetOrCreateLR(List<LineRenderer> pool, GameObject container, string objName)
    {
        foreach (var existing in pool)
        {
            if (existing != null && !existing.gameObject.activeSelf)
            {
                existing.gameObject.SetActive(true);
                return existing;
            }
        }

        GameObject go = new GameObject(objName);
        go.transform.SetParent(container.transform);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        pool.Add(lr);
        return lr;
    }

    void ClearAllVisuals()
    {
        foreach (var lr in laserLines) if (lr != null) lr.gameObject.SetActive(false);
        foreach (var lr in normalLines) if (lr != null) lr.gameObject.SetActive(false);
        foreach (var lr in arcLines) if (lr != null) lr.gameObject.SetActive(false);
        foreach (var tm in labelPool) if (tm != null) tm.gameObject.SetActive(false);
    }

    Material CreateUnlitMaterial(Color color)
    {
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = color;
        return mat;
    }
}