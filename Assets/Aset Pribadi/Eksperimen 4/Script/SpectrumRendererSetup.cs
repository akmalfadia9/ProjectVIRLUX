using UnityEngine;

/// <summary>
/// Helper yang secara otomatis membuat dan mengkonfigurasi 6 LineRenderer
/// untuk spektrum warna. Attach script ini ke PrismGameObject yang sama,
/// lalu klik tombol "Auto-Generate Spectrum Renderers" di Inspector
/// (atau jalankan dari kode).
///
/// Dengan cara ini, Anda tidak perlu membuat 6 LineRenderer secara manual.
/// </summary>
[RequireComponent(typeof(PrismDispersionController))]
public class SpectrumRendererSetup : MonoBehaviour
{
    [Header("=== MATERIAL UNTUK LINE RENDERER ===")]
    [Tooltip("Material untuk LineRenderer. Gunakan Unlit/Color atau custom shader emissive.")]
    public Material lineRendererMaterial;

    [Tooltip("Lebar sinar spektrum dalam meter")]
    public float rayWidth = 0.004f;

    // Nama dan warna tiap slot spektrum
    private static readonly string[] ColorNames = { "Red", "Orange", "Yellow", "Green", "Blue", "Violet" };
    private static readonly Color[] ColorValues = new Color[]
    {
        new Color(1.0f, 0.1f, 0.1f),
        new Color(1.0f, 0.5f, 0.0f),
        new Color(1.0f, 0.95f, 0.0f),
        new Color(0.1f, 0.9f, 0.1f),
        new Color(0.2f, 0.4f, 1.0f),
        new Color(0.7f, 0.0f, 1.0f),
    };

    /// <summary>
    /// Membuat child GameObject dengan LineRenderer untuk setiap warna spektrum,
    /// lalu otomatis assign ke array spectrumRenderers di PrismDispersionController.
    ///
    /// CARA PAKAI: Panggil dari Editor button atau dari Start() jika belum ada.
    /// </summary>
    [ContextMenu("Auto-Generate 6 Spectrum LineRenderers")]
    public void GenerateSpectrumRenderers()
    {
        var controller = GetComponent<PrismDispersionController>();
        if (controller == null)
        {
            Debug.LogError("[SpectrumSetup] PrismDispersionController tidak ditemukan!");
            return;
        }

        // Hapus LineRenderer lama yang mungkin sudah ada
        CleanupOldRenderers();

        var renderers = new LineRenderer[6];

        for (int i = 0; i < 6; i++)
        {
            // Buat child GameObject untuk setiap warna
            var go = new GameObject($"Spectrum_{ColorNames[i]}");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;

            var lr = go.AddComponent<LineRenderer>();

            // Konfigurasi LineRenderer
            lr.positionCount   = 2;
            lr.startWidth      = rayWidth;
            lr.endWidth        = rayWidth * 0.5f;  // Sedikit meruncing di ujung
            lr.startColor      = Color.white;
            lr.endColor        = ColorValues[i];
            lr.useWorldSpace   = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows  = false;
            lr.enabled         = false; // Mulai tersembunyi

            // Assign material jika ada, atau buat default
            if (lineRendererMaterial != null)
                lr.material = lineRendererMaterial;
            else
            {
                // Fallback: gunakan Standard dengan emission
                var mat = new Material(Shader.Find("Unlit/Color"))
                {
                    color = ColorValues[i]
                };
                lr.material = mat;
            }

            renderers[i] = lr;
        }

        // Assign ke controller
        controller.spectrumRenderers = renderers;

        Debug.Log($"[SpectrumSetup] 6 LineRenderer spektrum berhasil dibuat untuk {name}.");

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(controller);
#endif
    }

    private void CleanupOldRenderers()
    {
        // Hapus child dengan nama "Spectrum_*"
        var children = new System.Collections.Generic.List<Transform>();
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Spectrum_"))
                children.Add(child);
        }
        foreach (var child in children)
            DestroyImmediate(child.gameObject);
    }
}
