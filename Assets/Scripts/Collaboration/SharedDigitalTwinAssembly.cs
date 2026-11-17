using System;
using System.Collections.Generic;
using UnityEngine;
using OmniOS.Core;
using OmniOS.Interaction;
using OmniOS.Environment;

namespace HoloBridge.Collaboration
{
    /// <summary>
    /// SharedDigitalTwinAssembly - Multi-Layered 3D Digital Twin for Collaborative Spatial Interaction.
    /// Represents a high-fidelity Quantum Aero-Turbine Assembly with nested, occluded mechanical components:
    /// - Layer 0: Aerodynamic Titanium Outer Casing (Occluding outer shell with inspection cutaways)
    /// - Layer 1: High-Bypass Turbine Intake Fan (Rotating forward stage)
    /// - Layer 2: Central Titanium Drive Shaft (Core rotational axis)
    /// - Layer 3: Internal Needle Roller Bearing (Deeply occluded precision component - PCDD target)
    /// - Layer 4: Annular Combustion Nozzle Manifold (Precision fuel injector array)
    /// Features:
    /// - Interactive Exploded View slider (0% to 100% radial/axial expansion)
    /// - X-Ray / Glass transparency toggle for in-situ occlusion inspection
    /// - Volumetric Magnetic Snapping into nominal sockets with audio-haptic feedback
    /// - Real-time state synchronization for cross-device VR + Desktop collaboration
    /// </summary>
    public class SharedDigitalTwinAssembly : MonoBehaviour
    {
        public enum ComponentType
        {
            OuterCasing = 0,
            IntakeFan = 1,
            DriveShaft = 2,
            InternalBearing = 3,
            CombustionNozzle = 4,
            WiringHarness = 5,
            GhostWiringProxy = 6
        }

        [System.Serializable]
        public class AssemblyPart
        {
            public string partId;
            public string displayName;
            public ComponentType componentType;
            public GameObject gameObject;
            public Renderer renderer;
            public Material defaultMaterial;
            public Material xRayMaterial;
            public Vector3 nominalLocalPos;
            public Quaternion nominalLocalRot;
            public Vector3 explodedOffset;
            public bool isSelected = false;
            public bool isDelegated = false;
            public string toleranceSpec = "±0.05 mm";
        }

        [Header("Assembly Configuration")]
        [Range(0f, 1f)]
        [SerializeField] private float explodeProgress = 0.0f;
        [SerializeField] private bool isXRayMode = false;
        [SerializeField] private bool isRotating = true;
        [SerializeField] private float rotationSpeed = 15f;

        [Header("Components")]
        [SerializeField] private List<AssemblyPart> parts = new List<AssemblyPart>();

        public List<AssemblyPart> Parts => parts;
        public float ExplodeProgress
        {
            get => explodeProgress;
            set { explodeProgress = Mathf.Clamp01(value); UpdatePartPositions(); }
        }

        public bool IsXRayMode
        {
            get => isXRayMode;
            set { isXRayMode = value; UpdateMaterials(); }
        }

        public bool IsRotating
        {
            get => isRotating;
            set => isRotating = value;
        }

        public void ToggleExplodedView()
        {
            ExplodeProgress = (explodeProgress > 0.5f) ? 0.0f : 1.0f;
        }

        public void ToggleXRay()
        {
            IsXRayMode = !isXRayMode;
        }

        public enum ModelPreset
        {
            SpaceXRaptor3 = 0,
            NASAIonThruster = 1,
            CryoTurbopump = 2
        }

        [Header("Model Presets")]
        [SerializeField] private ModelPreset currentModelPreset = ModelPreset.SpaceXRaptor3;
        public ModelPreset CurrentModelPreset => currentModelPreset;

        public void SetModelPreset(int presetIndex)
        {
            currentModelPreset = (ModelPreset)Mathf.Clamp(presetIndex, 0, 2);
            hasCableCollision = true;
            isProposalActive = false;
            isProposalCommitted = false;
            BuildProceduralTurbine();
            OmniOSManager.Instance?.ShowNotificationToast($"🚀 ACTIVE CAD MODEL: {currentModelPreset}");
            SpatialAudioEngine.Instance?.PlayNotificationChime();
        }

        public void CycleNextPreset()
        {
            int next = ((int)currentModelPreset + 1) % 3;
            SetModelPreset(next);
        }

        [Header("Aerospace Wire-Harness & Consensus State")]
        [SerializeField] private bool hasCableCollision = true;
        [SerializeField] private bool isProposalActive = false;
        [SerializeField] private bool isProposalCommitted = false;

        public bool HasCableCollision => hasCableCollision;
        public bool IsProposalActive => isProposalActive;
        public bool IsProposalCommitted => isProposalCommitted;

        public void ProposeWireReroute()
        {
            isProposalActive = true;
            isProposalCommitted = false;
            var ghost = GetPart(ComponentType.GhostWiringProxy);
            if (ghost != null && ghost.gameObject != null)
            {
                ghost.gameObject.SetActive(true);
            }
            UpdateMaterials();
            OmniOSManager.Instance?.ShowNotificationToast("⚡ PROPOSED WIRE RE-ROUTE ACTIVE (+3.2mm Clearance)");
            SpatialAudioEngine.Instance?.PlayThemeChime();
        }

        public bool CommitProposal()
        {
            hasCableCollision = false;
            isProposalActive = false;
            isProposalCommitted = true;

            var ghost = GetPart(ComponentType.GhostWiringProxy);
            if (ghost != null && ghost.gameObject != null)
            {
                ghost.gameObject.SetActive(false);
            }

            var wire = GetPart(ComponentType.WiringHarness);
            if (wire != null && wire.gameObject != null)
            {
                wire.nominalLocalPos = new Vector3(0.04f, 0.03f, 0.12f);
                UpdatePartPositions();
            }

            UpdateMaterials();
            OmniOSManager.Instance?.ShowNotificationToast("✅ [AGREE] CONSENSUS COMMITTED: WIRE RE-ROUTE LOCKED!");
            SpatialAudioEngine.Instance?.PlayNotificationChime();
            return true;
        }

        public bool RejectProposal()
        {
            isProposalActive = false;
            isProposalCommitted = false;

            var ghost = GetPart(ComponentType.GhostWiringProxy);
            if (ghost != null && ghost.gameObject != null)
            {
                ghost.gameObject.SetActive(false);
            }

            UpdateMaterials();
            OmniOSManager.Instance?.ShowNotificationToast("✕ [DISAGREE] PROPOSAL REJECTED: REVERTED TO BASELINE");
            SpatialAudioEngine.Instance?.PlayButtonClick();
            return true;
        }

        private static SharedDigitalTwinAssembly instance;
        public static SharedDigitalTwinAssembly Instance => instance;

        private Transform assemblyRoot;
        private Material highlightMatCyan;
        private Material highlightMatAmber;
        private Material ghostMat;

        private void Awake()
        {
            instance = this;
            BuildProceduralTurbine();
        }

        private void Update()
        {
            if (isRotating && explodeProgress < 0.1f)
            {
                // Gently rotate fan and shaft when in assembled state
                var fan = GetPart(ComponentType.IntakeFan);
                if (fan != null && fan.gameObject != null)
                {
                    fan.gameObject.transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime, Space.Self);
                }
            }

            // Direct Quick-Keys for Unity Editor & VR Testing:
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetModelPreset(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SetModelPreset(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SetModelPreset(2);
            if (Input.GetKeyDown(KeyCode.R)) MetaverseEnvironmentManager.Instance?.ToggleCleanRoom();
            if (Input.GetKeyDown(KeyCode.Y)) CommitProposal();
            if (Input.GetKeyDown(KeyCode.N)) RejectProposal();
            if (Input.GetKeyDown(KeyCode.Z)) ToggleXRay();
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.X)) ToggleExplodedView();
        }

        public AssemblyPart GetPart(ComponentType type)
        {
            return parts.Find(p => p.componentType == type);
        }

        public AssemblyPart GetPartByGameObject(GameObject go)
        {
            return parts.Find(p => p.gameObject == go || go.transform.IsChildOf(p.gameObject.transform));
        }

        private void BuildProceduralTurbine()
        {
            if (assemblyRoot != null)
            {
                Destroy(assemblyRoot.gameObject);
            }
            parts.Clear();

            assemblyRoot = new GameObject($"DigitalTwin_{currentModelPreset}").transform;
            assemblyRoot.SetParent(transform, false);
            assemblyRoot.localPosition = Vector3.zero;
            assemblyRoot.localRotation = Quaternion.identity;

            // Shared shader fallbacks
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");

            highlightMatCyan = new Material(litShader);
            highlightMatCyan.color = new Color(0.0f, 0.95f, 1.0f, 0.95f);
            if (highlightMatCyan.HasProperty("_EmissionColor"))
            {
                highlightMatCyan.EnableKeyword("_EMISSION");
                highlightMatCyan.SetColor("_EmissionColor", new Color(0.0f, 0.8f, 1.0f) * 1.5f);
            }

            highlightMatAmber = new Material(litShader);
            highlightMatAmber.color = new Color(1.0f, 0.65f, 0.15f, 0.95f);
            if (highlightMatAmber.HasProperty("_EmissionColor"))
            {
                highlightMatAmber.EnableKeyword("_EMISSION");
                highlightMatAmber.SetColor("_EmissionColor", new Color(1.0f, 0.5f, 0.05f) * 1.5f);
            }

            // 1. Layer 0: Titanium Outer Casing (Occludes the interior)
            CreateOuterCasing(litShader);

            // 2. Layer 1: High-Bypass Turbine Intake Fan
            CreateIntakeFan(litShader);

            // 3. Layer 2: Central Titanium Drive Shaft
            CreateDriveShaft(litShader);

            // 4. Layer 3: Internal Needle Roller Bearing (Deep occluded target!)
            CreateInternalBearing(litShader);

            // 5. Layer 4: Annular Combustion Nozzle Manifold
            CreateCombustionNozzle(litShader);

            // 6. Layer 5: Telemetry Wiring Harness Bus 04 (⚠️ Chafing Hazard & Cable Clash)
            CreateWiringHarness(litShader);

            // 7. Layer 6: Holographic Safe Re-Route Ghost Proxy (+3.2mm Clearance)
            CreateGhostWiringProxy(litShader);

            // 8. In-World VR Holographic Mission Console (For Meta Quest 3 & XR direct interaction)
            BuildWorldSpaceVRConsole(litShader);

            UpdatePartPositions();
            UpdateMaterials();
        }

        #region VR World-Space Holographic Console
        private GameObject vrConsoleRoot;
        private TextMesh consoleStatusTextMesh;

        private void BuildWorldSpaceVRConsole(Shader litShader)
        {
            if (vrConsoleRoot != null) Destroy(vrConsoleRoot);

            vrConsoleRoot = new GameObject("VR_Aerospace_HoloConsole");
            vrConsoleRoot.transform.SetParent(assemblyRoot, false);
            // Positioned conveniently to the right of the engine, tilted towards user
            vrConsoleRoot.transform.localPosition = new Vector3(0.92f, 0.15f, -0.15f);
            vrConsoleRoot.transform.localRotation = Quaternion.Euler(0f, -28f, 0f);

            // Floating Glass Backplate
            GameObject backplate = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backplate.name = "Console_GlassBackplate";
            backplate.transform.SetParent(vrConsoleRoot.transform, false);
            backplate.transform.localScale = new Vector3(0.95f, 0.72f, 1f);
            var colliders = backplate.GetComponents<Collider>();
            foreach (var c in colliders) Destroy(c);

            Material glassMat = SpatialTheme.CreateGlassMaterial(
                new Color(0.05f, 0.08f, 0.14f, 0.94f),
                new Color(0.0f, 0.95f, 1.0f, 0.8f)
            );
            backplate.GetComponent<Renderer>().material = glassMat;

            // Header Text
            GameObject headerObj = new GameObject("Console_Header");
            headerObj.transform.SetParent(vrConsoleRoot.transform, false);
            headerObj.transform.localPosition = new Vector3(0f, 0.28f, -0.015f);
            var tm = headerObj.AddComponent<TextMesh>();
            tm.text = "🚀 AEROSPACE CAD CONSOLE";
            tm.fontSize = 24;
            tm.characterSize = 0.014f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = new Color(0.0f, 0.95f, 1.0f);

            // Status Text
            GameObject statusObj = new GameObject("Console_Status");
            statusObj.transform.SetParent(vrConsoleRoot.transform, false);
            statusObj.transform.localPosition = new Vector3(0f, 0.21f, -0.015f);
            consoleStatusTextMesh = statusObj.AddComponent<TextMesh>();
            consoleStatusTextMesh.fontSize = 18;
            consoleStatusTextMesh.characterSize = 0.012f;
            consoleStatusTextMesh.anchor = TextAnchor.MiddleCenter;
            UpdateConsoleStatusText();

            // 3D Spatial Buttons (Interactable with VR laser ray and finger poke)
            CreateConsoleButton("🚀 Raptor 3", new Vector3(-0.28f, 0.10f, -0.02f), new Vector2(0.26f, 0.07f), () => SetModelPreset(0), new Color(0.0f, 0.85f, 1.0f));
            CreateConsoleButton("⚡ Ion Thruster", new Vector3(0f, 0.10f, -0.02f), new Vector2(0.26f, 0.07f), () => SetModelPreset(1), new Color(0.2f, 0.85f, 1.0f));
            CreateConsoleButton("⚙️ Turbopump", new Vector3(0.28f, 0.10f, -0.02f), new Vector2(0.26f, 0.07f), () => SetModelPreset(2), new Color(0.3f, 0.95f, 0.6f));

            CreateConsoleButton("🏛️ Cleanroom", new Vector3(-0.28f, 0.00f, -0.02f), new Vector2(0.26f, 0.07f), () => MetaverseEnvironmentManager.Instance?.ToggleCleanRoom(), new Color(0.8f, 0.45f, 1.0f));
            CreateConsoleButton("💥 Explode", new Vector3(0f, 0.00f, -0.02f), new Vector2(0.26f, 0.07f), () => ToggleExplodedView(), new Color(1.0f, 0.65f, 0.15f));
            CreateConsoleButton("👁️ X-Ray", new Vector3(0.28f, 0.00f, -0.02f), new Vector2(0.26f, 0.07f), () => ToggleXRay(), new Color(0.2f, 0.85f, 1.0f));

            // Big Consensus Buttons (Row 3)
            CreateConsoleButton("✅ AGREE [Commit]", new Vector3(-0.20f, -0.14f, -0.02f), new Vector2(0.38f, 0.09f), () => CommitProposal(), new Color(0.0f, 1.0f, 0.55f));
            CreateConsoleButton("❌ DISAGREE [Revert]", new Vector3(0.20f, -0.14f, -0.02f), new Vector2(0.38f, 0.09f), () => RejectProposal(), new Color(1.0f, 0.35f, 0.35f));

            // Footer Hint
            GameObject footerObj = new GameObject("Console_Footer");
            footerObj.transform.SetParent(vrConsoleRoot.transform, false);
            footerObj.transform.localPosition = new Vector3(0f, -0.26f, -0.015f);
            var footTm = footerObj.AddComponent<TextMesh>();
            footTm.text = "VR: Aim Controller Laser or Poke with Finger • PC: Keys 1,2,3,R,Y,N,X,Z";
            footTm.fontSize = 14;
            footTm.characterSize = 0.009f;
            footTm.anchor = TextAnchor.MiddleCenter;
            footTm.color = new Color(0.45f, 0.55f, 0.65f);
        }

        private void CreateConsoleButton(string label, Vector3 localPos, Vector2 size, Action onClick, Color accentCol)
        {
            GameObject btnObj = new GameObject($"VRBtn_{label.Replace(" ", "_")}");
            btnObj.transform.SetParent(vrConsoleRoot.transform, false);
            btnObj.transform.localPosition = localPos;

            var spatialBtn = btnObj.AddComponent<SpatialButton3D>();
            spatialBtn.ButtonText = label;
            spatialBtn.SetSize(size.x, size.y);
            spatialBtn.SetColors(new Color(0.08f, 0.11f, 0.16f, 0.95f), accentCol);
            spatialBtn.onClick.AddListener(() => onClick?.Invoke());
        }

        private void UpdateConsoleStatusText()
        {
            if (consoleStatusTextMesh == null) return;
            if (isProposalCommitted)
            {
                consoleStatusTextMesh.text = $"Model: {currentModelPreset} • <color=#00ff88>LOCKED: +3.20mm (SAFE)</color>";
            }
            else if (isProposalActive)
            {
                consoleStatusTextMesh.text = $"Model: {currentModelPreset} • <color=#00f0ff>GHOST ACTIVE (+3.20mm)</color>";
            }
            else
            {
                consoleStatusTextMesh.text = $"Model: {currentModelPreset} • <color=#ffaa00>CLASH: Line 04 (0.00mm)</color>";
            }
        }
        #endregion

        private void CreateOuterCasing(Shader litShader)
        {
            GameObject casing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            casing.name = "Part_OuterCasing";
            casing.transform.SetParent(assemblyRoot, false);
            casing.transform.localPosition = new Vector3(0, 0, 0);
            casing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            casing.transform.localScale = new Vector3(0.68f, 0.38f, 0.68f);

            var mat = new Material(litShader);
            mat.color = new Color(0.18f, 0.22f, 0.30f, 0.95f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.85f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.65f);
            casing.GetComponent<Renderer>().material = mat;

            // X-Ray transparent material
            var xRayMat = SpatialTheme.CreateGlassMaterial(
                new Color(0.10f, 0.20f, 0.35f, 0.25f),
                new Color(0.0f, 0.8f, 1.0f, 0.8f)
            );

            parts.Add(new AssemblyPart
            {
                partId = "TURB_CASING_01",
                displayName = "Outer Casing (Titanium Shell)",
                componentType = ComponentType.OuterCasing,
                gameObject = casing,
                renderer = casing.GetComponent<Renderer>(),
                defaultMaterial = mat,
                xRayMaterial = xRayMat,
                nominalLocalPos = new Vector3(0, 0, 0),
                nominalLocalRot = Quaternion.Euler(90f, 0f, 0f),
                explodedOffset = new Vector3(0, 0.42f, 0),
                toleranceSpec = "±0.50 mm"
            });
        }

        private void CreateIntakeFan(Shader litShader)
        {
            GameObject fanRoot = new GameObject("Part_IntakeFan");
            fanRoot.transform.SetParent(assemblyRoot, false);
            fanRoot.transform.localPosition = new Vector3(0, 0, -0.32f);

            // Hub
            GameObject hub = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hub.name = "Fan_Hub";
            hub.transform.SetParent(fanRoot.transform, false);
            hub.transform.localScale = new Vector3(0.24f, 0.24f, 0.32f);

            var mat = new Material(litShader);
            mat.color = new Color(0.85f, 0.88f, 0.92f, 1.0f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.95f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.85f);
            hub.GetComponent<Renderer>().material = mat;

            // 12 Aerodynamic Blades
            for (int i = 0; i < 12; i++)
            {
                float angle = i * (360f / 12f);
                GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blade.name = $"Blade_{i}";
                blade.transform.SetParent(fanRoot.transform, false);
                blade.transform.localScale = new Vector3(0.042f, 0.26f, 0.012f);
                blade.transform.localPosition = Quaternion.Euler(0, 0, angle) * new Vector3(0, 0.16f, 0);
                blade.transform.localRotation = Quaternion.Euler(22f, 0, angle);
                blade.GetComponent<Renderer>().material = mat;
            }

            // Ensure collider on fan root for raycasting
            var col = fanRoot.AddComponent<BoxCollider>();
            col.size = new Vector3(0.62f, 0.62f, 0.35f);

            parts.Add(new AssemblyPart
            {
                partId = "TURB_FAN_02",
                displayName = "Intake Turbine Fan Stage",
                componentType = ComponentType.IntakeFan,
                gameObject = fanRoot,
                renderer = hub.GetComponent<Renderer>(),
                defaultMaterial = mat,
                xRayMaterial = mat,
                nominalLocalPos = new Vector3(0, 0, -0.32f),
                nominalLocalRot = Quaternion.identity,
                explodedOffset = new Vector3(0, 0, -0.55f),
                toleranceSpec = "±0.10 mm"
            });
        }

        private void CreateDriveShaft(Shader litShader)
        {
            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = "Part_DriveShaft";
            shaft.transform.SetParent(assemblyRoot, false);
            shaft.transform.localPosition = new Vector3(0, 0, 0);
            shaft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shaft.transform.localScale = new Vector3(0.12f, 0.44f, 0.12f);

            var mat = new Material(litShader);
            mat.color = new Color(0.35f, 0.40f, 0.48f, 1f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.90f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.70f);
            shaft.GetComponent<Renderer>().material = mat;

            parts.Add(new AssemblyPart
            {
                partId = "TURB_SHAFT_03",
                displayName = "Central Drive Shaft",
                componentType = ComponentType.DriveShaft,
                gameObject = shaft,
                renderer = shaft.GetComponent<Renderer>(),
                defaultMaterial = mat,
                xRayMaterial = mat,
                nominalLocalPos = new Vector3(0, 0, 0),
                nominalLocalRot = Quaternion.Euler(90f, 0f, 0f),
                explodedOffset = new Vector3(0, -0.35f, 0),
                toleranceSpec = "±0.02 mm"
            });
        }

        private void CreateInternalBearing(Shader litShader)
        {
            // Deeply occluded component inside the casing and wrapped around the drive shaft
            GameObject bearingRoot = new GameObject("Part_InternalNeedleBearing");
            bearingRoot.transform.SetParent(assemblyRoot, false);
            bearingRoot.transform.localPosition = new Vector3(0, 0, 0.05f);

            // Outer Ring
            GameObject outerRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            outerRing.name = "Bearing_OuterRing";
            outerRing.transform.SetParent(bearingRoot.transform, false);
            outerRing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            outerRing.transform.localScale = new Vector3(0.24f, 0.065f, 0.24f);

            // Precision Gold/Brass Bearing Material for high visual contrast
            var mat = new Material(litShader);
            mat.color = new Color(1.0f, 0.78f, 0.22f, 1.0f); // High-visibility golden brass
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.95f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.90f);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(0.8f, 0.5f, 0.1f) * 0.4f);
            }
            outerRing.GetComponent<Renderer>().material = mat;

            // 8 Cylindrical Roller Pins
            for (int i = 0; i < 8; i++)
            {
                float angle = i * (360f / 8f);
                GameObject pin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pin.name = $"RollerPin_{i}";
                pin.transform.SetParent(bearingRoot.transform, false);
                pin.transform.localScale = new Vector3(0.025f, 0.055f, 0.025f);
                pin.transform.localPosition = Quaternion.Euler(0, 0, angle) * new Vector3(0, 0.088f, 0);
                pin.transform.localRotation = Quaternion.Euler(90f, 0, 0);
                pin.GetComponent<Renderer>().material = mat;
            }

            var col = bearingRoot.AddComponent<BoxCollider>();
            col.size = new Vector3(0.26f, 0.26f, 0.12f);

            parts.Add(new AssemblyPart
            {
                partId = "TURB_BEARING_04",
                displayName = "Internal Needle Roller Bearing [PCDD Target]",
                componentType = ComponentType.InternalBearing,
                gameObject = bearingRoot,
                renderer = outerRing.GetComponent<Renderer>(),
                defaultMaterial = mat,
                xRayMaterial = mat,
                nominalLocalPos = new Vector3(0, 0, 0.05f),
                nominalLocalRot = Quaternion.identity,
                explodedOffset = new Vector3(0, 0.22f, 0.35f),
                toleranceSpec = "±0.005 mm (Sub-Millimeter Critical)"
            });
        }

        private void CreateCombustionNozzle(Shader litShader)
        {
            GameObject nozzleRoot = new GameObject("Part_CombustionNozzle");
            nozzleRoot.transform.SetParent(assemblyRoot, false);
            nozzleRoot.transform.localPosition = new Vector3(0, 0, 0.34f);

            // Manifold Ring
            GameObject manifold = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            manifold.name = "Nozzle_ManifoldRing";
            manifold.transform.SetParent(nozzleRoot.transform, false);
            manifold.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            manifold.transform.localScale = new Vector3(0.48f, 0.08f, 0.48f);

            var mat = new Material(litShader);
            mat.color = new Color(0.25f, 0.30f, 0.36f, 1.0f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.85f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.60f);
            manifold.GetComponent<Renderer>().material = mat;

            // 6 Injector Nozzles with Cyan Fuel Glow
            for (int i = 0; i < 6; i++)
            {
                float angle = i * (360f / 6f);
                GameObject nozzle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                nozzle.name = $"Injector_{i}";
                nozzle.transform.SetParent(nozzleRoot.transform, false);
                nozzle.transform.localScale = new Vector3(0.045f, 0.09f, 0.045f);
                nozzle.transform.localPosition = Quaternion.Euler(0, 0, angle) * new Vector3(0, 0.18f, 0);
                nozzle.transform.localRotation = Quaternion.Euler(90f, 0, angle);

                var nozMat = new Material(litShader);
                nozMat.color = new Color(0.0f, 0.85f, 0.95f);
                if (nozMat.HasProperty("_EmissionColor"))
                {
                    nozMat.EnableKeyword("_EMISSION");
                    nozMat.SetColor("_EmissionColor", new Color(0.0f, 0.7f, 1.0f) * 1.2f);
                }
                nozzle.GetComponent<Renderer>().material = nozMat;
            }

            var col = nozzleRoot.AddComponent<BoxCollider>();
            col.size = new Vector3(0.52f, 0.52f, 0.20f);

            parts.Add(new AssemblyPart
            {
                partId = "TURB_NOZZLE_05",
                displayName = "Combustion Fuel Nozzle Manifold",
                componentType = ComponentType.CombustionNozzle,
                gameObject = nozzleRoot,
                renderer = manifold.GetComponent<Renderer>(),
                defaultMaterial = mat,
                xRayMaterial = mat,
                nominalLocalPos = new Vector3(0, 0, 0.34f),
                nominalLocalRot = Quaternion.identity,
                explodedOffset = new Vector3(0, 0, 0.65f),
                toleranceSpec = "±0.05 mm"
            });
        }

        private void CreateWiringHarness(Shader litShader)
        {
            GameObject harnessRoot = new GameObject("Part_WiringHarness_Bus04");
            harnessRoot.transform.SetParent(assemblyRoot, false);
            harnessRoot.transform.localPosition = new Vector3(0, 0, 0.12f);

            for (int i = 0; i < 4; i++)
            {
                float angle = i * 45f + 15f;
                GameObject wire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wire.name = $"HarnessCable_{i + 1}";
                wire.transform.SetParent(harnessRoot.transform, false);
                wire.transform.localScale = new Vector3(0.022f, 0.28f, 0.022f);
                wire.transform.localPosition = Quaternion.Euler(0, 0, angle) * new Vector3(0.24f, 0, 0);
                wire.transform.localRotation = Quaternion.Euler(90f, 0, angle);

                var wireMat = new Material(litShader);
                if (i == 3)
                {
                    wireMat.color = new Color(1.0f, 0.35f, 0.05f);
                    if (wireMat.HasProperty("_EmissionColor"))
                    {
                        wireMat.EnableKeyword("_EMISSION");
                        wireMat.SetColor("_EmissionColor", new Color(1.0f, 0.2f, 0.0f) * 1.6f);
                    }
                }
                else
                {
                    wireMat.color = new Color(0.12f, 0.16f, 0.22f);
                }
                wire.GetComponent<Renderer>().material = wireMat;
            }

            var col = harnessRoot.AddComponent<BoxCollider>();
            col.size = new Vector3(0.55f, 0.55f, 0.35f);

            var defaultMat = new Material(litShader);
            defaultMat.color = new Color(1.0f, 0.35f, 0.05f);

            parts.Add(new AssemblyPart
            {
                partId = "WIRE_HARNESS_BUS04",
                displayName = "Ignition Wire Harness Bus 04 (⚠️ Chafing Hazard)",
                componentType = ComponentType.WiringHarness,
                gameObject = harnessRoot,
                renderer = harnessRoot.transform.GetChild(3).GetComponent<Renderer>(),
                defaultMaterial = defaultMat,
                xRayMaterial = defaultMat,
                nominalLocalPos = new Vector3(0, 0, 0.12f),
                nominalLocalRot = Quaternion.identity,
                explodedOffset = new Vector3(0.25f, 0, 0.12f),
                toleranceSpec = "±0.02 mm"
            });
        }

        private void CreateGhostWiringProxy(Shader litShader)
        {
            GameObject ghostRoot = new GameObject("Part_GhostWiringProxy");
            ghostRoot.transform.SetParent(assemblyRoot, false);
            ghostRoot.transform.localPosition = new Vector3(0.04f, 0.03f, 0.12f);

            GameObject ghostTube = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ghostTube.name = "GhostWire_SafePath";
            ghostTube.transform.SetParent(ghostRoot.transform, false);
            ghostTube.transform.localScale = new Vector3(0.026f, 0.29f, 0.026f);
            ghostTube.transform.localPosition = Quaternion.Euler(0, 0, 150f) * new Vector3(0.28f, 0, 0);
            ghostTube.transform.localRotation = Quaternion.Euler(90f, 0, 150f);

            var ghostMat = SpatialTheme.CreateGlassMaterial(
                new Color(0.0f, 0.95f, 0.65f, 0.45f),
                new Color(0.0f, 1.0f, 0.6f, 0.9f)
            );
            ghostTube.GetComponent<Renderer>().material = ghostMat;

            ghostRoot.SetActive(false);

            parts.Add(new AssemblyPart
            {
                partId = "GHOST_WIRE_PROXY",
                displayName = "Holographic Re-Route Ghost (+3.2mm Clearance)",
                componentType = ComponentType.GhostWiringProxy,
                gameObject = ghostRoot,
                renderer = ghostTube.GetComponent<Renderer>(),
                defaultMaterial = ghostMat,
                xRayMaterial = ghostMat,
                nominalLocalPos = new Vector3(0.04f, 0.03f, 0.12f),
                nominalLocalRot = Quaternion.identity,
                explodedOffset = new Vector3(0.25f, 0, 0.12f),
                toleranceSpec = "Clearance: +3.20 mm"
            });
        }

        public void UpdatePartPositions()
        {
            foreach (var part in parts)
            {
                if (part.gameObject == null) continue;
                Vector3 targetPos = Vector3.Lerp(part.nominalLocalPos, part.nominalLocalPos + part.explodedOffset, explodeProgress);
                part.gameObject.transform.localPosition = targetPos;
            }
        }

        public void UpdateMaterials()
        {
            foreach (var part in parts)
            {
                if (part.renderer == null) continue;

                if (part.isDelegated)
                {
                    part.renderer.material = highlightMatAmber;
                }
                else if (part.isSelected)
                {
                    part.renderer.material = highlightMatCyan;
                }
                else if (part.componentType == ComponentType.WiringHarness)
                {
                    if (isProposalCommitted)
                    {
                        part.renderer.material = highlightMatCyan;
                    }
                    else if (hasCableCollision)
                    {
                        part.renderer.material = highlightMatAmber;
                    }
                    else
                    {
                        part.renderer.material = part.defaultMaterial;
                    }
                }
                else
                {
                    part.renderer.material = (isXRayMode && part.componentType == ComponentType.OuterCasing)
                        ? part.xRayMaterial
                        : part.defaultMaterial;
                }
            }
        }

        public void SetPartSelected(ComponentType type, bool selected)
        {
            foreach (var p in parts)
            {
                if (p.componentType == type) p.isSelected = selected;
                else if (selected) p.isSelected = false; // Single active selection
            }
            UpdateMaterials();
        }

        public void SetPartDelegated(ComponentType type, bool delegated)
        {
            var p = GetPart(type);
            if (p != null)
            {
                p.isDelegated = delegated;
                UpdateMaterials();
            }
        }

        public void ResetAssembly()
        {
            explodeProgress = 0f;
            isXRayMode = false;
            foreach (var p in parts)
            {
                p.isSelected = false;
                p.isDelegated = false;
            }
            UpdatePartPositions();
            UpdateMaterials();
        }
    }
}
