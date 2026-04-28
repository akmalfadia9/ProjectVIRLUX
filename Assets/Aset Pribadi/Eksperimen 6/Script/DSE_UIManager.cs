using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DoubleSlit
{
    /// <summary>
    /// Mengontrol semua UI elemen dan meneruskan perubahan ke sistem laser/screen.
    /// Attach ke: DSE_UICanvas (atau child GameObject kosong).
    /// </summary>
    public class DSE_UIManager : MonoBehaviour
    {
        // ── Referensi Laser & Screen ──────────────────────────────────
        [Header("Referensi Sistem Utama")]
        public DSE_LaserController DSE_Laser1;
        public DSE_LaserController DSE_Laser2;
        public DSE_ScreenRenderer DSE_Screen;

        // ── UI Elements ───────────────────────────────────────────────
        [Header("Tombol Aktivasi")]
        public Button DSE_ActivateButton;
        public TMP_Text DSE_ActivateButtonText;

        [Header("Dropdown Pilihan Warna")]
        public TMP_Dropdown DSE_Dropdown_Laser1;
        public TMP_Dropdown DSE_Dropdown_Laser2;

        [Header("Info Panel — Parameter Tetap")]
        public TMP_Text DSE_Text_DistLaserSlit;   // Jarak laser → celah
        public TMP_Text DSE_Text_DistSlitScreen;  // Jarak celah → layar
        public TMP_Text DSE_Text_SlitWidth;       // Lebar celah a
        public TMP_Text DSE_Text_SlitDistance;    // Jarak antar celah d

        [Header("Info Panel — Parameter Dinamis")]
        public TMP_Text DSE_Text_Wavelength;
        public TMP_Text DSE_Text_CentralWidth;
        public TMP_Text DSE_Text_Y1;
        public TMP_Text DSE_Text_Y2;
        public TMP_Text DSE_Text_Y3;
        public TMP_Text DSE_Text_DeltaY;
        public TMP_Text DSE_Text_InterferenceStatus;

        // ── State ──────────────────────────────────────────────────────
        private bool DSE_LaserIsOn = false;
        private DSE_LaserColor DSE_CurrentColor1 = DSE_LaserColor.Green;
        private DSE_LaserColor DSE_CurrentColor2 = DSE_LaserColor.Green;

        void Start()
        {
            DSE_InitDropdowns();
            DSE_SetupButtonListener();
            DSE_PopulateFixedParameters();
            DSE_UpdateAllDynamicInfo();
        }

        // ─────────────────────────────────────────────────────────────
        // INISIALISASI
        // ─────────────────────────────────────────────────────────────

        private void DSE_InitDropdowns()
        {
            string[] colorNames = { "Merah (700 nm)", "Kuning (580 nm)", "Hijau (532 nm)", "Biru (450 nm)", "Ungu (405 nm)" };

            if (DSE_Dropdown_Laser1 != null)
            {
                DSE_Dropdown_Laser1.ClearOptions();
                DSE_Dropdown_Laser1.AddOptions(new System.Collections.Generic.List<string>(colorNames));
                DSE_Dropdown_Laser1.value = 2; // Default: Hijau
                DSE_Dropdown_Laser1.onValueChanged.AddListener(DSE_OnLaser1ColorChanged);
            }

            if (DSE_Dropdown_Laser2 != null)
            {
                DSE_Dropdown_Laser2.ClearOptions();
                DSE_Dropdown_Laser2.AddOptions(new System.Collections.Generic.List<string>(colorNames));
                DSE_Dropdown_Laser2.value = 2; // Default: Hijau
                DSE_Dropdown_Laser2.onValueChanged.AddListener(DSE_OnLaser2ColorChanged);
            }
        }

        private void DSE_SetupButtonListener()
        {
            if (DSE_ActivateButton != null)
                DSE_ActivateButton.onClick.AddListener(DSE_OnActivateButtonClicked);

            DSE_UpdateActivateButtonVisual();
        }

        private void DSE_PopulateFixedParameters()
        {
            if (DSE_Text_DistLaserSlit != null) DSE_Text_DistLaserSlit.text = $"Jarak Laser → Celah : {DSE_InterferenceCalculator.D_laser:F1} m";
            if (DSE_Text_DistSlitScreen != null) DSE_Text_DistSlitScreen.text = $"Jarak Celah → Layar : {DSE_InterferenceCalculator.L:F1} m";
            if (DSE_Text_SlitWidth != null) DSE_Text_SlitWidth.text = $"Lebar celah (a)      : {DSE_InterferenceCalculator.a * 1e6f:F0} μm";
            if (DSE_Text_SlitDistance != null) DSE_Text_SlitDistance.text = $"Jarak antar celah (d): {DSE_InterferenceCalculator.d * 1e6f:F0} μm";
        }

        // ─────────────────────────────────────────────────────────────
        // EVENT HANDLERS
        // ─────────────────────────────────────────────────────────────

        private void DSE_OnActivateButtonClicked()
        {
            DSE_LaserIsOn = !DSE_LaserIsOn;

            DSE_Laser1?.DSE_SetActive(DSE_LaserIsOn);
            DSE_Laser2?.DSE_SetActive(DSE_LaserIsOn);

            DSE_Screen?.DSE_UpdatePattern(DSE_CurrentColor1, DSE_CurrentColor2, DSE_LaserIsOn);
            DSE_UpdateActivateButtonVisual();
            DSE_UpdateAllDynamicInfo();
        }

        private void DSE_OnLaser1ColorChanged(int index)
        {
            DSE_CurrentColor1 = (DSE_LaserColor)index;
            DSE_Laser1?.DSE_SetColor(DSE_CurrentColor1);
            DSE_Screen?.DSE_UpdatePattern(DSE_CurrentColor1, DSE_CurrentColor2, DSE_LaserIsOn);
            DSE_UpdateAllDynamicInfo();
        }

        private void DSE_OnLaser2ColorChanged(int index)
        {
            DSE_CurrentColor2 = (DSE_LaserColor)index;
            DSE_Laser2?.DSE_SetColor(DSE_CurrentColor2);
            DSE_Screen?.DSE_UpdatePattern(DSE_CurrentColor1, DSE_CurrentColor2, DSE_LaserIsOn);
            DSE_UpdateAllDynamicInfo();
        }

        // ─────────────────────────────────────────────────────────────
        // UPDATE INFO PANEL
        // ─────────────────────────────────────────────────────────────

        private void DSE_UpdateAllDynamicInfo()
        {
            bool isCoherent = (DSE_CurrentColor1 == DSE_CurrentColor2);
            DSE_LaserColor activeColor = DSE_CurrentColor1; // gunakan color1 saat koheren

            float lambda = DSE_InterferenceCalculator.GetWavelength(activeColor);
            float lambdaNm = DSE_ColorHelper.GetWavelengthNm(activeColor);

            // Panjang gelombang — tampilkan keduanya jika berbeda
            if (DSE_Text_Wavelength != null)
            {
                if (isCoherent)
                    DSE_Text_Wavelength.text = $"Panjang gelombang (λ) : {lambdaNm:F0} nm";
                else
                {
                    float lam2Nm = DSE_ColorHelper.GetWavelengthNm(DSE_CurrentColor2);
                    DSE_Text_Wavelength.text = $"λ₁ = {lambdaNm:F0} nm | λ₂ = {lam2Nm:F0} nm";
                }
            }

            if (isCoherent)
            {
                float w = DSE_InterferenceCalculator.CentralMaximaWidth(lambda) * 1000f;  // → mm
                float y1 = DSE_InterferenceCalculator.BrightFringePosition(1, lambda) * 1000f;
                float y2 = DSE_InterferenceCalculator.BrightFringePosition(2, lambda) * 1000f;
                float y3 = DSE_InterferenceCalculator.BrightFringePosition(3, lambda) * 1000f;
                float dy = DSE_InterferenceCalculator.FringeSpacing(lambda) * 1000f;

                if (DSE_Text_CentralWidth != null) DSE_Text_CentralWidth.text = $"Lebar terang pusat (w): {w:F2} mm";
                if (DSE_Text_Y1 != null) DSE_Text_Y1.text = $"Terang ke-1 (y₁)     : ±{y1:F2} mm";
                if (DSE_Text_Y2 != null) DSE_Text_Y2.text = $"Terang ke-2 (y₂)     : ±{y2:F2} mm";
                if (DSE_Text_Y3 != null) DSE_Text_Y3.text = $"Terang ke-3 (y₃)     : ±{y3:F2} mm";
                if (DSE_Text_DeltaY != null) DSE_Text_DeltaY.text = $"Δy (jarak antar terang): {dy:F2} mm";

                if (DSE_Text_InterferenceStatus != null)
                {
                    DSE_Text_InterferenceStatus.text = "Status: Interferensi Terjadi ✓";
                    DSE_Text_InterferenceStatus.color = Color.green;
                }
            }
            else
            {
                if (DSE_Text_CentralWidth != null) DSE_Text_CentralWidth.text = "Lebar terang pusat (w): —";
                if (DSE_Text_Y1 != null) DSE_Text_Y1.text = "Terang ke-1 (y₁)     : —";
                if (DSE_Text_Y2 != null) DSE_Text_Y2.text = "Terang ke-2 (y₂)     : —";
                if (DSE_Text_Y3 != null) DSE_Text_Y3.text = "Terang ke-3 (y₃)     : —";
                if (DSE_Text_DeltaY != null) DSE_Text_DeltaY.text = "Δy (jarak antar terang): —";

                if (DSE_Text_InterferenceStatus != null)
                {
                    DSE_Text_InterferenceStatus.text = "Status: Tidak Ada Interferensi ✗";
                    DSE_Text_InterferenceStatus.color = new Color(1f, 0.4f, 0f);
                }
            }
        }

        private void DSE_UpdateActivateButtonVisual()
        {
            if (DSE_ActivateButtonText == null) return;
            DSE_ActivateButtonText.text = DSE_LaserIsOn ? "🔴 MATIKAN LASER" : "🟢 AKTIFKAN LASER";

            if (DSE_ActivateButton != null)
            {
                var colors = DSE_ActivateButton.colors;
                colors.normalColor = DSE_LaserIsOn
                    ? new Color(0.8f, 0.1f, 0.1f)
                    : new Color(0.1f, 0.7f, 0.2f);
                DSE_ActivateButton.colors = colors;
            }
        }
    }
}