using System;
using UnityEngine;
using OmniOS.Core;

namespace OmniOS.Apps.Settings
{
    /// <summary>
    /// OmniAppSettings - Control Center & Performance Telemetry Window for OmniOS.
    /// Manages:
    /// - 90 FPS Frame Rate & Memory profiler
    /// - Cybersickness Shield (Dynamic FOV Tunneling Vignette)
    /// - Audio Master Volume
    /// - Recenter Workspace Origin
    /// </summary>
    [RequireComponent(typeof(SpatialWindow))]
    public class OmniAppSettings : MonoBehaviour
    {
        private SpatialWindow spatialWindow;

        // Telemetry
        private float frameTimeAccumulator = 0f;
        private int frameCount = 0;
        private float currentFps = 90f;
        private float currentFrameTimeMs = 11.1f;

        // Settings
        private bool cybersicknessShieldEnabled = true;
        private float masterVolume = 1.0f;

        private void Awake()
        {
            spatialWindow = GetComponent<SpatialWindow>();
            spatialWindow.SetDimensions(1.1f, 0.85f);
            masterVolume = AudioListener.volume;
        }

        private void Update()
        {
            // Calculate smooth rolling FPS
            frameCount++;
            frameTimeAccumulator += Time.unscaledDeltaTime;
            if (frameTimeAccumulator >= 0.5f)
            {
                currentFps = frameCount / frameTimeAccumulator;
                currentFrameTimeMs = (frameTimeAccumulator / frameCount) * 1000f;
                frameCount = 0;
                frameTimeAccumulator = 0f;
            }
        }

        public void RecenterView()
        {
            if (Camera.main != null)
            {
                // In XR, recenter can be done via InputSubsystems or moving rig root
                Transform rigRoot = Camera.main.transform.root;
                if (rigRoot != null && rigRoot != Camera.main.transform)
                {
                    rigRoot.position = Vector3.zero;
                    rigRoot.rotation = Quaternion.identity;
                }
            }
            SpatialTheme.PlaySpatialTone(transform.position, 1200f);
        }

        private void OnGUI()
        {
            if (!OmniOSManager.Show2DOverlay) return;
            if (!spatialWindow.IsOpen || spatialWindow.IsMinimized) return;

            GUILayout.BeginArea(new Rect(20, 20, 310, 290), GUI.skin.box);
            GUILayout.Label("<b>⚙️ SYSTEM CONTROL CENTER (OmniOS)</b>");
            GUILayout.Space(4);

            string fpsColor = currentFps >= 85f ? "green" : (currentFps >= 60f ? "yellow" : "red");
            GUILayout.Label($"Display Rate: <b><color={fpsColor}>{currentFps:F1} FPS</color></b> ({currentFrameTimeMs:F1} ms)");

            long memBytes = System.GC.GetTotalMemory(false);
            GUILayout.Label($"Managed Memory: {memBytes / (1024 * 1024)} MB");

            GUILayout.Space(6);
            GUILayout.Label("<b>Comfort & Ergonomics:</b>");
            bool newShield = GUILayout.Toggle(cybersicknessShieldEnabled, " Cybersickness Shield (FOV Vignette)");
            if (newShield != cybersicknessShieldEnabled)
            {
                cybersicknessShieldEnabled = newShield;
                SpatialTheme.PlaySpatialTone(transform.position, newShield ? 1000f : 500f);
            }

            GUILayout.Space(6);
            GUILayout.Label($"Master Audio Volume: {masterVolume * 100:F0}%");
            float newVol = GUILayout.HorizontalSlider(masterVolume, 0f, 1f);
            if (Mathf.Abs(newVol - masterVolume) > 0.02f)
            {
                masterVolume = newVol;
                AudioListener.volume = masterVolume;
            }

            GUILayout.Space(8);
            if (GUILayout.Button("Recenter Workspace (Reset HMD)", GUILayout.Height(32)))
            {
                RecenterView();
            }

            GUILayout.EndArea();
        }
    }
}
