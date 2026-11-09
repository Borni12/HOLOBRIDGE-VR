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

        // Multi-Participant 3D Avatars with Complete Human Anatomy & Procedural Locomotion
        public class CollaboratorAvatar
        {
            public string clientId;
            public string id => clientId;
            public string displayName;
            public Color teamColor;
            public DeviceRole role;
            public GameObject rootObj;

            // Core Anatomy Transforms
            public Transform pelvisTransform;
            public Transform torsoTransform;
            public Transform chestTransform;
            public Transform headTransform;

            // Limbs & Joints - Arms
            public Transform leftShoulderPivot;
            public Transform leftUpperArm;
            public Transform leftElbowPivot;
            public Transform leftForearm;
            public Transform leftHandTransform;

            public Transform rightShoulderPivot;
            public Transform rightUpperArm;
            public Transform rightElbowPivot;
            public Transform rightForearm;
            public Transform rightHandTransform;

            // Limbs & Joints - Legs
            public Transform leftHipPivot;
            public Transform leftThigh;
            public Transform leftKneePivot;
            public Transform leftCalf;
            public Transform leftFoot;

            public Transform rightHipPivot;
            public Transform rightThigh;
            public Transform rightKneePivot;
            public Transform rightCalf;
            public Transform rightFoot;

            // Laser Beam & Reticle
            public LineRenderer laserBeam;
            public Transform laserReticle;
            public TextMesh nameTagTextMesh;
            public Vector2 touchCoords = new Vector2(0.5f, 0.5f);
            public Vector3 currentTargetPoint;

            // Procedural Human Locomotion
            public float walkPhase;
            public float walkSpeed;
            public Vector3 lastPosition;
            public Vector3 moveVelocity;
            public bool isWalking;
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

        public enum PlayerCameraMode
        {
            FirstPerson,
            ThirdPerson,
            BirdsEye,
            ArenaOverview
        }

        [Header("Camera Vantage & Perspectives")]
        public PlayerCameraMode currentCameraMode = PlayerCameraMode.FirstPerson;
        private bool isLocalJoined = false;

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
            gameObject.name = "[HOLOBRIDGE_CROSS_DEVICE_HUB]";
            DontDestroyOnLoad(gameObject);

            FindLocalIP();
            SetupLiveStreamCamera();
            Build3DDeviceAvatars();
        }

        private void Start()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            enableLocalWebServer = false;
#endif
            if (enableLocalWebServer)
            {
                StartLocalWebServer();
            }
            EnsureLocalOperatorJoined();
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

        [Header("Active Stream Perspective")]
        [SerializeField] private string streamTargetUserId = "main"; // "main", "arena", or a specific user's clientId
        public string StreamTargetUserId => streamTargetUserId;

        public void SetStreamTarget(string targetId)
        {
            if (string.IsNullOrEmpty(targetId)) targetId = "main";
            streamTargetUserId = targetId;

            string targetName = targetId switch
            {
                "birds_eye" => "🦅 Tactical Bird's Eye View",
                "topdown" => "🦅 Tactical Bird's Eye View",
                "arena" => "🌐 Overview Arena Cam",
                "main" => "🖥️ Host Main Cam",
                _ => participants.TryGetValue(targetId, out var av) ? av.displayName : targetId
            };

            OmniOSManager.Instance?.ShowNotificationToast($"📹 Stream Perspective: {targetName}");
            SpatialAudioEngine.Instance?.PlayHoverTick();
        }

        private void UpdateStreamCameraPerspective()
        {
            if (streamCamera == null) return;

            Camera mainCam = Camera.main;

            if (streamTargetUserId == "birds_eye" || streamTargetUserId == "topdown")
            {
                // Overhead Tactical Bird's-Eye View looking down over entire room and all participants
                Vector3 birdsEyePos = new Vector3(0f, 6.8f, -0.6f);
                Vector3 birdsEyeLook = new Vector3(0f, 0.7f, 0.8f);
                streamCamera.transform.position = Vector3.Lerp(streamCamera.transform.position, birdsEyePos, Time.deltaTime * 6f);
                streamCamera.transform.rotation = Quaternion.Slerp(streamCamera.transform.rotation, Quaternion.LookRotation(birdsEyeLook - streamCamera.transform.position), Time.deltaTime * 6f);
                streamCamera.fieldOfView = 75f;
                return;
            }

            if (streamTargetUserId == "arena")
            {
                // Cinematic Wide-Angle Arena Cam
                Vector3 arenaCamPos = new Vector3(0f, 3.2f, -2.85f);
                Vector3 arenaLookTarget = new Vector3(0f, 1.30f, 0f);
                streamCamera.transform.position = Vector3.Lerp(streamCamera.transform.position, arenaCamPos, Time.deltaTime * 6f);
                streamCamera.transform.rotation = Quaternion.Slerp(streamCamera.transform.rotation, Quaternion.LookRotation((arenaLookTarget - streamCamera.transform.position).normalized), Time.deltaTime * 6f);
                streamCamera.fieldOfView = 65f;
                return;
            }

            // Check if targeting a specific participant avatar
            if (!string.IsNullOrEmpty(streamTargetUserId) && streamTargetUserId != "main")
            {
                lock (participantsLock)
                {
                    if (participants.TryGetValue(streamTargetUserId, out CollaboratorAvatar targetAv) && targetAv != null && targetAv.rootObj != null)
                    {
                        // Over-the-shoulder third-person camera looking past avatar towards their aim point
                        Vector3 rootPos = targetAv.rootObj.transform.position;
                        Vector3 shoulderOffset = targetAv.rootObj.transform.up * 1.55f 
                                               - targetAv.rootObj.transform.forward * 0.95f 
                                               + targetAv.rootObj.transform.right * 0.35f;
                        Vector3 desiredPos = rootPos + shoulderOffset;
                        Vector3 lookTarget = targetAv.currentTargetPoint != Vector3.zero 
                                           ? targetAv.currentTargetPoint 
                                           : (rootPos + targetAv.rootObj.transform.forward * 5f + Vector3.up * 1.3f);

                        streamCamera.transform.position = Vector3.Lerp(streamCamera.transform.position, desiredPos, Time.deltaTime * 8f);
                        Vector3 toLook = (lookTarget - streamCamera.transform.position).normalized;
                        if (toLook.sqrMagnitude > 0.001f)
                        {
                            streamCamera.transform.rotation = Quaternion.Slerp(streamCamera.transform.rotation, Quaternion.LookRotation(toLook), Time.deltaTime * 8f);
                        }
                        streamCamera.fieldOfView = 62f;
                        return;
                    }
                }
            }

            // Default: Mirror Main Host Camera
            if (mainCam != null)
            {
                streamCamera.transform.position = mainCam.transform.position;
                streamCamera.transform.rotation = mainCam.transform.rotation;
                streamCamera.fieldOfView = mainCam.fieldOfView;
            }
        }

        private void LateUpdate()
        {
            UpdatePlayerCameraView();
            UpdateStreamCameraPerspective();

#if UNITY_WEBGL && !UNITY_EDITOR
            return;
#endif

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

            lock (participantsLock)
            {
                participants.Clear();
            }

            // In-World 3D Live Collaborator Scoreboard (Active User Count starts strictly at 0)
            BuildInWorldCollaboratorBoard();
        }

        public string GetLocalOperatorId()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return "web_local";
#elif UNITY_ANDROID || UNITY_IOS
            return "vr_local";
#else
            return activeRole == DeviceRole.VRHeadset ? "vr_local" : "pc_local";
#endif
        }

        public void EnsureLocalOperatorJoined()
        {
            string localId = GetLocalOperatorId();
            lock (participantsLock)
            {
                if (participants.ContainsKey(localId)) return;
            }

            string localName = "🖥️ PC Inspector";
            Color localColor = colorPC;
            DeviceRole localRole = DeviceRole.DesktopPC;
            Vector3 spawnPos = new Vector3(0.9f, 0f, -1.1f);
            Quaternion spawnRot = Quaternion.Euler(0f, -25f, 0f);

#if UNITY_WEBGL && !UNITY_EDITOR
            localName = "🌐 Web Operator";
            localColor = new Color(0.0f, 0.95f, 0.75f);
            localRole = DeviceRole.DesktopPC;
            spawnPos = new Vector3(0f, 0f, -1.25f);
            spawnRot = Quaternion.identity;
#elif UNITY_ANDROID || UNITY_IOS
            localName = "🥽 VR Lead Operator";
            localColor = colorVR;
            localRole = DeviceRole.VRHeadset;
            spawnPos = new Vector3(-0.9f, 0f, -1.1f);
            spawnRot = Quaternion.Euler(0f, 25f, 0f);
#else
            if (activeRole == DeviceRole.VRHeadset)
            {
                localName = "🥽 VR Lead Operator";
                localColor = colorVR;
                localRole = DeviceRole.VRHeadset;
                spawnPos = new Vector3(-0.9f, 0f, -1.1f);
                spawnRot = Quaternion.Euler(0f, 25f, 0f);
            }
            else
            {
                localName = "🖥️ PC Inspector";
                localColor = colorPC;
                localRole = DeviceRole.DesktopPC;
                spawnPos = new Vector3(0.9f, 0f, -1.1f);
                spawnRot = Quaternion.Euler(0f, -25f, 0f);
            }
#endif

            var av = CreateHumanoidAvatar(localId, localName, localColor, localRole, spawnPos, spawnRot);
            lock (participantsLock)
            {
                participants[localId] = av;
            }
            if (localRole == DeviceRole.VRHeadset) vrAvatar = av;
            else pcAvatar = av;

            isLocalJoined = true;
            OmniOSManager.Instance?.ShowNotificationToast($"👥 Joined Scene: {localName} (Articulated Humanoid Avatar Active)!");
        }

        public void JoinAsLocalOperator()
        {
            EnsureLocalOperatorJoined();
            SpatialAudioEngine.Instance?.PlayThemeChime();
        }

        public void SetCameraMode(PlayerCameraMode mode)
        {
            currentCameraMode = mode;
            string modeName = currentCameraMode switch
            {
                PlayerCameraMode.BirdsEye => "🦅 BIRD'S EYE VIEW (High Overhead View)",
                PlayerCameraMode.ThirdPerson => "👤 THIRD-PERSON VIEW (Avatar Tracking)",
                PlayerCameraMode.ArenaOverview => "🌐 ARENA OVERVIEW (Cinematic Cam)",
                _ => "🖥️ FIRST-PERSON VIEW (Eye-Level)"
            };

            OmniOSManager.Instance?.ShowNotificationToast($"📹 View: {modeName}");
            SpatialAudioEngine.Instance?.PlayHoverTick();
        }

        public void ToggleBirdsEyeView()
        {
            if (currentCameraMode == PlayerCameraMode.BirdsEye)
            {
                SetCameraMode(PlayerCameraMode.FirstPerson);
            }
            else
            {
                SetCameraMode(PlayerCameraMode.BirdsEye);
            }
        }

        public void ToggleThirdPersonView()
        {
            if (currentCameraMode == PlayerCameraMode.ThirdPerson)
            {
                SetCameraMode(PlayerCameraMode.FirstPerson);
            }
            else
            {
                SetCameraMode(PlayerCameraMode.ThirdPerson);
            }
        }

        private void UpdatePlayerCameraView()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var all = Camera.allCameras;
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] != streamCamera && all[i].targetTexture == null)
                    {
                        cam = all[i];
                        break;
                    }
                }
            }
            if (cam == null) return;

            string localId = GetLocalOperatorId();
            CollaboratorAvatar localAv = null;
            lock (participantsLock)
            {
                participants.TryGetValue(localId, out localAv);
                if (localAv == null || localAv.rootObj == null)
                {
                    localAv = pcAvatar ?? vrAvatar;
                }
                if (localAv == null || localAv.rootObj == null)
                {
                    foreach (var p in participants.Values)
                    {
                        if (p != null && p.rootObj != null)
                        {
                            localAv = p;
                            break;
                        }
                    }
                }
            }

            Vector3 focusPos = (localAv != null && localAv.rootObj != null) ? localAv.rootObj.transform.position : Vector3.zero;
            Vector3 fwd = (localAv != null && localAv.rootObj != null) ? localAv.rootObj.transform.forward : Vector3.forward;

            if (currentCameraMode == PlayerCameraMode.BirdsEye)
            {
                // Dynamic Bird's Eye View: Tracks player from high altitude (Y = 7.5m) looking down
                Vector3 birdsEyePos = focusPos + Vector3.up * 7.5f - Vector3.forward * 1.6f;
                Vector3 birdsEyeLook = focusPos + Vector3.up * 0.4f;
                cam.transform.position = Vector3.Lerp(cam.transform.position, birdsEyePos, Time.deltaTime * 8f);
                Vector3 lookDir = (birdsEyeLook - cam.transform.position).normalized;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 8f);
                }
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 75f, Time.deltaTime * 6f);
            }
            else if (currentCameraMode == PlayerCameraMode.ThirdPerson)
            {
                // Dynamic Third-Person View: Follows right behind the avatar
                Vector3 desiredPos = focusPos - fwd * 2.6f + Vector3.up * 1.85f;
                Vector3 lookTarget = focusPos + Vector3.up * 1.30f;

                cam.transform.position = Vector3.Lerp(cam.transform.position, desiredPos, Time.deltaTime * 9f);
                Vector3 lookDir = (lookTarget - cam.transform.position).normalized;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 9f);
                }
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 65f, Time.deltaTime * 6f);
            }
            else if (currentCameraMode == PlayerCameraMode.ArenaOverview)
            {
                // Static/Cinematic Arena Overview
                Vector3 arenaPos = new Vector3(0f, 3.8f, -3.4f);
                Vector3 arenaLook = new Vector3(0f, 1.2f, 0f);
                cam.transform.position = Vector3.Lerp(cam.transform.position, arenaPos, Time.deltaTime * 6f);
                cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation(arenaLook - cam.transform.position), Time.deltaTime * 6f);
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 68f, Time.deltaTime * 6f);
            }
            else if (currentCameraMode == PlayerCameraMode.FirstPerson)
            {
                // First-Person Eye Level View
                Vector3 eyePos = focusPos + Vector3.up * 1.62f;
                cam.transform.position = Vector3.Lerp(cam.transform.position, eyePos, Time.deltaTime * 8f);
                if (fwd.sqrMagnitude > 0.001f)
                {
                    cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation(fwd), Time.deltaTime * 8f);
                }
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 60f, Time.deltaTime * 6f);
            }
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

            // Title TextMesh (Starts strictly at 0)
            GameObject titleObj = new GameObject("TitleLabel");
            titleObj.transform.SetParent(collaboratorBoardRoot.transform, false);
            titleObj.transform.localPosition = new Vector3(0f, 0.09f, -0.015f);
            titleObj.transform.localScale = Vector3.one * 0.015f;

            boardTitleTextMesh = titleObj.AddComponent<TextMesh>();
            boardTitleTextMesh.text = "👥 ACTIVE OPERATORS: 0";
            boardTitleTextMesh.fontSize = 26;
            boardTitleTextMesh.characterSize = 1f;
            boardTitleTextMesh.anchor = TextAnchor.MiddleCenter;
            boardTitleTextMesh.alignment = TextAlignment.Center;
            boardTitleTextMesh.fontStyle = FontStyle.Bold;
            boardTitleTextMesh.color = new Color(0.35f, 0.75f, 1.0f);

            // Details Breakdown TextMesh
            GameObject detailObj = new GameObject("DetailLabel");
            detailObj.transform.SetParent(collaboratorBoardRoot.transform, false);
            detailObj.transform.localPosition = new Vector3(0f, -0.09f, -0.015f);
            detailObj.transform.localScale = Vector3.one * 0.012f;

            boardDetailsTextMesh = detailObj.AddComponent<TextMesh>();
            boardDetailsTextMesh.text = "[ Waiting for operators to connect... ]";
            boardDetailsTextMesh.fontSize = 19;
            boardDetailsTextMesh.characterSize = 1f;
            boardDetailsTextMesh.anchor = TextAnchor.MiddleCenter;
            boardDetailsTextMesh.alignment = TextAlignment.Center;
            boardDetailsTextMesh.color = new Color(0.65f, 0.85f, 1.0f);
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

            Material skinMat = new Material(litShader);
            skinMat.color = new Color(0.92f, 0.76f, 0.65f);
            if (skinMat.HasProperty("_BaseColor")) skinMat.SetColor("_BaseColor", new Color(0.92f, 0.76f, 0.65f));

            Material armorMat = new Material(litShader);
            armorMat.color = new Color(0.18f, 0.22f, 0.28f);
            if (armorMat.HasProperty("_BaseColor")) armorMat.SetColor("_BaseColor", new Color(0.18f, 0.22f, 0.28f));

            Material neonMat = new Material(unlitShader) { color = themeColor };

            // -------------------------------------------------------------
            // 1. PELVIS & HIPS (Center Root of Locomotion)
            // -------------------------------------------------------------
            GameObject pelvis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pelvis.name = "Pelvis";
            pelvis.transform.SetParent(root.transform, false);
            pelvis.transform.localPosition = new Vector3(0f, 0.92f, 0f);
            pelvis.transform.localScale = new Vector3(0.30f, 0.15f, 0.18f);
            RemoveCollider(pelvis);
            pelvis.GetComponent<MeshRenderer>().material = suitMat;

            // Glowing Belt Trim
            GameObject belt = GameObject.CreatePrimitive(PrimitiveType.Cube);
            belt.name = "BeltGlow";
            belt.transform.SetParent(pelvis.transform, false);
            belt.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            belt.transform.localScale = new Vector3(1.04f, 0.25f, 1.05f);
            RemoveCollider(belt);
            belt.GetComponent<MeshRenderer>().material = neonMat;

            // -------------------------------------------------------------
            // 2. TORSO, CHEST & PAULDRONS (Spine Chain)
            // -------------------------------------------------------------
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torso.name = "Torso";
            torso.transform.SetParent(pelvis.transform, false);
            torso.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            torso.transform.localScale = new Vector3(0.32f, 0.24f, 0.20f);
            RemoveCollider(torso);
            torso.GetComponent<MeshRenderer>().material = suitMat;

            GameObject chest = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chest.name = "Chest";
            chest.transform.SetParent(torso.transform, false);
            chest.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            chest.transform.localScale = new Vector3(1.12f, 1.05f, 1.10f);
            RemoveCollider(chest);
            chest.GetComponent<MeshRenderer>().material = armorMat;

            // Chest Emblem
            GameObject emblem = GameObject.CreatePrimitive(PrimitiveType.Cube);
            emblem.name = "ChestEmblem";
            emblem.transform.SetParent(chest.transform, false);
            emblem.transform.localPosition = new Vector3(0f, 0.05f, 0.52f);
            emblem.transform.localScale = new Vector3(0.35f, 0.25f, 0.08f);
            RemoveCollider(emblem);
            emblem.GetComponent<MeshRenderer>().material = neonMat;

            // Shoulder Pauldrons
            for (int s = -1; s <= 1; s += 2)
            {
                GameObject pauldron = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pauldron.name = s < 0 ? "Pauldron_L" : "Pauldron_R";
                pauldron.transform.SetParent(chest.transform, false);
                pauldron.transform.localPosition = new Vector3(s * 0.58f, 0.35f, 0f);
                pauldron.transform.localScale = new Vector3(0.35f, 0.35f, 0.40f);
                RemoveCollider(pauldron);
                pauldron.GetComponent<MeshRenderer>().material = neonMat;
            }

            // -------------------------------------------------------------
            // 3. NECK & HEAD (With Cyber Visor & Stylized Face)
            // -------------------------------------------------------------
            GameObject neck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            neck.name = "Neck";
            neck.transform.SetParent(chest.transform, false);
            neck.transform.localPosition = new Vector3(0f, 0.58f, 0f);
            neck.transform.localScale = new Vector3(0.25f, 0.12f, 0.25f);
            RemoveCollider(neck);
            neck.GetComponent<MeshRenderer>().material = skinMat;

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(neck.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.25f, 0.05f);
            head.transform.localScale = new Vector3(2.2f, 2.3f, 2.2f);
            RemoveCollider(head);
            head.GetComponent<MeshRenderer>().material = skinMat;

            // Cyber Visor / Expressive Eyes
            GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "VisorEyes";
            visor.transform.SetParent(head.transform, false);
            visor.transform.localPosition = new Vector3(0f, 0.05f, 0.44f);
            visor.transform.localScale = new Vector3(0.72f, 0.24f, 0.20f);
            RemoveCollider(visor);
            visor.GetComponent<MeshRenderer>().material = neonMat;

            // Hair / Helmet Cap
            GameObject hair = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hair.name = "CyberCap";
            hair.transform.SetParent(head.transform, false);
            hair.transform.localPosition = new Vector3(0f, 0.15f, -0.05f);
            hair.transform.localScale = new Vector3(1.02f, 0.90f, 1.05f);
            RemoveCollider(hair);
            hair.GetComponent<MeshRenderer>().material = armorMat;

            // Ear Communicators
            for (int e = -1; e <= 1; e += 2)
            {
                GameObject ear = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ear.name = e < 0 ? "Ear_L" : "Ear_R";
                ear.transform.SetParent(head.transform, false);
                ear.transform.localPosition = new Vector3(e * 0.50f, 0.05f, 0f);
                ear.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                ear.transform.localScale = new Vector3(0.16f, 0.08f, 0.16f);
                RemoveCollider(ear);
                ear.GetComponent<MeshRenderer>().material = neonMat;
            }

            // -------------------------------------------------------------
            // 4. LEGS & FEET (Biomechanical Walking Limbs)
            // -------------------------------------------------------------
            // LEFT LEG
            GameObject hipL = new GameObject("Hip_L");
            hipL.transform.SetParent(pelvis.transform, false);
            hipL.transform.localPosition = new Vector3(-0.10f, -0.06f, 0f);

            GameObject thighL = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            thighL.name = "Thigh_L";
            thighL.transform.SetParent(hipL.transform, false);
            thighL.transform.localPosition = new Vector3(0f, -0.20f, 0f);
            thighL.transform.localScale = new Vector3(0.12f, 0.20f, 0.12f);
            RemoveCollider(thighL);
            thighL.GetComponent<MeshRenderer>().material = suitMat;

            GameObject kneeL = new GameObject("Knee_L");
            kneeL.transform.SetParent(hipL.transform, false);
            kneeL.transform.localPosition = new Vector3(0f, -0.40f, 0f);

            GameObject calfL = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            calfL.name = "Calf_L";
            calfL.transform.SetParent(kneeL.transform, false);
            calfL.transform.localPosition = new Vector3(0f, -0.20f, 0f);
            calfL.transform.localScale = new Vector3(0.10f, 0.20f, 0.10f);
            RemoveCollider(calfL);
            calfL.GetComponent<MeshRenderer>().material = suitMat;

            GameObject footL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            footL.name = "Boot_L";
            footL.transform.SetParent(kneeL.transform, false);
            footL.transform.localPosition = new Vector3(0f, -0.42f, 0.06f);
            footL.transform.localScale = new Vector3(0.11f, 0.07f, 0.22f);
            RemoveCollider(footL);
            footL.GetComponent<MeshRenderer>().material = armorMat;

            // RIGHT LEG
            GameObject hipR = new GameObject("Hip_R");
            hipR.transform.SetParent(pelvis.transform, false);
            hipR.transform.localPosition = new Vector3(0.10f, -0.06f, 0f);

            GameObject thighR = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            thighR.name = "Thigh_R";
            thighR.transform.SetParent(hipR.transform, false);
            thighR.transform.localPosition = new Vector3(0f, -0.20f, 0f);
            thighR.transform.localScale = new Vector3(0.12f, 0.20f, 0.12f);
            RemoveCollider(thighR);
            thighR.GetComponent<MeshRenderer>().material = suitMat;

            GameObject kneeR = new GameObject("Knee_R");
            kneeR.transform.SetParent(hipR.transform, false);
            kneeR.transform.localPosition = new Vector3(0f, -0.40f, 0f);

            GameObject calfR = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            calfR.name = "Calf_R";
            calfR.transform.SetParent(kneeR.transform, false);
            calfR.transform.localPosition = new Vector3(0f, -0.20f, 0f);
            calfR.transform.localScale = new Vector3(0.10f, 0.20f, 0.10f);
            RemoveCollider(calfR);
            calfR.GetComponent<MeshRenderer>().material = suitMat;

            GameObject footR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            footR.name = "Boot_R";
            footR.transform.SetParent(kneeR.transform, false);
            footR.transform.localPosition = new Vector3(0f, -0.42f, 0.06f);
            footR.transform.localScale = new Vector3(0.11f, 0.07f, 0.22f);
            RemoveCollider(footR);
            footR.GetComponent<MeshRenderer>().material = armorMat;

            // -------------------------------------------------------------
            // 5. LEFT ARM & HELD TOOL (Smartphone / Console)
            // -------------------------------------------------------------
            GameObject shoulderL = new GameObject("Shoulder_L");
            shoulderL.transform.SetParent(chest.transform, false);
            shoulderL.transform.localPosition = new Vector3(-0.24f, 0.12f, 0f);

            GameObject upperArmL = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            upperArmL.name = "UpperArm_L";
            upperArmL.transform.SetParent(shoulderL.transform, false);
            upperArmL.transform.localPosition = new Vector3(0f, -0.15f, 0f);
            upperArmL.transform.localScale = new Vector3(0.09f, 0.15f, 0.09f);
            RemoveCollider(upperArmL);
            upperArmL.GetComponent<MeshRenderer>().material = suitMat;

            GameObject elbowL = new GameObject("Elbow_L");
            elbowL.transform.SetParent(shoulderL.transform, false);
            elbowL.transform.localPosition = new Vector3(0f, -0.30f, 0f);

            GameObject forearmL = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            forearmL.name = "Forearm_L";
            forearmL.transform.SetParent(elbowL.transform, false);
            forearmL.transform.localPosition = new Vector3(0f, -0.12f, 0f);
            forearmL.transform.localScale = new Vector3(0.08f, 0.13f, 0.08f);
            RemoveCollider(forearmL);
            forearmL.GetComponent<MeshRenderer>().material = suitMat;

            GameObject handL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            handL.name = "Hand_Left";
            handL.transform.SetParent(elbowL.transform, false);
            handL.transform.localPosition = new Vector3(0f, -0.25f, 0.02f);
            handL.transform.localScale = Vector3.one * 0.085f;
            RemoveCollider(handL);
            handL.GetComponent<MeshRenderer>().material = skinMat;

            if (role == DeviceRole.MobileTablet)
            {
                GameObject phone = GameObject.CreatePrimitive(PrimitiveType.Cube);
                phone.name = "Smartphone";
                phone.transform.SetParent(handL.transform, false);
                phone.transform.localPosition = new Vector3(0f, 0.04f, 0.05f);
                phone.transform.localScale = new Vector3(1.2f, 2.2f, 0.15f);
                phone.transform.localRotation = Quaternion.Euler(-30f, 15f, 0f);
                RemoveCollider(phone);
                phone.GetComponent<MeshRenderer>().material = neonMat;
            }
            else if (role == DeviceRole.DesktopPC)
            {
                GameObject console = GameObject.CreatePrimitive(PrimitiveType.Cube);
                console.name = "CADConsole";
                console.transform.SetParent(handL.transform, false);
                console.transform.localPosition = new Vector3(0.12f, 0.08f, 0.15f);
                console.transform.localScale = new Vector3(2.8f, 0.2f, 1.6f);
                RemoveCollider(console);
                console.GetComponent<MeshRenderer>().material = neonMat;
            }

            // -------------------------------------------------------------
            // 6. RIGHT ARM & OPTICAL LASER RAY
            // -------------------------------------------------------------
            GameObject shoulderR = new GameObject("Shoulder_R");
            shoulderR.transform.SetParent(chest.transform, false);
            shoulderR.transform.localPosition = new Vector3(0.24f, 0.12f, 0f);

            GameObject upperArmR = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            upperArmR.name = "UpperArm_R";
            upperArmR.transform.SetParent(shoulderR.transform, false);
            upperArmR.transform.localPosition = new Vector3(0f, -0.15f, 0f);
            upperArmR.transform.localScale = new Vector3(0.09f, 0.15f, 0.09f);
            RemoveCollider(upperArmR);
            upperArmR.GetComponent<MeshRenderer>().material = suitMat;

            GameObject elbowR = new GameObject("Elbow_R");
            elbowR.transform.SetParent(shoulderR.transform, false);
            elbowR.transform.localPosition = new Vector3(0f, -0.30f, 0f);

            GameObject forearmR = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            forearmR.name = "Forearm_R";
            forearmR.transform.SetParent(elbowR.transform, false);
            forearmR.transform.localPosition = new Vector3(0f, -0.12f, 0f);
            forearmR.transform.localScale = new Vector3(0.08f, 0.13f, 0.08f);
            RemoveCollider(forearmR);
            forearmR.GetComponent<MeshRenderer>().material = suitMat;

            GameObject handR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            handR.name = "Hand_Right";
            handR.transform.SetParent(elbowR.transform, false);
            handR.transform.localPosition = new Vector3(0f, -0.25f, 0.02f);
            handR.transform.localScale = Vector3.one * 0.085f;
            RemoveCollider(handR);
            handR.GetComponent<MeshRenderer>().material = skinMat;

            // Optical Laser LineRenderer
            GameObject laserObj = new GameObject("LaserBeam");
            laserObj.transform.SetParent(handR.transform, false);
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

            // -------------------------------------------------------------
            // 7. 3D FLOATING NAME BADGE (At Y = 1.95m above head)
            // -------------------------------------------------------------
            GameObject badgeObj = new GameObject("NameBadge");
            badgeObj.transform.SetParent(root.transform, false);
            badgeObj.transform.localPosition = new Vector3(0f, 1.95f, 0f);

            GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "Plate";
            plate.transform.SetParent(badgeObj.transform, false);
            plate.transform.localScale = new Vector3(0.72f, 0.11f, 0.015f);
            RemoveCollider(plate);
            plate.GetComponent<MeshRenderer>().material = suitMat;

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
            tm.fontStyle = FontStyle.Bold;
            tm.color = themeColor;

            var avatar = new CollaboratorAvatar
            {
                clientId = clientId,
                displayName = name,
                teamColor = themeColor,
                role = role,
                rootObj = root,

                pelvisTransform = pelvis.transform,
                torsoTransform = torso.transform,
                chestTransform = chest.transform,
                headTransform = head.transform,

                leftShoulderPivot = shoulderL.transform,
                leftUpperArm = upperArmL.transform,
                leftElbowPivot = elbowL.transform,
                leftForearm = forearmL.transform,
                leftHandTransform = handL.transform,

                rightShoulderPivot = shoulderR.transform,
                rightUpperArm = upperArmR.transform,
                rightElbowPivot = elbowR.transform,
                rightForearm = forearmR.transform,
                rightHandTransform = handR.transform,

                leftHipPivot = hipL.transform,
                leftThigh = thighL.transform,
                leftKneePivot = kneeL.transform,
                leftCalf = calfL.transform,
                leftFoot = footL.transform,

                rightHipPivot = hipR.transform,
                rightThigh = thighR.transform,
                rightKneePivot = kneeR.transform,
                rightCalf = calfR.transform,
                rightFoot = footR.transform,

                laserBeam = lr,
                laserReticle = reticle.transform,
                nameTagTextMesh = tm,
                lastPosition = pos,
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

            // WASD Navigation in Third-Person or Bird's-Eye View
            if (currentCameraMode == PlayerCameraMode.ThirdPerson || currentCameraMode == PlayerCameraMode.BirdsEye)
            {
                float h = Input.GetAxisRaw("Horizontal");
                float v = Input.GetAxisRaw("Vertical");
                if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
                {
                    MoveLocalPlayer(h * 3.2f * Time.deltaTime, v * 3.2f * Time.deltaTime);
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

            // 'B' Key: Toggle Tactical Bird's-Eye View (🦅 বার্ডস আই ভিউ)
            if (Input.GetKeyDown(KeyCode.B) && !OmniOS.Apps.MediaHub.OmniAppMediaHub.IsTypingActive)
            {
                ToggleBirdsEyeView();
            }

            // 'V' Key: Toggle Third-Person View (Over-the-shoulder avatar follow)
            if (Input.GetKeyDown(KeyCode.V) && !OmniOS.Apps.MediaHub.OmniAppMediaHub.IsTypingActive)
            {
                ToggleThirdPersonView();
            }

            // 'F' Key: Switch to First-Person View
            if (Input.GetKeyDown(KeyCode.F) && !OmniOS.Apps.MediaHub.OmniAppMediaHub.IsTypingActive)
            {
                SetCameraMode(PlayerCameraMode.FirstPerson);
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

                    // Sync local avatar to player camera when in First Person
                    if (cam != null && currentCameraMode == PlayerCameraMode.FirstPerson && (av.id == GetLocalOperatorId() || av.id == "vr_local" || av.id == "pc_local" || av.id == "web_local"))
                    {
                        Vector3 camGround = new Vector3(cam.transform.position.x, 0f, cam.transform.position.z);
                        float dist = Vector3.Distance(camGround, av.lastPosition);
                        float instSpeed = dist / Mathf.Max(0.0001f, Time.deltaTime);
                        av.walkSpeed = Mathf.Lerp(av.walkSpeed, instSpeed, Time.deltaTime * 8f);
                        av.isWalking = av.walkSpeed > 0.05f;
                        av.rootObj.transform.position = camGround;

                        Vector3 fwd = cam.transform.forward;
                        fwd.y = 0;
                        if (fwd.sqrMagnitude > 0.001f)
                        {
                            av.rootObj.transform.rotation = Quaternion.Slerp(av.rootObj.transform.rotation, Quaternion.LookRotation(fwd.normalized), Time.deltaTime * 12f);
                        }
                        av.lastPosition = camGround;
                    }

                    // 1. Procedural Human Locomotion & Walking Stride (Legs, Knees, Arms, Bobbing)
                    UpdateHumanoidLocomotion(av);

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
                if (totalCount == 0)
                {
                    boardTitleTextMesh.text = "👥 ACTIVE OPERATORS: 0";
                    boardTitleTextMesh.color = new Color(0.35f, 0.75f, 1.0f);
                }
                else
                {
                    boardTitleTextMesh.text = $"👥 ACTIVE OPERATORS: {totalCount}";
                    boardTitleTextMesh.color = new Color(0.0f, 1.0f, 0.55f);
                }
            }
            if (boardDetailsTextMesh != null)
            {
                if (totalCount == 0)
                {
                    boardDetailsTextMesh.text = "[ Waiting for operators to connect... ]";
                    boardDetailsTextMesh.color = new Color(0.65f, 0.85f, 1.0f);
                }
                else
                {
                    boardDetailsTextMesh.text = $"[ 🥽 VR: {vrCount}  |  🖥️ PC: {pcCount}  |  📱 Mobile: {mobCount} ]";
                    boardDetailsTextMesh.color = new Color(0.80f, 0.95f, 1.0f);
                }
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

        public void MoveLocalPlayer(float strafe, float forward)
        {
            Camera cam = Camera.main;
            Transform refTrans = cam != null ? cam.transform : transform;

            Vector3 fwd = refTrans.forward;
            fwd.y = 0;
            if (fwd.sqrMagnitude > 0.001f) fwd.Normalize();

            Vector3 right = refTrans.right;
            right.y = 0;
            if (right.sqrMagnitude > 0.001f) right.Normalize();

            Vector3 moveDelta = (fwd * forward + right * strafe);

            string localId = GetLocalOperatorId();
            lock (participantsLock)
            {
                if (!participants.TryGetValue(localId, out var localAv) || localAv == null || localAv.rootObj == null)
                {
                    localAv = pcAvatar ?? vrAvatar;
                    if (localAv == null || localAv.rootObj == null)
                    {
                        foreach (var p in participants.Values)
                        {
                            if (p != null && p.rootObj != null)
                            {
                                localAv = p;
                                break;
                            }
                        }
                    }
                }

                if (localAv != null && localAv.rootObj != null)
                {
                    localAv.rootObj.transform.position += moveDelta;
                    localAv.walkSpeed = Mathf.Max(localAv.walkSpeed, 1.8f);
                    localAv.isWalking = true;
                    localAv.lastActivityTime = Time.time;
                    if (moveDelta.sqrMagnitude > 0.001f)
                    {
                        localAv.rootObj.transform.rotation = Quaternion.Slerp(localAv.rootObj.transform.rotation, Quaternion.LookRotation(moveDelta.normalized), Time.deltaTime * 10f);
                    }
                }
            }

            if (currentCameraMode == PlayerCameraMode.FirstPerson)
            {
                OmniOS.Interaction.OmniSpatialLocomotion.Instance?.ApplyRemoteMoveDelta(strafe, forward, 0f, 0f);
            }
        }

        #region WebGL Direct JavaScript Messaging Bridge

        /// <summary>
        /// Direct WebGL JavaScript Bridge: Called when player drags 1-finger virtual joystick on web canvas
        /// Format: "dx,dy" or "dx,dy,speed"
        /// </summary>
        public void OnWebTouchMove(string deltaStr)
        {
            if (string.IsNullOrEmpty(deltaStr)) return;
            string[] parts = deltaStr.Split(',');
            if (parts.Length >= 2 &&
                float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float dx) &&
                float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float dy))
            {
                float speed = 2.4f;
                if (parts.Length >= 3 && float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float sp))
                {
                    speed = sp;
                }

                MoveLocalPlayer(dx * speed * Time.deltaTime, dy * speed * Time.deltaTime);
            }
        }

        public void OnWebSetCameraView(string viewMode)
        {
            switch (viewMode?.ToLower())
            {
                case "birds_eye":
                case "topdown":
                case "bird":
                    SetCameraMode(PlayerCameraMode.BirdsEye);
                    break;
                case "third_person":
                case "third":
                case "3rd":
                    SetCameraMode(PlayerCameraMode.ThirdPerson);
                    break;
                case "first_person":
                case "first":
                case "1st":
                    SetCameraMode(PlayerCameraMode.FirstPerson);
                    break;
                case "arena":
                case "overview":
                    SetCameraMode(PlayerCameraMode.ArenaOverview);
                    break;
            }
        }

        public void OnWebTriggerAction(string action)
        {
            switch (action?.ToLower())
            {
                case "theme":
                    MetaverseEnvironmentManager.Instance?.NextTheme();
                    break;
                case "explode":
                    SharedDigitalTwinAssembly.Instance?.ToggleExplodedView();
                    break;
                case "peel":
                    TriggerScreenPeel();
                    break;
                case "role":
                    CycleNextRole();
                    break;
                case "join":
                    EnsureLocalOperatorJoined();
                    break;
            }
        }

        public void OnWebLaserAim(string coords)
        {
            if (string.IsNullOrEmpty(coords)) return;
            string[] parts = coords.Split(',');
            if (parts.Length >= 2 &&
                float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x) &&
                float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y))
            {
                string localId = GetLocalOperatorId();
                lock (participantsLock)
                {
                    if (participants.TryGetValue(localId, out var localAv) && localAv != null)
                    {
                        localAv.touchCoords = new Vector2(x, y);
                    }
                }
            }
        }

        #endregion

        private void UpdateHumanoidLocomotion(CollaboratorAvatar av)
        {
            if (av == null || av.rootObj == null || av.pelvisTransform == null) return;

            float dt = Mathf.Max(0.001f, Time.deltaTime);
            Vector3 curPos = av.rootObj.transform.position;
            Vector3 disp = curPos - av.lastPosition;
            disp.y = 0f;
            float instantSpeed = disp.magnitude / dt;
            av.lastPosition = curPos;

            // Blend speed
            av.walkSpeed = Mathf.Lerp(av.walkSpeed, instantSpeed, dt * 10f);
            av.isWalking = av.walkSpeed > 0.05f;

            if (av.isWalking)
            {
                float freq = Mathf.Clamp(av.walkSpeed * 5.0f, 4.0f, 9.0f);
                av.walkPhase += dt * freq;

                float sin = Mathf.Sin(av.walkPhase);
                float cos = Mathf.Cos(av.walkPhase);

                // 1. Natural Leg Stride (Forward / Backward leg swings)
                float strideAngle = sin * 32f;
                if (av.leftHipPivot != null) av.leftHipPivot.localRotation = Quaternion.Euler(strideAngle, 0f, 0f);
                if (av.rightHipPivot != null) av.rightHipPivot.localRotation = Quaternion.Euler(-strideAngle, 0f, 0f);

                // 2. Realistic Knee Bends (Bends backwards on forward stride lift)
                float leftKnee = Mathf.Max(0f, -sin) * 48f;
                float rightKnee = Mathf.Max(0f, sin) * 48f;
                if (av.leftKneePivot != null) av.leftKneePivot.localRotation = Quaternion.Euler(leftKnee, 0f, 0f);
                if (av.rightKneePivot != null) av.rightKneePivot.localRotation = Quaternion.Euler(rightKnee, 0f, 0f);

                // 3. Counter Arm Swing (Left Arm swings with Right Leg, Right Arm swings with Left Leg)
                float armSwing = -strideAngle * 0.85f;
                if (av.leftShoulderPivot != null)
                {
                    av.leftShoulderPivot.localRotation = Quaternion.Euler(armSwing, 0f, -8f);
                }
                if (av.leftElbowPivot != null)
                {
                    av.leftElbowPivot.localRotation = Quaternion.Euler(-18f + Mathf.Abs(armSwing) * 0.35f, 0f, 0f);
                }

                // Right Arm (Aiming or natural counter-swing)
                if (av.rightShoulderPivot != null && av.rightElbowPivot != null)
                {
                    if (av.currentTargetPoint != Vector3.zero)
                    {
                        Vector3 toTarget = (av.currentTargetPoint - av.rightShoulderPivot.position).normalized;
                        Vector3 localAim = av.rootObj.transform.InverseTransformDirection(toTarget);
                        float pitch = -Mathf.Asin(Mathf.Clamp(localAim.y, -0.85f, 0.85f)) * Mathf.Rad2Deg;
                        float yaw = Mathf.Atan2(localAim.x, localAim.z) * Mathf.Rad2Deg;
                        av.rightShoulderPivot.localRotation = Quaternion.Slerp(av.rightShoulderPivot.localRotation, Quaternion.Euler(pitch, yaw, 0f), dt * 10f);
                        av.rightElbowPivot.localRotation = Quaternion.Slerp(av.rightElbowPivot.localRotation, Quaternion.Euler(-10f, 0f, 0f), dt * 10f);
                    }
                    else
                    {
                        av.rightShoulderPivot.localRotation = Quaternion.Euler(-armSwing, 0f, 8f);
                        av.rightElbowPivot.localRotation = Quaternion.Euler(-18f + Mathf.Abs(armSwing) * 0.35f, 0f, 0f);
                    }
                }

                // 4. Pelvis Vertical Bounce & Torso Twist
                float bounce = Mathf.Abs(sin) * 0.035f;
                av.pelvisTransform.localPosition = new Vector3(0f, 0.92f - bounce, 0f);
                if (av.chestTransform != null)
                {
                    av.chestTransform.localRotation = Quaternion.Euler(0f, sin * 6f, -sin * 2.5f);
                }
            }
            else
            {
                // Standing Idle: Smoothly return joints to resting pose
                if (av.leftHipPivot != null) av.leftHipPivot.localRotation = Quaternion.Slerp(av.leftHipPivot.localRotation, Quaternion.Euler(0f, 0f, -2f), dt * 6f);
                if (av.leftKneePivot != null) av.leftKneePivot.localRotation = Quaternion.Slerp(av.leftKneePivot.localRotation, Quaternion.identity, dt * 6f);
                if (av.rightHipPivot != null) av.rightHipPivot.localRotation = Quaternion.Slerp(av.rightHipPivot.localRotation, Quaternion.Euler(0f, 0f, 2f), dt * 6f);
                if (av.rightKneePivot != null) av.rightKneePivot.localRotation = Quaternion.Slerp(av.rightKneePivot.localRotation, Quaternion.identity, dt * 6f);

                // Natural Breathing Rhythm
                float breath = Mathf.Sin(Time.time * 2.2f + av.animPhase) * 0.010f;
                av.pelvisTransform.localPosition = Vector3.Lerp(av.pelvisTransform.localPosition, new Vector3(0f, 0.92f + breath * 0.5f, 0f), dt * 5f);
                if (av.chestTransform != null)
                {
                    av.chestTransform.localRotation = Quaternion.Slerp(av.chestTransform.localRotation, Quaternion.Euler(breath * 80f, 0f, 0f), dt * 5f);
                }

                // Left Arm holds device naturally at waist/chest
                if (av.leftShoulderPivot != null)
                {
                    av.leftShoulderPivot.localRotation = Quaternion.Slerp(av.leftShoulderPivot.localRotation, Quaternion.Euler(22f, -10f, -8f), dt * 5f);
                }
                if (av.leftElbowPivot != null)
                {
                    av.leftElbowPivot.localRotation = Quaternion.Slerp(av.leftElbowPivot.localRotation, Quaternion.Euler(-45f, 0f, 0f), dt * 5f);
                }

                // Right Arm points forward if aiming, else rests at side
                if (av.rightShoulderPivot != null && av.rightElbowPivot != null)
                {
                    if (av.currentTargetPoint != Vector3.zero)
                    {
                        Vector3 toTarget = (av.currentTargetPoint - av.rightShoulderPivot.position).normalized;
                        Vector3 localAim = av.rootObj.transform.InverseTransformDirection(toTarget);
                        float pitch = -Mathf.Asin(Mathf.Clamp(localAim.y, -0.85f, 0.85f)) * Mathf.Rad2Deg;
                        float yaw = Mathf.Atan2(localAim.x, localAim.z) * Mathf.Rad2Deg;
                        av.rightShoulderPivot.localRotation = Quaternion.Slerp(av.rightShoulderPivot.localRotation, Quaternion.Euler(pitch, yaw, 0f), dt * 8f);
                        av.rightElbowPivot.localRotation = Quaternion.Slerp(av.rightElbowPivot.localRotation, Quaternion.Euler(-15f, 0f, 0f), dt * 8f);
                    }
                    else
                    {
                        av.rightShoulderPivot.localRotation = Quaternion.Slerp(av.rightShoulderPivot.localRotation, Quaternion.Euler(0f, 0f, 6f), dt * 5f);
                        av.rightElbowPivot.localRotation = Quaternion.Slerp(av.rightElbowPivot.localRotation, Quaternion.Euler(-10f, 0f, 0f), dt * 5f);
                    }
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

        public void CenterCameraView()
        {
            var loc = OmniOS.Interaction.OmniSpatialLocomotion.Instance;
            if (loc != null)
            {
                loc.ResolveRigReferences();
            }
            Camera cam = Camera.main;
            if (cam != null)
            {
                Transform rig = cam.transform.parent != null && cam.transform.parent.parent != null 
                    ? cam.transform.parent.parent 
                    : (cam.transform.parent != null ? cam.transform.parent : cam.transform);
                if (rig != null)
                {
                    rig.position = new Vector3(0f, 0f, 0f);
                    rig.rotation = Quaternion.identity;
                }
            }
            OmniOSManager.Instance?.ShowNotificationToast("🔄 View Reset to Center");
        }

        public void HandleRemoteStreamClick(float normX, float normY)
        {
            Camera cam = streamCamera != null ? streamCamera : Camera.main;
            if (cam == null) return;

            // Viewport coords are 0..1 from bottom-left
            Ray ray = cam.ViewportPointToRay(new Vector3(normX, normY, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, 35f))
            {
                var btn = hit.collider.GetComponentInParent<SpatialButton3D>();
                if (btn != null)
                {
                    btn.TriggerClick();
                    return;
                }

                var twinAssembly = SharedDigitalTwinAssembly.Instance;
                if (twinAssembly != null)
                {
                    var part = twinAssembly.GetPartByGameObject(hit.collider.gameObject);
                    if (part != null)
                    {
                        twinAssembly.SetPartSelected(part.componentType, !part.isSelected);
                        SpatialAudioEngine.Instance?.PlayButtonClick();
                        OmniOSManager.Instance?.ShowNotificationToast($"Selected Twin Part: {part.displayName}");
                        return;
                    }
                }

                SpatialAudioEngine.Instance?.PlayHoverTick();
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
                    // 4. REMOTE MOVEMENT & STEERING ENDPOINT (/api/move?client=...&target=...&x=...&z=...&yaw=...&y=...)
                    else if (url.StartsWith("/api/move"))
                    {
                        float mx = 0f, mz = 0f, yaw = 0f, my = 0f;
                        string cId = null;
                        string targetUser = null;
                        int qIdx = url.IndexOf('?');
                        if (qIdx >= 0)
                        {
                            string query = url.Substring(qIdx + 1);
                            var pairs = query.Split('&');
                            foreach (var p in pairs)
                            {
                                var kv = p.Split('=');
                                if (kv.Length == 2)
                                {
                                    if (kv[0] == "client") cId = kv[1];
                                    if (kv[0] == "target") targetUser = Uri.UnescapeDataString(kv[1]);
                                    if (kv[0] == "x") float.TryParse(kv[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out mx);
                                    if (kv[0] == "z") float.TryParse(kv[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out mz);
                                    if (kv[0] == "y") float.TryParse(kv[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out my);
                                    if (kv[0] == "yaw") float.TryParse(kv[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out yaw);
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(cId))
                        {
                            EnqueueMainThread(() => EnsureParticipantRegistered(cId));
                        }

                        EnqueueMainThread(() => {
                            string target = !string.IsNullOrEmpty(targetUser) ? targetUser : streamTargetUserId;
                            if (!string.IsNullOrEmpty(target) && target != "arena")
                            {
                                lock (participantsLock)
                                {
                                    if (participants.TryGetValue(target, out CollaboratorAvatar targetAv) && targetAv != null && targetAv.rootObj != null)
                                    {
                                        Vector3 fwd = targetAv.rootObj.transform.forward;
                                        fwd.y = 0; if (fwd.sqrMagnitude > 0.001f) fwd.Normalize();
                                        Vector3 right = targetAv.rootObj.transform.right;
                                        right.y = 0; if (right.sqrMagnitude > 0.001f) right.Normalize();

                                        Vector3 moveDelta = (fwd * mz + right * mx + Vector3.up * my) * 0.45f;
                                        targetAv.rootObj.transform.position += moveDelta;
                                        targetAv.walkSpeed = Mathf.Max(targetAv.walkSpeed, 1.8f);
                                        targetAv.isWalking = true;
                                        targetAv.lastActivityTime = Time.time;

                                        if (Mathf.Abs(yaw) > 0.01f)
                                        {
                                            targetAv.rootObj.transform.Rotate(0f, yaw * 20f, 0f);
                                        }

                                        if (target == "vr_local" || target == "main")
                                        {
                                            OmniOS.Interaction.OmniSpatialLocomotion.Instance?.ApplyRemoteMoveDelta(mx, mz, yaw, my);
                                        }
                                        return;
                                    }
                                }
                            }
                            OmniOS.Interaction.OmniSpatialLocomotion.Instance?.ApplyRemoteMoveDelta(mx, mz, yaw, my);
                        });

                        string json = "{\"status\":\"moved\"}";
                        byte[] body = Encoding.UTF8.GetBytes(json);
                        writer.Write($"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nAccess-Control-Allow-Origin: *\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n{json}");
                    }
                    // 5. REMOTE CLICK & RAYCAST ENDPOINT (/api/click?client=...&x=...&y=...)
                    else if (url.StartsWith("/api/click"))
                    {
                        float cx = 0.5f, cy = 0.5f;
                        string cId = null;
                        int qIdx = url.IndexOf('?');
                        if (qIdx >= 0)
                        {
                            string query = url.Substring(qIdx + 1);
                            var pairs = query.Split('&');
                            foreach (var p in pairs)
                            {
                                var kv = p.Split('=');
                                if (kv.Length == 2)
                                {
                                    if (kv[0] == "client") cId = kv[1];
                                    if (kv[0] == "x") float.TryParse(kv[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out cx);
                                    if (kv[0] == "y") float.TryParse(kv[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out cy);
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(cId))
                        {
                            EnqueueMainThread(() => EnsureParticipantRegistered(cId));
                        }

                        EnqueueMainThread(() => HandleRemoteStreamClick(cx, cy));

                        string json = "{\"status\":\"clicked\"}";
                        byte[] body = Encoding.UTF8.GetBytes(json);
                        writer.Write($"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nAccess-Control-Allow-Origin: *\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n{json}");
                    }
                    // 6. STREAM PERSPECTIVE / USER VIEW SWITCHER ENDPOINT (/api/view?target=...&client=...)
                    else if (url.StartsWith("/api/view"))
                    {
                        string target = "main";
                        int qIdx = url.IndexOf('?');
                        if (qIdx >= 0)
                        {
                            string query = url.Substring(qIdx + 1);
                            var pairs = query.Split('&');
                            foreach (var p in pairs)
                            {
                                var kv = p.Split('=');
                                if (kv.Length == 2 && kv[0] == "target" && !string.IsNullOrEmpty(kv[1]))
                                {
                                    target = Uri.UnescapeDataString(kv[1]);
                                }
                            }
                        }
                        EnqueueMainThread(() => SetStreamTarget(target));
                        string json = "{\"status\":\"view_switched\",\"target\":\"" + EscapeJson(target) + "\"}";
                        byte[] body = Encoding.UTF8.GetBytes(json);
                        writer.Write($"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nAccess-Control-Allow-Origin: *\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n{json}");
                    }
                    // 7. ACTION ENDPOINT (/api/action?type=...&client=...)
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
                                    else if (act == "center") EnqueueMainThread(() => CenterCameraView());
                                    else if (act == "join") EnqueueMainThread(() => JoinAsLocalOperator());
                                }
                            }
                        }
                        string json = "{\"status\":\"action_executed\"}";
                        byte[] body = Encoding.UTF8.GetBytes(json);
                        writer.Write($"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nAccess-Control-Allow-Origin: *\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n{json}");
                    }
                    // 8. MAIN WEB CONTROLLER HTML WITH LIVE EMBEDDED STREAM
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
                        _ => (av.clientId.StartsWith("web") ? "Web" : "Mobile")
                    };
                    sb.Append($"{{\"id\":\"{EscapeJson(av.clientId)}\",\"name\":\"{EscapeJson(av.displayName)}\",\"role\":\"{roleStr}\",\"color\":\"#{hexCol}\"}}");
                }
            }

            int total = vr + pc + mob;
            return $"{{\"count\":{total},\"vr\":{vr},\"pc\":{pc},\"mobile\":{mob},\"activeView\":\"{EscapeJson(streamTargetUserId)}\",\"users\":[{sb}]}}";
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

                int guestIndex = participants.Count + 1;

                Color[] guestPalette = new Color[]
                {
                    new Color(0.0f, 1.0f, 0.55f),  // Emerald
                    new Color(0.0f, 0.85f, 1.0f),  // Electric Cyan
                    new Color(1.0f, 0.65f, 0.15f),  // Amber / Gold
                    new Color(1.0f, 0.0f, 0.75f),  // Magenta
                    new Color(0.60f, 0.20f, 1.0f),  // Violet
                    new Color(1.0f, 0.85f, 0.10f)  // Cyber Yellow
                };
                Color assignedCol = guestPalette[(guestIndex - 1) % guestPalette.Length];

                // Position around the collaborative arena in a circular formation
                float angle = (guestIndex * 50f - 25f) * Mathf.Deg2Rad;
                Vector3 guestPos = new Vector3(Mathf.Sin(angle) * 1.30f, 0f, -Mathf.Cos(angle) * 1.30f);
                Quaternion guestRot = Quaternion.LookRotation(-guestPos);

                string guestName = cId.StartsWith("web") ? $"🌐 Web Operator #{guestIndex}" :
                                   cId.StartsWith("pc") ? $"🖥️ PC Operator #{guestIndex}" :
                                   cId.StartsWith("vr") ? $"🥽 VR Operator #{guestIndex}" :
                                   $"📱 Mobile Operator #{guestIndex}";

                DeviceRole assignedRole = cId.StartsWith("web") ? DeviceRole.MobileTablet :
                                          cId.StartsWith("pc") ? DeviceRole.DesktopPC :
                                          cId.StartsWith("vr") ? DeviceRole.VRHeadset :
                                          DeviceRole.MobileTablet;

                var newAvatar = CreateHumanoidAvatar(cId, guestName, assignedCol, assignedRole, guestPos, guestRot);
                newAvatar.touchCoords = new Vector2(tx, ty);
                participants[cId] = newAvatar;
                if (primaryMobileAvatar == null) primaryMobileAvatar = newAvatar;
            }

            OmniOSManager.Instance?.ShowNotificationToast($"👥 New Operator Connected: Operator #{participants.Count}!");
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
  body { background: #07090e; color: #00e5ff; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 0; padding: 10px; text-align: center; user-select: none; }
  h1 { font-size: 16px; color: #00ff88; letter-spacing: 1.5px; margin: 4px 0 2px 0; text-transform: uppercase; }
  p { font-size: 11px; color: #7ab0d6; margin: 0 0 8px 0; }
  
  /* Live Operator Counter & Badges */
  .users-card { width: 100%; max-width: 500px; background: rgba(14,24,38,0.92); border: 1.5px solid #00e5ff; border-radius: 10px; margin: 0 auto 8px auto; padding: 8px 12px; box-shadow: 0 0 14px rgba(0,229,255,0.18); }
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
  .feed-box { width: 100%; max-width: 500px; height: 215px; background: #000; border: 2px solid #00e5ff; border-radius: 10px; margin: 0 auto 8px auto; overflow: hidden; position: relative; box-shadow: 0 0 16px rgba(0,229,255,0.25); cursor: pointer; }
  #liveStream { width: 100%; height: 100%; object-fit: cover; display: block; }
  .feed-badge { position: absolute; top: 6px; left: 8px; background: rgba(0,0,0,0.75); color: #00ff88; font-size: 10px; font-weight: bold; padding: 3px 8px; border-radius: 4px; border: 1px solid #00ff88; pointer-events: none; }
  .fps-badge { position: absolute; top: 6px; right: 8px; background: rgba(0,0,0,0.75); color: #00e5ff; font-size: 10px; font-weight: bold; padding: 3px 8px; border-radius: 4px; border: 1px solid #00e5ff; pointer-events: none; }
  .stream-hint { position: absolute; bottom: 6px; left: 0; width: 100%; font-size: 10px; color: #fff; background: rgba(0,0,0,0.70); padding: 3px 0; pointer-events: none; }

  /* Mode Switch Tabs */
  .mode-tabs { display: flex; max-width: 500px; margin: 0 auto 8px auto; gap: 6px; }
  .mode-tab { flex: 1; padding: 8px; font-size: 12px; font-weight: bold; border-radius: 6px; border: 1.5px solid #00e5ff; background: #0c1624; color: #7ab0d6; cursor: pointer; }
  .mode-tab.active { background: #00e5ff; color: #000; border-color: #00ff88; box-shadow: 0 0 10px rgba(0,229,255,0.4); }

  /* Multi-Operator Camera Switcher Ribbon */
  .cam-ribbon { display: flex; gap: 5px; max-width: 500px; margin: 0 auto 6px auto; overflow-x: auto; padding: 4px 2px; align-items: center; }
  .cam-label { font-size: 10px; font-weight: bold; color: #ffaa00; margin-right: 4px; white-space: nowrap; }
  .cam-btn { padding: 4px 9px; font-size: 10px; font-weight: bold; background: #0c1624; border: 1.5px solid #00e5ff; color: #7ab0d6; border-radius: 5px; cursor: pointer; white-space: nowrap; transition: 0.15s; }
  .cam-btn:active { transform: scale(0.95); }
  .cam-btn.active { background: #00e5ff; color: #000; border-color: #00ff88; box-shadow: 0 0 10px rgba(0,229,255,0.4); font-weight: 900; }
  .user-chip { font-size: 9px; padding: 3px 7px; border-radius: 5px; border: 1px solid #4e7899; background: #09111c; color: #cfe6f8; cursor: pointer; transition: 0.15s; }
  .user-chip:hover { transform: scale(1.05); }
  .user-chip.active-view { box-shadow: 0 0 8px #00ff88; font-weight: bold; }
  .user-chip.me { border-color: #00ff88; color: #00ff88; font-weight: bold; }

  /* D-PAD Locomotion Deck */
  #walkDeck { max-width: 500px; margin: 0 auto 8px auto; background: rgba(14,24,38,0.92); border: 1.5px solid #00ff88; border-radius: 10px; padding: 10px; box-shadow: 0 0 14px rgba(0,255,136,0.18); }
  .dpad-container { display: flex; justify-content: space-around; align-items: center; gap: 8px; }
  .dpad-grid { display: grid; grid-template-columns: 52px 52px 52px; grid-template-rows: 44px 44px 44px; gap: 5px; justify-content: center; }
  .nav-btn { background: #13273e; border: 1.5px solid #00e5ff; color: #00ff88; font-size: 16px; font-weight: bold; border-radius: 8px; display: flex; align-items: center; justify-content: center; cursor: pointer; user-select: none; touch-action: manipulation; transition: 0.1s; }
  .nav-btn:active { background: #00ff88; color: #000; transform: scale(0.94); }
  .nav-btn.special { background: #1a1a2e; border-color: #ffaa00; color: #ffaa00; font-size: 11px; }
  .turn-col { display: flex; flex-direction: column; gap: 6px; }
  .turn-btn { padding: 9px 12px; font-size: 11px; font-weight: bold; background: #13273e; border: 1.5px solid #00e5ff; color: #00e5ff; border-radius: 6px; cursor: pointer; }
  .turn-btn:active { background: #00e5ff; color: #000; }
  .walk-hint { font-size: 10px; color: #5a8aa8; margin-top: 6px; }

  /* Spatial Touchpad */
  #laserDeck { width: 100%; max-width: 500px; height: 175px; background: radial-gradient(circle, #102030 0%, #080c14 100%); border: 2px solid #00ff88; border-radius: 10px; margin: 0 auto 8px auto; touch-action: none; position: relative; cursor: crosshair; display: none; }
  #puck { width: 32px; height: 32px; background: #00ff88; border-radius: 50%; position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%); pointer-events: none; box-shadow: 0 0 14px #00ff88; }
  .pad-hint { position: absolute; bottom: 6px; left: 0; width: 100%; font-size: 10px; color: #5a8aa8; pointer-events: none; }
  
  /* Action Deck Buttons */
  .btn-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 8px; max-width: 500px; margin: 0 auto; }
  .btn { width: 100%; padding: 11px 6px; font-size: 12px; font-weight: bold; border: none; border-radius: 8px; cursor: pointer; color: #000; text-transform: uppercase; }
  .btn-peel { background: linear-gradient(90deg, #00ff88, #00e5ff); }
  .btn-explode { background: #ffaa00; }
  .btn-xray { background: #00bfff; }
  .btn-theme { background: linear-gradient(90deg, #ff00bb, #8833ff); color: #fff; }
  .status-bar { font-size: 10px; color: #4e7899; margin-top: 8px; }
</style>
</head>
<body>
<h1>HoloBridge Spatial Hub</h1>
<p>Live 3D Multi-Device Spatial Computing &bull; IEEE VR 2027</p>

<!-- Live Connected Operators / Collaborators Bar (Starts at 0) -->
<div class='users-card'>
  <div class='users-row-main'>
    <div class='live-tag'><span class='pulse-dot'></span> LIVE OPERATORS</div>
    <div class='total-badge'>👥 <span id='userCount'>0</span> ACTIVE</div>
  </div>
  <div class='roles-pills'>
    <span class='role-pill vr'>🥽 VR: <b id='vrCount'>0</b></span>
    <span class='role-pill pc'>🖥️ PC: <b id='pcCount'>0</b></span>
    <span class='role-pill mob'>📱 Mobile: <b id='mobCount'>0</b></span>
  </div>
  <div class='users-list' id='userChips'></div>
</div>

<!-- Multi-Operator Camera Switcher Ribbon -->
<div class='cam-ribbon'>
  <span class='cam-label'>📹 VIEW:</span>
  <button class='cam-btn active' id='camBtn_main' onclick='switchPerspective(""main"", ""🖥️ Host PC"")'>🖥️ Host</button>
  <button class='cam-btn' id='camBtn_birds_eye' onclick='switchPerspective(""birds_eye"", ""🦅 Bird'\''s Eye View"")'>🦅 Bird's Eye</button>
  <button class='cam-btn' id='camBtn_arena' onclick='switchPerspective(""arena"", ""🌐 Arena Overhead"")'>🌐 Arena</button>
  <button class='cam-btn' id='camBtn_vr_local' onclick='switchPerspective(""vr_local"", ""🥽 VR Lead"")'>🥽 VR</button>
  <span id='dynamicUserCamBtns' style='display:contents;'></span>
</div>

<!-- Live Camera Viewport from Unity Engine (Click to Interact) -->
<div class='feed-box' id='feedContainer' onclick='handleStreamClick(event)'>
  <div class='feed-badge' id='streamBadge'>🔴 LIVE FEED: 🖥️ HOST VIEW</div>
  <div class='fps-badge' id='fpsCounter'>25 FPS</div>
  <img id='liveStream' src='/api/frame' alt='Connecting to Unity Live View...' />
  <div class='stream-hint'>👆 Click anywhere on 3D Video to interact &bull; Click Users above to switch live view</div>
</div>

<!-- Active Multi-Platform Control Ribbon -->
<div id='controlTargetBanner' style='background:#0e1e30; border:1px solid #00ff88; color:#00ff88; font-size:11px; font-weight:bold; padding:6px 12px; border-radius:6px; margin:0 auto 8px auto; max-width:500px; display:flex; justify-content:space-between; align-items:center;'>
  <span>🎮 ACTIVE CONTROL: <span id='controllingTargetName' style='color:#00e5ff;'>🖥️ Host Player</span></span>
  <span style='font-size:9px; color:#ffaa00; background:rgba(255,170,0,0.18); border:1px solid #ffaa00; padding:2px 6px; border-radius:3px;'>ALL PLATFORMS ENABLED</span>
</div>

<!-- Mode Switch Tabs -->
<div class='mode-tabs'>
  <button class='mode-tab active' id='tabWalk' onclick='switchMode(""walk"")'>🕹️ WALK & EXPLORE IN 3D</button>
  <button class='mode-tab' id='tabLaser' onclick='switchMode(""laser"")'>🎯 3D LASER TOUCHPAD</button>
</div>

<!-- 1. D-PAD & Height Locomotion Panel -->
<div id='walkDeck'>
  <div class='dpad-container'>
    <!-- Left Column: Fly & Crouch -->
    <div class='turn-col'>
      <button class='turn-btn' onclick='sendMove(0,0,0,1)'>🔺 Fly Up</button>
      <button class='turn-btn' onclick='sendMove(0,0,0,-1)'>🔻 Crouch</button>
      <button class='turn-btn' style='color:#ffaa00; border-color:#ffaa00;' onclick='callAction(""center"")'>🔄 Reset Cam</button>
    </div>

    <!-- Center: Standard D-Pad -->
    <div class='dpad-grid'>
      <div></div>
      <button class='nav-btn' onclick='sendMove(0,1,0,0)'>⬆ W</button>
      <div></div>
      <button class='nav-btn' onclick='sendMove(-1,0,0,0)'>⬅ A</button>
      <button class='nav-btn special' onclick='callAction(""center"")'>●</button>
      <button class='nav-btn' onclick='sendMove(1,0,0,0)'>➡ D</button>
      <div></div>
      <button class='nav-btn' onclick='sendMove(0,-1,0,0)'>⬇ S</button>
      <div></div>
    </div>

    <!-- Right Column: Rotation -->
    <div class='turn-col'>
      <button class='turn-btn' onclick='sendMove(0,0,-1,0)'>↺ Turn Left</button>
      <button class='turn-btn' onclick='sendMove(0,0,1,0)'>↻ Turn Right</button>
      <button class='turn-btn' style='color:#00ff88; border-color:#00ff88;' onclick='callAction(""join"")'>+ Join 3D</button>
    </div>
  </div>
  <div class='walk-hint'>💡 On PC: Press W/A/S/D or Arrow keys on keyboard to walk around in Unity</div>
</div>

<!-- 2. 3D Optical Laser Touchpad -->
<div id='laserDeck'>
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
    myClientId = 'web_guest_' + Math.floor(Math.random() * 8999 + 1000);
    sessionStorage.setItem('holobridge_client_id', myClientId);
  }
  document.getElementById('clientStatus').innerText = 'Connected as: ' + myClientId + ' (3D Human Avatar Walking Active)';
  fetch('/api/touch?client=' + encodeURIComponent(myClientId) + '&x=0.5&y=0.5').catch(e=>{});

  let activeTargetView = 'main';

  function switchPerspective(targetId, targetName) {
    activeTargetView = targetId;
    fetch('/api/view?client=' + myClientId + '&target=' + encodeURIComponent(targetId));
    document.querySelectorAll('.cam-btn').forEach(b => b.classList.remove('active'));
    const btn = document.getElementById('camBtn_' + targetId);
    if (btn) btn.classList.add('active');
    document.getElementById('streamBadge').innerText = '🔴 LIVE FEED: ' + (targetName || targetId).toUpperCase();
    const ctrlTarget = document.getElementById('controllingTargetName');
    if (ctrlTarget) ctrlTarget.innerText = (targetName || targetId);
    if (navigator.vibrate) navigator.vibrate(20);
  }

  // Switch between Walk Navigation and Laser Pointer
  function switchMode(m) {
    const isWalk = m === 'walk';
    document.getElementById('tabWalk').className = 'mode-tab' + (isWalk ? ' active' : '');
    document.getElementById('tabLaser').className = 'mode-tab' + (!isWalk ? ' active' : '');
    document.getElementById('walkDeck').style.display = isWalk ? 'block' : 'none';
    document.getElementById('laserDeck').style.display = !isWalk ? 'block' : 'none';
  }

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
        const dynCam = document.getElementById('dynamicUserCamBtns');
        if (dynCam) dynCam.innerHTML = '';

        if (d.users) {
          d.users.forEach(u => {
            const isTarget = (d.activeView === u.id) || (activeTargetView === u.id);

            // Clickable chip in Users card (Click to switch live feed)
            const chip = document.createElement('button');
            chip.className = 'user-chip' + (u.id === myClientId ? ' me' : '') + (isTarget ? ' active-view' : '');
            chip.style.borderColor = u.color;
            chip.innerHTML = (u.id === myClientId ? '⭐ ' : '👁️ ') + u.name + ' <span style=\'opacity:0.65; font-size:8px;\'>[View]</span>';
            chip.onclick = function() { switchPerspective(u.id, u.name); };
            chips.appendChild(chip);

            // Dynamic camera button in Ribbon for mobile guests
            if (dynCam && u.id !== 'vr_local' && u.id !== 'pc_local') {
              const cBtn = document.createElement('button');
              cBtn.className = 'cam-btn' + (isTarget ? ' active' : '');
              cBtn.id = 'camBtn_' + u.id;
              cBtn.innerText = '📱 ' + u.name.replace('📱 Mobile ', '').replace('📱 ', '');
              cBtn.onclick = function() { switchPerspective(u.id, u.name); };
              dynCam.appendChild(cBtn);
            }
          });
        }
      })
      .catch(e => {});
  }
  setInterval(pollUsers, 1100);
  pollUsers();

  // 1. High-Frequency Live Frame Streamer (~25 FPS)
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

  // 2. Direct Click on Video Stream -> 3D Raycast into Unity Scene
  function handleStreamClick(e) {
    const rect = streamImg.getBoundingClientRect();
    const clickX = e.clientX - rect.left;
    const clickY = e.clientY - rect.top;
    if (clickX >= 0 && clickX <= rect.width && clickY >= 0 && clickY <= rect.height) {
      const normX = Math.max(0, Math.min(1, clickX / rect.width));
      const normY = Math.max(0, Math.min(1, 1.0 - (clickY / rect.height)));
      fetch('/api/click?client=' + myClientId + '&x=' + normX.toFixed(3) + '&y=' + normY.toFixed(3));
      if (navigator.vibrate) navigator.vibrate(20);
    }
  }

  // 3. Remote Movement Dispatcher (Controls Active Perspective/Avatar)
  function sendMove(x, z, yaw, y) {
    if (navigator.vibrate) navigator.vibrate(15);
    fetch('/api/move?client=' + myClientId + '&target=' + encodeURIComponent(activeTargetView) + '&x=' + x + '&z=' + z + '&yaw=' + yaw + '&y=' + y);
  }
    if (navigator.vibrate) navigator.vibrate(15);
    fetch('/api/move?client=' + myClientId + '&x=' + x + '&z=' + z + '&yaw=' + yaw + '&y=' + y);
  }

  // PC Keyboard Walking Support in Web Browser
  window.addEventListener('keydown', function(e) {
    if (e.target && e.target.tagName === 'INPUT') return;
    const k = e.key.toLowerCase();
    if (k === 'w' || k === 'arrowup') { sendMove(0, 1, 0, 0); e.preventDefault(); }
    else if (k === 's' || k === 'arrowdown') { sendMove(0, -1, 0, 0); e.preventDefault(); }
    else if (k === 'a' || k === 'arrowleft') { sendMove(-1, 0, 0, 0); e.preventDefault(); }
    else if (k === 'd' || k === 'arrowright') { sendMove(1, 0, 0, 0); e.preventDefault(); }
    else if (k === 'e' || k === ' ') { sendMove(0, 0, 0, 1); e.preventDefault(); }
    else if (k === 'q' || k === 'c') { sendMove(0, 0, 0, -1); e.preventDefault(); }
  });

  // 4. Touchpad Motion Steering
  const pad = document.getElementById('laserDeck');
  const puck = document.getElementById('puck');
  let lastSend = 0;

  function handlePadMove(e) {
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

  pad.addEventListener('touchmove', handlePadMove);
  pad.addEventListener('mousemove', (e) => { if (e.buttons === 1) handlePadMove(e); });

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
            if (totalOperators == 0)
            {
                GUI.Label(new Rect(bannerX, 8, bannerW, bannerH),
                    $"<color=#38bdf8>👥 ACTIVE OPERATORS: 0</color>  |  <color=#94a3b8>Waiting for operators to connect (Open Web or click '+ Join')...</color>  |  <color=#7ab0d6>🌐 Port {webServerPort}</color>",
                    bannerStyle);
            }
            else
            {
                GUI.Label(new Rect(bannerX, 8, bannerW, bannerH),
                    $"<color=#00ff88>👥 ACTIVE OPERATORS: {totalOperators}</color>  |  <color=#00e5ff>🥽 VR: {vrCount}</color>  <color=#ffaa00>🖥️ PC: {pcCount}</color>  <color=#00ff88>📱 Mobile: {mobCount}</color>  |  <color=#7ab0d6>🌐 Port {webServerPort}</color>",
                    bannerStyle);
            }

            // 1. Top-Left Collaborative Participant HUD
            bool isLocalJoined = participants.ContainsKey("pc_local") || participants.ContainsKey("vr_local") || participants.ContainsKey("web_local");
            GUILayout.BeginArea(new Rect(16, 55, 390, 195), GUI.skin.box);
            GUILayout.Label("<b>🌐 HOLOBRIDGE MULTI-DEVICE SPATIAL HUB</b>");
            GUILayout.Label($"<color=#7ab0d6>Camera Mode:</color> <color=#00e5ff>{currentCameraMode}</color>");
            GUILayout.Label($"<color=#00e5ff>Active Role [F1]:</color> {activeRole} {(isLocalJoined ? "<color=#00ff88>(Joined)</color>" : "<color=#94a3b8>(Not Joined)</color>")}");
            GUILayout.Label($"<color=#ffaa00>Connected Avatars:</color> {totalOperators} Active in 3D Room");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(currentCameraMode == PlayerCameraMode.BirdsEye ? "🦅 Bird's Eye [B]*" : "🦅 Bird's Eye [B]", GUILayout.Width(115), GUILayout.Height(24))) ToggleBirdsEyeView();
            if (GUILayout.Button(currentCameraMode == PlayerCameraMode.ThirdPerson ? "👤 3rd Person [V]*" : "👤 3rd Person [V]", GUILayout.Width(115), GUILayout.Height(24))) ToggleThirdPersonView();
            if (GUILayout.Button("🖥️ 1st [F]", GUILayout.Width(65), GUILayout.Height(24))) SetCameraMode(PlayerCameraMode.FirstPerson);
            if (GUILayout.Button("🌐 Arena", GUILayout.Width(65), GUILayout.Height(24))) SetCameraMode(PlayerCameraMode.ArenaOverview);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (!isLocalJoined)
            {
                GUI.backgroundColor = new Color(0f, 0.9f, 0.5f);
                if (GUILayout.Button("+ Join", GUILayout.Width(65), GUILayout.Height(24))) JoinAsLocalOperator();
                GUI.backgroundColor = Color.white;
            }
            if (GUILayout.Button("F1: Role", GUILayout.Width(70), GUILayout.Height(24))) CycleNextRole();
            if (GUILayout.Button("T: Theme", GUILayout.Width(70), GUILayout.Height(24))) MetaverseEnvironmentManager.Instance?.NextTheme();
            if (GUILayout.Button("M: Phone", GUILayout.Width(70), GUILayout.Height(24))) showMobileSimulator = !showMobileSimulator;
            if (GUILayout.Button("X: Explode", GUILayout.Width(75), GUILayout.Height(24))) SharedDigitalTwinAssembly.Instance?.ToggleExplodedView();
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
