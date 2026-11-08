using System;
using UnityEngine;
using OmniOS.Interaction;
using OmniOS.Environment;

namespace OmniOS.Core
{
    /// <summary>
    /// SpatialDock - Meta Quest Pro / Quest 3 (Horizon OS) Style 3D Navigation Bar.
    /// Floats ergonomically at eye/chest level, facing user with gentle spring damping.
    /// Contains real 3D SpatialButton3D interactive buttons that work 100% reliably in VR & PC.
    /// </summary>
    [DisallowMultipleComponent]
    public class SpatialDock : MonoBehaviour
    {
        private struct DockBtnDef
        {
            public string label;
            public Action action;
            public Color color;
            public DockBtnDef(string l, Action a, Color c) { label = l; action = a; color = c; }
        }

        [Header("Dock Ergonomics (Meta Horizon OS Standard)")]
        [SerializeField] private float dockDistance = 1.25f;
        [SerializeField] private float dockHeightOffset = -0.22f; // Sits comfortably in direct field of view
        [SerializeField] private float followDamping = 4.5f;

        [Header("System References")]
        [SerializeField] private Transform headCamera;
        [SerializeField] private OmniOSManager osManager;

        // Visuals
        private GameObject dockFloatingRoot;
        private GameObject dockBackplate;
        private bool isDockVisible = true;
        private Transform buttonsContainer;

        private void Awake()
        {
            // Lock [OMNIOS_CORE] container firmly at origin
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;

            if (headCamera == null && Camera.main != null)
            {
                headCamera = Camera.main.transform;
            }
            if (osManager == null)
            {
                osManager = UnityEngine.Object.FindAnyObjectByType<OmniOSManager>();
            }

            // Create dedicated independent floating dock root so [OMNIOS_CORE] is NEVER moved
            if (dockFloatingRoot == null)
            {
                dockFloatingRoot = new GameObject("SpatialDock_FloatingRoot");
                dockFloatingRoot.transform.SetParent(null, false);
                dockFloatingRoot.transform.position = new Vector3(0f, 1.35f, 1.25f);
            }

            BuildDockChrome();
            Build3DButtons();
        }

        private void BuildDockChrome()
        {
            // Curved / Rounded Dock Backplate Quad
            dockBackplate = GameObject.CreatePrimitive(PrimitiveType.Quad);
            dockBackplate.name = "Dock_GlassBackplate";
            dockBackplate.transform.SetParent(dockFloatingRoot.transform, false);
            dockBackplate.transform.localScale = new Vector3(2.05f, 0.19f, 1f);
            dockBackplate.transform.localPosition = Vector3.zero;

            // Remove any colliders on backplate to prevent raycast obstruction of buttons
            var colliders = dockBackplate.GetComponents<Collider>();
            foreach (var c in colliders) Destroy(c);

            Shader glassShader = Shader.Find("OmniOS/SpatialGlassRealistic") ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (glassShader == null) glassShader = Shader.Find("Sprites/Default");

            Material glassMat = new Material(glassShader);
            glassMat.SetColor("_BaseTint", new Color(0.06f, 0.08f, 0.13f, 0.94f));
            glassMat.SetColor("_BorderGlowColor", SpatialTheme.AccentCyan);
            dockBackplate.GetComponent<Renderer>().material = glassMat;
        }

        private void Build3DButtons()
        {
            GameObject container = new GameObject("Dock_Buttons_Container");
            container.transform.SetParent(dockFloatingRoot.transform, false);
            container.transform.localPosition = new Vector3(0, 0, -0.015f);
            buttonsContainer = container.transform;

            DockBtnDef[] defs = new DockBtnDef[]
            {
                new DockBtnDef("🔄 Reload", () => osManager?.FullSystemReload(), new Color(0.0f, 1.0f, 0.9f)),
                new DockBtnDef("📱 Android", () => osManager?.ToggleApp("media_hub"), new Color(0.2f, 0.95f, 0.55f)),
                new DockBtnDef(" Apps", () => OmniAppLauncherBox.Instance?.ToggleLaunchpad(), new Color(1.0f, 0.75f, 0.15f)),
                new DockBtnDef("🔮 Peel", () => OmniGenesis.Core.OmniGenesis5DExperience.Instance?.TriggerRealityPeel(), new Color(0.0f, 0.95f, 1.0f)),
                new DockBtnDef("⚛️ Matter", () => {
                    var exp = OmniGenesis.Core.OmniGenesis5DExperience.Instance;
                    if (exp != null)
                    {
                        int next = ((int)exp.CurrentStiffness + 1) % 3;
                        exp.SetStiffness((OmniGenesis.Core.OmniGenesis5DExperience.MaterialStiffness)next);
                    }
                }, new Color(1.0f, 0.55f, 0.15f)),
                new DockBtnDef("⏳ Orbs", () => OmniGenesis.Core.OmniGenesis5DExperience.Instance?.ToggleMultiverseOrbs(), new Color(0.85f, 0.45f, 1.0f)),
                new DockBtnDef("🤝 Handoff", () => OmniGenesis.Core.OmniGenesis5DExperience.Instance?.TriggerHandoff(), new Color(0.2f, 0.95f, 0.55f)),
                new DockBtnDef("📷 Cam", () => osManager?.ToggleApp("camera"), new Color(0.25f, 0.95f, 0.55f)),
                new DockBtnDef("🌌 Scene", () => osManager?.ToggleApp("scenery"), new Color(0.35f, 0.85f, 1.0f)),
                new DockBtnDef("🖼️ Photos", () => osManager?.ToggleApp("gallery"), new Color(0.45f, 0.65f, 1.0f)),
                new DockBtnDef("🎨 Theme", () => {
                    var env = UnityEngine.Object.FindAnyObjectByType<MetaverseEnvironmentManager>();
                    env?.NextTheme();
                    SpatialAudioEngine.Instance?.PlayThemeChime();
                }, new Color(0.95f, 0.55f, 0.85f)),
                new DockBtnDef("👁️ Zen", () => osManager?.ToggleCleanView(), new Color(0.6f, 0.65f, 0.75f))
            };

            int count = defs.Length;
            float btnWidth = 0.138f;
            float btnHeight = 0.088f;
            float spacing = 0.016f;
            float totalWidth = count * btnWidth + (count - 1) * spacing;
            float startX = -totalWidth * 0.5f + btnWidth * 0.5f;

            for (int i = 0; i < count; i++)
            {
                float x = startX + i * (btnWidth + spacing);
                CreateDockButton(defs[i].label, new Vector3(x, 0, 0), new Vector2(btnWidth, btnHeight), defs[i].action, defs[i].color);
            }
        }

        private void CreateDockButton(string label, Vector3 localPos, Vector2 size, Action onClick, Color accentCol)
        {
            GameObject btnObj = new GameObject($"DockBtn_{label.Replace(" ", "_")}");
            btnObj.transform.SetParent(buttonsContainer, false);
            btnObj.transform.localPosition = localPos;

            var spatialBtn = btnObj.AddComponent<SpatialButton3D>();
            spatialBtn.ButtonText = label;
            spatialBtn.SetSize(size.x, size.y);
            spatialBtn.SetColors(new Color(0.09f, 0.12f, 0.17f, 0.95f), accentCol);
            spatialBtn.onClick.AddListener(() => onClick?.Invoke());
        }

        private void Update()
        {
            // Keep [OMNIOS_CORE] locked at origin
            if (transform.position != Vector3.zero) transform.position = Vector3.zero;
            if (transform.rotation != Quaternion.identity) transform.rotation = Quaternion.identity;

            // Smooth ergonomic floating behavior: moves dockFloatingRoot instead of [OMNIOS_CORE]
            if (dockFloatingRoot != null && headCamera != null && isDockVisible)
            {
                Vector3 forward = headCamera.forward;
                forward.y = 0;
                if (forward.sqrMagnitude > 0.01f) forward.Normalize();

                Vector3 targetPos = headCamera.position + forward * dockDistance + Vector3.up * dockHeightOffset;
                dockFloatingRoot.transform.position = Vector3.Lerp(dockFloatingRoot.transform.position, targetPos, Time.deltaTime * followDamping);

                Vector3 toCam = dockFloatingRoot.transform.position - headCamera.position;
                if (toCam.sqrMagnitude > 0.04f)
                {
                    // Tilt slightly up (12 degrees) directly facing user's eyes
                    Quaternion targetRot = Quaternion.LookRotation(toCam.normalized, Vector3.up) * Quaternion.Euler(12f, 0, 0);
                    dockFloatingRoot.transform.rotation = Quaternion.Slerp(dockFloatingRoot.transform.rotation, targetRot, Time.deltaTime * (followDamping * 1.5f));
                }
            }

            // Keyboard shortcut 'D' or 'F2' toggles dock visibility (avoiding conflict with 'M')
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.F2))
            {
                ToggleVisibility();
            }

            // Spacebar snaps dock directly in front of camera
            if (Input.GetKeyDown(KeyCode.Space) && headCamera != null)
            {
                SnapToFront();
            }
        }

        public void SnapToFront()
        {
            if (headCamera == null || dockFloatingRoot == null) return;
            Vector3 forward = headCamera.forward;
            forward.y = 0;
            if (forward.sqrMagnitude > 0.01f) forward.Normalize();

            dockFloatingRoot.transform.position = headCamera.position + forward * dockDistance + Vector3.up * dockHeightOffset;
            Vector3 toCam = dockFloatingRoot.transform.position - headCamera.position;
            dockFloatingRoot.transform.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up) * Quaternion.Euler(12f, 0, 0);
            SpatialAudioEngine.Instance?.PlayButtonClick();
        }

        public void ToggleVisibility()
        {
            isDockVisible = !isDockVisible;
            if (dockFloatingRoot != null) dockFloatingRoot.SetActive(isDockVisible);
            SpatialAudioEngine.Instance?.PlayButtonClick();
        }
    }
}
