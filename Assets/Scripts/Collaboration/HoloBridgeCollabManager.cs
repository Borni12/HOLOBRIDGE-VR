using System;
using UnityEngine;
using OmniOS.Core;
using OmniOS.Interaction;
using OmniOS.Apps.MediaHub;

namespace HoloBridge.Collaboration
{
    /// <summary>
    /// HoloBridgeCollabManager - Master Coordinator for IEEE VR 2027 3DUI Collaborative Suite.
    /// Orchestrates:
    /// 1. Shared 3D Digital Twin Assembly (Multi-layer occluded Quantum Turbine)
    /// 2. Partner-Context Depth Disambiguation (PCDD Engine)
    /// 3. TaskBubble Context-Preserving Micro-Handoff & Holographic Ghost Preview
    /// 4. 3D Spatial Annotations & Persistent Callouts
    /// 5. Asymmetric Cross-Device Co-Presence (VR Quest 3 + Desktop PC)
    /// </summary>
    [DisallowMultipleComponent]
    public class HoloBridgeCollabManager : MonoBehaviour
    {
        private static HoloBridgeCollabManager instance;
        public static HoloBridgeCollabManager Instance
        {
            get
            {
                if (instance == null || !instance)
                {
                    instance = UnityEngine.Object.FindAnyObjectByType<HoloBridgeCollabManager>();
                }
                return instance;
            }
        }

        [Header("Collaborative Subsystems")]
        [SerializeField] private SharedDigitalTwinAssembly digitalTwinAssembly;
        [SerializeField] private PartnerContextDepthDisambiguator pcddEngine;
        [SerializeField] private TaskBubbleHandoffEngine handoffEngine;
        [SerializeField] private SpatialAnnotationSystem annotationSystem;
        [SerializeField] private CrossDeviceCoPresence coPresence;

        public SharedDigitalTwinAssembly DigitalTwin => digitalTwinAssembly;
        public PartnerContextDepthDisambiguator PCDD => pcddEngine;
        public TaskBubbleHandoffEngine Handoff => handoffEngine;
        public SpatialAnnotationSystem Annotations => annotationSystem;
        public CrossDeviceCoPresence CoPresence => coPresence;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrapCollabManager()
        {
            if (instance == null && UnityEngine.Object.FindAnyObjectByType<HoloBridgeCollabManager>() == null)
            {
                GameObject root = new GameObject("[HOLOBRIDGE_COLLAB_ENGINE]");
                root.AddComponent<HoloBridgeCollabManager>();
                Debug.Log("<color=#00e5ff>[HoloBridge IEEE VR 2027]</color> Collaborative Spatial Interaction Engine Online!");
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this && instance.gameObject != null)
            {
                Destroy(this);
                return;
            }
            instance = this;

            EnsureSubsystems();
        }

        private void EnsureSubsystems()
        {
            // 1. Digital Twin Turbine Assembly
            if (digitalTwinAssembly == null)
            {
                digitalTwinAssembly = UnityEngine.Object.FindAnyObjectByType<SharedDigitalTwinAssembly>();
                if (digitalTwinAssembly == null)
                {
                    GameObject turbineObj = new GameObject("HoloBridge_SharedDigitalTwin");
                    // Positioned at eye level in the collaboration arena
                    turbineObj.transform.position = new Vector3(0f, 1.35f, 1.85f);
                    digitalTwinAssembly = turbineObj.AddComponent<SharedDigitalTwinAssembly>();
                }
            }

            // 2. PCDD Engine
            if (pcddEngine == null)
            {
                pcddEngine = GetComponent<PartnerContextDepthDisambiguator>();
                if (pcddEngine == null) pcddEngine = gameObject.AddComponent<PartnerContextDepthDisambiguator>();
            }

            // 3. TaskBubble Handoff Engine
            if (handoffEngine == null)
            {
                handoffEngine = GetComponent<TaskBubbleHandoffEngine>();
                if (handoffEngine == null) handoffEngine = gameObject.AddComponent<TaskBubbleHandoffEngine>();
            }

            // 4. Spatial Annotation System
            if (annotationSystem == null)
            {
                annotationSystem = GetComponent<SpatialAnnotationSystem>();
                if (annotationSystem == null) annotationSystem = gameObject.AddComponent<SpatialAnnotationSystem>();
            }

            // 5. Cross-Device Co-Presence
            if (coPresence == null)
            {
                coPresence = GetComponent<CrossDeviceCoPresence>();
                if (coPresence == null) coPresence = gameObject.AddComponent<CrossDeviceCoPresence>();
            }
        }

        private void Update()
        {
            HandleDesktopPCDDClickInput();
        }

        private void HandleDesktopPCDDClickInput()
        {
            // When user clicks with mouse and is not interacting with 2D launcher box or tablet window
            if (Input.GetMouseButtonDown(0) && !OmniAppLauncherBox.IsOpen && !OmniAppMediaHub.IsTypingActive)
            {
                Camera cam = Camera.main ?? UnityEngine.Object.FindAnyObjectByType<Camera>();
                if (cam != null)
                {
                    Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                    Vector3 vrHandPos = coPresence != null ? coPresence.VRRightHandPosition : new Vector3(0, 1.45f, -0.2f);

                    // Execute PCDD
                    var result = pcddEngine?.DisambiguateRay(ray, vrHandPos);
                    if (result != null)
                    {
                        string pName = result.part.displayName;
                        float conf = result.partnerContextScore * 100f;
                        OmniOSManager.Instance?.ShowNotificationToast($"🎯 PCDD Selected: {pName} ({conf:F0}% VR Partner Context Match)");
                    }
                }
            }
        }

        public void TriggerQuickHandoffDemo()
        {
            if (handoffEngine != null)
            {
                handoffEngine.DelegateTaskToDesktop(SharedDigitalTwinAssembly.ComponentType.InternalBearing);
            }
        }

        public void TriggerCommitDemo()
        {
            if (handoffEngine != null)
            {
                handoffEngine.CommitPlacement();
            }
        }

        public void TriggerRejectDemo()
        {
            if (handoffEngine != null)
            {
                handoffEngine.RejectPlacement();
            }
        }
    }
}
