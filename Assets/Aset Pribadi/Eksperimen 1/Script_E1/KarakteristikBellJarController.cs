using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// KarakteristikBellJarController
/// Mengontrol Bell Jar: animasi angkat/turun, dan memastikan isi jar (lampu + alarm)
/// tetap di posisi fixed di dalam tabung dan tidak bisa dipindahkan.
/// </summary>
public class KarakteristikBellJarController : MonoBehaviour
{
    [Header("=== Referensi Objek Bell Jar ===")]
    [Tooltip("Transform Bell Jar utama (tabung kaca)")]
    public Transform bellJarTransform;

    [Tooltip("Objek lampu pijar (harus child dari Bell Jar atau fixed position)")]
    public Transform lampuPijarTransform;

    [Tooltip("Objek alarm (harus child dari Bell Jar atau fixed position)")]
    public Transform alarmTransform;

    [Tooltip("XR Interactable handle untuk grab jar (opsional, untuk VR angkat manual)")]
    public XRSimpleInteractable jarHandle;

    [Header("=== Posisi Jar ===")]
    [Tooltip("Posisi Y awal jar (di atas base/platform)")]
    public float jarRestPositionY   = 0f;

    [Tooltip("Posisi Y saat jar diangkat")]
    public float jarLiftedPositionY = 0.4f;

    [Tooltip("Kecepatan animasi angkat jar")]
    public float liftSpeed = 1.2f;

    [Header("=== Konfigurasi Interior ===")]
    [Tooltip("Apakah jar sedang terangkat")]
    [SerializeField, ReadOnly] private bool isLifted = false;

    // Posisi fixed isi jar (disimpan saat Start, tidak bisa berubah)
    private Vector3 lampuFixedLocalPosition;
    private Vector3 alarmFixedLocalPosition;

    private Vector3 jarTargetPosition;
    private bool    isAnimating = false;

    // ─────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        // Simpan posisi lokal awal - ini akan jadi posisi permanen
        if (lampuPijarTransform != null)
            lampuFixedLocalPosition = lampuPijarTransform.localPosition;

        if (alarmTransform != null)
            alarmFixedLocalPosition = alarmTransform.localPosition;

        // Set posisi awal jar
        if (bellJarTransform != null)
        {
            Vector3 pos = bellJarTransform.position;
            pos.y = jarRestPositionY;
            bellJarTransform.position = pos;
            jarTargetPosition = bellJarTransform.position;
        }

        // Daftarkan XR event untuk handle grab (opsional)
        if (jarHandle != null)
            jarHandle.selectEntered.AddListener(OnJarHandleGrabbed);
    }

    private void OnDestroy()
    {
        if (jarHandle != null)
            jarHandle.selectEntered.RemoveListener(OnJarHandleGrabbed);
    }

    private void Update()
    {
        AnimateJar();
        EnforceInteriorPositions();
    }

    // ─── Animasi ──────────────────────────────────────────────────────────────
    private void AnimateJar()
    {
        if (!isAnimating || bellJarTransform == null) return;

        bellJarTransform.position = Vector3.MoveTowards(
            bellJarTransform.position,
            jarTargetPosition,
            liftSpeed * Time.deltaTime
        );

        if (Vector3.Distance(bellJarTransform.position, jarTargetPosition) < 0.001f)
            isAnimating = false;
    }

    // ─── Paksa posisi isi jar agar tidak berubah ──────────────────────────────
    /// <summary>
    /// Lampu dan alarm SELALU di posisi fixed.
    /// Tidak bisa digeser oleh siapapun termasuk physics.
    /// </summary>
    private void EnforceInteriorPositions()
    {
        if (lampuPijarTransform != null)
            lampuPijarTransform.localPosition = lampuFixedLocalPosition;

        if (alarmTransform != null)
            alarmTransform.localPosition = alarmFixedLocalPosition;
    }

    // ─── Public Controls ──────────────────────────────────────────────────────
    public void LiftJar()
    {
        if (bellJarTransform == null) return;
        isLifted = true;
        jarTargetPosition = new Vector3(
            bellJarTransform.position.x,
            jarLiftedPositionY,
            bellJarTransform.position.z
        );
        isAnimating = true;
    }

    public void LowerJar()
    {
        if (bellJarTransform == null) return;
        isLifted = false;
        jarTargetPosition = new Vector3(
            bellJarTransform.position.x,
            jarRestPositionY,
            bellJarTransform.position.z
        );
        isAnimating = true;
    }

    public void ToggleLift()
    {
        if (isLifted) LowerJar();
        else          LiftJar();
    }

    // XR grab pada handle = toggle lift
    private void OnJarHandleGrabbed(SelectEnterEventArgs args) => ToggleLift();

    public bool IsLifted => isLifted;
}
