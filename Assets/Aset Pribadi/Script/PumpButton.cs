using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class PumpButton : MonoBehaviour
{
    [Header("Referensi")]
    public VacuumChamberManager chamberManager;
    public Transform buttonVisual;       // drag ButtonVisual ke sini

    [Header("Animasi Tombol")]
    [Tooltip("Seberapa jauh tombol tertekan ke bawah (meter)")]
    public float pressDepth = 0.015f;

    [Tooltip("Kecepatan tombol kembali ke posisi semula")]
    public float returnSpeed = 8f;

    private Vector3 restPosition;        // posisi awal ButtonVisual
    private Vector3 pressedPosition;     // posisi saat tertekan
    private bool isPressed = false;
    private XRSimpleInteractable interactable;

    void Start()
    {
        interactable = GetComponent<XRSimpleInteractable>();

        // Simpan posisi awal ButtonVisual
        restPosition = buttonVisual.localPosition;
        pressedPosition = restPosition - new Vector3(0, pressDepth, 0);

        // Saat tangan VR menyentuh dan "select" tombol
        interactable.selectEntered.AddListener(OnButtonPressed);
    }

    void Update()
    {
        // Tombol kembali ke posisi semula secara smooth
        if (!isPressed)
        {
            buttonVisual.localPosition = Vector3.Lerp(
                buttonVisual.localPosition,
                restPosition,
                returnSpeed * Time.deltaTime
            );
        }
    }

    void OnButtonPressed(SelectEnterEventArgs args)
    {
        // Gerakkan tombol ke bawah
        buttonVisual.localPosition = pressedPosition;

        // Beritahu chamber manager
        chamberManager.TryToggleVacuum();

        // Langsung lepas agar bisa ditekan lagi
        isPressed = false;
    }

    void OnDestroy()
    {
        if (interactable != null)
            interactable.selectEntered.RemoveListener(OnButtonPressed);
    }
}