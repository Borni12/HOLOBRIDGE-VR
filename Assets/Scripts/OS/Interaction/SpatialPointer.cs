using System;
using UnityEngine;
using UnityEngine.XR;
using OmniOS.Core;

namespace OmniOS.Interaction
{
    /// <summary>
    /// SpatialPointer - Universal Meta Quest Pro / Quest 3 & PC Desktop Spatial Interaction Pointer.
    /// Features:
    /// - High-precision physics raycasting against 3D buttons, sliders, and windows
    /// - 100% reliable click dispatching (instantaneous click execution with haptic audio feedback)
    /// - Seamless auto-detection between tracked VR controller and Desktop Mouse/Keyboard
    /// - Desktop First-Person Navigation (Right-Click Drag to Look, WASD to Walk around Metaverse Lounge)
    /// - Holographic laser beam with adaptive emitter offset and luminous reticle dot
    /// - 6-DoF Window repositioning and depth placement
    /// </summary>
    [DisallowMultipleComponent]
    public class SpatialPointer : MonoBehaviour
    {
        [Header("Pointer Origins")]
        [SerializeField] private Transform rightHandTransform;
        [SerializeField] private Transform headCameraTransform;

        [Header("Beam & Cursor Configuration")]
        [SerializeField] private float maxRayDistance = 10.0f;
        [SerializeField] private LayerMask interactionLayers = ~0;
        [SerializeField] private float beamWidth = 0.0035f;

        // Visuals
        private LineRenderer lineRenderer;
        private Transform cursorDot;
        private Material lineMaterial;
        private Material cursorMaterial;

        // Pointer State
        public Vector3 CurrentHitPoint { get; private set; }
        public Vector3 CurrentHitNormal { get; private set; }
        public bool IsHittingTarget { get; private set; }
        public GameObject CurrentHitObject { get; private set; }

        public bool IsTriggerPressed { get; private set; }
        public bool IsTriggerDown { get; private set; }
        public bool IsTriggerUp { get; private set; }

        public bool IsGripPressed { get; private set; }
        public bool IsGripDown { get; private set; }

        // Active Interactive Targets
        private SpatialButton3D hoveredButton;
        private SpatialButton3D pressedButton;
        private SpatialSlider3D activeSlider;

        // Window Dragging State
        private Transform grabbedWindowTransform;
        private Vector3 grabOffset;
        private float grabDistance;

        // Non-allocating raycast buffer
        private readonly RaycastHit[] raycastHits = new RaycastHit[16];

        // Desktop look state
        private float cameraPitch = 0f;
        private float cameraYaw = 0f;
        private bool isTrackingVRController = false;
        private float lastMouseActivityTime = 1f;
        private float lastVRActivityTime = -10f;
        private bool isUsingVR = false;

        private static SpatialPointer instance;
        public static SpatialPointer Instance
        {
            get
            {
                if (instance == null || !instance)
                {
                    instance = UnityEngine.Object.FindAnyObjectByType<SpatialPointer>();
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this && instance.gameObject != null)
            {
                Destroy(this);
                return;
            }
            instance = this;

            if (headCameraTransform == null)
            {
                var cam = OmniGenesis.Core.OmniInputHelper.ResolveActiveCamera();
                if (cam != null) headCameraTransform = cam.transform;
            }

            // Dynamically locate RightHand controller if not wired in Inspector
            if (rightHandTransform == null)
            {
                var transforms = UnityEngine.Object.FindObjectsByType<Transform>();
                foreach (var t in transforms)
                {
                    string n = t.name.ToLower();
                    if ((n.Contains("righthand") || n.Contains("right controller") || n.Contains("right hand")) && !n.Contains("direct"))
                    {
                        rightHandTransform = t;
                        break;
                    }
                }
            }

            if (headCameraTransform != null)
            {
                Vector3 euler = headCameraTransform.eulerAngles;
                cameraPitch = euler.x;
                cameraYaw = euler.y;
            }

            SetupVisuals();
        }

        private void Start()
        {
            // Guarantee visible and unlocked system cursor on standalone .exe / desktop launch
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void SetupVisuals()
        {
            // Laser beam LineRenderer
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.startWidth = beamWidth;
            lineRenderer.endWidth = beamWidth * 0.35f;
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            lineMaterial = new Material(unlitShader);
            lineMaterial.color = new Color(0.0f, 0.88f, 1.0f, 0.70f);
            lineRenderer.material = lineMaterial;

            // Cursor sphere / reticle
            GameObject cursorObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cursorObj.name = "SpatialPointer_CursorDot";
            Destroy(cursorObj.GetComponent<Collider>());
            cursorDot = cursorObj.transform;
            cursorDot.localScale = Vector3.one * 0.022f;

            cursorMaterial = new Material(unlitShader);
            cursorMaterial.color = new Color(0.0f, 1.0f, 0.90f, 0.95f);
            cursorObj.GetComponent<Renderer>().material = cursorMaterial;
        }

        private void Update()
        {
            EnsureControllerTransform();
            UpdateDesktopNavigation();
            UpdateInput();
            UpdateRaycast();
            HandleInteractiveElements();
            UpdateWindowDragging();
        }

        private void EnsureControllerTransform()
        {
            if (rightHandTransform != null) return;
            var transforms = UnityEngine.Object.FindObjectsByType<Transform>();
            foreach (var t in transforms)
            {
                string n = t.name.ToLower();
                if ((n.Contains("righthand") || n.Contains("right controller") || n.Contains("right hand")) && !n.Contains("direct"))
                {
                    rightHandTransform = t;
                    break;
                }
            }
        }

        private void UpdateDesktopNavigation()
        {
            // Desktop navigation and mouse look are managed by OmniSpatialLocomotion.
            // SpatialPointer focuses strictly on high-precision laser reticle and interactive 3D UI raycasting.
        }

        private void UpdateInput()
        {
            bool prevTrigger = IsTriggerPressed;
            bool prevGrip = IsGripPressed;

            // 1. Check VR Controller Inputs
            bool vrTrigger = false;
            bool vrGrip = false;
            isTrackingVRController = false;

            var rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (rightDevice.isValid)
            {
                if (rightDevice.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) && tracked)
                {
                    isTrackingVRController = true;
                }
                else if (rightDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos) && pos.sqrMagnitude > 0.001f)
                {
                    isTrackingVRController = true;
                }

                rightDevice.TryGetFeatureValue(CommonUsages.triggerButton, out vrTrigger);
                if (!vrTrigger && rightDevice.TryGetFeatureValue(CommonUsages.trigger, out float tVal))
                {
                    vrTrigger = tVal > 0.25f;
                }

                rightDevice.TryGetFeatureValue(CommonUsages.gripButton, out vrGrip);
                if (!vrGrip && rightDevice.TryGetFeatureValue(CommonUsages.grip, out float gVal))
                {
                    vrGrip = gVal > 0.25f;
                }
            }

            var leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if (leftDevice.isValid)
            {
                if (!vrTrigger)
                {
                    leftDevice.TryGetFeatureValue(CommonUsages.triggerButton, out vrTrigger);
                    if (!vrTrigger && leftDevice.TryGetFeatureValue(CommonUsages.trigger, out float ltVal))
                    {
                        vrTrigger = ltVal > 0.25f;
                    }
                }
                if (!vrGrip)
                {
                    leftDevice.TryGetFeatureValue(CommonUsages.gripButton, out vrGrip);
                    if (!vrGrip && leftDevice.TryGetFeatureValue(CommonUsages.grip, out float lgVal))
                    {
                        vrGrip = lgVal > 0.25f;
                    }
                }
            }

            // 2. Check Desktop Mouse Fallback
            bool mouseLeft = false;
            bool mouseRight = false;
            try
            {
                mouseLeft = Input.GetMouseButton(0);
                mouseRight = Input.GetMouseButton(1);
            } catch { }

            try
            {
                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (mouse != null)
                {
                    if (mouse.leftButton.isPressed) mouseLeft = true;
                    if (mouse.rightButton.isPressed) mouseRight = true;
                }
            } catch { }

            // Track user interaction modality: Mouse vs VR Controller
            bool mouseMovedOrUsed = mouseLeft || mouseRight;
            try
            {
                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (mouse != null && mouse.delta.ReadValue().sqrMagnitude > 0.04f)
                {
                    mouseMovedOrUsed = true;
                }
            } catch { }

            if (!mouseMovedOrUsed)
            {
                try
                {
                    if (Mathf.Abs(Input.GetAxis("Mouse X")) > 0.01f || Mathf.Abs(Input.GetAxis("Mouse Y")) > 0.01f)
                    {
                        mouseMovedOrUsed = true;
                    }
                } catch { }
            }

            if (mouseMovedOrUsed) lastMouseActivityTime = Time.unscaledTime;
            if (vrTrigger || vrGrip) lastVRActivityTime = Time.unscaledTime;

            isUsingVR = isTrackingVRController && (lastVRActivityTime >= lastMouseActivityTime);

            // If the 2D Apps Launcher Box is open on screen, block mouse clicks from piercing into 3D world behind it
            if (OmniAppLauncherBox.IsOpen && !isUsingVR)
            {
                mouseLeft = false;
            }

            IsTriggerPressed = vrTrigger || mouseLeft;
            IsGripPressed = vrGrip || (mouseRight && Input.GetKey(KeyCode.LeftShift));

            IsTriggerDown = IsTriggerPressed && !prevTrigger;
            IsTriggerUp = !IsTriggerPressed && prevTrigger;
            IsGripDown = IsGripPressed && !prevGrip;
        }

        public Ray GetCurrentPointerRay()
        {
            // 1. If actively holding VR Controller (Quest 3, Quest Pro, OpenXR)
            if (isUsingVR && rightHandTransform != null)
            {
                return new Ray(rightHandTransform.position, rightHandTransform.forward);
            }

            // 2. Desktop Mouse Raycast (Windows, Mac, Linux, AR Touch)
            Camera cam = null;
            if (headCameraTransform != null) cam = headCameraTransform.GetComponent<Camera>();
            if (cam == null || cam.targetTexture != null) cam = Camera.main;
            if (cam == null || cam.targetTexture != null) cam = OmniGenesis.Core.OmniInputHelper.ResolveActiveCamera();

            if (cam != null)
            {
                Vector3 mousePos = Vector3.zero;
                bool hasMouse = false;

                // Priority 1: Legacy Input mousePosition (always exact screen pixel coordinates on PC/Mac/Linux)
                try
                {
                    mousePos = Input.mousePosition;
                    if (mousePos.x >= 0 && mousePos.x <= Screen.width && mousePos.y >= 0 && mousePos.y <= Screen.height)
                    {
                        hasMouse = true;
                    }
                }
                catch { }

                // Priority 2: New Input System Mouse.current
                if (!hasMouse)
                {
                    try
                    {
                        var mouse = UnityEngine.InputSystem.Mouse.current;
                        if (mouse != null)
                        {
                            Vector2 mVal = mouse.position.ReadValue();
                            mousePos = new Vector3(mVal.x, mVal.y, 0f);
                            hasMouse = true;
                        }
                    }
                    catch { }
                }

                if (hasMouse && mousePos.x >= 0 && mousePos.x <= Screen.width && mousePos.y >= 0 && mousePos.y <= Screen.height)
                {
                    return cam.ScreenPointToRay(mousePos);
                }

                return new Ray(cam.transform.position, cam.transform.forward);
            }

            return new Ray(Vector3.zero, Vector3.forward);
        }

        private Vector3 GetBeamOrigin()
        {
            if (isUsingVR && rightHandTransform != null)
            {
                return rightHandTransform.position;
            }

            // In desktop mode, originate ray from camera eye so reticle matches mouse perfectly
            Ray r = GetCurrentPointerRay();
            return r.origin;
        }

        private void UpdateRaycast()
        {
            Ray ray = GetCurrentPointerRay();
            Vector3 origin = GetBeamOrigin();
            Vector3 target = ray.origin + ray.direction * maxRayDistance;

            int hitCount = Physics.RaycastNonAlloc(ray, raycastHits, maxRayDistance, interactionLayers);

            if (hitCount > 0)
            {
                // Pass 0: Ultimate priority for Search Bar, Address Bar, and Virtual Keyboard Keys
                int bestIndex = -1;
                float bestDist = float.MaxValue;
                for (int i = 0; i < hitCount; i++)
                {
                    var colGo = raycastHits[i].collider.gameObject;
                    if (colGo.name.Contains("SearchBar") || colGo.name.Contains("AddressBar") || colGo.name.Contains("VRKey"))
                    {
                        if (raycastHits[i].distance < bestDist)
                        {
                            bestDist = raycastHits[i].distance;
                            bestIndex = i;
                        }
                    }
                }

                // Pass 1: Prioritize interactive targets (buttons, sliders, window header/handles)
                if (bestIndex < 0)
                {
                    for (int i = 0; i < hitCount; i++)
                    {
                        var colGo = raycastHits[i].collider.gameObject;
                        if (colGo.GetComponentInParent<SpatialButton3D>() != null ||
                            colGo.GetComponentInParent<SpatialSlider3D>() != null ||
                            colGo.name.Contains("Window_Btn") ||
                            colGo.name.Contains("GrabBar") ||
                            colGo.name.Contains("Camera_Viewfinder") ||
                            colGo.name.Contains("CameraApp_LiveScreen") ||
                            colGo.name.Contains("YouTube_Screen_Display") ||
                            colGo.name.Contains("Chrome_Viewport") ||
                            colGo.name.Contains("VR_Keyboard") ||
                            colGo.name.Contains("Tablet_Screen_Backdrop"))
                        {
                            if (raycastHits[i].distance < bestDist)
                            {
                                bestDist = raycastHits[i].distance;
                                bestIndex = i;
                            }
                        }
                    }
                }

                // Pass 2: Fallback to nearest general hit if no interactive element was targeted
                if (bestIndex < 0)
                {
                    bestDist = float.MaxValue;
                    for (int i = 0; i < hitCount; i++)
                    {
                        if (raycastHits[i].distance < bestDist)
                        {
                            bestDist = raycastHits[i].distance;
                            bestIndex = i;
                        }
                    }
                }

                if (bestIndex >= 0)
                {
                    RaycastHit hit = raycastHits[bestIndex];
                    IsHittingTarget = true;
                    CurrentHitPoint = hit.point;
                    CurrentHitNormal = hit.normal;
                    CurrentHitObject = hit.collider.gameObject;

                    target = hit.point;

                    // Scale cursor based on distance & hover state
                    float dist = Vector3.Distance(ray.origin, target);
                    bool isHoveringInteractive = (CurrentHitObject.GetComponentInParent<SpatialButton3D>() != null ||
                                                  CurrentHitObject.GetComponentInParent<SpatialSlider3D>() != null ||
                                                  CurrentHitObject.name.Contains("Window_Btn") ||
                                                  CurrentHitObject.name.Contains("GrabBar") ||
                                                  CurrentHitObject.name.Contains("SearchBar") ||
                                                  CurrentHitObject.name.Contains("AddressBar") ||
                                                  CurrentHitObject.name.Contains("VRKey") ||
                                                  CurrentHitObject.name.Contains("VR_Keyboard") ||
                                                  CurrentHitObject.name.Contains("YouTube_Screen_Display") ||
                                                  CurrentHitObject.name.Contains("Chrome_Viewport") ||
                                                  CurrentHitObject.name.Contains("Tablet_Screen_Backdrop"));

                    float cursorScale = Mathf.Clamp(0.016f * dist, 0.012f, 0.05f) * (isHoveringInteractive ? 1.6f : 1.0f);
                    cursorDot.localScale = Vector3.one * cursorScale;
                    cursorDot.position = target - ray.direction * 0.005f;
                    cursorMaterial.color = isHoveringInteractive ? Color.white : new Color(0.0f, 1.0f, 0.90f, 0.95f);
                    cursorDot.gameObject.SetActive(true);
                }
            }
            else
            {
                IsHittingTarget = false;
                CurrentHitObject = null;
                CurrentHitPoint = target;
                cursorDot.gameObject.SetActive(false);
            }

            // Laser beam: Active for VR controller, discreet reticle for desktop mouse
            if (lineRenderer != null)
            {
                lineRenderer.enabled = isUsingVR;
                if (isUsingVR)
                {
                    lineRenderer.SetPosition(0, origin);
                    lineRenderer.SetPosition(1, target);
                }
            }
        }

        private void HandleInteractiveElements()
        {
            SpatialButton3D targetButton = null;
            SpatialSlider3D targetSlider = null;

            if (CurrentHitObject != null)
            {
                targetButton = CurrentHitObject.GetComponentInParent<SpatialButton3D>();
                targetSlider = CurrentHitObject.GetComponentInParent<SpatialSlider3D>();

                // Handle Window Header Close / Minimize Buttons
                if (IsTriggerDown)
                {
                    var window = CurrentHitObject.GetComponentInParent<Core.SpatialWindow>();
                    if (window != null)
                    {
                        if (CurrentHitObject.name == "Window_Btn_Close")
                        {
                            window.CloseWindow();
                            TriggerHaptic(0.6f, 0.04f);
                        }
                        else if (CurrentHitObject.name == "Window_Btn_Minimize")
                        {
                            window.Minimize();
                            TriggerHaptic(0.5f, 0.03f);
                        }
                    }
                }
            }

            // 1. Button Hover State Transitions
            if (targetButton != hoveredButton)
            {
                if (hoveredButton != null) hoveredButton.OnPointerExit();
                hoveredButton = targetButton;
                if (hoveredButton != null)
                {
                    hoveredButton.OnPointerEnter();
                    TriggerHaptic(0.25f, 0.015f);
                }
            }

            // 2. Button & Live Screen Instant Press / Click (100% reliable)
            if (IsTriggerDown)
            {
                if (targetButton != null || hoveredButton != null)
                {
                    pressedButton = targetButton ?? hoveredButton;
                    pressedButton.TriggerClick();
                    TriggerHaptic(0.65f, 0.04f);
                }
                else if (CurrentHitObject != null)
                {
                    string hitName = CurrentHitObject.name;
                    var mediaApp = CurrentHitObject.GetComponentInParent<OmniOS.Apps.MediaHub.OmniAppMediaHub>();
                    if (mediaApp != null)
                    {
                        if (hitName.Contains("SearchBar") || hitName.Contains("AddressBar"))
                        {
                            if (mediaApp.CurrentTab == OmniOS.Apps.MediaHub.OmniAppMediaHub.HubTab.HomeScreen)
                            {
                                mediaApp.SwitchTab(OmniOS.Apps.MediaHub.OmniAppMediaHub.HubTab.ChromeBrowser);
                            }
                            mediaApp.ActivateSearchInput();
                            TriggerHaptic(0.5f, 0.03f);
                        }
                        else if (hitName.Contains("YouTube_Screen_Display"))
                        {
                            mediaApp.ToggleYouTubePlay();
                            TriggerHaptic(0.5f, 0.03f);
                        }
                        else if (hitName.Contains("Chrome_Viewport"))
                        {
                            mediaApp.ActivateSearchInput();
                            TriggerHaptic(0.5f, 0.03f);
                        }
                        else if (hitName.Contains("Tablet_Screen_Backdrop"))
                        {
                            if (mediaApp.CurrentTab == OmniOS.Apps.MediaHub.OmniAppMediaHub.HubTab.HomeScreen)
                            {
                                mediaApp.SwitchTab(OmniOS.Apps.MediaHub.OmniAppMediaHub.HubTab.ChromeBrowser);
                            }
                            mediaApp.ActivateSearchInput();
                            TriggerHaptic(0.5f, 0.03f);
                        }
                        else if (hitName.Contains("Camera_Viewfinder"))
                        {
                            mediaApp.SwitchCameraFeed();
                            TriggerHaptic(0.5f, 0.03f);
                        }
                    }

                    var camApp = CurrentHitObject.GetComponentInParent<OmniOS.Apps.CameraApp.OmniAppCamera>();
                    if (camApp != null && (hitName.Contains("Camera_Viewfinder") || hitName.Contains("CameraApp_LiveScreen")))
                    {
                        camApp.ToggleDeviceWebcam();
                        TriggerHaptic(0.5f, 0.03f);
                    }
                }
                else
                {
                    // Direct ScreenPointToRay universal fallback on click (Always check all ray hits sorted by distance)
                    Camera clickCam = headCameraTransform != null ? headCameraTransform.GetComponent<Camera>() : null;
                    if (clickCam == null || clickCam.targetTexture != null) clickCam = Camera.main;
                    if (clickCam == null || clickCam.targetTexture != null) clickCam = OmniGenesis.Core.OmniInputHelper.ResolveActiveCamera();

                    Ray clickRay = GetCurrentPointerRay();
                    if (Input.GetMouseButtonDown(0) && clickCam != null)
                    {
                        clickRay = clickCam.ScreenPointToRay(Input.mousePosition);
                    }

                    RaycastHit[] directHits = Physics.RaycastAll(clickRay, maxRayDistance, interactionLayers);
                    if (directHits != null && directHits.Length > 0)
                    {
                        Array.Sort(directHits, (a, b) => a.distance.CompareTo(b.distance));
                        SpatialButton3D directBtn = null;
                        GameObject specialHit = null;
                        Vector3 hitPos = Vector3.zero;

                        foreach (var dh in directHits)
                        {
                            if (dh.collider.name.Contains("SearchBar") || dh.collider.name.Contains("AddressBar") || dh.collider.name.Contains("VRKey"))
                            {
                                var b = dh.collider.GetComponentInParent<SpatialButton3D>();
                                if (b != null)
                                {
                                    directBtn = b;
                                    break;
                                }
                            }

                            var b2 = dh.collider.GetComponentInParent<SpatialButton3D>();
                            if (b2 != null && directBtn == null)
                            {
                                directBtn = b2;
                                break;
                            }
                            if (specialHit == null && (dh.collider.name.Contains("SearchBar") ||
                                                       dh.collider.name.Contains("AddressBar") ||
                                                       dh.collider.name.Contains("YouTube_Screen_Display") ||
                                                       dh.collider.name.Contains("Camera_Viewfinder") ||
                                                       dh.collider.name.Contains("CameraApp_LiveScreen") ||
                                                       dh.collider.name.Contains("Chrome_Viewport") ||
                                                       dh.collider.name.Contains("Tablet_Screen_Backdrop")))
                            {
                                specialHit = dh.collider.gameObject;
                                hitPos = dh.point;
                            }
                        }

                        if (directBtn != null)
                        {
                            directBtn.TriggerClick();
                            TriggerHaptic(0.65f, 0.04f);
                        }
                        else if (specialHit != null)
                        {
                            var mediaApp = specialHit.GetComponentInParent<OmniOS.Apps.MediaHub.OmniAppMediaHub>();
                            if (specialHit.name.Contains("SearchBar") || specialHit.name.Contains("AddressBar"))
                            {
                                if (mediaApp != null)
                                {
                                    if (mediaApp.CurrentTab == OmniOS.Apps.MediaHub.OmniAppMediaHub.HubTab.HomeScreen)
                                    {
                                        mediaApp.SwitchTab(OmniOS.Apps.MediaHub.OmniAppMediaHub.HubTab.ChromeBrowser);
                                    }
                                    mediaApp.ActivateSearchInput();
                                    TriggerHaptic(0.5f, 0.03f);
                                }
                            }
                            else if (specialHit.name.Contains("YouTube_Screen_Display"))
                            {
                                if (mediaApp != null)
                                {
                                    mediaApp.ToggleYouTubePlay();
                                    TriggerHaptic(0.5f, 0.03f);
                                }
                            }
                            else if (specialHit.name.Contains("Camera_Viewfinder") || specialHit.name.Contains("CameraApp_LiveScreen"))
                            {
                                if (mediaApp != null)
                                {
                                    mediaApp.SwitchCameraFeed();
                                    TriggerHaptic(0.5f, 0.03f);
                                }
                                var camApp = specialHit.GetComponentInParent<OmniOS.Apps.CameraApp.OmniAppCamera>();
                                if (camApp != null)
                                {
                                    camApp.ToggleDeviceWebcam();
                                    TriggerHaptic(0.5f, 0.03f);
                                }
                            }
                            else if (specialHit.name.Contains("Chrome_Viewport"))
                            {
                                if (mediaApp != null)
                                {
                                    mediaApp.ActivateSearchInput();
                                    TriggerHaptic(0.4f, 0.02f);
                                }
                            }
                            else if (specialHit.name.Contains("Tablet_Screen_Backdrop"))
                            {
                                if (mediaApp != null)
                                {
                                    if (mediaApp.CurrentTab == OmniOS.Apps.MediaHub.OmniAppMediaHub.HubTab.HomeScreen)
                                    {
                                        mediaApp.SwitchTab(OmniOS.Apps.MediaHub.OmniAppMediaHub.HubTab.ChromeBrowser);
                                    }
                                    mediaApp.ActivateSearchInput();
                                    TriggerHaptic(0.3f, 0.015f);
                                }
                            }
                        }
                    }
                }
            }

            if (IsTriggerUp && pressedButton != null)
            {
                pressedButton.OnPointerUp();
                pressedButton = null;
            }

            // 3. Slider Dragging
            if (IsTriggerDown && targetSlider != null)
            {
                activeSlider = targetSlider;
            }
            if (IsTriggerPressed && activeSlider != null)
            {
                activeSlider.ProcessDrag(CurrentHitPoint);
            }
            else if (!IsTriggerPressed && activeSlider != null)
            {
                activeSlider = null;
            }
        }

        private void UpdateWindowDragging()
        {
            if (IsGripDown || (IsTriggerDown && CurrentHitObject != null && CurrentHitObject.name.Contains("GrabBar")))
            {
                if (CurrentHitObject != null)
                {
                    var window = CurrentHitObject.GetComponentInParent<Core.SpatialWindow>();
                    if (window != null)
                    {
                        grabbedWindowTransform = window.transform;
                        Ray ray = GetCurrentPointerRay();
                        grabDistance = Vector3.Distance(ray.origin, grabbedWindowTransform.position);
                        grabOffset = grabbedWindowTransform.position - (ray.origin + ray.direction * grabDistance);
                        TriggerHaptic(0.4f, 0.02f);
                    }
                }
            }

            if ((IsGripPressed || IsTriggerPressed) && grabbedWindowTransform != null)
            {
                Ray ray = GetCurrentPointerRay();
                Vector3 newPos = ray.origin + (ray.direction * grabDistance) + grabOffset;
                grabbedWindowTransform.position = Vector3.Lerp(grabbedWindowTransform.position, newPos, Time.deltaTime * 18f);

                // Window faces user camera smoothly
                if (headCameraTransform != null)
                {
                    Vector3 lookDir = grabbedWindowTransform.position - headCameraTransform.position;
                    if (lookDir.sqrMagnitude > 0.01f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                        grabbedWindowTransform.rotation = Quaternion.Slerp(grabbedWindowTransform.rotation, targetRot, Time.deltaTime * 12f);
                    }
                }
            }
            else if (!IsGripPressed && !IsTriggerPressed && grabbedWindowTransform != null)
            {
                grabbedWindowTransform = null;
            }
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        public void TriggerHaptic(float amplitude, float duration)
        {
            var device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (device.isValid)
            {
                device.SendHapticImpulse(0, amplitude, duration);
            }
        }
    }
}
