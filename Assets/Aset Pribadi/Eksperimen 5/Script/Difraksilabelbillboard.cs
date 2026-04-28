using UnityEngine;
using TMPro;

/// <summary>
/// DifraksiLabelBillboard.cs
/// Pasang script ini pada prefab label layar agar selalu menghadap kamera.
/// Juga menganimasikan kemunculan label (fade in).
/// </summary>
public class DifraksiLabelBillboard : MonoBehaviour
{
    [Tooltip("Aktifkan animasi fade-in saat label muncul")]
    public bool fadeIn = true;
    public float durasiMuncul = 0.4f;

    private TextMeshPro tmp;
    private float timer = 0f;
    private bool selesai = false;
    private Camera cam;

    void Awake()
    {
        tmp = GetComponent<TextMeshPro>();
        cam = Camera.main;

        if (fadeIn && tmp != null)
        {
            Color c = tmp.color; c.a = 0f;
            tmp.color = c;
        }
    }

    void Update()
    {
        // Billboard: selalu hadap kamera
        if (cam == null) cam = Camera.main;
        if (cam != null)
        {
            Vector3 dir = transform.position - cam.transform.position;
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(dir);
        }

        // Fade in
        if (fadeIn && !selesai && tmp != null)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / durasiMuncul);
            Color c = tmp.color; c.a = t;
            tmp.color = c;
            if (t >= 1f) selesai = true;
        }
    }
}