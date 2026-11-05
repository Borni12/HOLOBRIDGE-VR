using System;
using System.Collections.Generic;
using UnityEngine;
using OmniOS.Core;
using OmniOS.Apps.MediaHub;
using OmniOS.Apps.CameraApp;
using OmniOS.Apps.Scenery;
using OmniOS.Apps.Gallery;
using OmniOS.Apps.VideoReality;
using OmniOS.Environment;
using OmniGenesis.Core;

namespace OmniOS.Core
{
    /// <summary>
    /// OmniAppLauncherBox - Apple Launchpad Style Master 3D & 2D Control Center.
    /// Engineered specifically for Desktop PC (Mouse + Keyboard / WASD) testing without a VR headset,
    /// while remaining 100% compatible with Meta Quest Pro / Quest 3 VR (OpenXR 6-DoF).
    /// </summary>
    [DisallowMultipleComponent]
    public class OmniAppLauncherBox : MonoBehaviour
    {
        private static OmniAppLauncherBox instance;
        public static OmniAppLauncherBox Instance
        {
            get
            {
                if (instance == null || !instance)
                {
                    instance = UnityEngine.Object.FindAnyObjectByType<OmniAppLauncherBox>();
                }
                return instance;
            }
        }

        public static bool IsOpen { get; set; } = false; // Minimized by default to keep 3D view clean, toggle with TAB
        public static bool AutoCloseOnLaunch { get; set; } = false; // User toggle to auto-close launchpad when app is brought to 3D front

        [Header("Keybindings")]
        [SerializeField] private KeyCode primaryToggleKey = KeyCode.Tab;
        [SerializeField] private KeyCode secondaryToggleKey = KeyCode.F4;
        [SerializeField] private KeyCode launchpadLetterKey = KeyCode.L;

        // Categories: 0 = All Apps & 5D, 1 = 5D Innovations, 2 = Spatial Apps, 3 = Controls & Guide
        private int activeCategory = 0;
        private readonly string[] categoryLabels = new string[]
        {
            "⭐ All Apps & 5D (13)",
            "🌟 5D Flagship Metaphors (6)",
            "📱 Spatial OS Apps (6)",
            "🎮 PC Desktop & VR Guide"
        };

        private string searchQuery = "";
        private Vector2 scrollPos = Vector2.zero;

        // Textures
        private Texture2D backdropTex;
        private Texture2D cardTex;
        private Texture2D cardHoverTex;
        private Texture2D pillTex;

        // GUI Styles
        private GUIStyle backdropStyle;
        private GUIStyle cardStyle;
        private GUIStyle headerTitleStyle;
        private GUIStyle headerSubTitleStyle;
        private GUIStyle searchBoxStyle;
        private GUIStyle categoryTabStyle;
        private GUIStyle cardTitleStyle;
        private GUIStyle cardDescStyle;
        private GUIStyle cardBadgeStyle;
        private GUIStyle cardStatusStyle;
        private GUIStyle btnPrimaryStyle;
        private GUIStyle btnSuccessStyle;
        private GUIStyle btnWarnStyle;
        private GUIStyle btnActionStyle;
        private GUIStyle dockBtnStyle;
        private bool stylesInitialized = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureLauncherInstance()
        {
            if (instance == null && UnityEngine.Object.FindAnyObjectByType<OmniAppLauncherBox>() == null)
            {
                GameObject launcherObj = new GameObject("[OMNI_LAUNCHPAD]");
                launcherObj.AddComponent<OmniAppLauncherBox>();
                UnityEngine.Object.DontDestroyOnLoad(launcherObj);
                Debug.Log("<color=cyan>[OmniLaunchpad]</color> Apple Launchpad Auto-bootstrapped successfully!");
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this && instance.gameObject != null)
            {
                Destroy(this); // Safe: destroy this component, never the GameObject
                return;
            }
            instance = this;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void Update()
        {
            // Toggle Launchpad with Tab, F4, or L (Note: M is reserved for Matter-Weave!)
            if (OmniInputHelper.CheckTab() || 
                OmniInputHelper.IsKeyTriggered(primaryToggleKey, UnityEngine.InputSystem.Key.Tab) ||
                OmniInputHelper.IsKeyTriggered(secondaryToggleKey, UnityEngine.InputSystem.Key.F4) || 
                OmniInputHelper.IsKeyTriggered(launchpadLetterKey, UnityEngine.InputSystem.Key.L))
            {
                ToggleLaunchpad();
            }

            // Escape closes Launchpad if open
            if ((Input.GetKeyDown(KeyCode.Escape) || OmniInputHelper.IsKeyTriggered(KeyCode.Escape, UnityEngine.InputSystem.Key.Escape)) && IsOpen)
            {
                IsOpen = false;
                SpatialAudioEngine.Instance?.PlayButtonClick();
            }
        }

        public void ToggleLaunchpad()
        {
            IsOpen = !IsOpen;
            SpatialAudioEngine.Instance?.PlayThemeChime();
            OmniOSManager.Instance?.ShowNotificationToast(IsOpen 
                ? "⚡ Apple Launchpad: OPEN (Press TAB or ESC to close)" 
                : "⚡ Apple Launchpad Minimized (Press TAB to re-open)");
        }

        private void InitStyles()
        {
            if (backdropTex == null) backdropTex = MakeColorTexture(new Color(0.04f, 0.06f, 0.10f, 0.95f));
            if (cardTex == null) cardTex = MakeColorTexture(new Color(0.08f, 0.12f, 0.19f, 0.92f));
            if (cardHoverTex == null) cardHoverTex = MakeColorTexture(new Color(0.12f, 0.18f, 0.28f, 0.95f));
            if (pillTex == null) pillTex = MakeColorTexture(new Color(0.0f, 0.85f, 1.0f, 0.85f));

            if (stylesInitialized && backdropStyle != null && cardStyle != null) return;

            // 1. Master Frosted Glass Backdrop
            backdropStyle = new GUIStyle(GUI.skin.box);
            backdropStyle.normal.background = backdropTex;

            // 2. Launchpad Tile Card Background
            cardStyle = new GUIStyle(GUI.skin.box);
            cardStyle.normal.background = cardTex;
            cardStyle.padding = new RectOffset(12, 12, 10, 10);
            cardStyle.margin = new RectOffset(4, 4, 6, 6);

            // 3. Apple Header Brand Titles
            headerTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 19,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            headerTitleStyle.normal.textColor = new Color(0.0f, 0.92f, 1.0f);

            headerSubTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft
            };
            headerSubTitleStyle.normal.textColor = new Color(0.70f, 0.78f, 0.88f);

            // 4. Search Field Style
            searchBoxStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 13,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                fixedHeight = 28
            };

            // 5. Category Tabs
            categoryTabStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                fixedHeight = 30
            };

            // 6. Card Labels & Badges
            cardTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                richText = true
            };
            cardTitleStyle.normal.textColor = Color.white;

            cardDescStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = true,
                richText = true
            };
            cardDescStyle.normal.textColor = new Color(0.75f, 0.82f, 0.90f);

            cardBadgeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight,
                richText = true
            };
            cardBadgeStyle.normal.textColor = new Color(1.0f, 0.80f, 0.20f);

            cardStatusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                richText = true
            };

            // 7. Action Buttons
            btnPrimaryStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            btnPrimaryStyle.normal.textColor = Color.white;

            btnSuccessStyle = new GUIStyle(btnPrimaryStyle);
            btnSuccessStyle.normal.textColor = new Color(0.2f, 1.0f, 0.5f);

            btnWarnStyle = new GUIStyle(btnPrimaryStyle);
            btnWarnStyle.normal.textColor = new Color(1.0f, 0.75f, 0.2f);

            btnActionStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                fixedHeight = 26
            };
            btnActionStyle.normal.textColor = new Color(0.95f, 0.95f, 0.98f);

            // 8. Bottom Dock Buttons
            dockBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                fixedHeight = 34
            };
            dockBtnStyle.normal.textColor = Color.white;

            stylesInitialized = true;
        }

        private Texture2D MakeColorTexture(Color col)
        {
            Texture2D tex = new Texture2D(2, 2);
            Color[] pix = new Color[] { col, col, col, col };
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }

        private void OnGUI()
        {
            try
            {
                OmniInputHelper.RegisterGUIEvent(Event.current);
                InitStyles();

                // 1. Floating Quick-Launch Dock Bar (Bottom Center, Always Available)
                RenderBottomFloatingDock();

                // 2. Full Apple Launchpad Master Window (Centered Frosted Overlay)
                if (IsOpen)
                {
                    RenderAppleLaunchpad();
                }
            }
            catch (Exception ex)
            {
                // Fallback guarantee: Never crash or hide UI
                RenderFallbackGUI(ex.Message);
            }
        }

        private void RenderFallbackGUI(string err)
        {
            GUILayout.BeginArea(new Rect(10, 10, 400, 70), GUI.skin.box);
            GUILayout.Label("<b><color=#00e5ff> OMNI LAUNCHPAD</color></b>");
            if (GUILayout.Button("Toggle Launchpad [TAB]"))
            {
                IsOpen = !IsOpen;
                stylesInitialized = false;
            }
            GUILayout.EndArea();
        }

        #region Bottom Floating Dock Bar
        private void RenderBottomFloatingDock()
        {
            float dockW = Mathf.Clamp(Screen.width - 20f, 320f, 880f);
            float dockH = 40f;
            float dockX = (Screen.width - dockW) * 0.5f;
            float dockY = Screen.height - dockH - 8f;

            GUI.Box(new Rect(dockX, dockY, dockW, dockH), "", cardStyle);
            GUILayout.BeginArea(new Rect(dockX + 6, dockY + 3, dockW - 12, dockH - 6));
            GUILayout.BeginHorizontal();

            // Master Launchpad Toggle Button
            GUI.color = IsOpen ? new Color(1.0f, 0.75f, 0.15f) : new Color(0.0f, 0.95f, 1.0f);
            string lpTitle = IsOpen ? "▲ Hide Launchpad [TAB]" : " Open Launchpad [TAB]";
            if (GUILayout.Button($"<b>{lpTitle}</b>", dockBtnStyle, GUILayout.Width(170)))
            {
                ToggleLaunchpad();
            }
            GUI.color = Color.white;

            // Direct Quick Shortcut Pills
            var exp5D = OmniGenesis5DExperience.Instance ?? UnityEngine.Object.FindAnyObjectByType<OmniGenesis5DExperience>() ?? OmniOSManager.Instance?.gameObject.AddComponent<OmniGenesis5DExperience>();

            if (GUILayout.Button("📱 Android 15 [1]", dockBtnStyle)) QuickLaunchApp("media_hub");
            if (GUILayout.Button("🔮 Peel [P]", dockBtnStyle)) exp5D?.TriggerRealityPeel();
            if (GUILayout.Button("⚛️ Matter [M]", dockBtnStyle))
            {
                if (exp5D != null)
                {
                    int next = ((int)exp5D.CurrentStiffness + 1) % 3;
                    exp5D.SetStiffness((OmniGenesis5DExperience.MaterialStiffness)next);
                }
            }
            if (GUILayout.Button("⏳ Orbs [O]", dockBtnStyle)) exp5D?.ToggleMultiverseOrbs();
            if (GUILayout.Button("🤝 Sophia [J]", dockBtnStyle)) exp5D?.TriggerHandoff();
            if (GUILayout.Button("📷 Cam [3]", dockBtnStyle)) QuickLaunchApp("camera");
            if (GUILayout.Button("🌌 Scenery [2]", dockBtnStyle)) QuickLaunchApp("scenery");

            // Utility buttons
            GUI.color = new Color(0.0f, 0.95f, 1.0f);
            if (GUILayout.Button("🔄 Reload [R]", dockBtnStyle, GUILayout.Width(95)))
            {
                OmniOSManager.Instance?.FullSystemReload();
            }
            GUI.color = new Color(0.4f, 0.9f, 1.0f);
            if (GUILayout.Button("🎯 Recenter", dockBtnStyle, GUILayout.Width(85)))
            {
                OmniOSManager.Instance?.RecenterAll();
            }
            GUI.color = Color.white;

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void QuickLaunchApp(string appId)
        {
            OmniOSManager.Instance?.BringAppToFront(appId);
            SpatialAudioEngine.Instance?.PlayWindowOpen();
        }
        #endregion

        #region Full Apple Launchpad Overlay
        private void RenderAppleLaunchpad()
        {
            float padW = Mathf.Clamp(Screen.width - 24f, 380f, 1180f);
            float padH = Mathf.Clamp(Screen.height - 56f, 300f, 740f);
            float padX = (Screen.width - padW) * 0.5f;
            float padY = (Screen.height - padH) * 0.5f - 14f;

            GUI.Box(new Rect(padX, padY, padW, padH), "", backdropStyle);

            GUILayout.BeginArea(new Rect(padX + 16, padY + 12, padW - 32, padH - 24));

            // Top Header: Brand & Search Bar
            RenderHeader(padW - 32);

            GUILayout.Space(8);

            // Category Filter Pills
            RenderCategoryTabs();

            GUILayout.Space(8);

            // Scrollable Content: Launchpad Grid or Controls Guide
            scrollPos = GUILayout.BeginScrollView(scrollPos);

            if (activeCategory == 3)
            {
                RenderDesktopControlsGuide();
            }
            else
            {
                RenderLaunchpadAppGrid(padW - 56);
            }

            GUILayout.EndScrollView();

            // Bottom Status & Master Actions Bar
            RenderFooter();

            GUILayout.EndArea();
        }

        private void RenderHeader(float width)
        {
            GUILayout.BeginHorizontal();

            // Apple Branding Title
            GUILayout.BeginVertical();
            GUILayout.Label("<b><color=#00e5ff> OMNI LAUNCHPAD</color></b> <color=#ffffff>· SPATIAL APPS & 5D SUITE</color>", headerTitleStyle);
            GUILayout.Label("Zero-VR Desktop (Mouse + Keyboard / WASD) & Meta Quest Pro / Quest 3 VR Hybrid Suite", headerSubTitleStyle);
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            // Search Bar
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("<color=#00e5ff>🔍 Filter:</color>", GUILayout.Width(55), GUILayout.Height(28));
            searchQuery = GUILayout.TextField(searchQuery, searchBoxStyle, GUILayout.Width(180));
            if (!string.IsNullOrEmpty(searchQuery))
            {
                if (GUILayout.Button("✕", GUILayout.Width(26), GUILayout.Height(28)))
                {
                    searchQuery = "";
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Auto-Close Checkbox
            AutoCloseOnLaunch = GUILayout.Toggle(AutoCloseOnLaunch, "Auto-close on Launch", GUILayout.Width(160), GUILayout.Height(28));

            GUILayout.Space(8);

            // Master Reload Button in Header
            GUI.color = new Color(0.0f, 1.0f, 0.9f);
            if (GUILayout.Button("🔄 Reload [R]", GUILayout.Width(105), GUILayout.Height(28)))
            {
                OmniOSManager.Instance?.FullSystemReload();
            }
            GUI.color = Color.white;

            GUILayout.Space(4);

            // Close Button
            GUI.color = new Color(1.0f, 0.4f, 0.4f);
            if (GUILayout.Button("✕ Close [TAB]", GUILayout.Width(105), GUILayout.Height(28)))
            {
                IsOpen = false;
                SpatialAudioEngine.Instance?.PlayButtonClick();
            }
            GUI.color = Color.white;

            GUILayout.EndHorizontal();
        }

        private void RenderCategoryTabs()
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < categoryLabels.Length; i++)
            {
                bool isActive = (activeCategory == i);
                GUI.color = isActive ? new Color(0.0f, 0.95f, 1.0f) : new Color(0.60f, 0.70f, 0.82f);
                if (GUILayout.Button(categoryLabels[i], categoryTabStyle))
                {
                    activeCategory = i;
                    SpatialAudioEngine.Instance?.PlayButtonClick();
                }
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
        }

        private void RenderFooter()
        {
            GUILayout.Space(6);
            GUILayout.BeginHorizontal();

            GUI.color = new Color(0.0f, 0.85f, 1.0f);
            if (GUILayout.Button("🎯 Recenter View [SPACE]", GUILayout.Height(28)))
            {
                OmniOSManager.Instance?.RecenterAll();
            }
            GUI.color = Color.white;

            if (GUILayout.Button("🎨 Next Theme [T]", GUILayout.Height(28)))
            {
                var env = UnityEngine.Object.FindAnyObjectByType<MetaverseEnvironmentManager>();
                env?.NextTheme();
                SpatialAudioEngine.Instance?.PlayThemeChime();
            }
            if (GUILayout.Button("👁️ Zen Mode [H]", GUILayout.Height(28)))
            {
                OmniOSManager.Instance?.ToggleCleanView();
            }
            if (GUILayout.Button("❌ Minimize Windows", GUILayout.Height(28)))
            {
                OmniOSManager.Instance?.MinimizeAll();
                SpatialAudioEngine.Instance?.PlayButtonClick();
            }
            GUI.color = new Color(0.0f, 0.95f, 1.0f);
            if (GUILayout.Button("🔄 Full Reload & Auto-Save [R]", GUILayout.Height(28)))
            {
                OmniOSManager.Instance?.FullSystemReload();
            }
            GUI.color = Color.white;

            if (GUILayout.Button("🚀 Reset All 5D Systems", GUILayout.Height(28)))
            {
                OmniGenesis5DExperience.Instance?.RebuildAll5DSystems();
                OmniOSManager.Instance?.ShowNotificationToast("⚡ 5D Neural Systems Re-initialized");
            }

            GUILayout.EndHorizontal();
        }
        #endregion

        #region Launchpad Grid Renderer
        private void RenderLaunchpadAppGrid(float availableWidth)
        {
            // Define all cards
            List<Action> cardsToRender = new List<Action>();

            // 1. Reality-Peel (5D)
            if (MatchesCategory(1) && MatchesSearch("Reality Peel 360 unprojection portal"))
            {
                cardsToRender.Add(() => RenderRealityPeelCard());
            }

            // 2. Matter-Weave Lab (5D)
            if (MatchesCategory(1) && MatchesSearch("Matter Weave Continuum Lab stiffness rigid elastic fluid squish"))
            {
                cardsToRender.Add(() => RenderMatterWeaveCard());
            }

            // 3. Multiverse Chronos (5D)
            if (MatchesCategory(1) && MatchesSearch("Multiverse Chronos 5 Time Orbs predictive future timelines"))
            {
                cardsToRender.Add(() => RenderMultiverseCard());
            }

            // 4. Conversational Avatar Sophia (5D)
            if (MatchesCategory(1) && MatchesSearch("Avatar Sophia conversational joint gaze handoff IK"))
            {
                cardsToRender.Add(() => RenderAvatarSophiaCard());
            }

            // 5. Vestibular Shield & Gaussians (5D)
            if (MatchesCategory(1) && MatchesSearch("Vestibular Shield cybersickness neural gaussian splats"))
            {
                cardsToRender.Add(() => RenderVestibularCard());
            }

            // 6. Media Hub (Apps)
            if (MatchesCategory(2) && MatchesSearch("Media Hub YouTube Facebook Web Browser video 1"))
            {
                cardsToRender.Add(() => RenderMediaHubCard());
            }

            // 7. Metaverse Scenery Explorer (Apps)
            if (MatchesCategory(2) && MatchesSearch("Metaverse Scenery Explorer Cyber Penthouse Cosmic Nebula Sunset Horizon drone 2"))
            {
                cardsToRender.Add(() => RenderSceneryCard());
            }

            // 8. OmniCam Pro (Apps)
            if (MatchesCategory(2) && MatchesSearch("OmniCam Pro 3D camera device webcam photo snapshot reality lift 3"))
            {
                cardsToRender.Add(() => RenderCameraCard());
            }

            // 9. Spatial Media Gallery (Apps)
            if (MatchesCategory(2) && MatchesSearch("Spatial Media Gallery photo album memories 3D lift 4"))
            {
                cardsToRender.Add(() => RenderGalleryCard());
            }

            // 10. Video Reality Studio (Apps)
            if (MatchesCategory(2) && MatchesSearch("Video Reality Studio 360 dome cinema immersion 5"))
            {
                cardsToRender.Add(() => RenderVideoRealityCard());
            }

            // 11. Spatial Canvas (Apps)
            if (MatchesCategory(2) && MatchesSearch("Spatial Canvas workspace notes 8"))
            {
                cardsToRender.Add(() => RenderCanvasCard());
            }

            // 12. Control Center / Settings (Apps)
            if (MatchesCategory(2) && MatchesSearch("System Settings Control Center 9"))
            {
                cardsToRender.Add(() => RenderSettingsCard());
            }

            // Calculate responsive columns: 3 columns on wide screens, 2 on medium
            int columns = Mathf.Max(1, Mathf.FloorToInt(availableWidth / 340f));
            float cardWidth = (availableWidth - (columns - 1) * 10f) / columns;

            for (int i = 0; i < cardsToRender.Count; i += columns)
            {
                GUILayout.BeginHorizontal();
                for (int c = 0; c < columns; c++)
                {
                    int index = i + c;
                    if (index < cardsToRender.Count)
                    {
                        GUILayout.BeginVertical(GUILayout.Width(cardWidth));
                        cardsToRender[index]();
                        GUILayout.EndVertical();
                    }
                    else
                    {
                        GUILayout.Space(cardWidth);
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(6);
            }

            if (cardsToRender.Count == 0)
            {
                GUILayout.Space(40);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label($"<color=yellow>🔍 No apps or features match '{searchQuery}'. Try another search term!</color>", cardTitleStyle);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
        }

        private bool MatchesCategory(int cardCategory)
        {
            if (activeCategory == 0) return true; // All
            return activeCategory == cardCategory;
        }

        private bool MatchesSearch(string keywords)
        {
            if (string.IsNullOrEmpty(searchQuery)) return true;
            return keywords.IndexOf(searchQuery.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void OnAppLaunched(string appId)
        {
            OmniOSManager.Instance?.BringAppToFront(appId);
            if (AutoCloseOnLaunch)
            {
                IsOpen = false;
            }
        }
        #endregion

        #region App Cards Implementation

        // 1. Reality-Peel 5D
        private void RenderRealityPeelCard()
        {
            var exp5D = OmniGenesis5DExperience.Instance;
            GUILayout.BeginVertical(cardStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label("🔮 <b>Reality-Peel (2D to 360°)</b>", cardTitleStyle);
            GUILayout.Label("<color=#ffd700>[Key: P]</color>", cardBadgeStyle, GUILayout.Width(75));
            GUILayout.EndHorizontal();

            string peelStatus = (exp5D != null && exp5D.IsPeelActive)
                ? "<color=#00ff88>● 360° UNPROJECTED WORLD</color>"
                : "<color=#ffaa00>○ 2D FLAT PORTAL FRAME</color>";
            GUILayout.Label(peelStatus, cardStatusStyle);

            GUILayout.Label("Bimanual projective unprojection: pulls flat 2D frame over head, expanding into full 360° stereoscopic Gaussian world.", cardDescStyle);
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            bool isPeel = (exp5D != null && exp5D.IsPeelActive);
            GUI.color = isPeel ? new Color(1f, 0.7f, 0.2f) : new Color(0f, 0.95f, 1f);
            if (GUILayout.Button(isPeel ? "🖼️ Compress 2D [P]" : "🔮 Peel 360° [P]", btnPrimaryStyle, GUILayout.Height(28)))
            {
                exp5D?.TriggerRealityPeel();
                if (AutoCloseOnLaunch) IsOpen = false;
            }
            GUI.color = Color.white;

            if (GUILayout.Button("🎯 In Front", btnActionStyle, GUILayout.Width(80)))
            {
                exp5D?.FocusRealityPeel();
                if (AutoCloseOnLaunch) IsOpen = false;
            }
            if (GUILayout.Button("↺ Reset", btnActionStyle, GUILayout.Width(65)))
            {
                exp5D?.ResetRealityPeel();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        // 2. Matter-Weave Lab
        private void RenderMatterWeaveCard()
        {
            var exp5D = OmniGenesis5DExperience.Instance;
            GUILayout.BeginVertical(cardStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label("⚛️ <b>Matter-Weave Continuum</b>", cardTitleStyle);
            GUILayout.Label("<color=#ffd700>[Key: M]</color>", cardBadgeStyle, GUILayout.Width(75));
            GUILayout.EndHorizontal();

            string stiffness = (exp5D != null) ? exp5D.CurrentStiffness.ToString() : "Elastic";
            GUILayout.Label($"<color=#00e5ff>● Stiffness: [{stiffness}]</color>", cardStatusStyle);

            GUILayout.Label("Continuum physics tuning: dynamically modulate material properties between rigid metal, elastic rubber, and viscous fluid.", cardDescStyle);
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Rigid", btnActionStyle)) exp5D?.SetStiffness(OmniGenesis5DExperience.MaterialStiffness.Rigid);
            if (GUILayout.Button("Elastic", btnActionStyle)) exp5D?.SetStiffness(OmniGenesis5DExperience.MaterialStiffness.Elastic);
            if (GUILayout.Button("Fluid", btnActionStyle)) exp5D?.SetStiffness(OmniGenesis5DExperience.MaterialStiffness.Fluid);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUI.color = new Color(1.0f, 0.75f, 0.2f);
            if (GUILayout.Button("💥 Squish & Morph [M]", btnPrimaryStyle, GUILayout.Height(26)))
            {
                exp5D?.TriggerMatterSquish();
            }
            GUI.color = Color.white;
            if (GUILayout.Button("🎯 Bring to Front", btnActionStyle, GUILayout.Width(110)))
            {
                exp5D?.FocusMatterWeaveLab();
                if (AutoCloseOnLaunch) IsOpen = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        // 3. Multiverse Chronos (5 Time Orbs)
        private void RenderMultiverseCard()
        {
            var exp5D = OmniGenesis5DExperience.Instance;
            GUILayout.BeginVertical(cardStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label("⏳ <b>Multiverse Chronos</b>", cardTitleStyle);
            GUILayout.Label("<color=#ffd700>[Key: O]</color>", cardBadgeStyle, GUILayout.Width(75));
            GUILayout.EndHorizontal();

            int activeT = (exp5D != null) ? (exp5D.ActiveTimelineIndex + 1) : 1;
            GUILayout.Label($"<color=#ff77ff>● Active: Timeline {activeT} (+10m Predictive)</color>", cardStatusStyle);

            GUILayout.Label("5 floating toroidal glass Time Orbs simulating parallel predictive futures. Click an orb to commit to that timeline.", cardDescStyle);
            GUILayout.Space(4);

            // 5 Timeline Orb Buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🟣 T1", btnActionStyle)) exp5D?.CommitTimeline(0, "Cyber Apex");
            if (GUILayout.Button("🔵 T2", btnActionStyle)) exp5D?.CommitTimeline(1, "Quantum Rift");
            if (GUILayout.Button("🟠 T3", btnActionStyle)) exp5D?.CommitTimeline(2, "Solar Flare");
            if (GUILayout.Button("🔴 T4", btnActionStyle)) exp5D?.CommitTimeline(3, "Deep Void");
            if (GUILayout.Button("🟢 T5", btnActionStyle)) exp5D?.CommitTimeline(4, "Singularity");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("⏳ Toggle 5 Orbs [O]", btnActionStyle)) exp5D?.ToggleMultiverseOrbs();
            if (GUILayout.Button("🎯 Bring Orbs to Front", btnActionStyle))
            {
                exp5D?.FocusMultiverseOrbs();
                if (AutoCloseOnLaunch) IsOpen = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        // 4. Conversational Avatar Sophia
        private void RenderAvatarSophiaCard()
        {
            var exp5D = OmniGenesis5DExperience.Instance;
            GUILayout.BeginVertical(cardStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label("🤝 <b>Avatar Sophia (Joint-Gaze)</b>", cardTitleStyle);
            GUILayout.Label("<color=#ffd700>[Key: J]</color>", cardBadgeStyle, GUILayout.Width(75));
            GUILayout.EndHorizontal();

            string handoff = (exp5D != null && exp5D.IsHandoffActive)
                ? "<color=#00ff88>● REACHING IK PHYSICAL HANDOFF</color>"
                : "<color=#00e5ff>● GAZE-TRACKING YOUR CAMERA</color>";
            GUILayout.Label(handoff, cardStatusStyle);

            GUILayout.Label("Photorealistic conversational human avatar with natural breathing, eye blinking, user gaze tracking, and reach IK transfer.", cardDescStyle);
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            GUI.color = new Color(0.2f, 1.0f, 0.5f);
            if (GUILayout.Button("🤝 Trigger Handoff [J]", btnPrimaryStyle, GUILayout.Height(28)))
            {
                exp5D?.TriggerHandoff();
            }
            GUI.color = Color.white;

            if (GUILayout.Button("👁️ Face Sophia", btnActionStyle, GUILayout.Width(110)))
            {
                exp5D?.FocusAvatar();
                if (AutoCloseOnLaunch) IsOpen = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        // 5. Vestibular Shield & Neural Gaussians
        private void RenderVestibularCard()
        {
            var exp5D = OmniGenesis5DExperience.Instance;
            GUILayout.BeginVertical(cardStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label("🛡️ <b>Vestibular Shield & Splats</b>", cardTitleStyle);
            GUILayout.Label("<color=#00ff88>[Active]</color>", cardBadgeStyle, GUILayout.Width(75));
            GUILayout.EndHorizontal();

            GUILayout.Label("<color=#00ff88>● 0-Cybersickness Motion Compensation</color>", cardStatusStyle);
            GUILayout.Label("Peripheral optical counter-flow motion shield for comfortable navigation + volumetric neural Gaussian field.", cardDescStyle);
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🛡️ Toggle Shield", btnActionStyle)) exp5D?.ToggleVestibularShield();
            if (GUILayout.Button("✨ Toggle Gaussians", btnActionStyle)) exp5D?.ToggleGaussianParticles();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        // 6. Media Hub
        private void RenderMediaHubCard()
        {
            GUILayout.BeginVertical(cardStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label("📱 <b>Android 15 Tablet (Chrome & Internet)</b>", cardTitleStyle);
            GUILayout.Label("<color=#ffd700>[Key: 1]</color>", cardBadgeStyle, GUILayout.Width(75));
            GUILayout.EndHorizontal();

            GUILayout.Label("<color=#00ff88>● LATEST ANDROID 15 (API 35) • INTERNET ACCESS ONLINE</color>", cardStatusStyle);
            GUILayout.Label("Google Pixel Tablet Pro running latest Android 15 with live Chrome web browsing, Google Search, YouTube streaming, and 5G internet testing.", cardDescStyle);
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            GUI.color = new Color(0.0f, 0.95f, 1.0f);
            if (GUILayout.Button("🚀 Launch in Front [1]", btnPrimaryStyle, GUILayout.Height(28)))
            {
                OnAppLaunched("media_hub");
            }
            GUI.color = Color.white;

            var mediaApp = UnityEngine.Object.FindAnyObjectByType<OmniAppMediaHub>();
            if (GUILayout.Button("▶ Play/Pause", btnActionStyle, GUILayout.Width(95)))
            {
                if (mediaApp != null) mediaApp.ToggleYouTubePlay();
                else OnAppLaunched("media_hub");
            }
            if (GUILayout.Button("📺 Channel", btnActionStyle, GUILayout.Width(75)))
            {
                if (mediaApp != null) mediaApp.SwitchTab(OmniAppMediaHub.HubTab.YouTube);
                else OnAppLaunched("media_hub");
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        // 7. Metaverse Scenery Explorer
        private void RenderSceneryCard()
        {
            GUILayout.BeginVertical(cardStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label("🌌 <b>Metaverse Scenery Explorer</b>", cardTitleStyle);
            GUILayout.Label("<color=#ffd700>[Key: 2 / T]</color>", cardBadgeStyle, GUILayout.Width(90));
            GUILayout.EndHorizontal();

            GUILayout.Label("<color=#00e5ff>● 3 Immersive Scenery Themes</color>", cardStatusStyle);
            GUILayout.Label("Change room environment between Cyber Penthouse, Cosmic Nebula, and Sunset Horizon with ambient audio.", cardDescStyle);
            GUILayout.Space(4);

            var sceneryApp = UnityEngine.Object.FindAnyObjectByType<OmniAppScenery>();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🏙️ Penthouse", btnActionStyle))
            {
                if (sceneryApp != null) sceneryApp.SwitchToTheme(MetaverseEnvironmentManager.EnvironmentTheme.CyberPenthouse);
                else OnAppLaunched("scenery");
            }
            if (GUILayout.Button("🌌 Nebula", btnActionStyle))
            {
                if (sceneryApp != null) sceneryApp.SwitchToTheme(MetaverseEnvironmentManager.EnvironmentTheme.CosmicNebula);
                else OnAppLaunched("scenery");
            }
            if (GUILayout.Button("🌅 Sunset", btnActionStyle))
            {
                if (sceneryApp != null) sceneryApp.SwitchToTheme(MetaverseEnvironmentManager.EnvironmentTheme.SunsetHorizon);
                else OnAppLaunched("scenery");
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUI.color = new Color(0.0f, 0.95f, 1.0f);
            if (GUILayout.Button("🚀 Launch in Front [2]", btnPrimaryStyle, GUILayout.Height(26)))
            {
                OnAppLaunched("scenery");
            }
            GUI.color = Color.white;
            if (GUILayout.Button("🎵 Drone Music", btnActionStyle, GUILayout.Width(110)))
            {
                if (sceneryApp != null) sceneryApp.ToggleAmbientDrone();
                else OnAppLaunched("scenery");
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        // 8. OmniCam Pro
        private void RenderCameraCard()
        {
            GUILayout.BeginVertical(cardStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label("📷 <b>OmniCam Pro (Spatial & Webcam)</b>", cardTitleStyle);
            GUILayout.Label("<color=#ffd700>[Key: 3 / K / V]</color>", cardBadgeStyle, GUILayout.Width(105));
            GUILayout.EndHorizontal();

            var camApp = UnityEngine.Object.FindAnyObjectByType<OmniAppCamera>();
            string camMode = (camApp != null && camApp.UseDeviceWebcam) ? "📹 Real Laptop Webcam Streaming" : "📷 3D VR Spatial Lens Active";
            GUILayout.Label($"<color=#00e5ff>● {camMode}</color>", cardStatusStyle);

            GUILayout.Label("Live optical viewfinder, hardware webcam streaming, snapshot capture, and instantaneous 3D Reality Lift.", cardDescStyle);
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            GUI.color = new Color(0.2f, 1.0f, 0.5f);
            if (GUILayout.Button("📸 Photo [K]", btnPrimaryStyle, GUILayout.Height(28)))
            {
                if (camApp != null) camApp.CapturePhoto();
                else OnAppLaunched("camera");
            }
            GUI.color = Color.white;

            if (GUILayout.Button("📹 Webcam [V]", btnActionStyle, GUILayout.Width(105)))
            {
                if (camApp != null) camApp.ToggleDeviceWebcam();
                else OnAppLaunched("camera");
            }
            if (GUILayout.Button("🌐 Reality Lift", btnActionStyle, GUILayout.Width(95)))
            {
                if (camApp != null) camApp.LiftTo3DReality();
                else OnAppLaunched("camera");
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUI.color = new Color(0.0f, 0.95f, 1.0f);
            if (GUILayout.Button("🚀 Launch in Front [3]", btnPrimaryStyle, GUILayout.Height(26)))
            {
                OnAppLaunched("camera");
            }
            GUI.color = Color.white;
            if (GUILayout.Button("✋ Summon to Hand", btnActionStyle, GUILayout.Width(130)))
            {
                if (camApp != null) camApp.SummonPhysicalCameraToHand();
                else OnAppLaunched("camera");
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        // 9. Spatial Media Gallery
        private void RenderGalleryCard()
        {
            GUILayout.BeginVertical(cardStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label("🖼️ <b>Spatial Media Gallery</b>", cardTitleStyle);
            GUILayout.Label("<color=#ffd700>[Key: 4]</color>", cardBadgeStyle, GUILayout.Width(75));
            GUILayout.EndHorizontal();

            GUILayout.Label("<color=#00e5ff>● 3D Memory Unprojection</color>", cardStatusStyle);
            GUILayout.Label("Browse saved spatial photos and instantly unproject 2D memories into the 3D Metaverse observation room.", cardDescStyle);
            GUILayout.Space(4);

            var galleryApp = UnityEngine.Object.FindAnyObjectByType<OmniAppGallery>();
            GUILayout.BeginHorizontal();
            GUI.color = new Color(0.0f, 0.95f, 1.0f);
            if (GUILayout.Button("🚀 Launch in Front [4]", btnPrimaryStyle, GUILayout.Height(28)))
            {
                OnAppLaunched("gallery");
            }
            GUI.color = Color.white;

            if (GUILayout.Button("🌐 3D Memory Lift", btnActionStyle, GUILayout.Width(130)))
            {
                if (galleryApp != null) galleryApp.LiftSelectedPhotoToReality();
                else OnAppLaunched("gallery");
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        // 10. Video Reality Studio
        private void RenderVideoRealityCard()
        {
            GUILayout.BeginVertical(cardStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label("🎥 <b>Video Reality Studio</b>", cardTitleStyle);
            GUILayout.Label("<color=#ffd700>[Key: 5 / R]</color>", cardBadgeStyle, GUILayout.Width(90));
            GUILayout.EndHorizontal();

            GUILayout.Label("<color=#00e5ff>● 360° Spherical Video Dome</color>", cardStatusStyle);
            GUILayout.Label("Cinema 2D player with smooth 0% to 100% Reality-Peel 360° spherical dome immersion transition.", cardDescStyle);
            GUILayout.Space(4);

            var vidApp = UnityEngine.Object.FindAnyObjectByType<OmniAppVideoReality>();
            GUILayout.BeginHorizontal();
            GUI.color = new Color(0.0f, 0.95f, 1.0f);
            if (GUILayout.Button("🚀 Launch in Front [5]", btnPrimaryStyle, GUILayout.Height(28)))
            {
                OnAppLaunched("video_reality");
            }
            GUI.color = Color.white;

            if (GUILayout.Button("🌐 Toggle 360° [R]", btnActionStyle, GUILayout.Width(130)))
            {
                if (vidApp != null) vidApp.ToggleRealityImmersion();
                else OnAppLaunched("video_reality");
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        // 11. Spatial Canvas
        private void RenderCanvasCard()
        {
            GUILayout.BeginVertical(cardStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label("🎨 <b>Spatial Workspace Canvas</b>", cardTitleStyle);
            GUILayout.Label("<color=#ffd700>[Key: 8]</color>", cardBadgeStyle, GUILayout.Width(75));
            GUILayout.EndHorizontal();

            GUILayout.Label("<color=#00e5ff>● Infinite Spatial Workspace</color>", cardStatusStyle);
            GUILayout.Label("3D holographic canvas for spatial diagrams, sticky notes, architectural models, and freehand drawing.", cardDescStyle);
            GUILayout.Space(4);

            GUI.color = new Color(0.0f, 0.95f, 1.0f);
            if (GUILayout.Button("🚀 Launch Canvas in Front [8]", btnPrimaryStyle, GUILayout.Height(28)))
            {
                OnAppLaunched("spatial_canvas");
            }
            GUI.color = Color.white;

            GUILayout.EndVertical();
        }

        // 12. Control Center / Settings
        private void RenderSettingsCard()
        {
            GUILayout.BeginVertical(cardStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label("⚙️ <b>System Control Center</b>", cardTitleStyle);
            GUILayout.Label("<color=#ffd700>[Key: 9]</color>", cardBadgeStyle, GUILayout.Width(75));
            GUILayout.EndHorizontal();

            GUILayout.Label("<color=#00e5ff>● Meta Horizon OS Core Kernel</color>", cardStatusStyle);
            GUILayout.Label("Adjust system themes, volume levels, graphics settings, view centering, and desktop controls.", cardDescStyle);
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            GUI.color = new Color(0.0f, 0.95f, 1.0f);
            if (GUILayout.Button("🚀 Open Settings [9]", btnPrimaryStyle, GUILayout.Height(28)))
            {
                OnAppLaunched("settings");
            }
            GUI.color = Color.white;

            if (GUILayout.Button("🎯 Recenter [Space]", btnActionStyle, GUILayout.Width(125)))
            {
                OmniOSManager.Instance?.RecenterAll();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
        #endregion

        #region Desktop Controls & VR Guide
        private void RenderDesktopControlsGuide()
        {
            GUILayout.BeginVertical(cardStyle);
            GUILayout.Label("<b>💻 PC Desktop Navigation Guide (Zero VR Headset Needed)</b>", headerTitleStyle);
            GUILayout.Space(4);
            GUILayout.Label("Every single feature in this project is 100% interactive and testable on your computer monitor using mouse & keyboard:", cardDescStyle);
            GUILayout.Space(8);

            GUILayout.Label("• <b>Right-Click Drag (Hold Right Mouse Button):</b> Smoothly rotate camera 360° to look around the Metaverse room and inspect all 3D objects.", cardDescStyle);
            GUILayout.Label("• <b>W, A, S, D / Arrow Keys:</b> Walk and fly around the observation lounge (W=Forward, S=Backward, A=Left, D=Right).", cardDescStyle);
            GUILayout.Label("• <b>Left-Click (Normal Mouse Click):</b> Fires the holographic laser pointer beam and clicks 3D buttons or opens windows.", cardDescStyle);
            GUILayout.Label("• <b>Tab, F4, or L:</b> Opens or closes this Master Apple Launchpad anytime.", cardDescStyle);
            GUILayout.Label("• <b>Space:</b> Re-centers your view and brings the active window directly in front of your eyes.", cardDescStyle);
            GUILayout.Label("• <b>Number Keys 1 to 9:</b> Instantly summon any of the 9 spatial applications directly in front of you.", cardDescStyle);
            GUILayout.Label("• <b>P Key:</b> Triggers the 3D Reality-Peel (2D to 360° unprojection).", cardDescStyle);
            GUILayout.Label("• <b>M Key:</b> Cycles Matter-Weave stiffness & squishes the physics continuum sphere.", cardDescStyle);
            GUILayout.Label("• <b>O Key:</b> Toggles the 5 Multiverse Predictive Time Orbs.", cardDescStyle);
            GUILayout.Label("• <b>J Key:</b> Triggers Avatar Sophia's cooperative reach IK handoff.", cardDescStyle);
            GUILayout.Label("• <b>K Key:</b> Captures a photo snapshot from OmniCam.", cardDescStyle);
            GUILayout.Label("• <b>V Key:</b> Toggles between the 3D VR Lens and your physical laptop/PC webcam.", cardDescStyle);
            GUILayout.Label("• <b>T Key:</b> Cycles to the next Metaverse scenery theme (Cyber Penthouse, Cosmic Nebula, Sunset Horizon).", cardDescStyle);
            GUILayout.Label("• <b>H Key:</b> Zen Mode (hides all UI for clean, uncluttered visual inspection).", cardDescStyle);

            GUILayout.Space(12);
            GUILayout.Label("<b>🥽 Meta Quest Pro / Quest 3 VR Readiness</b>", headerTitleStyle);
            GUILayout.Label("When you deploy to your Meta Quest VR headset later, the system automatically detects the 6-DoF Touch controllers: the 3D curved dock, laser pointer, and physical 3D buttons work natively in physical virtual reality!", cardDescStyle);
            GUILayout.EndVertical();
        }
        #endregion
    }
}
