# Module 3 Specification: Dynamic Bio-Adaptive Rest Frame

## Target Filename
`Assets/Scripts/Comfort/AdaptiveRestFrame.cs`

## Functional Requirements
1. Motion & Inertial Tracking:
   - Sample the Main XR Camera transform in `Update()`.
   - Calculate instantaneous angular velocity (deg/sec) using `Quaternion.Angle` over `Time.deltaTime`.
   - Calculate instantaneous linear velocity (m/s).

2. Inertially Anchored Visual Rest Grid:
   - Target an overlay grid mesh or screen-space vignette.
   - The Rest Frame must maintain an artificial horizon / earth-referenced orientation rather than being rigidly attached to head roll, reducing conflict between visual and vestibular cues.
   - When angular velocity exceeds `angularThreshold` (default: 40 deg/sec) or linear velocity exceeds `linearThreshold` (default: 1.5 m/s), smoothly lerp the material's alpha from 0.0 to `maxAlpha` (default: 0.65).
   - When motion drops below thresholds, decay alpha back to 0.0.

3. Standalone Optimization:
   - Cache material property IDs (`Shader.PropertyToID("_Alpha")`).
   - Use `MaterialPropertyBlock` or an instantiated runtime material instance cached in `Awake()` to avoid creating new materials during execution.
