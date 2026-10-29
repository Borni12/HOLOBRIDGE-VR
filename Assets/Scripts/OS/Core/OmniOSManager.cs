using System;
using System.Collections.Generic;
using UnityEngine;
using OmniOS.Apps.CameraApp;
using OmniOS.Apps.Gallery;
using OmniOS.Apps.VideoReality;
using OmniOS.Apps.MatterWeave;
using OmniOS.Apps.Multiverse;
using OmniOS.Apps.SpatialCanvas;
using OmniOS.Apps.Settings;
using OmniOS.Apps.MediaHub;
using OmniOS.Apps.Scenery;
using OmniOS.Environment;
using OmniOS.Interaction;

namespace OmniOS.Core
{
    /// <summary>
    /// OmniOSManager - Master Kernel for the Meta OS Style Spatial Metaverse Operating System.
    /// Manages:
    /// - High-Fidelity Metaverse Virtual Room Environment & Scenery Switcher
    /// - Cinematic Startup Boot Sequence with Harmonic Boot Chime
    /// - Procedural Spatial Sound Effects Engine (SFX)
    /// - YouTube, Facebook & Android Web Apps Media Hub
    /// - Hardware Device Camera (Webcam) and 3D Virtual Camera Subsystems
    /// - 9 Spatial Applications (all start cleanly minimized by default)
    /// - Clean View / Zen Mode (H key) for 100% unblocked scenery
    /// - Notification Toast System
    /// </summary>
    [DisallowMultipleComponent]
    public class OmniOSManager : MonoBehaviour
    {
        private static OmniOSManager instance;
        public static OmniOSManager Instance => instance;

        public static bool Show2DOverlay = false; // Default: Clean view without screen-blocking overlays

        [Header("System References")]
        [SerializeField] private Transform userHead;
        [SerializeField] private SpatialDock spatialDock;
        [SerializeField] private SpatialPointer spatialPointer;
        [SerializeField] private VideoRealityManager videoRealityManager;
        [SerializeField] private SpatialCameraDevice cameraDevice;
        [SerializeField] private DeviceCameraManager deviceCameraManager;
        [SerializeField] private MetaverseEnvironmentManager metaverseEnv;
        [SerializeField] private OmniOSBootSequence bootSequence;

        [Header("Window Prefab / Root")]
        [SerializeField] private Transform windowsRoot;

        // Active Apps Dictionary
        private readonly Dictionary<string, SpatialWindow> registeredApps = new Dictionary<string, SpatialWindow>();

        // Toast Notification State
        private string activeToastMessage = "";
        private float toastTimer = 0f;
        private const float ToastDuration = 3.5f;

        public bool HasActiveToast => toastTimer > 0f;
        public string ActiveToastMessage => activeToastMessage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrapOnPlay()
        {
            if (instance == null && UnityEngine.Object.FindAnyObjectByType<OmniOSManager>() == null)
            {
                GameObject osRoot = new GameObject("[OMNIOS_CORE]");
                osRoot.AddComponent<OmniOSManager>();
                Debug.Log("<color=cyan>[OmniOS Pro]</color> Auto-bootstrapped Spatial Metaverse OS on Play!");
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }
            instance = this;

            if (userHead == null && Camera.main != null)
            {
                userHead = Camera.main.transform;
            }

            InitializeCoreSubsystems();
        }

        private void Start()
        {
            BootstrapDefaultApps();
        }

        private void InitializeCoreSubsystems()
        {
            // 1. Procedural Spatial Sound Engine
            if (SpatialAudioEngine.Instance == null)
            {
                GameObject audioObj = new GameObject("OmniOS_SpatialAudioEngine");
                audioObj.AddComponent<SpatialAudioEngine>();
            }

            // 2. Metaverse Virtual Room Environment
            if (metaverseEnv == null)
            {
                metaverseEnv = UnityEngine.Object.FindAnyObjectByType<MetaverseEnvironmentManager>();
                if (metaverseEnv == null)
                {
                    GameObject envObj = new GameObject("OmniOS_Metaverse_Environment");
                    metaverseEnv = envObj.AddComponent<MetaverseEnvironmentManager>();
                }
            }

            // 3. Hardware Device Camera Manager
            if (deviceCameraManager == null)
            {
                deviceCameraManager = UnityEngine.Object.FindAnyObjectByType<DeviceCameraManager>();
                if (deviceCameraManager == null)
                {
                    GameObject devCamObj = new GameObject("OmniOS_DeviceCamera_Manager");
                    deviceCameraManager = devCamObj.AddComponent<DeviceCameraManager>();
                }
            }

            // 4. Windows Root
            if (windowsRoot == null)
            {
                GameObject rootObj = new GameObject("OmniOS_Windows_Root");
                windowsRoot = rootObj.transform;
            }

            // 5. Spatial Pointer
            if (spatialPointer == null)
            {
                spatialPointer = UnityEngine.Object.FindAnyObjectByType<SpatialPointer>();
                if (spatialPointer == null)
                {
                    GameObject pointerObj = new GameObject("OmniOS_SpatialPointer");
                    spatialPointer = pointerObj.AddComponent<SpatialPointer>();
                }
            }

            // 6. Video Reality Engine
            if (videoRealityManager == null)
            {
                videoRealityManager = UnityEngine.Object.FindAnyObjectByType<VideoRealityManager>();
                if (videoRealityManager == null)
                {
                    GameObject vidObj = new GameObject("OmniOS_VideoReality_Engine");
                    videoRealityManager = vidObj.AddComponent<VideoRealityManager>();
                }
            }

            // 7. Physical Spatial Camera Device
            if (cameraDevice == null)
            {
                cameraDevice = UnityEngine.Object.FindAnyObjectByType<SpatialCameraDevice>();
                if (cameraDevice == null)
                {
                    GameObject camDevObj = new GameObject("OmniOS_SpatialCameraDevice");
                    cameraDevice = camDevObj.AddComponent<SpatialCameraDevice>();
                }
            }

            // 8. Spatial Dock
            if (spatialDock == null)
            {
                spatialDock = UnityEngine.Object.FindAnyObjectByType<SpatialDock>();
                if (spatialDock == null)
                {
                    GameObject dockObj = new GameObject("OmniOS_SpatialDock");
                    spatialDock = dockObj.AddComponent<SpatialDock>();
                }
            }

            // 9. Startup Boot Sequence
            if (bootSequence == null)
            {
                bootSequence = UnityEngine.Object.FindAnyObjectByType<OmniOSBootSequence>();
                if (bootSequence == null)
                {
                    GameObject bootObj = new GameObject("OmniOS_BootSequence");
                    bootSequence = bootObj.AddComponent<OmniOSBootSequence>();
                }
            }

            // 10. Omni-Genesis 5D Neural Graphics & Extreme 3DUI Master Engine
            if (UnityEngine.Object.FindAnyObjectByType<OmniGenesis.Core.OmniGenesis5DExperience>() == null)
            {
                gameObject.AddComponent<OmniGenesis.Core.OmniGenesis5DExperience>();
            }

            // 11. Universal Apps & 5D Master Launcher Box (PC Desktop & VR Controller)
            if (UnityEngine.Object.FindAnyObjectByType<OmniAppLauncherBox>() == null)
            {
                gameObject.AddComponent<OmniAppLauncherBox>();
            }
        }

        private void BootstrapDefaultApps()
        {
            Vector3 headPos = userHead != null ? userHead.position : Vector3.up * 1.6f;
            Vector3 headFwd = userHead != null ? userHead.forward : Vector3.forward;
            headFwd.y = 0;
            if (headFwd.sqrMagnitude < 0.01f) headFwd = Vector3.forward;
            headFwd.Normalize();

            // 1. YouTube & Facebook Media Hub (Direct Center)
            Vector3 mediaPos = headPos + headFwd * 1.75f + Vector3.up * 0.06f;
            var mediaWin = CreateWindow("media_hub", "Media Hub (YouTube, Facebook & Apps)", mediaPos, Quaternion.LookRotation(headFwd));
            mediaWin.gameObject.AddComponent<OmniAppMediaHub>();
            mediaWin.Restore(); // Flagship app opens prominently on boot!

            // 2. Scenery & Environment Explorer (Front Center Right)
            Vector3 sceneryPos = headPos + (Quaternion.Euler(0, 22f, 0) * headFwd) * 1.8f + Vector3.up * 0.08f;
            var sceneryWin = CreateWindow("scenery", "Metaverse Scenery Explorer", sceneryPos, Quaternion.LookRotation(Quaternion.Euler(0, 22f, 0) * headFwd));
            sceneryWin.gameObject.AddComponent<OmniAppScenery>();
            sceneryWin.Minimize();

            // 3. OmniCam App (Front Center Left)
            Vector3 camPos = headPos + (Quaternion.Euler(0, -22f, 0) * headFwd) * 1.8f + Vector3.up * 0.08f;
            var camWin = CreateWindow("camera", "OmniCam Pro (Spatial & Device Camera)", camPos, Quaternion.LookRotation(Quaternion.Euler(0, -22f, 0) * headFwd));
            camWin.gameObject.AddComponent<OmniAppCamera>();
            camWin.Minimize();

            // 4. Gallery App (Right)
            Vector3 galleryPos = headPos + (Quaternion.Euler(0, 44f, 0) * headFwd) * 1.85f;
            var galleryWin = CreateWindow("gallery", "Spatial Media Gallery", galleryPos, Quaternion.LookRotation(Quaternion.Euler(0, 44f, 0) * headFwd));
            galleryWin.gameObject.AddComponent<OmniAppGallery>();
            galleryWin.Minimize();

            // 5. Video Reality Studio (Left)
            Vector3 centerPos = headPos + (Quaternion.Euler(0, -44f, 0) * headFwd) * 1.85f;
            var videoWin = CreateWindow("video_reality", "Video Reality Studio", centerPos, Quaternion.LookRotation(Quaternion.Euler(0, -44f, 0) * headFwd));
            videoWin.gameObject.AddComponent<OmniAppVideoReality>();
            videoWin.Minimize();

            // 6. Matter-Weave App (Far Left)
            Vector3 leftPos = headPos + (Quaternion.Euler(0, -66f, 0) * headFwd) * 1.85f;
            var matterWin = CreateWindow("matter_weave", "Matter-Weave Continuum Lab", leftPos, Quaternion.LookRotation(Quaternion.Euler(0, -66f, 0) * headFwd));
            matterWin.gameObject.AddComponent<OmniAppMatterWeave>();
            matterWin.Minimize();

            // 7. Multiverse App (Far Right)
            Vector3 rightPos = headPos + (Quaternion.Euler(0, 66f, 0) * headFwd) * 1.85f;
            var multiWin = CreateWindow("multiverse", "Multiverse Chronos", rightPos, Quaternion.LookRotation(Quaternion.Euler(0, 66f, 0) * headFwd));
            multiWin.gameObject.AddComponent<OmniAppMultiverse>();
            multiWin.Minimize();

            // 8. Spatial Canvas App (Bottom Left)
            Vector3 canvasPos = headPos + (Quaternion.Euler(0, -82f, 0) * headFwd) * 1.65f + Vector3.down * 0.2f;
            var canvasWin = CreateWindow("spatial_canvas", "Spatial Canvas", canvasPos, Quaternion.LookRotation(Quaternion.Euler(0, -82f, 0) * headFwd));
            canvasWin.gameObject.AddComponent<OmniAppSpatialCanvas>();
            canvasWin.Minimize();

            // 9. System Settings App (Bottom Right)
            Vector3 settingsPos = headPos + (Quaternion.Euler(0, 82f, 0) * headFwd) * 1.65f + Vector3.down * 0.2f;
            var settingsWin = CreateWindow("settings", "System Control Center", settingsPos, Quaternion.LookRotation(Quaternion.Euler(0, 82f, 0) * headFwd));
            settingsWin.gameObject.AddComponent<OmniAppSettings>();
            settingsWin.Minimize();
        }

        public SpatialWindow CreateWindow(string appId, string title, Vector3 position, Quaternion rotation)
        {
            GameObject winObj = new GameObject($"Window_{appId}");
            winObj.transform.SetParent(windowsRoot, false);
            winObj.transform.position = position;
            winObj.transform.rotation = rotation;

            var window = winObj.AddComponent<SpatialWindow>();
            registeredApps[appId] = window;
            return window;
        }

        private void Update()
        {
            // Toast timer
            if (toastTimer > 0f)
            {
                toastTimer -= Time.deltaTime;
            }

            // Global Shortcuts
            if (Input.GetKeyDown(KeyCode.H)) ToggleCleanView();
            if (Input.GetKeyDown(KeyCode.Space)) RecenterAll();
            if (Input.GetKeyDown(KeyCode.T))
            {
                metaverseEnv?.NextTheme();
                SpatialAudioEngine.Instance?.PlayThemeChime();
            }
            if (Input.GetKeyDown(KeyCode.F1))
            {
                Show2DOverlay = !Show2DOverlay;
                ShowNotificationToast($"2D Screen Overlay: {(Show2DOverlay ? "ON" : "OFF")}");
            }

            // Number key shortcuts to toggle apps
            if (Input.GetKeyDown(KeyCode.Alpha1)) ToggleApp("media_hub");
            if (Input.GetKeyDown(KeyCode.Alpha2)) ToggleApp("scenery");
            if (Input.GetKeyDown(KeyCode.Alpha3)) ToggleApp("camera");
            if (Input.GetKeyDown(KeyCode.Alpha4)) ToggleApp("gallery");
            if (Input.GetKeyDown(KeyCode.Alpha5)) ToggleApp("video_reality");
            if (Input.GetKeyDown(KeyCode.Alpha6)) ToggleApp("matter_weave");
            if (Input.GetKeyDown(KeyCode.Alpha7)) ToggleApp("multiverse");
            if (Input.GetKeyDown(KeyCode.Alpha8)) ToggleApp("spatial_canvas");
            if (Input.GetKeyDown(KeyCode.Alpha9)) ToggleApp("settings");
        }

        public void RecenterAll()
        {
            if (userHead == null && Camera.main != null) userHead = Camera.main.transform;
            if (userHead == null) return;

            Vector3 headPos = userHead.position;
            Vector3 headFwd = userHead.forward;
            headFwd.y = 0;
            if (headFwd.sqrMagnitude < 0.01f) headFwd = Vector3.forward;
            headFwd.Normalize();

            if (spatialDock != null)
            {
                spatialDock.SnapToFront();
            }

            // Reposition open window right in front of user
            foreach (var kvp in registeredApps)
            {
                var win = kvp.Value;
                if (win != null && win.IsOpen && !win.IsMinimized)
                {
                    win.transform.position = headPos + headFwd * 1.65f + Vector3.up * 0.06f;
                    win.transform.rotation = Quaternion.LookRotation(headFwd);
                    break;
                }
            }

            ShowNotificationToast("🎯 View Re-centered to Current Gaze");
        }

        public void ToggleCleanView()
        {
            if (spatialDock != null)
            {
                spatialDock.ToggleVisibility();
            }
            SpatialAudioEngine.Instance?.PlayButtonClick();
            ShowNotificationToast("👀 View Toggled (Full Scenery Mode)");
        }

        public void ShowNotificationToast(string message)
        {
            activeToastMessage = message;
            toastTimer = ToastDuration;
            SpatialAudioEngine.Instance?.PlayNotificationChime();
        }

        public void ToggleApp(string appId)
        {
            if (registeredApps.TryGetValue(appId, out var window))
            {
                if (!window.IsOpen || window.IsMinimized)
                {
                    MinimizeAllExcept(appId);
                    window.Restore();
                    SpatialAudioEngine.Instance?.PlayWindowOpen();
                }
                else
                {
                    window.Minimize();
                    SpatialAudioEngine.Instance?.PlayWindowMinimize();
                }
            }
        }

        public void MinimizeAllExcept(string keepAppId)
        {
            foreach (var kvp in registeredApps)
            {
                if (kvp.Key != keepAppId && kvp.Value != null && kvp.Value.IsOpen && !kvp.Value.IsMinimized)
                {
                    kvp.Value.Minimize();
                }
            }
        }

        public void MinimizeAll()
        {
            foreach (var kvp in registeredApps)
            {
                if (kvp.Value != null) kvp.Value.Minimize();
            }
        }

        public void OpenApp(string appId)
        {
            if (registeredApps.TryGetValue(appId, out var window))
            {
                MinimizeAllExcept(appId);
                if (!window.IsOpen) window.OpenWindow();
                else if (window.IsMinimized) window.Restore();
                SpatialAudioEngine.Instance?.PlayWindowOpen();
            }
        }

        public void MinimizeApp(string appId)
        {
            if (registeredApps.TryGetValue(appId, out var window))
            {
                window.Minimize();
                SpatialAudioEngine.Instance?.PlayWindowMinimize();
            }
        }

        public void BringAppToFront(string appId)
        {
            if (registeredApps.TryGetValue(appId, out var window))
            {
                MinimizeAllExcept(appId);
                if (!window.IsOpen) window.OpenWindow();
                else if (window.IsMinimized) window.Restore();

                if (userHead == null && Camera.main != null) userHead = Camera.main.transform;
                if (userHead != null)
                {
                    Vector3 headPos = userHead.position;
                    Vector3 headFwd = userHead.forward;
                    headFwd.y = 0;
                    if (headFwd.sqrMagnitude < 0.01f) headFwd = Vector3.forward;
                    headFwd.Normalize();

                    window.transform.position = headPos + headFwd * 1.55f + Vector3.up * 0.05f;
                    window.transform.rotation = Quaternion.LookRotation(headFwd);
                }

                SpatialAudioEngine.Instance?.PlayWindowOpen();
                ShowNotificationToast($"📱 {window.AppTitle} placed directly in front of view!");
            }
        }

        public SpatialWindow GetAppWindow(string appId)
        {
            registeredApps.TryGetValue(appId, out var win);
            return win;
        }

        private void OnGUI()
        {
            // Only render notification toast, zero clutter
            if (toastTimer > 0f)
            {
                float alpha = Mathf.Clamp01(toastTimer / 0.5f);
                float toastW = 500f;
                float toastH = 42f;
                float tx = (Screen.width - toastW) * 0.5f;
                float ty = 20f;

                Color prevColor = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.Box(new Rect(tx, ty, toastW, toastH), $"<b><color=#00e5ff>{activeToastMessage}</color></b>");
                GUI.color = prevColor;
            }
        }
    }
}
