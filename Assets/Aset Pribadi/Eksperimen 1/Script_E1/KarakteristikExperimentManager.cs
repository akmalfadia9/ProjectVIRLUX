using UnityEngine;
using System;

/// <summary>
/// KarakteristikExperimentManager
/// Pusat kontrol seluruh eksperimen Karakteristik Gelombang (Cahaya & Bunyi) dalam Bell Jar.
/// Mengelola state lampu pijar, alarm, vacuum pump, serta menghitung nilai fisika secara real-time.
/// Compatible: Unity 6.0004 + XR Interaction Toolkit
/// </summary>
public class KarakteristikExperimentManager : MonoBehaviour
{
    public static KarakteristikExperimentManager Instance { get; private set; }

    // ─── Events ───────────────────────────────────────────────────────────────
    public event Action<bool>  OnLampToggled;
    public event Action<bool>  OnAlarmToggled;
    public event Action<bool>  OnVacuumToggled;
    public event Action<float> OnLightIntensityChanged;
    public event Action<float> OnSoundIntensityChanged;
    public event Action<float> OnVacuumLevelChanged;    // 0 = udara penuh, 1 = vakum penuh

    // ─── State ────────────────────────────────────────────────────────────────
    [Header("=== State Eksperimen Karakteristik ===")]
    [SerializeField, ReadOnly] private bool  isLampOn       = false;
    [SerializeField, ReadOnly] private bool  isAlarmOn      = false;
    [SerializeField, ReadOnly] private bool  isVacuumOn     = false;
    [SerializeField, ReadOnly] private float vacuumLevel    = 0f;   // 0-1
    [SerializeField, ReadOnly] private float lightIntensity = 0f;   // lux
    [SerializeField, ReadOnly] private float soundIntensity = 0f;   // dB

    // ─── Fisika Karakteristik ─────────────────────────────────────────────────
    [Header("=== Parameter Fisika Karakteristik ===")]
    [Tooltip("Intensitas cahaya awal lampu pijar (lux)")]
    public float baseLightIntensityLux   = 800f;

    [Tooltip("Intensitas bunyi awal alarm (dB)")]
    public float baseAlarmIntensityDb    = 75f;

    [Tooltip("Kecepatan pompa vacuum (level/detik)")]
    public float vacuumPumpSpeed         = 0.05f;

    [Tooltip("Kecepatan vacuum kembali ke normal saat dimatikan (level/detik)")]
    public float vacuumReleaseSpeed      = 0.03f;

    /// <summary>
    /// Cahaya TIDAK terpengaruh vacuum (foton tidak butuh medium).
    /// Ini adalah konsep inti Karakteristik Gelombang Elektromagnetik.
    /// Bunyi MENURUN seiring vacuum (gelombang mekanik butuh medium).
    /// Sesuai Hukum Fisika: I_bunyi ~ rho * c (bergantung pada kerapatan medium).
    /// </summary>
    [Tooltip("Faktor penurunan bunyi saat vacuum penuh (0 = sunyi total)")]
    [Range(0f, 0.02f)]
    public float soundFloorAtFullVacuum  = 0f;

    // ─── Properties publik ───────────────────────────────────────────────────
    public bool  IsLampOn        => isLampOn;
    public bool  IsAlarmOn       => isAlarmOn;
    public bool  IsVacuumOn      => isVacuumOn;
    public float VacuumLevel     => vacuumLevel;
    public float LightIntensity  => lightIntensity;
    public float SoundIntensity  => soundIntensity;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        UpdateVacuumLevel();
        UpdatePhysicsValues();
    }

    // ─── Vacuum Level Simulation ──────────────────────────────────────────────
    private void UpdateVacuumLevel()
    {
        if (isVacuumOn)
        {
            vacuumLevel = Mathf.MoveTowards(vacuumLevel, 1f, vacuumPumpSpeed * Time.deltaTime);

            // Matikan vacuum pump otomatis saat level mencapai 100%
            if (vacuumLevel >= 1f)
            {
                isVacuumOn = false;
                OnVacuumToggled?.Invoke(false);
                Debug.Log("[Karakteristik] Vacuum otomatis mati - level 100%");
            }
        }
        else
        {
            vacuumLevel = Mathf.MoveTowards(vacuumLevel, 0f, vacuumReleaseSpeed * Time.deltaTime);
        }
        OnVacuumLevelChanged?.Invoke(vacuumLevel);
    }

    // ─── Physics Values ───────────────────────────────────────────────────────
    private void UpdatePhysicsValues()
    {
        // --- CAHAYA: Tidak terpengaruh vacuum ---
        // Gelombang elektromagnetik tidak memerlukan medium perambatan
        float newLight = isLampOn ? baseLightIntensityLux : 0f;

        // --- BUNYI: Menurun eksponensial seiring berkurangnya kerapatan udara ---
        // Model: I = I0 * (1 - vacuumLevel)^2  [mendekati perilaku nyata]
        float newSound = 0f;
        if (isAlarmOn)
        {
            float mediumFactor = Mathf.Pow(1f - vacuumLevel, 2f);
            newSound = Mathf.Lerp(soundFloorAtFullVacuum, baseAlarmIntensityDb, mediumFactor);
        }

        // Fire events hanya jika berubah
        if (!Mathf.Approximately(newLight, lightIntensity))
        {
            lightIntensity = newLight;
            OnLightIntensityChanged?.Invoke(lightIntensity);
        }

        if (!Mathf.Approximately(newSound, soundIntensity))
        {
            soundIntensity = newSound;
            OnSoundIntensityChanged?.Invoke(soundIntensity);
        }
    }

    // ─── Public Toggles (dipanggil dari Button Scripts) ───────────────────────
    public void ToggleLamp()
    {
        isLampOn = !isLampOn;
        OnLampToggled?.Invoke(isLampOn);
        Debug.Log($"[Karakteristik] Lampu Pijar: {(isLampOn ? "ON" : "OFF")}");
    }

    public void ToggleAlarm()
    {
        isAlarmOn = !isAlarmOn;
        OnAlarmToggled?.Invoke(isAlarmOn);
        Debug.Log($"[Karakteristik] Alarm: {(isAlarmOn ? "ON" : "OFF")}");
    }

    public void ToggleVacuumPump()
    {
        isVacuumOn = !isVacuumOn;
        OnVacuumToggled?.Invoke(isVacuumOn);
        Debug.Log($"[Karakteristik] Vacuum Pump: {(isVacuumOn ? "ON" : "OFF")}");
    }
}
