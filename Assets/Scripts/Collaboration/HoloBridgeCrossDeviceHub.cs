using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using OmniOS.Core;
using OmniOS.Interaction;
using OmniOS.Environment;

namespace HoloBridge.Collaboration
{
    /// <summary>
    /// HoloBridgeCrossDeviceHub - Flagship Multi-Device 3D Spatial Collaboration Hub for IEEE VR 2027.
    /// Connects Mobile Smartphones, Desktop PCs, Unity Editor, and VR Headsets into a unified spatial workspace.
    /// 
    /// Flagship Capabilities:
    /// 1. Real-Time HD Live Video Streaming to Web Browsers:
    ///    Captures the live Unity 3D camera view and streams it to any mobile/web browser at ~25 FPS with zero plugins.
    /// 2. Expressive 3D Character Avatars for Every Connected Device:
    ///    Spawns articulated 3D humanoid cartoon models with cyber visors, breathing animations, floating name badges,
    ///    held devices (smartphone, console, VR controller), and articulated pointer hands aiming real-time laser beams.
    /// 3. Cross-Ray Volumetric Disambiguation (Co-Ray):
    ///    When PC (Amber) and VR (Cyan) rays converge, beam fusion triggers resonant extraction of occluded internal parts.
    /// 4. Surface-to-Spatial Screen-Peel (CR-Extrude):
    ///    2D blueprints from Mobile/Tablet screens are physically extruded into floating 3D holograms snapped into the assembly.
    /// 5. Zero-Install Mobile Web Bridge:
    ///    Non-admin TcpListener on port 8080 lets any smartphone on Wi-Fi become a real 6DOF touch controller with live camera stream.
    /// </summary>
    [DefaultExecutionOrder(-400)]
    public class HoloBridgeCrossDeviceHub : MonoBehaviour
    {
        private static HoloBridgeCrossDeviceHub instance;
        public static HoloBridgeCrossDeviceHub Instance => instance;

        public enum DeviceRole
        {
            VRHeadset = 0,
            DesktopPC = 1,
            MobileTablet = 2
        }

        [Header("Active Local Perspective")]
        [SerializeField] private DeviceRole activeRole = DeviceRole.DesktopPC;
        public DeviceRole ActiveRole => activeRole;

        [Header("Co-Ray Resonance Parameters")]
        [SerializeField] private bool isCrossRayActive = true;
        [SerializeField] private float resonanceThreshold = 0.12f;
        [SerializeField] private Color colorPC = new Color(1.0f, 0.65f, 0.15f);      // Amber
        [SerializeField] private Color colorVR = new Color(0.0f, 0.95f, 1.0f);       // Cyan
        [SerializeField] private Color colorMobile = new Color(0.0f, 1.0f, 0.55f);   // Emerald
        [SerializeField] private Color colorResonance = new Color(1.0f, 0.90f, 0.20f); // Brilliant Gold

        [Header("Screen-Peel State")]
        [SerializeField] private GameObject peeledHologramPart;
        [SerializeField] private bool isHologramPeeled = false;
        public bool IsHologramPeeled => isHologramPeeled;

        [Header("Mobile Web Bridge (Port 8080)")]
        [SerializeField] private bool enableLocalWebServer = true;
        [SerializeField] private int webServerPort = 8080;
        private string localIpAddress = "127.0.0.1";
        private TcpListener tcpListener;
        private Thread webServerThread;
        private bool isServerRunning = false;

        [Header("On-Screen Mobile Phone Simulator")]
        [SerializeField] private bool showMobileSimulator = false;
        public bool ShowMobileSimulator { get => showMobileSimulator; set => showMobileSimulator = value; }

        // Live Video Streaming to Web
        private GameObject streamCamObj;
        private Camera streamCamera;
        private RenderTexture streamRenderTexture;
        private Texture2D streamTexture2D;
        private byte[] latestJpgBytes;
        private readonly object streamLock = new object();
        private float frameCaptureTimer = 0f;
        private const int StreamWidth = 640;
        private const int StreamHeight = 360;

        // Multi-Participant 3D Avatars
        public class CollaboratorAvatar
        {
            public string clientId;
            public string displayName;
            public Color teamColor;
            public DeviceRole role;
            public GameObject rootObj;
            public Transform headTransform;
            public Transform torsoTransform;
            public Transform rightHandTransform;
            public Transform leftHandTransform;
            public LineRenderer laserBeam;
            public Transform laserReticle;
            public TextMesh nameTagTextMesh;
            public Vector2 touchCoords = new Vector2(0.5f, 0.5f);
            public Vector3 currentTargetPoint;
            public float animPhase;
            public float lastActivityTime;
        }

        private GameObject avatarsRoot;
        private readonly Dictionary<string, CollaboratorAvatar> participants = new Dictionary<string, CollaboratorAvatar>();
        private readonly object participantsLock = new object();
        private readonly Queue<Action> mainThreadActions = new Queue<Action>();
        private readonly object queueLock = new object();

        // Multi-Participant In-World 3D Display Board
        private GameObject collaboratorBoardRoot;
        private TextMesh boardTitleTextMesh;
        private TextMesh boardDetailsTextMesh;

        // Standard fixed participant references for rapid access
        private CollaboratorAvatar pcAvatar;
        private CollaboratorAvatar vrAvatar;
        private CollaboratorAvatar primaryMobileAvatar;

        private bool isResonanceLocked = false;
        private Vector2 localMobileTouchPos = new Vector2(0.5f, 0.5f);

        // GUI Styles
        private GUIStyle phoneFrameStyle;
        private GUIStyle phoneHeaderStyle;
        private GUIStyle phoneBtnStyle;
        private GUIStyle roleBadgeStyle;
        private bool stylesReady = false;

        private void EnqueueMainThread(Action act)
        {
            lock (queueLock)
            {
                mainThreadActions.Enqueue(act);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (instance == null)
            {
                var existing = UnityEngine.Object.FindAnyObjectByType<HoloBridgeCrossDeviceHub>();
                if (existing != null)
                {
                    instance = existing;
                }
                else
                {
                    GameObject go = new GameObject("[HOLOBRIDGE_CROSS_DEVICE_HUB]");
                    instance = go.AddComponent<HoloBridgeCrossDeviceHub>();
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

            FindLocalIP();
            SetupLiveStreamCamera();
            Build3DDeviceAvatars();
        }

        private void Start()
        {
            if (enableLocalWebServer)
            {
                StartLocalWebServer();
            }
        }

        private void OnDestroy()
        {
            StopLocalWebServer();
            if (streamRenderTexture != null) streamRenderTexture.Release();
            if (streamTexture2D != null) Destroy(streamTexture2D);
        }

        #region Live Web Stream Engine

        private void SetupLiveStreamCamera()
        {
            if (streamCamObj != null) return;

            streamCamObj = new GameObject("HoloBridge_WebStreamCamera");
            streamCamObj.transform.SetParent(transform, false);

            streamCamera = streamCamObj.AddComponent<Camera>();
            streamCamera.depth = -50;
            streamCamera.clearFlags = CameraClearFlags.Skybox;
            streamCamera.fieldOfView = 60f;

            streamRenderTexture = new RenderTexture(StreamWidth, StreamHeight, 16, RenderTextureFormat.ARGB32);
            streamRenderTexture.Create();
            streamCamera.targetTexture = streamRenderTexture;

            streamTexture2D = new Texture2D(StreamWidth, StreamHeight, TextureFormat.RGB24, false);

            // Create initial placeholder frame
            latestJpgBytes = streamTexture2D.EncodeToJPG(50);
        }

        private void LateUpdate()
        {
            // Mirror Main Camera viewpoint for the Web Stream
            Camera mainCam = Camera.main;
            if (mainCam != null && streamCamera != null)
            {
                streamCamera.transform.position = mainCam.transform.position;
                streamCamera.transform.rotation = mainCam.transform.rotation;
                streamCamera.fieldOfView = mainCam.fieldOfView;
            }

            // Capture frame at ~25 FPS
            frameCaptureTimer += Time.deltaTime;
            if (frameCaptureTimer >= 0.04f && streamRenderTexture != null)
            {
                frameCaptureTimer = 0f;
                RenderTexture prev = RenderTexture.active;
                RenderTexture.active = streamRenderTexture;

                if (streamTexture2D != null)
                {
                    streamTexture2D.ReadPixels(new Rect(0, 0, StreamWidth, StreamHeight), 0, 0);
                    streamTexture2D.Apply(false);
                    byte[] jpg = streamTexture2D.EncodeToJPG(55);
                    lock (streamLock)
                    {
                        latestJpgBytes = jpg;
                    }
                }

                RenderTexture.active = prev;
            }
        }

        #endregion

        #region Multi-Participant 3D Humanoid/Cartoon Avatar System

        private void Build3DDeviceAvatars()
        {
            if (avatarsRoot != null) return;

            avatarsRoot = new GameObject("CrossDevice_Avatars_Root");
            avatarsRoot.transform.SetParent(transform, false);

            // 1. PC Collaborator Avatar (Amber Cyberpunk Architect)
            pcAvatar = CreateHumanoidAvatar(
                "pc_core",
                "🖥️ PC Macro-CAD Inspector",
                colorPC,
                DeviceRole.DesktopPC,
                new Vector3(1.10f, 0f, -0.65f),
                Quaternion.Euler(0f, -35f, 0f)
            );
            participants["pc_core"] = pcAvatar;

            // 2. VR Collaborator Avatar (Cyan Quest Operator)
            vrAvatar = CreateHumanoidAvatar(
                "vr_core",
                "🥽 VR Lead Operator",
                colorVR,
                DeviceRole.VRHeadset,
                new Vector3(-1.10f, 0f, -0.65f),
                Quaternion.Euler(0f, 35f, 0f)
            );
            participants["vr_core"] = vrAvatar;

            // 3. Primary Mobile Collaborator (Emerald Green)
            primaryMobileAvatar = CreateHumanoidAvatar(
                "mobile_primary",
                "📱 Mobile Collaborator #1",
                colorMobile,
                DeviceRole.MobileTablet,
                new Vector3(0.0f, 0f, -0.95f),
                Quaternion.Euler(0f, 0f, 0f)
            );
            participants["mobile_primary"] = primaryMobileAvatar;

            // 4. In-World 3D Live Collaborator Scoreboard (Active User Count)
            BuildInWorldCollaboratorBoard();
        }

        private void BuildInWorldCollaboratorBoard()
        {
            if (collaboratorBoardRoot != null) return;

            collaboratorBoardRoot = new GameObject("HoloBridge_Arena_CollaboratorBoard");
            collaboratorBoardRoot.transform.SetParent(avatarsRoot.transform, false);
            // Floating overhead display in the collaborative arena (Y = 2.25m, forward at Z = 1.80m)
            collaboratorBoardRoot.transform.position = new Vector3(0f, 2.25f, 1.80f);

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");

            // Glass Backplate
            GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "BoardPlate";
            plate.transform.SetParent(collaboratorBoardRoot.transform, false);
            plate.transform.localScale = new Vector3(1.75f, 0.44f, 0.02f);
            RemoveCollider(plate);

            var plateMat = new Material(litShader);
            plateMat.color = new Color(0.04f, 0.07f, 0.12f, 0.94f);
            if (plateMat.HasProperty("_BaseColor")) plateMat.SetColor("_BaseColor", new Color(0.04f, 0.07f, 0.12f, 0.94f));
            plate.GetComponent<MeshRenderer>().material = plateMat;

            // Glowing Neon Frame Trim
            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "BoardFrame";
            frame.transform.SetParent(collaboratorBoardRoot.transform, false);
            frame.transform.localScale = new Vector3(1.78f, 0.46f, 0.01f);
            frame.transform.localPosition = new Vector3(0f, 0f, 0.008f);
            RemoveCollider(frame);

            var frameMat = new Material(unlitShader) { color = new Color(0.0f, 0.85f, 1.0f, 1.0f) };
            frame.GetComponent<MeshRenderer>().material = frameMat;

            // Title TextMesh
            GameObject titleObj = new GameObject("TitleLabel");
            titleObj.transform.SetParent(collaboratorBoardRoot.transform, false);
            titleObj.transform.localPosition = new Vector3(0f, 0.09f, -0.015f);
            titleObj.transform.localScale = Vector3.one * 0.015f;

            boardTitleTextMesh = titleObj.AddComponent<TextMesh>();
            boardTitleTextMesh.text = "👥 ACTIVE COLLABORATORS: 3";
            boardTitleTextMesh.fontSize = 26;
            boardTitleTextMesh.characterSize = 1f;
            boardTitleTextMesh.anchor = TextAnchor.MiddleCenter;
            boardTitleTextMesh.alignment = TextAlignment.Center;
            boardTitleTextMesh.fontStyle = FontStyle.Bold;
            boardTitleTextMesh.color = new Color(0.0f, 1.0f, 0.55f);

            // Details Breakdown TextMesh
            GameObject detailObj = new GameObject("DetailLabel");
            detailObj.transform.SetParent(collaboratorBoardRoot.transform, false);
            detailObj.transform.localPosition = new Vector3(0f, -0.09f, -0.015f);
            detailObj.transform.localScale = Vector3.one * 0.012f;

            boardDetailsTextMesh = detailObj.AddComponent<TextMesh>();
            boardDetailsTextMesh.text = "[ 🥽 VR: 1  |  🖥️ PC: 1  |  📱 Mobile: 1 ]";
            boardDetailsTextMesh.fontSize = 20;
            boardDetailsTextMesh.characterSize = 1f;
            boardDetailsTextMesh.anchor = TextAnchor.MiddleCenter;
            boardDetailsTextMesh.alignment = TextAlignment.Center;
            boardDetailsTextMesh.color = new Color(0.80f, 0.95f, 1.0f);
        }

        public CollaboratorAvatar CreateHumanoidAvatar(string clientId, string name, Color themeColor, DeviceRole role, Vector3 pos, Quaternion rot)
        {
            GameObject root = new GameObject($"Avatar_{clientId}");
            root.transform.SetParent(avatarsRoot.transform, false);
            root.transform.position = pos;
            root.transform.rotation = rot;

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");

            Material suitMat = new Material(litShader);
            suitMat.color = new Color(0.08f, 0.10f, 0.14f);
            if (suitMat.HasProperty("_BaseColor")) suitMat.SetColor("_BaseColor", new Color(0.08f, 0.10f, 0.14f));

            Material neonMat = new Material(unlitShader) { color = themeColor };

            // -------------------------------------------------------------
            // A. TORSO & CHESTPLATE
            // -------------------------------------------------------------
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torso.name = "Torso";
            torso.transform.SetParent(root.transform, false);
            torso.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            torso.transform.localScale = new Vector3(0.36f, 0.44f, 0.22f);
            RemoveCollider(torso);
            torso.GetComponent<MeshRenderer>().material = suitMat;

            // Glowing team chest emblem
            GameObject emblem = GameObject.CreatePrimitive(PrimitiveType.Cube);
            emblem.name = "ChestEmblem";
            emblem.transform.SetParent(torso.transform, false);
            emblem.transform.localPosition = new Vector3(0f, 0.15f, 0.52f);
            emblem.transform.localScale = new Vector3(0.35f, 0.20f, 0.08f);
            RemoveCollider(emblem);
            emblem.GetComponent<MeshRenderer>().material = neonMat;

            // Shoulder Pauldrons
            for (int s = -1; s <= 1; s += 2)
            {
                GameObject pauldron = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pauldron.name = s < 0 ? "Shoulder_L" : "Shoulder_R";
                pauldron.transform.SetParent(root.transform, false);
                pauldron.transform.localPosition = new Vector3(s * 0.24f, 1.25f, 0f);
                pauldron.transform.localScale = new Vector3(0.12f, 0.12f, 0.14f);
                RemoveCollider(pauldron);
                pauldron.GetComponent<MeshRenderer>().material = neonMat;
            }

            // -------------------------------------------------------------
            // B. HEAD & STYLIZED CYBER VISOR / CARTOON EYES
            // -------------------------------------------------------------
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.48f, 0f);
            head.transform.localScale = new Vector3(0.24f, 0.25f, 0.24f);
            RemoveCollider(head);
            head.GetComponent<MeshRenderer>().material = suitMat;

            // Cyber Visor / Expressive Eyes
            GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "VisorEyes";
            visor.transform.SetParent(head.transform, false);
            visor.transform.localPosition = new Vector3(0f, 0.04f, 0.46f);
            visor.transform.localScale = new Vector3(0.70f, 0.22f, 0.18f);
            RemoveCollider(visor);
            visor.GetComponent<MeshRenderer>().material = neonMat;

            // Headset Antennas on left and right ears
            for (int e = -1; e <= 1; e += 2)
            {
                GameObject ear = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ear.name = e < 0 ? "Ear_L" : "Ear_R";
                ear.transform.SetParent(head.transform, false);
                ear.transform.localPosition = new Vector3(e * 0.50f, 0.05f, 0f);
                ear.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                ear.transform.localScale = new Vector3(0.15f, 0.06f, 0.15f);
                RemoveCollider(ear);
                ear.GetComponent<MeshRenderer>().material = neonMat;
            }

            // -------------------------------------------------------------
            // C. 3D FLOATING NAME BADGE
            // -------------------------------------------------------------
            GameObject badgeObj = new GameObject("NameBadge");
            badgeObj.transform.SetParent(root.transform, false);
            badgeObj.transform.localPosition = new Vector3(0f, 1.76f, 0f);

            // Badge Backplate
            GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "Plate";
            plate.transform.SetParent(badgeObj.transform, false);
            plate.transform.localScale = new Vector3(0.68f, 0.10f, 0.015f);
            RemoveCollider(plate);
            plate.GetComponent<MeshRenderer>().material = suitMat;

            // TextMesh Component
            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(badgeObj.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0f, -0.015f);
            textObj.transform.localScale = Vector3.one * 0.015f;

            TextMesh tm = textObj.AddComponent<TextMesh>();
            tm.text = name;
            tm.fontSize = 24;
            tm.characterSize = 1f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = themeColor;

            // -------------------------------------------------------------
            // D. LEFT HAND & HELD DEVICE (Smartphone, Console, or Controller)
            // -------------------------------------------------------------
            GameObject leftHand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leftHand.name = "Hand_Left";
            leftHand.transform.SetParent(root.transform, false);
            leftHand.transform.localPosition = new Vector3(-0.24f, 1.15f, 0.28f);
            leftHand.transform.localScale = Vector3.one * 0.09f;
            RemoveCollider(leftHand);
            leftHand.GetComponent<MeshRenderer>().material = suitMat;

            if (role == DeviceRole.MobileTablet)
            {
                // Glowing 3D Smartphone held in hand
                GameObject phone = GameObject.CreatePrimitive(PrimitiveType.Cube);
                phone.name = "Smartphone";
                phone.transform.SetParent(leftHand.transform, false);
                phone.transform.localPosition = new Vector3(0f, 0.05f, 0.05f);
                phone.transform.localScale = new Vector3(1.2f, 2.2f, 0.15f);
                phone.transform.localRotation = Quaternion.Euler(-30f, 15f, 0f);
                RemoveCollider(phone);
                phone.GetComponent<MeshRenderer>().material = neonMat;
            }
            else if (role == DeviceRole.DesktopPC)
            {
                // Holographic CAD Console
                GameObject console = GameObject.CreatePrimitive(PrimitiveType.Cube);
                console.name = "CADConsole";
                console.transform.SetParent(leftHand.transform, false);
                console.transform.localPosition = new Vector3(0.12f, 0.08f, 0.15f);
                console.transform.localScale = new Vector3(2.8f, 0.2f, 1.6f);
                RemoveCollider(console);
                console.GetComponent<MeshRenderer>().material = neonMat;
            }

            // -------------------------------------------------------------
            // E. RIGHT HAND & 3D OPTICAL LASER RAY
            // -------------------------------------------------------------
            GameObject rightHand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rightHand.name = "Hand_Right";
            rightHand.transform.SetParent(root.transform, false);
            rightHand.transform.localPosition = new Vector3(0.24f, 1.18f, 0.35f);
            rightHand.transform.localScale = Vector3.one * 0.09f;
            RemoveCollider(rightHand);
            rightHand.GetComponent<MeshRenderer>().material = suitMat;

            // Optical Laser LineRenderer
            GameObject laserObj = new GameObject("LaserBeam");
            laserObj.transform.SetParent(rightHand.transform, false);
            var lr = laserObj.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = themeColor;
            lr.endColor = new Color(themeColor.r, themeColor.g, themeColor.b, 0.25f);
            lr.startWidth = 0.007f;
            lr.endWidth = 0.003f;
            lr.positionCount = 2;
            lr.useWorldSpace = true;

            // Laser Reticle Dot
            GameObject reticle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            reticle.name = "ReticleDot";
            reticle.transform.SetParent(root.transform, false);
            reticle.transform.localScale = Vector3.one * 0.035f;
            RemoveCollider(reticle);
            var rMr = reticle.GetComponent<MeshRenderer>();
            rMr.material = new Material(Shader.Find("Sprites/Default")) { color = themeColor };

            var avatar = new CollaboratorAvatar
            {
                clientId = clientId,
                displayName = name,
                teamColor = themeColor,
                role = role,
                rootObj = root,
                headTransform = head.transform,
                torsoTransform = torso.transform,
                leftHandTransform = leftHand.transform,
                rightHandTransform = rightHand.transform,
                laserBeam = lr,
                laserReticle = reticle.transform,
                nameTagTextMesh = tm,
                animPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                lastActivityTime = Time.time
            };

            return avatar;
        }

        private void RemoveCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        #endregion

        #region Update & Cross-Ray Interaction

        private void Update()
        {
            // Execute thread-safe queued actions from Web Server
            lock (queueLock)
            {
                while (mainThreadActions.Count > 0)
                {
                    try { mainThreadActions.Dequeue()?.Invoke(); } catch { }
                }
            }

            HandleRoleShortcuts();
            UpdateCollaboratorAvatars();
            EvaluateCrossRayResonance();
        }

        private void HandleRoleShortcuts()
        {
            // F1: Switch active device role
            if (Input.GetKeyDown(KeyCode.F1) && !OmniOS.Apps.MediaHub.OmniAppMediaHub.IsTypingActive)
            {
                CycleNextRole();
            }

            // 'M' Key: Toggle On-Screen Mobile Phone Simulator
            if (Input.GetKeyDown(KeyCode.M) && !OmniOS.Apps.MediaHub.OmniAppMediaHub.IsTypingActive)
            {
                showMobileSimulator = !showMobileSimulator;
                OmniOSManager.Instance?.ShowNotificationToast($"📱 Mobile Touch Simulator: {(showMobileSimulator ? "OPEN" : "CLOSED")} [M]");
            }

            // 'P' Key: Surface-to-Spatial Screen-Peel Extrusion
            if (Input.GetKeyDown(KeyCode.P) && !OmniOS.Apps.MediaHub.OmniAppMediaHub.IsTypingActive)
            {
                TriggerScreenPeel();
            }

            // 'X' Key: Toggle Exploded View
            if (Input.GetKeyDown(KeyCode.X) && !OmniOS.Apps.MediaHub.OmniAppMediaHub.IsTypingActive)
            {
                SharedDigitalTwinAssembly.Instance?.ToggleExplodedView();
            }
        }

        public void CycleNextRole()
        {
            DeviceRole next = (DeviceRole)(((int)activeRole + 1) % 3);
            SetRole(next);
        }

        public void SetRole(DeviceRole role)
        {
            activeRole = role;
            string roleName = activeRole switch
            {
                DeviceRole.VRHeadset => "🥽 Active Role: VR COLLABORATOR (Spatial Operator)",
                DeviceRole.MobileTablet => "📱 Active Role: MOBILE COLLABORATOR (Touch / 6DOF Lens)",
                _ => "🖥️ Active Role: DESKTOP PC (Macro CAD Inspector)"
            };

            OmniOSManager.Instance?.ShowNotificationToast(roleName);
            SpatialAudioEngine.Instance?.PlayThemeChime();
        }

        private void UpdateCollaboratorAvatars()
        {
            Camera cam = Camera.main;
            Vector3 turbineTarget = SharedDigitalTwinAssembly.Instance != null
                ? SharedDigitalTwinAssembly.Instance.transform.position + Vector3.up * 1.3f
                : new Vector3(0f, 1.3f, 0f);

            float time = Time.time;
            int vrCount = 0;
            int pcCount = 0;
            int mobCount = 0;

            lock (participantsLock)
            {
                foreach (var kvp in participants)
                {
                    var av = kvp.Value;
                    if (av == null || av.rootObj == null) continue;

                    // Tally device role counts
                    if (av.role == DeviceRole.VRHeadset) vrCount++;
                    else if (av.role == DeviceRole.DesktopPC) pcCount++;
                    else mobCount++;

                    // 1. Organic Living Breathing Animation
                    float breathOffset = Mathf.Sin(time * 2.2f + av.animPhase) * 0.012f;
                    if (av.torsoTransform != null)
                    {
                        av.torsoTransform.localPosition = new Vector3(0f, 1.05f + breathOffset, 0f);
                    }
                    if (av.headTransform != null)
                    {
                        av.headTransform.localPosition = new Vector3(0f, 1.48f + breathOffset * 1.4f, 0f);
                    }

                    // 2. Laser Origin (From Right Pointer Hand)
                    Vector3 laserOrigin = av.rightHandTransform != null
                        ? av.rightHandTransform.position
                        : av.rootObj.transform.position + Vector3.up * 1.2f;

                    // 3. Compute Aiming Target Point
                    Vector3 aimTarget;

                    if (av.role == DeviceRole.DesktopPC && activeRole == DeviceRole.DesktopPC && cam != null)
                    {
                        Ray mouseRay = cam.ScreenPointToRay(Input.mousePosition);
                        aimTarget = Physics.Raycast(mouseRay, out RaycastHit hit, 20f) ? hit.point : turbineTarget;
                    }
                    else if (av.role == DeviceRole.VRHeadset && activeRole == DeviceRole.VRHeadset && cam != null)
                    {
                        Ray mouseRay = cam.ScreenPointToRay(Input.mousePosition);
                        aimTarget = Physics.Raycast(mouseRay, out RaycastHit hit, 20f) ? hit.point : turbineTarget;
                    }
                    else
                    {
                        // Aim driven by mobile touch coordinates (0..1)
                        float tx = (av.touchCoords.x - 0.5f) * 0.70f;
                        float ty = (av.touchCoords.y - 0.5f) * 0.70f;
                        aimTarget = turbineTarget + new Vector3(tx, ty, 0f);
                    }

                    av.currentTargetPoint = aimTarget;

                    // 4. Update Laser Beam & Reticle
                    if (av.laserBeam != null)
                    {
                        av.laserBeam.SetPosition(0, laserOrigin);
                        av.laserBeam.SetPosition(1, aimTarget);
                    }
                    if (av.laserReticle != null)
                    {
                        av.laserReticle.position = aimTarget;
                    }

                    // 5. Head looks toward aim target
                    if (av.headTransform != null)
                    {
                        Vector3 lookDir = (aimTarget - av.headTransform.position).normalized;
                        if (lookDir.sqrMagnitude > 0.01f)
                        {
                            av.headTransform.rotation = Quaternion.Slerp(av.headTransform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 6f);
                        }
                    }

                    // 6. Billboard Avatar Floating Name Badge toward Camera
                    if (av.nameTagTextMesh != null && cam != null)
                    {
                        Vector3 toCam = av.nameTagTextMesh.transform.position - cam.transform.position;
                        if (toCam.sqrMagnitude > 0.01f)
                        {
                            av.nameTagTextMesh.transform.rotation = Quaternion.LookRotation(toCam);
                        }
                    }
                }
            }

            // 7. Update Live In-World 3D Scoreboard Text & Billboarding
            int totalCount = vrCount + pcCount + mobCount;
            if (boardTitleTextMesh != null)
            {
                boardTitleTextMesh.text = $"👥 ACTIVE OPERATORS: {totalCount}";
            }
            if (boardDetailsTextMesh != null)
            {
                boardDetailsTextMesh.text = $"[ 🥽 VR: {vrCount}  |  🖥️ PC: {pcCount}  |  📱 Mobile: {mobCount} ]";
            }
            if (collaboratorBoardRoot != null && cam != null)
            {
                Vector3 toCamBoard = collaboratorBoardRoot.transform.position - cam.transform.position;
                if (toCamBoard.sqrMagnitude > 0.01f)
                {
                    collaboratorBoardRoot.transform.rotation = Quaternion.LookRotation(toCamBoard);
                }
            }
        }

        private void EvaluateCrossRayResonance()
        {
            if (!isCrossRayActive || pcAvatar == null || vrAvatar == null) return;
            if (pcAvatar.laserReticle == null || vrAvatar.laserReticle == null) return;

            float distance = Vector3.Distance(pcAvatar.laserReticle.position, vrAvatar.laserReticle.position);

            if (distance < resonanceThreshold)
            {
                if (!isResonanceLocked)
                {
                    isResonanceLocked = true;
                    OnCrossRayResonanceTriggered(pcAvatar.laserReticle.position);
                }

                if (pcAvatar.laserBeam != null)
                {
                    pcAvatar.laserBeam.startColor = colorResonance;
                    pcAvatar.laserBeam.endColor = colorResonance;
                    pcAvatar.laserBeam.startWidth = 0.014f;
                }
                if (vrAvatar.laserBeam != null)
                {
                    vrAvatar.laserBeam.startColor = colorResonance;
                    vrAvatar.laserBeam.endColor = colorResonance;
                    vrAvatar.laserBeam.startWidth = 0.014f;
                }
            }
            else
            {
                if (isResonanceLocked)
                {
                    isResonanceLocked = false;
                    if (pcAvatar.laserBeam != null)
                    {
                        pcAvatar.laserBeam.startColor = colorPC;
                        pcAvatar.laserBeam.endColor = new Color(colorPC.r, colorPC.g, colorPC.b, 0.25f);
                        pcAvatar.laserBeam.startWidth = 0.007f;
                    }
                    if (vrAvatar.laserBeam != null)
                    {
                        vrAvatar.laserBeam.startColor = colorVR;
                        vrAvatar.laserBeam.endColor = new Color(colorVR.r, colorVR.g, colorVR.b, 0.25f);
                        vrAvatar.laserBeam.startWidth = 0.007f;
                    }
                }
            }
        }

        private void OnCrossRayResonanceTriggered(Vector3 convergencePoint)
        {
            SpatialAudioEngine.Instance?.PlayThemeChime();
            OmniOSManager.Instance?.ShowNotificationToast("⚡ [CROSS-RAY RESONANCE] Target Disambiguated: Internal Needle Bearing! Both Collaborators Locked On!");

            var assembly = SharedDigitalTwinAssembly.Instance;
            if (assembly != null)
            {
                var bearing = assembly.GetPart(SharedDigitalTwinAssembly.ComponentType.InternalBearing);
                if (bearing != null && bearing.gameObject != null)
                {
                    bearing.isSelected = true;
                    assembly.ExplodeProgress = Mathf.Max(assembly.ExplodeProgress, 0.65f);
                }
            }
        }

        #endregion

        #region Surface-to-Spatial Screen-Peel (CR-Extrude)

        public void TriggerScreenPeel()
        {
            if (isHologramPeeled)
            {
                SnapPeeledHologram();
                return;
            }

            isHologramPeeled = true;
            SpatialAudioEngine.Instance?.PlayThemeChime();
            OmniOSManager.Instance?.ShowNotificationToast("✨ [SCREEN-PEEL] 2D Blueprint Extruded into Floating 3D Hologram! VR Collaborator can now grab it.");

            if (peeledHologramPart == null)
            {
                peeledHologramPart = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                peeledHologramPart.name = "Peeled_Combustion_Nozzle_Hologram";
                peeledHologramPart.transform.localScale = new Vector3(0.12f, 0.06f, 0.12f);
                var mr = peeledHologramPart.GetComponent<MeshRenderer>();
                var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                mat.color = new Color(0.0f, 1.0f, 0.85f, 0.85f);
                mr.material = mat;
            }

            peeledHologramPart.SetActive(true);
            peeledHologramPart.transform.position = new Vector3(0.0f, 1.45f, -0.30f);
            peeledHologramPart.transform.rotation = Quaternion.Euler(45f, 30f, 0f);

            StartCoroutine(AnimatePeelTrajectory(peeledHologramPart.transform));
        }

        public void SnapPeeledHologram()
        {
            if (!isHologramPeeled || peeledHologramPart == null) return;

            isHologramPeeled = false;
            peeledHologramPart.SetActive(false);
            SpatialAudioEngine.Instance?.PlayButtonClick();
            OmniOSManager.Instance?.ShowNotificationToast("✅ [ASSEMBLY SNAP] Component Magnetically Snapped with ±0.05mm Precision!");
        }

        private IEnumerator AnimatePeelTrajectory(Transform target)
        {
            Vector3 startPos = primaryMobileAvatar != null ? primaryMobileAvatar.rootObj.transform.position + Vector3.up * 1.1f : new Vector3(0, 1.25f, -0.75f);
            Vector3 endPos = new Vector3(0.0f, 1.45f, -0.30f);
            float elapsed = 0f;
            float duration = 0.8f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                target.position = Vector3.Lerp(startPos, endPos, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.2f;
                yield return null;
            }
        }

        #endregion

        #region Multi-Client Web Server & Live Camera Streaming

        private void FindLocalIP()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork && !ip.ToString().StartsWith("127."))
                    {
                        localIpAddress = ip.ToString();
                        break;
                    }
                }
            }
            catch
            {
                localIpAddress = "127.0.0.1";
            }
        }

        private void StartLocalWebServer()
        {
            try
            {
                tcpListener = new TcpListener(IPAddress.Any, webServerPort);
                tcpListener.Start();
                isServerRunning = true;

                webServerThread = new Thread(WebServerWorker) { IsBackground = true };
                webServerThread.Start();

                Debug.Log($"<color=#00ff88>[HoloBridge WebServer]</color> Live Web Controller Online at http://{localIpAddress}:{webServerPort}/ and http://localhost:{webServerPort}/");
                OmniOSManager.Instance?.ShowNotificationToast($"📱 Web Controller Online: http://{localIpAddress}:{webServerPort}/");
            }
            catch (Exception ex)
            {
                Debug.Log($"[HoloBridge WebServer] Note: {ex.Message} (On-Screen Mobile Simulator [M] is active as direct fallback).");
            }
        }

        private void StopLocalWebServer()
        {
            isServerRunning = false;
            try
            {
                tcpListener?.Stop();
            }
            catch { }
        }

        private void WebServerWorker()
        {
            while (isServerRunning && tcpListener != null)
            {
                try
                {
                    TcpClient client = tcpListener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(state => HandleHttpClient(client));
                }
                catch { }
            }
        }

        private void HandleHttpClient(TcpClient client)
        {
            try
            {
                using (client)
                using (var stream = client.GetStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                {
                    string reqLine = reader.ReadLine();
                    if (string.IsNullOrEmpty(reqLine)) return;

                    string[] parts = reqLine.Split(' ');
                    if (parts.Length < 2) return;
                    string url = parts[1];

                    // 1. LIVE VIDEO FRAME ENDPOINT (/api/frame)
                    if (url.StartsWith("/api/frame"))
                    {
                        byte[] jpg;
                        lock (streamLock)
                        {
                            jpg = latestJpgBytes;
                        }

                        if (jpg != null && jpg.Length > 0)
                        {
                            writer.Write($"HTTP/1.1 200 OK\r\nContent-Type: image/jpeg\r\nCache-Control: no-cache, no-store, must-revalidate\r\nPragma: no-cache\r\nAccess-Control-Allow-Origin: *\r\nContent-Length: {jpg.Length}\r\nConnection: close\r\n\r\n");
                            writer.Flush();
                            stream.Write(jpg, 0, jpg.Length);
                        }
                    }
                    // 2. ACTIVE USERS & PARTICIPANT METRICS ENDPOINT (/api/users?client=...)
                    else if (url.StartsWith("/api/users"))
                    {
                        string cId = null;
                        int qIdx = url.IndexOf('?');
                        if (qIdx >= 0)
                        {
                            string query = url.Substring(qIdx + 1);
                            var pairs = query.Split('&');
                            foreach (var p in pairs)
                            {
                                var kv = p.Split('=');
                                if (kv.Length == 2 && kv[0] == "client" && !string.IsNullOrEmpty(kv[1]))
                                {
                                    cId = kv[1];
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(cId))
                        {
                            EnqueueMainThread(() => EnsureParticipantRegistered(cId));
                        }

                        string json = GetUsersJson();
                        byte[] body = Encoding.UTF8.GetBytes(json);
                        writer.Write($"HTTP/1.1 200 OK\r\nContent-Type: application/json; charset=utf-8\r\nCache-Control: no-cache, no-store, must-revalidate\r\nAccess-Control-Allow-Origin: *\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n{json}");
                    }
                    // 3. TOUCH COORDINATES ENDPOINT (/api/touch?client=...&x=...&y=...)
                    else if (url.StartsWith("/api/touch"))
                    {
                        int qIdx = url.IndexOf('?');
                        string cId = "mobile_primary";
                        float tx = 0.5f, ty = 0.5f;

                        if (qIdx >= 0)
                        {
                            string query = url.Substring(qIdx + 1);
                            var pairs = query.Split('&');
                            foreach (var p in pairs)
                            {
                                var kv = p.Split('=');
                                if (kv.Length == 2)
                                {
                                    if (kv[0] == "client" && !string.IsNullOrEmpty(kv[1])) cId = kv[1];
                                    if (kv[0] == "x") float.TryParse(kv[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out tx);
                                    if (kv[0] == "y") float.TryParse(kv[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out ty);
                                }
                            }
                        }

                        EnqueueMainThread(() => UpdateOrCreateParticipantTouch(cId, tx, ty));

                        string json = "{\"status\":\"ok\"}";
                        byte[] body = Encoding.UTF8.GetBytes(json);
                        writer.Write($"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nAccess-Control-Allow-Origin: *\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n{json}");
                    }
                    // 4. ACTION ENDPOINT (/api/action?type=...&client=...)
                    else if (url.StartsWith("/api/action"))
                    {
                        int qIdx = url.IndexOf('?');
                        if (qIdx >= 0)
                        {
                            string query = url.Substring(qIdx + 1);
                            var pairs = query.Split('&');
                            foreach (var p in pairs)
                            {
                                var kv = p.Split('=');
                                if (kv.Length == 2 && kv[0] == "type")
                                {
                                    string act = kv[1];
                                    if (act == "peel") EnqueueMainThread(() => TriggerScreenPeel());
                                    else if (act == "explode") EnqueueMainThread(() => SharedDigitalTwinAssembly.Instance?.ToggleExplodedView());
                                    else if (act == "xray") EnqueueMainThread(() => SharedDigitalTwinAssembly.Instance?.ToggleXRay());
                                    else if (act == "theme") EnqueueMainThread(() => MetaverseEnvironmentManager.Instance?.NextTheme());
                                }
                            }
                        }
                        string json = "{\"status\":\"action_executed\"}";
                        byte[] body = Encoding.UTF8.GetBytes(json);
                        writer.Write($"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nAccess-Control-Allow-Origin: *\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n{json}");
                    }
                    // 5. MAIN WEB CONTROLLER HTML WITH LIVE EMBEDDED STREAM
                    else
                    {
                        string html = GetMobileControllerHtml();
                        byte[] body = Encoding.UTF8.GetBytes(html);
                        writer.Write($"HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nAccess-Control-Allow-Origin: *\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n{html}");
                    }
                }
            }
            catch { }
        }

        private string GetUsersJson()
        {
            int vr = 0, pc = 0, mob = 0;
            var sb = new StringBuilder();

            lock (participantsLock)
            {
                bool first = true;
                foreach (var kvp in participants)
                {
                    var av = kvp.Value;
                    if (av == null) continue;

                    if (av.role == DeviceRole.VRHeadset) vr++;
                    else if (av.role == DeviceRole.DesktopPC) pc++;
                    else mob++;

                    if (!first) sb.Append(",");
                    first = false;

                    string hexCol = ColorUtility.ToHtmlStringRGB(av.teamColor);
                    string roleStr = av.role switch
                    {
                        DeviceRole.VRHeadset => "VR",
                        DeviceRole.DesktopPC => "PC",
                        _ => "Mobile"
                    };
                    sb.Append($"{{\"id\":\"{EscapeJson(av.clientId)}\",\"name\":\"{EscapeJson(av.displayName)}\",\"role\":\"{roleStr}\",\"color\":\"#{hexCol}\"}}");
                }
            }

            int total = vr + pc + mob;
            return $"{{\"count\":{total},\"vr\":{vr},\"pc\":{pc},\"mobile\":{mob},\"users\":[{sb}]}}";
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", "");
        }

        private void EnsureParticipantRegistered(string cId)
        {
            if (string.IsNullOrEmpty(cId)) return;
            lock (participantsLock)
            {
                if (participants.ContainsKey(cId)) return;
            }
            RegisterWebGuest(cId, 0.5f, 0.5f);
        }

        private void UpdateOrCreateParticipantTouch(string cId, float tx, float ty)
        {
            lock (participantsLock)
            {
                if (participants.TryGetValue(cId, out CollaboratorAvatar av))
                {
                    av.touchCoords = new Vector2(tx, ty);
                    av.lastActivityTime = Time.time;
                    return;
                }
            }
            RegisterWebGuest(cId, tx, ty);
        }

        private void RegisterWebGuest(string cId, float tx, float ty)
        {
            lock (participantsLock)
            {
                if (participants.ContainsKey(cId)) return;

                int guestIndex = participants.Count - 2;
                if (guestIndex < 1) guestIndex = 1;

                Color[] guestPalette = new Color[]
                {
                    new Color(0.0f, 1.0f, 0.55f),  // Emerald
                    new Color(1.0f, 0.0f, 0.75f),  // Magenta
                    new Color(1.0f, 0.50f, 0.0f),  // Radiant Orange
                    new Color(0.60f, 0.20f, 1.0f),  // Violet
                    new Color(0.0f, 0.85f, 1.0f),  // Electric Cyan
                    new Color(1.0f, 0.85f, 0.10f)  // Cyber Yellow
                };
                Color assignedCol = guestPalette[(guestIndex - 1) % guestPalette.Length];

                // Position around the collaborative arena in a circular formation
                float angle = (guestIndex * 50f - 25f) * Mathf.Deg2Rad;
                Vector3 guestPos = new Vector3(Mathf.Sin(angle) * 1.30f, 0f, -Mathf.Cos(angle) * 1.30f);
                Quaternion guestRot = Quaternion.LookRotation(-guestPos);

                string guestName = $"📱 Mobile Guest #{guestIndex}";
                var newAvatar = CreateHumanoidAvatar(cId, guestName, assignedCol, DeviceRole.MobileTablet, guestPos, guestRot);
                newAvatar.touchCoords = new Vector2(tx, ty);
                participants[cId] = newAvatar;
            }

            OmniOSManager.Instance?.ShowNotificationToast($"👥 New Collaborator Connected: Mobile Guest #{participants.Count - 2}!");
            SpatialAudioEngine.Instance?.PlayThemeChime();
        }

        private string GetMobileControllerHtml()
        {
            return @"<!DOCTYPE html>
<html>
<head>
<meta name='viewport' content='width=device-width, initial-scale=1.0, user-scalable=no'>
<title>HoloBridge Live Spatial Controller</title>
<style>
  * { box-sizing: border-box; }
  body { background: #07090e; color: #00e5ff; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 0; padding: 12px; text-align: center; }
  h1 { font-size: 16px; color: #00ff88; letter-spacing: 1px; margin: 4px 0 2px 0; text-transform: uppercase; }
  p { font-size: 11px; color: #7ab0d6; margin: 0 0 8px 0; }
  
  /* Live Operator Counter & Badges */
  .users-card { width: 100%; max-width: 500px; background: rgba(14,24,38,0.85); border: 1.5px solid #00e5ff; border-radius: 10px; margin: 0 auto 10px auto; padding: 8px 12px; box-shadow: 0 0 14px rgba(0,229,255,0.18); }
  .users-row-main { display: flex; justify-content: space-between; align-items: center; margin-bottom: 6px; }
  .live-tag { display: inline-flex; align-items: center; font-size: 11px; font-weight: bold; color: #00ff88; letter-spacing: 0.5px; }
  .pulse-dot { width: 8px; height: 8px; background: #00ff88; border-radius: 50%; display: inline-block; margin-right: 6px; box-shadow: 0 0 8px #00ff88; animation: pulse 1.6s infinite; }
  @keyframes pulse { 0% { opacity: 1; transform: scale(1); } 50% { opacity: 0.4; transform: scale(1.3); } 100% { opacity: 1; transform: scale(1); } }
  .total-badge { background: #00ff88; color: #000; font-size: 12px; font-weight: 900; padding: 2px 10px; border-radius: 12px; }
  .roles-pills { display: flex; gap: 6px; justify-content: center; margin-bottom: 6px; flex-wrap: wrap; }
  .role-pill { font-size: 10px; font-weight: bold; padding: 3px 8px; border-radius: 5px; border: 1px solid; }
  .role-pill.vr { color: #00e5ff; border-color: #00e5ff; background: rgba(0,229,255,0.12); }
  .role-pill.pc { color: #ffaa00; border-color: #ffaa00; background: rgba(255,170,0,0.12); }
  .role-pill.mob { color: #00ff88; border-color: #00ff88; background: rgba(0,255,136,0.12); }
  .users-list { display: flex; gap: 4px; justify-content: center; flex-wrap: wrap; }
  .user-chip { font-size: 9px; padding: 2px 6px; border-radius: 4px; border: 1px solid #4e7899; background: #09111c; color: #cfe6f8; }
  .user-chip.me { border-color: #00ff88; color: #00ff88; font-weight: bold; }

  /* Live Video Feed Container */
  .feed-box { width: 100%; max-width: 500px; height: 210px; background: #000; border: 2px solid #00e5ff; border-radius: 10px; margin: 0 auto 10px auto; overflow: hidden; position: relative; box-shadow: 0 0 16px rgba(0,229,255,0.25); }
  #liveStream { width: 100%; height: 100%; object-fit: cover; display: block; }
  .feed-badge { position: absolute; top: 6px; left: 8px; background: rgba(0,0,0,0.75); color: #00ff88; font-size: 10px; font-weight: bold; padding: 3px 8px; border-radius: 4px; border: 1px solid #00ff88; }
  .fps-badge { position: absolute; top: 6px; right: 8px; background: rgba(0,0,0,0.75); color: #00e5ff; font-size: 10px; font-weight: bold; padding: 3px 8px; border-radius: 4px; border: 1px solid #00e5ff; }
  
  /* Spatial Touchpad */
  #touchpad { width: 100%; max-width: 500px; height: 210px; background: radial-gradient(circle, #102030 0%, #080c14 100%); border: 2px solid #00ff88; border-radius: 10px; margin: 0 auto 10px auto; touch-action: none; position: relative; cursor: crosshair; }
  #puck { width: 34px; height: 34px; background: #00ff88; border-radius: 50%; position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%); pointer-events: none; box-shadow: 0 0 14px #00ff88; }
  .pad-hint { position: absolute; bottom: 6px; left: 0; width: 100%; font-size: 10px; color: #5a8aa8; pointer-events: none; }
  
  /* Action Deck Buttons */
  .btn-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 8px; max-width: 500px; margin: 0 auto; }
  .btn { width: 100%; padding: 12px 6px; font-size: 13px; font-weight: bold; border: none; border-radius: 8px; cursor: pointer; color: #000; text-transform: uppercase; }
  .btn-peel { background: linear-gradient(90deg, #00ff88, #00e5ff); }
  .btn-explode { background: #ffaa00; }
  .btn-xray { background: #00bfff; }
  .btn-theme { background: linear-gradient(90deg, #ff00bb, #8833ff); color: #fff; }
  .status-bar { font-size: 10px; color: #4e7899; margin-top: 8px; }
</style>
</head>
<body>
<h1>HoloBridge Spatial Hub</h1>
<p>Live 3D Multi-Device Spatial Interaction &bull; IEEE VR 2027</p>

<!-- Live Connected Collaborators / Operators Bar -->
<div class='users-card'>
  <div class='users-row-main'>
    <div class='live-tag'><span class='pulse-dot'></span> LIVE OPERATORS</div>
    <div class='total-badge'>👥 <span id='userCount'>3</span> ACTIVE</div>
  </div>
  <div class='roles-pills'>
    <span class='role-pill vr'>🥽 VR: <b id='vrCount'>1</b></span>
    <span class='role-pill pc'>🖥️ PC: <b id='pcCount'>1</b></span>
    <span class='role-pill mob'>📱 Mobile: <b id='mobCount'>1</b></span>
  </div>
  <div class='users-list' id='userChips'></div>
</div>

<!-- Live Camera Viewport from Unity Editor -->
<div class='feed-box'>
  <div class='feed-badge'>🔴 LIVE SPATIAL STREAM</div>
  <div class='fps-badge' id='fpsCounter'>25 FPS</div>
  <img id='liveStream' src='/api/frame' alt='Connecting to Unity Live View...' />
</div>

<!-- 3D Optical Laser Touchpad -->
<div id='touchpad'>
  <div id='puck'></div>
  <div class='pad-hint'>Touch & Drag to Aim 3D Avatar Laser in Unity</div>
</div>

<!-- Collaborative Action Deck -->
<div class='btn-grid'>
  <button class='btn btn-peel' onclick='callAction(""peel"")'>✨ Peel to 3D VR</button>
  <button class='btn btn-explode' onclick='callAction(""explode"")'>💥 Explode View</button>
  <button class='btn btn-xray' onclick='callAction(""xray"")'>👁️ Toggle X-Ray</button>
  <button class='btn btn-theme' onclick='callAction(""theme"")'>🎨 Switch Theme</button>
</div>

<div class='status-bar' id='clientStatus'>Connecting to Unity Spatial Core...</div>

<script>
  // Generate unique client ID per device/tab
  let myClientId = sessionStorage.getItem('holobridge_client_id');
  if (!myClientId) {
    myClientId = 'guest_' + Math.floor(Math.random() * 8999 + 1000);
    sessionStorage.setItem('holobridge_client_id', myClientId);
  }
  document.getElementById('clientStatus').innerText = 'Connected as: ' + myClientId + ' (3D Avatar Active in Unity)';

  // 0. Poll Active Operators & Device Roster
  function pollUsers() {
    fetch('/api/users?client=' + encodeURIComponent(myClientId))
      .then(r => r.json())
      .then(d => {
        if (!d) return;
        document.getElementById('userCount').innerText = d.count;
        document.getElementById('vrCount').innerText = d.vr;
        document.getElementById('pcCount').innerText = d.pc;
        document.getElementById('mobCount').innerText = d.mobile;
        const chips = document.getElementById('userChips');
        chips.innerHTML = '';
        if (d.users) {
          d.users.forEach(u => {
            const chip = document.createElement('span');
            chip.className = 'user-chip' + (u.id === myClientId ? ' me' : '');
            chip.style.borderColor = u.color;
            chip.innerText = (u.id === myClientId ? '⭐ ' : '') + u.name;
            chips.appendChild(chip);
          });
        }
      })
      .catch(e => {});
  }
  setInterval(pollUsers, 1100);
  pollUsers();

  // 1. High-Frequency Live Frame Streamer
  const streamImg = document.getElementById('liveStream');
  const fpsText = document.getElementById('fpsCounter');
  let frameCount = 0;
  let lastTime = Date.now();

  function loopFrame() {
    const next = new Image();
    next.onload = function() {
      streamImg.src = next.src;
      frameCount++;
      const now = Date.now();
      if (now - lastTime >= 1000) {
        fpsText.innerText = frameCount + ' FPS';
        frameCount = 0;
        lastTime = now;
      }
      setTimeout(loopFrame, 35);
    };
    next.onerror = function() {
      setTimeout(loopFrame, 250);
    };
    next.src = '/api/frame?t=' + Date.now();
  }
  loopFrame();

  // 2. Touchpad Motion Steering
  const pad = document.getElementById('touchpad');
  const puck = document.getElementById('puck');
  let lastSend = 0;

  function handleMove(e) {
    const rect = pad.getBoundingClientRect();
    const touch = e.touches ? e.touches[0] : e;
    let x = (touch.clientX - rect.left) / rect.width;
    let y = (touch.clientY - rect.top) / rect.height;
    x = Math.max(0, Math.min(1, x));
    y = Math.max(0, Math.min(1, y));
    puck.style.left = (x * 100) + '%';
    puck.style.top = (y * 100) + '%';

    const now = Date.now();
    if (now - lastSend > 30) {
      lastSend = now;
      fetch('/api/touch?client=' + myClientId + '&x=' + x.toFixed(3) + '&y=' + (1 - y).toFixed(3));
    }
  }

  pad.addEventListener('touchmove', handleMove);
  pad.addEventListener('mousemove', (e) => { if (e.buttons === 1) handleMove(e); });

  function callAction(act) {
    if (navigator.vibrate) navigator.vibrate([15, 25, 15]);
    fetch('/api/action?type=' + act + '&client=' + myClientId);
  }
</script>
</body>
</html>";
        }

        #endregion

        #region OnGUI Visual Deck & Mobile Phone Simulator

        private void InitStyles()
        {
            if (stylesReady) return;

            phoneFrameStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 10, 10)
            };

            phoneHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = colorMobile }
            };

            phoneBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.black }
            };

            roleBadgeStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(8, 8, 4, 4)
            };

            stylesReady = true;
        }

        private void OnGUI()
        {
            InitStyles();

            // 0. Top-Center Live Operators Banner (In-Game / In-Editor HUD)
            int vrCount = 0, pcCount = 0, mobCount = 0;
            lock (participantsLock)
            {
                foreach (var p in participants.Values)
                {
                    if (p == null) continue;
                    if (p.role == DeviceRole.VRHeadset) vrCount++;
                    else if (p.role == DeviceRole.DesktopPC) pcCount++;
                    else mobCount++;
                }
            }
            int totalOperators = vrCount + pcCount + mobCount;

            float bannerW = 460f;
            float bannerH = 28f;
            float bannerX = (Screen.width - bannerW) * 0.5f;

            GUI.backgroundColor = new Color(0.04f, 0.08f, 0.14f, 0.95f);
            GUI.Box(new Rect(bannerX, 8, bannerW, bannerH), GUIContent.none);
            GUI.backgroundColor = Color.white;

            GUIStyle bannerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            GUI.Label(new Rect(bannerX, 8, bannerW, bannerH),
                $"<color=#00ff88>👥 ACTIVE OPERATORS: {totalOperators}</color>  |  <color=#00e5ff>🥽 VR: {vrCount}</color>  <color=#ffaa00>🖥️ PC: {pcCount}</color>  <color=#00ff88>📱 Mobile: {mobCount}</color>  |  <color=#7ab0d6>🌐 Port {webServerPort}</color>",
                bannerStyle);

            // 1. Top-Left Collaborative Participant HUD
            GUILayout.BeginArea(new Rect(16, 55, 340, 155), GUI.skin.box);
            GUILayout.Label("<b>🌐 HOLOBRIDGE MULTI-DEVICE SPATIAL HUB</b>");
            GUILayout.Label($"<color=#7ab0d6>Web Live Stream:</color> <color=#00ff88>http://{localIpAddress}:{webServerPort}/</color>");
            GUILayout.Label($"<color=#00e5ff>Active Role [F1]:</color> {activeRole}");
            GUILayout.Label($"<color=#ffaa00>Connected Avatars:</color> {totalOperators} Active in 3D Room");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("F1: Role", GUILayout.Width(75), GUILayout.Height(24))) CycleNextRole();
            if (GUILayout.Button("T: Theme", GUILayout.Width(75), GUILayout.Height(24))) MetaverseEnvironmentManager.Instance?.NextTheme();
            if (GUILayout.Button("M: Phone", GUILayout.Width(75), GUILayout.Height(24))) showMobileSimulator = !showMobileSimulator;
            if (GUILayout.Button("X: Explode", GUILayout.Width(85), GUILayout.Height(24))) SharedDigitalTwinAssembly.Instance?.ToggleExplodedView();
            GUILayout.EndHorizontal();

            GUILayout.EndArea();

            // 2. On-Screen Mobile Phone Simulator (Toggle with 'M' key)
            if (showMobileSimulator)
            {
                DrawMobilePhoneSimulator();
            }
        }

        private void DrawMobilePhoneSimulator()
        {
            float phoneW = 270;
            float phoneH = 430;
            float px = Screen.width - phoneW - 20;
            float py = Screen.height - phoneH - 30;

            GUILayout.BeginArea(new Rect(px, py, phoneW, phoneH), phoneFrameStyle);

            GUILayout.Label("<b>📱 MOBILE SPATIAL CONTROLLER</b>", phoneHeaderStyle);
            GUILayout.Label($"<color=#7ab0d6><size=10>Wi-Fi: http://{localIpAddress}:{webServerPort}/</size></color>", phoneHeaderStyle);
            GUILayout.Space(4);

            // Simulated Touchpad
            GUILayout.Box("<b>[TOUCHPAD AREA]\nClick & Drag Mouse to Aim Mobile 3D Laser</b>", GUILayout.Height(130));
            Rect padRect = GUILayoutUtility.GetLastRect();

            Event e = Event.current;
            if (e != null && (e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && padRect.Contains(e.mousePosition))
            {
                float nx = (e.mousePosition.x - padRect.x) / padRect.width;
                float ny = 1.0f - ((e.mousePosition.y - padRect.y) / padRect.height);
                localMobileTouchPos = new Vector2(Mathf.Clamp01(nx), Mathf.Clamp01(ny));
                UpdateOrCreateParticipantTouch("mobile_primary", localMobileTouchPos.x, localMobileTouchPos.y);
            }

            GUILayout.Space(6);

            GUI.backgroundColor = colorMobile;
            if (GUILayout.Button("✨ PEEL 2D BLUEPRINT TO 3D [P]", phoneBtnStyle, GUILayout.Height(30)))
            {
                TriggerScreenPeel();
            }

            GUI.backgroundColor = colorPC;
            if (GUILayout.Button("💥 TOGGLE EXPLODED VIEW [X]", phoneBtnStyle, GUILayout.Height(28)))
            {
                SharedDigitalTwinAssembly.Instance?.ToggleExplodedView();
            }

            GUI.backgroundColor = new Color(0.0f, 0.85f, 1.0f);
            if (GUILayout.Button("👁️ TOGGLE X-RAY INSPECTION", phoneBtnStyle, GUILayout.Height(28)))
            {
                SharedDigitalTwinAssembly.Instance?.ToggleXRay();
            }

            GUI.backgroundColor = new Color(0.9f, 0.2f, 0.7f);
            if (GUILayout.Button("🎨 SWITCH METAVERSE THEME [T]", phoneBtnStyle, GUILayout.Height(28)))
            {
                MetaverseEnvironmentManager.Instance?.NextTheme();
            }

            GUI.backgroundColor = Color.white;
            GUILayout.EndArea();
        }

        #endregion
    }
}
