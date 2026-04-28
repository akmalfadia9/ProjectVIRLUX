using UnityEngine;

/// <summary>
/// KarakteristikAlarmController
/// Mengontrol alarm di dalam bell jar.
/// Volume suara MENURUN seiring bertambahnya level vacuum.
/// Simulasi fisika: gelombang bunyi membutuhkan medium (udara) untuk merambat.
/// Semakin sedikit udara (vakum) → semakin kecil intensitas bunyi.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class KarakteristikAlarmController : MonoBehaviour
{
    [Header("=== Audio Alarm ===")]
    [Tooltip("AudioSource untuk suara alarm (akan di-assign otomatis)")]
    private AudioSource audioSource;

    [Tooltip("Audio clip suara alarm (assign di Inspector)")]
    public AudioClip alarmClip;

    [Tooltip("Volume maksimum saat tidak ada vacuum")]
    [Range(0f, 1f)]
    public float maxVolume = 0.8f;

    [Tooltip("Kecepatan transisi volume")]
    public float volumeTransitionSpeed = 2f;

    [Header("=== Visual Alarm ===")]
    [Tooltip("Renderer untuk lampu LED alarm (merah berkedip saat ON)")]
    public Renderer alarmLedRenderer;

    [Tooltip("Material LED aktif")]
    public Material ledActiveMaterial;

    [Tooltip("Material LED tidak aktif")]
    public Material ledInactiveMaterial;

    [Tooltip("Kecepatan kedip LED (Hz)")]
    public float ledBlinkRate = 2f;

    // ─── State ────────────────────────────────────────────────────────────────
    private bool  isOn          = false;
    private float targetVolume  = 0f;
    private float currentVolume = 0f;
    private float vacuumLevel   = 0f;
    private float blinkTimer    = 0f;
    private bool  ledState      = false;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop       = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;  // 3D sound
        audioSource.volume     = 0f;

        if (alarmClip != null)
            audioSource.clip = alarmClip;
    }

    private void Start()
    {
        if (KarakteristikExperimentManager.Instance != null)
        {
            KarakteristikExperimentManager.Instance.OnAlarmToggled      += SetAlarmState;
            KarakteristikExperimentManager.Instance.OnVacuumLevelChanged += OnVacuumChanged;
        }
    }

    private void OnDestroy()
    {
        if (KarakteristikExperimentManager.Instance != null)
        {
            KarakteristikExperimentManager.Instance.OnAlarmToggled      -= SetAlarmState;
            KarakteristikExperimentManager.Instance.OnVacuumLevelChanged -= OnVacuumChanged;
        }
    }

    private void Update()
    {
        UpdateAudio();
        UpdateLedBlink();
    }

    // ─── Audio Update ─────────────────────────────────────────────────────────
    private void UpdateAudio()
    {
        if (isOn)
        {
            // Volume turun seiring vacuum level naik
            // Model fisika: I ~ rho (kerapatan medium)
            // Approx: volume = maxVolume * (1 - vacuumLevel)^1.5
            float mediumFactor = Mathf.Pow(1f - vacuumLevel, 1.5f);
            targetVolume = maxVolume * mediumFactor;

            // Pastikan audio sedang play
            if (!audioSource.isPlaying && alarmClip != null)
                audioSource.Play();
        }
        else
        {
            targetVolume = 0f;
        }

        // Smooth transition volume
        currentVolume = Mathf.MoveTowards(currentVolume, targetVolume, volumeTransitionSpeed * Time.deltaTime);
        audioSource.volume = currentVolume;

        // Stop audio jika volume = 0 dan alarm mati
        if (!isOn && currentVolume < 0.001f && audioSource.isPlaying)
            audioSource.Stop();
    }

    // ─── LED Blink ────────────────────────────────────────────────────────────
    private void UpdateLedBlink()
    {
        if (alarmLedRenderer == null) return;

        if (isOn)
        {
            blinkTimer += Time.deltaTime;
            if (blinkTimer >= 1f / ledBlinkRate)
            {
                blinkTimer = 0f;
                ledState = !ledState;
                alarmLedRenderer.material = ledState ? ledActiveMaterial : ledInactiveMaterial;
            }
        }
        else
        {
            // LED mati
            blinkTimer = 0f;
            ledState   = false;
            if (ledInactiveMaterial != null)
                alarmLedRenderer.material = ledInactiveMaterial;
        }
    }

    // ─── Callbacks ────────────────────────────────────────────────────────────
    private void SetAlarmState(bool state)
    {
        isOn = state;
        if (!isOn)
        {
            // Reset LED
            ledState = false;
        }
        Debug.Log($"[Karakteristik] Alarm: {(isOn ? "ON" : "OFF")}");
    }

    private void OnVacuumChanged(float level)
    {
        vacuumLevel = level;
    }

    // ─── Public getter untuk AudioSource (diakses KarakteristikSensorController) ──
    public float GetCurrentVolumeNormalized() => audioSource.volume / Mathf.Max(maxVolume, 0.001f);
}
