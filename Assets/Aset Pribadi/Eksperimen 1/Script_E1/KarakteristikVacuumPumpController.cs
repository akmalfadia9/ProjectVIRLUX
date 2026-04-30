using UnityEngine;

/// <summary>
/// KarakteristikVacuumPumpController
/// Mengontrol visual dan audio vacuum pump.
/// Menampilkan jarum manometer yang bergerak, animasi motor, dan suara pompa.
/// </summary>
public class KarakteristikVacuumPumpController : MonoBehaviour
{
    [Header("=== Manometer (Pressure Gauge) ===")]
    [Tooltip("Transform jarum manometer - akan dirotasi")]
    public Transform jarumManometer;

    [Tooltip("Sudut rotasi jarum saat vacuum = 0 (tidak ada tekanan)")]
    public float jarumAngleAtZero = -30f;

    [Tooltip("Sudut rotasi jarum saat vacuum = 1 (tekanan penuh/vakum)")]
    public float jarumAngleAtFull = 210f;

    [Tooltip("Axis rotasi jarum (biasanya Z untuk jarum 2D)")]
    public Vector3 rotationAxis = Vector3.forward;

    [Header("=== Animasi Motor ===")]
    [Tooltip("Transform komponen berputar pada pompa (rotor/fan)")]
    public Transform rotatingPart;

    [Tooltip("Kecepatan rotasi motor saat aktif (degree/detik)")]
    public float motorRotationSpeed = 360f;

    [Header("=== Audio Pompa ===")]
    [Tooltip("AudioSource untuk suara mesin pompa")]
    public AudioSource pumpAudioSource;

    [Tooltip("Audio clip suara pompa berjalan")]
    public AudioClip pumpRunningClip;

    [Tooltip("Audio clip saat pompa start (opsional)")]
    public AudioClip pumpStartClip;

    [Tooltip("Volume suara pompa")]
    [Range(0f, 1f)]
    public float pumpVolume = 0.5f;

    [Header("=== Visual Indikator ===")]
    [Tooltip("Renderer lampu indikator ON/OFF pada panel pompa")]
    public Renderer indicatorLedRenderer;

    [Tooltip("Material LED aktif (hijau)")]
    public Material ledOnMaterial;

    [Tooltip("Material LED tidak aktif (abu)")]
    public Material ledOffMaterial;

    [Header("=== Selang Vacuum ===")]
    [Tooltip("Renderer selang/kabel vacuum untuk efek visual (opsional)")]
    public Renderer hoseRenderer;

    [Tooltip("Material selang saat vacuum aktif (sedikit bergetar/bercahaya)")]
    public Material hoseActiveMaterial;

    [Tooltip("Material selang normal")]
    public Material hoseNormalMaterial;

    // ─── State ────────────────────────────────────────────────────────────────
    private bool  isVacuumOn  = false;
    private float vacuumLevel = 0f;
    private float currentJarumAngle;

    // ─────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        currentJarumAngle = jarumAngleAtZero;

        if (pumpAudioSource != null)
        {
            pumpAudioSource.loop        = true;
            pumpAudioSource.playOnAwake = false;
            pumpAudioSource.volume      = 0f;
            if (pumpRunningClip != null)
                pumpAudioSource.clip = pumpRunningClip;
        }

        if (KarakteristikExperimentManager.Instance != null)
        {
            KarakteristikExperimentManager.Instance.OnVacuumToggled     += OnVacuumToggled;
            KarakteristikExperimentManager.Instance.OnVacuumLevelChanged += OnVacuumLevelChanged;
        }

        RefreshIndicatorLed();
    }

    private void OnDestroy()
    {
        if (KarakteristikExperimentManager.Instance != null)
        {
            KarakteristikExperimentManager.Instance.OnVacuumToggled     -= OnVacuumToggled;
            KarakteristikExperimentManager.Instance.OnVacuumLevelChanged -= OnVacuumLevelChanged;
        }
    }

    private void Update()
    {
        UpdateManometer();
        UpdateMotorRotation();
        UpdateAudio();
    }

    // ─── Manometer ────────────────────────────────────────────────────────────
    private void UpdateManometer()
    {
        if (jarumManometer == null) return;

        float targetAngle = Mathf.Lerp(jarumAngleAtZero, jarumAngleAtFull, vacuumLevel);
        currentJarumAngle = Mathf.MoveTowards(currentJarumAngle, targetAngle, 60f * Time.deltaTime);

        jarumManometer.localRotation = Quaternion.AngleAxis(currentJarumAngle, rotationAxis);
    }

    // ─── Motor ────────────────────────────────────────────────────────────────
    private void UpdateMotorRotation()
    {
        if (rotatingPart == null || !isVacuumOn) return;
        rotatingPart.Rotate(rotationAxis, motorRotationSpeed * Time.deltaTime);
    }

    // ─── Audio ────────────────────────────────────────────────────────────────
    private void UpdateAudio()
    {
        if (pumpAudioSource == null) return;

        float targetVol = isVacuumOn ? pumpVolume : 0f;
        pumpAudioSource.volume = Mathf.MoveTowards(pumpAudioSource.volume, targetVol, 2f * Time.deltaTime);

        if (isVacuumOn && !pumpAudioSource.isPlaying)
            pumpAudioSource.Play();

        if (!isVacuumOn && pumpAudioSource.volume < 0.01f && pumpAudioSource.isPlaying)
            pumpAudioSource.Stop();
    }

    // ─── Callbacks ────────────────────────────────────────────────────────────
    private void OnVacuumToggled(bool state)
    {
        isVacuumOn = state;
        RefreshIndicatorLed();
        RefreshHose();

        // Play start sound
        if (state && pumpStartClip != null && pumpAudioSource != null)
            pumpAudioSource.PlayOneShot(pumpStartClip, pumpVolume);

        Debug.Log($"[Karakteristik] Vacuum Pump visual: {(isVacuumOn ? "ON" : "OFF")}");
    }

    private void OnVacuumLevelChanged(float level)
    {
        vacuumLevel = level;
    }

    private void RefreshIndicatorLed()
    {
        if (indicatorLedRenderer == null) return;
        indicatorLedRenderer.material = isVacuumOn ? ledOnMaterial : ledOffMaterial;
    }

    private void RefreshHose()
    {
        if (hoseRenderer == null) return;
        hoseRenderer.material = isVacuumOn ? hoseActiveMaterial : hoseNormalMaterial;
    }
}
