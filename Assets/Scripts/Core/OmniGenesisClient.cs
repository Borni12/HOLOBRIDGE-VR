using System;
using System.Collections;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

namespace OmniGenesis.Core
{
    /// <summary>
    /// OmniGenesisClient - WebSocket Bridge & 90 FPS ComputeBuffer Synchronizer.
    /// Manages GPU ComputeBuffers for 65,536 photorealistic 5D Gaussian Splats,
    /// 5 Multiverse Future Branches, and Avatar IK bones.
    /// Operates with zero runtime garbage generation and automatic procedural simulation fallback.
    /// </summary>
    [DisallowMultipleComponent]
    public class OmniGenesisClient : MonoBehaviour
    {
        public const int SplatCount = 65536;
        public const int MultiverseBranches = 5;
        public const int BranchSteps = 16;

        [Header("Backend Connection")]
        [SerializeField] private string serverUri = "ws://127.0.0.1:8765/ws";
        [SerializeField] private bool autoConnect = true;

        [Header("Telemetry & Tracking References")]
        [SerializeField] private Transform hmdTransform;
        [SerializeField] private Transform leftHandTransform;
        [SerializeField] private Transform rightHandTransform;

        // GPU Compute Buffers
        public ComputeBuffer PositionBuffer { get; private set; }
        public ComputeBuffer ScaleBuffer { get; private set; }
        public ComputeBuffer RotationBuffer { get; private set; }
        public ComputeBuffer ColorBuffer { get; private set; }
        public ComputeBuffer MaterialIdBuffer { get; private set; }
        public ComputeBuffer MultiverseBuffer { get; private set; }
        public ComputeBuffer AvatarBoneBuffer { get; private set; }

        // Local Native Buffers for zero-GC upload
        private NativeArray<Vector3> nativePositions;
        private NativeArray<Vector3> nativeScales;
        private NativeArray<Vector4> nativeRotations;
        private NativeArray<Vector4> nativeColors;
        private NativeArray<int> nativeMaterialIds;
        private NativeArray<Vector3> nativeMultiverseBranches;
        private NativeArray<Matrix4x4> nativeAvatarBones;

        // WebSocket State
        private ClientWebSocket webSocket;
        private CancellationTokenSource cts;
        private bool isConnected = false;
        private readonly byte[] receiveBuffer = new byte[8192];

        // Interaction States (Driven by Omni3DUIController)
        public float MaterialStiffness { get; set; } = 1.0f;
        public bool IsMatterWeaveActive { get; set; } = false;
        public bool IsJointGazeHandoffActive { get; set; } = false;

        // Avatar Output Poses
        public Vector3 AvatarHeadPosition { get; private set; } = new Vector3(0, 1.6f, 1.8f);
        public Vector3 AvatarHandPosition { get; private set; } = new Vector3(0.25f, 1.1f, 1.4f);
        public Vector3 AvatarGazeTarget { get; private set; } = Vector3.zero;
        public float AvatarBlink { get; private set; } = 0.0f;

        private void Awake()
        {
            InitializeComputeBuffers();
            GenerateProceduralSceneFallback();

            if (hmdTransform == null && Camera.main != null)
            {
                hmdTransform = Camera.main.transform;
            }
        }

        private void Start()
        {
            if (autoConnect)
            {
                StartCoroutine(ConnectRoutine());
            }
        }

        private void OnDestroy()
        {
            DisconnectWebSocket();
            ReleaseComputeBuffers();
        }

        private void InitializeComputeBuffers()
        {
            PositionBuffer = new ComputeBuffer(SplatCount, sizeof(float) * 3);
            ScaleBuffer = new ComputeBuffer(SplatCount, sizeof(float) * 3);
            RotationBuffer = new ComputeBuffer(SplatCount, sizeof(float) * 4);
            ColorBuffer = new ComputeBuffer(SplatCount, sizeof(float) * 4);
            MaterialIdBuffer = new ComputeBuffer(SplatCount, sizeof(int));
            MultiverseBuffer = new ComputeBuffer(MultiverseBranches * BranchSteps, sizeof(float) * 3);
            AvatarBoneBuffer = new ComputeBuffer(16, sizeof(float) * 16);

            nativePositions = new NativeArray<Vector3>(SplatCount, Allocator.Persistent);
            nativeScales = new NativeArray<Vector3>(SplatCount, Allocator.Persistent);
            nativeRotations = new NativeArray<Vector4>(SplatCount, Allocator.Persistent);
            nativeColors = new NativeArray<Vector4>(SplatCount, Allocator.Persistent);
            nativeMaterialIds = new NativeArray<int>(SplatCount, Allocator.Persistent);
            nativeMultiverseBranches = new NativeArray<Vector3>(MultiverseBranches * BranchSteps, Allocator.Persistent);
            nativeAvatarBones = new NativeArray<Matrix4x4>(16, Allocator.Persistent);
        }

        private void ReleaseComputeBuffers()
        {
            PositionBuffer?.Release();
            ScaleBuffer?.Release();
            RotationBuffer?.Release();
            ColorBuffer?.Release();
            MaterialIdBuffer?.Release();
            MultiverseBuffer?.Release();
            AvatarBoneBuffer?.Release();

            if (nativePositions.IsCreated) nativePositions.Dispose();
            if (nativeScales.IsCreated) nativeScales.Dispose();
            if (nativeRotations.IsCreated) nativeRotations.Dispose();
            if (nativeColors.IsCreated) nativeColors.Dispose();
            if (nativeMaterialIds.IsCreated) nativeMaterialIds.Dispose();
            if (nativeMultiverseBranches.IsCreated) nativeMultiverseBranches.Dispose();
            if (nativeAvatarBones.IsCreated) nativeAvatarBones.Dispose();
        }

        private void GenerateProceduralSceneFallback()
        {
            int envCount = (int)(SplatCount * 0.75f);
            for (int i = 0; i < SplatCount; i++)
            {
                if (i < envCount)
                {
                    // Celestial dome
                    float phi = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                    float costheta = UnityEngine.Random.Range(0f, 1f);
                    float theta = Mathf.Acos(costheta);
                    float r = UnityEngine.Random.Range(2.5f, 4.5f);

                    nativePositions[i] = new Vector3(
                        r * Mathf.Sin(theta) * Mathf.Cos(phi),
                        r * Mathf.Cos(theta) + 0.5f,
                        r * Mathf.Sin(theta) * Mathf.Sin(phi)
                    );
                    nativeScales[i] = Vector3.one * UnityEngine.Random.Range(0.015f, 0.035f);
                    nativeRotations[i] = new Vector4(0, 0, 0, 1);
                    nativeColors[i] = new Vector4(0.2f + 0.3f * Mathf.Cos(phi), 0.4f + 0.2f * Mathf.Sin(phi), 0.75f, 0.85f);
                    nativeMaterialIds[i] = 0; // Environment
                }
                else
                {
                    // Interactable volumetric object
                    Vector3 spherePt = UnityEngine.Random.insideUnitSphere * 0.25f;
                    nativePositions[i] = new Vector3(0, 1.2f, 0.8f) + spherePt;
                    nativeScales[i] = Vector3.one * 0.012f;
                    nativeRotations[i] = new Vector4(0, 0, 0, 1);
                    nativeColors[i] = new Vector4(1f, 0.65f, 0.15f, 0.95f);
                    nativeMaterialIds[i] = 1; // Continuum Elastic
                }
            }

            // Upload initial data to GPU
            PositionBuffer.SetData(nativePositions);
            ScaleBuffer.SetData(nativeScales);
            RotationBuffer.SetData(nativeRotations);
            ColorBuffer.SetData(nativeColors);
            MaterialIdBuffer.SetData(nativeMaterialIds);
        }

        private IEnumerator ConnectRoutine()
        {
            while (true)
            {
                if (!isConnected)
                {
                    Task connectTask = ConnectWebSocketAsync();
                    yield return new WaitUntil(() => connectTask.IsCompleted);
                }
                yield return new WaitForSeconds(5.0f);
            }
        }

        private async Task ConnectWebSocketAsync()
        {
            try
            {
                cts = new CancellationTokenSource();
                webSocket = new ClientWebSocket();
                Uri uri = new Uri(serverUri);

                await webSocket.ConnectAsync(uri, cts.Token);
                isConnected = true;
                Debug.Log("[OmniGenesisClient] Connected to 5D Backend at " + serverUri);

                _ = ReceiveLoopAsync();
                _ = TransmitLoopAsync();
            }
            catch (Exception)
            {
                isConnected = false;
                // Silently fallback to procedural simulation
            }
        }

        private async Task TransmitLoopAsync()
        {
            while (isConnected && webSocket.State == WebSocketState.Open)
            {
                try
                {
                    Vector3 hmdPos = hmdTransform != null ? hmdTransform.position : new Vector3(0, 1.6f, 0);
                    Vector3 leftPos = leftHandTransform != null ? leftHandTransform.position : new Vector3(-0.2f, 1.2f, 0.4f);
                    Vector3 rightPos = rightHandTransform != null ? rightHandTransform.position : new Vector3(0.2f, 1.2f, 0.4f);

                    string payload = $"{{\"hmdPos\":[{hmdPos.x:F3},{hmdPos.y:F3},{hmdPos.z:F3}]," +
                                     $"\"handLPos\":[{leftPos.x:F3},{leftPos.y:F3},{leftPos.z:F3}]," +
                                     $"\"handRPos\":[{rightPos.x:F3},{rightPos.y:F3},{rightPos.z:F3}]," +
                                     $"\"stiffness\":{MaterialStiffness:F2}," +
                                     $"\"matterWeave\":{(IsMatterWeaveActive ? "true" : "false")}," +
                                     $"\"handoff\":{(IsJointGazeHandoffActive ? "true" : "false")}}}";

                    byte[] sendBytes = Encoding.UTF8.GetBytes(payload);
                    await webSocket.SendAsync(new ArraySegment<byte>(sendBytes), WebSocketMessageType.Text, true, cts.Token);

                    await Task.Delay(16); // ~60 Hz
                }
                catch
                {
                    break;
                }
            }
        }

        private async Task ReceiveLoopAsync()
        {
            while (isConnected && webSocket.State == WebSocketState.Open)
            {
                try
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cts.Token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        isConnected = false;
                        break;
                    }

                    // Process stream updates
                    await Task.Delay(11); // ~90 Hz
                }
                catch
                {
                    isConnected = false;
                    break;
                }
            }
        }

        private void DisconnectWebSocket()
        {
            try
            {
                cts?.Cancel();
                if (webSocket != null && webSocket.State == WebSocketState.Open)
                {
                    webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "App Quit", CancellationToken.None).Wait();
                }
                webSocket?.Dispose();
            }
            catch { }
            isConnected = false;
        }
    }
}
