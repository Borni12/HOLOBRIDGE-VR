using UnityEngine;
using Unity.Mathematics;
using System;

namespace OmniGenesis.Core
{
    public enum OmniGestureState { NONE, REALITY_PEEL, MATTER_WEAVE, CHRONO_AURA, JOINT_GAZE_HANDOFF }
    public enum OmniMaterialType { Rigid = 0, Elastic = 1, Fluid = 2, AvatarSkin = 3 }

    public interface IOmniInputSource
    {
        Pose HeadPose { get; }
        Pose LeftHandPose { get; }
        Pose RightHandPose { get; }
        Ray GazeRay { get; }
        float EyelidAperture { get; }
        OmniGestureState ActiveGesture { get; }
    }

    public static class OmniEventBus
    {
        public static event Action<OmniGestureState> OnGestureStateChanged;
        public static event Action<float> OnTimeScrubDelta;
        public static event Action<int, OmniMaterialType> OnMaterialTypeOverride;
        public static event Action<Vector3> OnAvatarHandTargetUpdated;

        public static void PublishGesture(OmniGestureState state) => OnGestureStateChanged?.Invoke(state);
        public static void PublishTimeScrub(float delta) => OnTimeScrubDelta?.Invoke(delta);
        public static void PublishMaterial(int splatIdx, OmniMaterialType mat) => OnMaterialTypeOverride?.Invoke(splatIdx, mat);
        public static void PublishAvatarTarget(Vector3 target) => OnAvatarHandTargetUpdated?.Invoke(target);
    }
}
