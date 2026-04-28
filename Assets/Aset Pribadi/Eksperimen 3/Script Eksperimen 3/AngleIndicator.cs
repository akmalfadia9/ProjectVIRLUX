using UnityEngine;
using TMPro;

public class AngleIndicator : MonoBehaviour
{
    [Header("Line Renderers")]
    public LineRenderer normalLine;
    public LineRenderer incidentArcRenderer;
    public LineRenderer refractedArcRenderer;

    [Header("Labels")]
    public TextMeshPro incidentLabel;
    public TextMeshPro refractedLabel;
    public TextMeshPro mediumLabel;

    [Header("Appearance")]
    public Color normalColor = Color.white;
    public Color incidentColor = new Color(1f, 0.92f, 0f);
    public Color refractedColor = new Color(0f, 1f, 0.75f);
    public float normalLength = 0.45f;
    public float arcRadius = 0.22f;
    public int arcSegments = 36;
    public float lineWidth = 0.006f;

    [Header("Label Size")]
    public float fontSizeAngle = 0.18f;
    public float fontSizeMedium = 0.22f;

    private Camera _cam;

    void Awake()
    {
        SetupLine(normalLine, normalColor);
        SetupLine(incidentArcRenderer, incidentColor);
        SetupLine(refractedArcRenderer, refractedColor);
        SetupLabel(incidentLabel, incidentColor, fontSizeAngle);
        SetupLabel(refractedLabel, refractedColor, fontSizeAngle);
        SetupLabel(mediumLabel, Color.white, fontSizeMedium);
    }

    void SetupLine(LineRenderer lr, Color c)
    {
        if (!lr) return;
        lr.useWorldSpace = true;
        lr.startColor = c;
        lr.endColor = c;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.material = new Material(Shader.Find("Unlit/Color")) { color = c };
    }

    void SetupLabel(TextMeshPro tmp, Color c, float size)
    {
        if (!tmp) return;
        tmp.fontSize = size;
        tmp.color = c;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
    }

    public void Display(Vector3 position, Vector3 normal,
                        Vector3 incidentDir, Vector3 refractedDir,
                        float theta1, float theta2,
                        string blockName,
                        int incidentIndex, int refractedIndex)
    {
        transform.position = position;
        if (!_cam) _cam = Camera.main;

        Quaternion bill = _cam
            ? Quaternion.LookRotation(_cam.transform.forward)
            : Quaternion.identity;

        // ── Garis Normal ──────────────────────────────────────────────────────
        if (normalLine)
        {
            normalLine.positionCount = 2;
            normalLine.SetPosition(0, position - normal * normalLength * 0.5f);
            normalLine.SetPosition(1, position + normal * normalLength);
        }

        // ── Busur sudut datang ────────────────────────────────────────────────
        if (incidentArcRenderer && theta1 > 0.5f)
            DrawArc(incidentArcRenderer, position, -incidentDir, normal, theta1, arcRadius);

        // ── Busur sudut bias ──────────────────────────────────────────────────
        if (refractedArcRenderer && theta2 > 0.5f)
            DrawArc(refractedArcRenderer, position, refractedDir, -normal, theta2, arcRadius * 0.75f);

        // ── Label sudut datang ────────────────────────────────────────────────
        if (incidentLabel)
        {
            Vector3 mid = ArcMidDir(-incidentDir, normal, theta1);
            incidentLabel.transform.position = position + mid * (arcRadius + 0.02f);
            incidentLabel.transform.rotation = bill;
            incidentLabel.fontSize = fontSizeAngle;
            // Pakai teks biasa tanpa unicode subscript
            incidentLabel.text = "\u03b8" + incidentIndex + " = " + theta1.ToString("F1") + "\u00b0";
        }

        // ── Label sudut bias ──────────────────────────────────────────────────
        if (refractedLabel)
        {
            Vector3 mid = ArcMidDir(refractedDir, -normal, theta2);
            refractedLabel.transform.position = position + mid * (arcRadius * 0.75f + 0.02f);
            refractedLabel.transform.rotation = bill;
            refractedLabel.fontSize = fontSizeAngle;
            refractedLabel.text = "\u03b8" + refractedIndex + " = " + theta2.ToString("F1") + "\u00b0";
        }

        // ── Label medium — di SAMPING garis normal, bukan di tengah ──────────
        if (mediumLabel)
        {
            // Cari arah tegak lurus dari normal (ke samping)
            Vector3 sideDir = Vector3.Cross(normal, _cam ? _cam.transform.forward : Vector3.forward).normalized;
            if (sideDir.sqrMagnitude < 0.01f)
                sideDir = Vector3.Cross(normal, Vector3.up).normalized;

            // Posisi: di ujung atas garis normal, geser ke samping
            Vector3 labelPos = position + normal * normalLength * 1.1f + sideDir * 0.12f;
            mediumLabel.transform.position = labelPos;
            mediumLabel.transform.rotation = bill;
            mediumLabel.fontSize = fontSizeMedium;
            mediumLabel.text = blockName;
        }
    }

    static Vector3 ArcMidDir(Vector3 from, Vector3 to, float angleDeg)
    {
        Vector3 u = from.normalized;
        Vector3 cross = Vector3.Cross(from, to);
        if (cross.sqrMagnitude < 1e-4f)
        {
            cross = Vector3.Cross(from, Vector3.up);
            if (cross.sqrMagnitude < 1e-4f)
                cross = Vector3.Cross(from, Vector3.right);
        }
        Vector3 v = Vector3.Cross(cross.normalized, u).normalized;
        if (Vector3.Dot(v, to) < 0f) v = -v;
        float half = angleDeg * 0.5f * Mathf.Deg2Rad;
        return (Mathf.Cos(half) * u + Mathf.Sin(half) * v).normalized;
    }

    bool DrawArc(LineRenderer lr, Vector3 centre,
                 Vector3 from, Vector3 to, float angleDeg, float radius)
    {
        if (angleDeg < 0.1f) { lr.positionCount = 0; return false; }

        Vector3 u = from.normalized;

        // Cari vektor tegak lurus u yang stabil di semua kasus
        Vector3 perp = Vector3.Cross(u, to.normalized);
        if (perp.sqrMagnitude < 1e-4f)
        {
            // to dan from sejajar — pakai axis dunia sebagai fallback
            perp = Vector3.Cross(u, Vector3.up);
            if (perp.sqrMagnitude < 1e-4f)
                perp = Vector3.Cross(u, Vector3.right);
            if (perp.sqrMagnitude < 1e-4f)
                perp = Vector3.Cross(u, Vector3.forward);
        }

        // Pastikan v benar-benar tegak lurus u
        Vector3 v = Vector3.Cross(perp.normalized, u).normalized;

        // Cek apakah arah busur benar (searah dengan to)
        // Jika dot(v, to) < 0, balik v agar busur mengarah ke medium yang benar
        if (Vector3.Dot(v, to) < 0f) v = -v;

        int count = Mathf.Max(2, arcSegments);
        lr.positionCount = count + 1;
        for (int i = 0; i <= count; i++)
        {
            float ang = (float)i / count * angleDeg * Mathf.Deg2Rad;
            lr.SetPosition(i, centre + radius * (Mathf.Cos(ang) * u + Mathf.Sin(ang) * v));
        }
        return true;
    }
}