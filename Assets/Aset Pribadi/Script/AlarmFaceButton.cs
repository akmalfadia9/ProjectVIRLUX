using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRSimpleInteractable))]
public class AlarmFaceButton : MonoBehaviour
{
    [Header("Reference")]
    public AlarmClock alarmClock;

    private XRSimpleInteractable interactable;

    void Start()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        interactable.activated.AddListener(OnFaceClicked);
    }

    void OnFaceClicked(ActivateEventArgs args)
    {
        if (alarmClock != null)
            alarmClock.ToggleAlarm();
    }

    void OnDestroy()
    {
        interactable.activated.RemoveListener(OnFaceClicked);
    }
}