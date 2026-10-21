using UnityEngine;
using Fusion;

public class VRPlayerController : NetworkBehaviour
{
    [Header("Avatar Visual Transforms")]
    [SerializeField] private Transform headVisual;
    [SerializeField] private Transform leftHandVisual;
    [SerializeField] private Transform rightHandVisual;
    [SerializeField] private Transform bodyVisual;

    [Header("Networked Synced Transforms")]
    [Networked] private Vector3 NetHeadPos { get; set; }
    [Networked] private Quaternion NetHeadRot { get; set; }
    [Networked] private Vector3 NetLeftPos { get; set; }
    [Networked] private Quaternion NetLeftRot { get; set; }
    [Networked] private Vector3 NetRightPos { get; set; }
    [Networked] private Quaternion NetRightRot { get; set; }

    private Transform localCam;
    private Transform localLeftCtrl;
    private Transform localRightCtrl;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (bodyVisual == null)
        {
            var body = transform.Find("BodyVisual");
            if (body != null) bodyVisual = body;
        }
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            Camera cam = Camera.main;
            if (cam == null) cam = FindFirstObjectByType<Camera>();
            if (cam != null) localCam = cam.transform;

            var leftObj = GameObject.Find("Left Controller");
            if (leftObj != null) localLeftCtrl = leftObj.transform;

            var rightObj = GameObject.Find("Right Controller");
            if (rightObj != null) localRightCtrl = rightObj.transform;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority)
        {
            if (localCam != null)
            {
                NetHeadPos = localCam.position;
                NetHeadRot = localCam.rotation;
            }
            if (localLeftCtrl != null)
            {
                NetLeftPos = localLeftCtrl.position;
                NetLeftRot = localLeftCtrl.rotation;
            }
            if (localRightCtrl != null)
            {
                NetRightPos = localRightCtrl.position;
                NetRightRot = localRightCtrl.rotation;
            }
        }
    }

    public override void Render()
    {
        if (headVisual != null)
        {
            headVisual.position = NetHeadPos;
            headVisual.rotation = NetHeadRot;
        }
        if (leftHandVisual != null)
        {
            leftHandVisual.position = NetLeftPos;
            leftHandVisual.rotation = NetLeftRot;
        }
        if (rightHandVisual != null)
        {
            rightHandVisual.position = NetRightPos;
            rightHandVisual.rotation = NetRightRot;
        }
        if (bodyVisual != null && headVisual != null)
        {
            bodyVisual.position = headVisual.position - new Vector3(0, 0.55f, 0);
            bodyVisual.rotation = Quaternion.Euler(0, headVisual.rotation.eulerAngles.y, 0);
        }
    }

    // মারামারি / ঘুসি লাগলে ফিজিক্স ধাক্কা
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("HandFist") && Object != null && HasStateAuthority)
        {
            Vector3 pushDir = (transform.position - collision.transform.position).normalized;
            pushDir.y = 0.1f;
            if (rb != null)
            {
                rb.AddForce(pushDir * 4f, ForceMode.Impulse);
            }
            Debug.Log("[VR Combat] ঘুসি বা ধাক্কা লেগেছে!");
        }
    }
}
