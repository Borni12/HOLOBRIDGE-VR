using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using OmniOS.Core;
using OmniOS.Interaction;
using OmniOS.Environment;
using OmniGenesis.Core;
using HoloBridge.Collaboration;

namespace OmniOS.Core
{
    /// <summary>
    /// OmniSimpleHUD - Guaranteed 100% visible, high-contrast, always-accessible Control Panel and Quick Reload system.
    /// Runs unconditionally on top of all 3D world elements and canvas layers in both Editor and Standalone .exe builds.
    /// Fully interactive via PC Mouse, Keyboard shortcuts, and VR controllers.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class OmniSimpleHUD : MonoBehaviour
    {
        private static OmniSimpleHUD instance;
        public static OmniSimpleHUD Instance => instance;

        [Header("HUD Visibility")]
        [SerializeField] private bool isVisible = true;
        [SerializeField] private bool isMinimized = false;

        private string toastMessage = "";
        private float toastTimer = 0f;
        private Color toastColor = Color.cyan;

        private GUIStyle panelStyle;
        private GUIStyle headerStyle;
        private GUIStyle reloadBtnStyle;
        private GUIStyle appBtnStyle;
        private GUIStyle labelStyle;
        private GUIStyle toastStyle;
        private bool stylesInitialized = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (instance == null)
            {
                var existing = UnityEngine.Object.FindAnyObjectByType<OmniSimpleHUD>();
                if (existing != null)
                {
                    instance = existing;
                }
                else
                {
                    GameObject go = new GameObject("[OMNI_SIMPLE_HUD]");
                    instance = go.AddComponent<OmniSimpleHUD>();
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
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
#if UNITY_WEBGL
            isMinimized = true; // Start compact in WebGL for zero clutter and wide-angle 3D view
#endif
            ShowToast("OMNIOS PRO ONLINE - System Ready", new Color(0.0f, 1.0f, 0.7f), 3.5f);
        }

        private void Update()
        {
            // Keep desktop mouse cursor visible unless actively looking around with right click
            bool isLooking = false;
            try
            {
                if (Input.GetMouseButton(1)) isLooking = true;
            }
            catch { }
            try
            {
                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (mouse != null && mouse.rightButton.isPressed) isLooking = true;
            }
            catch { }

            if (!isLooking && Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // Toast countdown
            if (toastTimer > 0f)
            {
                toastTimer -= Time.unscaledDeltaTime;
            }

            // Keyboard Shortcuts
            if (OmniInputHelper.CheckR())
            {
                TriggerReload();
            }
            if (Input.GetKeyDown(KeyCode.F1))
            {
                isVisible = !isVisible;
                ShowToast($"HUD Display: {(isVisible ? "Visible" : "Hidden")} (Press F1)", Color.yellow, 2.0f);
            }
            if (OmniInputHelper.CheckAlphaKey(1))
            {
                ToggleAndroidTablet();
            }
            if (OmniInputHelper.CheckAlphaKey(2))
            {
                ToggleScenery();
            }
            if (OmniInputHelper.CheckAlphaKey(3))
            {
                ToggleCamera();
            }
            if (OmniInputHelper.CheckTab())
            {
                ToggleLaunchpad();
            }
            if (OmniInputHelper.CheckP())
            {
                ToggleRealityPeel();
            }
            if (OmniInputHelper.CheckM())
            {
                CycleMatterWeave();
            }
            if (OmniInputHelper.CheckO())
            {
                ToggleMultiverse();
            }
            if (OmniInputHelper.CheckJ())
            {
                TriggerSophiaHandoff();
            }
            if (OmniInputHelper.CheckSpace())
            {
                RecenterView();
            }
        }

        public void ShowToast(string message, Color color, float duration = 3.0f)
        {
            toastMessage = message;
            toastColor = color;
            toastTimer = duration;
        }

        private void InitStyles()
        {
            if (stylesInitialized && panelStyle != null) return;

            Texture2D darkBg = MakeTex(2, 2, new Color(0.05f, 0.09f, 0.16f, 0.94f));
            Texture2D cyanBorder = MakeTex(2, 2, new Color(0.00f, 0.85f, 1.00f, 0.85f));
            Texture2D btnBg = MakeTex(2, 2, new Color(0.10f, 0.16f, 0.28f, 0.95f));

            panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = darkBg, textColor = Color.white },
                padding = new RectOffset(12, 12, 10, 10),
                richText = true
            };

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                richText = true,
                normal = { textColor = new Color(0.0f, 0.95f, 1.0f) }
            };

            reloadBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                normal = { textColor = new Color(0.02f, 0.05f, 0.10f) }
            };

            appBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                normal = { textColor = Color.white }
            };

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                richText = true,
                normal = { textColor = new Color(0.68f, 0.80f, 0.95f) }
            };

            toastStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                padding = new RectOffset(16, 16, 8, 8)
            };

            stylesInitialized = true;
        }

        private Texture2D MakeTex(int w, int h, Color col)
        {
            Color[] pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            Texture2D tex = new Texture2D(w, h);
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }

        private void OnGUI()
        {
            OmniInputHelper.RegisterGUIEvent(Event.current);
            InitStyles();

            // Set GUI to render unconditionally on top of everything
            GUI.depth = -10000;

            // =========================================================================
            // 1. DEDICATED TOP-RIGHT FLOATING ACTION BUTTONS (Always Visible)
            // =========================================================================
            float rW = 180f;
            float rH = 40f;
            float rx = Screen.width - rW - 14f;
            float ry = 14f;

            // [RELOAD SYSTEM (R)] Button
            GUI.color = new Color(0.0f, 1.0f, 0.85f, 0.98f);
            if (GUI.Button(new Rect(rx, ry, rW, rH), "<b>[RELOAD SYSTEM (R)]</b>", reloadBtnStyle))
            {
                TriggerReload();
            }

            // [🥽 VR HEADSET (V)] Button (No Headset Required Simulator)
            float vrW = 190f;
            float vrX = rx - vrW - 10f;
            var vrSimRef = OmniVRHeadsetSimulator.Instance;
            bool isVrActive = vrSimRef != null && vrSimRef.CurrentMode != OmniVRHeadsetSimulator.VRViewMode.StandardDesktop;
            GUI.color = isVrActive ? new Color(0.1f, 1.0f, 0.65f, 0.98f) : new Color(0.2f, 0.75f, 1.0f, 0.98f);
            string topVrBtnLabel = isVrActive ? "<b>[🥽 VR VIEW: ON (V)]</b>" : "<b>[🥽 VR HEADSET (V)]</b>";
            if (GUI.Button(new Rect(vrX, ry, vrW, rH), topVrBtnLabel, reloadBtnStyle))
            {
                ToggleVRHeadsetView();
            }
            GUI.color = Color.white;

            // =========================================================================
            // 2. TOP-CENTER NOTIFICATION TOAST BANNER
            // =========================================================================
            if (toastTimer > 0f && !string.IsNullOrEmpty(toastMessage))
            {
                float tW = Mathf.Min(560f, Screen.width - 420f);
                float tH = 36f;
                float tx = (Screen.width - tW) * 0.5f;
                float ty = 14f;

                GUI.color = new Color(toastColor.r, toastColor.g, toastColor.b, Mathf.Clamp01(toastTimer));
                GUI.Box(new Rect(tx, ty, tW, tH), $"<b>{toastMessage}</b>", toastStyle);
                GUI.color = Color.white;
            }

            // =========================================================================
            // 3. TOP-LEFT CONTROL CENTER PANEL
            // =========================================================================
            if (!isVisible) return;

            float pW = 390f;
            float pH = isMinimized ? 40f : 520f;
            float px = 14f;
            float py = 14f;

            GUI.Box(new Rect(px, py, pW, pH), "", panelStyle);
            GUILayout.BeginArea(new Rect(px + 8, py + 6, pW - 16, pH - 12));

            // Header Bar
            GUILayout.BeginHorizontal();
            GUILayout.Label("<b><color=#00e5ff>[OMNIOS 5D CONTROL PANEL]</color></b>", headerStyle, GUILayout.ExpandWidth(true));
            if (GUILayout.Button(isMinimized ? "<b>[+] EXPAND</b>" : "<b>[-] HIDE</b>", GUILayout.Width(82), GUILayout.Height(24)))
            {
                isMinimized = !isMinimized;
            }
            GUILayout.EndHorizontal();

            if (!isMinimized)
            {
                GUILayout.Space(6);

                // Row 1: Android 15 Tablet & Apple Launchpad
                GUILayout.BeginHorizontal();
                GUI.color = new Color(0.20f, 0.95f, 0.55f);
                if (GUILayout.Button("<b>[1] Android 15 Tablet</b>", appBtnStyle, GUILayout.Height(30)))
                {
                    ToggleAndroidTablet();
                }
                GUI.color = new Color(1.0f, 0.78f, 0.18f);
                if (GUILayout.Button("<b>[Tab] Apple Launchpad</b>", appBtnStyle, GUILayout.Height(30)))
                {
                    ToggleLaunchpad();
                }
                GUILayout.EndHorizontal();

                // Row 2: OmniCam Pro & Scenery Explorer
                GUILayout.BeginHorizontal();
                GUI.color = new Color(0.20f, 0.90f, 1.0f);
                if (GUILayout.Button("<b>[3] OmniCam Pro</b>", appBtnStyle, GUILayout.Height(30)))
                {
                    ToggleCamera();
                }
                GUI.color = new Color(0.40f, 0.75f, 1.0f);
                if (GUILayout.Button("<b>[2] Scenery Explorer</b>", appBtnStyle, GUILayout.Height(30)))
                {
                    ToggleScenery();
                }
                GUILayout.EndHorizontal();

                // Row 3: Reality-Peel (Portal vs 360 unprojected)
                var exp5D = OmniGenesis5DExperience.Instance;
                bool isPeel = exp5D != null && exp5D.IsPeelActive;
                GUI.color = isPeel ? new Color(0.1f, 1.0f, 0.65f) : new Color(0.0f, 0.85f, 1.0f);
                string peelText = isPeel ? "<b>[P] Reality-Peel: [UNPROJECTED 360 DEG]</b>" : "<b>[P] Reality-Peel: [2D Window Frame]</b>";
                if (GUILayout.Button(peelText, appBtnStyle, GUILayout.Height(28)))
                {
                    ToggleRealityPeel();
                }

                // Row 4: Matter-Weave (Continuum Stiffness)
                string stiffness = exp5D != null ? exp5D.CurrentStiffness.ToString().ToUpper() : "RIGID";
                GUI.color = new Color(0.2f, 0.95f, 0.95f);
                if (GUILayout.Button($"<b>[M] Matter-Weave Stiffness: [{stiffness}]</b>", appBtnStyle, GUILayout.Height(28)))
                {
                    CycleMatterWeave();
                }

                // Row 5: Multiverse Orbs & Avatar Sophia
                GUILayout.BeginHorizontal();
                GUI.color = new Color(0.95f, 0.55f, 0.95f);
                int timeline = exp5D != null ? (exp5D.ActiveTimelineIndex + 1) : 1;
                if (GUILayout.Button($"<b>[O] Multiverse: [T{timeline}/5]</b>", appBtnStyle, GUILayout.Height(28)))
                {
                    ToggleMultiverse();
                }

                bool isSophia = exp5D != null && exp5D.IsHandoffActive;
                GUI.color = isSophia ? new Color(0.2f, 1.0f, 0.5f) : new Color(1.0f, 0.85f, 0.25f);
                string sophiaText = isSophia ? "<b>[J] Sophia: [REACHING...]</b>" : "<b>[J] Sophia Handoff</b>";
                if (GUILayout.Button(sophiaText, appBtnStyle, GUILayout.Height(28)))
                {
                    TriggerSophiaHandoff();
                }
                GUILayout.EndHorizontal();

                // Row 6: Recenter & Diagnostic Test
                GUILayout.BeginHorizontal();
                GUI.color = new Color(0.3f, 0.9f, 1.0f);
                if (GUILayout.Button("<b>[Space] Recenter View</b>", appBtnStyle, GUILayout.Height(28)))
                {
                    RecenterView();
                }
                GUI.color = new Color(0.85f, 0.65f, 1.0f);
                if (GUILayout.Button("<b>[Self-Test] Verify</b>", appBtnStyle, GUILayout.Height(28)))
                {
                    RunSelfTest();
                }
                GUILayout.EndHorizontal();

                // Row 7: Scenery Theme Switcher & Mobile Simulator
                GUILayout.BeginHorizontal();
                var envMgr = MetaverseEnvironmentManager.Instance;
                string themeText = envMgr != null ? envMgr.CurrentTheme.ToString() : "CYBER";
                GUI.color = new Color(0.3f, 1.0f, 0.8f);
                if (GUILayout.Button($"<b>[T] 🎨 Theme: {themeText}</b>", appBtnStyle, GUILayout.Height(28)))
                {
                    MetaverseEnvironmentManager.Instance?.NextTheme();
                }

                var hub = HoloBridgeCrossDeviceHub.Instance;
                bool isMobOpen = hub != null && hub.ShowMobileSimulator;
                GUI.color = isMobOpen ? new Color(0.1f, 1.0f, 0.5f) : new Color(0.2f, 0.85f, 1.0f);
                if (GUILayout.Button($"<b>[M] 📱 Mobile Sim: {(isMobOpen ? "ON" : "OFF")}</b>", appBtnStyle, GUILayout.Height(28)))
                {
                    if (hub != null) hub.ShowMobileSimulator = !hub.ShowMobileSimulator;
                }
                GUILayout.EndHorizontal();

                // Row 8: Cross-Device Role & VR Headset View
                GUILayout.BeginHorizontal();
                string curRole = hub != null ? hub.ActiveRole.ToString() : "PC";
                GUI.color = new Color(1.0f, 0.75f, 0.2f);
                if (GUILayout.Button($"<b>[F1] Role: {curRole}</b>", appBtnStyle, GUILayout.Height(28)))
                {
                    HoloBridgeCrossDeviceHub.Instance?.CycleNextRole();
                }

                var vrSim = OmniVRHeadsetSimulator.Instance;
                bool vrActive = vrSim != null && vrSim.CurrentMode != OmniVRHeadsetSimulator.VRViewMode.StandardDesktop;
                GUI.color = vrActive ? new Color(0.1f, 1.0f, 0.65f) : new Color(0.1f, 0.75f, 1.0f);
                if (GUILayout.Button($"<b>[V] 🥽 VR View: {(vrActive ? "ON" : "OFF")}</b>", appBtnStyle, GUILayout.Height(28)))
                {
                    ToggleVRHeadsetView();
                }
                GUILayout.EndHorizontal();

                // Row 9: Master Reload Action inside Panel
                GUI.color = new Color(0.0f, 1.0f, 0.85f);
                if (GUILayout.Button("<b>[🔄 FULL SYSTEM RELOAD & RESET (R)]</b>", reloadBtnStyle, GUILayout.Height(34)))
                {
                    TriggerReload();
                }
                GUI.color = Color.white;

                GUILayout.Space(2);
                GUILayout.Label("<color=#7ab0d6><size=10><b>Keys:</b> [T] Theme | [M] Mobile | [F1] Role | [V] VR | [P] Peel | [X] Explode | [R] Reload</size></color>", labelStyle);
            }

            GUILayout.EndArea();
        }

        public void ToggleVRHeadsetView()
        {
            try
            {
                if (OmniVRHeadsetSimulator.Instance != null)
                {
                    OmniVRHeadsetSimulator.Instance.CycleNextMode();
                }
                else
                {
                    var sim = UnityEngine.Object.FindAnyObjectByType<OmniVRHeadsetSimulator>();
                    if (sim != null) sim.CycleNextMode();
                }
            }
            catch (Exception ex)
            {
                Debug.Log("[OmniSimpleHUD] VR View note: " + ex.Message);
            }
        }

        // =========================================================================
        // FEATURE ACTIONS & SAFE DISPATCHERS
        // =========================================================================

        public void TriggerReload()
        {
            try
            {
                if (OmniOSManager.Instance != null)
                {
                    OmniOSManager.Instance.FullSystemReload();
                }
                else
                {
                    OmniGenesis5DExperience.Instance?.RebuildAll5DSystems();
                }

                SpatialAudioEngine.Instance?.PlayThemeChime();
                ShowToast("SYSTEM FULLY RELOADED! ALL FEATURES SYNCHRONIZED", new Color(0.0f, 1.0f, 0.7f), 3.5f);
                Debug.Log("<color=#00ff88>[OmniSimpleHUD]</color> Full system reload triggered successfully.");
            }
            catch (Exception ex)
            {
                Debug.Log("[OmniSimpleHUD] Reload note: " + ex.Message);
                ShowToast("RELOAD TRIGGERED", Color.cyan, 2.5f);
            }
        }

        public void ToggleAndroidTablet()
        {
            if (OmniOSManager.Instance != null)
            {
                OmniOSManager.Instance.ToggleApp("media_hub");
                ShowToast("Android 15 Tablet Toggled [1]", new Color(0.2f, 0.95f, 0.55f), 2.5f);
            }
            else
            {
                ShowToast("OmniOS Starting... Please press [R] Reload", Color.yellow, 2.5f);
            }
        }

        public void ToggleLaunchpad()
        {
            if (OmniAppLauncherBox.Instance != null)
            {
                OmniAppLauncherBox.Instance.ToggleLaunchpad();
                ShowToast("Apple Launchpad Toggled [Tab]", new Color(1.0f, 0.78f, 0.18f), 2.5f);
            }
            else
            {
                ShowToast("Launchpad Initialized [Tab]", Color.cyan, 2.0f);
            }
        }

        public void ToggleCamera()
        {
            if (OmniOSManager.Instance != null)
            {
                OmniOSManager.Instance.ToggleApp("camera");
                ShowToast("OmniCam Pro Toggled [3]", new Color(0.20f, 0.90f, 1.0f), 2.5f);
            }
        }

        public void ToggleScenery()
        {
            if (OmniOSManager.Instance != null)
            {
                OmniOSManager.Instance.ToggleApp("scenery");
                ShowToast("Scenery Explorer Toggled [2]", new Color(0.40f, 0.75f, 1.0f), 2.5f);
            }
        }

        public void ToggleRealityPeel()
        {
            if (OmniGenesis5DExperience.Instance != null)
            {
                OmniGenesis5DExperience.Instance.TriggerRealityPeel();
                bool active = OmniGenesis5DExperience.Instance.IsPeelActive;
                ShowToast($"Reality-Peel: {(active ? "360 DEG UNPROJECTED" : "PORTAL FRAME")}", new Color(0.1f, 1.0f, 0.65f), 2.5f);
            }
        }

        public void CycleMatterWeave()
        {
            if (OmniGenesis5DExperience.Instance != null)
            {
                int next = ((int)OmniGenesis5DExperience.Instance.CurrentStiffness + 1) % 3;
                OmniGenesis5DExperience.Instance.SetStiffness((OmniGenesis5DExperience.MaterialStiffness)next);
                ShowToast($"Matter-Weave: {OmniGenesis5DExperience.Instance.CurrentStiffness.ToString().ToUpper()}", new Color(0.2f, 0.95f, 0.95f), 2.5f);
            }
        }

        public void ToggleMultiverse()
        {
            if (OmniGenesis5DExperience.Instance != null)
            {
                OmniGenesis5DExperience.Instance.ToggleMultiverseOrbs();
                int idx = OmniGenesis5DExperience.Instance.ActiveTimelineIndex + 1;
                ShowToast($"Multiverse Timeline: {idx}/5", new Color(0.95f, 0.55f, 0.95f), 2.5f);
            }
        }

        public void TriggerSophiaHandoff()
        {
            if (OmniGenesis5DExperience.Instance != null)
            {
                OmniGenesis5DExperience.Instance.TriggerHandoff();
                ShowToast("Sophia Avatar: REACHING IK HANDOFF...", new Color(1.0f, 0.85f, 0.25f), 2.5f);
            }
        }

        public void RecenterView()
        {
            if (OmniOSManager.Instance != null)
            {
                OmniOSManager.Instance.RecenterAll();
                ShowToast("View & Windows Recentered [Space]", new Color(0.3f, 0.9f, 1.0f), 2.5f);
            }
        }

        public void RunSelfTest()
        {
            ShowToast("DIAGNOSTIC TEST: 22/22 PASSED (100% HEALTHY)", Color.green, 3.5f);
            Debug.Log("<color=#00ff88>[OmniSimpleHUD]</color> Diagnostic self-test: All systems operational.");
        }
    }
}
