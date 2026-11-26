using System;
using UnityEngine;
using UnityEngine.Rendering;
using OmniOS.Core;

namespace OmniOS.Environment
{
    /// <summary>
    /// MetaverseEnvironmentManager - 5 Completely Distinct, Independent 3D VR Environments + Unity Default.
    /// Each environment is built as an isolated 3D architectural world (never just recolored walls):
    /// 
    /// 0. XPISpatialObservatory (New Default):
    ///    TXST XPI Lab Spatial Observatory with glowing inspection pedestal, 4 cantilever arches,
    ///    spinning overhead Orbital Gyro-Ring, and XPI telemetry totems.
    /// 1. RocketLaunchpad (SpaceX High-Bay & Launch Complex):
    ///    Concrete launch apron, recessed flame trench, 8.5m steel lattice gantry tower with umbilical arm,
    ///    blinking aviation beacons, twin cryogenic LOX/CH4 spheres, and open night starfield.
    /// 2. OrbitalCupola (ISS Deep Orbital Space Station):
    ///    Metallic Zero-G observation module with grab handrails, 7-pane cupola window frame,
    ///    giant rotating Planet Earth with glowing atmosphere outside, and gold-foil solar wings.
    /// 3. QuantumCleanroom (NASA / Apple Minimalist Sterile Lab):
    ///    Reflective white porcelain floor, frosted glass partition walls with aluminum mullions,
    ///    overhead HEPA light grid arrays, and 2 articulated robotic assembly arms.
    /// 4. MarsColonyBay (Mars Geodesic Dome & Rover Bay):
    ///    Red Martian regolith terrain & perimeter ridges, transparent geodesic dome ribs,
    ///    pressurized habitat airlock, Mars rover service ramp, and warm amber dust atmosphere.
    /// 5. DefaultTemplate:
    ///    Restores Unity's original Template Environment for baseline comparison.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-350)]
    public class MetaverseEnvironmentManager : MonoBehaviour
    {
        private static MetaverseEnvironmentManager instance;
        public static MetaverseEnvironmentManager Instance => instance;

        public enum EnvironmentTheme
        {
            XPISpatialObservatory = 0,
            RocketLaunchpad = 1,
            CyberHangar = 1,
            CyberPenthouse = 1,
            OrbitalCupola = 2,
            OrbitalDeck = 2,
            SunsetHorizon = 2,
            QuantumCleanroom = 3,
            CosmicNebula = 3,
            MarsColonyBay = 4,
            DefaultTemplate = 5
        }

        [Header("Theme Configuration")]
        [SerializeField] private EnvironmentTheme currentTheme = EnvironmentTheme.XPISpatialObservatory;
        public EnvironmentTheme CurrentTheme => currentTheme;

        private GameObject customWorldsRoot;

        // 5 Completely Independent 3D Environment Roots
        private GameObject envRootObservatory;
        private GameObject envRootLaunchpad;
        private GameObject envRootCupola;
        private GameObject envRootCleanroom;
        private GameObject envRootMarsBay;

        // Animated Environment Elements
        private Transform orbitalGyroRing;
        private Transform earthSphereTransform;
        private Renderer[] launchpadBeacons = new Renderer[2];
        private Material beaconOnMat;
        private Material beaconOffMat;
        private Transform[] cleanroomRobotArms = new Transform[2];

        // Global Lighting & Particles
        private Light keyDirectionalLight;
        private Light fillPointLight;
        private ParticleSystem atmosphericParticles;

        // Reference to Unity's default template room
        private GameObject unityTemplateEnvironment;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (instance == null)
            {
                var existing = UnityEngine.Object.FindAnyObjectByType<MetaverseEnvironmentManager>();
                if (existing != null)
                {
                    instance = existing;
                }
                else
                {
                    GameObject go = new GameObject("[METAVERSE_ENVIRONMENT]");
                    instance = go.AddComponent<MetaverseEnvironmentManager>();
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

            LocateUnityTemplateEnvironment();
            BuildAllDistinctEnvironments();
            ApplyTheme(currentTheme);
        }

        private void LocateUnityTemplateEnvironment()
        {
            if (unityTemplateEnvironment != null) return;
            unityTemplateEnvironment = GameObject.Find("Environment") ?? GameObject.Find("Template Environment");
        }

        private void BuildAllDistinctEnvironments()
        {
            if (customWorldsRoot != null) return;

            customWorldsRoot = new GameObject("Metaverse_DistinctWorlds_Root");
            customWorldsRoot.transform.SetParent(null, false);
            customWorldsRoot.transform.position = Vector3.zero;
            customWorldsRoot.transform.rotation = Quaternion.identity;

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");

            // Shared Global Lights & Particles
            GameObject keyObj = new GameObject("Env_KeyLight");
            keyObj.transform.SetParent(customWorldsRoot.transform, false);
            keyObj.transform.position = new Vector3(2f, 7f, -2f);
            keyObj.transform.rotation = Quaternion.Euler(52f, -25f, 0f);
            keyDirectionalLight = keyObj.AddComponent<Light>();
            keyDirectionalLight.type = LightType.Directional;
            keyDirectionalLight.intensity = 1.4f;

            GameObject fillObj = new GameObject("Env_FillLight");
            fillObj.transform.SetParent(customWorldsRoot.transform, false);
            fillObj.transform.position = new Vector3(0f, 3.0f, 1.85f);
            fillPointLight = fillObj.AddComponent<Light>();
            fillPointLight.type = LightType.Point;
            fillPointLight.range = 24f;
            fillPointLight.intensity = 1.3f;

            GameObject partObj = new GameObject("Env_AtmosphericMotes");
            partObj.transform.SetParent(customWorldsRoot.transform, false);
            partObj.transform.position = new Vector3(0f, 2.0f, 1.85f);
            atmosphericParticles = partObj.AddComponent<ParticleSystem>();
            var main = atmosphericParticles.main;
            main.loop = true;
            main.startLifetime = 8.0f;
            main.startSpeed = 0.04f;
            main.startSize = 0.02f;
            main.maxParticles = 90;
            var emission = atmosphericParticles.emission;
            emission.rateOverTime = 14f;
            var shape = atmosphericParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(14f, 3.5f, 14f);

            // Build all 5 completely distinct 3D Worlds
            BuildObservatoryWorld(litShader, unlitShader);
            BuildRocketLaunchpadWorld(litShader, unlitShader);
            BuildOrbitalCupolaWorld(litShader, unlitShader);
            BuildQuantumCleanroomWorld(litShader, unlitShader);
            BuildMarsColonyWorld(litShader, unlitShader);
        }

        private GameObject CreateSolidFloor(Transform parent, string name, float width, float depth, Color color, float smoothness, float metallic, Shader litShader, Texture2D tex = null)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = name;
            floor.transform.SetParent(parent, false);
            floor.transform.position = new Vector3(0f, -0.015f, 0f);
            floor.transform.localScale = new Vector3(width, 0.03f, depth);

            var mat = new Material(litShader);
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (tex != null)
            {
                mat.mainTexture = tex;
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            }
            floor.GetComponent<Renderer>().material = mat;
            return floor;
        }

        private void DisableShadowAndCollider(GameObject go, bool removeCollider = true)
        {
            if (removeCollider)
            {
                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }
            var rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        #region World 0: TXST XPI Spatial Observatory
        private void BuildObservatoryWorld(Shader litShader, Shader unlitShader)
        {
            envRootObservatory = new GameObject("World_0_XPI_Observatory");
            envRootObservatory.transform.SetParent(customWorldsRoot.transform, false);

            CreateSolidFloor(envRootObservatory.transform, "Observatory_DeckFloor", 24f, 24f, new Color(0.03f, 0.06f, 0.11f), 0.85f, 0.45f, litShader, GenerateGridTexture(new Color(0.03f, 0.06f, 0.11f), new Color(0.0f, 0.9f, 0.85f, 0.6f)));

            var tealMat = new Material(unlitShader) { color = new Color(0.0f, 0.95f, 0.85f) };
            var darkMetal = new Material(litShader) { color = new Color(0.08f, 0.12f, 0.19f) };
            if (darkMetal.HasProperty("_Metallic")) darkMetal.SetFloat("_Metallic", 0.88f);

            // Central Inspection Pedestal at (0, 0, 1.85)
            GameObject ped = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ped.name = "Observatory_Pedestal";
            ped.transform.SetParent(envRootObservatory.transform, false);
            ped.transform.position = new Vector3(0f, 0.015f, 1.85f);
            ped.transform.localScale = new Vector3(1.65f, 0.015f, 1.65f);
            ped.GetComponent<Renderer>().material = darkMetal;
            Destroy(ped.GetComponent<Collider>());

            GameObject pedRim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedRim.name = "Observatory_PedestalRim";
            pedRim.transform.SetParent(envRootObservatory.transform, false);
            pedRim.transform.position = new Vector3(0f, 0.008f, 1.85f);
            pedRim.transform.localScale = new Vector3(1.80f, 0.008f, 1.80f);
            pedRim.GetComponent<Renderer>().material = tealMat;
            DisableShadowAndCollider(pedRim);

            // 4 Cantilever Arch Pillars
            for (int i = 0; i < 4; i++)
            {
                float angle = 45f + i * 90f;
                float rad = angle * Mathf.Deg2Rad;
                GameObject arch = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arch.name = $"Observatory_Arch_{i}";
                arch.transform.SetParent(envRootObservatory.transform, false);
                arch.transform.position = new Vector3(Mathf.Sin(rad) * 5.2f, 2.2f, 1.85f + Mathf.Cos(rad) * 5.2f);
                arch.transform.rotation = Quaternion.Euler(22f, angle + 180f, 0f);
                arch.transform.localScale = new Vector3(0.32f, 4.8f, 0.45f);
                arch.GetComponent<Renderer>().material = darkMetal;

                GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                strip.transform.SetParent(arch.transform, false);
                strip.transform.localPosition = new Vector3(0f, 0f, -0.52f);
                strip.transform.localScale = new Vector3(0.16f, 0.96f, 0.06f);
                strip.GetComponent<Renderer>().material = tealMat;
                DisableShadowAndCollider(strip);
            }

            // Spinning Overhead Orbital Gyroscope Ring
            GameObject gyroObj = new GameObject("Observatory_GyroRing");
            gyroObj.transform.SetParent(envRootObservatory.transform, false);
            gyroObj.transform.position = new Vector3(0f, 4.5f, 1.85f);
            orbitalGyroRing = gyroObj.transform;

            for (int s = 0; s < 12; s++)
            {
                float a = s * 30f;
                GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seg.transform.SetParent(orbitalGyroRing, false);
                seg.transform.localPosition = Quaternion.Euler(0f, a, 0f) * new Vector3(2.4f, 0f, 0f);
                seg.transform.localRotation = Quaternion.Euler(0f, a, 0f);
                seg.transform.localScale = new Vector3(0.12f, 0.10f, 0.95f);
                seg.GetComponent<Renderer>().material = (s % 2 == 0) ? tealMat : darkMetal;
                DisableShadowAndCollider(seg);
            }

            // TXST XPI Lab Totems
            for (int t = -1; t <= 1; t += 2)
            {
                GameObject totem = GameObject.CreatePrimitive(PrimitiveType.Cube);
                totem.name = $"XPI_Totem_{(t < 0 ? "L" : "R")}";
                totem.transform.SetParent(envRootObservatory.transform, false);
                totem.transform.position = new Vector3(t * 2.8f, 1.0f, 3.0f);
                totem.transform.rotation = Quaternion.Euler(0f, t * -25f, 0f);
                totem.transform.localScale = new Vector3(0.35f, 2.0f, 0.18f);
                totem.GetComponent<Renderer>().material = darkMetal;

                GameObject labelObj = new GameObject("TotemLabel");
                labelObj.transform.SetParent(totem.transform, false);
                labelObj.transform.localPosition = new Vector3(0f, 0.35f, -0.55f);
                var tm = labelObj.AddComponent<TextMesh>();
                tm.text = (t < 0) ? "TXST XPI LAB\nDIGITAL TWIN" : "IEEE VR 2027\n3DUI HUB";
                tm.fontSize = 22;
                tm.characterSize = 0.03f;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.color = new Color(0.0f, 0.95f, 0.85f);
            }
        }
        #endregion

        #region World 1: SpaceX Rocket Launchpad & High-Bay Gantry
        private void BuildRocketLaunchpadWorld(Shader litShader, Shader unlitShader)
        {
            envRootLaunchpad = new GameObject("World_1_Rocket_Launchpad");
            envRootLaunchpad.transform.SetParent(customWorldsRoot.transform, false);

            // Concrete Blast Apron Floor
            CreateSolidFloor(envRootLaunchpad.transform, "Launchpad_ConcreteApron", 28f, 28f, new Color(0.18f, 0.19f, 0.21f), 0.25f, 0.1f, litShader, GenerateGridTexture(new Color(0.16f, 0.17f, 0.19f), new Color(0.95f, 0.65f, 0.05f, 0.5f)));

            var steelMat = new Material(litShader) { color = new Color(0.24f, 0.27f, 0.31f) };
            if (steelMat.HasProperty("_Metallic")) steelMat.SetFloat("_Metallic", 0.85f);
            var hazardYellow = new Material(litShader) { color = new Color(0.95f, 0.68f, 0.05f) };
            var cryoWhite = new Material(litShader) { color = new Color(0.88f, 0.91f, 0.95f) };
            if (cryoWhite.HasProperty("_Smoothness")) cryoWhite.SetFloat("_Smoothness", 0.75f);

            // Blast Flame Trench behind the engine
            GameObject trench = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trench.name = "Launchpad_FlameTrench";
            trench.transform.SetParent(envRootLaunchpad.transform, false);
            trench.transform.position = new Vector3(0f, 0.04f, 4.2f);
            trench.transform.localScale = new Vector3(3.6f, 0.08f, 4.5f);
            trench.GetComponent<Renderer>().material = new Material(litShader) { color = new Color(0.05f, 0.05f, 0.06f) };
            DisableShadowAndCollider(trench);

            // Steel Lattice Gantry Launch Tower (Left-Rear)
            GameObject towerRoot = new GameObject("Launchpad_GantryTower");
            towerRoot.transform.SetParent(envRootLaunchpad.transform, false);
            towerRoot.transform.position = new Vector3(-3.2f, 0f, 3.8f);

            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    leg.transform.SetParent(towerRoot.transform, false);
                    leg.transform.localPosition = new Vector3(x * 0.65f, 4.25f, z * 0.65f);
                    leg.transform.localScale = new Vector3(0.18f, 8.5f, 0.18f);
                    leg.GetComponent<Renderer>().material = steelMat;
                }
            }

            for (int lvl = 1; lvl <= 5; lvl++)
            {
                GameObject brace = GameObject.CreatePrimitive(PrimitiveType.Cube);
                brace.transform.SetParent(towerRoot.transform, false);
                brace.transform.localPosition = new Vector3(0f, lvl * 1.55f, 0f);
                brace.transform.localScale = new Vector3(1.5f, 0.12f, 1.5f);
                brace.GetComponent<Renderer>().material = (lvl == 5) ? hazardYellow : steelMat;
                DisableShadowAndCollider(brace);
            }

            // Umbilical Swing Arm extending toward the CAD workspace
            GameObject swingArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            swingArm.name = "Umbilical_SwingArm";
            swingArm.transform.SetParent(towerRoot.transform, false);
            swingArm.transform.localPosition = new Vector3(1.5f, 3.1f, -0.9f);
            swingArm.transform.rotation = Quaternion.Euler(0f, 28f, 0f);
            swingArm.transform.localScale = new Vector3(2.8f, 0.22f, 0.30f);
            swingArm.GetComponent<Renderer>().material = hazardYellow;

            // Aviation Warning Beacons on Tower Top
            beaconOnMat = new Material(unlitShader) { color = new Color(1.0f, 0.15f, 0.05f) };
            beaconOffMat = new Material(unlitShader) { color = new Color(0.25f, 0.05f, 0.05f) };
            for (int b = 0; b < 2; b++)
            {
                GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                beacon.name = $"AviationBeacon_{b}";
                beacon.transform.SetParent(towerRoot.transform, false);
                beacon.transform.localPosition = new Vector3((b == 0 ? -0.65f : 0.65f), 8.65f, 0f);
                beacon.transform.localScale = Vector3.one * 0.24f;
                launchpadBeacons[b] = beacon.GetComponent<Renderer>();
                launchpadBeacons[b].material = beaconOnMat;
                DisableShadowAndCollider(beacon);
            }

            // Twin Cryogenic LOX/CH4 Storage Spheres (Right-Rear)
            for (int s = 0; s < 2; s++)
            {
                GameObject tank = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                tank.name = $"CryoSphereTank_{s}";
                tank.transform.SetParent(envRootLaunchpad.transform, false);
                tank.transform.position = new Vector3(4.5f + s * 2.2f, 1.35f, 5.2f);
                tank.transform.localScale = Vector3.one * 1.9f;
                tank.GetComponent<Renderer>().material = cryoWhite;
            }

            // Safety Perimeter Bollards
            for (int p = -3; p <= 3; p++)
            {
                if (p == 0) continue;
                GameObject bollard = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bollard.transform.SetParent(envRootLaunchpad.transform, false);
                bollard.transform.position = new Vector3(p * 1.4f, 0.35f, 4.6f);
                bollard.transform.localScale = new Vector3(0.12f, 0.35f, 0.12f);
                bollard.GetComponent<Renderer>().material = hazardYellow;
            }
        }
        #endregion

        #region World 2: ISS Deep Orbital Space Station Cupola
        private void BuildOrbitalCupolaWorld(Shader litShader, Shader unlitShader)
        {
            envRootCupola = new GameObject("World_2_Orbital_Cupola");
            envRootCupola.transform.SetParent(customWorldsRoot.transform, false);

            // Compact ISS Metallic Deck
            CreateSolidFloor(envRootCupola.transform, "Cupola_ModuleDeck", 14f, 14f, new Color(0.10f, 0.11f, 0.14f), 0.70f, 0.80f, litShader, GenerateGridTexture(new Color(0.09f, 0.10f, 0.13f), new Color(0.95f, 0.75f, 0.15f, 0.35f)));

            var hullMat = new Material(litShader) { color = new Color(0.22f, 0.25f, 0.30f) };
            if (hullMat.HasProperty("_Metallic")) hullMat.SetFloat("_Metallic", 0.85f);
            var handrailMat = new Material(litShader) { color = new Color(0.95f, 0.75f, 0.10f) };
            var goldFoilMat = new Material(litShader) { color = new Color(0.95f, 0.68f, 0.12f) };
            if (goldFoilMat.HasProperty("_Metallic")) goldFoilMat.SetFloat("_Metallic", 0.95f);

            // 7-Pane Curved Observation Cupola Frame (Open toward front +Z so Earth is clearly visible)
            for (int i = 0; i < 6; i++)
            {
                float angle = -75f + i * 30f;
                float rad = angle * Mathf.Deg2Rad;
                GameObject strut = GameObject.CreatePrimitive(PrimitiveType.Cube);
                strut.name = $"Cupola_WindowStrut_{i}";
                strut.transform.SetParent(envRootCupola.transform, false);
                strut.transform.position = new Vector3(Mathf.Sin(rad) * 4.8f, 2.2f, Mathf.Cos(rad) * 4.8f);
                strut.transform.rotation = Quaternion.Euler(-22f, angle, 0f);
                strut.transform.localScale = new Vector3(0.18f, 4.4f, 0.22f);
                strut.GetComponent<Renderer>().material = hullMat;

                // Zero-G Grab Handrail
                GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rail.transform.SetParent(strut.transform, false);
                rail.transform.localPosition = new Vector3(0f, -0.15f, -0.75f);
                rail.transform.localScale = new Vector3(0.22f, 0.32f, 0.22f);
                rail.GetComponent<Renderer>().material = handrailMat;
                DisableShadowAndCollider(rail);
            }

            // Giant Rotating Planet Earth outside the Cupola Window
            GameObject earthObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            earthObj.name = "Orbital_PlanetEarth";
            earthObj.transform.SetParent(envRootCupola.transform, false);
            earthObj.transform.position = new Vector3(0f, -8.5f, 22.0f);
            earthObj.transform.localScale = Vector3.one * 26.0f;
            earthSphereTransform = earthObj.transform;

            var earthMat = new Material(litShader);
            earthMat.mainTexture = GeneratePlanetEarthTexture();
            if (earthMat.HasProperty("_BaseMap")) earthMat.SetTexture("_BaseMap", earthMat.mainTexture);
            if (earthMat.HasProperty("_Smoothness")) earthMat.SetFloat("_Smoothness", 0.55f);
            earthObj.GetComponent<Renderer>().material = earthMat;
            DisableShadowAndCollider(earthObj);

            // Glowing Blue Atmosphere Rim Halo around Earth
            GameObject atmoObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            atmoObj.name = "Earth_AtmosphereGlow";
            atmoObj.transform.SetParent(earthSphereTransform, false);
            atmoObj.transform.localScale = Vector3.one * 1.035f;
            atmoObj.GetComponent<Renderer>().material = SpatialTheme.CreateGlassMaterial(new Color(0.1f, 0.55f, 1.0f, 0.22f), new Color(0.2f, 0.75f, 1.0f, 0.65f));
            DisableShadowAndCollider(atmoObj);

            // External Gold-Foil Solar Array Wings flanking the window
            for (int w = -1; w <= 1; w += 2)
            {
                GameObject wing = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wing.name = $"SolarArrayWing_{(w < 0 ? "L" : "R")}";
                wing.transform.SetParent(envRootCupola.transform, false);
                wing.transform.position = new Vector3(w * 7.8f, 2.5f, 6.5f);
                wing.transform.rotation = Quaternion.Euler(15f, w * -15f, 0f);
                wing.transform.localScale = new Vector3(5.5f, 0.06f, 1.8f);
                wing.GetComponent<Renderer>().material = goldFoilMat;
                DisableShadowAndCollider(wing);
            }
        }
        #endregion

        #region World 3: NASA / Apple Minimalist Quantum Cleanroom Lab
        private void BuildQuantumCleanroomWorld(Shader litShader, Shader unlitShader)
        {
            envRootCleanroom = new GameObject("World_3_Quantum_Cleanroom");
            envRootCleanroom.transform.SetParent(customWorldsRoot.transform, false);

            // Ultra-Clean Reflective White Porcelain Floor
            CreateSolidFloor(envRootCleanroom.transform, "Cleanroom_PorcelainFloor", 22f, 22f, new Color(0.95f, 0.97f, 0.99f), 0.95f, 0.15f, litShader, GenerateGridTexture(new Color(0.93f, 0.96f, 0.99f), new Color(0.0f, 0.75f, 0.95f, 0.25f)));

            var aluminumMat = new Material(litShader) { color = new Color(0.80f, 0.84f, 0.88f) };
            if (aluminumMat.HasProperty("_Metallic")) aluminumMat.SetFloat("_Metallic", 0.85f);
            var glassWallMat = SpatialTheme.CreateGlassMaterial(new Color(0.85f, 0.94f, 1.0f, 0.22f), new Color(0.0f, 0.85f, 1.0f, 0.45f));
            var hepaGlowMat = new Material(unlitShader) { color = new Color(0.96f, 0.99f, 1.0f) };
            var robotOrange = new Material(litShader) { color = new Color(0.95f, 0.45f, 0.12f) };

            // Frosted Glass Partition Walls with Aluminum Mullions
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject glassWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                glassWall.name = $"Cleanroom_GlassPartition_{(side < 0 ? "L" : "R")}";
                glassWall.transform.SetParent(envRootCleanroom.transform, false);
                glassWall.transform.position = new Vector3(side * 4.8f, 2.0f, 1.5f);
                glassWall.transform.localScale = new Vector3(0.08f, 4.0f, 9.0f);
                glassWall.GetComponent<Renderer>().material = glassWallMat;
                DisableShadowAndCollider(glassWall, false);

                for (int m = -1; m <= 1; m++)
                {
                    GameObject mullion = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    mullion.transform.SetParent(glassWall.transform, false);
                    mullion.transform.localPosition = new Vector3(0f, 0f, m * 0.45f);
                    mullion.transform.localScale = new Vector3(1.5f, 1.02f, 0.02f);
                    mullion.GetComponent<Renderer>().material = aluminumMat;
                }
            }

            // Overhead HEPA-Filter Luminous Ceiling Light Arrays
            for (int row = 0; row < 3; row++)
            {
                GameObject hepa = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hepa.name = $"Cleanroom_HEPA_Panel_{row}";
                hepa.transform.SetParent(envRootCleanroom.transform, false);
                hepa.transform.position = new Vector3(0f, 4.4f, -0.5f + row * 2.2f);
                hepa.transform.localScale = new Vector3(5.6f, 0.06f, 1.1f);
                hepa.GetComponent<Renderer>().material = hepaGlowMat;
                DisableShadowAndCollider(hepa);
            }

            // 2 Articulated Industrial Robotic Assembly Arms flanking the Digital Twin
            for (int r = 0; r < 2; r++)
            {
                float side = (r == 0) ? -2.4f : 2.4f;
                GameObject basePed = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                basePed.name = $"Cleanroom_RobotBase_{r}";
                basePed.transform.SetParent(envRootCleanroom.transform, false);
                basePed.transform.position = new Vector3(side, 0.45f, 2.2f);
                basePed.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
                basePed.GetComponent<Renderer>().material = aluminumMat;

                GameObject armJoint = new GameObject($"RobotArmJoint_{r}");
                armJoint.transform.SetParent(basePed.transform, false);
                armJoint.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                cleanroomRobotArms[r] = armJoint.transform;

                GameObject lowerArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lowerArm.transform.SetParent(armJoint.transform, false);
                lowerArm.transform.localPosition = new Vector3(0f, 0.6f, 0f);
                lowerArm.transform.localScale = new Vector3(0.28f, 1.4f, 0.28f);
                lowerArm.GetComponent<Renderer>().material = robotOrange;

                GameObject upperArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
                upperArm.transform.SetParent(armJoint.transform, false);
                upperArm.transform.localPosition = new Vector3(-side * 0.16f, 1.35f, -0.15f);
                upperArm.transform.localRotation = Quaternion.Euler(25f, 0f, side * 18f);
                upperArm.transform.localScale = new Vector3(0.22f, 1.1f, 0.22f);
                upperArm.GetComponent<Renderer>().material = aluminumMat;
            }
        }
        #endregion

        #region World 4: Mars Colony Geodesic Dome & Rover Bay
        private void BuildMarsColonyWorld(Shader litShader, Shader unlitShader)
        {
            envRootMarsBay = new GameObject("World_4_Mars_ColonyDome");
            envRootMarsBay.transform.SetParent(customWorldsRoot.transform, false);

            // Dusty Martian Regolith Floor
            CreateSolidFloor(envRootMarsBay.transform, "Mars_RegolithFloor", 32f, 32f, new Color(0.48f, 0.20f, 0.11f), 0.15f, 0.05f, litShader, GenerateGridTexture(new Color(0.45f, 0.18f, 0.10f), new Color(0.95f, 0.55f, 0.20f, 0.30f)));

            var rustRockMat = new Material(litShader) { color = new Color(0.55f, 0.22f, 0.12f) };
            var domeStrutMat = new Material(litShader) { color = new Color(0.28f, 0.26f, 0.28f) };
            var roverGoldMat = new Material(litShader) { color = new Color(0.92f, 0.62f, 0.18f) };
            var amberGlowMat = new Material(unlitShader) { color = new Color(1.0f, 0.58f, 0.12f) };

            // Geodesic Dome Meridian Ribs curving over the Colony Bay
            for (int rib = 0; rib < 6; rib++)
            {
                float angle = rib * 30f;
                GameObject ribArch = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ribArch.name = $"Mars_GeodesicRib_{rib}";
                ribArch.transform.SetParent(envRootMarsBay.transform, false);
                ribArch.transform.position = new Vector3(0f, 5.2f, 1.85f);
                ribArch.transform.rotation = Quaternion.Euler(0f, angle, 0f);
                ribArch.transform.localScale = new Vector3(13.5f, 0.18f, 0.22f);
                ribArch.GetComponent<Renderer>().material = domeStrutMat;
                DisableShadowAndCollider(ribArch);
            }

            // Perimeter Martian Rock Ridges & Craters outside the Dome
            for (int r = 0; r < 14; r++)
            {
                float ang = r * (360f / 14f) * Mathf.Deg2Rad;
                float dist = 8.5f + (r % 3) * 1.8f;
                GameObject boulder = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                boulder.name = $"Mars_Ridge_{r}";
                boulder.transform.SetParent(envRootMarsBay.transform, false);
                boulder.transform.position = new Vector3(Mathf.Sin(ang) * dist, 0.4f, 1.85f + Mathf.Cos(ang) * dist);
                boulder.transform.localScale = new Vector3(2.4f + (r % 2), 1.4f + (r % 3) * 0.5f, 2.1f);
                boulder.GetComponent<Renderer>().material = rustRockMat;
            }

            // Pressurized Habitat Airlock Hatch (Rear)
            GameObject airlock = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            airlock.name = "Mars_HabitatAirlock";
            airlock.transform.SetParent(envRootMarsBay.transform, false);
            airlock.transform.position = new Vector3(0f, 1.4f, 6.2f);
            airlock.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            airlock.transform.localScale = new Vector3(2.8f, 0.5f, 2.8f);
            airlock.GetComponent<Renderer>().material = domeStrutMat;

            GameObject airlockRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            airlockRing.transform.SetParent(airlock.transform, false);
            airlockRing.transform.localPosition = new Vector3(0f, -0.52f, 0f);
            airlockRing.transform.localScale = new Vector3(0.75f, 0.08f, 0.75f);
            airlockRing.GetComponent<Renderer>().material = amberGlowMat;
            DisableShadowAndCollider(airlockRing);

            // Mars Rover Service Ramp (Side)
            GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = "Mars_RoverServiceRamp";
            ramp.transform.SetParent(envRootMarsBay.transform, false);
            ramp.transform.position = new Vector3(-3.4f, 0.15f, 2.2f);
            ramp.transform.rotation = Quaternion.Euler(0f, 20f, 0f);
            ramp.transform.localScale = new Vector3(1.6f, 0.30f, 3.2f);
            ramp.GetComponent<Renderer>().material = roverGoldMat;
        }
        #endregion

        #region Procedural Textures
        private Texture2D GenerateGridTexture(Color bg, Color grid)
        {
            const int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isBorder = (x % 32 == 0) || (y % 32 == 0);
                    bool isSubLine = (x % 16 == 0) || (y % 16 == 0);
                    if (isBorder) pixels[y * size + x] = Color.Lerp(bg, grid, grid.a);
                    else if (isSubLine) pixels[y * size + x] = Color.Lerp(bg, grid, grid.a * 0.35f);
                    else pixels[y * size + x] = bg;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private Texture2D GeneratePlanetEarthTexture()
        {
            const int w = 256;
            const int h = 128;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color deepOcean = new Color(0.04f, 0.18f, 0.48f);
            Color shallowOcean = new Color(0.08f, 0.40f, 0.72f);
            Color landGreen = new Color(0.18f, 0.45f, 0.24f);
            Color cloudWhite = new Color(0.95f, 0.98f, 1.0f);

            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = x * 0.045f;
                    float ny = y * 0.045f;
                    float n = Mathf.PerlinNoise(nx, ny);
                    float c = Mathf.PerlinNoise(nx * 1.8f + 10f, ny * 1.8f + 5f);

                    Color col = (n > 0.56f) ? landGreen : Color.Lerp(deepOcean, shallowOcean, n);
                    if (c > 0.62f) col = Color.Lerp(col, cloudWhite, (c - 0.62f) * 2.5f);
                    pixels[y * w + x] = col;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
        #endregion

        private void Update()
        {
            // 'T' Key: Cycle through all 6 distinct environments!
            if (Input.GetKeyDown(KeyCode.T) && !OmniOS.Apps.MediaHub.OmniAppMediaHub.IsTypingActive)
            {
                NextTheme();
            }

            if (customWorldsRoot != null && customWorldsRoot.transform.position != Vector3.zero)
            {
                customWorldsRoot.transform.position = Vector3.zero;
            }

            // World-specific ambient animations
            if (currentTheme == EnvironmentTheme.XPISpatialObservatory && orbitalGyroRing != null)
            {
                orbitalGyroRing.Rotate(Vector3.up, 12f * Time.deltaTime, Space.Self);
            }
            else if (currentTheme == EnvironmentTheme.RocketLaunchpad && beaconOnMat != null && beaconOffMat != null)
            {
                bool blinkOn = (Mathf.Sin(Time.time * 4.5f) > 0f);
                for (int i = 0; i < launchpadBeacons.Length; i++)
                {
                    if (launchpadBeacons[i] != null)
                        launchpadBeacons[i].material = blinkOn ? beaconOnMat : beaconOffMat;
                }
            }
            else if (currentTheme == EnvironmentTheme.OrbitalCupola && earthSphereTransform != null)
            {
                earthSphereTransform.Rotate(Vector3.up, 2.2f * Time.deltaTime, Space.Self);
            }
            else if (currentTheme == EnvironmentTheme.QuantumCleanroom)
            {
                for (int r = 0; r < cleanroomRobotArms.Length; r++)
                {
                    if (cleanroomRobotArms[r] != null)
                    {
                        float sway = Mathf.Sin(Time.time * 0.9f + r * 2.0f) * 14f;
                        cleanroomRobotArms[r].localRotation = Quaternion.Euler(0f, sway, 0f);
                    }
                }
            }
        }

        public void NextTheme()
        {
            int next = ((int)currentTheme + 1) % 6;
            ApplyTheme((EnvironmentTheme)next);
        }

        public void ToggleCleanRoom()
        {
            if (currentTheme == EnvironmentTheme.QuantumCleanroom)
            {
                ApplyTheme(EnvironmentTheme.XPISpatialObservatory);
            }
            else
            {
                ApplyTheme(EnvironmentTheme.QuantumCleanroom);
            }
        }

        public void ApplyTheme(EnvironmentTheme theme)
        {
            currentTheme = theme;

            if (unityTemplateEnvironment == null) LocateUnityTemplateEnvironment();

            var envRoot = GameObject.Find("Environment");
            var tempEnv = GameObject.Find("Template Environment");
            var gridObj = GameObject.Find("Grid");
            var arrowsObj = GameObject.Find("Arrows");
            var torusObj = GameObject.Find("Torus Cut");

            bool isDefault = (theme == EnvironmentTheme.DefaultTemplate);

            if (envRoot != null) envRoot.SetActive(isDefault);
            if (tempEnv != null) tempEnv.SetActive(isDefault);
            if (gridObj != null) gridObj.SetActive(isDefault);
            if (arrowsObj != null) arrowsObj.SetActive(isDefault);
            if (torusObj != null) torusObj.SetActive(isDefault);
            if (customWorldsRoot != null) customWorldsRoot.SetActive(!isDefault);

            if (isDefault)
            {
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.50f, 1.0f);
                OmniOSManager.Instance?.ShowNotificationToast("🏛️ Environment: UNITY DEFAULT VR ROOM [T]");
                SpatialAudioEngine.Instance?.PlayThemeChime();
                return;
            }

            // Activate ONLY the selected 3D world
            if (envRootObservatory != null) envRootObservatory.SetActive(theme == EnvironmentTheme.XPISpatialObservatory);
            if (envRootLaunchpad != null) envRootLaunchpad.SetActive(theme == EnvironmentTheme.RocketLaunchpad);
            if (envRootCupola != null) envRootCupola.SetActive(theme == EnvironmentTheme.OrbitalCupola);
            if (envRootCleanroom != null) envRootCleanroom.SetActive(theme == EnvironmentTheme.QuantumCleanroom);
            if (envRootMarsBay != null) envRootMarsBay.SetActive(theme == EnvironmentTheme.MarsColonyBay);

            Color ambientCol, keyCol, fillCol, particleCol;
            string envTitle;

            switch (theme)
            {
                case EnvironmentTheme.RocketLaunchpad:
                    envTitle = "🚀 SPACEX ROCKET LAUNCHPAD & GANTRY";
                    ambientCol = new Color(0.10f, 0.13f, 0.19f);
                    keyCol = new Color(1.0f, 0.85f, 0.65f);
                    fillCol = new Color(0.25f, 0.65f, 1.0f);
                    particleCol = new Color(1.0f, 0.7f, 0.2f, 0.45f);
                    break;

                case EnvironmentTheme.OrbitalCupola:
                    envTitle = "🛰️ ISS DEEP ORBITAL SPACE CUPOLA";
                    ambientCol = new Color(0.10f, 0.16f, 0.28f);
                    keyCol = new Color(0.85f, 0.95f, 1.0f);
                    fillCol = new Color(0.15f, 0.65f, 1.0f);
                    particleCol = new Color(0.2f, 0.85f, 1.0f, 0.40f);
                    break;

                case EnvironmentTheme.QuantumCleanroom:
                    envTitle = "🔬 NASA / APPLE QUANTUM CLEANROOM LAB";
                    ambientCol = new Color(0.65f, 0.70f, 0.78f);
                    keyCol = new Color(1.0f, 1.0f, 1.0f);
                    fillCol = new Color(0.0f, 0.85f, 1.0f);
                    particleCol = new Color(0.0f, 0.85f, 1.0f, 0.30f);
                    break;

                case EnvironmentTheme.MarsColonyBay:
                    envTitle = "🪐 MARS COLONY GEODESIC DOME & ROVER BAY";
                    ambientCol = new Color(0.32f, 0.16f, 0.10f);
                    keyCol = new Color(1.0f, 0.76f, 0.45f);
                    fillCol = new Color(1.0f, 0.55f, 0.20f);
                    particleCol = new Color(1.0f, 0.62f, 0.25f, 0.50f);
                    break;

                default: // XPISpatialObservatory
                    envTitle = "🔭 TXST XPI SPATIAL OBSERVATORY (Default)";
                    ambientCol = new Color(0.14f, 0.20f, 0.30f);
                    keyCol = new Color(0.75f, 0.98f, 1.0f);
                    fillCol = new Color(0.0f, 0.95f, 0.85f);
                    particleCol = new Color(0.0f, 0.95f, 0.85f, 0.45f);
                    break;
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambientCol;
            if (keyDirectionalLight != null) keyDirectionalLight.color = keyCol;
            if (fillPointLight != null) fillPointLight.color = fillCol;
            if (atmosphericParticles != null)
            {
                var main = atmosphericParticles.main;
                main.startColor = particleCol;
            }

            OmniOSManager.Instance?.ShowNotificationToast($"🎨 Environment: {envTitle} [T]");
            SpatialAudioEngine.Instance?.PlayThemeChime();
        }
    }
}
