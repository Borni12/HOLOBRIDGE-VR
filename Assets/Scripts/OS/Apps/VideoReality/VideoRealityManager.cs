using System;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

namespace OmniOS.Apps.VideoReality
{
    /// <summary>
    /// VideoRealityManager - Video to VR Environment Generation Engine.
    /// Implements:
    /// 1. 2D Video Ingestion & Playback via Unity VideoPlayer.
    /// 2. Reality-Peel 3D Expansion: Smoothly unprojects 2D video frames into an immersive 360 VR Environment Dome.
    /// 3. Volumetric Neural Depth Relief: Procedural depth extrusion using luminance gradients and vertex shaders.
    /// 4. 3D Volumetric Point Grid unprojection for real-time 3D particle depth pop-out.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(VideoPlayer), typeof(AudioSource))]
    public class VideoRealityManager : MonoBehaviour
    {
        [Header("Video Ingestion")]
        [SerializeField] private string defaultVideoRelativePath = "Assets/VRTemplateAssets/Videos/onboarding_video_final.mp4";
        [SerializeField] private bool autoPlayOnStart = true;
        [SerializeField] private bool loopVideo = true;

        [Header("Reality Expansion & Immersion")]
        [Range(0.0f, 1.0f)]
        [SerializeField] private float immersionLevel = 0.0f; // 0 = 2D Window, 1 = Full 360 VR Room
        [Range(0.0f, 2.0f)]
        [SerializeField] private float depthRelief = 0.45f;
        [Range(0.5f, 3.0f)]
        [SerializeField] private float exposure = 1.05f;

        [Header("Environment Dome Configuration")]
        [SerializeField] private float domeRadius = 24.0f;
        [SerializeField] private Transform userHeadTransform;

        // Components
        private VideoPlayer videoPlayer;
        private AudioSource audioSource;
        private RenderTexture videoRenderTexture;

        // 360 Environment Dome
        private GameObject environmentDome;
        private Material domeMaterial;

        // Volumetric Splat Particle Cloud
        private ParticleSystem volumetricCloud;
        private ParticleSystem.Particle[] cloudParticles;
        private const int GridWidth = 48;
        private const int GridHeight = 32;
        private Texture2D readableFrameTexture;

        // Public Properties
        public RenderTexture VideoTexture => videoRenderTexture;
        public VideoPlayer Player => videoPlayer;
        public float ImmersionLevel
        {
            get => immersionLevel;
            set
            {
                immersionLevel = Mathf.Clamp01(value);
                UpdateImmersionMaterials();
            }
        }
        public float DepthRelief
        {
            get => depthRelief;
            set
            {
                depthRelief = Mathf.Clamp(value, 0.0f, 2.0f);
                UpdateImmersionMaterials();
            }
        }
        public bool IsPlaying => videoPlayer != null && videoPlayer.isPlaying;
        public double NormalizedTime => videoPlayer != null && videoPlayer.frameCount > 0
            ? (double)videoPlayer.frame / videoPlayer.frameCount
            : 0.0;

        private void Awake()
        {
            videoPlayer = GetComponent<VideoPlayer>();
            audioSource = GetComponent<AudioSource>();

            if (userHeadTransform == null && Camera.main != null)
            {
                userHeadTransform = Camera.main.transform;
            }

            SetupVideoPipeline();
            SetupEnvironmentDome();
            SetupVolumetricCloud();
        }

        private void Start()
        {
            LoadDefaultVideo();
            if (autoPlayOnStart)
            {
                Play();
            }
        }

        private void Update()
        {
            // Dome gently follows user head so user is always centered in the 360 environment
            if (environmentDome != null && userHeadTransform != null)
            {
                environmentDome.transform.position = userHeadTransform.position;
            }

            // Sync video audio to audio source
            if (videoPlayer.canSetDirectAudioVolume && audioSource != null)
            {
                videoPlayer.SetDirectAudioVolume(0, audioSource.volume);
            }

            // Update volumetric cloud when immersion is active
            if (immersionLevel > 0.1f && volumetricCloud != null)
            {
                UpdateVolumetricDepthCloud();
            }
        }

        private void SetupVideoPipeline()
        {
            videoRenderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
            videoRenderTexture.name = "OmniOS_VideoReality_RT";
            videoRenderTexture.wrapMode = TextureWrapMode.Clamp;
            videoRenderTexture.filterMode = FilterMode.Bilinear;
            videoRenderTexture.Create();

            videoPlayer.playOnAwake = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = videoRenderTexture;
            videoPlayer.isLooping = loopVideo;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.SetTargetAudioSource(0, audioSource);

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.5f; // Semi-spatial ambient sound
            audioSource.volume = 0.8f;
        }

        private void SetupEnvironmentDome()
        {
            environmentDome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            environmentDome.name = "OmniOS_VideoReality_EnvironmentDome";
            Destroy(environmentDome.GetComponent<Collider>()); // No collision needed

            environmentDome.transform.localScale = Vector3.one * (domeRadius * 2.0f);
            if (userHeadTransform != null)
            {
                environmentDome.transform.position = userHeadTransform.position;
            }

            Shader domeShader = Shader.Find("OmniOS/VideoRealityDome");
            if (domeShader == null) domeShader = Shader.Find("Universal Render Pipeline/Unlit");

            domeMaterial = new Material(domeShader);
            domeMaterial.SetTexture("_MainTex", videoRenderTexture);
            domeMaterial.SetFloat("_Immersion", immersionLevel);
            domeMaterial.SetFloat("_DepthRelief", depthRelief);
            domeMaterial.SetFloat("_Exposure", exposure);

            environmentDome.GetComponent<Renderer>().material = domeMaterial;
            environmentDome.SetActive(true);
        }

        private void SetupVolumetricCloud()
        {
            GameObject cloudObj = new GameObject("OmniOS_VolumetricDepthCloud");
            cloudObj.transform.SetParent(transform);
            cloudObj.transform.localPosition = new Vector3(0, 0, 1.4f);

            volumetricCloud = cloudObj.AddComponent<ParticleSystem>();
            var main = volumetricCloud.main;
            main.loop = false;
            main.playOnAwake = false;
            main.maxParticles = GridWidth * GridHeight;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startSize = 0.045f;

            var emission = volumetricCloud.emission;
            emission.enabled = false;

            var shape = volumetricCloud.shape;
            shape.enabled = false;

            cloudParticles = new ParticleSystem.Particle[GridWidth * GridHeight];
            readableFrameTexture = new Texture2D(GridWidth, GridHeight, TextureFormat.RGB24, false);
        }

        private void UpdateVolumetricDepthCloud()
        {
            if (videoRenderTexture == null || !videoPlayer.isPlaying) return;

            // Sample downscaled render texture into readable texture for 3D depth displacement
            RenderTexture prevActive = RenderTexture.active;
            RenderTexture.active = videoRenderTexture;
            readableFrameTexture.ReadPixels(new Rect(0, 0, GridWidth, GridHeight), 0, 0);
            readableFrameTexture.Apply();
            RenderTexture.active = prevActive;

            Color[] pixels = readableFrameTexture.GetPixels();
            int count = pixels.Length;

            float aspect = (float)GridWidth / GridHeight;
            float totalWidth = 1.8f;
            float totalHeight = totalWidth / aspect;

            for (int y = 0; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    int index = y * GridWidth + x;
                    Color pixel = pixels[index];
                    float luma = pixel.r * 0.299f + pixel.g * 0.587f + pixel.b * 0.114f;

                    float nx = ((float)x / (GridWidth - 1) - 0.5f) * totalWidth;
                    float ny = ((float)y / (GridHeight - 1) - 0.5f) * totalHeight;
                    // Extrude depth along Z based on luma and immersion
                    float nz = (1.0f - luma) * depthRelief * immersionLevel * 0.8f;

                    cloudParticles[index].position = new Vector3(nx, ny, nz);
                    cloudParticles[index].startColor = new Color(pixel.r, pixel.g, pixel.b, immersionLevel * 0.9f);
                    cloudParticles[index].startSize = 0.042f * (1.0f + luma * 0.5f);
                }
            }

            volumetricCloud.SetParticles(cloudParticles, count);
        }

        public void LoadDefaultVideo()
        {
            string streamingPath = Path.Combine(Application.streamingAssetsPath, "Videos/onboarding_video_final.mp4");
            if (File.Exists(streamingPath))
            {
                videoPlayer.url = streamingPath;
                return;
            }

            string fullPath = Path.Combine(Application.dataPath, "..", defaultVideoRelativePath);
            fullPath = Path.GetFullPath(fullPath);

            if (File.Exists(fullPath))
            {
                videoPlayer.url = fullPath;
            }
            else
            {
                Debug.LogWarning($"[VideoRealityManager] Video file not found at: {fullPath}. Ready for URL input.");
            }
        }

        public void LoadVideoFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return;
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = url;
            Play();
        }

        public void Play()
        {
            if (videoPlayer != null)
            {
                videoPlayer.Play();
                if (audioSource != null) audioSource.Play();
            }
        }

        public void Pause()
        {
            if (videoPlayer != null)
            {
                videoPlayer.Pause();
                if (audioSource != null) audioSource.Pause();
            }
        }

        public void TogglePlayPause()
        {
            if (IsPlaying) Pause();
            else Play();
        }

        public void Seek(float normalized)
        {
            if (videoPlayer != null && videoPlayer.frameCount > 0)
            {
                long targetFrame = (long)(Mathf.Clamp01(normalized) * videoPlayer.frameCount);
                videoPlayer.frame = targetFrame;
            }
        }

        public void SetVolume(float volume)
        {
            if (audioSource != null)
            {
                audioSource.volume = Mathf.Clamp01(volume);
            }
        }

        public void SmoothTransitionImmersion(float targetImmersion, float speed = 2.0f)
        {
            StopAllCoroutines();
            StartCoroutine(ImmersionTransitionRoutine(targetImmersion, speed));
        }

        private System.Collections.IEnumerator ImmersionTransitionRoutine(float target, float speed)
        {
            while (Mathf.Abs(immersionLevel - target) > 0.005f)
            {
                immersionLevel = Mathf.MoveTowards(immersionLevel, target, Time.deltaTime * speed);
                UpdateImmersionMaterials();
                yield return null;
            }
            immersionLevel = target;
            UpdateImmersionMaterials();
        }

        private void UpdateImmersionMaterials()
        {
            if (domeMaterial != null)
            {
                domeMaterial.SetFloat("_Immersion", immersionLevel);
                domeMaterial.SetFloat("_DepthRelief", depthRelief);
                domeMaterial.SetFloat("_Exposure", exposure);
            }
        }

        private void OnDestroy()
        {
            if (videoRenderTexture != null)
            {
                videoRenderTexture.Release();
                Destroy(videoRenderTexture);
            }
            if (domeMaterial != null) Destroy(domeMaterial);
            if (readableFrameTexture != null) Destroy(readableFrameTexture);
            if (environmentDome != null) Destroy(environmentDome);
        }
    }
}
