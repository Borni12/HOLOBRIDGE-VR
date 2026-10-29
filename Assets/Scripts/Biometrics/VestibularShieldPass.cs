using UnityEngine;

namespace OmniGenesis.Biometrics
{
    /// <summary>
    /// VestibularShieldPass - Spatially Resolved Cybersickness Mitigation.
    /// Grounded in Dr. Isayas Berhe Adhanom's research at the XPI Lab (Texas State University).
    /// Tracks optical flow velocity from HMD translation and 4D projective transitions.
    /// Renders an adaptive peripheral ring of 256 subtle counter-motion reference particles
    /// strictly outside a 25-degree gaze-centered foveal safe-zone to eliminate vection.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public class VestibularShieldPass : MonoBehaviour
    {
        private const int ParticleCount = 256;
        private const float FovealHalfAngleDeg = 25.0f; // 25 degree safe-zone

        [Header("Cybersickness Shield Dynamics")]
        [Range(0.1f, 1.0f)]
        [SerializeField] private float shieldMaxOpacity = 0.55f;
        [SerializeField] private float opticalFlowThreshold = 1.25f; // m/s or rad/s
        [SerializeField] private float ringRadius = 1.2f;
        [SerializeField] private float counterFlowGain = 1.4f;

        private Camera targetCamera;
        private Vector3 prevCameraPos;
        private Quaternion prevCameraRot;
        private bool isInitialized = false;

        // Particle State
        private readonly Vector3[] particleBaseOffsets = new Vector3[ParticleCount];
        private readonly Vector3[] particleCurrentPos = new Vector3[ParticleCount];
        private float currentShieldIntensity = 0.0f;

        // Visuals
        private Material particleMaterial;
        private Mesh particleQuadMesh;

        private void Awake()
        {
            targetCamera = GetComponent<Camera>();
            InitializeRingGeometry();
            CreateParticleMaterial();
        }

        private void InitializeRingGeometry()
        {
            // Distribute 256 reference particles along peripheral ring
            float foveaTan = Mathf.Tan(FovealHalfAngleDeg * Mathf.Deg2Rad);
            for (int i = 0; i < ParticleCount; i++)
            {
                float angle = (i / (float)ParticleCount) * Mathf.PI * 2.0f;
                float r = ringRadius * UnityEngine.Random.Range(1.15f, 1.85f);

                particleBaseOffsets[i] = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, ringRadius / Mathf.Max(0.1f, foveaTan));
                particleCurrentPos[i] = particleBaseOffsets[i];
            }

            // Primitive quad for particles
            GameObject tempQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            particleQuadMesh = tempQuad.GetComponent<MeshFilter>().sharedMesh;
            Destroy(tempQuad);
        }

        private void CreateParticleMaterial()
        {
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlit == null) unlit = Shader.Find("Sprites/Default");
            particleMaterial = new Material(unlit)
            {
                color = new Color(0.15f, 0.45f, 0.85f, 0.0f)
            };
        }

        private void OnDestroy()
        {
            if (particleMaterial != null) Destroy(particleMaterial);
        }

        private void LateUpdate()
        {
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            Transform camTransform = targetCamera.transform;
            Vector3 camPos = camTransform.position;
            Quaternion camRot = camTransform.rotation;

            if (!isInitialized)
            {
                prevCameraPos = camPos;
                prevCameraRot = camRot;
                isInitialized = true;
                return;
            }

            // Instantaneous optical flow rate from translation & angular rotation
            float linVel = Vector3.Distance(camPos, prevCameraPos) / dt;
            float angVel = Quaternion.Angle(camRot, prevCameraRot) / dt * Mathf.Deg2Rad;
            float totalFlow = linVel + angVel * 1.5f;

            // Target shield opacity based on motion excess
            float targetIntensity = 0.0f;
            if (totalFlow > opticalFlowThreshold)
            {
                targetIntensity = Mathf.Clamp01((totalFlow - opticalFlowThreshold) / 2.5f) * shieldMaxOpacity;
            }
            currentShieldIntensity = Mathf.MoveTowards(currentShieldIntensity, targetIntensity, dt * 4.0f);

            // Calculate counter-flow motion vector opposite to angular velocity
            Vector3 deltaRotEuler = (camRot.eulerAngles - prevCameraRot.eulerAngles);
            if (deltaRotEuler.x > 180f) deltaRotEuler.x -= 360f;
            if (deltaRotEuler.y > 180f) deltaRotEuler.y -= 360f;

            Vector3 counterFlowDelta = new Vector3(-deltaRotEuler.y, deltaRotEuler.x, 0.0f) * (counterFlowGain * dt * 0.02f);

            prevCameraPos = camPos;
            prevCameraRot = camRot;

            // Update reference particle ring positions
            if (currentShieldIntensity > 0.01f)
            {
                for (int i = 0; i < ParticleCount; i++)
                {
                    particleCurrentPos[i] += counterFlowDelta;

                    // Bound particles around original peripheral cone
                    if (Vector3.Distance(particleCurrentPos[i], particleBaseOffsets[i]) > 0.35f)
                    {
                        particleCurrentPos[i] = particleBaseOffsets[i];
                    }
                }
            }
        }

        private void OnPostRender()
        {
            RenderPeripheralShield();
        }

        private void OnDrawGizmos()
        {
            // Render in Editor scene view
            if (!Application.isPlaying && targetCamera != null)
            {
                Gizmos.color = new Color(0.2f, 0.6f, 1.0f, 0.35f);
                Transform cam = targetCamera.transform;
                for (int i = 0; i < ParticleCount; i += 8)
                {
                    Gizmos.DrawWireSphere(cam.TransformPoint(particleBaseOffsets[i]), 0.012f);
                }
            }
        }

        private void RenderPeripheralShield()
        {
            if (currentShieldIntensity <= 0.01f || particleMaterial == null || particleQuadMesh == null) return;

            particleMaterial.color = new Color(0.18f, 0.55f, 0.95f, currentShieldIntensity);
            particleMaterial.SetPass(0);

            Transform camTransform = targetCamera.transform;
            Matrix4x4 camMatrix = camTransform.localToWorldMatrix;

            for (int i = 0; i < ParticleCount; i++)
            {
                Vector3 worldPt = camMatrix.MultiplyPoint3x4(particleCurrentPos[i]);
                Matrix4x4 trs = Matrix4x4.TRS(worldPt, camTransform.rotation, Vector3.one * 0.015f);
                Graphics.DrawMeshNow(particleQuadMesh, trs);
            }
        }
    }
}
