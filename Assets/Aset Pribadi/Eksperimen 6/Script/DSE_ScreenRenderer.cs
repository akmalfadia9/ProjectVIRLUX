using UnityEngine;

namespace DoubleSlit
{
    /// <summary>
    /// Menggambar pola interferensi di layar.
    ///
    /// PERBAIKAN:
    /// - Pusat pola selalu di TENGAH tekstur (pixel 512 dari 1024)
    /// - DSE_ScreenPhysicalWidth diperkecil ke 0.01 m agar pita terlihat
    /// - Gunakan Quad dengan pivot Center agar tidak offset
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class DSE_ScreenRenderer : MonoBehaviour
    {
        [Header("Resolusi Tekstur")]
        public int DSE_TextureWidth = 1024;
        public int DSE_TextureHeight = 512;

        [Header("Rentang Fisik Layar (meter)")]
        [Tooltip(
            "Lebar fisik total yang direpresentasikan oleh tekstur.\n" +
            "KECIL = zoom in, pita terang-gelap lebih besar dan jelas.\n" +
            "Gunakan 0.01 (1 cm) untuk melihat beberapa pita terang-gelap.\n" +
            "Δy hijau = 5.32 mm, jadi 0.01 m akan menampilkan ~2 pita.")]
        public float DSE_ScreenPhysicalWidth = 0.01f;  // 1 cm — KUNCI utama visibilitas pola!

        [Header("Kecerahan Pola")]
        [Range(1f, 10f)] public float DSE_Brightness = 4f;

        [Header("Ketebalan Pita Vertikal")]
        [Tooltip("Gaussian falloff vertikal. Nilai kecil = pita lebih tipis")]
        [Range(2f, 20f)] public float DSE_VerticalFalloff = 8f;

        private Texture2D DSE_Texture;
        private Renderer DSE_Rend;
        private Material DSE_ScreenMat;

        // State
        private DSE_LaserColor DSE_Color1 = DSE_LaserColor.Green;
        private DSE_LaserColor DSE_Color2 = DSE_LaserColor.Green;
        private bool DSE_LaserOn = false;

        void Awake()
        {
            DSE_Rend = GetComponent<Renderer>();
            DSE_InitTexture();

            // Pastikan pivot objek layar di center
            // (seharusnya sudah default untuk Quad)
        }

        private void DSE_InitTexture()
        {
            DSE_Texture = new Texture2D(DSE_TextureWidth, DSE_TextureHeight,
                                        TextureFormat.RGBA32, false);
            DSE_Texture.name = "DSE_PatternTex";
            DSE_Texture.filterMode = FilterMode.Bilinear;
            DSE_Texture.wrapMode = TextureWrapMode.Clamp;

            // Shader unlit agar warna tidak terpengaruh pencahayaan scene
            Shader sh = Shader.Find("Unlit/Texture")
                     ?? Shader.Find("Sprites/Default");
            DSE_ScreenMat = new Material(sh);
            DSE_ScreenMat.mainTexture = DSE_Texture;
            DSE_Rend.material = DSE_ScreenMat;

            DSE_ClearBlack();
        }

        // ─────────────────────────────────────────────────────────────
        // PUBLIC API
        // ─────────────────────────────────────────────────────────────

        public void DSE_UpdatePattern(DSE_LaserColor c1, DSE_LaserColor c2, bool laserOn)
        {
            DSE_Color1 = c1;
            DSE_Color2 = c2;
            DSE_LaserOn = laserOn;
            DSE_Render();
        }

        // ─────────────────────────────────────────────────────────────
        // RENDER
        // ─────────────────────────────────────────────────────────────

        private void DSE_Render()
        {
            if (!DSE_LaserOn)
            {
                DSE_ClearBlack();
                return;
            }

            if (DSE_Color1 == DSE_Color2)
                DSE_DrawInterference(DSE_Color1);
            else
                DSE_DrawIncoherent(DSE_Color1, DSE_Color2);
        }

        /// <summary>
        /// Pola interferensi lengkap.
        /// KUNCI: pixel x=0 adalah tepi kiri, x=Width/2 adalah PUSAT LAYAR (y_fisik=0).
        /// </summary>
        private void DSE_DrawInterference(DSE_LaserColor lc)
        {
            float lambda = DSE_InterferenceCalculator.GetWavelength(lc);
            Color baseCol = DSE_ColorHelper.ToUnityColor(lc);

            Color[] pixels = new Color[DSE_TextureWidth * DSE_TextureHeight];

            int cx = DSE_TextureWidth / 2;   // Pixel tengah horizontal = y_fisik=0
            float pixelToMeter = DSE_ScreenPhysicalWidth / DSE_TextureWidth;

            for (int px = 0; px < DSE_TextureWidth; px++)
            {
                // Konversi pixel ke posisi fisik (meter), diukur dari PUSAT
                // px=cx → y=0 (pusat), px=0 → y=-halfWidth, px=Width-1 → y=+halfWidth
                float y_physical = (px - cx) * pixelToMeter;

                // Hitung intensitas menggunakan rumus fisika lengkap
                float intensity = DSE_InterferenceCalculator.Intensity(y_physical, lambda);
                intensity = Mathf.Clamp01(intensity * DSE_Brightness);

                // Warna pixel
                Color pixCol = baseCol * intensity;
                // Sedikit highlight putih di puncak terang
                if (intensity > 0.85f)
                    pixCol += Color.white * (intensity - 0.85f) * 0.4f;
                pixCol.a = 1f;

                // Isi seluruh kolom vertikal dengan gaussian falloff
                for (int py = 0; py < DSE_TextureHeight; py++)
                {
                    float vn = (py / (float)(DSE_TextureHeight - 1)) - 0.5f;
                    float vFade = Mathf.Exp(-vn * vn * DSE_VerticalFalloff * DSE_VerticalFalloff * 0.5f);

                    Color final = pixCol * vFade;
                    final.a = 1f;
                    pixels[py * DSE_TextureWidth + px] = final;
                }
            }

            DSE_Texture.SetPixels(pixels);
            DSE_Texture.Apply();
        }

        /// <summary>
        /// Tidak ada interferensi: 2 titik/pita lebar dari masing-masing sumber,
        /// tidak ada pola gelap-terang.
        /// </summary>
        private void DSE_DrawIncoherent(DSE_LaserColor lc1, DSE_LaserColor lc2)
        {
            float lam1 = DSE_InterferenceCalculator.GetWavelength(lc1);
            float lam2 = DSE_InterferenceCalculator.GetWavelength(lc2);
            Color col1 = DSE_ColorHelper.ToUnityColor(lc1);
            Color col2 = DSE_ColorHelper.ToUnityColor(lc2);

            Color[] pixels = new Color[DSE_TextureWidth * DSE_TextureHeight];
            int cx = DSE_TextureWidth / 2;
            float pixelToMeter = DSE_ScreenPhysicalWidth / DSE_TextureWidth;

            // Offset sumber: celah 1 di +d/2 dari pusat, celah 2 di -d/2
            // Dalam satuan sudut/posisi layar: y_offset = (d/2) * L / L = d/2
            // tapi kita pakai nilai visual sederhana
            float slitOffsetM = DSE_InterferenceCalculator.d * 0.5f;
            // Proyeksi ke layar (approx): offset_layar ≈ slitOffset * L / L = slitOffset
            // Namun karena layar sangat dekat, anggap saja keduanya ke titik tengah
            // dengan sedikit offset kecil yang terlihat
            float visualOffset = DSE_ScreenPhysicalWidth * 0.1f; // 10% dari lebar layar visual

            for (int px = 0; px < DSE_TextureWidth; px++)
            {
                float y = (px - cx) * pixelToMeter;

                // Difraksi dari celah 1 (sedikit ke atas dari pusat)
                float i1 = DSE_InterferenceCalculator.DiffractionOnlyIntensity(
                               y - visualOffset, lam1) * 0.7f;
                // Difraksi dari celah 2 (sedikit ke bawah dari pusat)
                float i2 = DSE_InterferenceCalculator.DiffractionOnlyIntensity(
                               y + visualOffset, lam2) * 0.7f;

                i1 = Mathf.Clamp01(i1 * DSE_Brightness);
                i2 = Mathf.Clamp01(i2 * DSE_Brightness);

                Color pixCol = col1 * i1 + col2 * i2;
                pixCol.a = 1f;

                for (int py = 0; py < DSE_TextureHeight; py++)
                {
                    float vn = (py / (float)(DSE_TextureHeight - 1)) - 0.5f;
                    float vFade = Mathf.Exp(-vn * vn * DSE_VerticalFalloff * DSE_VerticalFalloff * 0.5f);
                    Color final = pixCol * vFade;
                    final.a = 1f;
                    pixels[py * DSE_TextureWidth + px] = final;
                }
            }

            DSE_Texture.SetPixels(pixels);
            DSE_Texture.Apply();
        }

        private void DSE_ClearBlack()
        {
            var pixels = new Color[DSE_TextureWidth * DSE_TextureHeight];
            // Semua hitam, alpha 1
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color(0, 0, 0, 1);
            DSE_Texture.SetPixels(pixels);
            DSE_Texture.Apply();
        }

        void OnDestroy()
        {
            if (DSE_Texture != null) Destroy(DSE_Texture);
            if (DSE_ScreenMat != null) Destroy(DSE_ScreenMat);
        }
    }
}