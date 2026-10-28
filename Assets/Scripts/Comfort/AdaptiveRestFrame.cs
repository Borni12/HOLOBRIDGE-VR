using UnityEngine;

namespace AegisSpatial.Comfort
{
    /// <summary>
    /// Dynamic Bio-Adaptive Rest Frame for cybersickness reduction and visual-vestibular conflict mitigation.
    /// Tracks instantaneous head angular and linear velocity in real time.
    /// Stabilizes an overlay grid to an earth-referenced artificial horizon (zero roll) and dynamically
    /// fades in a visual rest frame via MaterialPropertyBlock to maintain locked 90 FPS on standalone Meta Quest.
    /// </summary>
    [DisallowMultipleComponent]
    public class AdaptiveRestFrame : MonoBehaviour
    {
        [Header("Camera & Visual Targets")]
        [Tooltip("Main XR Camera. If unassigned, defaults to Camera.main.")]
        [SerializeField] private Camera xrCamera;

        [Tooltip("Renderer displaying the horizon grid overlay / vignette.")]
        [SerializeField] private Renderer restFrameRenderer;

        [Tooltip("Optional CanvasGroup if a screen-space UI vignette is used alongside or instead of a mesh.")]
        [SerializeField] private CanvasGroup optionalCanvasGroup;

        [Header("Kinematic Activation Thresholds")]
        [Tooltip("Angular velocity threshold in degrees/sec above which the rest frame activates.")]
        [SerializeField] private float angularThreshold = 40.0f;

        [Tooltip("Linear velocity threshold in meters/sec above which the rest frame activates.")]
        [SerializeField] private float linearThreshold = 1.5f;

        [Header("Visual Dynamics")]
        [Tooltip("Maximum opacity for the comfort frame.")]
        [Range(0f, 1f)]
        [SerializeField] private float maxAlpha = 0.65f;

        [Tooltip("Fade-in rate when motion exceeds threshold.")]
        [SerializeField] private float fadeInSpeed = 6.0f;

        [Tooltip("Fade-out / decay rate when motion ceases.")]
        [SerializeField] private float fadeOutSpeed = 3.5f;

        [Tooltip("Distance in front of the camera to position the earth-anchored grid.")]
        [SerializeField] private float forwardDistance = 1.2f;

        [Header("Shader Property Overrides")]
        [SerializeField] private string alphaPropertyName = "_Alpha";
        [SerializeField] private string colorPropertyName = "_BaseColor";

        // Cached Property IDs for zero runtime allocations
        private int alphaPropertyId;
        private int colorPropertyId;
        private MaterialPropertyBlock propBlock;

        // Kinematic Tracking State
        private Vector3 prevCameraPosition;
        private Quaternion prevCameraRotation;
        private float currentAlpha;
        private float targetAlpha;
        private bool isInitialized;

        // Public Diagnostics
        public float CurrentAngularVelocityDegSec { get; private set; }
        public float CurrentLinearVelocityMS { get; private set; }
        public float CurrentAlpha => currentAlpha;
        public bool IsRestFrameActive => currentAlpha > 0.01f;

        private void Awake()
        {
            if (xrCamera == null)
            {
                xrCamera = Camera.main;
            }

            if (restFrameRenderer == null)
            {
                restFrameRenderer = GetComponent<Renderer>();
            }

            // Cache shader property IDs
            alphaPropertyId = Shader.PropertyToID(alphaPropertyName);
            colorPropertyId = Shader.PropertyToID(colorPropertyName);
            propBlock = new MaterialPropertyBlock();

            ApplyAlpha(0f);
        }

        private void OnEnable()
        {
            isInitialized = false;
            currentAlpha = 0f;
            targetAlpha = 0f;
            ApplyAlpha(0f);
        }

        private void LateUpdate()
        {
            if (xrCamera == null)
            {
                xrCamera = Camera.main;
                if (xrCamera == null) return;
            }

            Transform camTransform = xrCamera.transform;
            Vector3 camPosition = camTransform.position;
            Quaternion camRotation = camTransform.rotation;
            float dt = Time.deltaTime;

            if (dt <= 0.00001f) return;

            if (!isInitialized)
            {
                prevCameraPosition = camPosition;
                prevCameraRotation = camRotation;
                isInitialized = true;
                return;
            }

            // Calculate instantaneous linear velocity (m/s)
            float linearDistance = Vector3.Distance(camPosition, prevCameraPosition);
            CurrentLinearVelocityMS = linearDistance / dt;

            // Calculate instantaneous angular velocity (deg/sec)
            float angularAngle = Quaternion.Angle(camRotation, prevCameraRotation);
            CurrentAngularVelocityDegSec = angularAngle / dt;

            prevCameraPosition = camPosition;
            prevCameraRotation = camRotation;

            // Evaluate motion excess
            float angularExcess = Mathf.Max(0f, (CurrentAngularVelocityDegSec - angularThreshold) / Mathf.Max(1f, angularThreshold));
            float linearExcess = Mathf.Max(0f, (CurrentLinearVelocityMS - linearThreshold) / Mathf.Max(0.5f, linearThreshold));
            float motionFactor = Mathf.Clamp01(angularExcess + linearExcess);

            targetAlpha = motionFactor > 0.001f ? (motionFactor * maxAlpha) : 0f;

            // Smooth fade transitions
            float transitionSpeed = targetAlpha > currentAlpha ? fadeInSpeed : fadeOutSpeed;
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, transitionSpeed * dt);

            // Update artificial horizon transform orientation
            UpdateRestFramePose(camTransform);

            // Apply Alpha to material and canvas
            ApplyAlpha(currentAlpha);
        }

        /// <summary>
        /// Aligns the rest frame to an earth-referenced artificial horizon (zero head roll).
        /// Eliminates visual-vestibular conflict between the otolith organs and visual cues.
        /// </summary>
        private void UpdateRestFramePose(Transform camTransform)
        {
            Vector3 camForward = camTransform.forward;
            // Project onto horizontal plane to completely neutralize roll and excessive pitch
            camForward.y = 0f;

            Quaternion horizonRotation;
            if (camForward.sqrMagnitude > 0.001f)
            {
                horizonRotation = Quaternion.LookRotation(camForward.normalized, Vector3.up);
            }
            else
            {
                horizonRotation = Quaternion.identity;
            }

            // Position anchored slightly in front of user's eye level
            Vector3 targetPosition = camTransform.position + (horizonRotation * Vector3.forward * forwardDistance);
            transform.position = targetPosition;
            transform.rotation = horizonRotation;
        }

        private void ApplyAlpha(float alpha)
        {
            // Apply via MaterialPropertyBlock to prevent runtime material instancing
            if (restFrameRenderer != null)
            {
                restFrameRenderer.GetPropertyBlock(propBlock);

                // Update float _Alpha
                propBlock.SetFloat(alphaPropertyId, alpha);

                // Also update Color _BaseColor alpha channel if applicable
                if (restFrameRenderer.sharedMaterial != null && restFrameRenderer.sharedMaterial.HasProperty(colorPropertyId))
                {
                    Color baseCol = restFrameRenderer.sharedMaterial.GetColor(colorPropertyId);
                    baseCol.a = alpha;
                    propBlock.SetColor(colorPropertyId, baseCol);
                }

                restFrameRenderer.SetPropertyBlock(propBlock);
                restFrameRenderer.enabled = alpha > 0.002f;
            }

            if (optionalCanvasGroup != null)
            {
                optionalCanvasGroup.alpha = alpha;
            }
        }
    }
}
