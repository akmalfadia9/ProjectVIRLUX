using UnityEngine;

using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class LaserGun : MonoBehaviour
{
    [Header("Laser")]
    public Transform laserSpawnPoint;
    public LineRenderer laserLine;
    public float laserRange = 50f;
    public LayerMask hitLayer;

    private XRGrabInteractable grab;
    private bool isGrabbed = false;
    private bool isFiring = false;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();

        // Perbaikan orientasi gun saat di-grab
        grab.trackPosition = true;
        grab.trackRotation = true;
        grab.throwOnDetach = false;

        grab.selectEntered.AddListener(_ => isGrabbed = true);
        grab.selectExited.AddListener(_ => { isGrabbed = false; StopLaser(); });
        grab.activated.AddListener(_ => StartLaser());
        grab.deactivated.AddListener(_ => StopLaser());

        laserLine.enabled = false;
    }

    void Update()
    {
        if (isFiring && isGrabbed)
            UpdateLaser();
    }

    void StartLaser()
    {
        if (!isGrabbed) return;
        isFiring = true;
        laserLine.enabled = true;
    }

    void StopLaser()
    {
        isFiring = false;
        laserLine.enabled = false;
    }

    void UpdateLaser()
    {
        laserLine.SetPosition(0, laserSpawnPoint.position);

        if (Physics.Raycast(laserSpawnPoint.position,
            laserSpawnPoint.forward, out RaycastHit hit, laserRange, hitLayer))
        {
            laserLine.SetPosition(1, hit.point);
        }
        else
        {
            laserLine.SetPosition(1,
                laserSpawnPoint.position + laserSpawnPoint.forward * laserRange);
        }
    }
}