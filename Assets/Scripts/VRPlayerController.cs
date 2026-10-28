using UnityEngine;
using Fusion;

/// <summary>
/// Controls VR player avatar locomotion, head/hand synchronization, and combat physics over Photon Fusion.
/// </summary>
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

    [Header("Body Alignment")]
    [SerializeField] private float bodyHeightOffset = 0.65f;

    [Header("Combat & Pushback")]
    [SerializeField] private float pushbackForce = 4.5f;

    private Transform localCam;
    private Transform localLeftCtrl;
    private Transform localRightCtrl;
    private Rigidbody rb;
    private CharacterController charController;
    private Vector3 pushVelocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        charController = GetComponent<CharacterController>();

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
            Camera cam = Camera.main ?? UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (cam != null) localCam = cam.transform;

            FindLocalControllers();
        }

        SetupPhysics();
    }

    private void FindLocalControllers()
    {
        var left = GameObject.Find("Left Controller") ?? GameObject.Find("LeftHand Controller");
        if (left != null) localLeftCtrl = left.transform;

        var right = GameObject.Find("Right Controller") ?? GameObject.Find("RightHand Controller");
        if (right != null) localRightCtrl = right.transform;
    }

    private void SetupPhysics()
    {
        if (bodyVisual != null)
        {
            var bodyCol = bodyVisual.GetComponent<Collider>();
            if (bodyCol == null)
            {
                var cap = bodyVisual.gameObject.AddComponent<CapsuleCollider>();
                cap.height = 1.2f;
                cap.radius = 0.22f;
                cap.center = Vector3.zero;
            }
        }

        if (leftHandVisual != null) SetupHandTrigger(leftHandVisual);
        if (rightHandVisual != null) SetupHandTrigger(rightHandVisual);
    }

    private void SetupHandTrigger(Transform hand)
    {
        var col = hand.GetComponent<Collider>();
        if (col == null)
        {
            var sphere = hand.gameObject.AddComponent<SphereCollider>();
            sphere.radius = 0.12f;
            sphere.isTrigger = true;
        }
        else
        {
            col.isTrigger = true;
        }

        if (hand.GetComponent<HandFistMarker>() == null)
        {
            hand.gameObject.AddComponent<HandFistMarker>();
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

            if (pushVelocity.sqrMagnitude > 0.01f)
            {
                if (charController != null && charController.enabled)
                {
                    charController.Move(pushVelocity * Runner.DeltaTime);
                }
                pushVelocity = Vector3.Lerp(pushVelocity, Vector3.zero, Runner.DeltaTime * 5f);
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

        UpdateBodySync();
    }

    private void UpdateBodySync()
    {
        if (bodyVisual == null || headVisual == null) return;

        bodyVisual.position = headVisual.position + (Vector3.down * bodyHeightOffset);

        Vector3 headForward = headVisual.forward;
        headForward.y = 0f;

        if (headForward.sqrMagnitude > 0.001f)
        {
            bodyVisual.rotation = Quaternion.LookRotation(headForward, Vector3.up);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!HasStateAuthority || Object == null) return;

        var handMarker = other.GetComponent<HandFistMarker>();
        if (handMarker != null)
        {
            var otherPlayer = other.GetComponentInParent<VRPlayerController>();
            if (otherPlayer != null && otherPlayer != this)
            {
                ApplyCombatPushback(other.transform.position);
            }
        }
    }

    private void ApplyCombatPushback(Vector3 attackerPoint)
    {
        Vector3 hitDirection = (transform.position - attackerPoint).normalized;
        hitDirection.y = 0.15f;

        if (rb != null && !rb.isKinematic)
        {
            rb.AddForce(hitDirection * pushbackForce, ForceMode.Impulse);
        }
        else
        {
            pushVelocity = hitDirection * pushbackForce;
            transform.position += hitDirection * (pushbackForce * 0.05f);
        }

        Debug.Log($"[Combat Pushback] Player received hit from: {attackerPoint}");
    }
}

public class HandFistMarker : MonoBehaviour { }
