using UnityEngine;

namespace DoubleSlit
{
    [RequireComponent(typeof(Renderer))]
    public class DSE_ScreenRenderer : MonoBehaviour
    {
        [Header("Resolusi Tekstur")]
        public int DSE_TextureWidth = 512;
        public int DSE_TextureHeight = 2048;

        [Header("Tinggi Fisik Layar (meter) — harus = Scale Y Quad")]
        public float DSE_ScreenPhysicalHeight = 0.837f;

        [Header("Kecerahan & Kontras")]
        [Range(1f, 20f)] public float DSE_Brightness = 10f;
        [Tooltip("Makin tinggi = pita gelap makin hitam pekat")]
        [Range(1f, 8f)] public float DSE_GammaCorrection = 2.5f;

        [Header("Lebar Fade Horizontal")]
        [Range(1f, 10f)] public float DSE_HorizontalFalloff = 2.5f;

        private Texture2D DSE_Texture;
        private Renderer DSE_Rend;
        private Material DSE_ScreenMat;

        private DSE_LaserColor DSE_Color1 = DSE_LaserColor.Green;
        private DSE_LaserColor DSE_Color2 = DSE_LaserColor.Green;
        private bool DSE_LaserOn = false;

        void Awake()
        {
            DSE_Rend = GetComponent<Renderer>();
            DSE_InitTexture();
        }

        private void DSE_InitTexture()
        {
            DSE_Texture = new Texture2D(DSE_TextureWidth, DSE_TextureHeight,
                                        TextureFormat.RGBA32, false);
            DSE_Texture.name = "DSE_PatternTex";
            DSE_Texture.filterMode = FilterMode.Bilinear;
            DSE_Texture.wrapMode = TextureWrapMode.Clamp;

            Shader sh = Shader.Find("Unlit/Texture")
                     ?? Shader.Find("Sprites/Default");
            DSE_ScreenMat = new Material(sh);

            // Render dua sisi agar tidak masalah rotasi
            DSE_ScreenMat.SetInt("_Cull",
                (int)UnityEngine.Rendering.CullMode.Off);

            DSE_ScreenMat.mainTexture = DSE_Texture;
            DSE_Rend.material = DSE_ScreenMat;

            DSE_ClearBlack();
        }

        public void DSE_UpdatePattern(DSE_LaserColor c1, DSE_LaserColor c2,
                                      bool laserOn)
        {
            DSE_Color1 = c1;
            DSE_Color2 = c2;
            DSE_LaserOn = laserOn;
            DSE_Render();
        }

        private void DSE_Render()
        {
            if (!DSE_LaserOn) { DSE_ClearBlack(); return; }

            if (DSE_Color1 == DSE_Color2)
                DSE_DrawInterference(DSE_Color1);
            else
                DSE_DrawIncoherent(DSE_Color1, DSE_Color2);
        }

        private void DSE_DrawInterference(DSE_LaserColor lc)
        {
            float lambda = DSE_InterferenceCalculator.GetWavelength(lc);
            Color baseCol = DSE_ColorHelper.ToUnityColor(lc);

            Color[] pixels = new Color[DSE_TextureWidth * DSE_TextureHeight];

            int cy = DSE_TextureHeight / 2; // Pusat vertikal = y_fisik 0
            float pixelToMeter = DSE_ScreenPhysicalHeight / DSE_TextureHeight;

            for (int py = 0; py < DSE_TextureHeight; py++)
            {
                // Posisi fisik dari pusat layar (meter)
                float y = (py - cy) * pixelToMeter;

                // ── Rumus intensitas lengkap I(θ) ──────────────────────
                float sinTheta = y / Mathf.Sqrt(y * y +
                    DSE_InterferenceCalculator.L * DSE_InterferenceCalculator.L);

                float beta = (2f * Mathf.PI * DSE_InterferenceCalculator.a
                               * sinTheta) / lambda;
                float delta = (2f * Mathf.PI * DSE_InterferenceCalculator.d
                               * sinTheta) / lambda;

                // sinc²: faktor difraksi single-slit
                float sinc = (Mathf.Abs(beta) < 1e-10f)
                    ? 1f : Mathf.Sin(beta * 0.5f) / (beta * 0.5f);
                float diffractionFactor = sinc * sinc;

                // cos²: faktor interferensi double-slit
                float cosFactor = Mathf.Cos(delta * 0.5f);
                cosFactor = cosFactor * cosFactor;

                // Intensitas mentah 0..1
                float rawIntensity = diffractionFactor * cosFactor;

                // ── Gamma correction untuk memperjelas pita gelap ──────
                // Tanpa ini, transisi terang→gelap terlalu gradual
                float intensity = Mathf.Pow(rawIntensity, DSE_GammaCorrection);
                intensity = Mathf.Clamp01(intensity * DSE_Brightness);

                // Warna: campurkan ke putih di puncak terang
                Color pixCol;
                if (intensity > 0.9f)
                {
                    // Puncak terang: campurkan ke putih agar terasa lebih silau
                    pixCol = Color.Lerp(baseCol, Color.white,
                                        (intensity - 0.9f) * 5f);
                }
                else
                {
                    pixCol = baseCol * intensity;
                }
                pixCol.a = 1f;

                // ── Isi baris horizontal dengan fade ke tepi ───────────
                for (int px = 0; px < DSE_TextureWidth; px++)
                {
                    float hn = (px / (float)(DSE_TextureWidth - 1)) - 0.5f;
                    float hFade = Mathf.Exp(-hn * hn
                                  * DSE_HorizontalFalloff
                                  * DSE_HorizontalFalloff * 8f);

                    Color final = pixCol * hFade;
                    final.a = 1f;
                    pixels[py * DSE_TextureWidth + px] = final;
                }
            }

            DSE_Texture.SetPixels(pixels);
            DSE_Texture.Apply();
        }

        private void DSE_DrawIncoherent(DSE_LaserColor lc1, DSE_LaserColor lc2)
        {
            float lam1 = DSE_InterferenceCalculator.GetWavelength(lc1);
            float lam2 = DSE_InterferenceCalculator.GetWavelength(lc2);
            Color col1 = DSE_ColorHelper.ToUnityColor(lc1);
            Color col2 = DSE_ColorHelper.ToUnityColor(lc2);

            Color[] pixels = new Color[DSE_TextureWidth * DSE_TextureHeight];
            int cy = DSE_TextureHeight / 2;
            float pixelToMeter = DSE_ScreenPhysicalHeight / DSE_TextureHeight;

            for (int py = 0; py < DSE_TextureHeight; py++)
            {
                float y = (py - cy) * pixelToMeter;

                float i1 = DSE_InterferenceCalculator
                               .DiffractionOnlyIntensity(y, lam1);
                float i2 = DSE_InterferenceCalculator
                               .DiffractionOnlyIntensity(y, lam2);

                i1 = Mathf.Clamp01(Mathf.Pow(i1, DSE_GammaCorrection)
                     * DSE_Brightness);
                i2 = Mathf.Clamp01(Mathf.Pow(i2, DSE_GammaCorrection)
                     * DSE_Brightness);

                Color pixCol = col1 * i1 + col2 * i2;
                pixCol.a = 1f;

                for (int px = 0; px < DSE_TextureWidth; px++)
                {
                    float hn = (px / (float)(DSE_TextureWidth - 1)) - 0.5f;
                    float hFade = Mathf.Exp(-hn * hn
                                  * DSE_HorizontalFalloff
                                  * DSE_HorizontalFalloff * 8f);
                    Color final = pixCol * hFade;
                    final.a = 1f;
                    pixels[py * DSE_TextureWidth + px] = final;
                }
            }

            DSE_Texture.SetPixels(pixels);
            DSE_Texture.Apply();
        }

        private void DSE_ClearBlack()
        {
            var px = new Color[DSE_TextureWidth * DSE_TextureHeight];
            for (int i = 0; i < px.Length; i++)
                px[i] = new Color(0, 0, 0, 1);
            DSE_Texture.SetPixels(px);
            DSE_Texture.Apply();
        }

        void OnDestroy()
        {
            if (DSE_Texture != null) Destroy(DSE_Texture);
            if (DSE_ScreenMat != null) Destroy(DSE_ScreenMat);
        }
    }
}