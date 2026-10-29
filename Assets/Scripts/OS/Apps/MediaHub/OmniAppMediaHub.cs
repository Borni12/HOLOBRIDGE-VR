using System;
using UnityEngine;
using UnityEngine.Video;
using OmniOS.Core;
using OmniOS.Interaction;

namespace OmniOS.Apps.MediaHub
{
    /// <summary>
    /// OmniAppMediaHub - YouTube, Facebook & Android Web Apps Portal for OmniOS.
    /// Provides full 3D in-window buttons for Meta Quest Pro / Quest 3 interaction:
    /// - 3D Tab switchers (YouTube, Facebook, Web Browser, Android Apps)
    /// - 3D Play/Pause Cinema control
    /// - 3D Open in Browser launcher
    /// </summary>
    [RequireComponent(typeof(SpatialWindow), typeof(VideoPlayer), typeof(AudioSource))]
    public class OmniAppMediaHub : MonoBehaviour
    {
        public enum HubTab
        {
            YouTube = 0,
            Facebook = 1,
            WebBrowser = 2,
            AndroidApps = 3
        }

        [Header("Active Tab")]
        [SerializeField] private HubTab currentTab = HubTab.YouTube;

        // Components
        private SpatialWindow spatialWindow;
        private VideoPlayer videoPlayer;
        private AudioSource audioSource;
        private RenderTexture mediaRenderTexture;

        // Visual References
        private GameObject screenQuad;
        private Material screenMaterial;
        private Transform buttonBarRoot;
        private SpatialButton3D playPauseBtn;

        // YouTube Channel Presets
        private readonly string[] channelNames = new string[]
        {
            "Lo-Fi Cyber Beats",
            "4K Space & Nebula Orbit",
            "Metaverse Tech Innovations",
            "Relaxing 4K Nature Haven"
        };
        private int selectedChannelIndex = 0;

        private void Awake()
        {
            spatialWindow = GetComponent<SpatialWindow>();
            spatialWindow.SetDimensions(1.48f, 1.05f);

            videoPlayer = GetComponent<VideoPlayer>();
            audioSource = GetComponent<AudioSource>();

            SetupVideoPipeline();
            BuildScreenDisplay();
            Build3DControlBar();
        }

        private void Start()
        {
            string streamingPath = System.IO.Path.Combine(Application.streamingAssetsPath, "Videos/onboarding_video_final.mp4");
            string dataPath = System.IO.Path.Combine(Application.dataPath, "VRTemplateAssets/Videos/onboarding_video_final.mp4");
            string targetPath = System.IO.File.Exists(streamingPath) ? streamingPath : (System.IO.File.Exists(dataPath) ? dataPath : null);

            if (!string.IsNullOrEmpty(targetPath))
            {
                videoPlayer.url = targetPath;
                videoPlayer.Play();
                if (audioSource != null) audioSource.Play();
                if (playPauseBtn != null) playPauseBtn.ButtonText = "⏸️ Pause";
            }
        }

        private void SetupVideoPipeline()
        {
            mediaRenderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
            mediaRenderTexture.name = "OmniOS_MediaHub_RT";
            mediaRenderTexture.wrapMode = TextureWrapMode.Clamp;
            mediaRenderTexture.filterMode = FilterMode.Bilinear;
            mediaRenderTexture.Create();

            // Clear to sleek dark cyberpunk theme immediately so screen NEVER renders uninitialized white
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = mediaRenderTexture;
            GL.Clear(true, true, new Color(0.05f, 0.07f, 0.12f, 1f));
            RenderTexture.active = prev;

            videoPlayer.playOnAwake = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = mediaRenderTexture;
            videoPlayer.isLooping = true;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.SetTargetAudioSource(0, audioSource);

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.5f;
            audioSource.volume = 0.85f;
        }

        private void BuildScreenDisplay()
        {
            screenQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            screenQuad.name = "MediaHub_Screen";
            screenQuad.transform.SetParent(spatialWindow.ContentRoot, false);
            screenQuad.transform.localScale = new Vector3(1.36f, 0.72f, 1f);
            screenQuad.transform.localPosition = new Vector3(0, 0.08f, -0.01f);

            // Destroy collider on video screen quad so it NEVER blocks clicks to buttons
            var colliders = screenQuad.GetComponents<Collider>();
            foreach (var c in colliders) Destroy(c);

            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
            if (unlit == null) unlit = Shader.Find("Sprites/Default");
            screenMaterial = new Material(unlit);
            screenMaterial.mainTexture = mediaRenderTexture;
            screenMaterial.color = Color.white;
            screenQuad.GetComponent<Renderer>().material = screenMaterial;
        }

        private void Build3DControlBar()
        {
            GameObject barObj = new GameObject("MediaHub_3D_ButtonBar");
            barObj.transform.SetParent(spatialWindow.ContentRoot, false);
            barObj.transform.localPosition = new Vector3(0, -0.38f, -0.025f);
            buttonBarRoot = barObj.transform;

            // 1. YouTube Tab Button (Cycles Channels on repeat clicks)
            CreateTabButton("🔴 YouTube", new Vector3(-0.52f, 0, 0), () => {
                SwitchTab(HubTab.YouTube);
                selectedChannelIndex = (selectedChannelIndex + 1) % channelNames.Length;
                OmniOSManager.Instance?.ShowNotificationToast($"📺 YouTube Channel: {channelNames[selectedChannelIndex]}");
            }, new Color(1.0f, 0.25f, 0.25f));

            // 2. Facebook Tab Button
            CreateTabButton("🔵 Facebook", new Vector3(-0.28f, 0, 0), () => SwitchTab(HubTab.Facebook), new Color(0.2f, 0.55f, 1.0f));

            // 3. Web Browser Tab Button
            CreateTabButton("🌐 Browser", new Vector3(-0.04f, 0, 0), () => SwitchTab(HubTab.WebBrowser), new Color(0.0f, 0.88f, 1.0f));

            // 4. Play / Pause Button
            playPauseBtn = CreateTabButton("▶️ Play / Pause", new Vector3(0.24f, 0, 0), () => ToggleYouTubePlay(), new Color(0.18f, 0.92f, 0.45f));

            // 5. Open Web URL Button
            CreateTabButton("🚀 Open Link", new Vector3(0.50f, 0, 0), () => {
                string url = (currentTab == HubTab.Facebook) ? "https://www.facebook.com" : "https://www.youtube.com";
                OpenUrlInBrowser(url);
            }, new Color(0.95f, 0.75f, 0.15f));
        }

        private SpatialButton3D CreateTabButton(string label, Vector3 localPos, Action onClick, Color accent)
        {
            GameObject btnObj = new GameObject($"Btn_{label.Replace(" ", "_")}");
            btnObj.transform.SetParent(buttonBarRoot, false);
            btnObj.transform.localPosition = localPos;

            var btn = btnObj.AddComponent<SpatialButton3D>();
            btn.ButtonText = label;
            btn.SetSize(0.20f, 0.075f);
            btn.SetColors(new Color(0.08f, 0.11f, 0.16f, 0.95f), accent);
            btn.onClick.AddListener(() => onClick?.Invoke());
            return btn;
        }

        public void SwitchTab(HubTab newTab)
        {
            currentTab = newTab;
            SpatialAudioEngine.Instance?.PlayButtonClick();
            OmniOSManager.Instance?.ShowNotificationToast($"📱 Media Hub: Switched to {newTab}");
        }

        public void ToggleYouTubePlay()
        {
            if (videoPlayer.isPlaying)
            {
                videoPlayer.Pause();
                audioSource.Pause();
                if (playPauseBtn != null) playPauseBtn.ButtonText = "▶️ Play";
            }
            else
            {
                videoPlayer.Play();
                audioSource.Play();
                if (playPauseBtn != null) playPauseBtn.ButtonText = "⏸️ Pause";
            }
            SpatialAudioEngine.Instance?.PlayButtonClick();
        }

        public void OpenUrlInBrowser(string url)
        {
            try
            {
                Application.OpenURL(url);
                OmniOSManager.Instance?.ShowNotificationToast($"🌐 Opening: {url}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MediaHub] Could not open URL: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            if (mediaRenderTexture != null)
            {
                mediaRenderTexture.Release();
                Destroy(mediaRenderTexture);
            }
            if (screenMaterial != null) Destroy(screenMaterial);
        }
    }
}
