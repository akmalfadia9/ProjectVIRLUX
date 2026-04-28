using UnityEngine;

namespace DoubleSlit
{
    /// <summary>
    /// Kalkulator fisika interferensi celah ganda.
    /// Semua parameter dalam satuan SI (meter).
    /// </summary>
    public static class DSE_InterferenceCalculator
    {
        // Parameter tetap
        public const float L = 2.0f;           // Jarak celah → layar (meter)
        public const float D_laser = 1.0f;     // Jarak laser → celah (meter)
        public const float a = 0.00004f;       // Lebar celah: 40 μm
        public const float d = 0.0002f;        // Jarak antar celah: 200 μm

        // Panjang gelombang per warna (meter)
        public static float GetWavelength(DSE_LaserColor color)
        {
            return color switch
            {
                DSE_LaserColor.Red => 700e-9f,
                DSE_LaserColor.Yellow => 580e-9f,
                DSE_LaserColor.Green => 532e-9f,
                DSE_LaserColor.Blue => 450e-9f,
                DSE_LaserColor.Violet => 405e-9f,
                _ => 532e-9f
            };
        }

        /// <summary>Posisi terang ke-m dari pusat: y_m = (m × λ × L) / d</summary>
        public static float BrightFringePosition(int m, float lambda)
            => (m * lambda * L) / d;

        /// <summary>Lebar terang pusat: w = (2 × λ × L) / a</summary>
        public static float CentralMaximaWidth(float lambda)
            => (2f * lambda * L) / a;

        /// <summary>Jarak antar terang berurutan: Δy = (λ × L) / d</summary>
        public static float FringeSpacing(float lambda)
            => (lambda * L) / d;

        /// <summary>
        /// Intensitas pada posisi y di layar.
        /// I(θ) = I₀ × [sinc(β/2)]² × cos²(δ/2)
        /// β = (2π × a × sinθ) / λ
        /// δ = (2π × d × sinθ) / λ
        /// sinθ ≈ y / L (paraxial approximation)
        /// </summary>
        public static float Intensity(float y, float lambda, float I0 = 1f)
        {
            float sinTheta = y / Mathf.Sqrt(y * y + L * L);

            float beta = (2f * Mathf.PI * a * sinTheta) / lambda;
            float delta = (2f * Mathf.PI * d * sinTheta) / lambda;

            float sincTerm = (Mathf.Abs(beta) < 1e-10f)
                ? 1f
                : Mathf.Sin(beta / 2f) / (beta / 2f);

            float cosTerm = Mathf.Cos(delta / 2f);

            return I0 * sincTerm * sincTerm * cosTerm * cosTerm;
        }

        /// <summary>
        /// Intensitas difraksi saja (single slit), digunakan saat tidak ada interferensi.
        /// I(θ) = I₀ × [sinc(β/2)]²
        /// </summary>
        public static float DiffractionOnlyIntensity(float y, float lambda, float I0 = 1f)
        {
            float sinTheta = y / Mathf.Sqrt(y * y + L * L);
            float beta = (2f * Mathf.PI * a * sinTheta) / lambda;

            float sincTerm = (Mathf.Abs(beta) < 1e-10f)
                ? 1f
                : Mathf.Sin(beta / 2f) / (beta / 2f);

            return I0 * sincTerm * sincTerm;
        }
    }
}