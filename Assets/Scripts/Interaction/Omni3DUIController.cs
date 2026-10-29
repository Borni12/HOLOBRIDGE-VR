using System;
using UnityEngine;
using UnityEngine.XR;
using OmniGenesis.Core;

namespace OmniGenesis.Interaction
{
    /// <summary>
    /// One-Euro Filter for sub-millimeter jitter reduction in 6-DoF XR tracking.
    /// </summary>
    public class OneEuroFilter3D
    {
        private float minCutoff;
        private float beta;
        private float dCutoff;
        private Vector3 xPrev;
        private Vector3 dxPrev;
        private float tPrev;
        private bool initialized;

        public OneEuroFilter3D(float minCutoff = 1.0f, float beta = 0.005f, float dCutoff = 1.0f)
        {
            this.minCutoff = minCutoff;
            this.beta = beta;
            this.dCutoff = dCutoff;
            initialized = false;
        }

        public Vector3 Filter(Vector3 x, float t)
        {
            if (!initialized)
            {
                xPrev = x;
                dxPrev = Vector3.zero;
                tPrev = t;
                initialized = true;
                return x;
            }

            float dt = Mathf.Max(t - tPrev, 1e-5f);
            tPrev = t;

            Vector3 dx = (x - xPrev) / dt;
            float alphaD = Alpha(dt, dCutoff);
            Vector3 dxHat = Vector3.Lerp(dxPrev, dx, alphaD);
            dxPrev = dxHat;

            float cutoff = minCutoff + beta * dxHat.magnitude;
            float alpha = Alpha(dt, cutoff);
            Vector3 xHat = Vector3.Lerp(xPrev, x, alpha);
            xPrev = xHat;

            return xHat;
        }

        private float Alpha(float dt, float cutoff)
        {
            float tau = 1.0f / (2.0f * Mathf.PI * cutoff);
            return 1.0f / (1.0f + tau / dt);
        }
    }

    /// <summary>
    /// Omni3DUIController - Implements 4 mathematical 3DUI interaction metaphors for IEEE VR:
    /// 1. RealityPeelTransition (Bimanual projective unprojection from 2D portal to 360 5D Gaussians)
    /// 2. MatterWeaveManipulator (Gaze cone selection + wrist-tuned continuum stiffness k in [Rigid, Elastic, Fluid])
    /// 3. MultiverseOrbSelector (Toroidal 5-branch phase-space time scrubber)
    /// 4. JointGazeHandoff (Bayesian gaze-hand intention prediction for avatar physical exchange)
    /// </summary>
    [DisallowMultipleComponent]
    public class Omni3DUIController : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private OmniGenesisClient client;
        [SerializeField] private Transform headCamera;
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform rightHand;
        [SerializeField] private Transform portalFrame;

        [Header("3DUI Interaction Thresholds")]
        [SerializeField] private float peelTriggerDistance = 0.45f;
        [SerializeField] private float handoffDistanceThreshold = 0.55f;
        [SerializeField] private float handoffVelocityThreshold = 0.08f; // m/s
        [SerializeField] private float multiversePullThreshold = 0.15f;

        // 1-Euro Filters
        private OneEuroFilter3D leftHandFilter;
        private OneEuroFilter3D rightHandFilter;
        private OneEuroFilter3D headFilter;

        // Metaphor 1: Reality-Peel State
        public float RealityPeelProgress { get; private set; } = 0.0f; // 0 = 2D portal, 1 = 360 3D world
        private bool isPeeling = false;

        // Metaphor 2: Matter-Weave State
        public float CurrentStiffness { get; private set; } = 1.0f; // 0 = Fluid, 1 = Elastic, 2 = Rigid
        public bool IsMatterWeaveActive { get; private set; } = false;

        // Metaphor 3: Multiverse Orbs State
        public bool AreMultiverseOrbsVisible { get; private set; } = false;
        public int SelectedTimelineBranch { get; private set; } = 2; // Default branch (0 to 4)

        // Metaphor 4: Joint-Gaze Handoff State
        public float HandoffIntentionScore { get; private set; } = 0.0f; // [0, 1]
        public bool IsHandoffEngaged { get; private set; } = false;

        private Vector3 prevRightHandPos;

        private void Awake()
        {
            if (client == null) client = UnityEngine.Object.FindAnyObjectByType<OmniGenesisClient>();
            if (headCamera == null && Camera.main != null) headCamera = Camera.main.transform;

            leftHandFilter = new OneEuroFilter3D(1.0f, 0.005f);
            rightHandFilter = new OneEuroFilter3D(1.0f, 0.005f);
            headFilter = new OneEuroFilter3D(1.5f, 0.003f);
        }

        private void Update()
        {
            float time = Time.time;
            Vector3 rawHead = headCamera != null ? headCamera.position : Vector3.up * 1.6f;
            Vector3 rawLeft = leftHand != null ? leftHand.position : new Vector3(-0.2f, 1.2f, 0.4f);
            Vector3 rawRight = rightHand != null ? rightHand.position : new Vector3(0.2f, 1.2f, 0.4f);

            // Sub-millimeter 1-Euro filtered positions
            Vector3 filteredHead = headFilter.Filter(rawHead, time);
            Vector3 filteredLeft = leftHandFilter.Filter(rawLeft, time);
            Vector3 filteredRight = rightHandFilter.Filter(rawRight, time);

            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            Vector3 rightVel = (filteredRight - prevRightHandPos) / dt;
            prevRightHandPos = filteredRight;

            // 1. Evaluate Reality-Peel Metaphor
            UpdateRealityPeel(filteredHead, filteredLeft, filteredRight);

            // 2. Evaluate Matter-Weave Metaphor
            UpdateMatterWeave(filteredLeft, filteredRight);

            // 3. Evaluate Multiverse Orbs Metaphor
            UpdateMultiverseOrbs(filteredHead, filteredLeft, filteredRight);

            // 4. Evaluate Joint-Gaze Handoff Metaphor
            UpdateJointGazeHandoff(filteredHead, filteredRight, rightVel.magnitude);

            // Push active states to client synchronizer
            if (client != null)
            {
                client.MaterialStiffness = CurrentStiffness;
                client.IsMatterWeaveActive = IsMatterWeaveActive;
                client.IsJointGazeHandoffActive = IsHandoffEngaged;
            }
        }

        private void UpdateRealityPeel(Vector3 headPos, Vector3 leftPos, Vector3 rightPos)
        {
            if (portalFrame == null) return;

            float distLeft = Vector3.Distance(leftPos, portalFrame.position);
            float distRight = Vector3.Distance(rightPos, portalFrame.position);

            // Bimanual grab when both hands pinch near portal edges
            if (distLeft < 0.25f && distRight < 0.25f)
            {
                isPeeling = true;
                Vector3 bimanualMid = (leftPos + rightPos) * 0.5f;
                float pullDistance = Vector3.Distance(bimanualMid, headPos);

                // Closer to eyes expands 2D portal into 360 3D Gaussian world
                RealityPeelProgress = Mathf.Clamp01(1.0f - (pullDistance / peelTriggerDistance));
            }
            else
            {
                if (RealityPeelProgress > 0.85f)
                {
                    RealityPeelProgress = 1.0f; // Fully transitioned into 3D Gaussian space
                }
                else
                {
                    RealityPeelProgress = Mathf.MoveTowards(RealityPeelProgress, 0.0f, Time.deltaTime * 2.0f);
                }
                isPeeling = false;
            }
        }

        private void UpdateMatterWeave(Vector3 leftPos, Vector3 rightPos)
        {
            // Evaluate non-dominant wrist tilt for material stiffness modulation
            if (leftHand != null)
            {
                float wristRoll = leftHand.localEulerAngles.z;
                if (wristRoll > 180f) wristRoll -= 360f;

                // Map wrist roll [-45, +45] to stiffness [0=Fluid, 1=Elastic, 2=Rigid]
                float normRoll = Mathf.Clamp((wristRoll + 45f) / 90f, 0f, 2f);
                CurrentStiffness = normRoll;
            }

            // Engage matter-weave when dominant hand is within object proximity
            float distToTarget = Vector3.Distance(rightPos, new Vector3(0, 1.2f, 0.8f));
            IsMatterWeaveActive = (distToTarget < 0.35f);
        }

        private void UpdateMultiverseOrbs(Vector3 headPos, Vector3 leftPos, Vector3 rightPos)
        {
            // Bimanual pull gesture outward reveals 5 multiverse branches
            float bimanualSpan = Vector3.Distance(leftPos, rightPos);
            AreMultiverseOrbsVisible = (bimanualSpan > 0.65f);

            if (AreMultiverseOrbsVisible)
            {
                // Measure proximity of dominant hand to commit a branch
                for (int i = 0; i < 5; i++)
                {
                    float angle = (i - 2) * 0.35f;
                    Vector3 orbPos = headPos + new Vector3(Mathf.Sin(angle) * 0.4f, -0.1f, 0.65f);

                    if (Vector3.Distance(rightPos, orbPos) < multiversePullThreshold)
                    {
                        SelectedTimelineBranch = i;
                        TriggerHapticImpulse(0.5f, 0.03f);
                        break;
                    }
                }
            }
        }

        private void UpdateJointGazeHandoff(Vector3 headPos, Vector3 rightHandPos, float handSpeed)
        {
            if (client == null) return;

            Vector3 gazeForward = headCamera != null ? headCamera.forward : Vector3.forward;
            Vector3 avatarHead = client.AvatarHeadPosition;
            Vector3 avatarHand = client.AvatarHandPosition;

            // Bayesian Intention Prior: Gaze alignment toward avatar eyes + static offering gesture
            Vector3 toAvatarHead = (avatarHead - headPos).normalized;
            float gazeFixation = Mathf.Clamp01(Vector3.Dot(gazeForward, toAvatarHead));

            float distHandToAvatar = Vector3.Distance(rightHandPos, avatarHand);
            float proximityFactor = Mathf.Clamp01(1.0f - (distHandToAvatar / handoffDistanceThreshold));

            // User holds object still in front of avatar (low velocity offering)
            float stillnessFactor = Mathf.Clamp01(1.0f - (handSpeed / handoffVelocityThreshold));

            // Bayesian Posterior intention probability
            HandoffIntentionScore = gazeFixation * 0.4f + proximityFactor * 0.4f + stillnessFactor * 0.2f;

            if (HandoffIntentionScore > 0.72f && !IsHandoffEngaged)
            {
                IsHandoffEngaged = true;
                TriggerHapticImpulse(0.85f, 0.06f);
            }
            else if (HandoffIntentionScore < 0.40f)
            {
                IsHandoffEngaged = false;
            }
        }

        private void TriggerHapticImpulse(float amplitude, float duration)
        {
            var device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (device.isValid)
            {
                device.SendHapticImpulse(0, amplitude, duration);
            }
        }
    }
}
