using UnityEngine;
using System.Collections.Generic;

public class ReflectionVisualizer : MonoBehaviour
{
    public Transform sourcePoint;
    public Transform reflectorPlane;
    public Transform reflectedPoint;
    public int rayCount = 3;
    public Color rayColor = Color.yellow;

    private List<LineRenderer> rays = new();

    void Start() { CreateRays(); }

    void Update()
    {
        UpdateReflectedPos();
        UpdateRays();
    }

    void CreateRays()
    {
        for (int i = 0; i < rayCount; i++)
        {
            var go = new GameObject("Ray_" + i);
            go.transform.parent = transform;
            var lr = go.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = lr.endColor = rayColor;
            lr.startWidth = lr.endWidth = 0.015f;
            lr.positionCount = 3;
            rays.Add(lr);
        }
    }

    void UpdateReflectedPos()
    {
        var n = reflectorPlane.right;
        var d = Vector3.Dot(sourcePoint.position
                - reflectorPlane.position, n);
        reflectedPoint.position =
                sourcePoint.position - 2f * d * n;
    }

    void UpdateRays()
    {
        Vector3[] targets = {
            reflectorPlane.position,
            reflectorPlane.position + reflectorPlane.up * 0.3f,
            reflectorPlane.position - reflectorPlane.up * 0.3f
        };
        for (int i = 0; i < rays.Count; i++)
        {
            var t = targets[i % targets.Length];
            var inc = (t - sourcePoint.position).normalized;
            var ref_ = Vector3.Reflect(inc, reflectorPlane.right);
            rays[i].SetPosition(0, sourcePoint.position);
            rays[i].SetPosition(1, t);
            rays[i].SetPosition(2, t + ref_ * 1.5f);
        }
    }
}