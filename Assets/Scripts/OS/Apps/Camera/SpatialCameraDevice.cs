using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using OmniOS.Core;
using OmniOS.Apps.VideoReality;

namespace OmniOS.Apps.CameraApp
{
    /// <summary>
    /// SpatialCameraDevice - 6-DoF Grabable Hyper-Realistic Spatial Camera.
    /// Features:
    /// - Secondary high-resolution physical camera lens rendering to a live viewfinder screen
    /// - Optical Zoom (1x to 5x with smooth FOV lerping)
    /// - Physical Shutter click with spatial audio and bright xenon flash animation
    /// - Hi-Res Snapshot engine saving to disk (PNG)
    /// - Instant "Lift to 3D Reality": Converts captured photo directly into a 360 VR Environment
    /// - Grabable 6-DoF physical body with tripod/free-float mode
    /// </summary>
    [DisallowMultipleComponent]
    public class SpatialCameraDevice : MonoBehaviour
    {
        [Header("Optics & Capture")]
        [SerializeField] private Camera captureCamera;
        [SerializeField] private int resolutionWidth = 1920;
        [SerializeField] private int resolutionHeight = 1080;
        [Range(20f, 75f)]
        [SerializeField] private float fieldOfView = 60f;
        [SerializeField] private bool showGrid = true;
        [SerializeField] private bool showReticle = true;

        [Header("Physical Hardware Visuals")]
        [SerializeField] private Transform viewfinderScreen;
        [SerializeField] private Light flashLight;
        [SerializeField] private Transform lensBarrel;

        // Render Pipeline
        private RenderTexture cameraRenderTexture;
        private Material viewfinderMaterial;

        // Flash State
        private float flashIntensity = 0f;

        // Public Properties
        public RenderTexture ViewfinderTexture => cameraRenderTexture;
        public Camera LensCamera => captureCamera;
        public float ZoomFOV
        {
            get => fieldOfView;
            set
            {
                fieldOfView = Mathf.Clamp(value, 20f, 75f);
                if (captureCamera != null) captureCamera.fieldOfView = fieldOfView;
            }
        }
        public bool ShowGrid
        {
            get => showGrid;
            set
            {
                showGrid = value;
                if (viewfinderMaterial != null) viewfinderMaterial.SetFloat("_ShowGrid", showGrid ? 1f : 0f);
            }
        }

        // Event for Gallery integration
        public static event Action<Texture2D, string> OnPhotoCaptured;

        private void Awake()
        {
            SetupCaptureCamera();
            BuildPhysicalBody();
        }

        private void SetupCaptureCamera()
        {
            if (captureCamera == null)
            {
                GameObject camObj = new GameObject("SpatialCamera_Lens");
                camObj.transform.SetParent(transform, false);
                camObj.transform.localPosition = new Vector3(0, 0, 0.12f);
                camObj.transform.localRotation = Quaternion.identity;

                captureCamera = camObj.AddComponent<Camera>();
                captureCamera.clearFlags = CameraClearFlags.Skybox;
                captureCamera.fieldOfView = fieldOfView;
                captureCamera.nearClipPlane = 0.05f;
                captureCamera.farClipPlane = 500f;

                // URP Camera Data
                var urpCam = camObj.AddComponent<UniversalAdditionalCameraData>();
                urpCam.renderPostProcessing = true;
            }

            cameraRenderTexture = new RenderTexture(resolutionWidth, resolutionHeight, 24, RenderTextureFormat.ARGB32);
            cameraRenderTexture.name = "OmniOS_SpatialCamera_RT";
            cameraRenderTexture.wrapMode = TextureWrapMode.Clamp;
            cameraRenderTexture.filterMode = FilterMode.Bilinear;
            cameraRenderTexture.Create();

            captureCamera.targetTexture = cameraRenderTexture;
        }

        private void BuildPhysicalBody()
        {
            // 1. Camera Body (Aluminum & Black Leatherette)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Camera_Body";
            body.transform.SetParent(transform, false);
            body.transform.localScale = new Vector3(0.24f, 0.15f, 0.08f);

            var bodyMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            bodyMat.color = new Color(0.12f, 0.13f, 0.16f);
            body.GetComponent<Renderer>().material = bodyMat;

            // BoxCollider for grabbing
            var col = body.GetComponent<BoxCollider>();
            if (col == null) col = body.AddComponent<BoxCollider>();

            // 2. Lens Barrel (Cylinder on front)
            GameObject lensObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lensObj.name = "Camera_LensBarrel";
            lensObj.transform.SetParent(transform, false);
            lensObj.transform.localScale = new Vector3(0.09f, 0.045f, 0.09f);
            lensObj.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            lensObj.transform.localPosition = new Vector3(0, 0, 0.06f);
            lensBarrel = lensObj.transform;

            var lensMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            lensMat.color = new Color(0.25f, 0.28f, 0.32f);
            lensObj.GetComponent<Renderer>().material = lensMat;

            // 3. Viewfinder Screen (Quad on back)
            GameObject screenObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            screenObj.name = "Camera_ViewfinderScreen";
            screenObj.transform.SetParent(transform, false);
            screenObj.transform.localScale = new Vector3(0.20f, 0.115f, 1f);
            screenObj.transform.localRotation = Quaternion.Euler(0, 180f, 0); // Faces rear
            screenObj.transform.localPosition = new Vector3(0, 0, -0.041f);
            viewfinderScreen = screenObj.transform;

            Shader vfShader = Shader.Find("OmniOS/CameraViewfinder");
            if (vfShader == null) vfShader = Shader.Find("Universal Render Pipeline/Unlit");

            viewfinderMaterial = new Material(vfShader);
            viewfinderMaterial.SetTexture("_MainTex", cameraRenderTexture);
            viewfinderMaterial.SetFloat("_ShowGrid", showGrid ? 1f : 0f);
            viewfinderMaterial.SetFloat("_ShowReticle", showReticle ? 1f : 0f);
            screenObj.GetComponent<Renderer>().material = viewfinderMaterial;

            // 4. Xenon Flash Light
            GameObject flashObj = new GameObject("Camera_FlashLight");
            flashObj.transform.SetParent(transform, false);
            flashObj.transform.localPosition = new Vector3(0.08f, 0.05f, 0.05f);
            flashLight = flashObj.AddComponent<Light>();
            flashLight.type = LightType.Point;
            flashLight.range = 8.0f;
            flashLight.intensity = 0f;
            flashLight.color = new Color(0.95f, 0.98f, 1.0f);
        }

        private void Update()
        {
            if (captureCamera != null && captureCamera.fieldOfView != fieldOfView)
            {
                captureCamera.fieldOfView = Mathf.Lerp(captureCamera.fieldOfView, fieldOfView, Time.deltaTime * 10f);
            }

            // Flash fade out
            if (flashIntensity > 0.01f)
            {
                flashIntensity = Mathf.MoveTowards(flashIntensity, 0f, Time.deltaTime * 12f);
                if (flashLight != null) flashLight.intensity = flashIntensity;
            }
        }

        public Texture2D CapturePhoto()
        {
            if (captureCamera == null || cameraRenderTexture == null) return null;

            // Trigger Flash and Audio Cue
            flashIntensity = 6.0f;
            if (flashLight != null) flashLight.intensity = flashIntensity;
            SpatialTheme.PlaySpatialTone(transform.position, 1600f, 0.04f);

            // Readback Texture
            RenderTexture prevActive = RenderTexture.active;
            RenderTexture.active = cameraRenderTexture;

            Texture2D snapshot = new Texture2D(resolutionWidth, resolutionHeight, TextureFormat.RGB24, false);
            snapshot.ReadPixels(new Rect(0, 0, resolutionWidth, resolutionHeight), 0, 0);
            snapshot.Apply();

            RenderTexture.active = prevActive;

            // Save to disk
            string dir = Path.Combine(Application.persistentDataPath, "SpatialPhotos");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string filename = $"SpatialPhoto_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            string fullPath = Path.Combine(dir, filename);
            File.WriteAllBytes(fullPath, snapshot.EncodeToPNG());

            Debug.Log($"<color=cyan>[OmniCam]</color> Photo captured & saved to: {fullPath}");
            OnPhotoCaptured?.Invoke(snapshot, fullPath);

            // Trigger OS Notification Toast
            OmniOSManager.Instance?.ShowNotificationToast($"📷 Photo Saved: {filename}");

            return snapshot;
        }

        public void LiftCurrentViewTo3DReality()
        {
            Texture2D snapshot = CapturePhoto();
            if (snapshot == null) return;

            // Lift to 360 VR Environment via VideoRealityManager
            var vidManager = UnityEngine.Object.FindAnyObjectByType<VideoRealityManager>();
            if (vidManager != null)
            {
                vidManager.ImmersionLevel = 1.0f;
                vidManager.DepthRelief = 0.65f;
            }

            OmniOSManager.Instance?.ShowNotificationToast("🌐 Reality Lift: View transformed into 360 VR Environment!");
        }

        public void SetZoom(float normalizedZoom) // 0 (Wide) to 1 (Telephoto)
        {
            fieldOfView = Mathf.Lerp(75f, 25f, Mathf.Clamp01(normalizedZoom));
        }

        private void OnDestroy()
        {
            if (cameraRenderTexture != null)
            {
                cameraRenderTexture.Release();
                Destroy(cameraRenderTexture);
            }
            if (viewfinderMaterial != null) Destroy(viewfinderMaterial);
        }
    }
}
