using UnityEngine;

/// <summary>
/// Mengangkat Lid Chamber beserta semua objek di atasnya seperti engsel (hinge).
/// TANPA perlu mengubah hierarchy / unpack prefab.
/// 
/// Cara pakai:
/// 1. Buat GameObject kosong baru, beri nama "LidController"
/// 2. Pasang script ini ke "LidController"
/// 3. Di Inspector, isi semua objek yang ingin ikut terangkat ke list "Objects To Move"
/// 4. Atur Hinge Point dan Hinge Axis sesuai posisi engsel
/// </summary>
public class LidChamberNoParent : MonoBehaviour
{
    [Header("Objek yang Ikut Terangkat")]
    [Tooltip("Masukkan semua objek yang harus ikut terangkat: Lid Chamber, Part Kran, Selang, dll")]
    public Transform[] objectsToMove;

    [Header("Titik & Sumbu Engsel")]
    [Tooltip("Drag objek yang jadi titik engsel (bisa buat Empty GameObject di posisi engsel)")]
    public Transform hingePoint;

    [Tooltip("Sumbu rotasi engsel. X = buka depan-belakang, Z = buka kiri-kanan")]
    public Vector3 hingeAxis = Vector3.right;

    [Header("Pengaturan Animasi")]
    [Tooltip("Sudut maksimal terbuka (derajat)")]
    [Range(10f, 180f)]
    public float maxOpenAngle = 90f;

    [Tooltip("Kecepatan buka/tutup")]
    public float speed = 60f;

    [Tooltip("Tombol keyboard untuk toggle buka/tutup")]
    public KeyCode toggleKey = KeyCode.Space;

    // State internal
    private bool isOpen = false;
    private float currentAngle = 0f;
    private float targetAngle = 0f;

    // Simpan posisi & rotasi awal semua objek
    private Vector3[] initialPositions;
    private Quaternion[] initialRotations;

    void Start()
    {
        // Simpan posisi & rotasi awal semua objek
        initialPositions = new Vector3[objectsToMove.Length];
        initialRotations = new Quaternion[objectsToMove.Length];

        for (int i = 0; i < objectsToMove.Length; i++)
        {
            if (objectsToMove[i] != null)
            {
                initialPositions[i] = objectsToMove[i].position;
                initialRotations[i] = objectsToMove[i].rotation;
            }
        }
    }

    void Update()
    {
        // Toggle dengan keyboard
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleLid();
        }

        // Animasi halus menuju target angle
        if (!Mathf.Approximately(currentAngle, targetAngle))
        {
            currentAngle = Mathf.MoveTowards(currentAngle, targetAngle, speed * Time.deltaTime);
            ApplyRotationToAll(currentAngle);
        }
    }

    /// <summary>
    /// Toggle buka/tutup - bisa dipanggil dari UI Button
    /// </summary>
    public void ToggleLid()
    {
        isOpen = !isOpen;
        targetAngle = isOpen ? maxOpenAngle : 0f;
    }

    /// <summary>
    /// Buka lid - bisa dipanggil dari script lain
    /// </summary>
    public void OpenLid()
    {
        isOpen = true;
        targetAngle = maxOpenAngle;
    }

    /// <summary>
    /// Tutup lid - bisa dipanggil dari script lain
    /// </summary>
    public void CloseLid()
    {
        isOpen = false;
        targetAngle = 0f;
    }

    /// <summary>
    /// Terapkan rotasi engsel ke semua objek berdasarkan sudut
    /// </summary>
    private void ApplyRotationToAll(float angle)
    {
        if (hingePoint == null)
        {
            Debug.LogWarning("LidChamberNoParent: Hinge Point belum diisi di Inspector!");
            return;
        }

        Vector3 pivot = hingePoint.position;
        // Gunakan world axis langsung (bisa pakai hingePoint.right / up / forward jika perlu lokal)
        Vector3 axis = hingePoint.TransformDirection(hingeAxis).normalized;

        Quaternion deltaRotation = Quaternion.AngleAxis(angle, axis);

        for (int i = 0; i < objectsToMove.Length; i++)
        {
            if (objectsToMove[i] == null) continue;

            // Hitung posisi baru berdasarkan rotasi terhadap pivot
            Vector3 offset = initialPositions[i] - pivot;
            Vector3 rotatedOffset = deltaRotation * offset;

            objectsToMove[i].position = pivot + rotatedOffset;
            objectsToMove[i].rotation = deltaRotation * initialRotations[i];
        }
    }

    // Tampilkan gizmo engsel di Scene View
    void OnDrawGizmosSelected()
    {
        if (hingePoint == null) return;

        // Titik engsel
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(hingePoint.position, 0.05f);

        // Sumbu engsel
        Gizmos.color = Color.cyan;
        Vector3 axis = hingePoint.TransformDirection(hingeAxis).normalized;
        Gizmos.DrawRay(hingePoint.position, axis * 0.4f);
        Gizmos.DrawRay(hingePoint.position, -axis * 0.4f);
    }
}