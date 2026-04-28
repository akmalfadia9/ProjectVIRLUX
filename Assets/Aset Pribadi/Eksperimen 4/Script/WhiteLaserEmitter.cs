    using UnityEngine;
    using UnityEngine.InputSystem;
    using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Laser putih yang ditembakkan dari Right Hand Controller ketika trigger ditekan.
/// 
/// PERUBAHAN DARI VERSI LAMA:
/// 1. Laser aktif hanya ketika trigger kanan ditekan (InputActionReference)
/// 2. Origin laser mengikuti Transform khusus (LaserOrigin) bukan transform controller langsung
/// 3. Deteksi dispersi terjadi di SEMUA permukaan prisma, bukan hanya satu titik
/// 4. Menggunakan tag "PrismEntry" dan "PrismExit" pada child collider tiap sisi prisma
///    sehingga sistem tahu mana sisi masuk dan mana sisi keluar secara otomatis
/// 
/// CARA KERJA DETEKSI PERMUKAAN BARU:
/// Setiap prisma segitiga memiliki 3 sisi. Masing-masing sisi diberi
/// child GameObject dengan MeshCollider/BoxCollider tipis + Tag khusus:
///   - "PrismEntry" : sisi yang menghadap laser (sisi masuk cahaya)
///   - "PrismExit"  : sisi tempat cahaya keluar setelah dibiaskan
///   - "PrismBase"  : alas prisma (tidak dipakai untuk dispersi)
/// 
/// Ketika raycast mengenai collider ber-tag "PrismEntry", sistem langsung
/// mendapat hitNormal yang akurat di SEMUA titik di seluruh permukaan sisi itu.
/// Tidak perlu Surface Marker satu titik lagi.
/// </summary>
public class WhiteLaserEmitter : MonoBehaviour
    {
        // =====================================================================
        // INSPECTOR FIELDS
        // =====================================================================

        [Header("=== XR INPUT ===")]
        [Tooltip("Assign: XRI RightHand/Select (Trigger) dari Input Actions asset XR Default.\n" +
                 "Path: Assets/XRI/Settings/XRI Default Input Actions > XRI RightHand > Select")]
        public InputActionReference triggerAction;

        [Tooltip("Threshold nilai trigger (0–1) untuk mulai menembak laser. Default 0.1.")]
        [Range(0.01f, 0.9f)]
        public float triggerThreshold = 0.1f;

        [Header("=== LASER ORIGIN ===")]
        [Tooltip("Transform titik asal laser. Buat Empty GameObject child dari controller, " +
                 "posisikan di ujung 'laras' laser pointer, arahkan sumbu Z (forward) ke depan.\n" +
                 "Jika kosong, otomatis pakai transform komponen ini.")]
        public Transform laserOrigin;

        [Header("=== RAY SETTINGS ===")]
        [Tooltip("Panjang maksimum raycast laser (meter)")]
        public float maxRayDistance = 15f;

        [Tooltip("Layer mask. Set ke layer 'Prism' saja agar tidak interference objek lain.")]
        public LayerMask prismLayerMask = ~0;

        [Header("=== LASER VISUAL ===")]
        [Tooltip("LineRenderer untuk sinar laser putih. Attach di GameObject yang sama.")]
        public LineRenderer laserLineRenderer;

        [Tooltip("Lebar sinar laser (meter)")]
        public float laserWidth = 0.005f;

        [Header("=== DEBUG ===")]
        public bool showDebugRay = true;

        // =====================================================================
        // PRIVATE
        // =====================================================================
        
        private bool isLaserOn = false;
        private XRGrabInteractable grabInteractable;
        //private PrismDispersionController _lastHitPrism = null;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================
        void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
        }


        private void Start()
        {
            SetupLineRenderer();
            grabInteractable.activated.AddListener(OnActivated);
            grabInteractable.deactivated.AddListener(OnDeactivated);
            //Debug.Log($"Prism Layer Mask Value: {prismLayerMask.value} | Prism Layer Index: {LayerMask.NameToLayer("Prism")}");


            // Fallback: jika laserOrigin tidak di-assign, pakai transform ini
            if (laserOrigin == null)
            {
                laserOrigin = this.transform;
                Debug.LogWarning("[LaserEmitter] LaserOrigin tidak di-assign. " +
                                 "Menggunakan transform controller langsung.");
            }
        }

        void OnDestroy()
        {
            grabInteractable.activated.RemoveListener(OnActivated);
            grabInteractable.deactivated.RemoveListener(OnDeactivated);
        }

        void OnActivated(ActivateEventArgs args)
        {
            isLaserOn = !isLaserOn;
            if (!isLaserOn) DeactivateLaser();
        }

        void OnDeactivated(DeactivateEventArgs args) { }

        private void Update()
        {
            if (isLaserOn)
            {
                FireLaser();
            }
            else
                DeactivateLaser();
        }

        // =====================================================================
        // LASER LOGIC
        // =====================================================================

        /// <summary>
        /// Tembakkan raycast dari laserOrigin ke arah forward-nya.
        /// 
        /// DETEKSI PERMUKAAN:
        /// Raycast mengenai collider child prisma yang ber-tag "PrismEntry".
        /// hitNormal dari RaycastHit sudah otomatis tegak lurus terhadap
        /// permukaan di titik tumbukan persis — akurat di seluruh permukaan,
        /// tidak perlu Surface Marker lagi.
        /// 
        /// Setelah mendapat hitNormal dan hitPoint, data dikirim ke
        /// PrismDispersionController yang ada di parent collider tersebut.
        /// </summary>
        private void FireLaser()
        {
            Vector3 origin = laserOrigin.position;
            Vector3 direction = laserOrigin.forward;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, maxRayDistance, prismLayerMask))
            {
                // Cari PrismDispersionController di objek yang kena atau parent-nya
                // (karena collider ada di child "sisi" prisma, controller ada di root prisma)
                var prism = hit.collider.GetComponentInParent<PrismDispersionController>();

                if (prism != null)
                {

                    prism.OnRayHit2(
                        hitPoint: hit.point,
                        incomingDirection: direction,
                        hitNormal: hit.normal
                    );

                    UpdateLaserVisual(origin, hit.point);
                }
                else
                {
                    // Kena objek bukan prisma
                    UpdateLaserVisual(origin, hit.point);
                    //DeactivateLastPrism();
                }
            }
            else
            {
                // Tidak kena apapun
                UpdateLaserVisual(origin, origin + direction * maxRayDistance);
                //DeactivateLastPrism();
            }
        }

        private void DeactivateLaser()
        {
            if (laserLineRenderer != null)
                laserLineRenderer.enabled = false;
            //DeactivateLastPrism();
        }

        //private void DeactivateLastPrism()
        //{
        //    // PrismDispersionController.Update() menangani hide otomatis
        //    // jika _isActive tidak di-set true dalam frame ini
        //    _lastHitPrism = null;
        //}

        private void UpdateLaserVisual(Vector3 start, Vector3 end)
        {
            if (laserLineRenderer == null) return;
            laserLineRenderer.enabled = true;
            laserLineRenderer.SetPosition(0, start);
            laserLineRenderer.SetPosition(1, end);
        }

        private void SetupLineRenderer()
        {
            if (laserLineRenderer == null) return;
            laserLineRenderer.positionCount = 2;
            laserLineRenderer.startWidth = laserWidth;
            laserLineRenderer.endWidth = laserWidth;
            laserLineRenderer.startColor = Color.white;
            laserLineRenderer.endColor = Color.white;
            laserLineRenderer.enabled = false;
        }

        //private void OnDrawGizmos()
        //{
        //    if (!showDebugRay || laserOrigin == null) return;
        //    Gizmos.color = Color.white;
        //    Gizmos.DrawRay(laserOrigin.position, laserOrigin.forward * maxRayDistance);
        //}
    }