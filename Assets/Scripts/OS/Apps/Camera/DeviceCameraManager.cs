using System;
using UnityEngine;

namespace OmniOS.Apps.CameraApp
{
    /// <summary>
    /// DeviceCameraManager - Hardware Device Camera & Webcam Integration.
    /// Accesses the local computer's or headset's physical webcam/cameras via WebCamTexture.
    /// Provides live camera feed streaming to the OmniCam viewfinder and Reality Lift engine.
    /// </summary>
    [DisallowMultipleComponent]
    public class DeviceCameraManager : MonoBehaviour
    {
        private static DeviceCameraManager instance;
        public static DeviceCameraManager Instance => instance;

        [Header("Hardware Configuration")]
        [SerializeField] private int requestedWidth = 1920;
        [SerializeField] private int requestedHeight = 1080;
        [SerializeField] private int requestedFPS = 30;

        private WebCamTexture activeWebCamTexture;
        private WebCamDevice[] availableDevices;
        private int currentDeviceIndex = 0;
        private bool isStreaming = false;

        public bool IsDeviceCameraAvailable => availableDevices != null && availableDevices.Length > 0;
        public bool IsStreaming => isStreaming && activeWebCamTexture != null && activeWebCamTexture.isPlaying;
        public Texture LiveCameraTexture => activeWebCamTexture;
        public string CurrentDeviceName => IsDeviceCameraAvailable ? availableDevices[currentDeviceIndex].name : "No Device Camera Found";
        public int DeviceCount => availableDevices != null ? availableDevices.Length : 0;

        public event Action<Texture> OnCameraFeedStarted;
        public event Action OnCameraFeedStopped;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            RefreshAvailableDevices();
        }

        public void RefreshAvailableDevices()
        {
            availableDevices = WebCamTexture.devices;
            Debug.Log($"<color=cyan>[DeviceCamera]</color> Found {availableDevices.Length} hardware cameras on device.");
            for (int i = 0; i < availableDevices.Length; i++)
            {
                Debug.Log($"  Camera [{i}]: {availableDevices[i].name} (FrontFacing: {availableDevices[i].isFrontFacing})");
            }
        }

        public void StartStreaming()
        {
            if (!IsDeviceCameraAvailable)
            {
                RefreshAvailableDevices();
                if (!IsDeviceCameraAvailable)
                {
                    Debug.LogWarning("[DeviceCamera] No hardware camera found to stream.");
                    return;
                }
            }

            if (activeWebCamTexture != null && activeWebCamTexture.isPlaying)
            {
                activeWebCamTexture.Stop();
            }

            string deviceName = availableDevices[currentDeviceIndex].name;
            activeWebCamTexture = new WebCamTexture(deviceName, requestedWidth, requestedHeight, requestedFPS);
            activeWebCamTexture.Play();
            isStreaming = true;

            Debug.Log($"<color=cyan>[DeviceCamera]</color> Started streaming from: {deviceName}");
            OnCameraFeedStarted?.Invoke(activeWebCamTexture);
        }

        public void StopStreaming()
        {
            if (activeWebCamTexture != null && activeWebCamTexture.isPlaying)
            {
                activeWebCamTexture.Stop();
            }
            isStreaming = false;
            OnCameraFeedStopped?.Invoke();
        }

        public void SwitchToNextCamera()
        {
            if (!IsDeviceCameraAvailable || availableDevices.Length <= 1) return;

            currentDeviceIndex = (currentDeviceIndex + 1) % availableDevices.Length;
            if (isStreaming)
            {
                StartStreaming();
            }
        }

        public Texture2D CaptureWebcamSnapshot()
        {
            if (!IsStreaming) return null;

            Texture2D snapshot = new Texture2D(activeWebCamTexture.width, activeWebCamTexture.height, TextureFormat.RGB24, false);
            snapshot.SetPixels(activeWebCamTexture.GetPixels());
            snapshot.Apply();
            return snapshot;
        }

        private void OnDestroy()
        {
            StopStreaming();
            if (activeWebCamTexture != null)
            {
                Destroy(activeWebCamTexture);
            }
        }
    }
}
