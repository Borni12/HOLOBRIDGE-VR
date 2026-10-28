# Module 1 Specification: Bi-Manual Multi-Scale Interaction

## Target Filename
`Assets/Scripts/Interaction/SpatialTransformManager.cs`

## Functional Requirements
1. Coordinate Frame Mapping:
   - Non-Dominant (Left) Hand: Acts as the reference anchor/bounding workspace frame.
   - Dominant (Right) Hand: Holds the precision pointer/stylus.
   - Calculate and map the dominant tool's position and rotation relative to the non-dominant hand's local coordinate frame using `Transform.InverseTransformPoint` and `Transform.TransformPoint`.

2. Dynamic Multi-Scale Precision Scaling:
   - Measure Euclidean distance between the left and right controller positions.
   - When distance drops below `precisionDistanceThreshold` (default: 0.25m), dynamically scale down the right hand's translation delta by an adjustable scaling factor (1.0x down to 0.1x). This enables sub-millimeter precision for fine 3D details.
   - Include a grip-triggered workspace lock (`InputActionProperty`) that freezes the anchor in world space while maintaining relative precision scaling.

3. Input & Visual Feedback:
   - Utilize Unity New Input System `InputActionProperty` for tracking position, rotation, and grip.
   - Implement `OnDrawGizmos()` to render debug coordinate axes for both frames.
   - Expose serialized parameters for `minDistance`, `maxScaleFactor`, and smoothing lerp rates.
