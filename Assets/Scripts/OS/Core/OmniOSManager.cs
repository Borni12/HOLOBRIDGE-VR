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
using OmniGenesis.Core;

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
        public static OmniOSManager Instance
        {
            get
            {
                if (instance == null || !instance)
                {
                    instance = UnityEngine.Object.FindAnyObjectByType<OmniOSManager>();
                }
                return instance;
            }
        }

        public static bool Show2DOverlay = true; // Enabled by default as requested by user

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
            if (instance != null && instance != this && instance.gameObject != null)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            if (userHead == null)
            {
                var cam = OmniInputHelper.ResolveActiveCamera();
                if (cam != null) userHead = cam.transform;
            }

            // Polish: Deduplicate AudioListeners to keep console 100% clean
            var listeners = UnityEngine.Object.FindObjectsByType<AudioListener>();
            if (listeners.Length > 1)
            {
                Camera mCam = Camera.main;
                for (int i = 0; i < listeners.Length; i++)
                {
                    if (listeners[i] != null && mCam != null && listeners[i].gameObject != mCam.gameObject)
                    {
                        Destroy(listeners[i]);
                    }
                }
            }

            InitializeCoreSubsystems();
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
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

        public void BootstrapDefaultApps()
        {
            if (windowsRoot == null)
            {
                var wr = GameObject.Find("OmniOS_Windows_Root");
                if (wr != null) windowsRoot = wr.transform;
                else
                {
                    GameObject rootObj = new GameObject("OmniOS_Windows_Root");
                    windowsRoot = rootObj.transform;
                }
            }

            Vector3 headPos = userHead != null ? userHead.position : Vector3.up * 1.6f;
            Vector3 headFwd = userHead != null ? userHead.forward : Vector3.forward;
            headFwd.y = 0;
            if (headFwd.sqrMagnitude < 0.01f) headFwd = Vector3.forward;
            headFwd.Normalize();

            // Populate from existing scene hierarchy if already created
            var existingWindows = UnityEngine.Object.FindObjectsByType<SpatialWindow>();
            foreach (var win in existingWindows)
            {
                if (win != null)
                {
                    string n = win.gameObject.name.ToLower();
                    if (n.Contains("media_hub")) registeredApps["media_hub"] = win;
                    else if (n.Contains("scenery")) registeredApps["scenery"] = win;
                    else if (n.Contains("camera")) registeredApps["camera"] = win;
                    else if (n.Contains("gallery")) registeredApps["gallery"] = win;
                    else if (n.Contains("video_reality")) registeredApps["video_reality"] = win;
                    else if (n.Contains("matter_weave")) registeredApps["matter_weave"] = win;
                    else if (n.Contains("multiverse")) registeredApps["multiverse"] = win;
                    else if (n.Contains("spatial_canvas")) registeredApps["spatial_canvas"] = win;
                    else if (n.Contains("settings")) registeredApps["settings"] = win;
                    else if (n.Contains("holobridge")) registeredApps["holobridge_collab"] = win;
                }
            }

            if (registeredApps.ContainsKey("media_hub") && registeredApps.ContainsKey("scenery") && registeredApps.ContainsKey("camera")) return;

            // 1. Android 15 Virtual Tablet & Internet Portal (Direct Center - Ergonomic 1.3m Distance)
            if (!registeredApps.ContainsKey("media_hub"))
            {
                Vector3 mediaPos = headPos + headFwd * 1.30f + Vector3.up * 0.02f;
                var mediaWin = CreateWindow("media_hub", "Android 15 Virtual Tablet (Vanilla Ice Cream • 5G)", mediaPos, Quaternion.LookRotation(headFwd));
                mediaWin.gameObject.AddComponent<OmniAppMediaHub>();
                mediaWin.Restore(); // Flagship app opens prominently on boot!
            }

            // 2. Scenery & Environment Explorer (Front Center Right)
            if (!registeredApps.ContainsKey("scenery"))
            {
                Vector3 sceneryPos = headPos + (Quaternion.Euler(0, 22f, 0) * headFwd) * 1.8f + Vector3.up * 0.08f;
                var sceneryWin = CreateWindow("scenery", "Metaverse Scenery Explorer", sceneryPos, Quaternion.LookRotation(Quaternion.Euler(0, 22f, 0) * headFwd));
                sceneryWin.gameObject.AddComponent<OmniAppScenery>();
                sceneryWin.Minimize();
            }

            // 3. OmniCam App (Front Center Left)
            if (!registeredApps.ContainsKey("camera"))
            {
                Vector3 camPos = headPos + (Quaternion.Euler(0, -22f, 0) * headFwd) * 1.8f + Vector3.up * 0.08f;
                var camWin = CreateWindow("camera", "OmniCam Pro (Spatial & Device Camera)", camPos, Quaternion.LookRotation(Quaternion.Euler(0, -22f, 0) * headFwd));
                camWin.gameObject.AddComponent<OmniAppCamera>();
                camWin.Minimize();
            }

            // 4. Gallery App (Right)
            if (!registeredApps.ContainsKey("gallery"))
            {
                Vector3 galleryPos = headPos + (Quaternion.Euler(0, 44f, 0) * headFwd) * 1.85f;
                var galleryWin = CreateWindow("gallery", "Spatial Media Gallery", galleryPos, Quaternion.LookRotation(Quaternion.Euler(0, 44f, 0) * headFwd));
                galleryWin.gameObject.AddComponent<OmniAppGallery>();
                galleryWin.Minimize();
            }

            // 5. Video Reality Studio (Left)
            if (!registeredApps.ContainsKey("video_reality"))
            {
                Vector3 centerPos = headPos + (Quaternion.Euler(0, -44f, 0) * headFwd) * 1.85f;
                var videoWin = CreateWindow("video_reality", "Video Reality Studio", centerPos, Quaternion.LookRotation(Quaternion.Euler(0, -44f, 0) * headFwd));
                videoWin.gameObject.AddComponent<OmniAppVideoReality>();
                videoWin.Minimize();
            }

            // 6. Matter-Weave App (Far Left)
            if (!registeredApps.ContainsKey("matter_weave"))
            {
                Vector3 leftPos = headPos + (Quaternion.Euler(0, -66f, 0) * headFwd) * 1.85f;
                var matterWin = CreateWindow("matter_weave", "Matter-Weave Continuum Lab", leftPos, Quaternion.LookRotation(Quaternion.Euler(0, -66f, 0) * headFwd));
                matterWin.gameObject.AddComponent<OmniAppMatterWeave>();
                matterWin.Minimize();
            }

            // 7. Multiverse App (Far Right)
            if (!registeredApps.ContainsKey("multiverse"))
            {
                Vector3 rightPos = headPos + (Quaternion.Euler(0, 66f, 0) * headFwd) * 1.85f;
                var multiWin = CreateWindow("multiverse", "Multiverse Chronos", rightPos, Quaternion.LookRotation(Quaternion.Euler(0, 66f, 0) * headFwd));
                multiWin.gameObject.AddComponent<OmniAppMultiverse>();
                multiWin.Minimize();
            }

            // 8. Spatial Canvas App (Bottom Left)
            if (!registeredApps.ContainsKey("spatial_canvas"))
            {
                Vector3 canvasPos = headPos + (Quaternion.Euler(0, -82f, 0) * headFwd) * 1.65f + Vector3.down * 0.2f;
                var canvasWin = CreateWindow("spatial_canvas", "Spatial Canvas", canvasPos, Quaternion.LookRotation(Quaternion.Euler(0, -82f, 0) * headFwd));
                canvasWin.gameObject.AddComponent<OmniAppSpatialCanvas>();
                canvasWin.Minimize();
            }

            // 9. System Settings App (Bottom Right)
            if (!registeredApps.ContainsKey("settings"))
            {
                Vector3 settingsPos = headPos + (Quaternion.Euler(0, 82f, 0) * headFwd) * 1.65f + Vector3.down * 0.2f;
                var settingsWin = CreateWindow("settings", "System Control Center", settingsPos, Quaternion.LookRotation(Quaternion.Euler(0, 82f, 0) * headFwd));
                settingsWin.gameObject.AddComponent<OmniAppSettings>();
                settingsWin.Minimize();
            }

            // 10. HoloBridge 3DUI Collaboration Suite (IEEE VR 2027 Flagship)
            if (!registeredApps.ContainsKey("holobridge_collab"))
            {
                Vector3 collabPos = headPos + (Quaternion.Euler(0, -18f, 0) * headFwd) * 1.55f + Vector3.up * 0.05f;
                var collabWin = CreateWindow("holobridge_collab", "HoloBridge: 3DUI Collaborative Workspace (IEEE VR 2027)", collabPos, Quaternion.LookRotation(Quaternion.Euler(0, -18f, 0) * headFwd));
                collabWin.gameObject.AddComponent<OmniOS.Apps.HoloBridge.OmniAppHoloBridgeCollab>();
                collabWin.Minimize();
            }
        }

        public SpatialWindow CreateWindow(string appId, string title, Vector3 position, Quaternion rotation)
        {
            if (registeredApps.TryGetValue(appId, out var existingWin) && existingWin != null)
            {
                existingWin.transform.position = position;
                existingWin.transform.rotation = rotation;
                return existingWin;
            }

            if (windowsRoot == null)
            {
                var wr = GameObject.Find("OmniOS_Windows_Root");
                if (wr != null) windowsRoot = wr.transform;
                else
                {
                    GameObject rootObj = new GameObject("OmniOS_Windows_Root");
                    windowsRoot = rootObj.transform;
                }
            }

            var existingChild = windowsRoot.Find($"Window_{appId}");
            if (existingChild != null)
            {
                var existingWinComp = existingChild.GetComponent<SpatialWindow>();
                if (existingWinComp != null)
                {
                    registeredApps[appId] = existingWinComp;
                    existingWinComp.transform.position = position;
                    existingWinComp.transform.rotation = rotation;
                    return existingWinComp;
                }
            }

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

            // 1. 5D Flagship Innovations Shortcuts
            if (OmniInputHelper.CheckP())
            {
                var exp = OmniGenesis5DExperience.Instance ?? GetComponent<OmniGenesis5DExperience>() ?? gameObject.AddComponent<OmniGenesis5DExperience>();
                exp.TriggerRealityPeel();
            }
            if (OmniInputHelper.CheckM())
            {
                var exp = OmniGenesis5DExperience.Instance ?? GetComponent<OmniGenesis5DExperience>() ?? gameObject.AddComponent<OmniGenesis5DExperience>();
                int next = ((int)exp.CurrentStiffness + 1) % 3;
                exp.SetStiffness((OmniGenesis5DExperience.MaterialStiffness)next);
            }
            if (OmniInputHelper.CheckO())
            {
                var exp = OmniGenesis5DExperience.Instance ?? GetComponent<OmniGenesis5DExperience>() ?? gameObject.AddComponent<OmniGenesis5DExperience>();
                exp.ToggleMultiverseOrbs();
            }
            if (OmniInputHelper.CheckJ())
            {
                var exp = OmniGenesis5DExperience.Instance ?? GetComponent<OmniGenesis5DExperience>() ?? gameObject.AddComponent<OmniGenesis5DExperience>();
                exp.TriggerHandoff();
            }

            // 2. Full Reload & Auto-Save Shortcut (R or F5)
            if (OmniInputHelper.CheckR())
            {
                FullSystemReload();
            }

            // 3. System Utilities
            if (OmniInputHelper.CheckSpace()) RecenterAll();
            if (OmniInputHelper.CheckH()) ToggleCleanView();
            if (OmniInputHelper.CheckT())
            {
                metaverseEnv?.NextTheme();
                SpatialAudioEngine.Instance?.PlayThemeChime();
            }
            if (Input.GetKeyDown(KeyCode.F2))
            {
                Show2DOverlay = !Show2DOverlay;
                ShowNotificationToast($"2D Screen Overlay: {(Show2DOverlay ? "ON" : "OFF")}");
            }

            // 4. Number key shortcuts to toggle apps (0 to 9)
            if (OmniInputHelper.CheckAlphaKey(0)) ToggleApp("holobridge_collab");
            if (OmniInputHelper.CheckAlphaKey(1)) ToggleApp("media_hub");
            if (OmniInputHelper.CheckAlphaKey(2)) ToggleApp("scenery");
            if (OmniInputHelper.CheckAlphaKey(3)) ToggleApp("camera");
            if (OmniInputHelper.CheckAlphaKey(4)) ToggleApp("gallery");
            if (OmniInputHelper.CheckAlphaKey(5)) ToggleApp("video_reality");
            if (OmniInputHelper.CheckAlphaKey(6)) ToggleApp("matter_weave");
            if (OmniInputHelper.CheckAlphaKey(7)) ToggleApp("multiverse");
            if (OmniInputHelper.CheckAlphaKey(8)) ToggleApp("spatial_canvas");
            if (OmniInputHelper.CheckAlphaKey(9)) ToggleApp("settings");
        }

        public void FullSystemReload()
        {
            Debug.Log("<color=#00e5ff>[OmniOS Full Reload]</color> Executing Full System Reload & Auto-Save...");

            // 1. Resolve Active Camera
            Camera activeCam = OmniInputHelper.ResolveActiveCamera();
            if (activeCam != null) userHead = activeCam.transform;

            // 2. Rebuild All 5D Neural Innovations
            var exp5D = OmniGenesis5DExperience.Instance ?? GetComponent<OmniGenesis5DExperience>() ?? gameObject.AddComponent<OmniGenesis5DExperience>();
            exp5D.RebuildAll5DSystems();

            // 3. Recenter View and Snap Dock
            RecenterAll();

            // 4. Play harmonic chime and notify user
            SpatialAudioEngine.Instance?.PlayThemeChime();
            ShowNotificationToast("⚡ FULL SYSTEM RELOAD: All 5D Systems & Spatial Apps Re-initialized!");

#if UNITY_EDITOR
            // Trigger Scene Auto-Save in Editor
            try
            {
                var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
                if (!string.IsNullOrEmpty(activeScene.path))
                {
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);
                    Debug.Log($"<color=#00ff88>[OmniAutoSave]</color> Scene '{activeScene.name}' auto-saved during Full Reload!");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[OmniAutoSave] Note: " + ex.Message);
            }
#endif
        }

        public void RecenterAll()
        {
            if (userHead == null && Camera.main != null) userHead = Camera.main.transform;
            if (userHead == null) return;

            // Enforce natural eye level so user never sinks into the floor
            if (userHead.position.y < 1.45f)
            {
                userHead.position = new Vector3(userHead.position.x, 1.60f, userHead.position.z);
            }

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
            Debug.Log($"<color=#00e5ff>[OmniOS Toast]</color> {message}");
        }

        public void ToggleApp(string appId)
        {
            if (registeredApps.Count == 0)
            {
                BootstrapDefaultApps();
            }

            if (registeredApps.TryGetValue(appId, out var window))
            {
                if (!window.IsOpen || window.IsMinimized)
                {
                    MinimizeAllExcept(appId);
                    window.Restore();
                    SpatialAudioEngine.Instance?.PlayWindowOpen();
                    Debug.Log($"<color=#00e5ff>[OmniOS App]</color> 📱 {window.AppTitle} Restored to front");
                }
                else
                {
                    window.Minimize();
                    SpatialAudioEngine.Instance?.PlayWindowMinimize();
                    Debug.Log($"<color=#00e5ff>[OmniOS App]</color> 📱 {window.AppTitle} Minimized");
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
            if (registeredApps.Count == 0)
            {
                BootstrapDefaultApps();
            }

            registeredApps.TryGetValue(appId, out var win);
            return win;
        }

        private void OnGUI()
        {
            OmniInputHelper.RegisterGUIEvent(Event.current);

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
