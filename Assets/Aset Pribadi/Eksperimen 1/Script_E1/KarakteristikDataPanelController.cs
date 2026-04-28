using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// KarakteristikDataPanelController
/// Panel UI yang menampilkan data angka real-time (intensitas cahaya & bunyi).
/// Panel ini TERSEMBUNYI di awal, muncul saat player menekan tombol
/// "Klik Disini untuk Melihat Data Eksperimen".
/// Sync dengan data dari KarakteristikSensorController.
/// </summary>
public class KarakteristikDataPanelController : MonoBehaviour
{
    // ─── Panel References ─────────────────────────────────────────────────────
    [Header("=== Panel Data Karakteristik ===")]
    [Tooltip("Root GameObject panel data (yang akan ditampilkan/disembunyikan)")]
    public GameObject dataPanelRoot;

    [Tooltip("Tombol reveal panel - XR Interactable dengan label 'Klik Disini untuk Melihat Data Eksperimen'")]
    public XRSimpleInteractable revealButton;

    [Tooltip("TextMeshPro label tombol reveal")]
    public TextMeshPro revealButtonLabel;

    [Header("=== Text Fields Panel Data ===")]
    [Tooltip("TMP: judul panel")]
    public TextMeshPro txtJudulPanel;

    [Tooltip("TMP: nilai intensitas cahaya (lux)")]
    public TextMeshPro txtIntensitasCahaya;

    [Tooltip("TMP: nilai intensitas bunyi (dB)")]
    public TextMeshPro txtIntensitasBunyi;

    [Tooltip("TMP: level vacuum (%)")]
    public TextMeshPro txtVacuumLevel;

    [Tooltip("TMP: status lampu")]
    public TextMeshPro txtStatusLampu;

    [Tooltip("TMP: status alarm")]
    public TextMeshPro txtStatusAlarm;

    [Tooltip("TMP: status vacuum pump")]
    public TextMeshPro txtStatusVacuum;

    [Tooltip("TMP: keterangan fisika (penjelasan singkat)")]
    public TextMeshPro txtKeteranganFisika;

    [Header("=== Referensi Sensor ===")]
    [Tooltip("Sensor yang datanya ditampilkan di panel ini")]
    public KarakteristikSensorController targetSensor;

    [Header("=== Animasi Panel ===")]
    public float panelFadeSpeed = 3f;
    private CanvasGroup panelCanvasGroup;
    private bool isPanelVisible = false;

    // ─────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        // Setup initial state - panel tersembunyi
        if (dataPanelRoot != null)
        {
            dataPanelRoot.SetActive(false);
            panelCanvasGroup = dataPanelRoot.GetComponentInChildren<CanvasGroup>();
        }

        // Setup reveal button label
        if (revealButtonLabel != null)
            revealButtonLabel.text = "KLIK DISINI\nUNTUK MELIHAT\nDATA EKSPERIMEN";

        // Setup judul panel
        if (txtJudulPanel != null)
            txtJudulPanel.text = "DATA EKSPERIMEN\nKARAKTERISTIK GELOMBANG";

        // XR Button event
        if (revealButton != null)
        {
            revealButton.selectEntered.AddListener(OnRevealButtonPressed);
        }

        // Subscribe ke experiment events
        if (KarakteristikExperimentManager.Instance != null)
        {
            KarakteristikExperimentManager.Instance.OnLampToggled   += OnLampChanged;
            KarakteristikExperimentManager.Instance.OnAlarmToggled  += OnAlarmChanged;
            KarakteristikExperimentManager.Instance.OnVacuumToggled += OnVacuumChanged;
        }

        RefreshKeteranganFisika(false, false, false);
    }

    private void OnDestroy()
    {
        if (revealButton != null)
            revealButton.selectEntered.RemoveListener(OnRevealButtonPressed);

        if (KarakteristikExperimentManager.Instance != null)
        {
            KarakteristikExperimentManager.Instance.OnLampToggled   -= OnLampChanged;
            KarakteristikExperimentManager.Instance.OnAlarmToggled  -= OnAlarmChanged;
            KarakteristikExperimentManager.Instance.OnVacuumToggled -= OnVacuumChanged;
        }
    }

    private void Update()
    {
        if (isPanelVisible && dataPanelRoot != null && dataPanelRoot.activeSelf)
            RefreshPanelData();
    }

    // ─── Toggle Panel ─────────────────────────────────────────────────────────
    private void OnRevealButtonPressed(SelectEnterEventArgs args) => TogglePanel();

    // Fallback mouse click
    public void OnRevealButtonClick() => TogglePanel();

    private void TogglePanel()
    {
        isPanelVisible = !isPanelVisible;

        if (dataPanelRoot != null)
        {
            dataPanelRoot.SetActive(isPanelVisible);
            if (revealButtonLabel != null)
                revealButtonLabel.text = isPanelVisible
                    ? "SEMBUNYIKAN DATA"
                    : "KLIK DISINI\nUNTUK MELIHAT\nDATA EKSPERIMEN";
        }
        Debug.Log($"[Karakteristik] Data Panel: {(isPanelVisible ? "TAMPIL" : "TERSEMBUNYI")}");
    }

    // ─── Data Refresh ─────────────────────────────────────────────────────────
    private void RefreshPanelData()
    {
        var mgr = KarakteristikExperimentManager.Instance;
        if (mgr == null) return;

        // Prioritas: data dari sensor; fallback dari manager langsung
        float cahaya = targetSensor != null
            ? targetSensor.BacaanCahayaLux
            : mgr.LightIntensity;

        float bunyi = targetSensor != null
            ? targetSensor.BacaanBunyiDb
            : mgr.SoundIntensity;

        float vacuumPct = mgr.VacuumLevel * 100f;

        if (txtIntensitasCahaya != null)
            txtIntensitasCahaya.text = $"Intensitas Cahaya\n<size=28><b>{cahaya:F1}</b></size> lux";

        if (txtIntensitasBunyi != null)
            txtIntensitasBunyi.text  = $"Intensitas Bunyi\n<size=28><b>{bunyi:F1}</b></size> dB";

        if (txtVacuumLevel != null)
            txtVacuumLevel.text = $"Level Vacuum\n<size=28><b>{vacuumPct:F0}</b></size>%";
    }

    // ─── Status Updates ───────────────────────────────────────────────────────
    private void OnLampChanged(bool state)
    {
        if (txtStatusLampu != null)
            txtStatusLampu.text = $"Lampu Pijar: <b>{(state ? "<color=#FFD700>ON</color>" : "OFF")}</b>";
        var mgr = KarakteristikExperimentManager.Instance;
        if (mgr != null) RefreshKeteranganFisika(state, mgr.IsAlarmOn, mgr.IsVacuumOn);
    }

    private void OnAlarmChanged(bool state)
    {
        if (txtStatusAlarm != null)
            txtStatusAlarm.text = $"Alarm: <b>{(state ? "<color=#FF4444>ON</color>" : "OFF")}</b>";
        var mgr = KarakteristikExperimentManager.Instance;
        if (mgr != null) RefreshKeteranganFisika(mgr.IsLampOn, state, mgr.IsVacuumOn);
    }

    private void OnVacuumChanged(bool state)
    {
        if (txtStatusVacuum != null)
            txtStatusVacuum.text = $"Vacuum Pump: <b>{(state ? "<color=#00BFFF>ON</color>" : "OFF")}</b>";
        var mgr = KarakteristikExperimentManager.Instance;
        if (mgr != null) RefreshKeteranganFisika(mgr.IsLampOn, mgr.IsAlarmOn, state);
    }

    // ─── Keterangan Fisika Dinamis ────────────────────────────────────────────
    private void RefreshKeteranganFisika(bool lamp, bool alarm, bool vacuum)
    {
        if (txtKeteranganFisika == null) return;

        if (!lamp && !alarm)
        {
            txtKeteranganFisika.text = "Aktifkan lampu atau alarm\nuntuk memulai pengamatan\nkarakteristik gelombang.";
        }
        else if (lamp && !alarm && !vacuum)
        {
            txtKeteranganFisika.text = "Cahaya merambat tanpa medium.\nIntensitas tetap konstan\nmeskipun vacuum dinyalakan.";
        }
        else if (!lamp && alarm && !vacuum)
        {
            txtKeteranganFisika.text = "Bunyi merambat melalui udara.\nAktifkan vacuum untuk\nmelihat perubahan intensitas.";
        }
        else if (!lamp && alarm && vacuum)
        {
            txtKeteranganFisika.text = "Vacuum mengurangi kerapatan udara.\nIntensitas bunyi menurun\nsesuai hukum gelombang mekanik.";
        }
        else if (lamp && alarm && vacuum)
        {
            txtKeteranganFisika.text = "Perbedaan terlihat!\nCahaya: tetap (gel. EM)\nBunyi: menurun (gel. mekanik)";
        }
        else
        {
            txtKeteranganFisika.text = "Amati perbedaan karakteristik\ngelombang cahaya dan bunyi\ndalam medium yang berbeda.";
        }
    }
}
