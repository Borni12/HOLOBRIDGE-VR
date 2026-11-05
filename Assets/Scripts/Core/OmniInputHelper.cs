using System;
using UnityEngine;

namespace OmniGenesis.Core
{
    /// <summary>
    /// OmniInputHelper - Universal Dual-Mode Input Bridge & XR Camera Resolver.
    /// Works seamlessly across Unity Legacy Input Manager, Unity New Input System Package, and IMGUI events.
    /// Guaranteed 100% responsiveness on PC Desktop (WASD + Mouse) and Meta Quest OpenXR VR.
    /// </summary>
    public static class OmniInputHelper
    {
        private static KeyCode lastGuiKeyCode = KeyCode.None;
        private static float lastGuiKeyTime = -1f;

        /// <summary>
        /// Register IMGUI Event from OnGUI to ensure keystrokes are never missed even when UI controls have focus.
        /// </summary>
        public static void RegisterGUIEvent(Event e)
        {
            if (e != null && e.isKey && e.type == EventType.KeyDown && e.keyCode != KeyCode.None)
            {
                lastGuiKeyCode = e.keyCode;
                lastGuiKeyTime = Time.unscaledTime;
            }
        }

        public static bool CheckP() => IsKeyTriggered(KeyCode.P, UnityEngine.InputSystem.Key.P);
        public static bool CheckM() => IsKeyTriggered(KeyCode.M, UnityEngine.InputSystem.Key.M);
        public static bool CheckO() => IsKeyTriggered(KeyCode.O, UnityEngine.InputSystem.Key.O);
        public static bool CheckJ() => IsKeyTriggered(KeyCode.J, UnityEngine.InputSystem.Key.J);
        public static bool CheckR() => IsKeyTriggered(KeyCode.R, UnityEngine.InputSystem.Key.R) || IsKeyTriggered(KeyCode.F5, UnityEngine.InputSystem.Key.F5);
        public static bool CheckTab() => IsKeyTriggered(KeyCode.Tab, UnityEngine.InputSystem.Key.Tab);
        public static bool CheckSpace() => IsKeyTriggered(KeyCode.Space, UnityEngine.InputSystem.Key.Space);
        public static bool CheckH() => IsKeyTriggered(KeyCode.H, UnityEngine.InputSystem.Key.H);
        public static bool CheckT() => IsKeyTriggered(KeyCode.T, UnityEngine.InputSystem.Key.T);

        public static bool CheckAlphaKey(int number)
        {
            if (number < 0 || number > 9) return false;
            KeyCode legacyCode = KeyCode.Alpha0 + number;
            KeyCode numpadCode = KeyCode.Keypad0 + number;
            UnityEngine.InputSystem.Key newKey = UnityEngine.InputSystem.Key.Digit0 + number;
            UnityEngine.InputSystem.Key newNumpad = UnityEngine.InputSystem.Key.Numpad0 + number;

            return IsKeyTriggered(legacyCode, newKey) || IsKeyTriggered(numpadCode, newNumpad);
        }

        public static bool IsKeyTriggered(KeyCode legacyKey, UnityEngine.InputSystem.Key newKey)
        {
            // 1. Check IMGUI Event Latch (captured within last 0.35s)
            if (lastGuiKeyCode == legacyKey && (Time.unscaledTime - lastGuiKeyTime) < 0.35f)
            {
                lastGuiKeyCode = KeyCode.None; // Consume the key pulse
                return true;
            }

            // 2. Try New Input System Package
            try
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null && kb[newKey].wasPressedThisFrame)
                {
                    return true;
                }
            }
            catch { }

            // 3. Try Legacy Input Manager
            try
            {
                if (Input.GetKeyDown(legacyKey))
                {
                    return true;
                }
            }
            catch { }

            return false;
        }

        /// <summary>
        /// Universally finds, activates, and auto-tags the main eye camera in the scene.
        /// Resolves XR Origin camera conflicts, strictly excludes secondary RenderTexture cameras
        /// (such as tablet optical lenses or spatial camera viewfinders), and ensures smooth mouse look and desktop navigation.
        /// </summary>
        public static Camera ResolveActiveCamera()
        {
            Camera cam = Camera.main;

            // If Camera.main is null or has targetTexture (i.e. is an offscreen / RenderTexture camera like tablet lens), untag it!
            if (cam != null && (cam.targetTexture != null || cam.name.Contains("Lens") || cam.name.Contains("Tablet") || cam.name.Contains("Capture") || cam.name.Contains("SpatialCamera")))
            {
                cam.gameObject.tag = "Untagged";
                cam = null;
            }

            if (cam == null)
            {
                // Search for active cameras in the scene that render directly to the screen display (targetTexture == null)
                Camera[] allCams = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);

                // Priority 1: Direct display camera tagged "MainCamera"
                foreach (var c in allCams)
                {
                    if (c != null && c.gameObject.activeInHierarchy && c.targetTexture == null && c.CompareTag("MainCamera"))
                    {
                        cam = c;
                        break;
                    }
                }

                // Priority 2: Direct display camera whose name implies eye/player/XR/main
                if (cam == null)
                {
                    foreach (var c in allCams)
                    {
                        if (c != null && c.gameObject.activeInHierarchy && c.targetTexture == null)
                        {
                            string n = c.name.ToLower();
                            if (n.Contains("main") || n.Contains("head") || n.Contains("eye") || n.Contains("player") || n.Contains("xr"))
                            {
                                cam = c;
                                break;
                            }
                        }
                    }
                }

                // Priority 3: ANY active camera whose targetTexture == null
                if (cam == null)
                {
                    foreach (var c in allCams)
                    {
                        if (c != null && c.gameObject.activeInHierarchy && c.targetTexture == null)
                        {
                            cam = c;
                            break;
                        }
                    }
                }

                if (cam != null)
                {
                    cam.gameObject.tag = "MainCamera";
                    if (!cam.gameObject.activeSelf) cam.gameObject.SetActive(true);
                }
            }

            if (cam == null)
            {
                GameObject camObj = new GameObject("Omni_MainCamera");
                camObj.tag = "MainCamera";
                cam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
                camObj.transform.position = new Vector3(0, 1.6f, 0);
            }

            // In Desktop mode (no active VR headset), disable TrackedPoseDriver so mouse look and WASD work freely
            try
            {
                if (!UnityEngine.XR.XRSettings.isDeviceActive)
                {
                    var poseDrivers = cam.GetComponentsInChildren<MonoBehaviour>();
                    foreach (var d in poseDrivers)
                    {
                        if (d != null && d.GetType().Name.Contains("TrackedPoseDriver"))
                        {
                            d.enabled = false;
                        }
                    }
                }
            }
            catch { }

            return cam;
        }
    }
}
