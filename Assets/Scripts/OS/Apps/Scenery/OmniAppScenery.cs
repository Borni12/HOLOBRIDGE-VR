using System;
using UnityEngine;
using OmniOS.Core;
using OmniOS.Interaction;
using OmniOS.Environment;

namespace OmniOS.Apps.Scenery
{
    /// <summary>
    /// OmniAppScenery - Metaverse Scenery & Environment Explorer Window for OmniOS.
    /// Provides full 3D interactive SpatialButton3D buttons for Meta Quest Pro / Quest 3.
    /// </summary>
    [RequireComponent(typeof(SpatialWindow))]
    public class OmniAppScenery : MonoBehaviour
    {
        private SpatialWindow spatialWindow;
        private MetaverseEnvironmentManager envManager;
        private bool isDroneActive = false;
        private Transform buttonContainer;
        private SpatialButton3D droneBtn;

        private void Awake()
        {
            spatialWindow = GetComponent<SpatialWindow>();
            spatialWindow.SetDimensions(1.35f, 0.95f);
            envManager = UnityEngine.Object.FindAnyObjectByType<MetaverseEnvironmentManager>();

            Build3DSceneryButtons();
        }

        private void Start()
        {
            if (envManager == null)
            {
                envManager = UnityEngine.Object.FindAnyObjectByType<MetaverseEnvironmentManager>();
            }
        }

        private void Build3DSceneryButtons()
        {
            GameObject container = new GameObject("Scenery_3D_Buttons");
            container.transform.SetParent(spatialWindow.ContentRoot, false);
            container.transform.localPosition = new Vector3(0, 0, -0.015f);
            buttonContainer = container.transform;

            // 1. Cyber Penthouse Button
            CreateSceneryButton("🏙️ Cyber Penthouse", new Vector3(-0.35f, 0.22f, 0), () => SwitchToTheme(MetaverseEnvironmentManager.EnvironmentTheme.CyberPenthouse), new Color(0.0f, 0.88f, 1.0f));

            // 2. Cosmic Nebula Button
            CreateSceneryButton("🌌 Cosmic Nebula", new Vector3(0.35f, 0.22f, 0), () => SwitchToTheme(MetaverseEnvironmentManager.EnvironmentTheme.CosmicNebula), new Color(0.72f, 0.28f, 1.0f));

            // 3. Sunset Horizon Button
            CreateSceneryButton("🌅 Sunset Horizon", new Vector3(-0.35f, 0.04f, 0), () => SwitchToTheme(MetaverseEnvironmentManager.EnvironmentTheme.SunsetHorizon), new Color(1.0f, 0.58f, 0.15f));

            // 4. Unity Default Room Button
            CreateSceneryButton("🏛️ Unity Default Room", new Vector3(0.35f, 0.04f, 0), () => SwitchToTheme(MetaverseEnvironmentManager.EnvironmentTheme.DefaultTemplate), new Color(0.75f, 0.78f, 0.85f));

            // 5. Ambient Music Toggle Button
            droneBtn = CreateSceneryButton("🎵 Ambient Music: OFF", new Vector3(-0.35f, -0.14f, 0), () => ToggleAmbientDrone(), new Color(0.2f, 0.9f, 0.5f));

            // 6. Hide All UI for Pure Scenery
            CreateSceneryButton("👁️ Hide All UI (Zen Mode)", new Vector3(0.35f, -0.14f, 0), () => OmniOSManager.Instance?.ToggleCleanView(), new Color(0.85f, 0.88f, 0.95f));
        }

        private SpatialButton3D CreateSceneryButton(string label, Vector3 localPos, Action onClick, Color accent, float width = 0.32f)
        {
            GameObject btnObj = new GameObject($"Btn_{label.Replace(" ", "_")}");
            btnObj.transform.SetParent(buttonContainer, false);
            btnObj.transform.localPosition = localPos;

            var btn = btnObj.AddComponent<SpatialButton3D>();
            btn.ButtonText = label;
            btn.SetSize(width, 0.10f);
            btn.SetColors(new Color(0.08f, 0.11f, 0.16f, 0.95f), accent);
            btn.onClick.AddListener(() => onClick?.Invoke());
            return btn;
        }

        public void SwitchToTheme(MetaverseEnvironmentManager.EnvironmentTheme theme)
        {
            if (envManager == null) envManager = UnityEngine.Object.FindAnyObjectByType<MetaverseEnvironmentManager>();
            if (envManager != null)
            {
                envManager.ApplyTheme(theme);
                SpatialAudioEngine.Instance?.PlayThemeChime();
                OmniOSManager.Instance?.ShowNotificationToast($"🌌 Scenery Switched: {theme}");
            }
        }

        public void ToggleAmbientDrone()
        {
            isDroneActive = !isDroneActive;
            if (isDroneActive)
            {
                SpatialAudioEngine.Instance?.StartAmbientDrone();
                if (droneBtn != null) droneBtn.ButtonText = "🎵 Ambient Music: ON";
                OmniOSManager.Instance?.ShowNotificationToast("🎵 Cosmic Ambient Music: ON");
            }
            else
            {
                SpatialAudioEngine.Instance?.StopAmbientDrone();
                if (droneBtn != null) droneBtn.ButtonText = "🎵 Ambient Music: OFF";
                OmniOSManager.Instance?.ShowNotificationToast("🎵 Cosmic Ambient Music: OFF");
            }
            SpatialAudioEngine.Instance?.PlayButtonClick();
        }
    }
}
