using UnityEngine;

/// <summary>
/// ReflectionMirrorSurface - Penanda bahwa objek ini adalah cermin datar.
/// Pasang script ini pada GameObject cermin (panel kanan maupun panel kiri).
/// Pastikan layer GameObject ini sesuai dengan "Mirror Layer" di ReflectionLaserGun.
/// 
/// Cara penggunaan:
/// 1. Pasang script ini pada cermin.
/// 2. Set layer GameObject cermin ke layer khusus (misalnya "Mirror").
/// 3. Di ReflectionLaserGun Inspector, set "Mirror Layer" ke layer "Mirror".
/// 4. Set "Reflectable Layer" ke layer "Mirror" + layer lain yang ingin terkena laser.
/// </summary>
public class ReflectionMirrorSurface : MonoBehaviour
{
    [Header("Mirror Visual Feedback")]
    [Tooltip("Aktifkan efek highlight ketika laser menyentuh cermin")]
    public bool showHitEffect = true;

    [Tooltip("Warna highlight ketika laser mengenai cermin")]
    public Color hitHighlightColor = new Color(0.8f, 0.9f, 1f, 0.5f);

    private Renderer mirrorRenderer;
    private Color originalColor;
    private bool isHit = false;
    private float hitTimer = 0f;

    void Awake()
    {
        mirrorRenderer = GetComponent<Renderer>();
        if (mirrorRenderer != null)
        {
            originalColor = mirrorRenderer.material.color;
        }
    }

    void Update()
    {
        if (isHit)
        {
            hitTimer -= Time.deltaTime;
            if (hitTimer <= 0f)
            {
                isHit = false;
                if (mirrorRenderer != null && showHitEffect)
                {
                    mirrorRenderer.material.color = originalColor;
                }
            }
        }
    }

    /// <summary>
    /// Dipanggil dari LaserGun ketika laser mengenai cermin ini.
    /// </summary>
    public void OnLaserHit()
    {
        isHit = true;
        hitTimer = 0.1f; // Reset timer setiap frame laser menyentuh

        if (mirrorRenderer != null && showHitEffect)
        {
            mirrorRenderer.material.color = hitHighlightColor;
        }
    }

    // Gizmo dihapus — garis normal ditampilkan oleh ReflectionLaserGun
    // hanya saat sinar laser benar-benar menyentuh cermin.
}