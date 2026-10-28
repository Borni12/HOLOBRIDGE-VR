using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace AegisSpatial.Interaction
{
    /// <summary>
    /// Volumetric Geometric Snapping for sub-millimeter precision VR manipulation.
    /// Uses non-allocating spatial queries (Physics.OverlapSphereNonAlloc) and distance-weighted
    /// attraction fields with smoothstep/exponential decay to attract the tool tip to geometry anchors.
    /// Fires haptic impulses on snap state transitions without generating garbage collection spikes.
    /// </summary>
    [DisallowMultipleComponent]
    public class VolumetricSnapping : MonoBehaviour
    {
        [Header("Snapping Parameters")]
        [Tooltip("Layer mask containing candidate geometry snapping anchors.")]
        [SerializeField] private LayerMask snappingLayer = ~0;

        [Tooltip("Radius within which geometry vertices/colliders exert magnetic attraction.")]
        [SerializeField] private float snapRadius = 0.05f;

        [Tooltip("Threshold distance where magnetic attraction smoothly breaks and releases to free manipulation.")]
        [SerializeField] private float breakThreshold = 0.08f;

        [Tooltip("Strength multiplier of the attraction field.")]
        [Range(0.1f, 1.0f)]
        [SerializeField] private float attractionStrength = 0.85f;

        [Header("Haptics Configuration")]
        [SerializeField] private bool enableHaptics = true;
        [SerializeField] private XRNode targetControllerNode = XRNode.RightHand;
        [Range(0f, 1f)]
        [SerializeField] private float hapticAmplitude = 0.65f;
        [SerializeField] private float hapticDuration = 0.04f;

        [Header("Debug Visualizer")]
        [SerializeField] private bool drawDebugGizmos = true;
        [SerializeField] private Color snapFieldColor = new Color(0f, 1f, 0.4f, 0.25f);
        [SerializeField] private Color breakFieldColor = new Color(1f, 0.3f, 0f, 0.15f);

        // Pre-allocated non-allocating collision buffer (strict zero-allocation in hot path)
        private const int MaxColliders = 16;
        private readonly Collider[] hitBuffer = new Collider[MaxColliders];

        // State Tracking
        private bool isSnapped;
        private Vector3 currentSnappedPosition;
        private Vector3 currentAnchorPoint;
        private InputDevice cachedXRDevice;

        /// <summary>
        /// True if the tool tip is currently captured inside an active geometry snapping basin.
        /// </summary>
        public bool IsSnapped => isSnapped;

        /// <summary>
        /// Current magnetic anchor position in world space.
        /// </summary>
        public Vector3 CurrentAnchorPoint => currentAnchorPoint;

        private void Awake()
        {
            RefreshXRDevice();
        }

        private void OnEnable()
        {
            RefreshXRDevice();
            isSnapped = false;
        }

        private void RefreshXRDevice()
        {
            cachedXRDevice = InputDevices.GetDeviceAtXRNode(targetControllerNode);
        }

        /// <summary>
        /// Evaluates non-allocating spatial attraction around rawPosition and returns the snapped coordinate.
        /// </summary>
        /// <param name="rawPosition">Raw tool tip position in world space.</param>
        /// <returns>Attracted / Snapped position.</returns>
        public Vector3 GetSnappedPosition(Vector3 rawPosition)
        {
            // Non-allocating physics query
            int hitCount = Physics.OverlapSphereNonAlloc(rawPosition, breakThreshold, hitBuffer, snappingLayer, QueryTriggerInteraction.Ignore);

            if (hitCount == 0)
            {
                UpdateSnapState(false);
                currentSnappedPosition = rawPosition;
                return rawPosition;
            }

            // Find closest candidate anchor point
            float closestDistanceSqr = float.MaxValue;
            Vector3 bestAnchor = rawPosition;
            bool foundValidAnchor = false;

            for (int i = 0; i < hitCount; i++)
            {
                Collider col = hitBuffer[i];
                if (col == null) continue;

                Vector3 candidatePoint = col.ClosestPoint(rawPosition);
                float distSqr = (candidatePoint - rawPosition).sqrMagnitude;

                if (distSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distSqr;
                    bestAnchor = candidatePoint;
                    foundValidAnchor = true;
                }
            }

            // Clear hitBuffer references immediately to prevent memory leaks
            for (int i = 0; i < hitCount; i++)
            {
                hitBuffer[i] = null;
            }

            if (!foundValidAnchor)
            {
                UpdateSnapState(false);
                currentSnappedPosition = rawPosition;
                return rawPosition;
            }

            float currentDistance = Mathf.Sqrt(closestDistanceSqr);
            currentAnchorPoint = bestAnchor;

            // Break condition
            if (currentDistance > breakThreshold)
            {
                UpdateSnapState(false);
                currentSnappedPosition = rawPosition;
                return rawPosition;
            }

            // If within breakThreshold but outside snapRadius, apply smooth release
            if (currentDistance <= snapRadius)
            {
                // Full or high magnetism with smoothstep falloff
                float t = currentDistance / snapRadius;
                // High magnetism near center (t=0 -> factor=1), smoothly decaying towards snapRadius
                float magneticWeight = Mathf.Cos(t * (Mathf.PI * 0.5f)) * attractionStrength;
                currentSnappedPosition = Vector3.Lerp(rawPosition, bestAnchor, magneticWeight);
                UpdateSnapState(true);
            }
            else
            {
                // In break-release corridor [snapRadius, breakThreshold]
                float releaseT = (currentDistance - snapRadius) / (breakThreshold - snapRadius);
                float releaseWeight = (1.0f - releaseT) * (1.0f - releaseT) * (attractionStrength * 0.5f);
                currentSnappedPosition = Vector3.Lerp(rawPosition, bestAnchor, releaseWeight);
                UpdateSnapState(false);
            }

            return currentSnappedPosition;
        }

        private void UpdateSnapState(bool newSnappedState)
        {
            if (newSnappedState && !isSnapped)
            {
                // Transition to SNAPPED: trigger haptic feedback
                TriggerSnapHaptic();
            }

            isSnapped = newSnappedState;
        }

        private void TriggerSnapHaptic()
        {
            if (!enableHaptics) return;

            if (!cachedXRDevice.isValid)
            {
                RefreshXRDevice();
            }

            if (cachedXRDevice.isValid)
            {
                uint channel = 0;
                cachedXRDevice.SendHapticImpulse(channel, hapticAmplitude, hapticDuration);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos) return;

            Vector3 origin = transform.position;

            // Draw Snap Attraction Radius (Inner sphere)
            Gizmos.color = snapFieldColor;
            Gizmos.DrawWireSphere(origin, snapRadius);

            // Draw Break Threshold Radius (Outer sphere)
            Gizmos.color = breakFieldColor;
            Gizmos.DrawWireSphere(origin, breakThreshold);

            if (isSnapped)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(origin, currentAnchorPoint);
                Gizmos.DrawSphere(currentAnchorPoint, 0.008f);
            }
        }
    }
}
