# OMNI-GENESIS 5D: Extreme 3DUI & Photorealistic Neural Graphics Architecture
## Master Blueprint, Current Project State & Continuity Guide

---

## 1. Project Overview & Meta Context
- **Project Name:** OMNI-GENESIS 5D (Spatial Metaverse OS & Neural Reality Lifting)
- **Target Platforms:** Meta Quest 3 / Quest Pro (OpenXR Standalone APK) & High-End PC VR (Windows Standalone)
- **Local Project Path:** `F:\VR Projects\HOLOBRIDGE-VR`
- **Unity Version:** Unity 6 (`6000.6.5f1`), Universal Render Pipeline (URP 17.6.0)
- **Primary Scene:** `Assets/Scenes/SampleScene.unity`

---

## 2. Core Technical Architecture & Novel 3DUI Metaphors

1. **Reality-Peel (Bimanual Projective Unprojection):**
   - 2D casual media (photos/videos) lifted into full stereoscopic 360° 3D Gaussian Splatting space.
   - User grabs the 2D frame with both hands and pulls it over their head to expand flat camera rays into a 3D volumetric world.
2. **Sub-Millimeter Micro-Pinch & Matter-Weave (Continuum Physics 3DUI):**
   - Eye-gaze cone selection on 3D Gaussian clusters.
   - Non-dominant wrist rotation dynamically modulates material stiffness: Rigid $\to$ Elastic Rubber $\to$ Fluid/Viscous $\to$ Human Skin.
   - Dominant hand performs 6-DoF sculpting and physical throwing simulated via GPU-driven Material Point Method (MPM) compute shaders.
3. **Multiverse Forking & Temporal Lens (5 Predictive Futures):**
   - Bimanual pull gesture halts time and spawns 5 floating, predictive toroidal Multiverse Orbs.
   - Orbs display 5 alternate physics trajectories over $t+1\text{s}$ to $t+5\text{s}$.
   - Pulling an orb to the headset commits the timeline.
4. **Joint-Gaze Cooperative Handoff (Photorealistic Conversational Avatar):**
   - Drivable 3D Gaussian human avatar with natural eye-blink, gaze saccades, and micro-expressions.
   - Bayesian intention scoring predicts object transfers; avatar reaches out with real-time Inverse Kinematics (IK) to physically take objects.
5. **Vestibular Counter-Flow Shield (Zero Cybersickness):**
   - Dynamically modulates peripheral optical flow velocity with 256 counter-motion reference particles outside a $25^\circ$ foveal safe-zone to eliminate vection and motion sickness.

---

## 3. Current Implementation State (Already Built in Unity 6)

### A. Core Spatial Operating System (`OmniOS Pro v2.5` - Meta Horizon OS Style)
- `Assets/Scripts/OS/Core/OmniOSManager.cs`: Master kernel managing 9 spatial applications, window lifecycle, Clean View (H), Re-centering (Space), Scenery switcher (T), and audio.
- `Assets/Scripts/OS/Core/SpatialDock.cs`: Meta Horizon OS style curved frosted glass dock floating at eye/chest level (`y = -0.22m`, 12° upward gaze tilt). Contains 11 glowing 3D buttons.
- `Assets/Scripts/OS/Core/SpatialWindow.cs`: VisionOS/Quest 3 frosted glass window with 3D header controls (Close, Minimize, Pin) and bottom pill grab handle.
- `Assets/Scripts/OS/Core/SpatialAudioEngine.cs`: Zero-dependency procedural audio synthesizer (Boot chime, window swooshes, hover ticks, mechanical clicks, camera shutter, ambient cosmic drone).
- `Assets/Scripts/OS/Core/OmniOSBootSequence.cs`: Cinematic 3D holographic boot sequence with glowing jewel, rotating orbit rings, and harmonic chord swell.

### B. Universal 3D Interaction Pointer & Desktop Navigation
- `Assets/Scripts/OS/Interaction/SpatialPointer.cs`:
  - Automatically auto-detects Meta Quest 3 tracked controller vs. Desktop Mouse raycast.
  - Holographic laser beam with luminous bloom reticle dot.
  - Instantaneous 0-latency click execution via `pressedButton` (100% reliable click registration).
  - Desktop Controls: Right-Click Drag for 360° mouse look; `W`, `A`, `S`, `D` for walking around the Metaverse lounge; `Space` for gaze re-centering.
- `Assets/Scripts/OS/Interaction/SpatialButton3D.cs`: Physical 3D button with tactile depth depression (-0.014m), 1.08x hover scaling, luminous neon glow, and acoustic ticks.
- `Assets/Scripts/OS/Interaction/SpatialSlider3D.cs`: 3D draggable slider track and glowing thumb bead.

### C. Metaverse Room Environment & Flagship Applications
- `Assets/Scripts/OS/Environment/MetaverseEnvironmentManager.cs`: Expansive 14m circular observation platform with neon radial grid, 8 architectural holographic pillars, ambient point light, floating motes, and 3 scenery themes (Cyber Penthouse, Cosmic Nebula, Sunset Horizon).
- `Assets/Scripts/OS/Apps/MediaHub/OmniAppMediaHub.cs`: Opens prominently on boot. Features YouTube player, Facebook portal, Web Browser launcher, and Android app switchers.
- `Assets/Scripts/OS/Apps/Camera/OmniAppCamera.cs` & `SpatialCameraDevice.cs`: 6-DoF spatial camera + real hardware webcam streaming (`WebCamTexture`) + Reality Lift.
- `Assets/Scripts/OS/Apps/Scenery/OmniAppScenery.cs`: Metaverse room theme explorer and cosmic ambient drone toggle.
- `Assets/Scripts/OS/Apps/Gallery/OmniAppGallery.cs`: Spatial photo and video album.
- `Assets/Scripts/OS/Apps/VideoReality/VideoRealityManager.cs`: 360° video dome player with depth relief.

---

## 4. Continuity & Handover Instructions for New Chats

When starting a new conversation:
1. Open this workspace or mention `F:\VR Projects\HOLOBRIDGE-VR`.
2. Reference this file: `OMNI_GENESIS_5D_BLUEPRINT.md`.
3. Inform the agent of the next step you wish to work on (e.g., Python Backend 5D Gaussian server, MPM compute shader, or Dr. Isayas Berhe Adhanom faculty proposal).
