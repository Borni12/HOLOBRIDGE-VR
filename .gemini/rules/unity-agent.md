# Unity Autonomous Developer Directives - HOLOBRIDGE-VR

### Environment & Engine
- Project Root: `F:\VR Projects\HOLOBRIDGE-VR`
- Unity Executable: `C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe` (Junction linked to `F:\Unity\Hub\Editor`)
- Unity Version: Unity 6 (6000.6.5f1)
- Render Pipeline: Universal Render Pipeline (URP)
- Core Packages: Photon Fusion 2, XR Interaction Toolkit (v3.x), Unity New Input System

### Autonomous Debugging & Execution
- Live Console Log: `F:\VR Projects\HOLOBRIDGE-VR\Logs\Editor.log`
- Compiler Errors: Always inspect `Logs\Editor.log` for compilation issues or runtime exceptions.
- Zero-LINQ Target: Avoid LINQ in hot paths (`Update`, `FixedUpdateNetwork`) for Meta Quest 90 FPS locked performance.
- Language: Keep all code, UI labels, Inspector attributes, and debug logs 100% in English.
