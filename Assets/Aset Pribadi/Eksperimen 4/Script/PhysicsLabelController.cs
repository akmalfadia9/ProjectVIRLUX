using UnityEngine;
using TMPro;

/// <summary>
/// Script untuk prefab label TMP World Space.
/// Di-attach pada root GameObject prefab label.
/// Mengatur skala, billboard behavior, dan styling teks.
///
/// CARA MEMBUAT PREFAB INI DI UNITY EDITOR:
/// 1. Buat empty GameObject, beri nama "PhysicsLabel"
/// 2. Tambah child: GameObject > 3D Object > Text - TextMeshPro
///    (pilih World Space canvas atau langsung TextMeshPro 3D)
/// 3. Attach script PhysicsLabelController ini ke "PhysicsLabel"
/// 4. Simpan sebagai Prefab ke Assets/VIR-LUX/Prefabs/
/// </summary>
public class PhysicsLabelController : MonoBehaviour
{
    [Header("=== REFERENSI ===")]
    public TextMeshPro labelTMP;

    [Header("=== TAMPILAN ===")]
    [Tooltip("Ukuran font label")]
    public float fontSize = 0.12f;

    [Tooltip("Lebar maksimum kotak teks (meter)")]
    public float textWidth = 0.6f;

    [Tooltip("Selalu hadapkan label ke kamera utama (billboard)")]
    public bool billboardMode = true;

    [Tooltip("Background panel untuk label (opsional)")]
    public GameObject backgroundPanel;

    [Tooltip("Warna background panel")]
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.75f);

    // =====================================================================

    private void Awake()
    {
        if (labelTMP == null)
            labelTMP = GetComponentInChildren<TextMeshPro>();

        ConfigureLabel();
    }

    private void LateUpdate()
    {
        // Billboard: putar label agar selalu menghadap kamera
        if (billboardMode && Camera.main != null)
        {
            transform.forward = Camera.main.transform.forward;
        }
    }

    private void ConfigureLabel()
    {
        if (labelTMP == null) return;

        labelTMP.fontSize           = fontSize;
        labelTMP.rectTransform.sizeDelta = new Vector2(textWidth, 0.5f);
        labelTMP.alignment          = TextAlignmentOptions.TopLeft;
        labelTMP.richText           = true;
        labelTMP.enableWordWrapping = false;
        labelTMP.overflowMode       = TextOverflowModes.Overflow;

        // Warna default putih dengan shadow hitam untuk keterbacaan di VR
        labelTMP.color = Color.white;
        labelTMP.fontStyle = FontStyles.Normal;
    }

    /// <summary>
    /// Set teks label dari luar (dipanggil oleh PrismDispersionController).
    /// </summary>
    public void SetText(string text)
    {
        if (labelTMP != null)
            labelTMP.text = text;
    }
}
