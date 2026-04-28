using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ReflectionAngleLabelManager - Menampilkan label sudut datang dan pantul secara real-time.
/// Pasang script ini pada GameObject kosong di scene, lalu assign ke ReflectionLaserGun.
///
/// Cara penggunaan:
/// 1. Buat GameObject kosong bernama "ReflectionAngleLabelManager".
/// 2. Pasang script ini.
/// 3. Di ReflectionLaserGun.cs, tambahkan referensi ke ReflectionAngleLabelManager (opsional, bisa standalone).
/// </summary>
public class ReflectionAngleLabelManager : MonoBehaviour
{
    [Header("Label Settings")]
    public Font labelFont;
    public int fontSize = 14;
    public Color incidentLabelColor = Color.yellow;
    public Color reflectedLabelColor = Color.cyan;
    public Color normalLabelColor = Color.white;

    private List<GameObject> activeLabels = new List<GameObject>();
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindAnyObjectByType<Camera>();
        }
    }

    public void ClearLabels()
    {
        foreach (var label in activeLabels)
        {
            if (label != null) Destroy(label);
        }
        activeLabels.Clear();
    }

    public void ShowAngleLabels(Vector3 hitPoint, Vector3 normal,
                                 Vector3 incidentDir, Vector3 reflectedDir,
                                 float arcRadius)
    {
        // Hitung sudut
        float incidentAngle = Vector3.Angle(incidentDir, normal);
        float reflectedAngle = Vector3.Angle(reflectedDir, normal);

        // Posisi label busur datang (tengah busur antara normal dan incidentDir)
        Vector3 incidentMid = (normal.normalized + incidentDir.normalized).normalized;
        Vector3 incidentLabelPos = hitPoint + incidentMid * (arcRadius * 1.5f);

        // Posisi label busur pantul (tengah busur antara normal dan reflectedDir)
        Vector3 reflectedMid = (normal.normalized + reflectedDir.normalized).normalized;
        Vector3 reflectedLabelPos = hitPoint + reflectedMid * (arcRadius * 1.5f);

        CreateWorldLabel(incidentLabelPos, $"θi = {incidentAngle:F1}°", incidentLabelColor);
        CreateWorldLabel(reflectedLabelPos, $"θr = {reflectedAngle:F1}°", reflectedLabelColor);
        CreateWorldLabel(hitPoint + normal * (arcRadius * 2f), "N", normalLabelColor);
    }

    void CreateWorldLabel(Vector3 worldPos, string text, Color color)
    {
        GameObject labelGo = new GameObject("AngleLabel_" + text);
        labelGo.transform.position = worldPos;

        TextMesh tm = labelGo.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = fontSize;
        tm.color = color;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.characterSize = 0.01f;

        if (labelFont != null) tm.font = labelFont;

        // Renderer agar selalu menghadap kamera
        labelGo.AddComponent<ReflectionBillboardLabel>();

        activeLabels.Add(labelGo);
    }
}

/// <summary>
/// ReflectionBillboardLabel - Membuat label selalu menghadap kamera (billboard effect).
/// </summary>
public class ReflectionBillboardLabel : MonoBehaviour
{
    private Camera targetCamera;

    void Start()
    {
        targetCamera = Camera.main;
        if (targetCamera == null) targetCamera = FindAnyObjectByType<Camera>();
    }

    void LateUpdate()
    {
        if (targetCamera != null)
        {
            transform.LookAt(transform.position + targetCamera.transform.rotation * Vector3.forward,
                             targetCamera.transform.rotation * Vector3.up);
        }
    }
}