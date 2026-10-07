using UnityEngine;
using Fusion;

public class VRPlayerController : NetworkBehaviour
{
    [Header("Visual Transforms")]
    public Transform headVisual;
    public Transform leftHandVisual;
    public Transform rightHandVisual;

    private Transform vrCamera;
    private Transform vrLeftController;
    private Transform vrRightController;

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                vrCamera = mainCam.transform;
            }

            GameObject leftHand = GameObject.Find("Left Controller") ?? GameObject.Find("LeftHand Controller");
            if (leftHand != null)
            {
                vrLeftController = leftHand.transform;
            }

            GameObject rightHand = GameObject.Find("Right Controller") ?? GameObject.Find("RightHand Controller");
            if (rightHand != null)
            {
                vrRightController = rightHand.transform;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority)
        {
            if (vrCamera != null && headVisual != null)
            {
                headVisual.position = vrCamera.position;
                headVisual.rotation = vrCamera.rotation;
            }

            if (vrLeftController != null && leftHandVisual != null)
            {
                leftHandVisual.position = vrLeftController.position;
                leftHandVisual.rotation = vrLeftController.rotation;
            }

            if (vrRightController != null && rightHandVisual != null)
            {
                rightHandVisual.position = vrRightController.position;
                rightHandVisual.rotation = vrRightController.rotation;
            }
        }
    }
}