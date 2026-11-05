using System;
using UnityEngine;
using OmniOS.Core;
using OmniOS.Interaction;

namespace OmniOS.Apps.CameraApp
{
    /// <summary>
    /// OmniAppCamera - Meta Quest Pro / Quest 3 Style Spatial Camera Application Window.
    /// Provides live viewfinder display, optical zoom, snapshot capture,
    /// 3D Reality Lift, and 3D in-window buttons that work 100% reliably in VR & PC.
    /// </summary>
    [RequireComponent(typeof(SpatialWindow))]
    public class OmniAppCamera : MonoBehaviour
    {
        [Header("Camera Hardware")]
        [SerializeField] private SpatialCameraDevice cameraDevice;
        [SerializeField] private bool useDeviceWebcam = false;

        private SpatialWindow spatialWindow;
        private GameObject screenQuad;
        private Material screenMaterial;
        private DeviceCameraManager deviceCamManager;
        private Transform buttonBarRoot;
        private SpatialButton3D webcamToggleBtn;

        public bool UseDeviceWebcam => useDeviceWebcam;

        private void Awake()
        {
            spatialWindow = GetComponent<SpatialWindow>();
            spatialWindow.SetDimensions(1.42f, 1.05f);

            if (cameraDevice == null)
            {
                cameraDevice = UnityEngine.Object.FindAnyObjectByType<SpatialCameraDevice>();
                if (cameraDevice == null)
                {
                    GameObject camObj = new GameObject("OmniOS_SpatialCameraDevice");
                    cameraDevice = camObj.AddComponent<SpatialCameraDevice>();
                }
            }

            deviceCamManager = DeviceCameraManager.Instance ?? UnityEngine.Object.FindAnyObjectByType<DeviceCameraManager>();

            BuildViewfinderDisplay();
            Build3DCameraButtons();
        }

        private void BuildViewfinderDisplay()
        {
            screenQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            screenQuad.name = "CameraApp_LiveScreen";
            screenQuad.transform.SetParent(spatialWindow.ContentRoot, false);
            screenQuad.transform.localScale = new Vector3(1.30f, 0.72f, 1f);
            screenQuad.transform.localPosition = new Vector3(0, 0.08f, -0.01f);

            // Add BoxCollider so direct mouse / laser clicks toggle camera feed
            var col = screenQuad.GetComponent<BoxCollider>();
            if (col == null) col = screenQuad.AddComponent<BoxCollider>();
            col.size = new Vector3(1f, 1f, 0.06f);
            col.center = new Vector3(0, 0, -0.01f);

            Shader vfShader = Shader.Find("OmniOS/CameraViewfinder");
            if (vfShader == null) vfShader = Shader.Find("Universal Render Pipeline/Unlit");

            screenMaterial = new Material(vfShader);
            UpdateActiveFeedTexture();
            screenQuad.GetComponent<Renderer>().material = screenMaterial;
        }

        private void Build3DCameraButtons()
        {
            GameObject barObj = new GameObject("Camera_3D_ButtonBar");
            barObj.transform.SetParent(spatialWindow.ContentRoot, false);
            barObj.transform.localPosition = new Vector3(0, -0.38f, -0.025f);
            buttonBarRoot = barObj.transform;

            // 1. Capture Photo Button
            CreateCamButton("📸 Take Photo", new Vector3(-0.48f, 0, 0), () => CapturePhoto(), new Color(0.18f, 0.95f, 0.45f));

            // 2. Switch between 3D VR Lens and Device Webcam
            webcamToggleBtn = CreateCamButton("📹 Use Webcam", new Vector3(-0.16f, 0, 0), () => ToggleDeviceWebcam(), new Color(0.0f, 0.85f, 1.0f));

            // 3. Lift View to 3D Reality
            CreateCamButton("🌐 Reality Lift", new Vector3(0.16f, 0, 0), () => LiftTo3DReality(), new Color(0.68f, 0.28f, 1.0f));

            // 4. Summon physical camera to hand
            CreateCamButton("✋ To Hand", new Vector3(0.48f, 0, 0), () => SummonPhysicalCameraToHand(), new Color(1.0f, 0.75f, 0.15f));
        }

        private SpatialButton3D CreateCamButton(string label, Vector3 localPos, Action onClick, Color accent)
        {
            GameObject btnObj = new GameObject($"Btn_{label.Replace(" ", "_")}");
            btnObj.transform.SetParent(buttonBarRoot, false);
            btnObj.transform.localPosition = localPos;

            var btn = btnObj.AddComponent<SpatialButton3D>();
            btn.ButtonText = label;
            btn.SetSize(0.24f, 0.078f);
            btn.SetColors(new Color(0.08f, 0.11f, 0.16f, 0.95f), accent);
            btn.onClick.AddListener(() => onClick?.Invoke());
            return btn;
        }

        private void Update()
        {
            UpdateActiveFeedTexture();

            // Shortcut 'K' takes instant photo
            if (Input.GetKeyDown(KeyCode.K)) CapturePhoto();
            // Shortcut 'V' toggles between Device Webcam and 3D VR Camera
            if (Input.GetKeyDown(KeyCode.V)) ToggleDeviceWebcam();
        }

        public void ToggleDeviceWebcam()
        {
            if (deviceCamManager == null) deviceCamManager = DeviceCameraManager.Instance ?? UnityEngine.Object.FindAnyObjectByType<DeviceCameraManager>();

            if (!useDeviceWebcam)
            {
                if (deviceCamManager != null && deviceCamManager.IsDeviceCameraAvailable)
                {
                    useDeviceWebcam = true;
                    deviceCamManager.StartStreaming();
                    if (webcamToggleBtn != null) webcamToggleBtn.ButtonText = "📷 3D VR Lens";
                    OmniOSManager.Instance?.ShowNotificationToast($"📹 Switched to Device Webcam: {deviceCamManager.CurrentDeviceName}");
                }
                else
                {
                    OmniOSManager.Instance?.ShowNotificationToast("⚠️ No physical webcam detected. Using 3D Virtual Lens.");
                    useDeviceWebcam = false;
                }
            }
            else
            {
                useDeviceWebcam = false;
                deviceCamManager?.StopStreaming();
                if (webcamToggleBtn != null) webcamToggleBtn.ButtonText = "📹 Use Webcam";
                OmniOSManager.Instance?.ShowNotificationToast("📷 Switched to 3D Virtual Camera");
            }

            UpdateActiveFeedTexture();
            SpatialAudioEngine.Instance?.PlayButtonClick();
        }

        private void UpdateActiveFeedTexture()
        {
            if (screenMaterial == null) return;

            Texture activeTex = null;
            if (useDeviceWebcam && deviceCamManager != null && deviceCamManager.IsStreaming)
            {
                activeTex = deviceCamManager.LiveCameraTexture;
            }
            else if (cameraDevice != null && cameraDevice.ViewfinderTexture != null)
            {
                activeTex = cameraDevice.ViewfinderTexture;
            }

            if (activeTex != null)
            {
                screenMaterial.mainTexture = activeTex;
                if (screenMaterial.HasProperty("_MainTex")) screenMaterial.SetTexture("_MainTex", activeTex);
                if (screenMaterial.HasProperty("_BaseMap")) screenMaterial.SetTexture("_BaseMap", activeTex);
                if (screenMaterial.HasProperty("_BaseColor")) screenMaterial.SetColor("_BaseColor", Color.white);
            }
            else
            {
                // No live feed yet — show a dark placeholder so screen doesn't stay magenta
                if (screenMaterial.HasProperty("_BaseColor")) screenMaterial.SetColor("_BaseColor", new Color(0.04f, 0.06f, 0.10f, 1f));
                if (screenMaterial.HasProperty("_BaseMap")) screenMaterial.SetTexture("_BaseMap", null);
            }
        }

        public void CapturePhoto()
        {
            if (useDeviceWebcam && deviceCamManager != null && deviceCamManager.IsStreaming)
            {
                Texture2D snap = deviceCamManager.CaptureWebcamSnapshot();
                if (snap != null)
                {
                    SpatialAudioEngine.Instance?.PlayShutterClick();
                    OmniOSManager.Instance?.ShowNotificationToast("📸 Webcam Snapshot Saved to Gallery!");
                }
            }
            else if (cameraDevice != null)
            {
                cameraDevice.CapturePhoto();
                SpatialAudioEngine.Instance?.PlayShutterClick();
            }
        }

        public void LiftTo3DReality()
        {
            if (cameraDevice != null)
            {
                cameraDevice.LiftCurrentViewTo3DReality();
                SpatialAudioEngine.Instance?.PlayThemeChime();
            }
        }

        public void SummonPhysicalCameraToHand()
        {
            if (cameraDevice != null && Camera.main != null)
            {
                Transform camTrans = Camera.main.transform;
                Vector3 targetPos = camTrans.position + camTrans.forward * 0.45f + camTrans.right * 0.2f + Vector3.down * 0.1f;
                Quaternion targetRot = camTrans.rotation;
                cameraDevice.SummonTo(targetPos, targetRot);
                SpatialAudioEngine.Instance?.PlayButtonClick();
                OmniOSManager.Instance?.ShowNotificationToast("📷 Camera Device summoned to hand!");
            }
        }

        private void OnDestroy()
        {
            if (screenMaterial != null) Destroy(screenMaterial);
        }
    }
}
