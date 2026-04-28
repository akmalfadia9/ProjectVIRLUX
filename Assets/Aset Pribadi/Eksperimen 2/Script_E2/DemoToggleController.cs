using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DemoToggleController : MonoBehaviour
{
    public GameObject demoRoot;
    public TextMeshProUGUI buttonText;
    public float animDuration = 0.35f;

    private bool isOpen = false;
    private Coroutine anim;

    void Start()
    {
        demoRoot.transform.localScale = Vector3.zero;
        demoRoot.SetActive(false);
    }

    public void ToggleDemo()
    {
        isOpen = !isOpen;
        buttonText.text = isOpen ? "Tutup Simulasi" : "Buka Simulasi";
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(isOpen ? Open() : Close());
    }

    IEnumerator Open()
    {
        demoRoot.SetActive(true);
        float t = 0;
        while (t < animDuration)
        {
            t += Time.deltaTime;
            float s = Mathf.SmoothStep(0, 1, t / animDuration);
            demoRoot.transform.localScale = Vector3.one * s;
            yield return null;
        }
        demoRoot.transform.localScale = Vector3.one;
    }

    IEnumerator Close()
    {
        float t = 0;
        while (t < animDuration)
        {
            t += Time.deltaTime;
            float s = 1 - Mathf.SmoothStep(0, 1, t / animDuration);
            demoRoot.transform.localScale = Vector3.one * s;
            yield return null;
        }
        demoRoot.SetActive(false);
    }
}