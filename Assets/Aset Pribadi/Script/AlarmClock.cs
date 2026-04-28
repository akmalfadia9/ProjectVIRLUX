using UnityEngine;


[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class AlarmClock : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip alarmSound;

    private AudioSource audioSource;
    private bool isRinging = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = alarmSound;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
    }

    public void ToggleAlarm()
    {
        isRinging = !isRinging;

        if (isRinging)
            audioSource.Play();
        else
            audioSource.Stop();
    }
}