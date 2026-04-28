using UnityEngine;

public class VacuumChamberManager : MonoBehaviour
{
    [Header("State (read only)")]
    [SerializeField] private bool isClosed = false;
    [SerializeField] private bool isVacuumOn = false;
    [SerializeField, Range(0f, 1f)] private float airPressure = 1f;

    [Header("Settings")]
    [Tooltip("Seberapa cepat udara tersedot. Nilai kecil = lebih lambat")]
    public float vacuumSpeed = 0.5f;

    [Tooltip("Seberapa cepat udara kembali normal saat vakum mati")]
    public float refillSpeed = 0.8f;

    // Script lain subscribe ke event ini untuk dapat nilai airPressure
    public event System.Action<float> OnPressureChanged;

    // Properti read-only untuk script lain
    public bool IsClosed => isClosed;
    public bool IsVacuumOn => isVacuumOn;
    public float AirPressure => airPressure;

    void Update()
    {
        float target = isVacuumOn ? 0f : 1f;
        float speed = isVacuumOn ? vacuumSpeed : refillSpeed;

        float newPressure = Mathf.MoveTowards(airPressure, target, speed * Time.deltaTime);

        if (newPressure != airPressure)
        {
            airPressure = newPressure;
            OnPressureChanged?.Invoke(airPressure);
        }
    }

    // Dipanggil oleh LidController setiap frame
    public void SetLidClosed(bool closed)
    {
        if (isClosed == closed) return; // tidak ada perubahan, skip

        isClosed = closed;

        if (!isClosed)
        {
            // Tutup terbuka — pompa mati otomatis
            TurnOffVacuum();
        }
    }

    // Dipanggil oleh PumpButton (XRSimpleInteractable → selectEntered)
    public void TryToggleVacuum()
    {
        if (!isClosed)
        {
            Debug.Log("Chamber belum tertutup, pompa tidak bisa dinyalakan.");
            return;
        }

        isVacuumOn = !isVacuumOn;
        Debug.Log($"Vakum: {(isVacuumOn ? "ON" : "OFF")}");
    }

    private void TurnOffVacuum()
    {
        isVacuumOn = false;
    }
}