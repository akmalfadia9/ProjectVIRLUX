using UnityEngine;

namespace DoubleSlit
{
    /// <summary>
    /// Menggambar sinar laser dari pistol ke TITIK CELAH yang spesifik.
    /// Gun1 → Slit atas (celah 1), Gun2 → Slit bawah (celah 2).
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class DSE_LaserController : MonoBehaviour
    {
        [Header("Identifikasi")]
        [Tooltip("1 = LaserGun1 menuju Slit atas, 2 = LaserGun2 menuju Slit bawah")]
        public int DSE_LaserIndex = 1;

        [Header("Referensi — WAJIB diisi di Inspector")]
        [Tooltip("Drag GameObject DSE_SlitPoint1 (titik celah atas)")]
        public Transform DSE_SlitPoint1;   // Titik celah atas (untuk Gun1)
        [Tooltip("Drag GameObject DSE_SlitPoint2 (titik celah bawah)")]
        public Transform DSE_SlitPoint2;   // Titik celah bawah (untuk Gun2)

        [Header("Visual Laser")]
        public float DSE_BeamWidth = 0.008f;
        [Range(1f, 6f)] public float DSE_GlowIntensity = 4f;
        public DSE_LaserColor DSE_CurrentColor = DSE_LaserColor.Green;

        private LineRenderer DSE_LineRenderer;
        private Material DSE_LaserMaterial;
        private bool DSE_IsActive = false;

        // Properti publik agar DSE_BeamAfterSlit bisa membaca
        public bool DSE_GetIsActive() => DSE_IsActive;
        public DSE_LaserColor DSE_GetCurrentColor() => DSE_CurrentColor;

        /// <summary>Kembalikan titik celah yang dituju laser ini.</summary>
        public Vector3 DSE_GetTargetSlitPosition()
        {
            // Gun1 selalu menuju Slit atas, Gun2 menuju Slit bawah
            if (DSE_LaserIndex == 1)
                return DSE_SlitPoint1 != null ? DSE_SlitPoint1.position : transform.position;
            else
                return DSE_SlitPoint2 != null ? DSE_SlitPoint2.position : transform.position;
        }

        void Awake()
        {
            DSE_LineRenderer = GetComponent<LineRenderer>();
            DSE_BuildMaterial();
            DSE_ConfigureLineRenderer();
        }

        void Start()
        {
            DSE_SetActive(false);
        }

        void Update()
        {
            if (!DSE_IsActive) return;
            DSE_DrawBeam();
        }

        // ─── Private ─────────────────────────────────────────────────

        private void DSE_BuildMaterial()
        {
            // Coba shader emissive, fallback ke sprite
            Shader sh = Shader.Find("Particles/Standard Unlit")
                     ?? Shader.Find("Unlit/Color")
                     ?? Shader.Find("Sprites/Default");
            DSE_LaserMaterial = new Material(sh);
        }

        private void DSE_ConfigureLineRenderer()
        {
            DSE_LineRenderer.material = DSE_LaserMaterial;
            DSE_LineRenderer.positionCount = 2;
            DSE_LineRenderer.startWidth = DSE_BeamWidth;
            DSE_LineRenderer.endWidth = DSE_BeamWidth * 0.6f;
            DSE_LineRenderer.useWorldSpace = true;
            DSE_LineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            DSE_LineRenderer.receiveShadows = false;
            DSE_LineRenderer.numCapVertices = 4;
        }

        private void DSE_DrawBeam()
        {
            Vector3 start = transform.position;
            Vector3 target = DSE_GetTargetSlitPosition();

            DSE_LineRenderer.SetPosition(0, start);
            DSE_LineRenderer.SetPosition(1, target);
        }

        // ─── Public API ───────────────────────────────────────────────

        public void DSE_SetColor(DSE_LaserColor newColor)
        {
            DSE_CurrentColor = newColor;
            DSE_ApplyColorToRenderer();
        }

        private void DSE_ApplyColorToRenderer()
        {
            Color baseColor = DSE_ColorHelper.ToUnityColor(DSE_CurrentColor);
            Color glow = baseColor * DSE_GlowIntensity;
            glow.a = 1f;

            if (DSE_LaserMaterial != null)
            {
                DSE_LaserMaterial.color = glow;
                // Untuk shader yang support emission
                if (DSE_LaserMaterial.HasProperty("_EmissionColor"))
                {
                    DSE_LaserMaterial.SetColor("_EmissionColor", glow);
                    DSE_LaserMaterial.EnableKeyword("_EMISSION");
                }
            }

            DSE_LineRenderer.startColor = glow;
            DSE_LineRenderer.endColor = baseColor;
        }

        public void DSE_SetActive(bool active)
        {
            DSE_IsActive = active;
            DSE_LineRenderer.enabled = active;

            if (active)
            {
                DSE_ApplyColorToRenderer();
                DSE_DrawBeam();
            }
        }

        void OnDestroy()
        {
            if (DSE_LaserMaterial != null) Destroy(DSE_LaserMaterial);
        }
    }
}