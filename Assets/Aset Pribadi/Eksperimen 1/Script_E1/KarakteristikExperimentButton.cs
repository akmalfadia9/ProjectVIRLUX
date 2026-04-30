using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// KarakteristikExperimentButton
/// Script tombol XR untuk eksperimen Karakteristik.
/// Pasang ke masing-masing 3 tombol (Lampu, Alarm, Vacuum Pump).
/// Bekerja dengan VR controller (XR Interaction Toolkit) dan mouse click biasa.
/// </summary>
[RequireComponent(typeof(XRSimpleInteractable))]
public class KarakteristikExperimentButton : MonoBehaviour
{
    public enum KarakteristikButtonType
    {
        Tombol1_LampuPijar,
        Tombol2_Alarm,
        Tombol3_VacuumPump
    }

    [Header("=== Konfigurasi Tombol Karakteristik ===")]
    public KarakteristikButtonType buttonType;

    [Header("=== Visual Feedback ===")]
    [Tooltip("Renderer untuk warna tombol (ON/OFF indicator)")]
    public Renderer buttonRenderer;

    [Tooltip("Material saat tombol aktif (ON)")]
    public Material materialActive;

    [Tooltip("Material saat tombol tidak aktif (OFF)")]
    public Material materialInactive;

    [Header("=== Label UI ===")]
    [Tooltip("TextMeshPro untuk label status tombol (opsional)")]
    public TMPro.TextMeshPro buttonLabel;

    private XRSimpleInteractable xrInteractable;
    private bool isActive = false;

    // ─── Label teks per tombol ────────────────────────────────────────────────
    private static readonly string[] labelON  = { "LAMPU: ON",  "ALARM: ON",  "VACUUM: ON"  };
    private static readonly string[] labelOFF = { "LAMPU: OFF", "ALARM: OFF", "VACUUM: OFF" };

    private void Awake()
    {
        xrInteractable = GetComponent<XRSimpleInteractable>();
    }

    private void OnEnable()
    {
        xrInteractable.selectEntered.AddListener(OnButtonPressed);
    }

    private void OnDisable()
    {
        xrInteractable.selectEntered.RemoveListener(OnButtonPressed);
    }

    private void Start()
    {
        // Subscribe ke state changes dari manager
        if (KarakteristikExperimentManager.Instance != null)
        {
            KarakteristikExperimentManager.Instance.OnLampToggled    += OnLampStateChanged;
            KarakteristikExperimentManager.Instance.OnAlarmToggled   += OnAlarmStateChanged;
            KarakteristikExperimentManager.Instance.OnVacuumToggled  += OnVacuumStateChanged;
        }
        RefreshVisual();
    }

    private void OnDestroy()
    {
        if (KarakteristikExperimentManager.Instance != null)
        {
            KarakteristikExperimentManager.Instance.OnLampToggled    -= OnLampStateChanged;
            KarakteristikExperimentManager.Instance.OnAlarmToggled   -= OnAlarmStateChanged;
            KarakteristikExperimentManager.Instance.OnVacuumToggled  -= OnVacuumStateChanged;
        }
    }

    // ─── XR Interaction ──────────────────────────────────────────────────────
    private void OnButtonPressed(SelectEnterEventArgs args)
    {
        ExecuteButtonAction();
    }

    // Fallback: juga bisa diklik dengan mouse di non-VR mode (untuk testing)
    private void OnMouseDown()
    {
        ExecuteButtonAction();
    }

    private void ExecuteButtonAction()
    {
        if (KarakteristikExperimentManager.Instance == null) return;

        switch (buttonType)
        {
            case KarakteristikButtonType.Tombol1_LampuPijar:
                KarakteristikExperimentManager.Instance.ToggleLamp();
                break;
            case KarakteristikButtonType.Tombol2_Alarm:
                KarakteristikExperimentManager.Instance.ToggleAlarm();
                break;
            case KarakteristikButtonType.Tombol3_VacuumPump:
                KarakteristikExperimentManager.Instance.ToggleVacuumPump();
                break;
        }
    }

    // ─── State Callbacks ──────────────────────────────────────────────────────
    private void OnLampStateChanged(bool state)
    {
        if (buttonType == KarakteristikButtonType.Tombol1_LampuPijar)
        { isActive = state; RefreshVisual(); }
    }

    private void OnAlarmStateChanged(bool state)
    {
        if (buttonType == KarakteristikButtonType.Tombol2_Alarm)
        { isActive = state; RefreshVisual(); }
    }

    private void OnVacuumStateChanged(bool state)
    {
        if (buttonType == KarakteristikButtonType.Tombol3_VacuumPump)
        { isActive = state; RefreshVisual(); }
    }

    // ─── Visual Update ────────────────────────────────────────────────────────
    private void RefreshVisual()
    {
        int idx = (int)buttonType;

        if (buttonRenderer != null)
            buttonRenderer.material = isActive ? materialActive : materialInactive;

        if (buttonLabel != null)
            buttonLabel.text = isActive ? labelON[idx] : labelOFF[idx];
    }
}
