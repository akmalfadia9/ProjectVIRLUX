#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility untuk membuat preset PrismMaterial langsung dari menu Unity.
/// Jalankan dari menu: VIR-LUX > Create Prism Material Presets
/// </summary>
public static class PrismMaterialPresetCreator
{
    [MenuItem("VIR-LUX/Create All Prism Material Presets")]
    public static void CreateAllPresets()
    {
        CreateCrownGlassPreset();
        CreateWaterPreset();
        CreateDiamondPreset();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[VIR-LUX] Tiga preset PrismMaterial telah dibuat di Assets/VIR-LUX/Materials/");
    }

    /// <summary>
    /// Kaca Crown — material standar optik. Dispersi rendah.
    /// Nilai n diukur pada garis Fraunhofer (standar optik internasional):
    ///   F (486nm biru)  → n = 1.528
    ///   D (589nm kuning)→ n = 1.520
    ///   C (656nm merah) → n = 1.515
    /// Angka-angka ini adalah data empiris dari literatur optik.
    /// </summary>
    private static void CreateCrownGlassPreset()
    {
        var mat = ScriptableObject.CreateInstance<PrismMaterial>();
        mat.materialName = "Kaca Crown";

        // Nilai berdasarkan Sellmeier equation untuk Crown Glass (BK7)
        mat.n_Red    = 1.5143f;  // 700nm
        mat.n_Orange = 1.5168f;  // 620nm
        mat.n_Yellow = 1.5187f;  // 580nm
        mat.n_Green  = 1.5214f;  // 550nm
        mat.n_Blue   = 1.5258f;  // 460nm
        mat.n_Violet = 1.5305f;  // 404nm

        // Koefisien Abbe number: Vd = (nD - 1) / (nF - nC) ≈ 64
        // Angka Abbe tinggi = dispersi rendah = cocok untuk lensa akromatis

        EnsureDirectory();
        AssetDatabase.CreateAsset(mat, "Assets/VIR-LUX/Materials/PrismMaterial_KacaCrown.asset");
        Debug.Log("[VIR-LUX] Preset Kaca Crown dibuat.");
    }

    /// <summary>
    /// Air — material transparan alami. Dispersi sangat rendah.
    /// Pelangi terbentuk karena dispersi air + pemantulan internal tetes hujan.
    /// Nilai n air jauh lebih rendah dari kaca → deviasi lebih kecil.
    /// </summary>
    private static void CreateWaterPreset()
    {
        var mat = ScriptableObject.CreateInstance<PrismMaterial>();
        mat.materialName = "Air";

        // Nilai berdasarkan Cauchy equation untuk air pada suhu 20°C
        mat.n_Red    = 1.3311f;  // 700nm
        mat.n_Orange = 1.3319f;  // 620nm
        mat.n_Yellow = 1.3328f;  // 580nm
        mat.n_Green  = 1.3343f;  // 550nm
        mat.n_Blue   = 1.3374f;  // 460nm
        mat.n_Violet = 1.3428f;  // 404nm

        // Angka Abbe ≈ 55.7 (lebih tinggi dari kaca crown berarti dispersi lebih rendah
        // NAMUN karena n jauh lebih rendah, sudut refraksi juga lebih kecil)

        EnsureDirectory();
        AssetDatabase.CreateAsset(mat, "Assets/VIR-LUX/Materials/PrismMaterial_Air.asset");
        Debug.Log("[VIR-LUX] Preset Air dibuat.");
    }

    /// <summary>
    /// Berlian — material dengan dispersi tertinggi di antara ketiganya.
    /// n yang sangat tinggi (>2.4) menyebabkan:
    ///   1. Sudut kritis TIR sangat kecil (~24°) → kilap berlian
    ///   2. Dispersi sangat tinggi → api (fire) berlian yang spektakuler
    /// Nilai Abbe berlian ≈ 55, NAMUN perbedaan absolut nF - nC jauh lebih besar.
    /// </summary>
    private static void CreateDiamondPreset()
    {
        var mat = ScriptableObject.CreateInstance<PrismMaterial>();
        mat.materialName = "Berlian";

        // Nilai berdasarkan data empiris berlian tipe IIa
        mat.n_Red    = 2.4071f;  // 700nm
        mat.n_Orange = 2.4153f;  // 620nm
        mat.n_Yellow = 2.4270f;  // 580nm  ← nilai n_D berlian yang terkenal
        mat.n_Green  = 2.4354f;  // 550nm
        mat.n_Blue   = 2.4499f;  // 460nm
        mat.n_Violet = 2.4651f;  // 404nm

        // Dengan n setinggi ini, sudut datang harus sangat kecil (<24°)
        // agar tidak terjadi TIR saat keluar. Ini sangat penting untuk setup demo!

        EnsureDirectory();
        AssetDatabase.CreateAsset(mat, "Assets/VIR-LUX/Materials/PrismMaterial_Berlian.asset");
        Debug.Log("[VIR-LUX] Preset Berlian dibuat.");
    }

    private static void EnsureDirectory()
    {
        if (!AssetDatabase.IsValidFolder("Assets/VIR-LUX"))
            AssetDatabase.CreateFolder("Assets", "VIR-LUX");
        if (!AssetDatabase.IsValidFolder("Assets/VIR-LUX/Materials"))
            AssetDatabase.CreateFolder("Assets/VIR-LUX", "Materials");
    }
}
#endif
