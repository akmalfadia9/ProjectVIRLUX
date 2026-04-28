using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class RefractionLaserGun : MonoBehaviour
{
    [Header("Laser Settings")]
    public Transform laserOrigin;
    public float maxLaserLength = 20f;
    public LayerMask laserLayerMask = ~0;
    public int maxBounces = 10;

    [Header("Visuals")]
    public Material laserMaterial;
    public Color laserColor = new Color(0f, 0.5f, 1f, 1f);
    public float laserWidth = 0.008f;

    [Header("Angle Indicators")]
    public GameObject angleIndicatorPrefab;

    private XRGrabInteractable _grab;
    private bool _firing;
    private LineRenderer _lr;
    private readonly List<AngleIndicator> _active = new();
    private readonly List<AngleIndicator> _pool = new();

    void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();

        _lr = gameObject.AddComponent<LineRenderer>();
        _lr.useWorldSpace = true;
        _lr.enabled = false;

        _grab.activated.AddListener(OnActivated);
        _grab.deactivated.AddListener(OnDeactivated);
    }

    void Start()
    {
        // Material dan warna di-assign di Start agar nilai Inspector sudah siap
        if (laserMaterial != null)
            _lr.material = laserMaterial;
        else
            _lr.material = new Material(Shader.Find("Unlit/Color")) { color = laserColor };

        _lr.startColor = laserColor;
        _lr.endColor = laserColor;
        _lr.startWidth = laserWidth;
        _lr.endWidth = laserWidth;
    }

    void OnDestroy()
    {
        _grab.activated.RemoveListener(OnActivated);
        _grab.deactivated.RemoveListener(OnDeactivated);
    }

    void OnActivated(ActivateEventArgs _) => _firing = true;
    void OnDeactivated(DeactivateEventArgs _)
    {
        _firing = false;
        _lr.enabled = false;
        ReturnAll();
    }

    void Update() { if (_firing) CastLaser(); }

    void CastLaser()
    {
        Vector3 origin = laserOrigin ? laserOrigin.position : transform.position;
        Vector3 direction = laserOrigin ? laserOrigin.forward : transform.forward;

        var points = new List<Vector3> { origin };
        ReturnAll();

        float remaining = maxLaserLength;
        float n_current = 1f;
        RefractiveBlock currentBlock = null;
        int interfaceCount = 0;

        for (int b = 0; b < maxBounces && remaining > 0f; b++)
        {
            if (!Physics.Raycast(origin, direction, out RaycastHit hit, remaining, laserLayerMask))
            {
                points.Add(origin + direction * remaining);
                break;
            }

            points.Add(hit.point);
            remaining -= hit.distance;

            RefractiveBlock block = hit.collider.GetComponent<RefractiveBlock>();
            if (block == null) break;

            bool entering = Vector3.Dot(direction, hit.normal) < 0f;

            float n1, n2;
            Vector3 surfaceNormal;

            if (entering)
            {
                n1 = n_current;
                n2 = block.refractiveIndex;
                surfaceNormal = hit.normal;
                currentBlock = block;
            }
            else
            {
                n1 = currentBlock != null ? currentBlock.refractiveIndex : n_current;
                n2 = 1f;
                surfaceNormal = -hit.normal;
                currentBlock = null;
            }

            float cosI = Mathf.Clamp(Vector3.Dot(-direction, surfaceNormal), -1f, 1f);
            float thetaI = Mathf.Acos(cosI);
            float sinT = (n1 / n2) * Mathf.Sin(thetaI);

            Vector3 newDir;
            float thetaR;

            if (sinT >= 1f)
            {
                newDir = Vector3.Reflect(direction, surfaceNormal);
                thetaR = thetaI * Mathf.Rad2Deg;
            }
            else
            {
                thetaR = Mathf.Asin(sinT) * Mathf.Rad2Deg;
                newDir = RefractVector(direction, surfaceNormal, n1, n2);
            }

            // Interface ke-N → θ(2N-1) datang, θ(2N) bias
            interfaceCount++;
            int idxInc = 2 * interfaceCount - 1;
            int idxRefr = 2 * interfaceCount;

            SpawnIndicator(hit.point, surfaceNormal, direction, newDir,
                           thetaI * Mathf.Rad2Deg, thetaR,
                           block.mediumName, idxInc, idxRefr);

            origin = hit.point + newDir * 0.002f;
            direction = newDir;
            n_current = n2;
        }

        _lr.enabled = true;
        _lr.positionCount = points.Count;
        _lr.SetPositions(points.ToArray());
    }

    static Vector3 RefractVector(Vector3 I, Vector3 N, float n1, float n2)
    {
        float r = n1 / n2;
        float cosI = -Vector3.Dot(N, I);
        float sin2T = r * r * (1f - cosI * cosI);
        if (sin2T > 1f) return Vector3.Reflect(I, N);
        return r * I + (r * cosI - Mathf.Sqrt(1f - sin2T)) * N;
    }

    void SpawnIndicator(Vector3 pos, Vector3 normal,
                        Vector3 inc, Vector3 refr,
                        float t1, float t2, string medName,
                        int idxInc, int idxRefr)
    {
        AngleIndicator ind;
        if (_pool.Count > 0) { ind = _pool[0]; _pool.RemoveAt(0); }
        else
        {
            if (!angleIndicatorPrefab) return;
            ind = Instantiate(angleIndicatorPrefab).GetComponent<AngleIndicator>();
        }
        ind.gameObject.SetActive(true);
        ind.Display(pos, normal, inc, refr, t1, t2, medName, idxInc, idxRefr);
        _active.Add(ind);
    }

    void ReturnAll()
    {
        foreach (var i in _active) { i.gameObject.SetActive(false); _pool.Add(i); }
        _active.Clear();
    }
}