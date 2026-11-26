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
    /// Provides 3 distinct aerospace procedural CAD models for IEEE VR 2027 3DUI Contest:
    /// 1. SpaceX Raptor 3 Full-Flow Staged Combustion Rocket Engine
    /// 2. NASA NEXT-C Deep Space Gridded Ion Thruster (with Electric Blue Plasma Jet)
    /// 3. Cryogenic LOX High-Pressure Turbopump (with Spiral Volute & Inducer Screw)
    /// 
    /// Features:
    /// - Interactive Exploded View (0% to 100% radial/axial expansion)
    /// - X-Ray / Glass transparency toggle for in-situ occlusion inspection
    /// - Consensus Voting Wire Re-Route ([✓ AGREE] / [✕ DISAGREE]) with +3.2mm Clearance
    /// - In-World 3D Holographic VR Mission Console interactable via VR ray, finger poke & PC
    /// - Zero-dependency AutoInitialize ensures presence in all Unity scenes at natural eye level
    /// </summary>
    [DisallowMultipleComponent]
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

        public enum ModelPreset
        {
            SpaceXRaptor3 = 0,
            NASAIonThruster = 1,
            CryoTurbopump = 2
        }

        [Header("Model Presets")]
        [SerializeField] private ModelPreset currentModelPreset = ModelPreset.SpaceXRaptor3;
        public ModelPreset CurrentModelPreset => currentModelPreset;

        [Header("Assembly Configuration")]
        [Range(0f, 1f)]
        [SerializeField] private float explodeProgress = 0.0f;
        [SerializeField] private bool isXRayMode = false;
        [SerializeField] private bool isRotating = true;
        [SerializeField] private float rotationSpeed = 20f;

        [Header("Aerospace Wire-Harness & Consensus State")]
        [SerializeField] private bool hasCableCollision = true;
        [SerializeField] private bool isProposalActive = false;
        [SerializeField] private bool isProposalCommitted = false;

        public bool HasCableCollision => hasCableCollision;
        public bool IsProposalActive => isProposalActive;
        public bool IsProposalCommitted => isProposalCommitted;

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

        private static SharedDigitalTwinAssembly instance;
        public static SharedDigitalTwinAssembly Instance => instance;

        private Transform assemblyRoot;
        private Material highlightMatCyan;
        private Material highlightMatAmber;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (instance == null)
            {
                var existing = UnityEngine.Object.FindAnyObjectByType<SharedDigitalTwinAssembly>();
                if (existing != null)
                {
                    instance = existing;
                }
                else
                {
                    GameObject go = new GameObject("[SHARED_DIGITAL_TWIN]");
                    go.transform.position = new Vector3(0f, 1.35f, 1.85f);
                    instance = go.AddComponent<SharedDigitalTwinAssembly>();
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
            BuildProceduralTurbine();
        }

        private void Update()
        {
            // Spin the active rotational element (Turbine Impeller / Inducer / Cathode)
            if (isRotating && explodeProgress < 0.1f)
            {
                var rotatingPart = GetPart(ComponentType.IntakeFan);
                if (rotatingPart != null && rotatingPart.gameObject != null)
                {
                    rotatingPart.gameObject.transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime, Space.Self);
                }
            }

            // Direct Quick-Keys for Unity Editor & PC Testing:
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetModelPreset(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SetModelPreset(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SetModelPreset(2);
            if (Input.GetKeyDown(KeyCode.R)) MetaverseEnvironmentManager.Instance?.ToggleCleanRoom();
            if (Input.GetKeyDown(KeyCode.Y)) CommitProposal();
            if (Input.GetKeyDown(KeyCode.N)) RejectProposal();
            if (Input.GetKeyDown(KeyCode.Z)) ToggleXRay();
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.X)) ToggleExplodedView();
        }

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

        public void ToggleExplodedView()
        {
            ExplodeProgress = (explodeProgress > 0.5f) ? 0.0f : 1.0f;
        }

        public void ToggleXRay()
        {
            IsXRayMode = !isXRayMode;
        }

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
            UpdateConsoleStatusText();
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
            UpdateConsoleStatusText();
            OmniOSManager.Instance?.ShowNotificationToast("✅ [AGREE] CONSENSUS COMMITTED: WIRE RE-ROUTE LOCKED!");
            SpatialAudioEngine.Instance?.PlayNotificationChime();
            return true;
        }

        public bool RejectProposal()
        {
            isProposalActive = false;
            isProposalCommitted = false;
            hasCableCollision = true;

            var ghost = GetPart(ComponentType.GhostWiringProxy);
            if (ghost != null && ghost.gameObject != null)
            {
                ghost.gameObject.SetActive(false);
            }

            var wire = GetPart(ComponentType.WiringHarness);
            if (wire != null && wire.gameObject != null)
            {
                wire.nominalLocalPos = (currentModelPreset == ModelPreset.CryoTurbopump) ? new Vector3(0.02f, -0.01f, 0.10f) : new Vector3(0f, 0f, 0.12f);
                UpdatePartPositions();
            }

            UpdateMaterials();
            UpdateConsoleStatusText();
            OmniOSManager.Instance?.ShowNotificationToast("✕ [DISAGREE] PROPOSAL REJECTED: REVERTED TO BASELINE");
            SpatialAudioEngine.Instance?.PlayButtonClick();
            return true;
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

            // Distinct procedural CAD mesh generation per preset
            switch (currentModelPreset)
            {
                case ModelPreset.SpaceXRaptor3:
                    BuildRaptor3Engine(litShader);
                    break;
                case ModelPreset.NASAIonThruster:
                    BuildIonThruster(litShader);
                    break;
                case ModelPreset.CryoTurbopump:
                    BuildCryoTurbopump(litShader);
                    break;
            }

            // In-World VR Holographic Mission Console (For Meta Quest 3, XR controllers & PC clicking)
            BuildWorldSpaceVRConsole(litShader);

            UpdatePartPositions();
            UpdateMaterials();
        }

        #region Preset 0: SpaceX Raptor 3 Rocket Engine
        private void BuildRaptor3Engine(Shader litShader)
        {
            // 1. Titanium Shroud with Cutaway Inspection Windows
            GameObject casing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            casing.name = "Part_Raptor_OuterShroud";
            casing.transform.SetParent(assemblyRoot, false);
            casing.transform.localPosition = Vector3.zero;
            casing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            casing.transform.localScale = new Vector3(0.68f, 0.38f, 0.68f);

            var casingMat = new Material(litShader);
            casingMat.color = new Color(0.18f, 0.22f, 0.30f, 0.95f);
            if (casingMat.HasProperty("_Metallic")) casingMat.SetFloat("_Metallic", 0.88f);
            if (casingMat.HasProperty("_Smoothness")) casingMat.SetFloat("_Smoothness", 0.70f);
            casing.GetComponent<Renderer>().material = casingMat;

            var xRayMat = SpatialTheme.CreateGlassMaterial(
                new Color(0.10f, 0.20f, 0.35f, 0.25f),
                new Color(0.0f, 0.8f, 1.0f, 0.8f)
            );

            parts.Add(new AssemblyPart
            {
                partId = "RAPTOR_SHROUD_01",
                displayName = "Titanium Casing (Cutaway Inspection Shell)",
                componentType = ComponentType.OuterCasing,
                gameObject = casing,
                renderer = casing.GetComponent<Renderer>(),
                defaultMaterial = casingMat,
                xRayMaterial = xRayMat,
                nominalLocalPos = Vector3.zero,
                nominalLocalRot = Quaternion.Euler(90f, 0f, 0f),
                explodedOffset = new Vector3(0, 0.42f, 0),
                toleranceSpec = "±0.50 mm"
            });

            // 2. High-Bypass Turbine Intake Fan (12 Aerodynamic Blades + Hub)
            GameObject fanRoot = new GameObject("Part_Raptor_IntakeFan");
            fanRoot.transform.SetParent(assemblyRoot, false);
            fanRoot.transform.localPosition = new Vector3(0, 0, -0.32f);

            GameObject hub = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hub.name = "Raptor_Hub";
            hub.transform.SetParent(fanRoot.transform, false);
            hub.transform.localScale = new Vector3(0.24f, 0.24f, 0.32f);

            var fanMat = new Material(litShader);
            fanMat.color = new Color(0.85f, 0.88f, 0.92f, 1.0f);
            if (fanMat.HasProperty("_Metallic")) fanMat.SetFloat("_Metallic", 0.95f);
            if (fanMat.HasProperty("_Smoothness")) fanMat.SetFloat("_Smoothness", 0.85f);
            hub.GetComponent<Renderer>().material = fanMat;

            for (int i = 0; i < 12; i++)
            {
                float angle = i * 30f;
                GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blade.name = $"Blade_{i}";
                blade.transform.SetParent(fanRoot.transform, false);
                blade.transform.localScale = new Vector3(0.042f, 0.26f, 0.012f);
                blade.transform.localPosition = Quaternion.Euler(0, 0, angle) * new Vector3(0, 0.16f, 0);
                blade.transform.localRotation = Quaternion.Euler(22f, 0, angle);
                blade.GetComponent<Renderer>().material = fanMat;
            }

            var fanCol = fanRoot.AddComponent<BoxCollider>();
            fanCol.size = new Vector3(0.62f, 0.62f, 0.35f);

            parts.Add(new AssemblyPart
            {
                partId = "RAPTOR_FAN_02",
                displayName = "High-Bypass Turbine Intake Stage",
                componentType = ComponentType.IntakeFan,
                gameObject = fanRoot,
                renderer = hub.GetComponent<Renderer>(),
                defaultMaterial = fanMat,
                xRayMaterial = fanMat,
                nominalLocalPos = new Vector3(0, 0, -0.32f),
                nominalLocalRot = Quaternion.identity,
                explodedOffset = new Vector3(0, 0, -0.55f),
                toleranceSpec = "±0.10 mm"
            });

            // 3. Central Titanium Drive Shaft
            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = "Part_Raptor_DriveShaft";
            shaft.transform.SetParent(assemblyRoot, false);
            shaft.transform.localPosition = Vector3.zero;
            shaft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shaft.transform.localScale = new Vector3(0.12f, 0.44f, 0.12f);

            var shaftMat = new Material(litShader);
            shaftMat.color = new Color(0.35f, 0.40f, 0.48f, 1f);
            if (shaftMat.HasProperty("_Metallic")) shaftMat.SetFloat("_Metallic", 0.90f);
            if (shaftMat.HasProperty("_Smoothness")) shaftMat.SetFloat("_Smoothness", 0.70f);
            shaft.GetComponent<Renderer>().material = shaftMat;

            parts.Add(new AssemblyPart
            {
                partId = "RAPTOR_SHAFT_03",
                displayName = "Central Drive Shaft",
                componentType = ComponentType.DriveShaft,
                gameObject = shaft,
                renderer = shaft.GetComponent<Renderer>(),
                defaultMaterial = shaftMat,
                xRayMaterial = shaftMat,
                nominalLocalPos = Vector3.zero,
                nominalLocalRot = Quaternion.Euler(90f, 0f, 0f),
                explodedOffset = new Vector3(0, -0.35f, 0),
                toleranceSpec = "±0.02 mm"
            });

            // 4. Internal Needle Roller Bearing (Deep Occluded Precision Target)
            GameObject bearingRoot = new GameObject("Part_Raptor_NeedleBearing");
            bearingRoot.transform.SetParent(assemblyRoot, false);
            bearingRoot.transform.localPosition = new Vector3(0, 0, 0.05f);

            GameObject outerRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            outerRing.name = "Bearing_OuterRing";
            outerRing.transform.SetParent(bearingRoot.transform, false);
            outerRing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            outerRing.transform.localScale = new Vector3(0.24f, 0.065f, 0.24f);

            var bearingMat = new Material(litShader);
            bearingMat.color = new Color(1.0f, 0.78f, 0.22f, 1.0f);
            if (bearingMat.HasProperty("_Metallic")) bearingMat.SetFloat("_Metallic", 0.95f);
            if (bearingMat.HasProperty("_Smoothness")) bearingMat.SetFloat("_Smoothness", 0.90f);
            if (bearingMat.HasProperty("_EmissionColor"))
            {
                bearingMat.EnableKeyword("_EMISSION");
                bearingMat.SetColor("_EmissionColor", new Color(0.8f, 0.5f, 0.1f) * 0.4f);
            }
            outerRing.GetComponent<Renderer>().material = bearingMat;

            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f;
                GameObject pin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pin.name = $"RollerPin_{i}";
                pin.transform.SetParent(bearingRoot.transform, false);
                pin.transform.localScale = new Vector3(0.025f, 0.055f, 0.025f);
                pin.transform.localPosition = Quaternion.Euler(0, 0, angle) * new Vector3(0, 0.088f, 0);
                pin.transform.localRotation = Quaternion.Euler(90f, 0, 0);
                pin.GetComponent<Renderer>().material = bearingMat;
            }

            var bearingCol = bearingRoot.AddComponent<BoxCollider>();
            bearingCol.size = new Vector3(0.26f, 0.26f, 0.12f);

            parts.Add(new AssemblyPart
            {
                partId = "RAPTOR_BEARING_04",
                displayName = "Internal Needle Roller Bearing [PCDD Target]",
                componentType = ComponentType.InternalBearing,
                gameObject = bearingRoot,
                renderer = outerRing.GetComponent<Renderer>(),
                defaultMaterial = bearingMat,
                xRayMaterial = bearingMat,
                nominalLocalPos = new Vector3(0, 0, 0.05f),
                nominalLocalRot = Quaternion.identity,
                explodedOffset = new Vector3(0, 0.22f, 0.35f),
                toleranceSpec = "±0.005 mm (Sub-Millimeter Critical)"
            });

            // 5. Annular Combustion Nozzle & Flared Bell Manifold
            GameObject nozzleRoot = new GameObject("Part_Raptor_CombustionNozzle");
            nozzleRoot.transform.SetParent(assemblyRoot, false);
            nozzleRoot.transform.localPosition = new Vector3(0, 0, 0.34f);

            GameObject manifold = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            manifold.name = "Raptor_Nozzle_Manifold";
            manifold.transform.SetParent(nozzleRoot.transform, false);
            manifold.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            manifold.transform.localScale = new Vector3(0.48f, 0.08f, 0.48f);

            var nozBaseMat = new Material(litShader);
            nozBaseMat.color = new Color(0.25f, 0.30f, 0.36f, 1.0f);
            if (nozBaseMat.HasProperty("_Metallic")) nozBaseMat.SetFloat("_Metallic", 0.85f);
            manifold.GetComponent<Renderer>().material = nozBaseMat;

            // Flared Bell Nozzle Extension
            GameObject bellNozzle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bellNozzle.name = "Raptor_BellNozzle";
            bellNozzle.transform.SetParent(nozzleRoot.transform, false);
            bellNozzle.transform.localPosition = new Vector3(0, 0, 0.14f);
            bellNozzle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            bellNozzle.transform.localScale = new Vector3(0.58f, 0.18f, 0.58f);
            bellNozzle.GetComponent<Renderer>().material = nozBaseMat;

            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f;
                GameObject nozzle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                nozzle.name = $"Injector_{i}";
                nozzle.transform.SetParent(nozzleRoot.transform, false);
                nozzle.transform.localScale = new Vector3(0.045f, 0.09f, 0.045f);
                nozzle.transform.localPosition = Quaternion.Euler(0, 0, angle) * new Vector3(0, 0.18f, 0);
                nozzle.transform.localRotation = Quaternion.Euler(90f, 0, angle);

                var injectorMat = new Material(litShader);
                injectorMat.color = new Color(0.0f, 0.85f, 0.95f);
                if (injectorMat.HasProperty("_EmissionColor"))
                {
                    injectorMat.EnableKeyword("_EMISSION");
                    injectorMat.SetColor("_EmissionColor", new Color(0.0f, 0.7f, 1.0f) * 1.2f);
                }
                nozzle.GetComponent<Renderer>().material = injectorMat;
            }

            var nozCol = nozzleRoot.AddComponent<BoxCollider>();
            nozCol.size = new Vector3(0.60f, 0.60f, 0.38f);

            parts.Add(new AssemblyPart
            {
                partId = "RAPTOR_NOZZLE_05",
                displayName = "Combustion Fuel Nozzle & Bell Manifold",
                componentType = ComponentType.CombustionNozzle,
                gameObject = nozzleRoot,
                renderer = manifold.GetComponent<Renderer>(),
                defaultMaterial = nozBaseMat,
                xRayMaterial = nozBaseMat,
                nominalLocalPos = new Vector3(0, 0, 0.34f),
                nominalLocalRot = Quaternion.identity,
                explodedOffset = new Vector3(0, 0, 0.65f),
                toleranceSpec = "±0.05 mm"
            });

            // 6. Telemetry Wiring Harness Bus 04 (⚠️ Chafing Hazard & Cable Clash)
            GameObject harnessRoot = new GameObject("Part_Raptor_WiringHarness_Bus04");
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

            var wireCol = harnessRoot.AddComponent<BoxCollider>();
            wireCol.size = new Vector3(0.55f, 0.55f, 0.35f);

            var defaultWireMat = new Material(litShader);
            defaultWireMat.color = new Color(1.0f, 0.35f, 0.05f);

            parts.Add(new AssemblyPart
            {
                partId = "WIRE_HARNESS_BUS04",
                displayName = "Ignition Wire Harness Bus 04 (⚠️ Chafing Hazard)",
                componentType = ComponentType.WiringHarness,
                gameObject = harnessRoot,
                renderer = harnessRoot.transform.GetChild(3).GetComponent<Renderer>(),
                defaultMaterial = defaultWireMat,
                xRayMaterial = defaultWireMat,
                nominalLocalPos = new Vector3(0, 0, 0.12f),
                nominalLocalRot = Quaternion.identity,
                explodedOffset = new Vector3(0.25f, 0, 0.12f),
                toleranceSpec = "±0.02 mm"
            });

            // 7. Holographic Safe Re-Route Ghost Proxy (+3.2mm Clearance)
            GameObject ghostRoot = new GameObject("Part_Raptor_GhostWiringProxy");
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
        #endregion

        #region Preset 1: NASA NEXT Ion Thruster
        private void BuildIonThruster(Shader litShader)
        {
            // 1. Titanium Anode Discharge Cylinder with Outer Cutaway Struts
            GameObject casing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            casing.name = "Part_Ion_AnodeChamber";
            casing.transform.SetParent(assemblyRoot, false);
            casing.transform.localPosition = Vector3.zero;
            casing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            casing.transform.localScale = new Vector3(0.56f, 0.42f, 0.56f);

            var casingMat = new Material(litShader);
            casingMat.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);
            if (casingMat.HasProperty("_Metallic")) casingMat.SetFloat("_Metallic", 0.92f);
            if (casingMat.HasProperty("_Smoothness")) casingMat.SetFloat("_Smoothness", 0.75f);
            casing.GetComponent<Renderer>().material = casingMat;

            // 4 Radial Anode Support Struts
            for (int i = 0; i < 4; i++)
            {
                float ang = i * 90f;
                GameObject strut = GameObject.CreatePrimitive(PrimitiveType.Cube);
                strut.name = $"AnodeStrut_{i}";
                strut.transform.SetParent(casing.transform, false);
                strut.transform.localScale = new Vector3(0.06f, 0.95f, 0.06f);
                strut.transform.localPosition = Quaternion.Euler(0f, ang, 0f) * new Vector3(0.55f, 0f, 0f);
                strut.GetComponent<Renderer>().material = casingMat;
            }

            var xRayMat = SpatialTheme.CreateGlassMaterial(
                new Color(0.05f, 0.18f, 0.32f, 0.25f),
                new Color(0.0f, 0.95f, 1.0f, 0.85f)
            );

            parts.Add(new AssemblyPart
            {
                partId = "ION_ANODE_01",
                displayName = "Perforated Titanium Anode Housing",
                componentType = ComponentType.OuterCasing,
                gameObject = casing,
                renderer = casing.GetComponent<Renderer>(),
                defaultMaterial = casingMat,
                xRayMaterial = xRayMat,
                nominalLocalPos = Vector3.zero,
                nominalLocalRot = Quaternion.Euler(90f, 0f, 0f),
                explodedOffset = new Vector3(0, 0.40f, 0),
                toleranceSpec = "±0.25 mm"
            });

            // 2. Central Xenon Cathode & 3 Solenoid Magnetic Containment Rings
            GameObject solenoidRoot = new GameObject("Part_Ion_SolenoidCoils");
            solenoidRoot.transform.SetParent(assemblyRoot, false);
            solenoidRoot.transform.localPosition = new Vector3(0, 0, -0.15f);

            var copperMat = new Material(litShader);
            copperMat.color = new Color(0.85f, 0.50f, 0.20f, 1.0f); // Polished Copper Solenoid
            if (copperMat.HasProperty("_Metallic")) copperMat.SetFloat("_Metallic", 0.95f);
            if (copperMat.HasProperty("_Smoothness")) copperMat.SetFloat("_Smoothness", 0.80f);

            for (int r = 0; r < 3; r++)
            {
                float zOffset = (r - 1) * 0.14f;
                GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = $"SolenoidRing_{r}";
                ring.transform.SetParent(solenoidRoot.transform, false);
                ring.transform.localPosition = new Vector3(0, 0, zOffset);
                ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                ring.transform.localScale = new Vector3(0.48f, 0.04f, 0.48f);
                ring.GetComponent<Renderer>().material = copperMat;
            }

            var solCol = solenoidRoot.AddComponent<BoxCollider>();
            solCol.size = new Vector3(0.52f, 0.52f, 0.35f);

            parts.Add(new AssemblyPart
            {
                partId = "ION_SOLENOID_02",
                displayName = "Triple Solenoid Magnetic Field Rings",
                componentType = ComponentType.IntakeFan,
                gameObject = solenoidRoot,
                renderer = solenoidRoot.transform.GetChild(0).GetComponent<Renderer>(),
                defaultMaterial = copperMat,
                xRayMaterial = copperMat,
                nominalLocalPos = new Vector3(0, 0, -0.15f),
                nominalLocalRot = Quaternion.identity,
                explodedOffset = new Vector3(0, 0, -0.48f),
                toleranceSpec = "±0.05 mm"
            });

            // 3. Central Xenon Plasma Discharge Channel Tube
            GameObject coreTube = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coreTube.name = "Part_Ion_DischargeCore";
            coreTube.transform.SetParent(assemblyRoot, false);
            coreTube.transform.localPosition = Vector3.zero;
            coreTube.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            coreTube.transform.localScale = new Vector3(0.14f, 0.46f, 0.14f);

            var coreMat = new Material(litShader);
            coreMat.color = new Color(0.28f, 0.34f, 0.42f, 1.0f);
            if (coreMat.HasProperty("_Metallic")) coreMat.SetFloat("_Metallic", 0.85f);
            coreTube.GetComponent<Renderer>().material = coreMat;

            parts.Add(new AssemblyPart
            {
                partId = "ION_CORE_03",
                displayName = "Ceramic Xenon Discharge Core",
                componentType = ComponentType.DriveShaft,
                gameObject = coreTube,
                renderer = coreTube.GetComponent<Renderer>(),
                defaultMaterial = coreMat,
                xRayMaterial = coreMat,
                nominalLocalPos = Vector3.zero,
                nominalLocalRot = Quaternion.Euler(90f, 0f, 0f),
                explodedOffset = new Vector3(0, -0.30f, 0),
                toleranceSpec = "±0.01 mm"
            });

            // 4. Downstream Molybdenum Ion Extraction Accelerator Grid
            GameObject gridRoot = new GameObject("Part_Ion_AcceleratorGrid");
            gridRoot.transform.SetParent(assemblyRoot, false);
            gridRoot.transform.localPosition = new Vector3(0, 0, 0.22f);

            GameObject screenDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            screenDisc.name = "ScreenGridDisc";
            screenDisc.transform.SetParent(gridRoot.transform, false);
            screenDisc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            screenDisc.transform.localScale = new Vector3(0.44f, 0.025f, 0.44f);

            var gridMat = new Material(litShader);
            gridMat.color = new Color(1.0f, 0.80f, 0.25f, 1.0f); // Gold-coated Molybdenum
            if (gridMat.HasProperty("_Metallic")) gridMat.SetFloat("_Metallic", 0.98f);
            if (gridMat.HasProperty("_Smoothness")) gridMat.SetFloat("_Smoothness", 0.90f);
            if (gridMat.HasProperty("_EmissionColor"))
            {
                gridMat.EnableKeyword("_EMISSION");
                gridMat.SetColor("_EmissionColor", new Color(0.9f, 0.6f, 0.1f) * 0.5f);
            }
            screenDisc.GetComponent<Renderer>().material = gridMat;

            var gridCol = gridRoot.AddComponent<BoxCollider>();
            gridCol.size = new Vector3(0.46f, 0.46f, 0.08f);

            parts.Add(new AssemblyPart
            {
                partId = "ION_GRID_04",
                displayName = "Molybdenum Accelerator Grid [PCDD Target]",
                componentType = ComponentType.InternalBearing,
                gameObject = gridRoot,
                renderer = screenDisc.GetComponent<Renderer>(),
                defaultMaterial = gridMat,
                xRayMaterial = gridMat,
                nominalLocalPos = new Vector3(0, 0, 0.22f),
                nominalLocalRot = Quaternion.identity,
                explodedOffset = new Vector3(0, 0.20f, 0.35f),
                toleranceSpec = "±0.002 mm (Micro-Aperture Optics)"
            });

            // 5. Electric Blue Glowing Ion Plasma Exhaust Beam
            GameObject plasmaRoot = new GameObject("Part_Ion_PlasmaBeam");
            plasmaRoot.transform.SetParent(assemblyRoot, false);
            plasmaRoot.transform.localPosition = new Vector3(0, 0, 0.46f);

            GameObject beamCylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beamCylinder.name = "PlasmaBeamCylinder";
            beamCylinder.transform.SetParent(plasmaRoot.transform, false);
            beamCylinder.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            beamCylinder.transform.localScale = new Vector3(0.32f, 0.30f, 0.32f);

            var plasmaMat = new Material(litShader);
            plasmaMat.color = new Color(0.0f, 0.85f, 1.0f, 0.95f);
            if (plasmaMat.HasProperty("_EmissionColor"))
            {
                plasmaMat.EnableKeyword("_EMISSION");
                plasmaMat.SetColor("_EmissionColor", new Color(0.0f, 0.9f, 1.0f) * 2.2f);
            }
            beamCylinder.GetComponent<Renderer>().material = plasmaMat;

            var plasmaCol = plasmaRoot.AddComponent<BoxCollider>();
            plasmaCol.size = new Vector3(0.36f, 0.36f, 0.60f);

            parts.Add(new AssemblyPart
            {
                partId = "ION_PLASMA_05",
                displayName = "Electric Blue Xenon Plasma Exhaust Jet",
                componentType = ComponentType.CombustionNozzle,
                gameObject = plasmaRoot,
                renderer = beamCylinder.GetComponent<Renderer>(),
                defaultMaterial = plasmaMat,
                xRayMaterial = plasmaMat,
                nominalLocalPos = new Vector3(0, 0, 0.46f),
                nominalLocalRot = Quaternion.identity,
                explodedOffset = new Vector3(0, 0, 0.60f),
                toleranceSpec = "Isp: 4,100 s"
            });

            // 6. High-Voltage Cathode Feeder Harness (⚠️ Chafing Hazard on Solenoid 2)
            GameObject wireRoot = new GameObject("Part_Ion_HV_WiringHarness");
            wireRoot.transform.SetParent(assemblyRoot, false);
            wireRoot.transform.localPosition = new Vector3(0, 0, 0.08f);

            for (int i = 0; i < 3; i++)
            {
                float ang = i * 40f + 20f;
                GameObject conduit = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                conduit.name = $"HV_Conduit_{i + 1}";
                conduit.transform.SetParent(wireRoot.transform, false);
                conduit.transform.localScale = new Vector3(0.020f, 0.26f, 0.020f);
                conduit.transform.localPosition = Quaternion.Euler(0, 0, ang) * new Vector3(0.23f, 0, 0);
                conduit.transform.localRotation = Quaternion.Euler(90f, 0, ang);

                var wMat = new Material(litShader);
                if (i == 2)
                {
                    wMat.color = new Color(1.0f, 0.35f, 0.05f);
                    if (wMat.HasProperty("_EmissionColor"))
                    {
                        wMat.EnableKeyword("_EMISSION");
                        wMat.SetColor("_EmissionColor", new Color(1.0f, 0.2f, 0.0f) * 1.6f);
                    }
                }
                else
                {
                    wMat.color = new Color(0.12f, 0.16f, 0.22f);
                }
                conduit.GetComponent<Renderer>().material = wMat;
            }

            var wireCol = wireRoot.AddComponent<BoxCollider>();
            wireCol.size = new Vector3(0.50f, 0.50f, 0.30f);

            var defMat = new Material(litShader);
            defMat.color = new Color(1.0f, 0.35f, 0.05f);

            parts.Add(new AssemblyPart
            {
                partId = "WIRE_ION_HV_06",
                displayName = "High-Voltage Cathode Line (⚠️ Solenoid Chafing)",
                componentType = ComponentType.WiringHarness,
                gameObject = wireRoot,
                renderer = wireRoot.transform.GetChild(2).GetComponent<Renderer>(),
                defaultMaterial = defMat,
                xRayMaterial = defMat,
                nominalLocalPos = new Vector3(0, 0, 0.08f),
                nominalLocalRot = Quaternion.identity,
                explodedOffset = new Vector3(0.22f, 0, 0.08f),
                toleranceSpec = "3,200 V Isolation"
            });

            // 7. Holographic Magnetically Shielded Re-Route Ghost (+3.2mm Clearance)
            GameObject ghostRoot = new GameObject("Part_Ion_GhostWiringProxy");
            ghostRoot.transform.SetParent(assemblyRoot, false);
            ghostRoot.transform.localPosition = new Vector3(0.04f, 0.03f, 0.08f);

            GameObject ghostTube = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ghostTube.name = "GhostIonWire";
            ghostTube.transform.SetParent(ghostRoot.transform, false);
            ghostTube.transform.localScale = new Vector3(0.024f, 0.27f, 0.024f);
            ghostTube.transform.localPosition = Quaternion.Euler(0, 0, 140f) * new Vector3(0.27f, 0, 0);
            ghostTube.transform.localRotation = Quaternion.Euler(90f, 0, 140f);

            var ghostMat = SpatialTheme.CreateGlassMaterial(
                new Color(0.0f, 0.95f, 0.65f, 0.45f),
                new Color(0.0f, 1.0f, 0.6f, 0.9f)
            );
            ghostTube.GetComponent<Renderer>().material = ghostMat;
            ghostRoot.SetActive(false);

            parts.Add(new AssemblyPart
            {
                partId = "GHOST_ION_PROXY",
                displayName = "Magnetically Isolated Ghost Path (+3.2mm)",
                componentType = ComponentType.GhostWiringProxy,
                gameObject = ghostRoot,
                renderer = ghostTube.GetComponent<Renderer>(),
                defaultMaterial = ghostMat,
                xRayMaterial = ghostMat,
                nominalLocalPos = new Vector3(0.04f, 0.03f, 0.08f),
                nominalLocalRot = Quaternion.identity,
                explodedOffset = new Vector3(0.22f, 0, 0.08f),
                toleranceSpec = "Clearance: +3.20 mm"
            });
        }
        #endregion

        #region Preset 2: Cryogenic LOX Turbopump
        private void BuildCryoTurbopump(Shader litShader)
        {
            // 1. Asymmetric Volute Spiral Pump Casing with High-Pressure Tangential Outlet
            GameObject voluteRoot = new GameObject("Part_Cryo_VoluteCasing");
            voluteRoot.transform.SetParent(assemblyRoot, false);
            voluteRoot.transform.localPosition = Vector3.zero;

            // Main Volute Scroll Cylinder
            GameObject scroll = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            scroll.name = "VoluteScroll";
            scroll.transform.SetParent(voluteRoot.transform, false);
            scroll.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            scroll.transform.localScale = new Vector3(0.64f, 0.32f, 0.64f);

            var voluteMat = new Material(litShader);
            voluteMat.color = new Color(0.20f, 0.26f, 0.34f, 0.95f);
            if (voluteMat.HasProperty("_Metallic")) voluteMat.SetFloat("_Metallic", 0.90f);
            if (voluteMat.HasProperty("_Smoothness")) voluteMat.SetFloat("_Smoothness", 0.70f);
            scroll.GetComponent<Renderer>().material = voluteMat;

            // Tangential High-Pressure Discharge Duct
            GameObject dischargeDuct = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dischargeDuct.name = "VoluteDischargeDuct";
            dischargeDuct.transform.SetParent(voluteRoot.transform, false);
            dischargeDuct.transform.localPosition = new Vector3(0.36f, 0.24f, 0f);
            dischargeDuct.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            dischargeDuct.transform.localScale = new Vector3(0.18f, 0.26f, 0.18f);
            dischargeDuct.GetComponent<Renderer>().material = voluteMat;

            var xRayMat = SpatialTheme.CreateGlassMaterial(
                new Color(0.12f, 0.22f, 0.35f, 0.25f),
                new Color(0.2f, 0.95f, 0.6f, 0.85f)
            );

            parts.Add(new AssemblyPart
            {
                partId = "CRYO_VOLUTE_01",
                displayName = "Asymmetric Cryo Volute Scroll Casing",
                componentType = ComponentType.OuterCasing,
                gameObject = voluteRoot,
                renderer = scroll.GetComponent<Renderer>(),
                defaultMaterial = voluteMat,
                xRayMaterial = xRayMat,
                nominalLocalPos = Vector3.zero,
                nominalLocalRot = Quaternion.identity,
                explodedOffset = new Vector3(0, 0.45f, 0),
                toleranceSpec = "±0.15 mm (300 Bar Hydrostatic)"
            });

            // 2. Cryogenic Helical Inducer Screw (4 Spiral Archimedes Blades)
            GameObject inducerRoot = new GameObject("Part_Cryo_InducerScrew");
            inducerRoot.transform.SetParent(assemblyRoot, false);
            inducerRoot.transform.localPosition = new Vector3(0, 0, -0.28f);

            GameObject inducerHub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            inducerHub.name = "InducerHub";
            inducerHub.transform.SetParent(inducerRoot.transform, false);
            inducerHub.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            inducerHub.transform.localScale = new Vector3(0.16f, 0.28f, 0.16f);

            var chromeMat = new Material(litShader);
            chromeMat.color = new Color(0.92f, 0.94f, 0.98f, 1.0f);
            if (chromeMat.HasProperty("_Metallic")) chromeMat.SetFloat("_Metallic", 0.98f);
            if (chromeMat.HasProperty("_Smoothness")) chromeMat.SetFloat("_Smoothness", 0.92f);
            inducerHub.GetComponent<Renderer>().material = chromeMat;

            // 4 Spiral Inducer Blades along the hub
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f;
                GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blade.name = $"InducerBlade_{i}";
                blade.transform.SetParent(inducerRoot.transform, false);
                blade.transform.localScale = new Vector3(0.038f, 0.22f, 0.16f);
                blade.transform.localPosition = Quaternion.Euler(0, 0, angle) * new Vector3(0, 0.14f, 0);
                blade.transform.localRotation = Quaternion.Euler(42f, 15f, angle);
                blade.GetComponent<Renderer>().material = chromeMat;
            }

            var inducerCol = inducerRoot.AddComponent<BoxCollider>();
            inducerCol.size = new Vector3(0.55f, 0.55f, 0.32f);

            parts.Add(new AssemblyPart
            {
                partId = "CRYO_INDUCER_02",
                displayName = "Helical Cryogenic Inducer Screw (Anti-Cavitation)",
                componentType = ComponentType.IntakeFan,
                gameObject = inducerRoot,
                renderer = inducerHub.GetComponent<Renderer>(),
                defaultMaterial = chromeMat,
                xRayMaterial = chromeMat,
                nominalLocalPos = new Vector3(0, 0, -0.28f),
                nominalLocalRot = Quaternion.identity,
                explodedOffset = new Vector3(0, 0, -0.52f),
                toleranceSpec = "±0.05 mm (Cryogenic Balance)"
            });

            // 3. Central Inconel Drive Shaft
            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = "Part_Cryo_DriveShaft";
            shaft.transform.SetParent(assemblyRoot, false);
            shaft.transform.localPosition = Vector3.zero;
            shaft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shaft.transform.localScale = new Vector3(0.11f, 0.42f, 0.11f);

            var shaftMat = new Material(litShader);
            shaftMat.color = new Color(0.32f, 0.38f, 0.44f, 1f);
            if (shaftMat.HasProperty("_Metallic")) shaftMat.SetFloat("_Metallic", 0.92f);
            shaft.GetComponent<Renderer>().material = shaftMat;

            parts.Add(new AssemblyPart
            {
                partId = "CRYO_SHAFT_03",
                displayName = "Inconel-718 Core Drive Shaft",
                componentType = ComponentType.DriveShaft,
                gameObject = shaft,
                renderer = shaft.GetComponent<Renderer>(),
                defaultMaterial = shaftMat,
                xRayMaterial = shaftMat,
                nominalLocalPos = Vector3.zero,
                nominalLocalRot = Quaternion.Euler(90f, 0f, 0f),
                explodedOffset = new Vector3(0, -0.32f, 0),
                toleranceSpec = "±0.01 mm"
            });

            // 4. Cryogenic Needle Roller Bearing (Sub-Millimeter Friction Hazard)
            GameObject bearingRoot = new GameObject("Part_Cryo_InternalBearing");
            bearingRoot.transform.SetParent(assemblyRoot, false);
            bearingRoot.transform.localPosition = new Vector3(0, 0, 0.04f);

            GameObject outerRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            outerRing.name = "CryoBearing_Ring";
            outerRing.transform.SetParent(bearingRoot.transform, false);
            outerRing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            outerRing.transform.localScale = new Vector3(0.22f, 0.06f, 0.22f);

            var bearingMat = new Material(litShader);
            bearingMat.color = new Color(1.0f, 0.75f, 0.20f, 1.0f);
            if (bearingMat.HasProperty("_Metallic")) bearingMat.SetFloat("_Metallic", 0.95f);
            if (bearingMat.HasProperty("_EmissionColor"))
            {
                bearingMat.EnableKeyword("_EMISSION");
                bearingMat.SetColor("_EmissionColor", new Color(0.9f, 0.5f, 0.1f) * 0.4f);
            }
            outerRing.GetComponent<Renderer>().material = bearingMat;

            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f;
                GameObject roller = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                roller.name = $"Roller_{i}";
                roller.transform.SetParent(bearingRoot.transform, false);
                roller.transform.localScale = new Vector3(0.022f, 0.05f, 0.022f);
                roller.transform.localPosition = Quaternion.Euler(0, 0, angle) * new Vector3(0, 0.080f, 0);
                roller.transform.localRotation = Quaternion.Euler(90f, 0, 0);
                roller.GetComponent<Renderer>().material = bearingMat;
            }

            var bCol = bearingRoot.AddComponent<BoxCollider>();
            bCol.size = new Vector3(0.24f, 0.24f, 0.10f);

            parts.Add(new AssemblyPart
            {
                partId = "CRYO_BEARING_04",
                displayName = "Cryogenic Roller Bearing [PCDD Target]",
                componentType = ComponentType.InternalBearing,
                gameObject = bearingRoot,
                renderer = outerRing.GetComponent<Renderer>(),
                defaultMaterial = bearingMat,
                xRayMaterial = bearingMat,
                nominalLocalPos = new Vector3(0, 0, 0.04f),
                nominalLocalRot = Quaternion.identity,
                explodedOffset = new Vector3(0, 0.18f, 0.30f),
                toleranceSpec = "±0.005 mm (Thermal Shock Qualified)"
            });

            // 5. High-Pressure LOX Flange Manifold with 8 Bolt Sockets
            GameObject flangeRoot = new GameObject("Part_Cryo_LOX_Flange");
            flangeRoot.transform.SetParent(assemblyRoot, false);
            flangeRoot.transform.localPosition = new Vector3(0, 0, 0.30f);

            GameObject flangeDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            flangeDisc.name = "LOX_FlangeDisc";
            flangeDisc.transform.SetParent(flangeRoot.transform, false);
            flangeDisc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            flangeDisc.transform.localScale = new Vector3(0.50f, 0.07f, 0.50f);

            var flangeMat = new Material(litShader);
            flangeMat.color = new Color(0.24f, 0.30f, 0.38f, 1.0f);
            if (flangeMat.HasProperty("_Metallic")) flangeMat.SetFloat("_Metallic", 0.90f);
            flangeDisc.GetComponent<Renderer>().material = flangeMat;

            // 8 Perimeter Flange Bolts
            for (int i = 0; i < 8; i++)
            {
                float ang = i * 45f;
                GameObject bolt = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bolt.name = $"Bolt_{i}";
                bolt.transform.SetParent(flangeRoot.transform, false);
                bolt.transform.localScale = new Vector3(0.035f, 0.09f, 0.035f);
                bolt.transform.localPosition = Quaternion.Euler(0, 0, ang) * new Vector3(0, 0.21f, 0);
                bolt.transform.localRotation = Quaternion.Euler(90f, 0, 0);

                var boltMat = new Material(litShader);
                boltMat.color = new Color(0.0f, 0.85f, 0.95f);
                if (boltMat.HasProperty("_EmissionColor"))
                {
                    boltMat.EnableKeyword("_EMISSION");
                    boltMat.SetColor("_EmissionColor", new Color(0.0f, 0.7f, 1.0f) * 1.0f);
                }
                bolt.GetComponent<Renderer>().material = boltMat;
            }

            var fCol = flangeRoot.AddComponent<BoxCollider>();
            fCol.size = new Vector3(0.54f, 0.54f, 0.16f);

            parts.Add(new AssemblyPart
            {
                partId = "CRYO_FLANGE_05",
                displayName = "High-Pressure LOX Flange Manifold",
                componentType = ComponentType.CombustionNozzle,
                gameObject = flangeRoot,
                renderer = flangeDisc.GetComponent<Renderer>(),
                defaultMaterial = flangeMat,
                xRayMaterial = flangeMat,
                nominalLocalPos = new Vector3(0, 0, 0.30f),
                nominalLocalRot = Quaternion.identity,
                explodedOffset = new Vector3(0, 0, 0.58f),
                toleranceSpec = "±0.02 mm (ASME B16.5 Class 2500)"
            });

            // 6. Cryogenic Sensor & Telemetry Line (⚠️ Thermal Shock Chafing Clash)
            GameObject wireRoot = new GameObject("Part_Cryo_TelemetryLine");
            wireRoot.transform.SetParent(assemblyRoot, false);
            wireRoot.transform.localPosition = new Vector3(0.02f, -0.01f, 0.10f);

            for (int i = 0; i < 3; i++)
            {
                float ang = i * 50f + 10f;
                GameObject wire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wire.name = $"CryoProbe_{i + 1}";
                wire.transform.SetParent(wireRoot.transform, false);
                wire.transform.localScale = new Vector3(0.022f, 0.25f, 0.022f);
                wire.transform.localPosition = Quaternion.Euler(0, 0, ang) * new Vector3(0.24f, 0, 0);
                wire.transform.localRotation = Quaternion.Euler(90f, 0, ang);

                var wMat = new Material(litShader);
                if (i == 2)
                {
                    wMat.color = new Color(1.0f, 0.35f, 0.05f);
                    if (wMat.HasProperty("_EmissionColor"))
                    {
                        wMat.EnableKeyword("_EMISSION");
                        wMat.SetColor("_EmissionColor", new Color(1.0f, 0.2f, 0.0f) * 1.6f);
                    }
                }
                else
                {
                    wMat.color = new Color(0.12f, 0.16f, 0.22f);
                }
                wire.GetComponent<Renderer>().material = wMat;
            }

            var wCol = wireRoot.AddComponent<BoxCollider>();
            wCol.size = new Vector3(0.52f, 0.52f, 0.30f);

            var defMat = new Material(litShader);
            defMat.color = new Color(1.0f, 0.35f, 0.05f);

            parts.Add(new AssemblyPart
            {
                partId = "WIRE_CRYO_SENSOR_06",
                displayName = "Cryo Telemetry Line (⚠️ Volute Chafing Hazard)",
                componentType = ComponentType.WiringHarness,
                gameObject = wireRoot,
                renderer = wireRoot.transform.GetChild(2).GetComponent<Renderer>(),
                defaultMaterial = defMat,
                xRayMaterial = defMat,
                nominalLocalPos = new Vector3(0.02f, -0.01f, 0.10f),
                nominalLocalRot = Quaternion.identity,
                explodedOffset = new Vector3(0.24f, 0, 0.10f),
                toleranceSpec = "-196°C Rated"
            });

            // 7. Holographic Standoff Re-Route Ghost (+3.2mm Clearance)
            GameObject ghostRoot = new GameObject("Part_Cryo_GhostWiringProxy");
            ghostRoot.transform.SetParent(assemblyRoot, false);
            ghostRoot.transform.localPosition = new Vector3(0.04f, 0.03f, 0.10f);

            GameObject ghostTube = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ghostTube.name = "GhostCryoSensor";
            ghostTube.transform.SetParent(ghostRoot.transform, false);
            ghostTube.transform.localScale = new Vector3(0.026f, 0.27f, 0.026f);
            ghostTube.transform.localPosition = Quaternion.Euler(0, 0, 145f) * new Vector3(0.28f, 0, 0);
            ghostTube.transform.localRotation = Quaternion.Euler(90f, 0, 145f);

            var ghostMat = SpatialTheme.CreateGlassMaterial(
                new Color(0.0f, 0.95f, 0.65f, 0.45f),
                new Color(0.0f, 1.0f, 0.6f, 0.9f)
            );
            ghostTube.GetComponent<Renderer>().material = ghostMat;
            ghostRoot.SetActive(false);

            parts.Add(new AssemblyPart
            {
                partId = "GHOST_CRYO_PROXY",
                displayName = "Thermal Standoff Ghost Route (+3.2mm)",
                componentType = ComponentType.GhostWiringProxy,
                gameObject = ghostRoot,
                renderer = ghostTube.GetComponent<Renderer>(),
                defaultMaterial = ghostMat,
                xRayMaterial = ghostMat,
                nominalLocalPos = new Vector3(0.04f, 0.03f, 0.10f),
                nominalLocalRot = Quaternion.identity,
                explodedOffset = new Vector3(0.24f, 0, 0.10f),
                toleranceSpec = "Clearance: +3.20 mm"
            });
        }
        #endregion

        #region VR World-Space Holographic Mission Console
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

            // 3D Spatial Buttons (Interactable with VR laser ray, finger poke, and PC clicks)
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
            string modelName = currentModelPreset switch
            {
                ModelPreset.SpaceXRaptor3 => "SpaceX Raptor 3",
                ModelPreset.NASAIonThruster => "NASA NEXT Ion Thruster",
                ModelPreset.CryoTurbopump => "Cryo LOX Turbopump",
                _ => currentModelPreset.ToString()
            };

            if (isProposalCommitted)
            {
                consoleStatusTextMesh.text = $"{modelName} • <color=#00ff88>LOCKED: +3.20mm (SAFE)</color>";
            }
            else if (isProposalActive)
            {
                consoleStatusTextMesh.text = $"{modelName} • <color=#00f0ff>GHOST ACTIVE (+3.20mm)</color>";
            }
            else
            {
                consoleStatusTextMesh.text = $"{modelName} • <color=#ffaa00>CLASH: Line 04 (0.00mm)</color>";
            }
        }
        #endregion

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
                else if (selected) p.isSelected = false;
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
