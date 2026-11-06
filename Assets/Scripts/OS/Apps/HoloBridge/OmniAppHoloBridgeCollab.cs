using System;
using UnityEngine;
using OmniOS.Core;
using OmniOS.Interaction;
using HoloBridge.Collaboration;

namespace OmniOS.Apps.HoloBridge
{
    /// <summary>
    /// OmniAppHoloBridgeCollab - Official IEEE VR 2027 3DUI Contest Spatial Window.
    /// Provides an integrated in-situ control suite for:
    /// 1. Digital Twin Assembly Manipulation & Exploded Views
    /// 2. Partner-Context Depth Disambiguation (PCDD)
    /// 3. TaskBubble Context-Preserving Micro-Handoff with Holographic Ghost Previews
    /// 4. 3D Spatial Annotations & Pins
    /// 5. Asymmetric Device Switcher (VR Quest 3 vs Desktop PC)
    /// </summary>
    [RequireComponent(typeof(SpatialWindow))]
    public class OmniAppHoloBridgeCollab : MonoBehaviour
    {
        public enum CollabTab
        {
            AssemblyInspect = 0,
            PCDDEngine = 1,
            TaskBubble = 2,
            Annotations = 3,
            DeviceRoles = 4
        }

        [Header("Active Screen")]
        [SerializeField] private CollabTab currentTab = CollabTab.AssemblyInspect;

        private SpatialWindow spatialWindow;
        private HoloBridgeCollabManager collabManager;

        // Container Roots
        private GameObject tabContainerInspect;
        private GameObject tabContainerPCDD;
        private GameObject tabContainerTaskBubble;
        private GameObject tabContainerAnnotations;
        private GameObject tabContainerRoles;

        // Live Text Readouts
        private TextMesh statusHeaderTitle;
        private TextMesh inspectStatusText;
        private TextMesh pcddStatusText;
        private TextMesh taskBubbleStatusText;
        private TextMesh roleStatusText;

        private void Awake()
        {
            spatialWindow = GetComponent<SpatialWindow>();
            spatialWindow.SetDimensions(1.52f, 1.05f);

            collabManager = HoloBridgeCollabManager.Instance;

            BuildWindowUI();
        }

        private void Start()
        {
            SwitchTab(CollabTab.AssemblyInspect);
        }

        private void Update()
        {
            UpdateLiveReadouts();
        }

        private void BuildWindowUI()
        {
            Transform root = spatialWindow.ContentRoot;

            // 1. Top Header Ribbon
            CreateQuadPanel("HoloBridge_HeaderRibbon", new Vector3(0, 0.44f, -0.010f), new Vector2(1.44f, 0.055f), root, new Color(0.06f, 0.12f, 0.20f, 0.95f), new Color(0.0f, 0.85f, 1.0f));

            statusHeaderTitle = CreateText(
                "🚀 <b>HoloBridge: Beyond Shared Reality</b> • IEEE VR 2027 3DUI Contest • Cross-Device Shared Spatial Workspace",
                new Vector3(0, 0.44f, -0.025f),
                root, 26, 0.0026f,
                new Color(0.92f, 0.96f, 1.0f, 1.0f)
            );

            // 2. Navigation Tabs (Row of 5 tabs below header)
            float tabY = 0.365f;
            CreateButton("📐 Digital Twin", new Vector3(-0.54f, tabY, -0.025f), new Vector2(0.24f, 0.055f), root, () => SwitchTab(CollabTab.AssemblyInspect), new Color(0.20f, 0.65f, 1.0f));
            CreateButton("🎯 PCDD (Depth)", new Vector3(-0.27f, tabY, -0.025f), new Vector2(0.24f, 0.055f), root, () => SwitchTab(CollabTab.PCDDEngine), new Color(0.25f, 0.90f, 0.50f));
            CreateButton("🤝 TaskBubble", new Vector3(0.00f, tabY, -0.025f), new Vector2(0.24f, 0.055f), root, () => SwitchTab(CollabTab.TaskBubble), new Color(1.0f, 0.65f, 0.15f));
            CreateButton("📌 3D Annotations", new Vector3(0.27f, tabY, -0.025f), new Vector2(0.24f, 0.055f), root, () => SwitchTab(CollabTab.Annotations), new Color(0.85f, 0.40f, 1.0f));
            CreateButton("🎮 Device Roles", new Vector3(0.54f, tabY, -0.025f), new Vector2(0.24f, 0.055f), root, () => SwitchTab(CollabTab.DeviceRoles), new Color(0.35f, 0.75f, 1.0f));

            // 3. Tab Containers
            tabContainerInspect = new GameObject("Tab_AssemblyInspect");
            tabContainerInspect.transform.SetParent(root, false);
            BuildInspectTab(tabContainerInspect.transform);

            tabContainerPCDD = new GameObject("Tab_PCDD");
            tabContainerPCDD.transform.SetParent(root, false);
            BuildPCDDTab(tabContainerPCDD.transform);

            tabContainerTaskBubble = new GameObject("Tab_TaskBubble");
            tabContainerTaskBubble.transform.SetParent(root, false);
            BuildTaskBubbleTab(tabContainerTaskBubble.transform);

            tabContainerAnnotations = new GameObject("Tab_Annotations");
            tabContainerAnnotations.transform.SetParent(root, false);
            BuildAnnotationsTab(tabContainerAnnotations.transform);

            tabContainerRoles = new GameObject("Tab_DeviceRoles");
            tabContainerRoles.transform.SetParent(root, false);
            BuildDeviceRolesTab(tabContainerRoles.transform);
        }

        // ==================== TAB 1: ASSEMBLY & INSPECTION ====================
        private void BuildInspectTab(Transform parent)
        {
            CreateQuadPanel("Inspect_Card", new Vector3(0, 0.06f, -0.015f), new Vector2(1.38f, 0.45f), parent, new Color(0.05f, 0.09f, 0.16f, 0.92f), new Color(0.0f, 0.85f, 1.0f));

            inspectStatusText = CreateText(
                "📐 <b>Quantum Aero-Turbine Digital Twin</b>\n\n" +
                "• <b>Total Layers:</b> 5 Nested Co-axial Stages (Titanium Casing, Fan, Shaft, Bearing, Nozzle)\n" +
                "• <b>Exploded View:</b> 0% (Fully Assembled & Seated)\n" +
                "• <b>Inspection Mode:</b> Standard Metallic Surface [X-Ray Available]\n" +
                "• <b>Volumetric Snapping:</b> Sockets Active (Auto-Seat within 80mm)",
                new Vector3(0, 0.14f, -0.025f),
                parent, 24, 0.0024f,
                new Color(0.92f, 0.96f, 1.0f, 1.0f)
            );

            // Controls Row 1: Exploded View Steps
            CreateButton("◀ 0% Assembled", new Vector3(-0.48f, -0.08f, -0.025f), new Vector2(0.24f, 0.060f), parent, () => SetExplode(0f), new Color(0.20f, 0.65f, 1.0f));
            CreateButton("🔍 35% Inspection", new Vector3(-0.23f, -0.08f, -0.025f), new Vector2(0.24f, 0.060f), parent, () => SetExplode(0.35f), new Color(0.30f, 0.90f, 0.50f));
            CreateButton("💥 70% Exploded", new Vector3(0.02f, -0.08f, -0.025f), new Vector2(0.24f, 0.060f), parent, () => SetExplode(0.70f), new Color(1.0f, 0.65f, 0.20f));
            CreateButton("🌌 100% Full Dissect", new Vector3(0.27f, -0.08f, -0.025f), new Vector2(0.24f, 0.060f), parent, () => SetExplode(1.0f), new Color(0.85f, 0.35f, 1.0f));
            CreateButton("🔄 Reset", new Vector3(0.51f, -0.08f, -0.025f), new Vector2(0.18f, 0.060f), parent, () => ResetAssembly(), new Color(0.65f, 0.75f, 0.85f));

            // Controls Row 2: Modes & Snapping
            CreateButton("👁️ Toggle X-Ray (See Inside Casing)", new Vector3(-0.35f, -0.18f, -0.025f), new Vector2(0.48f, 0.060f), parent, () => ToggleXRay(), new Color(0.0f, 0.95f, 0.95f));
            CreateButton("🔄 Toggle Rotation", new Vector3(0.15f, -0.18f, -0.025f), new Vector2(0.34f, 0.060f), parent, () => ToggleRotation(), new Color(0.40f, 0.75f, 1.0f));
            CreateButton("📱 Center Digital Twin", new Vector3(0.46f, -0.18f, -0.025f), new Vector2(0.28f, 0.060f), parent, () => CenterDigitalTwin(), new Color(0.20f, 0.95f, 0.45f));
        }

        // ==================== TAB 2: PCDD (DEPTH DISAMBIGUATION) ====================
        private void BuildPCDDTab(Transform parent)
        {
            CreateQuadPanel("PCDD_Card", new Vector3(0, 0.06f, -0.015f), new Vector2(1.38f, 0.45f), parent, new Color(0.04f, 0.10f, 0.14f, 0.92f), new Color(0.25f, 0.90f, 0.50f));

            pcddStatusText = CreateText(
                "🎯 <b>Partner-Context Depth Disambiguation (PCDD)</b>\n\n" +
                "• <b>Status:</b> Active (Gaussian Workspace Proximity Weighting Online)\n" +
                "• <b>VR Partner Hand Focus:</b> Tracking near [Internal Needle Roller Bearing]\n" +
                "• <b>Last Disambiguated Hit:</b> Bearing (94% Partner Context Match)\n" +
                "• <b>Baseline Comparison:</b> Without PCDD, click would hit Outer Casing (Wrong Part!)",
                new Vector3(0, 0.14f, -0.025f),
                parent, 24, 0.0024f,
                new Color(0.85f, 1.0f, 0.92f, 1.0f)
            );

            // One-Click Layer Selectors
            CreateButton("Layer 1: Casing (Outer)", new Vector3(-0.48f, -0.08f, -0.025f), new Vector2(0.26f, 0.060f), parent, () => SelectPart(SharedDigitalTwinAssembly.ComponentType.OuterCasing), new Color(0.4f, 0.6f, 0.8f));
            CreateButton("Layer 2: Intake Fan", new Vector3(-0.18f, -0.08f, -0.025f), new Vector2(0.26f, 0.060f), parent, () => SelectPart(SharedDigitalTwinAssembly.ComponentType.IntakeFan), new Color(0.5f, 0.7f, 0.9f));
            CreateButton("Layer 3: ★ Bearing [PCDD]", new Vector3(0.12f, -0.08f, -0.025f), new Vector2(0.28f, 0.060f), parent, () => SelectPart(SharedDigitalTwinAssembly.ComponentType.InternalBearing), new Color(0.2f, 0.95f, 0.5f));
            CreateButton("Layer 4: Nozzle Array", new Vector3(0.42f, -0.08f, -0.025f), new Vector2(0.26f, 0.060f), parent, () => SelectPart(SharedDigitalTwinAssembly.ComponentType.CombustionNozzle), new Color(0.6f, 0.4f, 0.9f));

            // Mode Toggle
            CreateButton("⚡ Toggle PCDD Engine [Active / Bypass]", new Vector3(-0.25f, -0.18f, -0.025f), new Vector2(0.52f, 0.060f), parent, () => TogglePCDD(), new Color(0.15f, 0.85f, 0.45f));
            CreateButton("🎯 Test PCDD Click on Bearing", new Vector3(0.25f, -0.18f, -0.025f), new Vector2(0.44f, 0.060f), parent, () => TestPCDDClick(), new Color(0.0f, 0.85f, 1.0f));
        }

        // ==================== TAB 3: TASKBUBBLE MICRO-HANDOFF ====================
        private void BuildTaskBubbleTab(Transform parent)
        {
            CreateQuadPanel("TaskBubble_Card", new Vector3(0, 0.06f, -0.015f), new Vector2(1.38f, 0.45f), parent, new Color(0.10f, 0.07f, 0.03f, 0.92f), new Color(1.0f, 0.65f, 0.15f));

            taskBubbleStatusText = CreateText(
                "🤝 <b>TaskBubble: Context-Preserving Micro-Handoff & Ghost Preview</b>\n\n" +
                "• <b>Handoff State:</b> Ready (VR User can delegate jitter-sensitive components to Desktop PC)\n" +
                "• <b>Active Component:</b> Internal Needle Roller Bearing [Tolerance Spec: ±0.25 mm]\n" +
                "• <b>Live Holographic Ghost Preview:</b> Translucent glowing proxy in VR scene\n" +
                "• <b>Sub-Millimeter Nudge:</b> Mouse or buttons below adjust with 0.1mm accuracy",
                new Vector3(0, 0.14f, -0.025f),
                parent, 24, 0.0024f,
                new Color(1.0f, 0.95f, 0.88f, 1.0f)
            );

            // Handoff Triggers
            CreateButton("🤝 1. Delegate Bearing to Desktop", new Vector3(-0.35f, -0.08f, -0.025f), new Vector2(0.52f, 0.060f), parent, () => StartHandoff(), new Color(1.0f, 0.65f, 0.15f));
            CreateButton("◀ Nudge -0.1mm", new Vector3(0.05f, -0.08f, -0.025f), new Vector2(0.22f, 0.060f), parent, () => NudgeHandoff(-0.0001f), new Color(0.40f, 0.70f, 1.0f));
            CreateButton("▶ Nudge +0.1mm", new Vector3(0.30f, -0.08f, -0.025f), new Vector2(0.22f, 0.060f), parent, () => NudgeHandoff(0.0001f), new Color(0.40f, 0.70f, 1.0f));

            // Consent-Based Commit Controls
            CreateButton("✅ 2. [Accept & Commit] Lock in Master Scene", new Vector3(-0.25f, -0.18f, -0.025f), new Vector2(0.56f, 0.060f), parent, () => CommitHandoff(), new Color(0.18f, 0.92f, 0.45f));
            CreateButton("❌ Reject Proposal (Revert)", new Vector3(0.25f, -0.18f, -0.025f), new Vector2(0.40f, 0.060f), parent, () => RejectHandoff(), new Color(0.95f, 0.35f, 0.35f));
        }

        // ==================== TAB 4: 3D ANNOTATIONS & PINS ====================
        private void BuildAnnotationsTab(Transform parent)
        {
            CreateQuadPanel("Annotations_Card", new Vector3(0, 0.06f, -0.015f), new Vector2(1.38f, 0.45f), parent, new Color(0.08f, 0.05f, 0.14f, 0.92f), new Color(0.85f, 0.40f, 1.0f));

            CreateText(
                "📌 <b>3D Collaborative Spatial Annotations & Pins</b>\n\n" +
                "• <b>Spatial Anchor:</b> Pins anchor directly to 3D parts and move with exploded views\n" +
                "• <b>Multi-User Badges:</b> Distinct visual callouts for [VR User] and [Desktop User]\n" +
                "• <b>Engineering Telemetry:</b> Clearance warnings, torque checks, and revision history\n" +
                "• <b>Persistence:</b> Synchronized across both VR headset and Desktop monitor",
                new Vector3(0, 0.14f, -0.025f),
                parent, 24, 0.0024f,
                new Color(0.95f, 0.92f, 1.0f, 1.0f)
            );

            // Pin Spawning Buttons
            CreateButton("📌 Add '⚠️ Clearance < 0.2mm' Pin", new Vector3(-0.40f, -0.08f, -0.025f), new Vector2(0.48f, 0.060f), parent, () => SpawnPin("⚠️ Clearance < 0.2mm", new Color(1f, 0.35f, 0.25f)), new Color(1.0f, 0.35f, 0.25f));
            CreateButton("📌 Add '✅ Verified OK' Pin", new Vector3(0.15f, -0.08f, -0.025f), new Vector2(0.42f, 0.060f), parent, () => SpawnPin("✅ Spec Verified OK", new Color(0.20f, 0.95f, 0.45f)), new Color(0.20f, 0.95f, 0.45f));
            CreateButton("📌 Add '🔧 Realigned' Pin", new Vector3(-0.40f, -0.18f, -0.025f), new Vector2(0.48f, 0.060f), parent, () => SpawnPin("🔧 Re-aligned via TaskBubble", new Color(1.0f, 0.65f, 0.15f)), new Color(1.0f, 0.65f, 0.15f));
            CreateButton("🗑️ Clear All 3D Pins", new Vector3(0.15f, -0.18f, -0.025f), new Vector2(0.42f, 0.060f), parent, () => ClearPins(), new Color(0.85f, 0.30f, 0.30f));
        }

        // ==================== TAB 5: ASYMMETRIC DEVICE ROLES ====================
        private void BuildDeviceRolesTab(Transform parent)
        {
            CreateQuadPanel("Roles_Card", new Vector3(0, 0.06f, -0.015f), new Vector2(1.38f, 0.45f), parent, new Color(0.04f, 0.08f, 0.16f, 0.92f), new Color(0.35f, 0.75f, 1.0f));

            roleStatusText = CreateText(
                "🎮 <b>Asymmetric Cross-Device Spatial Roles</b>\n\n" +
                "• <b>Role A: VR Collaborator (Context Agent)</b>\n" +
                "  Meta Quest 3 • OpenXR 6-DoF • Stereoscopic Full Context • Natural Spatial Presence\n\n" +
                "• <b>Role B: Desktop Collaborator (Precision Agent)</b>\n" +
                "  Windows PC • Sub-Millimeter Mouse Navigation • Precision Transform Gizmo\n\n" +
                "👉 <i>Press [F1] or click below to instantly switch perspective for video recording!</i>",
                new Vector3(0, 0.14f, -0.025f),
                parent, 23, 0.0023f,
                new Color(0.92f, 0.96f, 1.0f, 1.0f)
            );

            CreateButton("🥽 Switch to VR Headset View (Quest 3)", new Vector3(-0.28f, -0.12f, -0.025f), new Vector2(0.52f, 0.065f), parent, () => SwitchPerspective(CrossDeviceCoPresence.ActivePerspective.VRHeadset), new Color(0.0f, 0.85f, 1.0f));
            CreateButton("🖥️ Switch to Desktop PC View (Mouse)", new Vector3(0.28f, -0.12f, -0.025f), new Vector2(0.52f, 0.065f), parent, () => SwitchPerspective(CrossDeviceCoPresence.ActivePerspective.DesktopPC), new Color(1.0f, 0.65f, 0.15f));
        }

        // ==================== ACTIONS & LOGIC ====================

        public void SwitchTab(CollabTab tab)
        {
            currentTab = tab;
            tabContainerInspect.SetActive(currentTab == CollabTab.AssemblyInspect);
            tabContainerPCDD.SetActive(currentTab == CollabTab.PCDDEngine);
            tabContainerTaskBubble.SetActive(currentTab == CollabTab.TaskBubble);
            tabContainerAnnotations.SetActive(currentTab == CollabTab.Annotations);
            tabContainerRoles.SetActive(currentTab == CollabTab.DeviceRoles);
            SpatialAudioEngine.Instance?.PlayButtonClick();
        }

        private void SetExplode(float progress)
        {
            var twin = collabManager?.DigitalTwin;
            if (twin != null) twin.ExplodeProgress = progress;
            SpatialAudioEngine.Instance?.PlayHoverTick();
            OmniOSManager.Instance?.ShowNotificationToast($"📐 Digital Twin: Explode {progress * 100f:F0}%");
        }

        private void ToggleXRay()
        {
            var twin = collabManager?.DigitalTwin;
            if (twin != null)
            {
                twin.IsXRayMode = !twin.IsXRayMode;
                OmniOSManager.Instance?.ShowNotificationToast(twin.IsXRayMode ? "👁️ X-Ray Mode: ON (Casing Translucent)" : "👁️ X-Ray Mode: OFF (Solid Metallic)");
            }
            SpatialAudioEngine.Instance?.PlayButtonClick();
        }

        private void ToggleRotation()
        {
            var twin = collabManager?.DigitalTwin;
            if (twin != null) twin.IsRotating = !twin.IsRotating;
            SpatialAudioEngine.Instance?.PlayButtonClick();
        }

        private void ResetAssembly()
        {
            collabManager?.DigitalTwin?.ResetAssembly();
            SpatialAudioEngine.Instance?.PlayButtonClick();
            OmniOSManager.Instance?.ShowNotificationToast("🔄 Digital Twin Reset to Nominal State");
        }

        private void CenterDigitalTwin()
        {
            var twin = collabManager?.DigitalTwin;
            if (twin != null)
            {
                Camera cam = Camera.main ?? UnityEngine.Object.FindAnyObjectByType<Camera>();
                if (cam != null)
                {
                    Vector3 fwd = cam.transform.forward;
                    fwd.y = 0;
                    if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
                    fwd.Normalize();
                    twin.transform.position = cam.transform.position + fwd * 1.55f + Vector3.up * -0.05f;
                    twin.transform.rotation = Quaternion.LookRotation(fwd);
                    OmniOSManager.Instance?.ShowNotificationToast("📱 Digital Twin Snapped into Center View");
                }
            }
            SpatialAudioEngine.Instance?.PlayButtonClick();
        }

        private void SelectPart(SharedDigitalTwinAssembly.ComponentType type)
        {
            collabManager?.DigitalTwin?.SetPartSelected(type, true);
            SpatialAudioEngine.Instance?.PlayButtonClick();
            OmniOSManager.Instance?.ShowNotificationToast($"🔍 Selected: {type}");
        }

        private void TogglePCDD()
        {
            var pcdd = collabManager?.PCDD;
            if (pcdd != null)
            {
                pcdd.IsEnabled = !pcdd.IsEnabled;
                OmniOSManager.Instance?.ShowNotificationToast(pcdd.IsEnabled ? "⚡ PCDD Engine: Active (Partner Context Enabled)" : "⚡ PCDD Engine: Bypassed (Conventional Raycast)");
            }
            SpatialAudioEngine.Instance?.PlayButtonClick();
        }

        private void TestPCDDClick()
        {
            var pcdd = collabManager?.PCDD;
            var twin = collabManager?.DigitalTwin;
            if (pcdd != null && twin != null)
            {
                var bearing = twin.GetPart(SharedDigitalTwinAssembly.ComponentType.InternalBearing);
                if (bearing != null && bearing.gameObject != null)
                {
                    Vector3 targetPos = bearing.gameObject.transform.position;
                    Camera cam = Camera.main ?? UnityEngine.Object.FindAnyObjectByType<Camera>();
                    Vector3 camPos = cam != null ? cam.transform.position : Vector3.back * 2f;
                    Ray ray = new Ray(camPos, (targetPos - camPos).normalized);

                    var result = pcdd.DisambiguateRay(ray, targetPos);
                    if (result != null)
                    {
                        OmniOSManager.Instance?.ShowNotificationToast($"🎯 PCDD Success! Inferred: {result.part.displayName} ({result.partnerContextScore * 100f:F0}% confidence)");
                    }
                }
            }
            SpatialAudioEngine.Instance?.PlayNotificationChime();
        }

        private void StartHandoff()
        {
            collabManager?.TriggerQuickHandoffDemo();
        }

        private void NudgeHandoff(float delta)
        {
            collabManager?.Handoff?.ApplyNudge(new Vector3(0, 0, delta));
            SpatialAudioEngine.Instance?.PlayHoverTick();
        }

        private void CommitHandoff()
        {
            collabManager?.TriggerCommitDemo();
        }

        private void RejectHandoff()
        {
            collabManager?.TriggerRejectDemo();
        }

        private void SpawnPin(string note, Color color)
        {
            var twin = collabManager?.DigitalTwin;
            var annot = collabManager?.Annotations;
            if (twin != null && annot != null)
            {
                var bearing = twin.GetPart(SharedDigitalTwinAssembly.ComponentType.InternalBearing);
                if (bearing != null && bearing.gameObject != null)
                {
                    annot.CreateAnnotationPin(bearing.gameObject.transform, new Vector3(0.08f, 0.12f, 0f), "Desktop Collaborator", note, color);
                    OmniOSManager.Instance?.ShowNotificationToast($"📌 Placed 3D Pin: {note}");
                }
            }
        }

        private void ClearPins()
        {
            collabManager?.Annotations?.ClearAllPins();
        }

        private void SwitchPerspective(CrossDeviceCoPresence.ActivePerspective pers)
        {
            collabManager?.CoPresence?.ApplyPerspective(pers);
            SpatialAudioEngine.Instance?.PlayNotificationChime();
            OmniOSManager.Instance?.ShowNotificationToast($"🔄 Active Perspective: {pers}");
        }

        private void UpdateLiveReadouts()
        {
            if (currentTab == CollabTab.AssemblyInspect && inspectStatusText != null)
            {
                var twin = collabManager?.DigitalTwin;
                float exp = twin != null ? twin.ExplodeProgress * 100f : 0f;
                bool xray = twin != null && twin.IsXRayMode;
                inspectStatusText.text =
                    "📐 <b>Quantum Aero-Turbine Digital Twin</b>\n\n" +
                    "• <b>Total Layers:</b> 5 Nested Co-axial Stages (Titanium Casing, Fan, Shaft, Bearing, Nozzle)\n" +
                    $"• <b>Exploded View:</b> {exp:F0}% ({((exp < 5f) ? "Assembled" : "Exploded")})\n" +
                    $"• <b>Inspection Mode:</b> {(xray ? "👁️ X-Ray Glass Translucent" : "Standard Metallic Titanium")}\n" +
                    "• <b>Volumetric Snapping:</b> Sockets Active (Auto-Seat within 80mm)";
            }
            else if (currentTab == CollabTab.TaskBubble && taskBubbleStatusText != null)
            {
                var handoff = collabManager?.Handoff;
                string stateStr = handoff != null ? handoff.CurrentState.ToString() : "Idle";
                float dev = handoff != null ? handoff.DeviationDistanceMm : 0f;
                bool inSpec = handoff != null && handoff.IsWithinTolerance;
                taskBubbleStatusText.text =
                    "🤝 <b>TaskBubble: Context-Preserving Micro-Handoff & Ghost Preview</b>\n\n" +
                    $"• <b>Handoff State:</b> {stateStr}\n" +
                    "• <b>Target Part:</b> Internal Needle Roller Bearing [Tolerance Spec: ±0.25 mm]\n" +
                    $"• <b>Deviation:</b> <color={(inSpec ? "#00ff88" : "#ff4444")}>{dev:F2} mm ({(inSpec ? "Within Spec" : "Out of Tolerance")})</color>\n" +
                    "• <b>Holographic Ghost:</b> Real-time synchronized in VR space";
            }
        }

        private SpatialButton3D CreateButton(string label, Vector3 localPos, Vector2 size, Transform parent, Action onClick, Color accent)
        {
            GameObject btnObj = new GameObject($"Btn_{label.Replace(" ", "_").Replace("/", "_").Replace("|", "_")}");
            btnObj.transform.SetParent(parent, false);
            btnObj.transform.localPosition = localPos;

            var btn = btnObj.AddComponent<SpatialButton3D>();
            btn.ButtonText = label;
            btn.SetSize(size.x, size.y);
            btn.SetColors(new Color(0.08f, 0.12f, 0.20f, 0.95f), accent);
            btn.onClick.AddListener(() => onClick?.Invoke());
            return btn;
        }

        private GameObject CreateQuadPanel(string name, Vector3 localPos, Vector2 size, Transform parent, Color baseCol, Color glowCol)
        {
            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Quad);
            panel.name = name;
            panel.transform.SetParent(parent, false);
            panel.transform.localPosition = localPos;
            panel.transform.localScale = new Vector3(size.x, size.y, 1f);

            var colliders = panel.GetComponents<Collider>();
            foreach (var c in colliders) DestroyImmediate(c);

            panel.GetComponent<Renderer>().material = SpatialTheme.CreateGlassMaterial(baseCol, glowCol);
            return panel;
        }

        private TextMesh CreateText(string text, Vector3 localPos, Transform parent, int fontSize, float charSize, Color color)
        {
            GameObject go = new GameObject("Text_" + (text.Length > 8 ? text.Substring(0, 8) : text));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = Mathf.Max(fontSize * 2, 64);
            tm.characterSize = charSize * 0.5f;
            tm.fontStyle = FontStyle.Bold;
            tm.color = color;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            return tm;
        }
    }
}
