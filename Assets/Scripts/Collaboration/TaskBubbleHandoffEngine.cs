using System;
using UnityEngine;
using OmniOS.Core;

namespace HoloBridge.Collaboration
{
    /// <summary>
    /// TaskBubbleHandoffEngine - Scoped Context-Preserving Micro-Handoff for IEEE VR 2027.
    /// Solves the "Precision Manipulation Breakdown" in Cross-Device Collaboration:
    /// - VR collaborator commands full stereoscopic scene context but suffers from 6-DoF controller jitter.
    /// - Desktop collaborator commands sub-millimeter 2D mouse precision.
    /// - Rather than crude direct manipulation (which causes unexpected scene jumps and disorientation),
    ///   TaskBubble delegates only the specific difficult component.
    /// - Desktop user adjusts fine alignment handles while the VR user inspects a live Holographic Ghost Preview.
    /// - Consent-Based Commit ensures the VR user retains spatial authority to approve [Accept] or [Reject].
    /// </summary>
    public class TaskBubbleHandoffEngine : MonoBehaviour
    {
        public enum HandoffState
        {
            Idle = 0,
            Delegated = 1,      // Part is delegated to desktop user; gizmo active
            Previewing = 2,     // Ghost preview is actively being transformed
            Committed = 3       // Alignment approved and permanently locked
        }

        [Header("State")]
        [SerializeField] private HandoffState currentState = HandoffState.Idle;
        [SerializeField] private SharedDigitalTwinAssembly.ComponentType activeDelegatedType = SharedDigitalTwinAssembly.ComponentType.InternalBearing;

        [Header("Ghost Preview Configuration")]
        [SerializeField] private Color ghostColor = new Color(0.0f, 0.85f, 1.0f, 0.45f);
        [SerializeField] private Color ghostGlowColor = new Color(0.1f, 1.0f, 0.8f, 0.9f);
        [SerializeField] private float mouseSensitivity = 0.0015f; // Precision sub-millimeter mouse scaling

        [Header("Alignment Telemetry")]
        [SerializeField] private Vector3 proposedOffset = Vector3.zero;
        [SerializeField] private float deviationDistanceMm = 0.0f;
        [SerializeField] private bool isWithinTolerance = true;

        public HandoffState CurrentState => currentState;
        public SharedDigitalTwinAssembly.ComponentType ActiveDelegatedType => activeDelegatedType;
        public Vector3 ProposedOffset => proposedOffset;
        public float DeviationDistanceMm => deviationDistanceMm;
        public bool IsWithinTolerance => isWithinTolerance;

        private SharedDigitalTwinAssembly targetAssembly;
        private GameObject ghostProxy;
        private Material ghostMaterial;
        private Transform originalPartTransform;
        private Vector3 initialPartLocalPos;
        private Quaternion initialPartLocalRot;

        private void Start()
        {
            targetAssembly = UnityEngine.Object.FindAnyObjectByType<SharedDigitalTwinAssembly>();
        }

        private void Update()
        {
            if (currentState == HandoffState.Delegated || currentState == HandoffState.Previewing)
            {
                HandleDesktopPrecisionMouseAdjustment();
            }
        }

        /// <summary>
        /// VR Collaborator delegates a micro-assembly task to the desktop collaborator.
        /// </summary>
        public void DelegateTaskToDesktop(SharedDigitalTwinAssembly.ComponentType partType)
        {
            if (targetAssembly == null) targetAssembly = UnityEngine.Object.FindAnyObjectByType<SharedDigitalTwinAssembly>();
            if (targetAssembly == null) return;

            var part = targetAssembly.GetPart(partType);
            if (part == null || part.gameObject == null) return;

            activeDelegatedType = partType;
            originalPartTransform = part.gameObject.transform;
            initialPartLocalPos = originalPartTransform.localPosition;
            initialPartLocalRot = originalPartTransform.localRotation;
            proposedOffset = Vector3.zero;

            // Mark part as delegated in the assembly
            targetAssembly.SetPartDelegated(partType, true);

            // Spawn Holographic Ghost Preview
            CreateGhostProxy(part);

            currentState = HandoffState.Delegated;
            SpatialAudioEngine.Instance?.PlayWindowOpen();
            OmniOSManager.Instance?.ShowNotificationToast($"🤝 Task Delegated: {part.displayName} assigned to Desktop Precision!");
        }

        private void CreateGhostProxy(SharedDigitalTwinAssembly.AssemblyPart part)
        {
            if (ghostProxy != null) Destroy(ghostProxy);

            // Duplicate the visual geometry of the part as a ghost
            ghostProxy = Instantiate(part.gameObject, part.gameObject.transform.parent);
            ghostProxy.name = $"{part.gameObject.name}_GhostPreview";
            ghostProxy.transform.localPosition = part.gameObject.transform.localPosition;
            ghostProxy.transform.localRotation = part.gameObject.transform.localRotation;
            ghostProxy.transform.localScale = part.gameObject.transform.localScale;

            // Remove colliders on ghost to prevent physics interference
            var colliders = ghostProxy.GetComponentsInChildren<Collider>();
            foreach (var c in colliders) Destroy(c);

            // Create glowing holographic ghost shader
            Shader ghostShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            ghostMaterial = new Material(ghostShader);
            ghostMaterial.color = ghostColor;
            if (ghostMaterial.HasProperty("_BaseColor")) ghostMaterial.SetColor("_BaseColor", ghostColor);

            var renderers = ghostProxy.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                r.material = ghostMaterial;
            }
        }

        /// <summary>
        /// Desktop user uses mouse dragging/nudges for sub-millimeter precision adjustment.
        /// </summary>
        private void HandleDesktopPrecisionMouseAdjustment()
        {
            if (ghostProxy == null) return;

            // Mouse Nudges: Right-click + drag or Arrow Keys for fine translational adjustments
            float dx = 0f;
            float dy = 0f;
            float dz = 0f;

            if (Input.GetKey(KeyCode.LeftBracket)) dz -= mouseSensitivity;
            if (Input.GetKey(KeyCode.RightBracket)) dz += mouseSensitivity;
            if (Input.GetKey(KeyCode.PageUp)) dy += mouseSensitivity;
            if (Input.GetKey(KeyCode.PageDown)) dy -= mouseSensitivity;

            // Mouse wheel sub-millimeter depth adjustment
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                dz += scroll * mouseSensitivity * 0.5f;
            }

            if (Mathf.Abs(dx) > 1e-6f || Mathf.Abs(dy) > 1e-6f || Mathf.Abs(dz) > 1e-6f)
            {
                ApplyNudge(new Vector3(dx, dy, dz));
            }

            // Pulse ghost glow slightly to indicate active previewing
            if (ghostMaterial != null)
            {
                float pulse = 0.35f + Mathf.Sin(Time.time * 5.0f) * 0.15f;
                Color c = ghostColor;
                c.a = pulse;
                ghostMaterial.color = c;
            }
        }

        public void ApplyNudge(Vector3 deltaLocal)
        {
            if (ghostProxy == null) return;

            proposedOffset += deltaLocal;
            ghostProxy.transform.localPosition = initialPartLocalPos + proposedOffset;

            // Calculate deviation in millimeters
            deviationDistanceMm = proposedOffset.magnitude * 1000f; // m to mm
            isWithinTolerance = deviationDistanceMm <= 0.25f; // 0.25mm engineering threshold

            currentState = HandoffState.Previewing;
        }

        /// <summary>
        /// VR Collaborator inspects the ghost preview in full stereoscopic context and confirms [Accept].
        /// </summary>
        public void CommitPlacement()
        {
            if (currentState == HandoffState.Idle || originalPartTransform == null) return;

            // Authoritatively apply the proposed transform to the physical object
            originalPartTransform.localPosition = initialPartLocalPos + proposedOffset;

            // Clear delegation state
            if (targetAssembly != null)
            {
                targetAssembly.SetPartDelegated(activeDelegatedType, false);
            }

            // Clean up ghost preview
            if (ghostProxy != null)
            {
                Destroy(ghostProxy);
                ghostProxy = null;
            }

            currentState = HandoffState.Committed;
            SpatialAudioEngine.Instance?.PlayNotificationChime();
            OmniOSManager.Instance?.ShowNotificationToast("✅ Alignment Approved! Part permanently locked into master assembly.");
        }

        /// <summary>
        /// VR Collaborator rejects the proposal if clearances or aesthetics do not match.
        /// </summary>
        public void RejectPlacement()
        {
            if (currentState == HandoffState.Idle || originalPartTransform == null) return;

            // Revert back to original position
            originalPartTransform.localPosition = initialPartLocalPos;

            // Clear delegation state
            if (targetAssembly != null)
            {
                targetAssembly.SetPartDelegated(activeDelegatedType, false);
            }

            // Clean up ghost preview
            if (ghostProxy != null)
            {
                Destroy(ghostProxy);
                ghostProxy = null;
            }

            currentState = HandoffState.Idle;
            SpatialAudioEngine.Instance?.PlayWindowMinimize();
            OmniOSManager.Instance?.ShowNotificationToast("❌ Micro-Handoff Rejected: Part reverted to nominal state.");
        }
    }
}
