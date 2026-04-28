using UnityEngine;
using System.Collections.Generic;

namespace DoubleSlit
{
    /// <summary>
    /// Menggambar sinar-sinar difraksi dari TIAP CELAH ke layar.
    ///
    /// Konsep fisika yang diimplementasikan:
    /// - Setiap celah bertindak sebagai sumber gelombang baru (prinsip Huygens)
    /// - Dari Slit1 dan Slit2 keluar kipas sinar (fan) ke berbagai sudut
    /// - Jika koherensi: sinar dari Slit1 dan Slit2 menuju posisi TERANG yang sama
    ///   (y_m = m*lambda*L/d) sehingga terlihat reinforcement
    /// - Jika tidak koherensi: hanya sinar lurus (tidak ada pola)
    ///
    /// Attach ke: DSE_DoubleSlitPlate
    /// </summary>
    public class DSE_BeamAfterSlit : MonoBehaviour
    {
        [Header("Referensi Wajib")]
        public Transform DSE_ScreenTransform;
        public DSE_LaserController DSE_Laser1Controller;
        public DSE_LaserController DSE_Laser2Controller;

        [Header("Titik Celah — drag DSE_SlitPoint1 dan DSE_SlitPoint2")]
        public Transform DSE_SlitPoint1Transform;  // Celah atas
        public Transform DSE_SlitPoint2Transform;  // Celah bawah

        [Header("Jumlah Sinar Difraksi per Celah")]
        [Range(3, 15)]
        [Tooltip("Jumlah sinar kipas yang keluar dari tiap celah. 7 sudah cukup terlihat.")]
        public int DSE_RaysPerSlit = 7;

        [Header("Sudut Kipas Difraksi")]
        [Tooltip("Sudut total kipas dalam derajat. 60 = ±30° dari arah lurus.")]
        public float DSE_FanAngleDegrees = 60f;

        [Header("Visual Sinar")]
        public float DSE_BeamWidth = 0.005f;
        [Range(1f, 5f)] public float DSE_GlowIntensity = 3f;

        // Pool sinar: [slit_index][ray_index]
        private LineRenderer[] DSE_Slit1Rays;
        private LineRenderer[] DSE_Slit2Rays;
        private Material[] DSE_Slit1Mats;
        private Material[] DSE_Slit2Mats;

        // Satu sinar lurus per celah untuk kasus tidak koherensi
        private LineRenderer DSE_Slit1StraightRay;
        private LineRenderer DSE_Slit2StraightRay;
        private Material DSE_Mat1Straight;
        private Material DSE_Mat2Straight;

        void Awake()
        {
            DSE_CreateAllBeams();
        }

        void Update()
        {
            bool on1 = DSE_Laser1Controller != null && DSE_Laser1Controller.DSE_GetIsActive();
            bool on2 = DSE_Laser2Controller != null && DSE_Laser2Controller.DSE_GetIsActive();

            if (!on1 && !on2)
            {
                DSE_HideAll();
                return;
            }

            bool sameColor = on1 && on2 &&
                             DSE_Laser1Controller.DSE_GetCurrentColor() ==
                             DSE_Laser2Controller.DSE_GetCurrentColor();

            if (sameColor)
            {
                // Interferensi: tampilkan kipas sinar ke posisi terang
                DSE_ShowInterferenceFan(DSE_Laser1Controller.DSE_GetCurrentColor());
            }
            else
            {
                // Tidak ada interferensi: hanya sinar lurus dari tiap celah yang aktif
                DSE_ShowStraightBeams(on1, on2);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // INISIALISASI
        // ─────────────────────────────────────────────────────────────

        private void DSE_CreateAllBeams()
        {
            DSE_Slit1Rays = new LineRenderer[DSE_RaysPerSlit];
            DSE_Slit2Rays = new LineRenderer[DSE_RaysPerSlit];
            DSE_Slit1Mats = new Material[DSE_RaysPerSlit];
            DSE_Slit2Mats = new Material[DSE_RaysPerSlit];

            for (int i = 0; i < DSE_RaysPerSlit; i++)
            {
                DSE_Slit1Rays[i] = DSE_MakeLine($"DSE_S1Ray_{i}", out DSE_Slit1Mats[i]);
                DSE_Slit2Rays[i] = DSE_MakeLine($"DSE_S2Ray_{i}", out DSE_Slit2Mats[i]);
            }

            DSE_Slit1StraightRay = DSE_MakeLine("DSE_S1Straight", out DSE_Mat1Straight);
            DSE_Slit2StraightRay = DSE_MakeLine("DSE_S2Straight", out DSE_Mat2Straight);

            DSE_HideAll();
        }

        private LineRenderer DSE_MakeLine(string goName, out Material mat)
        {
            var go = new GameObject(goName);
            go.transform.SetParent(transform, false);

            var lr = go.AddComponent<LineRenderer>();

            Shader sh = Shader.Find("Particles/Standard Unlit")
                     ?? Shader.Find("Unlit/Color")
                     ?? Shader.Find("Sprites/Default");
            mat = new Material(sh);

            lr.material = mat;
            lr.positionCount = 2;
            lr.startWidth = DSE_BeamWidth;
            lr.endWidth = DSE_BeamWidth * 0.4f;
            lr.useWorldSpace = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.numCapVertices = 4;
            lr.enabled = false;

            return lr;
        }

        // ─────────────────────────────────────────────────────────────
        // MODE 1: KIPAS INTERFERENSI
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Tampilkan kipas sinar dari KEDUA celah.
        /// Sinar-sinar diarahkan ke posisi terang (bright fringes) di layar,
        /// sehingga secara visual mencerminkan di mana gelombang berintegrasi.
        /// </summary>
        private void DSE_ShowInterferenceFan(DSE_LaserColor color)
        {
            // Sembunyikan sinar lurus
            DSE_HideStraightBeams();

            float lambda = DSE_InterferenceCalculator.GetWavelength(color);
            Color baseCol = DSE_ColorHelper.ToUnityColor(color);
            Color glowCol = baseCol * DSE_GlowIntensity;
            glowCol.a = 1f;

            Vector3 slit1Pos = DSE_GetSlit1Position();
            Vector3 slit2Pos = DSE_GetSlit2Position();
            Vector3 screenCenter = DSE_ScreenTransform.position;

            // Hitung posisi-posisi terang di layar menggunakan rumus fisika
            // y_m = m * lambda * L / d
            List<float> brightPositions = DSE_ComputeBrightFringePositions(lambda);

            // Distribusikan sinar ke posisi terang
            // Jika jumlah sinar < jumlah posisi terang, pakai semua sinar ke sebagian posisi
            for (int i = 0; i < DSE_RaysPerSlit; i++)
            {
                if (i < brightPositions.Count)
                {
                    float yPos = brightPositions[i];

                    // Titik target di layar: center layar + offset vertikal
                    // Gunakan up vector layar untuk arah vertikal
                    Vector3 targetOnScreen = screenCenter
                        + DSE_ScreenTransform.up * yPos;

                    // Intensitas: terang pusat (m=0) paling terang, tepi lebih redup
                    float intensity = DSE_InterferenceCalculator.Intensity(yPos, lambda);
                    intensity = Mathf.Clamp01(intensity);

                    Color rayColor = Color.Lerp(baseCol * 0.2f, glowCol, intensity);
                    rayColor.a = 1f;

                    // Slit 1 → target terang
                    DSE_SetRay(DSE_Slit1Rays[i], DSE_Slit1Mats[i],
                               slit1Pos, targetOnScreen, rayColor, intensity);

                    // Slit 2 → target terang yang sama (interferensi konstruktif)
                    DSE_SetRay(DSE_Slit2Rays[i], DSE_Slit2Mats[i],
                               slit2Pos, targetOnScreen, rayColor, intensity);
                }
                else
                {
                    // Lebih banyak sinar dari posisi terang — sembunyikan selebihnya
                    DSE_Slit1Rays[i].enabled = false;
                    DSE_Slit2Rays[i].enabled = false;
                }
            }
        }

        /// <summary>
        /// Hitung daftar posisi terang (dalam meter dari pusat layar).
        /// Hasilkan posisi untuk m = 0, ±1, ±2, ±3, ... sampai DSE_RaysPerSlit terpenuhi.
        /// </summary>
        private List<float> DSE_ComputeBrightFringePositions(float lambda)
        {
            var positions = new List<float>();

            // m = 0 (terang pusat)
            positions.Add(0f);

            // m = ±1, ±2, ±3, ...
            int maxM = (DSE_RaysPerSlit - 1) / 2 + 1;
            for (int m = 1; m <= maxM && positions.Count < DSE_RaysPerSlit; m++)
            {
                float ym = DSE_InterferenceCalculator.BrightFringePosition(m, lambda);

                // Tambahkan +m dan -m, pastikan tidak melebihi setengah lebar layar fisik
                float halfScreenHeight = 0.08f; // 8 cm setengah tinggi layar visual

                if (ym < halfScreenHeight)
                {
                    if (positions.Count < DSE_RaysPerSlit)
                        positions.Add(+ym);
                    if (positions.Count < DSE_RaysPerSlit)
                        positions.Add(-ym);
                }
            }

            return positions;
        }

        private void DSE_SetRay(LineRenderer lr, Material mat,
                                Vector3 from, Vector3 to,
                                Color color, float alpha)
        {
            lr.enabled = true;
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);

            Color c = color;
            c.a = 1f;
            mat.color = c;
            lr.startColor = c;

            Color endC = c;
            endC.a = alpha * 0.7f;
            lr.endColor = endC;

            float w = DSE_BeamWidth * Mathf.Lerp(0.3f, 1f, alpha);
            lr.startWidth = w;
            lr.endWidth = w * 0.3f;
        }

        // ─────────────────────────────────────────────────────────────
        // MODE 2: TIDAK ADA INTERFERENSI (warna berbeda)
        // ─────────────────────────────────────────────────────────────

        private void DSE_ShowStraightBeams(bool show1, bool show2)
        {
            // Sembunyikan semua sinar fan
            DSE_HideFanBeams();

            Vector3 screenCenter = DSE_ScreenTransform.position;

            if (show1 && DSE_Laser1Controller != null)
            {
                Color col1 = DSE_ColorHelper.ToUnityColor(DSE_Laser1Controller.DSE_GetCurrentColor())
                             * DSE_GlowIntensity;
                col1.a = 1f;
                DSE_SetRay(DSE_Slit1StraightRay, DSE_Mat1Straight,
                           DSE_GetSlit1Position(), screenCenter, col1, 0.8f);
            }
            else DSE_Slit1StraightRay.enabled = false;

            if (show2 && DSE_Laser2Controller != null)
            {
                Color col2 = DSE_ColorHelper.ToUnityColor(DSE_Laser2Controller.DSE_GetCurrentColor())
                             * DSE_GlowIntensity;
                col2.a = 1f;
                DSE_SetRay(DSE_Slit2StraightRay, DSE_Mat2Straight,
                           DSE_GetSlit2Position(), screenCenter, col2, 0.8f);
            }
            else DSE_Slit2StraightRay.enabled = false;
        }

        // ─────────────────────────────────────────────────────────────
        // HELPER
        // ─────────────────────────────────────────────────────────────

        private Vector3 DSE_GetSlit1Position()
        {
            if (DSE_SlitPoint1Transform != null) return DSE_SlitPoint1Transform.position;
            // Fallback: posisi plat + offset atas
            return transform.position + transform.up * 0.05f;
        }

        private Vector3 DSE_GetSlit2Position()
        {
            if (DSE_SlitPoint2Transform != null) return DSE_SlitPoint2Transform.position;
            return transform.position - transform.up * 0.05f;
        }

        private void DSE_HideFanBeams()
        {
            for (int i = 0; i < DSE_RaysPerSlit; i++)
            {
                if (DSE_Slit1Rays != null && i < DSE_Slit1Rays.Length)
                    DSE_Slit1Rays[i].enabled = false;
                if (DSE_Slit2Rays != null && i < DSE_Slit2Rays.Length)
                    DSE_Slit2Rays[i].enabled = false;
            }
        }

        private void DSE_HideStraightBeams()
        {
            if (DSE_Slit1StraightRay != null) DSE_Slit1StraightRay.enabled = false;
            if (DSE_Slit2StraightRay != null) DSE_Slit2StraightRay.enabled = false;
        }

        private void DSE_HideAll()
        {
            DSE_HideFanBeams();
            DSE_HideStraightBeams();
        }

        void OnDestroy()
        {
            if (DSE_Slit1Mats != null)
                foreach (var m in DSE_Slit1Mats) if (m) Destroy(m);
            if (DSE_Slit2Mats != null)
                foreach (var m in DSE_Slit2Mats) if (m) Destroy(m);
            if (DSE_Mat1Straight) Destroy(DSE_Mat1Straight);
            if (DSE_Mat2Straight) Destroy(DSE_Mat2Straight);
        }
    }
}