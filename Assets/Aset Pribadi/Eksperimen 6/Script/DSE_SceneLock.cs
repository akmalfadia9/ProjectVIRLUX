using UnityEngine;

namespace DoubleSlit
{
    /// <summary>
    /// Mengunci posisi, rotasi, dan skala semua objek scene agar tidak bisa dipindahkan.
    /// Attach ke masing-masing GameObject yang ingin dikunci.
    /// </summary>
    public class DSE_SceneLock : MonoBehaviour
    {
        [Header("Kunci Transform")]
        public bool DSE_LockPosition = true;
        public bool DSE_LockRotation = true;
        public bool DSE_LockScale = false;

        // Simpan transform awal
        private Vector3 DSE_OriginalPosition;
        private Quaternion DSE_OriginalRotation;
        private Vector3 DSE_OriginalScale;

        void Awake()
        {
            DSE_OriginalPosition = transform.position;
            DSE_OriginalRotation = transform.rotation;
            DSE_OriginalScale = transform.localScale;

            // Jika ada Rigidbody, buat kinematic agar tidak terpengaruh fisika
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.constraints = RigidbodyConstraints.FreezeAll;
            }
        }

        void LateUpdate()
        {
            // Paksa kembalikan ke posisi/rotasi/skala semula setiap frame
            if (DSE_LockPosition && transform.position != DSE_OriginalPosition)
                transform.position = DSE_OriginalPosition;

            if (DSE_LockRotation && transform.rotation != DSE_OriginalRotation)
                transform.rotation = DSE_OriginalRotation;

            if (DSE_LockScale && transform.localScale != DSE_OriginalScale)
                transform.localScale = DSE_OriginalScale;
        }

        /// <summary>Panggil ini jika perlu memperbarui posisi kunci secara programatik.</summary>
        public void DSE_UpdateLockTarget()
        {
            DSE_OriginalPosition = transform.position;
            DSE_OriginalRotation = transform.rotation;
            DSE_OriginalScale = transform.localScale;
        }
    }
}