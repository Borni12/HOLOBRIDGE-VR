using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using OmniOS.Core;

namespace OmniOS.Interaction
{
    /// <summary>
    /// OmniVRHeadsetSimulator - Zero-Hardware VR Simulator & Stereoscopic Lens Preview Engine.
    /// Enables developers, designers, and judges to test and experience the full VR headset perspective
    /// directly on a PC monitor without requiring physical VR headset hardware.
    /// 
    /// Features:
    /// 1. Stereo Headset Lens Mode: Left Eye & Right Eye dual-screen stereoscopic split with Quest 3 optical masks.
    /// 2. Monocular Goggles Mode: Single circular VR lens view with peripheral vision boundary.
    /// 3. Procedural Virtual VR Controllers: 3D Quest Touch controller models with responsive laser pointers and triggers.
    /// 4. Headset 6-DOF Simulation: WASD room walk, Right-Click head look, Q/E altitude, and Space recenter.
    /// 5. Single-Key Toggle (V Key): Instantly toggle between Standard Desktop, Stereo VR Lenses, and Goggles.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class OmniVRHeadsetSimulator : MonoBehaviour
    {
        private static OmniVRHeadsetSimulator instance;
        public static OmniVRHeadsetSimulator Instance => instance;

        public enum VRViewMode
        {
            StandardDesktop = 0,
            StereoHeadsetLens = 1,
            MonocularGoggles = 2
        }

        [Header("VR Simulation Modes")]
        [SerializeField] private VRViewMode currentMode = VRViewMode.StandardDesktop;
        public VRViewMode CurrentMode => currentMode;

        [Header("Optics & IPD (Interpupillary Distance)")]
        [SerializeField] private float ipdMeters = 0.063f; // 63mm Meta Quest 3 default
        [SerializeField] private float vrFov = 96f;        // Quest 3 horizontal/vertical optic FOV
        [SerializeField] private bool showLensMask = true;
        [SerializeField] private bool showVROverlayInfo = true;
        [SerializeField] private bool showControllers = true;

        [Header("Cameras")]
        private Camera mainCam;
        private Camera leftEyeCam;
        private Camera rightEyeCam;
        private GameObject leftEyeObj;
        private GameObject rightEyeObj;

        [Header("Virtual VR Controllers")]
        private GameObject controllerRig;
        private GameObject rightControllerObj;
        private GameObject leftControllerObj;
        private Transform rightControllerTransform;
        private Transform leftControllerTransform;
        private LineRenderer rightLaserRenderer;
        private Transform rightLaserReticle;
        private Transform rightTriggerMesh;
        private Transform leftTriggerMesh;
        private Transform rightGripMesh;
        private Transform leftGripMesh;

        // Texture assets for lens mask
        private Texture2D lensMaskTexture;
        private Texture2D centerDividerTexture;
        private Texture2D reticleTexture;

        // GUI Styles
        private GUIStyle vrHudBadgeStyle;
        private GUIStyle vrHelpBoxStyle;
        private GUIStyle vrKeyLabelStyle;
        private bool guiStylesReady = false;
        private bool showQuickControlsGuide = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (instance == null)
            {
                var existing = UnityEngine.Object.FindAnyObjectByType<OmniVRHeadsetSimulator>();
                if (existing != null)
                {
                    instance = existing;
                }
                else
                {
                    GameObject go = new GameObject("[OMNI_VR_HEADSET_SIMULATOR]");
                    instance = go.AddComponent<OmniVRHeadsetSimulator>();
                    DontDestroyOnLoad(go);
                }
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            LocateMainCamera();
            BuildStereoCameras();
            BuildVirtualControllers();
            GenerateProceduralTextures();
            ApplyViewMode(currentMode);
        }

        private void LocateMainCamera()
        {
            if (mainCam == null)
            {
                mainCam = Camera.main;
                if (mainCam == null)
                {
                    var allCams = Camera.allCameras;
                    if (allCams != null && allCams.Length > 0) mainCam = allCams[0];
                }
            }
        }

        #region Stereo Cameras Setup

        private void BuildStereoCameras()
        {
            if (mainCam == null) LocateMainCamera();
            if (mainCam == null) return;

            // 1. Left Eye Camera
            if (leftEyeObj == null)
            {
                leftEyeObj = new GameObject("VR_Sim_LeftEye");
                leftEyeObj.transform.SetParent(mainCam.transform, false);
                leftEyeCam = leftEyeObj.AddComponent<Camera>();
                leftEyeCam.CopyFrom(mainCam);
                leftEyeCam.rect = new Rect(0f, 0f, 0.5f, 1f);
                leftEyeCam.fieldOfView = vrFov;

                var urpLeft = leftEyeObj.AddComponent<UniversalAdditionalCameraData>();
                urpLeft.renderPostProcessing = true;

                // Remove audio listener from secondary cameras to prevent Unity warnings
                var audio = leftEyeObj.GetComponent<AudioListener>();
                if (audio != null) Destroy(audio);

                leftEyeObj.SetActive(false);
            }

            // 2. Right Eye Camera
            if (rightEyeObj == null)
            {
                rightEyeObj = new GameObject("VR_Sim_RightEye");
                rightEyeObj.transform.SetParent(mainCam.transform, false);
                rightEyeCam = rightEyeObj.AddComponent<Camera>();
                rightEyeCam.CopyFrom(mainCam);
                rightEyeCam.rect = new Rect(0.5f, 0f, 0.5f, 1f);
                rightEyeCam.fieldOfView = vrFov;

                var urpRight = rightEyeObj.AddComponent<UniversalAdditionalCameraData>();
                urpRight.renderPostProcessing = true;

                var audio = rightEyeObj.GetComponent<AudioListener>();
                if (audio != null) Destroy(audio);

                rightEyeObj.SetActive(false);
            }

            UpdateEyePositions();
        }

        private void UpdateEyePositions()
        {
            float halfIpd = ipdMeters * 0.5f;
            if (leftEyeObj != null)
            {
                leftEyeObj.transform.localPosition = new Vector3(-halfIpd, 0f, 0f);
                leftEyeObj.transform.localRotation = Quaternion.identity;
            }
            if (rightEyeObj != null)
            {
                rightEyeObj.transform.localPosition = new Vector3(halfIpd, 0f, 0f);
                rightEyeObj.transform.localRotation = Quaternion.identity;
            }
        }

        #endregion

        #region Procedural Virtual VR Controllers

        private void BuildVirtualControllers()
        {
            if (mainCam == null) LocateMainCamera();
            if (mainCam == null) return;

            if (controllerRig != null) return;

            controllerRig = new GameObject("VR_Sim_VirtualControllers");
            controllerRig.transform.SetParent(mainCam.transform, false);

            // ==========================================
            // RIGHT CONTROLLER (Dominant Interaction Hand)
            // ==========================================
            rightControllerObj = new GameObject("VR_Controller_Right");
            rightControllerObj.transform.SetParent(controllerRig.transform, false);
            rightControllerTransform = rightControllerObj.transform;
            rightControllerTransform.localPosition = new Vector3(0.18f, -0.16f, 0.42f);
            rightControllerTransform.localRotation = Quaternion.Euler(12f, -4f, 0f);

            BuildQuestControllerModel(rightControllerObj, isRight: true, out rightTriggerMesh, out rightGripMesh);

            // Right Controller Laser Pointer
            var laserObj = new GameObject("VR_LaserPointer");
            laserObj.transform.SetParent(rightControllerObj.transform, false);
            laserObj.transform.localPosition = new Vector3(0f, 0.02f, 0.08f);
            laserObj.transform.localRotation = Quaternion.identity;

            rightLaserRenderer = laserObj.AddComponent<LineRenderer>();
            rightLaserRenderer.material = new Material(Shader.Find("Sprites/Default"));
            rightLaserRenderer.startColor = new Color(0.0f, 1.0f, 0.95f, 0.85f);
            rightLaserRenderer.endColor = new Color(0.0f, 0.8f, 1.0f, 0.15f);
            rightLaserRenderer.startWidth = 0.005f;
            rightLaserRenderer.endWidth = 0.002f;
            rightLaserRenderer.positionCount = 2;
            rightLaserRenderer.useWorldSpace = true;

            // Reticle dot
            var reticleObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            reticleObj.name = "VR_LaserReticle";
            reticleObj.transform.SetParent(controllerRig.transform, false);
            reticleObj.transform.localScale = Vector3.one * 0.018f;
            var col = reticleObj.GetComponent<Collider>();
            if (col != null) Destroy(col);
            var mr = reticleObj.GetComponent<MeshRenderer>();
            mr.material = new Material(Shader.Find("Sprites/Default"));
            mr.material.color = new Color(0.0f, 1.0f, 0.85f, 0.95f);
            rightLaserReticle = reticleObj.transform;

            // ==========================================
            // LEFT CONTROLLER (Offhand / Wrist UI)
            // ==========================================
            leftControllerObj = new GameObject("VR_Controller_Left");
            leftControllerObj.transform.SetParent(controllerRig.transform, false);
            leftControllerTransform = leftControllerObj.transform;
            leftControllerTransform.localPosition = new Vector3(-0.20f, -0.22f, 0.40f);
            leftControllerTransform.localRotation = Quaternion.Euler(15f, 8f, -10f);

            BuildQuestControllerModel(leftControllerObj, isRight: false, out leftTriggerMesh, out leftGripMesh);
            BuildWristHologram(leftControllerObj);
        }

        private void BuildQuestControllerModel(GameObject parent, bool isRight, out Transform triggerMesh, out Transform gripMesh)
        {
            var matBlack = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.12f, 0.13f, 0.16f) };
            var matAccent = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = isRight ? new Color(0.0f, 0.85f, 1.0f) : new Color(0.9f, 0.4f, 1.0f) };
            var matButton = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.28f, 0.30f, 0.35f) };

            // Handle Grip Body
            var handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handle.name = "Handle";
            handle.transform.SetParent(parent.transform, false);
            handle.transform.localScale = new Vector3(0.032f, 0.065f, 0.032f);
            handle.transform.localPosition = new Vector3(0f, -0.04f, 0f);
            handle.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
            RemoveCollider(handle);
            handle.GetComponent<MeshRenderer>().material = matBlack;

            // Circular Sensor Tracking Ring
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "TrackingRing";
            ring.transform.SetParent(parent.transform, false);
            ring.transform.localScale = new Vector3(0.095f, 0.008f, 0.095f);
            ring.transform.localPosition = new Vector3(0f, 0.045f, 0.035f);
            ring.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
            RemoveCollider(ring);
            ring.GetComponent<MeshRenderer>().material = matBlack;

            // Tracking Ring Accent Rim
            var ringAccent = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ringAccent.name = "RingAccent";
            ringAccent.transform.SetParent(ring.transform, false);
            ringAccent.transform.localScale = new Vector3(1.02f, 0.3f, 1.02f);
            ringAccent.transform.localPosition = Vector3.zero;
            RemoveCollider(ringAccent);
            ringAccent.GetComponent<MeshRenderer>().material = matAccent;

            // Faceplate
            var faceplate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            faceplate.name = "Faceplate";
            faceplate.transform.SetParent(parent.transform, false);
            faceplate.transform.localScale = new Vector3(0.046f, 0.008f, 0.046f);
            faceplate.transform.localPosition = new Vector3(0f, 0.022f, 0.015f);
            faceplate.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
            RemoveCollider(faceplate);
            faceplate.GetComponent<MeshRenderer>().material = matBlack;

            // Thumbstick
            var thumbstick = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            thumbstick.name = "Thumbstick";
            thumbstick.transform.SetParent(faceplate.transform, false);
            thumbstick.transform.localScale = new Vector3(0.35f, 0.8f, 0.35f);
            thumbstick.transform.localPosition = new Vector3(isRight ? -0.2f : 0.2f, 0.6f, -0.1f);
            RemoveCollider(thumbstick);
            thumbstick.GetComponent<MeshRenderer>().material = matButton;

            // Primary Button (A / X)
            var btn1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            btn1.name = isRight ? "Button_A" : "Button_X";
            btn1.transform.SetParent(faceplate.transform, false);
            btn1.transform.localScale = new Vector3(0.22f, 0.4f, 0.22f);
            btn1.transform.localPosition = new Vector3(isRight ? 0.2f : -0.2f, 0.4f, -0.2f);
            RemoveCollider(btn1);
            btn1.GetComponent<MeshRenderer>().material = matButton;

            // Secondary Button (B / Y)
            var btn2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            btn2.name = isRight ? "Button_B" : "Button_Y";
            btn2.transform.SetParent(faceplate.transform, false);
            btn2.transform.localScale = new Vector3(0.22f, 0.4f, 0.22f);
            btn2.transform.localPosition = new Vector3(isRight ? 0.3f : -0.3f, 0.4f, 0.15f);
            RemoveCollider(btn2);
            btn2.GetComponent<MeshRenderer>().material = matButton;

            // Index Trigger Mesh (Pulls on Click)
            var trigger = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trigger.name = "IndexTrigger";
            trigger.transform.SetParent(parent.transform, false);
            trigger.transform.localScale = new Vector3(0.014f, 0.022f, 0.024f);
            trigger.transform.localPosition = new Vector3(0f, 0.015f, 0.038f);
            trigger.transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);
            RemoveCollider(trigger);
            trigger.GetComponent<MeshRenderer>().material = matBlack;
            triggerMesh = trigger.transform;

            // Grip Button Mesh (Compresses on G key)
            var grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grip.name = "GripButton";
            grip.transform.SetParent(parent.transform, false);
            grip.transform.localScale = new Vector3(0.012f, 0.036f, 0.016f);
            grip.transform.localPosition = new Vector3(isRight ? -0.018f : 0.018f, -0.032f, 0.005f);
            RemoveCollider(grip);
            grip.GetComponent<MeshRenderer>().material = matBlack;
            gripMesh = grip.transform;
        }

        private void BuildWristHologram(GameObject leftParent)
        {
            var wristUi = GameObject.CreatePrimitive(PrimitiveType.Quad);
            wristUi.name = "Wrist_Holographic_HUD";
            wristUi.transform.SetParent(leftParent.transform, false);
            wristUi.transform.localScale = new Vector3(0.08f, 0.045f, 1f);
            wristUi.transform.localPosition = new Vector3(0f, 0.055f, -0.02f);
            wristUi.transform.localRotation = Quaternion.Euler(-55f, 0f, 0f);
            RemoveCollider(wristUi);

            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.color = new Color(0.0f, 0.85f, 1.0f, 0.85f);
            wristUi.GetComponent<MeshRenderer>().material = mat;
        }

        private void RemoveCollider(GameObject go)
        {
            var c = go.GetComponent<Collider>();
            if (c != null) Destroy(c);
        }

        #endregion

        #region Procedural Visual Masks

        private void GenerateProceduralTextures()
        {
            // 1. Dual-Lens Circular Vignette Mask (Darkens corners like Quest 3 pancake optics)
            int size = 256;
            lensMaskTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] cols = new Color[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.48f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float normalized = dist / radius;
                    if (normalized > 1.0f)
                    {
                        cols[y * size + x] = new Color(0f, 0f, 0f, 0.98f);
                    }
                    else if (normalized > 0.80f)
                    {
                        float alpha = Mathf.SmoothStep(0.0f, 0.95f, (normalized - 0.80f) / 0.20f);
                        cols[y * size + x] = new Color(0f, 0f, 0f, alpha);
                    }
                    else
                    {
                        cols[y * size + x] = Color.clear;
                    }
                }
            }
            lensMaskTexture.SetPixels(cols);
            lensMaskTexture.Apply();

            // 2. Center Divider Texture (Nose bridge divider between eyes)
            centerDividerTexture = new Texture2D(1, 1);
            centerDividerTexture.SetPixel(0, 0, new Color(0.03f, 0.03f, 0.05f, 1f));
            centerDividerTexture.Apply();

            // 3. Reticle texture
            reticleTexture = new Texture2D(16, 16);
            Color[] rCols = new Color[16 * 16];
            for (int i = 0; i < rCols.Length; i++) rCols[i] = new Color(0f, 1f, 0.85f, 0.9f);
            reticleTexture.SetPixels(rCols);
            reticleTexture.Apply();
        }

        #endregion

        #region Mode Switching & Update Loop

        private void Update()
        {
            HandleModeShortcuts();
            UpdateVirtualControllers();
        }

        private void HandleModeShortcuts()
        {
            // 'V' Key: Toggle through VR Headset simulation modes
            if (Input.GetKeyDown(KeyCode.V) && !OmniOS.Apps.MediaHub.OmniAppMediaHub.IsTypingActive)
            {
                CycleNextMode();
            }

            // 'H' Key: Toggle VR Controller Models visibility
            if (Input.GetKeyDown(KeyCode.H) && !OmniOS.Apps.MediaHub.OmniAppMediaHub.IsTypingActive)
            {
                showControllers = !showControllers;
                if (controllerRig != null) controllerRig.SetActive(showControllers);
                OmniOSManager.Instance?.ShowNotificationToast($"🎮 VR Controllers: {(showControllers ? "VISIBLE" : "HIDDEN")} [H]");
            }

            // 'F1' Key: Toggle VR Controls Quick Guide
            if (Input.GetKeyDown(KeyCode.F1))
            {
                showQuickControlsGuide = !showQuickControlsGuide;
            }
        }

        public void CycleNextMode()
        {
            VRViewMode next = (VRViewMode)(((int)currentMode + 1) % 3);
            SetViewMode(next);
        }

        public void SetViewMode(VRViewMode mode)
        {
            currentMode = mode;
            ApplyViewMode(currentMode);

            string modeName = currentMode switch
            {
                VRViewMode.StereoHeadsetLens => "🥽 VR STEREO HEADSET LENSES (Left & Right Eye Active)",
                VRViewMode.MonocularGoggles => "🥽 VR GOGGLES MONOCULAR FOV",
                _ => "🖥️ STANDARD DESKTOP VIEW"
            };

            OmniOSManager.Instance?.ShowNotificationToast(modeName);
            SpatialAudioEngine.Instance?.PlayThemeChime();
        }

        private void ApplyViewMode(VRViewMode mode)
        {
            if (mainCam == null) LocateMainCamera();

            switch (mode)
            {
                case VRViewMode.StandardDesktop:
                    if (mainCam != null)
                    {
                        mainCam.enabled = true;
                        mainCam.rect = new Rect(0f, 0f, 1f, 1f);
                    }
                    if (leftEyeObj != null) leftEyeObj.SetActive(false);
                    if (rightEyeObj != null) rightEyeObj.SetActive(false);
                    break;

                case VRViewMode.StereoHeadsetLens:
                    BuildStereoCameras();
                    UpdateEyePositions();
                    if (mainCam != null)
                    {
                        // Main camera remains enabled to manage AudioListener, but renders background or zero-depth
                        mainCam.rect = new Rect(0f, 0f, 0.001f, 0.001f);
                    }
                    if (leftEyeObj != null)
                    {
                        leftEyeObj.SetActive(true);
                        leftEyeCam.rect = new Rect(0f, 0f, 0.5f, 1f);
                    }
                    if (rightEyeObj != null)
                    {
                        rightEyeObj.SetActive(true);
                        rightEyeCam.rect = new Rect(0.5f, 0f, 0.5f, 1f);
                    }
                    break;

                case VRViewMode.MonocularGoggles:
                    if (mainCam != null)
                    {
                        mainCam.enabled = true;
                        mainCam.rect = new Rect(0f, 0f, 1f, 1f);
                    }
                    if (leftEyeObj != null) leftEyeObj.SetActive(false);
                    if (rightEyeObj != null) rightEyeObj.SetActive(false);
                    break;
            }
        }

        private void UpdateVirtualControllers()
        {
            if (mainCam == null) return;
            if (controllerRig == null || !showControllers) return;

            // 1. Right Controller Aim & Laser Raycast
            Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
            Vector3 targetAimPoint = ray.origin + ray.direction * 15f;
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 25f))
            {
                targetAimPoint = hit.point;
                if (rightLaserReticle != null)
                {
                    rightLaserReticle.gameObject.SetActive(true);
                    rightLaserReticle.position = hit.point - ray.direction * 0.02f;
                    rightLaserReticle.rotation = Quaternion.LookRotation(hit.normal);
                }
            }
            else
            {
                if (rightLaserReticle != null) rightLaserReticle.gameObject.SetActive(false);
            }

            if (rightControllerTransform != null)
            {
                // Smoothly orient controller toward aim target
                Vector3 localAim = controllerRig.transform.InverseTransformPoint(targetAimPoint);
                Vector3 toAim = (localAim - rightControllerTransform.localPosition).normalized;
                if (toAim.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(toAim, Vector3.up);
                    rightControllerTransform.localRotation = Quaternion.Slerp(rightControllerTransform.localRotation, targetRot, Time.deltaTime * 18f);
                }

                // Laser Line
                if (rightLaserRenderer != null)
                {
                    rightLaserRenderer.SetPosition(0, rightControllerTransform.position + rightControllerTransform.forward * 0.06f);
                    rightLaserRenderer.SetPosition(1, targetAimPoint);
                }
            }

            // 2. Trigger Pull Animation (Left Click)
            bool isTriggering = Input.GetMouseButton(0);
            if (rightTriggerMesh != null)
            {
                float triggerAngle = isTriggering ? -35f : -15f;
                rightTriggerMesh.localRotation = Quaternion.Euler(triggerAngle, 0f, 0f);
            }

            // 3. Grip Animation (G Key)
            bool isGripping = Input.GetKey(KeyCode.G);
            if (rightGripMesh != null)
            {
                rightGripMesh.localPosition = new Vector3(isGripping ? -0.012f : -0.018f, -0.032f, 0.005f);
            }
            if (leftGripMesh != null)
            {
                leftGripMesh.localPosition = new Vector3(isGripping ? 0.012f : 0.018f, -0.032f, 0.005f);
            }

            // 4. Left Controller Float / Raise (Shift or F key)
            if (leftControllerTransform != null)
            {
                bool raiseLeft = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.F);
                Vector3 targetPos = raiseLeft ? new Vector3(-0.16f, -0.08f, 0.44f) : new Vector3(-0.20f, -0.22f, 0.40f);
                leftControllerTransform.localPosition = Vector3.Lerp(leftControllerTransform.localPosition, targetPos, Time.deltaTime * 10f);
            }
        }

        #endregion

        #region OnGUI Visual Mask & VR Simulator Deck

        private void InitGUIStyles()
        {
            if (guiStylesReady) return;

            vrHudBadgeStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                padding = new RectOffset(8, 8, 4, 4)
            };

            vrHelpBoxStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 12,
                richText = true,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(14, 14, 10, 10)
            };

            vrKeyLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                richText = true,
                normal = { textColor = new Color(0.8f, 0.95f, 1.0f) }
            };

            guiStylesReady = true;
        }

        private void OnGUI()
        {
            InitGUIStyles();

            // Set VR Lens masks behind HUD controls
            GUI.depth = -500;

            // ==============================================================
            // 1. STEREO HEADSET LENS VIGNETTE MASK (Left Eye & Right Eye)
            // ==============================================================
            if (currentMode == VRViewMode.StereoHeadsetLens && lensMaskTexture != null && showLensMask)
            {
                float sw = Screen.width;
                float sh = Screen.height;
                float eyeW = sw * 0.5f;

                // Left Eye Circular Lens Mask
                GUI.DrawTexture(new Rect(0, 0, eyeW, sh), lensMaskTexture, ScaleMode.StretchToFill);

                // Right Eye Circular Lens Mask
                GUI.DrawTexture(new Rect(eyeW, 0, eyeW, sh), lensMaskTexture, ScaleMode.StretchToFill);

                // Center Nose Bridge Divider (12px black vertical bar)
                if (centerDividerTexture != null)
                {
                    GUI.DrawTexture(new Rect(eyeW - 6f, 0, 12f, sh), centerDividerTexture);
                }

                // Eye Identifiers & Optics Crosshair (Subtle holographic indicators)
                if (showVROverlayInfo)
                {
                    GUI.color = new Color(0f, 1f, 0.9f, 0.45f);
                    GUI.Label(new Rect(20, sh - 30, 150, 24), "<b>[L] LEFT EYE (63mm IPD)</b>");
                    GUI.Label(new Rect(eyeW + 20, sh - 30, 150, 24), "<b>[R] RIGHT EYE (63mm IPD)</b>");
                    GUI.color = Color.white;
                }
            }
            else if (currentMode == VRViewMode.MonocularGoggles && lensMaskTexture != null && showLensMask)
            {
                // Fullscreen Monocular VR Goggles Vignette
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), lensMaskTexture, ScaleMode.StretchToFill);
            }

            // ==============================================================
            // 2. BOTTOM-RIGHT VR SIMULATOR CONTROL PILL
            // ==============================================================
            float pillW = 260f;
            float pillH = 36f;
            float pillX = Screen.width - pillW - 14f;
            float pillY = Screen.height - pillH - 14f;

            string btnLabel = currentMode switch
            {
                VRViewMode.StereoHeadsetLens => "<b>🥽 VR STEREO: ON [V]</b>",
                VRViewMode.MonocularGoggles => "<b>🥽 VR GOGGLES: ON [V]</b>",
                _ => "<b>🥽 VR HEADSET PREVIEW [V]</b>"
            };

            Color btnColor = currentMode switch
            {
                VRViewMode.StereoHeadsetLens => new Color(0.0f, 1.0f, 0.65f, 0.95f),
                VRViewMode.MonocularGoggles => new Color(0.1f, 0.85f, 1.0f, 0.95f),
                _ => new Color(0.2f, 0.5f, 0.85f, 0.90f)
            };

            GUI.color = btnColor;
            if (GUI.Button(new Rect(pillX, pillY, pillW, pillH), btnLabel))
            {
                CycleNextMode();
            }
            GUI.color = Color.white;

            // Quick Help Toggle Button
            float helpX = pillX - 38f;
            if (GUI.Button(new Rect(helpX, pillY, 32f, pillH), "<b>?</b>"))
            {
                showQuickControlsGuide = !showQuickControlsGuide;
            }

            // ==============================================================
            // 3. VR CONTROLS POPUP CHEAT SHEET (Toggled via '?' or F1)
            // ==============================================================
            if (showQuickControlsGuide)
            {
                float gW = 340f;
                float gH = 220f;
                float gX = Screen.width - gW - 14f;
                float gY = pillY - gH - 8f;

                GUI.color = new Color(0.06f, 0.08f, 0.12f, 0.96f);
                GUI.Box(new Rect(gX, gY, gW, gH), "", vrHelpBoxStyle);
                GUI.color = Color.white;

                GUILayout.BeginArea(new Rect(gX + 10, gY + 8, gW - 20, gH - 16));
                GUILayout.Label("<b><color=#00e5ff>🥽 VR SIMULATOR CONTROLS GUIDE</color></b>", vrKeyLabelStyle);
                GUILayout.Space(4);
                GUILayout.Label("• <b>[Right Click + Drag]</b>: Look Around in VR (Head 6-DOF)", vrKeyLabelStyle);
                GUILayout.Label("• <b>[W / A / S / D]</b>: Walk Around VR Room", vrKeyLabelStyle);
                GUILayout.Label("• <b>[Q / E]</b>: Float Down / Up in VR Space", vrKeyLabelStyle);
                GUILayout.Label("• <b>[Left Click]</b>: VR Trigger (Laser Point & Click)", vrKeyLabelStyle);
                GUILayout.Label("• <b>[G Key]</b>: VR Controller Grip (Grab 3D Objects)", vrKeyLabelStyle);
                GUILayout.Label("• <b>[Left Shift]</b>: Raise Left Hand / Wrist HUD", vrKeyLabelStyle);
                GUILayout.Label("• <b>[V Key]</b>: Toggle Dual-Eye VR Headset View", vrKeyLabelStyle);
                GUILayout.Label("• <b>[H Key]</b>: Show/Hide 3D VR Controllers", vrKeyLabelStyle);
                GUILayout.Label("• <b>[Spacebar]</b>: Recenter VR View in Front", vrKeyLabelStyle);
                GUILayout.EndArea();
            }
        }

        #endregion
    }
}
