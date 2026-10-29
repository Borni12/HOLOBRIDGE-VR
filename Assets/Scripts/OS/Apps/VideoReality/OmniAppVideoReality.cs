using System;
using UnityEngine;
using OmniOS.Core;
using OmniOS.Interaction;

namespace OmniOS.Apps.VideoReality
{
    /// <summary>
    /// OmniAppVideoReality - Interactive 3D Spatial Window Application for Video-to-VR Reality Generation.
    /// Integrates VideoRealityManager into the OmniOS Window system.
    /// Provides:
    /// - 2D Floating Video Player display
    /// - Transport controls (Play/Pause, Timeline Progress, Volume)
    /// - Reality-Peel 3D Expansion Trigger (0% to 100% full 360 VR Room)
    /// - Volumetric 3D Depth Relief slider
    /// </summary>
    [RequireComponent(typeof(SpatialWindow))]
    public class OmniAppVideoReality : MonoBehaviour
    {
        [Header("Engine Reference")]
        [SerializeField] private VideoRealityManager videoRealityManager;

        // Visual References
        private SpatialWindow spatialWindow;
        private GameObject videoDisplayQuad;
        private Material displayMaterial;
        private SpatialPointer pointer;

        // UI Interactive State
        private bool isImmersionExpanded = false;

        private void Awake()
        {
            spatialWindow = GetComponent<SpatialWindow>();
            if (videoRealityManager == null)
            {
                videoRealityManager = UnityEngine.Object.FindAnyObjectByType<VideoRealityManager>();
            }

            spatialWindow.SetDimensions(1.4f, 0.95f);
            BuildVideoScreen();
        }

        private void Start()
        {
            pointer = UnityEngine.Object.FindAnyObjectByType<SpatialPointer>();
        }

        private void BuildVideoScreen()
        {
            // Video Display Screen Quad inside the window
            videoDisplayQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            videoDisplayQuad.name = "VideoReality_Screen";
            videoDisplayQuad.transform.SetParent(spatialWindow.ContentRoot, false);
            videoDisplayQuad.transform.localScale = new Vector3(1.24f, 0.70f, 1f);
            videoDisplayQuad.transform.localPosition = new Vector3(0, 0.06f, -0.01f);

            var colliders = videoDisplayQuad.GetComponents<Collider>();
            foreach (var c in colliders) Destroy(c);

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null) unlitShader = Shader.Find("Unlit/Texture");

            displayMaterial = new Material(unlitShader);
            if (videoRealityManager != null && videoRealityManager.VideoTexture != null)
            {
                displayMaterial.mainTexture = videoRealityManager.VideoTexture;
            }
            videoDisplayQuad.GetComponent<Renderer>().material = displayMaterial;
        }

        private void Update()
        {
            // Ensure material points to active video render texture
            if (videoRealityManager != null && displayMaterial != null && displayMaterial.mainTexture == null)
            {
                displayMaterial.mainTexture = videoRealityManager.VideoTexture;
            }

            // Keyboard Shortcuts for easy testing
            if (Input.GetKeyDown(KeyCode.Space))
            {
                TogglePlayPause();
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                ToggleRealityImmersion();
            }
        }

        public void TogglePlayPause()
        {
            if (videoRealityManager == null) return;
            videoRealityManager.TogglePlayPause();
            SpatialTheme.PlaySpatialTone(transform.position, videoRealityManager.IsPlaying ? 900f : 500f);
        }

        public void ToggleRealityImmersion()
        {
            if (videoRealityManager == null) return;
            isImmersionExpanded = !isImmersionExpanded;
            float target = isImmersionExpanded ? 1.0f : 0.0f;
            videoRealityManager.SmoothTransitionImmersion(target, 2.5f);
            SpatialTheme.PlaySpatialTone(transform.position, isImmersionExpanded ? 1400f : 400f, 0.12f);
        }

        public void SetImmersionLevel(float level)
        {
            if (videoRealityManager != null)
            {
                videoRealityManager.ImmersionLevel = level;
                isImmersionExpanded = level > 0.5f;
            }
        }

        public void SetDepthRelief(float relief)
        {
            if (videoRealityManager != null)
            {
                videoRealityManager.DepthRelief = relief;
            }
        }

        // OnGUI fallback overlay for immediate interactive control in both Editor and VR
        private void OnGUI()
        {
            if (!OmniOSManager.Show2DOverlay) return;
            if (!spatialWindow.IsOpen || spatialWindow.IsMinimized) return;
            if (videoRealityManager == null) return;

            // Clean bottom-right control card
            GUILayout.BeginArea(new Rect(Screen.width - 340, Screen.height - 310, 320, 290), GUI.skin.box);
            
            GUILayout.Label("<b>🌐 VIDEO REALITY STUDIO (OmniOS)</b>");
            GUILayout.Space(4);

            string status = videoRealityManager.IsPlaying ? "<color=green>PLAYING</color>" : "<color=yellow>PAUSED</color>";
            GUILayout.Label($"Status: {status} | Mode: {(isImmersionExpanded ? "<color=cyan>360 VR WORLD</color>" : "2D WINDOW")}");

            // Progress bar
            float prog = (float)videoRealityManager.NormalizedTime;
            GUILayout.HorizontalSlider(prog, 0f, 1f);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(videoRealityManager.IsPlaying ? "Pause (Space)" : "Play (Space)", GUILayout.Height(30)))
            {
                TogglePlayPause();
            }
            if (GUILayout.Button("Restart", GUILayout.Height(30)))
            {
                videoRealityManager.Seek(0f);
                videoRealityManager.Play();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label($"<b>VR Reality Immersion: {videoRealityManager.ImmersionLevel * 100:F0}%</b>");
            float newImmersion = GUILayout.HorizontalSlider(videoRealityManager.ImmersionLevel, 0.0f, 1.0f);
            if (Mathf.Abs(newImmersion - videoRealityManager.ImmersionLevel) > 0.01f)
            {
                SetImmersionLevel(newImmersion);
            }

            // Big 1-click Reality-Peel expansion button
            GUI.color = isImmersionExpanded ? Color.cyan : Color.white;
            if (GUILayout.Button(isImmersionExpanded ? "Collapse to 2D Window (R)" : "EXPAND TO 360 VR ENVIRONMENT (R)", GUILayout.Height(36)))
            {
                ToggleRealityImmersion();
            }
            GUI.color = Color.white;

            GUILayout.Space(4);
            GUILayout.Label($"3D Volumetric Depth Relief: {videoRealityManager.DepthRelief:F2}m");
            float newRelief = GUILayout.HorizontalSlider(videoRealityManager.DepthRelief, 0.0f, 2.0f);
            if (Mathf.Abs(newRelief - videoRealityManager.DepthRelief) > 0.02f)
            {
                SetDepthRelief(newRelief);
            }

            GUILayout.EndArea();
        }
    }
}
