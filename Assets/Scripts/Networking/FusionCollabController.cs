using Fusion;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace AegisSpatial.Networking
{
    /// <summary>
    /// Photon Fusion 2 State Synchronization & Soft-Locking Controller for Collaborative VR.
    /// Manages state authority handoffs, multi-client soft-locking with zero-allocation visual feedback,
    /// and low-latency polyline stroke broadcasting for collaborative 3D spatial drawing.
    /// Fully optimized for Meta Quest standalone (zero LINQ, cached properties).
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class FusionCollabController : NetworkBehaviour
    {
        [Header("Mesh & Visual Feedback")]
        [SerializeField] private Renderer targetMeshRenderer;
        [SerializeField] private LineRenderer targetLineRenderer;

        [Header("Soft-Lock Material Colors")]
        [SerializeField] private Color unlockedColor = Color.white;
        [SerializeField] private Color ownerSelectedColor = new Color(0.1f, 1.0f, 0.4f, 1.0f);
        [SerializeField] private Color remoteLockedColor = new Color(1.0f, 0.45f, 0.05f, 0.65f);

        [Header("Shader Property Names")]
        [SerializeField] private string colorPropertyName = "_BaseColor";
        [SerializeField] private string emissionPropertyName = "_EmissionColor";

        // Photon Fusion 2 Networked State
        [Networked] public NetworkBool IsLocked { get; set; }
        [Networked] public PlayerRef LockedBy { get; set; }

        // Change detector for reactive visual updates
        private ChangeDetector changeDetector;
        private MaterialPropertyBlock propBlock;
        private int colorPropId;
        private int emissionPropId;

        // XR Interaction Toolkit listener
        private XRBaseInteractable cachedInteractable;

        private void Awake()
        {
            if (targetMeshRenderer == null)
            {
                targetMeshRenderer = GetComponent<Renderer>();
            }

            if (targetLineRenderer == null)
            {
                targetLineRenderer = GetComponent<LineRenderer>();
            }

            cachedInteractable = GetComponent<XRBaseInteractable>();

            colorPropId = Shader.PropertyToID(colorPropertyName);
            emissionPropId = Shader.PropertyToID(emissionPropertyName);
            propBlock = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            if (cachedInteractable != null)
            {
                cachedInteractable.selectEntered.AddListener(OnSelectEntered);
                cachedInteractable.selectExited.AddListener(OnSelectExited);
            }
        }

        private void OnDisable()
        {
            if (cachedInteractable != null)
            {
                cachedInteractable.selectEntered.RemoveListener(OnSelectEntered);
                cachedInteractable.selectExited.RemoveListener(OnSelectExited);
            }
        }

        public override void Spawned()
        {
            changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
            UpdateVisualFeedback();
        }

        public override void Render()
        {
            if (changeDetector == null) return;

            foreach (var change in changeDetector.DetectChanges(this))
            {
                if (change == nameof(IsLocked) || change == nameof(LockedBy))
                {
                    UpdateVisualFeedback();
                }
            }
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            TryLockObject();
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            ReleaseLock();
        }

        /// <summary>
        /// Requests state authority and soft-locks the object for the local player.
        /// Returns true if lock was successfully obtained or requested.
        /// </summary>
        public bool TryLockObject()
        {
            if (Object == null || !Object.IsValid || Runner == null) return false;

            // Reject if already locked by a remote participant
            if (IsLocked && LockedBy != Runner.LocalPlayer)
            {
                return false;
            }

            // Transfer authority if not already held
            if (!Object.HasStateAuthority)
            {
                Object.RequestStateAuthority();
            }

            IsLocked = true;
            LockedBy = Runner.LocalPlayer;
            UpdateVisualFeedback();
            return true;
        }

        /// <summary>
        /// Releases soft-lock if currently held by the local player.
        /// </summary>
        public void ReleaseLock()
        {
            if (Object == null || !Object.IsValid || Runner == null) return;

            if (IsLocked && LockedBy == Runner.LocalPlayer)
            {
                IsLocked = false;
                LockedBy = PlayerRef.None;
                UpdateVisualFeedback();
            }
        }

        /// <summary>
        /// Broadcasts a newly captured precision stroke coordinate to all remote clients.
        /// </summary>
        /// <param name="worldPoint">Point to append to the polyline stroke.</param>
        public void BroadcastStrokePoint(Vector3 worldPoint)
        {
            AppendPointInternal(worldPoint);

            if (Object != null && Object.IsValid && HasStateAuthority)
            {
                Rpc_SyncStrokePoint(worldPoint);
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)]
        public void Rpc_SyncStrokePoint(Vector3 worldPoint)
        {
            AppendPointInternal(worldPoint);
        }

        private void AppendPointInternal(Vector3 point)
        {
            if (targetLineRenderer == null) return;

            int count = targetLineRenderer.positionCount;
            targetLineRenderer.positionCount = count + 1;
            targetLineRenderer.SetPosition(count, point);
        }

        /// <summary>
        /// Updates visual material properties across clients with zero runtime garbage allocation.
        /// </summary>
        private void UpdateVisualFeedback()
        {
            if (targetMeshRenderer == null) return;

            Color targetColor;
            Color emissionColor;

            if (!IsLocked)
            {
                targetColor = unlockedColor;
                emissionColor = Color.black;
            }
            else if (Runner != null && LockedBy == Runner.LocalPlayer)
            {
                // Owner client: Subtle green highlight
                targetColor = ownerSelectedColor;
                emissionColor = ownerSelectedColor * 0.4f;
            }
            else
            {
                // Remote clients: Semi-transparent orange locked outline
                targetColor = remoteLockedColor;
                emissionColor = remoteLockedColor * 0.5f;
            }

            targetMeshRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor(colorPropId, targetColor);
            propBlock.SetColor(emissionPropId, emissionColor);
            targetMeshRenderer.SetPropertyBlock(propBlock);
        }
    }
}
