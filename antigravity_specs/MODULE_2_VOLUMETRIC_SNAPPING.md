# Module 2 Specification: Volumetric Geometric Snapping

## Target Filename
`Assets/Scripts/Interaction/VolumetricSnapping.cs`

## Functional Requirements
1. Non-Allocating Spatial Detection:
   - Use `Physics.OverlapSphereNonAlloc` around the active tool tip to detect candidate anchor nodes on a specified `LayerMask` (`SnappingLayer`).
   - Pre-allocate the `Collider[]` buffer (size: 16) to avoid runtime heap allocations.

2. Distance-Weighted Vectorial Attraction:
   - If candidate nodes exist within `snapRadius` (default: 0.05m), calculate a weighted attraction vector toward the nearest valid anchor.
   - Implement an exponential decay or smoothstep curve: high magnetism near the center, smoothly releasing when the user pulls past `breakThreshold` (default: 0.08m).

3. Output API & Haptics:
   - Expose public method: `Vector3 GetSnappedPosition(Vector3 rawPosition)`.
   - Expose public property: `bool IsSnapped { get; }`.
   - Fire a single haptic pulse via the XR controller's `SendHapticImpulse(amplitude, duration)` only upon transition from unsnapped to snapped state.
