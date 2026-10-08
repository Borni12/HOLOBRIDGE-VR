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

            
            GameObject leftObj = GameObject.Find("XR Origin (XR Rig)/Camera Offset/Left Controller");
            if (leftObj != null)
            {
                vrLeftController = leftObj.transform;
            }
            else
            {
                vrLeftController = GameObject.Find("Left Controller")?.transform;
            }

            GameObject rightObj = GameObject.Find("XR Origin (XR Rig)/Camera Offset/Right Controller");
            if (rightObj != null)
            {
                vrRightController = rightObj.transform;
            }
            else
            {
                vrRightController = GameObject.Find("Right Controller")?.transform;
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
