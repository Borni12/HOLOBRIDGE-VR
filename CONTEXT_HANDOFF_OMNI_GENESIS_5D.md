# CONTEXT HANDOFF: OMNI-GENESIS 5D & OMNIOS METAVERSE VR/PC SUITE
**Generated for Seamless Session Continuity in Antigravity / Claude / ChatGPT**
**Project Directory:** `F:\VR Projects\HOLOBRIDGE-VR`
**Unity Version:** Unity 6 (`6000.6.5f1`), Universal Render Pipeline (URP 17.6.0)
**Target Platform:** Dual-Mode — Standalone Windows 64-bit Desktop (`Build/SpatialSync_3DUI.exe`) & Meta Quest Pro / Quest 3 VR (OpenXR 6-DoF)

---

## 1. USER & HARDWARE CONTEXT (CRITICAL)
- **Current Testing Environment:** The user is currently testing on a **PC / Laptop without a VR headset connected**.
- **Interaction Mode:** Desktop Mouse (Right-Click Drag to look around 360°, Left-Click laser pointer, Scroll) and Keyboard (WASD to walk, `Tab` for Launcher Box, `Space` to Recenter, `1`-`9` for Apps, `P` for Reality-Peel, `O` for Multiverse Orbs, `J` for Avatar Handoff, `K` for Photo, `V` for Webcam).
- **Future Deployment:** Once desktop testing is completed, the project will be deployed and tested on a **Meta Quest 3 / Quest Pro VR headset**.
- **Compilation Status:** 100% clean build. Both `Assembly-CSharp.csproj` and `Assembly-CSharp-Editor.csproj` compile with **`0 Errors`**.

---

## 2. CORE FLAGSHIP 5D INNOVATIONS (from OMNI_GENESIS_5D_BLUEPRINT.md)
The system implements all 5 flagship interaction metaphors from the master blueprint:
1. **Reality-Peel (2D to 360° Unprojection):**
   - Floating 2D portal frame expands into a full 360° stereoscopic Gaussian environment dome.
   - Script: `Assets/Scripts/Core/OmniGenesis5DExperience.cs` (`TriggerRealityPeel()`, `ResetRealityPeel()`, `FocusRealityPeel()`).
   - Hotkey: `P` or Launcher Box button.
2. **Matter-Weave Continuum Lab (Stiffness Tuning):**
   - Interactive morphing sphere whose physics and elasticity can be tuned between `Rigid Metal`, `Elastic Rubber`, and `Viscous Liquid Fluid`.
   - Script: `OmniGenesis5DExperience.cs` (`SetStiffness()`, `TriggerMatterSquish()`, `FocusMatterWeaveLab()`).
   - Hotkey: `M` or Launcher Box buttons.
3. **Multiverse Forking (5 Predictive Time Orbs):**
   - 5 floating toroidal glass Time Orbs (`T1: Cyber Apex`, `T2: Quantum Rift`, `T3: Solar Flare`, `T4: Deep Void`, `T5: Singularity`) simulating parallel future timelines.
   - Script: `OmniGenesis5DExperience.cs` (`CommitTimeline()`, `ToggleMultiverseOrbs()`, `FocusMultiverseOrbs()`).
   - Hotkey: `O` or Launcher Box buttons.
4. **Conversational Avatar Sophia (Joint-Gaze Cooperative Handoff):**
   - Photorealistic human avatar with natural breathing, eye blinking, gaze tracking directed at the user's camera, and Inverse Kinematics (IK) reach transfer of a Quantum Relic.
   - Script: `OmniGenesis5DExperience.cs` (`TriggerHandoff()`, `FocusAvatar()`).
   - Hotkey: `J` or Launcher Box buttons.
5. **Vestibular Counter-Flow Shield:**
   - Active peripheral optical counter-flow motion sickness shield on `Camera.main` for 0 cybersickness during aggressive travel or 4D unprojection.
   - Script: `Assets/Scripts/Biometrics/VestibularShieldPass.cs` & `OmniGenesis5DExperience.cs` (`ToggleVestibularShield()`).
6. **Volumetric Gaussian Radiance Field:**
   - Particle field representing neural 3D Gaussian splats.

---

## 3. SPATIAL OS & APPLICATION SUITE
1. **OmniAppLauncherBox (`Assets/Scripts/OS/Core/OmniAppLauncherBox.cs`):**
   - Master on-screen 2D/3D Controller Box.
   - Opens on start or toggles with `Tab`, `M`, or bottom pill button.
   - 3 Tabs: `🌟 5D Innovations`, `📱 Spatial Apps Suite`, `🎮 PC Desktop & VR Guide`.
   - Has "Bring to Front" ergonomics: clicking any app or 5D feature automatically positions and rotates that 3D window directly in front of the camera at `1.55m`.
2. **Media Hub (`Assets/Scripts/OS/Apps/MediaHub/OmniAppMediaHub.cs`):**
   - YouTube channels, Facebook, Web Browser launcher, 3D playback transport controls.
   - Loads video from `Application.streamingAssetsPath` (`Assets/StreamingAssets/Videos/onboarding_video_final.mp4`).
   - Pre-clears render texture with dark cyberpunk theme to eliminate white panel bugs.
3. **OmniCam Pro (`Assets/Scripts/OS/Apps/Camera/OmniAppCamera.cs`):**
   - Live viewfinder, 3D VR lens, hardware device webcam streaming (`V`), snapshot capture (`K`), Reality Lift.
4. **Metaverse Scenery Explorer (`Assets/Scripts/OS/Apps/Scenery/OmniAppScenery.cs`):**
   - Cyber Penthouse, Cosmic Nebula, Sunset Horizon themes + Cosmic Ambient Drone audio.
5. **Spatial Media Gallery (`Assets/Scripts/OS/Apps/Gallery/OmniAppGallery.cs`):**
   - Saved photos browser + instant 3D memory unprojection into the VR room.
6. **Video Reality Studio (`Assets/Scripts/OS/Apps/VideoReality/OmniAppVideoReality.cs` & `VideoRealityManager.cs`):**
   - 2D player with smooth 0% to 100% 360° dome expansion.
7. **Spatial Dock (`Assets/Scripts/OS/Core/SpatialDock.cs`):**
   - Ergonomic curved glass dock floating at waist level with 10 direct 3D buttons.
8. **Spatial Pointer (`Assets/Scripts/OS/Interaction/SpatialPointer.cs`):**
   - 2-Pass raycasting with strict priority for interactive buttons (`SpatialButton3D`, sliders, handles).
   - Desktop WASD movement and Right-Click camera look.
   - Blocks clicks through 2D launcher box into 3D objects behind it.

---

## 4. CRITICAL BUG FIXES COMPLETED
1. **White Panel Bug in Standalone Build Resolved:**
   - Moved video to `Assets/StreamingAssets/Videos/onboarding_video_final.mp4` and also mirrored it into `Build/SpatialSync_3DUI_Data/StreamingAssets/Videos/`.
   - Initialized and cleared `RenderTexture` with `GL.Clear` dark navy color.
2. **Shader Stripping in Standalone Build Prevented:**
   - Added all 6 custom shaders to `ProjectSettings/GraphicsSettings.asset` under `m_AlwaysIncludedShaders`.
   - Created serialized `.mat` assets in `Assets/Resources/Materials/` (`M_SpatialGlassRealistic.mat`, `M_SpatialGlassUI.mat`, `M_MetaverseFloor.mat`).
   - Implemented `SpatialTheme.ApplyVisualProperties` supporting custom glass (`_BaseTint`, `_BorderGlowColor`) and URP Unlit (`_BaseColor`, `_Color`).
3. **Collider Raycast Hijacking Eliminated:**
   - Removed decorative colliders from window backplates, media screens, gallery quads, and portal quads.
   - Enforced 2-pass interactive raycasting priority in `SpatialPointer.cs`.
4. **Desktop Accessibility without VR Solved:**
   - Created `OmniAppLauncherBox.cs` and added `BringAppToFront()` in `OmniOSManager.cs` so any feature can be summoned directly in front of the camera.

---

## 5. REPOSITORY FILE STRUCTURE (KEY FILES)
```
Assets/
├── Editor/
│   └── OmniOSSetupTool.cs                      # Scene setup & standalone build menu
├── Resources/
│   └── Materials/
│       ├── M_SpatialGlassRealistic.mat         # Material asset to prevent shader stripping
│       ├── M_SpatialGlassUI.mat
│       └── M_MetaverseFloor.mat
├── Scripts/
│   ├── Core/
│   │   ├── OmniGenesis5DExperience.cs          # Master 5D Metaphor Controller
│   │   └── OmniGenesisClient.cs                # WebSocket & Gaussian buffer sync
│   ├── OS/
│   │   ├── Apps/
│   │   │   ├── Camera/                         # OmniAppCamera & DeviceCameraManager
│   │   │   ├── Gallery/                        # OmniAppGallery
│   │   │   ├── MatterWeave/                    # OmniAppMatterWeave
│   │   │   ├── MediaHub/                       # OmniAppMediaHub
│   │   │   ├── Multiverse/                     # OmniAppMultiverse
│   │   │   ├── Scenery/                        # OmniAppScenery
│   │   │   ├── Settings/                       # OmniAppSettings
│   │   │   ├── SpatialCanvas/                  # OmniAppSpatialCanvas
│   │   │   └── VideoReality/                   # OmniAppVideoReality & VideoRealityManager
│   │   ├── Core/
│   │   │   ├── OmniAppLauncherBox.cs           # Master on-screen Apps & 5D Controller Box
│   │   │   ├── OmniOSManager.cs                # Master OS Singleton & App Lifecycle
│   │   │   ├── OmniOSBootSequence.cs           # Holographic startup sequence
│   │   │   ├── SpatialAudioEngine.cs           # Procedural SFX synthesizer
│   │   │   ├── SpatialDock.cs                  # 3D floating curved dock
│   │   │   ├── SpatialTheme.cs                 # Styling, colors, material generator
│   │   │   └── SpatialWindow.cs                # Frosted glass 3D window
│   │   ├── Environment/
│   │   │   └── MetaverseEnvironmentManager.cs  # 360 Room, themes, lighting, pillars
│   │   └── Interaction/
│   │       ├── SpatialButton3D.cs              # 3D interactive button with haptics
│   │       ├── SpatialPointer.cs               # 2-pass prioritized laser pointer & WASD
│   │       └── SpatialSlider3D.cs              # 3D slider
│   └── Biometrics/
│       └── VestibularShieldPass.cs             # Optical counter-flow motion shield
├── Shaders/
│   ├── SpatialGlassRealistic.shader
│   ├── SpatialGlassUI.shader
│   ├── VideoRealityDome.shader
│   ├── CameraViewfinder.shader
│   ├── MetaverseFloor.shader
│   └── OmniGaussianURP.shader
└── StreamingAssets/
    └── Videos/
        └── onboarding_video_final.mp4          # Bundled 18.1MB video for standalone builds
```

---

## 6. INSTRUCTIONS FOR THE NEXT ASSISTANT / CHAT SESSION
When continuing this project in a new chat:
1. **Respect Platform Duality:** Always ensure features are 100% functional on PC Desktop (Mouse + Keyboard via `OmniAppLauncherBox` and WASD) AND on Meta Quest VR (OpenXR tracked controllers & laser pointer).
2. **Preserve Shader Inclusion:** Never create materials with raw `Shader.Find()` without using `SpatialTheme.CreateGlassMaterial` or `SpatialTheme.ApplyVisualProperties`, to prevent shader stripping in standalone builds.
3. **Preserve StreamingAssets:** All video and media assets must load via `Application.streamingAssetsPath`.
4. **Preserve Collider Cleanness:** Decorative backplates, screen quads, and frames must NEVER have colliders that intercept `SpatialPointer` raycasts.
