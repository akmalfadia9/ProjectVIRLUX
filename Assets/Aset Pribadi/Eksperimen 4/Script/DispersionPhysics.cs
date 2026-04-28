using UnityEngine;

/// <summary>
/// Kelas utilitas statis berisi seluruh logika fisika optik.
/// Tidak perlu di-attach ke GameObject — dipakai oleh script lain.
/// Semua metode bersifat pure function (tidak ada state).
/// </summary>
public static class DispersionPhysics
{
    // =====================================================================
    // KONSTANTA FISIKA
    // =====================================================================

    /// <summary>
    /// Indeks bias udara/vakum. Selalu = 1.0 untuk kalkulasi standar.
    /// </summary>
    public const float N_AIR = 1.0f;

    // =====================================================================
    // HUKUM SNELLIUS (Snell's Law)
    // =====================================================================

    /// <summary>
    /// Menghitung sudut bias menggunakan Hukum Snellius:
    ///   n1 * sin(θ1) = n2 * sin(θ2)
    ///   θ2 = arcsin( (n1 / n2) * sin(θ1) )
    ///
    /// Parameter:
    ///   incidenceAngleDeg : sudut datang dari garis normal (derajat)
    ///   n1                : indeks bias medium asal
    ///   n2                : indeks bias medium tujuan
    ///
    /// Mengembalikan sudut bias dalam derajat, atau float.NaN jika
    /// terjadi Total Internal Reflection (TIR).
    /// </summary>
    public static float SnellRefraction(float incidenceAngleDeg, float n1, float n2)
    {
        float sinTheta1 = Mathf.Sin(incidenceAngleDeg * Mathf.Deg2Rad);

        // Rasio n1/n2 dikalikan sin(θ1) untuk mendapatkan sin(θ2)
        float sinTheta2 = (n1 / n2) * sinTheta1;

        // Jika |sin(θ2)| > 1, tidak ada pembiasan — terjadi TIR
        if (Mathf.Abs(sinTheta2) > 1.0f)
        {
            return float.NaN; // Total Internal Reflection
        }

        return Mathf.Asin(sinTheta2) * Mathf.Rad2Deg;
    }

    // =====================================================================
    // GEOMETRI PRISMA SEGITIGA
    // =====================================================================

    /// <summary>
    /// Menghitung sudut datang pada permukaan KEDUA prisma.
    ///
    /// DERIVASI GEOMETRI:
    /// Untuk prisma segitiga dengan sudut pembias A (apex angle):
    ///   - Setelah melewati permukaan pertama, sinar membentuk sudut
    ///     refraksi θ2 terhadap normal permukaan pertama.
    ///   - Di dalam prisma, hubungan geometris antara dua permukaan
    ///     menghasilkan:
    ///     θ3 = A - θ2
    ///   - di mana θ3 adalah sudut datang pada permukaan kedua.
    ///
    /// Ini adalah turunan langsung dari sifat sudut dalam segitiga.
    /// </summary>
    /// <param name="apexAngleDeg">Sudut pembias prisma (A) dalam derajat</param>
    /// <param name="firstRefractionAngleDeg">Sudut bias pada permukaan pertama (θ2)</param>
    /// <returns>Sudut datang pada permukaan kedua (θ3) dalam derajat</returns>
    public static float SecondIncidenceAngle(float apexAngleDeg, float firstRefractionAngleDeg)
    {
        // θ3 = A - θ2
        return apexAngleDeg - firstRefractionAngleDeg;
    }

    // =====================================================================
    // KALKULASI DISPERSI LENGKAP
    // =====================================================================

    /// <summary>
    /// Struktur data yang menyimpan hasil kalkulasi fisika untuk satu warna.
    /// </summary>
    public struct DispersionResult
    {
        public SpectrumColor color;
        public float n;                        // Indeks bias material untuk warna ini
        public float theta1;                   // Sudut datang pertama (udara→prisma)
        public float theta2;                   // Sudut bias pertama (dalam prisma)
        public float theta3;                   // Sudut datang kedua (dalam prisma, ke permukaan keluar)
        public float theta4;                   // Sudut bias kedua (keluar prisma ke udara)
        public float deviationAngle;           // Sudut deviasi total (D)
        public bool totalInternalReflection;   // Apakah terjadi TIR di permukaan kedua?
        public Vector3 exitDirection;          // Arah keluar sinar dalam world space
    }

    /// <summary>
    /// Menghitung seluruh proses dispersi untuk satu warna cahaya melalui prisma.
    ///
    /// ALUR PERHITUNGAN FISIKA:
    /// 1. Sinar putih masuk ke permukaan pertama prisma dengan sudut θ1.
    ///    Hukum Snell: n_udara * sin(θ1) = n_material * sin(θ2)
    ///    → θ2 = arcsin((1.0 / n) * sin(θ1))
    ///
    /// 2. Sinar merambat di dalam prisma. Geometri prisma menentukan:
    ///    θ3 = A - θ2    (A = sudut pembias / apex angle)
    ///
    /// 3. Sinar mencapai permukaan kedua dengan sudut datang θ3.
    ///    Hukum Snell: n_material * sin(θ3) = n_udara * sin(θ4)
    ///    → θ4 = arcsin(n * sin(θ3))
    ///
    /// 4. Sudut deviasi total:
    ///    D = (θ1 - θ2) + (θ4 - θ3)
    ///      = θ1 + θ4 - A
    ///
    /// Karena n berbeda untuk setiap warna, θ2, θ3, θ4 juga berbeda
    /// → DISPERSI: setiap warna keluar dengan arah yang berbeda.
    /// Ungu (n terbesar) → θ2 terkecil → θ3 terbesar → θ4 terbesar → D terbesar
    /// Merah (n terkecil) → D terkecil
    /// </summary>
    public static DispersionResult CalculateDispersion(
        SpectrumColor color,
        PrismMaterial material,
        float incidenceAngleDeg,   // θ1: sudut datang cahaya ke prisma
        float apexAngleDeg,        // A: sudut pembias prisma
        Vector3 surfaceNormal1,    // Normal permukaan masuk (world space)
        Vector3 surfaceNormal2,    // Normal permukaan keluar (world space)
        Vector3 incomingDirection  // Arah sinar datang (world space, normalized)
    )
    {
        var result = new DispersionResult();
        result.color = color;
        result.n = material.GetRefractiveIndex(color);
        result.theta1 = incidenceAngleDeg;
        result.totalInternalReflection = false;

        // --- LANGKAH 1: Pembiasan di permukaan pertama (udara → material) ---
        result.theta2 = SnellRefraction(result.theta1, N_AIR, result.n);

        // Jika theta2 NaN, sinar tidak masuk (tidak mungkin untuk udara→kaca,
        // tapi kita handle untuk keamanan)
        if (float.IsNaN(result.theta2))
        {
            result.totalInternalReflection = true;
            result.exitDirection = Vector3.zero;
            return result;
        }

        // --- LANGKAH 2: Geometri dalam prisma → sudut datang permukaan 2 ---
        result.theta3 = SecondIncidenceAngle(apexAngleDeg, result.theta2);

        // --- LANGKAH 3: Pembiasan di permukaan kedua (material → udara) ---
        result.theta4 = SnellRefraction(result.theta3, result.n, N_AIR);

        // Periksa Total Internal Reflection di permukaan kedua
        if (float.IsNaN(result.theta4))
        {
            result.totalInternalReflection = true;
            result.exitDirection = Vector3.zero;
            return result;
        }

        // --- LANGKAH 4: Sudut Deviasi Total ---
        // D = θ1 + θ4 - A
        result.deviationAngle = result.theta1 + result.theta4 - apexAngleDeg;

        // --- LANGKAH 5: Hitung arah keluar dalam world space ---
        result.exitDirection = CalculateExitDirection(
            incomingDirection,
            surfaceNormal2,
            result.theta4
        );

        return result;
    }

    /// <summary>
    /// Menghitung vektor arah keluar sinar berdasarkan sudut bias dan normal permukaan.
    /// Menggunakan rumus refraksi vektor (vector form of Snell's law).
    ///
    /// Formula vektor Snell:
    ///   T = (n1/n2) * I + ((n1/n2)*cosθi - cosθt) * N
    /// di mana I = arah datang, N = normal permukaan, T = arah transmisi
    /// </summary>
    public static Vector3 CalculateExitDirection(
        Vector3 incidentDir,
        Vector3 surfaceNormal,
        float refractionAngleDeg)
    {
        // Pastikan vektor sudah ternormalisasi
        incidentDir = incidentDir.normalized;
        surfaceNormal = surfaceNormal.normalized;

        // Jika normal berlawanan arah dengan sinar datang, balik normal
        // (normal harus menghadap medium asal sinar)
        if (Vector3.Dot(incidentDir, surfaceNormal) > 0)
            surfaceNormal = -surfaceNormal;

        float cosI = -Vector3.Dot(incidentDir, surfaceNormal);
        float cosT = Mathf.Cos(refractionAngleDeg * Mathf.Deg2Rad);

        // Rumus refraksi vektor
        // Rasio n1/n2 sudah "tertanam" di refractionAngleDeg melalui Snell,
        // jadi kita rekonstruksi arah dari sudut output langsung
        float sinT = Mathf.Sin(refractionAngleDeg * Mathf.Deg2Rad);
        float sinI = Mathf.Sqrt(1f - cosI * cosI);

        float ratio = (sinI > 0.0001f) ? (sinT / sinI) : 1f;

        Vector3 tangent = (incidentDir + cosI * surfaceNormal).normalized;
        Vector3 exitDir = sinT * tangent - cosT * surfaceNormal;

        return exitDir.normalized;
    }

    /// <summary>
    /// Menghitung sudut datang (incidence angle) antara sinar dan normal permukaan.
    /// Mengembalikan nilai dalam derajat (0° = tegak lurus, 90° = sejajar permukaan).
    /// </summary>
    public static float CalculateIncidenceAngle(Vector3 rayDirection, Vector3 surfaceNormal)
    {
        // Sudut antara sinar dan NORMAL (bukan permukaan)
        // Kita perlu -rayDirection karena sinar menuju permukaan
        float cosAngle = Vector3.Dot(-rayDirection.normalized, surfaceNormal.normalized);
        cosAngle = Mathf.Clamp(cosAngle, -1f, 1f);
        return Mathf.Acos(cosAngle) * Mathf.Rad2Deg;
    }
}
