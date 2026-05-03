using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

/// <summary>
/// RefractiveIndexSlider.cs
/// Pasang ke SliderRoot (empty GameObject di samping balok unknown).
///
/// Struktur Hierarchy:
///   SliderRoot  ← script ini
///   ├── TrackTop      (empty, penanda ujung atas rel)
///   ├── TrackBottom   (empty, penanda ujung bawah rel)
///   ├── SliderTrack   (LineRenderer, garis rel visual)
///   ├── SliderHandle  (Sphere + XRGrabInteractable)
///   ├── NLabel        (TextMeshPro, ikut handle)
///   └── MinMaxLabels  (empty)
///       ├── LabelMin  (TextMeshPro, ujung bawah)
///       └── LabelMax  (TextMeshPro, ujung atas)
/// </summary>
public class RefractiveIndexSlider : MonoBehaviour
{
    [Header("Target Balok")]
    public RefractiveBlock targetBlock;
    public Renderer        targetRenderer;

    [Header("Slider Settings")]
    public float nMin     = 1.0f;
    public float nMax     = 3.0f;
    public float nInitial = 1.5f;

    [Header("Warna Balok")]
    public Color colorAtNMin = new Color(0.55f, 0.88f, 1.0f, 0.15f);  // transparan biru muda
    public Color colorAtNMax = new Color(0.04f, 0.04f, 0.10f, 0.88f); // hitam pekat

    [Header("Referensi Objek")]
    public Transform    sliderHandle;
    public Transform    trackBottom;
    public Transform    trackTop;

    [Header("Label")]
    public TextMeshPro  nLabel;
    public TextMeshPro  labelMin;
    public TextMeshPro  labelMax;
    public float        labelFontSize  = 0.12f;
    public float        labelOffset    = 0.07f;

    [Header("Track Visual")]
    public LineRenderer trackLine;
    public Color        trackColor = new Color(0.85f, 0.85f, 0.85f, 1f);

    // ── private ───────────────────────────────────────────────────────────────
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _grab;
    private Material           _blockMat;
    private float              _currentN;
    private bool               _isGrabbed;
    private Camera             _cam;

    void Awake()
    {
        _currentN = nInitial;

        // Buat instance material balok sendiri
        if (targetRenderer != null)
        {
            _blockMat = new Material(targetRenderer.sharedMaterial);
            targetRenderer.material = _blockMat;
        }

        // Setup XRGrabInteractable di handle
        if (sliderHandle != null)
        {
            _grab = sliderHandle.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (_grab == null)
                _grab = sliderHandle.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

            _grab.movementType  = UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.Instantaneous;
            _grab.trackRotation = false;

            _grab.selectEntered.AddListener(OnGrabbed);
            _grab.selectExited.AddListener(OnReleased);
        }
    }

    void Start()
    {
        _cam = Camera.main;
        SetupTrackLine();
        SetupStaticLabels();
        ApplyN(_currentN);
        PlaceHandleAtN(_currentN);
    }

    void OnDestroy()
    {
        if (_grab != null)
        {
            _grab.selectEntered.RemoveListener(OnGrabbed);
            _grab.selectExited.RemoveListener(OnReleased);
        }
    }

    void OnGrabbed(SelectEnterEventArgs args) => _isGrabbed = true;
    void OnReleased(SelectExitEventArgs args)
    {
        _isGrabbed = false;
        PlaceHandleAtN(_currentN);
    }

    void Update()
    {
        if (_cam == null) _cam = Camera.main;

        if (_isGrabbed && sliderHandle != null && trackBottom != null && trackTop != null)
        {
            // Kunci X dan Z — handle hanya boleh gerak di sumbu Y
            float clampedY = Mathf.Clamp(
                sliderHandle.position.y,
                trackBottom.position.y,
                trackTop.position.y);

            sliderHandle.position = new Vector3(
                trackBottom.position.x,
                clampedY,
                trackBottom.position.z);

            // Hitung n dari posisi Y
            float t = Mathf.InverseLerp(
                trackBottom.position.y,
                trackTop.position.y,
                clampedY);

            _currentN = Mathf.Lerp(nMin, nMax, t);
            ApplyN(_currentN);
        }

        // PC testing: Q = naik, E = turun
        float keyAxis = 0f;
        if (Input.GetKey(KeyCode.Q)) keyAxis =  1f;
        if (Input.GetKey(KeyCode.E)) keyAxis = -1f;
        if (Mathf.Abs(keyAxis) > 0f)
        {
            _currentN = Mathf.Clamp(_currentN + keyAxis * 0.8f * Time.deltaTime, nMin, nMax);
            ApplyN(_currentN);
            PlaceHandleAtN(_currentN);
        }

        UpdateNLabel();
        BillboardLabels();
    }

    // ── Apply nilai n ─────────────────────────────────────────────────────────
    void ApplyN(float n)
    {
        if (targetBlock != null)
            targetBlock.refractiveIndex = n;

        if (_blockMat != null)
        {
            float t = Mathf.InverseLerp(nMin, nMax, n);
            _blockMat.color = Color.Lerp(colorAtNMin, colorAtNMax, t);
        }
    }

    // ── Posisikan handle sesuai nilai n ───────────────────────────────────────
    void PlaceHandleAtN(float n)
    {
        if (sliderHandle == null || trackBottom == null || trackTop == null) return;
        float t = Mathf.InverseLerp(nMin, nMax, n);
        float y = Mathf.Lerp(trackBottom.position.y, trackTop.position.y, t);
        sliderHandle.position = new Vector3(
            trackBottom.position.x, y, trackBottom.position.z);
    }

    // ── Label n bergerak ikut handle ──────────────────────────────────────────
    void UpdateNLabel()
    {
        if (nLabel == null || sliderHandle == null) return;
        Vector3 side = GetSideDir();
        nLabel.transform.position = sliderHandle.position + side * labelOffset;
        nLabel.fontSize           = labelFontSize;
        nLabel.text = "n = " + _currentN.ToString("F2") + "\n"
                    + "<size=80%>" + GetMaterialName(_currentN) + "</size>";
    }

    // ── Label statis min/max ──────────────────────────────────────────────────
    void SetupStaticLabels()
    {
        Vector3 side = GetSideDir();

        if (labelMin != null && trackBottom != null)
        {
            labelMin.transform.position = trackBottom.position + side * labelOffset;
            labelMin.fontSize           = labelFontSize * 0.8f;
            labelMin.color              = new Color(0.6f, 0.9f, 1f);
            labelMin.textWrappingMode   = TextWrappingModes.NoWrap;
            labelMin.text = "n = " + nMin.ToString("F1") + "  (Udara)";
        }

        if (labelMax != null && trackTop != null)
        {
            labelMax.transform.position = trackTop.position + side * labelOffset;
            labelMax.fontSize           = labelFontSize * 0.8f;
            labelMax.color              = new Color(0.8f, 0.8f, 1f);
            labelMax.textWrappingMode   = TextWrappingModes.NoWrap;
            labelMax.text = "n = " + nMax.ToString("F1") + "  (Eksotis)";
        }

        if (nLabel != null)
        {
            nLabel.color           = Color.white;
            nLabel.textWrappingMode = TextWrappingModes.NoWrap;
            nLabel.alignment       = TextAlignmentOptions.Left;
        }
    }

    // ── Track line (garis rel) ─────────────────────────────────────────────────
    void SetupTrackLine()
    {
        if (trackLine == null || trackBottom == null || trackTop == null) return;
        trackLine.useWorldSpace = true;
        trackLine.startColor    = trackColor;
        trackLine.endColor      = trackColor;
        trackLine.startWidth    = 0.008f;
        trackLine.endWidth      = 0.008f;
        trackLine.material      = new Material(Shader.Find("Unlit/Color")) { color = trackColor };
        trackLine.positionCount = 2;
        trackLine.SetPosition(0, trackBottom.position);
        trackLine.SetPosition(1, trackTop.position);
    }

    // ── Billboard semua label ─────────────────────────────────────────────────
    void BillboardLabels()
    {
        if (_cam == null) return;
        Quaternion rot = Quaternion.LookRotation(_cam.transform.forward);
        if (nLabel   != null) nLabel.transform.rotation   = rot;
        if (labelMin != null) labelMin.transform.rotation = rot;
        if (labelMax != null) labelMax.transform.rotation = rot;
    }

    Vector3 GetSideDir()
    {
        return _cam != null ? _cam.transform.right : Vector3.right;
    }

    // ── Nama material berdasarkan n ───────────────────────────────────────────
    string GetMaterialName(float n)
    {
        if (n < 1.05f) return "Udara";
        if (n < 1.20f) return "Gas / Uap";
        if (n < 1.36f) return "Es";
        if (n < 1.38f) return "Air";
        if (n < 1.46f) return "Kaca Borosilikat";
        if (n < 1.52f) return "Kaca Crown";
        if (n < 1.60f) return "Kaca Biasa";
        if (n < 1.70f) return "Kaca Flint";
        if (n < 1.80f) return "Kaca Berat";
        if (n < 2.00f) return "Zirkonia";
        if (n < 2.25f) return "Berlian Sintetis";
        if (n < 2.45f) return "Berlian";
        return "Material Eksotis";
    }
}
