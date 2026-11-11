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
    [SerializeField] private Transform leftArmVisual;
    [SerializeField] private Transform rightArmVisual;
    [SerializeField] private Transform leftLegVisual;
    [SerializeField] private Transform rightLegVisual;

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
    private float walkCyclePhase = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        charController = GetComponent<CharacterController>();

        if (bodyVisual == null)
        {
            var body = transform.Find("BodyVisual");
            if (body != null) bodyVisual = body;
            else BuildDefaultHumanoidVisuals();
        }
    }

    private void BuildDefaultHumanoidVisuals()
    {
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");

        Material armorMat = new Material(litShader) { color = new Color(0.12f, 0.16f, 0.22f) };
        Material suitMat = new Material(litShader) { color = new Color(0.08f, 0.10f, 0.14f) };
        Material glowMat = new Material(unlitShader) { color = new Color(0f, 0.92f, 1f) };

        // 1. Torso & Pelvis
        GameObject bodyObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bodyObj.name = "BodyVisual";
        bodyObj.transform.SetParent(transform, false);
        bodyObj.transform.localPosition = new Vector3(0f, 0.85f, 0f);
        bodyObj.transform.localScale = new Vector3(0.32f, 0.45f, 0.22f);
        var colB = bodyObj.GetComponent<Collider>();
        if (colB != null) Destroy(colB);
        bodyObj.GetComponent<MeshRenderer>().material = armorMat;
        bodyVisual = bodyObj.transform;

        // Chest Armor Trim
        GameObject chestTrim = GameObject.CreatePrimitive(PrimitiveType.Cube);
        chestTrim.name = "ChestTrim";
        chestTrim.transform.SetParent(bodyVisual, false);
        chestTrim.transform.localPosition = new Vector3(0f, 0.05f, 0.52f);
        chestTrim.transform.localScale = new Vector3(0.40f, 0.30f, 0.08f);
        var colCT = chestTrim.GetComponent<Collider>();
        if (colCT != null) Destroy(colCT);
        chestTrim.GetComponent<MeshRenderer>().material = glowMat;

        // 2. Head & Cyber Visor
        if (headVisual == null)
        {
            GameObject headObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            headObj.name = "HeadVisual";
            headObj.transform.SetParent(transform, false);
            headObj.transform.localPosition = new Vector3(0f, 1.55f, 0f);
            headObj.transform.localScale = new Vector3(0.24f, 0.26f, 0.24f);
            var colH = headObj.GetComponent<Collider>();
            if (colH != null) Destroy(colH);
            headObj.GetComponent<MeshRenderer>().material = suitMat;

            // Glowing Cyber Visor
            GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "VRVisor";
            visor.transform.SetParent(headObj.transform, false);
            visor.transform.localPosition = new Vector3(0f, 0.04f, 0.38f);
            visor.transform.localScale = new Vector3(0.75f, 0.25f, 0.35f);
            var colV = visor.GetComponent<Collider>();
            if (colV != null) Destroy(colV);
            visor.GetComponent<MeshRenderer>().material = glowMat;

            headVisual = headObj.transform;
        }

        // 3. Hands
        if (leftHandVisual == null)
        {
            GameObject lHand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lHand.name = "LeftHandVisual";
            lHand.transform.SetParent(transform, false);
            lHand.transform.localPosition = new Vector3(-0.35f, 1.0f, 0.25f);
            lHand.transform.localScale = new Vector3(0.10f, 0.12f, 0.14f);
            var colL = lHand.GetComponent<Collider>();
            if (colL != null) Destroy(colL);
            lHand.GetComponent<MeshRenderer>().material = glowMat;
            leftHandVisual = lHand.transform;
        }

        if (rightHandVisual == null)
        {
            GameObject rHand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rHand.name = "RightHandVisual";
            rHand.transform.SetParent(transform, false);
            rHand.transform.localPosition = new Vector3(0.35f, 1.0f, 0.25f);
            rHand.transform.localScale = new Vector3(0.10f, 0.12f, 0.14f);
            var colR = rHand.GetComponent<Collider>();
            if (colR != null) Destroy(colR);
            rHand.GetComponent<MeshRenderer>().material = glowMat;
            rightHandVisual = rHand.transform;
        }

        // 4. Arms (Upper Arm & Forearm segments connecting Torso to Hands)
        if (leftArmVisual == null)
        {
            GameObject lArm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            lArm.name = "LeftArmVisual";
            lArm.transform.SetParent(bodyVisual, false);
            lArm.transform.localPosition = new Vector3(-0.25f, 0.1f, 0.1f);
            lArm.transform.localScale = new Vector3(0.08f, 0.22f, 0.08f);
            var colLA = lArm.GetComponent<Collider>();
            if (colLA != null) Destroy(colLA);
            lArm.GetComponent<MeshRenderer>().material = suitMat;
            leftArmVisual = lArm.transform;
        }

        if (rightArmVisual == null)
        {
            GameObject rArm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            rArm.name = "RightArmVisual";
            rArm.transform.SetParent(bodyVisual, false);
            rArm.transform.localPosition = new Vector3(0.25f, 0.1f, 0.1f);
            rArm.transform.localScale = new Vector3(0.08f, 0.22f, 0.08f);
            var colRA = rArm.GetComponent<Collider>();
            if (colRA != null) Destroy(colRA);
            rArm.GetComponent<MeshRenderer>().material = suitMat;
            rightArmVisual = rArm.transform;
        }

        // 5. Legs & Boots (Procedurally animated lower limbs)
        if (leftLegVisual == null)
        {
            GameObject lLeg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            lLeg.name = "LeftLegVisual";
            lLeg.transform.SetParent(bodyVisual, false);
            lLeg.transform.localPosition = new Vector3(-0.11f, -0.45f, 0f);
            lLeg.transform.localScale = new Vector3(0.10f, 0.35f, 0.10f);
            var colLL = lLeg.GetComponent<Collider>();
            if (colLL != null) Destroy(colLL);
            lLeg.GetComponent<MeshRenderer>().material = armorMat;
            leftLegVisual = lLeg.transform;
        }

        if (rightLegVisual == null)
        {
            GameObject rLeg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            rLeg.name = "RightLegVisual";
            rLeg.transform.SetParent(bodyVisual, false);
            rLeg.transform.localPosition = new Vector3(0.11f, -0.45f, 0f);
            rLeg.transform.localScale = new Vector3(0.10f, 0.35f, 0.10f);
            var colRL = rLeg.GetComponent<Collider>();
            if (colRL != null) Destroy(colRL);
            rLeg.GetComponent<MeshRenderer>().material = armorMat;
            rightLegVisual = rLeg.transform;
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
        float lerpFactor = Mathf.Clamp01(Time.deltaTime * 28f);

        if (HasStateAuthority)
        {
            // Local Player: Follow tracking devices directly for zero latency
            if (headVisual != null && localCam != null)
            {
                headVisual.position = localCam.position;
                headVisual.rotation = localCam.rotation;
                // Cull head mesh locally in VR to prevent clipping player's eyes
                headVisual.localScale = Vector3.zero;
            }
            if (leftHandVisual != null && localLeftCtrl != null)
            {
                leftHandVisual.position = localLeftCtrl.position;
                leftHandVisual.rotation = localLeftCtrl.rotation;
            }
            if (rightHandVisual != null && localRightCtrl != null)
            {
                rightHandVisual.position = localRightCtrl.position;
                rightHandVisual.rotation = localRightCtrl.rotation;
            }
        }
        else
        {
            // Remote Players: Fluid high-frequency interpolation for zero micro-stutter/jitter
            if (headVisual != null)
            {
                headVisual.localScale = new Vector3(0.24f, 0.26f, 0.24f);
                headVisual.position = Vector3.Lerp(headVisual.position, NetHeadPos, lerpFactor);
                headVisual.rotation = Quaternion.Slerp(headVisual.rotation, NetHeadRot, lerpFactor);
            }
            if (leftHandVisual != null)
            {
                leftHandVisual.position = Vector3.Lerp(leftHandVisual.position, NetLeftPos, lerpFactor);
                leftHandVisual.rotation = Quaternion.Slerp(leftHandVisual.rotation, NetLeftRot, lerpFactor);
            }
            if (rightHandVisual != null)
            {
                rightHandVisual.position = Vector3.Lerp(rightHandVisual.position, NetRightPos, lerpFactor);
                rightHandVisual.rotation = Quaternion.Slerp(rightHandVisual.rotation, NetRightRot, lerpFactor);
            }
        }

        UpdateBodySync();
    }

    private void UpdateBodySync()
    {
        if (bodyVisual == null || headVisual == null) return;

        // 1. Torso Alignment below Head
        Vector3 targetBodyPos = headVisual.position + (Vector3.down * bodyHeightOffset);
        bodyVisual.position = Vector3.Lerp(bodyVisual.position, targetBodyPos, Time.deltaTime * 24f);

        Vector3 headForward = headVisual.forward;
        headForward.y = 0f;
        if (headForward.sqrMagnitude > 0.001f)
        {
            Quaternion targetBodyRot = Quaternion.LookRotation(headForward, Vector3.up);
            bodyVisual.rotation = Quaternion.Slerp(bodyVisual.rotation, targetBodyRot, Time.deltaTime * 20f);
        }

        // 2. Articulated Arm IK (Connecting Shoulders to Hands)
        if (leftArmVisual != null && leftHandVisual != null)
        {
            Vector3 leftShoulder = bodyVisual.position + bodyVisual.rotation * new Vector3(-0.20f, 0.12f, 0.02f);
            Vector3 leftArmMid = (leftShoulder + leftHandVisual.position) * 0.5f;
            leftArmVisual.position = leftArmMid;
            Vector3 leftArmDir = (leftHandVisual.position - leftShoulder).normalized;
            if (leftArmDir.sqrMagnitude > 0.001f)
            {
                leftArmVisual.rotation = Quaternion.LookRotation(leftArmDir, Vector3.up);
            }
        }

        if (rightArmVisual != null && rightHandVisual != null)
        {
            Vector3 rightShoulder = bodyVisual.position + bodyVisual.rotation * new Vector3(0.20f, 0.12f, 0.02f);
            Vector3 rightArmMid = (rightShoulder + rightHandVisual.position) * 0.5f;
            rightArmVisual.position = rightArmMid;
            Vector3 rightArmDir = (rightHandVisual.position - rightShoulder).normalized;
            if (rightArmDir.sqrMagnitude > 0.001f)
            {
                rightArmVisual.rotation = Quaternion.LookRotation(rightArmDir, Vector3.up);
            }
        }

        // 3. Articulated Legs & Natural Locomotion Swing
        float moveSpeed = pushVelocity.magnitude;
        if (charController != null) moveSpeed = Mathf.Max(moveSpeed, charController.velocity.magnitude);

        bool isMoving = moveSpeed > 0.08f;
        if (isMoving)
        {
            walkCyclePhase += Time.deltaTime * Mathf.Clamp(moveSpeed * 6f, 4f, 10f);
        }
        else
        {
            walkCyclePhase = Mathf.Lerp(walkCyclePhase, 0f, Time.deltaTime * 6f);
        }

        float legSwing = Mathf.Sin(walkCyclePhase) * Mathf.Clamp01(moveSpeed / 1.5f) * 28f;

        if (leftLegVisual != null)
        {
            leftLegVisual.localPosition = new Vector3(-0.11f, -0.45f, Mathf.Sin(walkCyclePhase) * 0.06f);
            leftLegVisual.localRotation = Quaternion.Euler(legSwing, 0f, 0f);
        }
        if (rightLegVisual != null)
        {
            rightLegVisual.localPosition = new Vector3(0.11f, -0.45f, -Mathf.Sin(walkCyclePhase) * 0.06f);
            rightLegVisual.localRotation = Quaternion.Euler(-legSwing, 0f, 0f);
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
