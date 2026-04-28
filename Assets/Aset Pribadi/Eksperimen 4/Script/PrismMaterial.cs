using UnityEngine;

/// <summary>
/// ScriptableObject yang menyimpan properti optis sebuah prisma.
/// Dibuat sebagai asset terpisah agar mudah diganti-ganti di Inspector
/// tanpa perlu mengubah kode. Setiap prisma di scene dapat memiliki
/// PrismMaterial yang berbeda.
/// </summary>
[CreateAssetMenu(fileName = "NewPrismMaterial", menuName = "VIR-LUX/Prism Material")]
public class PrismMaterial : ScriptableObject
{
    [Header("Identitas Material")]
    public string materialName = "Kaca Crown";

    [Header("Indeks Bias (n) per Panjang Gelombang")]
    [Tooltip("Indeks bias untuk cahaya merah (~700nm)")]
    public float n_Red = 1.515f;

    [Tooltip("Indeks bias untuk cahaya jingga (~620nm)")]
    public float n_Orange = 1.518f;

    [Tooltip("Indeks bias untuk cahaya kuning (~580nm)")]
    public float n_Yellow = 1.520f;

    [Tooltip("Indeks bias untuk cahaya hijau (~550nm)")]
    public float n_Green = 1.523f;

    [Tooltip("Indeks bias untuk cahaya biru (~460nm)")]
    public float n_Blue = 1.528f;

    [Tooltip("Indeks bias untuk cahaya ungu (~400nm)")]
    public float n_Violet = 1.532f;

    // -------------------------------------------------------
    // CATATAN FISIKA: Dispersi Cahaya
    // Setiap material memiliki indeks bias (n) yang berbeda
    // untuk setiap panjang gelombang cahaya. Fenomena ini disebut
    // DISPERSI. Semakin pendek panjang gelombang (menuju ungu),
    // semakin besar nilai n, sehingga cahaya tersebut dibelokkan
    // lebih tajam. Ini adalah alasan mengapa prisma mengurai
    // cahaya putih menjadi spektrum pelangi.
    //
    // Nilai-nilai di bawah menggunakan model Cauchy yang
    // disederhanakan untuk tiga material studi kasus:
    //
    // KACA CROWN (Crown Glass):
    //   n_Red=1.515, n_Yellow=1.520, n_Violet=1.532
    //   Dispersi rendah, digunakan di lensa kamera & kacamata.
    //
    // AIR (Water):
    //   n_Red=1.331, n_Yellow=1.333, n_Violet=1.343
    //   Dispersi sangat rendah, namun tetap menghasilkan pelangi.
    //
    // BERLIAN (Diamond):
    //   n_Red=2.407, n_Yellow=2.426, n_Violet=2.465
    //   Dispersi sangat tinggi = kilap spektral yang luar biasa.
    // -------------------------------------------------------

    /// <summary>
    /// Mengambil indeks bias berdasarkan warna spektrum.
    /// </summary>
    public float GetRefractiveIndex(SpectrumColor color)
    {
        return color switch
        {
            SpectrumColor.Red    => n_Red,
            SpectrumColor.Orange => n_Orange,
            SpectrumColor.Yellow => n_Yellow,
            SpectrumColor.Green  => n_Green,
            SpectrumColor.Blue   => n_Blue,
            SpectrumColor.Violet => n_Violet,
            _ => n_Yellow // default ke tengah spektrum
        };
    }

    /// <summary>
    /// Indeks bias rata-rata (representasi cahaya kuning/sodium D-line).
    /// </summary>
    public float AverageN => n_Yellow;
}

/// <summary>
/// Enum untuk 6 warna spektrum yang digunakan dalam simulasi.
/// </summary>
public enum SpectrumColor
{
    Red,
    Orange,
    Yellow,
    Green,
    Blue,
    Violet
}
