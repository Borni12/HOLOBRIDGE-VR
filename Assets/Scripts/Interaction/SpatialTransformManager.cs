using UnityEngine;
using UnityEngine.InputSystem;

namespace AegisSpatial.Interaction
{
    /// <summary>
    /// Bi-Manual Multi-Scale Interaction Manager.
    /// Maps the dominant (right) tool into the non-dominant (left) hand workspace anchor.
    /// Dynamically scales delta translation down to sub-millimeter precision based on hand proximity.
    /// Supports grip-triggered workspace frame locking for zero-fatigue high-precision interactions.
    /// </summary>
    [DisallowMultipleComponent]
    public class SpatialTransformManager : MonoBehaviour
    {
        [Header("Hand Transforms (Fallback / Direct Anchor)")]
        [SerializeField] private Transform leftHandAnchor;
        [SerializeField] private Transform rightHandTool;
        [SerializeField] private Transform manipulatedTarget;

        [Header("New Input System Actions (Optional Override)")]
        [SerializeField] private InputActionProperty leftPositionAction;
        [SerializeField] private InputActionProperty leftRotationAction;
        [SerializeField] private InputActionProperty leftGripAction;
        [SerializeField] private InputActionProperty rightPositionAction;
        [SerializeField] private InputActionProperty rightRotationAction;

        [Header("Multi-Scale Precision Parameters")]
        [Tooltip("Distance threshold (meters) where precision scaling begins engaging.")]
        [SerializeField] private float precisionDistanceThreshold = 0.25f;

        [Tooltip("Minimum distance at which maximum precision scaling is reached.")]
        [SerializeField] private float minDistance = 0.05f;

        [Tooltip("Scaling factor at closest approach (0.1 means 10x slower movement for sub-mm precision).")]
        [SerializeField] private float minScaleFactor = 0.1f;

        [Tooltip("Smoothing factor for precision position interpolation.")]
        [SerializeField] private float positionLerpRate = 25f;

        [Tooltip("Smoothing factor for rotation interpolation.")]
        [SerializeField] private float rotationLerpRate = 20f;

        [Header("Gizmo Settings")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private float gizmoAxisLength = 0.08f;

        // Internal State
        private bool isWorkspaceLocked;
        private Vector3 lockedAnchorPosition;
        private Quaternion lockedAnchorRotation;

        private Vector3 rawRightPrevPosition;
        private Vector3 accumulatedPrecisionLocalPosition;
        private Vector3 smoothedPrecisionPosition;
        private Quaternion smoothedPrecisionRotation;
        private float currentScaleFactor = 1.0f;
        private bool isFirstFrame = true;

        // Public Properties
        public Vector3 CurrentPrecisionPosition => smoothedPrecisionPosition;
        public Quaternion CurrentPrecisionRotation => smoothedPrecisionRotation;
        public bool IsWorkspaceLocked => isWorkspaceLocked;
        public float CurrentScaleFactor => currentScaleFactor;
        public float HandDistance { get; private set; }

        private void OnEnable()
        {
            EnableAction(leftPositionAction);
            EnableAction(leftRotationAction);
            EnableAction(leftGripAction);
            EnableAction(rightPositionAction);
            EnableAction(rightRotationAction);

            isFirstFrame = true;
        }

        private void OnDisable()
        {
            DisableAction(leftPositionAction);
            DisableAction(leftRotationAction);
            DisableAction(leftGripAction);
            DisableAction(rightPositionAction);
            DisableAction(rightRotationAction);
        }

        private void EnableAction(InputActionProperty property)
        {
            if (property.action != null)
            {
                property.action.Enable();
            }
        }

        private void DisableAction(InputActionProperty property)
        {
            if (property.action != null)
            {
                property.action.Disable();
            }
        }

        private void Update()
        {
            Vector3 rawLeftPos = GetLeftHandPosition();
            Quaternion rawLeftRot = GetLeftHandRotation();
            Vector3 rawRightPos = GetRightHandPosition();
            Quaternion rawRightRot = GetRightHandRotation();

            // Evaluate Grip for Workspace Frame Lock
            bool gripPressed = IsGripActive();
            if (gripPressed && !isWorkspaceLocked)
            {
                // Lock anchor in world space
                isWorkspaceLocked = true;
                lockedAnchorPosition = rawLeftPos;
                lockedAnchorRotation = rawLeftRot;
            }
            else if (!gripPressed && isWorkspaceLocked)
            {
                // Unlock anchor to follow left hand freely
                isWorkspaceLocked = false;
            }

            Vector3 activeAnchorPos = isWorkspaceLocked ? lockedAnchorPosition : rawLeftPos;
            Quaternion activeAnchorRot = isWorkspaceLocked ? lockedAnchorRotation : rawLeftRot;

            // Euclidean distance between controller positions
            HandDistance = Vector3.Distance(rawLeftPos, rawRightPos);

            // Compute Precision Scaling Factor
            if (HandDistance >= precisionDistanceThreshold)
            {
                currentScaleFactor = 1.0f;
            }
            else if (HandDistance <= minDistance)
            {
                currentScaleFactor = minScaleFactor;
            }
            else
            {
                float t = (HandDistance - minDistance) / (precisionDistanceThreshold - minDistance);
                // Smooth cosine curve for natural non-fatiguing transition
                float smoothT = 0.5f * (1.0f - Mathf.Cos(t * Mathf.PI));
                currentScaleFactor = Mathf.Lerp(minScaleFactor, 1.0f, smoothT);
            }

            if (isFirstFrame)
            {
                rawRightPrevPosition = rawRightPos;
                Matrix4x4 anchorMatrix = Matrix4x4.TRS(activeAnchorPos, activeAnchorRot, Vector3.one);
                accumulatedPrecisionLocalPosition = anchorMatrix.inverse.MultiplyPoint3x4(rawRightPos);
                smoothedPrecisionPosition = rawRightPos;
                smoothedPrecisionRotation = rawRightRot;
                isFirstFrame = false;
                return;
            }

            // Raw delta translation in world space
            Vector3 rawDeltaWorld = rawRightPos - rawRightPrevPosition;
            rawRightPrevPosition = rawRightPos;

            // Transform delta into active anchor's local coordinates
            Vector3 deltaLocal = Quaternion.Inverse(activeAnchorRot) * rawDeltaWorld;

            // Apply multi-scale precision scaling factor to local movement
            accumulatedPrecisionLocalPosition += deltaLocal * currentScaleFactor;

            // Convert back to world space
            Vector3 targetWorldPosition = activeAnchorPos + (activeAnchorRot * accumulatedPrecisionLocalPosition);
            Quaternion targetWorldRotation = rawRightRot;

            // Non-allocating smooth interpolation for zero jitter
            float dt = Time.deltaTime;
            smoothedPrecisionPosition = Vector3.Lerp(smoothedPrecisionPosition, targetWorldPosition, 1f - Mathf.Exp(-positionLerpRate * dt));
            smoothedPrecisionRotation = Quaternion.Slerp(smoothedPrecisionRotation, targetWorldRotation, 1f - Mathf.Exp(-rotationLerpRate * dt));

            // Apply directly to manipulated target if designated
            if (manipulatedTarget != null)
            {
                manipulatedTarget.position = smoothedPrecisionPosition;
                manipulatedTarget.rotation = smoothedPrecisionRotation;
            }
        }

        private Vector3 GetLeftHandPosition()
        {
            if (leftPositionAction.action != null && leftPositionAction.action.enabled)
            {
                return leftPositionAction.action.ReadValue<Vector3>();
            }
            return leftHandAnchor != null ? leftHandAnchor.position : transform.position;
        }

        private Quaternion GetLeftHandRotation()
        {
            if (leftRotationAction.action != null && leftRotationAction.action.enabled)
            {
                return leftRotationAction.action.ReadValue<Quaternion>();
            }
            return leftHandAnchor != null ? leftHandAnchor.rotation : Quaternion.identity;
        }

        private Vector3 GetRightHandPosition()
        {
            if (rightPositionAction.action != null && rightPositionAction.action.enabled)
            {
                return rightPositionAction.action.ReadValue<Vector3>();
            }
            return rightHandTool != null ? rightHandTool.position : (transform.position + transform.forward * 0.3f);
        }

        private Quaternion GetRightHandRotation()
        {
            if (rightRotationAction.action != null && rightRotationAction.action.enabled)
            {
                return rightRotationAction.action.ReadValue<Quaternion>();
            }
            return rightHandTool != null ? rightHandTool.rotation : Quaternion.identity;
        }

        private bool IsGripActive()
        {
            if (leftGripAction.action != null && leftGripAction.action.enabled)
            {
                return leftGripAction.action.ReadValue<float>() > 0.5f;
            }
            return false;
        }

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;

            Vector3 leftPos = leftHandAnchor != null ? leftHandAnchor.position : transform.position;
            Quaternion leftRot = leftHandAnchor != null ? leftHandAnchor.rotation : Quaternion.identity;

            // Anchor frame (Blue sphere with axes)
            Gizmos.color = isWorkspaceLocked ? Color.red : Color.cyan;
            Gizmos.DrawWireSphere(leftPos, 0.03f);
            DrawAxes(leftPos, leftRot, gizmoAxisLength);

            // Precision tool frame (Green sphere with axes)
            Vector3 toolPos = Application.isPlaying ? smoothedPrecisionPosition : (rightHandTool != null ? rightHandTool.position : transform.position);
            Quaternion toolRot = Application.isPlaying ? smoothedPrecisionRotation : (rightHandTool != null ? rightHandTool.rotation : Quaternion.identity);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(toolPos, 0.015f);
            DrawAxes(toolPos, toolRot, gizmoAxisLength * 0.75f);

            // Connecting vector
            Gizmos.color = Color.Lerp(Color.yellow, Color.magenta, currentScaleFactor);
            Gizmos.DrawLine(leftPos, toolPos);
        }

        private void DrawAxes(Vector3 origin, Quaternion orientation, float length)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(origin, orientation * Vector3.right * length);
            Gizmos.color = Color.green;
            Gizmos.DrawRay(origin, orientation * Vector3.up * length);
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(origin, orientation * Vector3.forward * length);
        }
    }
}
