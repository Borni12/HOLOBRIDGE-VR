using UnityEditor;
using UnityEngine;
using OmniOS.Core;
using OmniOS.Apps.VideoReality;
using OmniOS.Apps.CameraApp;
using OmniOS.Interaction;

namespace OmniOS.EditorTools
{
    public static class OmniOSSetupTool
    {
        [MenuItem("Holobridge/🔧 Legacy Setup OmniOS in Current Scene", false, 10)]
        public static void SetupOmniOS()
        {
            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(activeScene.path))
            {
                activeScene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            }

            // Find or create OmniOS Master Root
            GameObject osRoot = GameObject.Find("[OMNIOS_CORE]");
            if (osRoot == null)
            {
                osRoot = new GameObject("[OMNIOS_CORE]");
                Undo.RegisterCreatedObjectUndo(osRoot, "Create OmniOS Core");
            }

            // 1. OmniOSManager
            var osManager = osRoot.GetComponent<OmniOSManager>();
            if (osManager == null)
            {
                osManager = Undo.AddComponent<OmniOSManager>(osRoot);
            }

            // 2. VideoRealityManager
            var vidManager = osRoot.GetComponent<VideoRealityManager>();
            if (vidManager == null)
            {
                vidManager = Undo.AddComponent<VideoRealityManager>(osRoot);
            }

            // 3. SpatialPointer
            var pointer = osRoot.GetComponent<SpatialPointer>();
            if (pointer == null)
            {
                pointer = Undo.AddComponent<SpatialPointer>(osRoot);
            }

            // 4. SpatialCameraDevice
            var camDevice = osRoot.GetComponent<SpatialCameraDevice>();
            if (camDevice == null)
            {
                camDevice = Undo.AddComponent<SpatialCameraDevice>(osRoot);
            }

            // 5. MetaverseEnvironmentManager (Metaverse Virtual Room Lounge)
            var envManager = osRoot.GetComponent<OmniOS.Environment.MetaverseEnvironmentManager>();
            if (envManager == null)
            {
                envManager = Undo.AddComponent<OmniOS.Environment.MetaverseEnvironmentManager>(osRoot);
            }

            // 6. DeviceCameraManager (Physical Hardware Webcam Access)
            var devCamManager = osRoot.GetComponent<OmniOS.Apps.CameraApp.DeviceCameraManager>();
            if (devCamManager == null)
            {
                devCamManager = Undo.AddComponent<OmniOS.Apps.CameraApp.DeviceCameraManager>(osRoot);
            }

            // 7. SpatialAudioEngine (Procedural SFX Synthesizer)
            var audioEng = osRoot.GetComponent<SpatialAudioEngine>();
            if (audioEng == null)
            {
                audioEng = Undo.AddComponent<SpatialAudioEngine>(osRoot);
            }

            // 8. OmniOSBootSequence (Cinematic Startup Sequence)
            var bootSeq = osRoot.GetComponent<OmniOSBootSequence>();
            if (bootSeq == null)
            {
                bootSeq = Undo.AddComponent<OmniOSBootSequence>(osRoot);
            }

            // 9. SpatialDock
            var dock = osRoot.GetComponent<SpatialDock>();
            if (dock == null)
            {
                dock = Undo.AddComponent<SpatialDock>(osRoot);
            }

            // 10. Omni-Genesis 5D Neural Graphics & Extreme 3DUI Master Engine
            var exp5D = osRoot.GetComponent<OmniGenesis.Core.OmniGenesis5DExperience>();
            if (exp5D == null)
            {
                exp5D = Undo.AddComponent<OmniGenesis.Core.OmniGenesis5DExperience>(osRoot);
            }

            // 11. Universal Apps & 5D Master Launcher Box (PC Desktop & VR Controller)
            var launcherBox = osRoot.GetComponent<OmniAppLauncherBox>();
            if (launcherBox == null)
            {
                launcherBox = Undo.AddComponent<OmniAppLauncherBox>(osRoot);
            }

            // Find RightHand Controller in XR Origin hierarchy
            var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            Transform rightHand = null;
            foreach (var t in allTransforms)
            {
                string n = t.name.ToLower();
                if ((n.Contains("righthand") || n.Contains("right controller") || n.Contains("right hand")) && !n.Contains("direct"))
                {
                    rightHand = t;
                    break;
                }
            }

            // Wire Camera references
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                // SerializedObject for OmniOSManager
                SerializedObject soManager = new SerializedObject(osManager);
                soManager.FindProperty("userHead").objectReferenceValue = mainCam.transform;
                soManager.FindProperty("cameraDevice").objectReferenceValue = camDevice;
                soManager.FindProperty("deviceCameraManager").objectReferenceValue = devCamManager;
                soManager.FindProperty("metaverseEnv").objectReferenceValue = envManager;
                soManager.FindProperty("bootSequence").objectReferenceValue = bootSeq;
                soManager.FindProperty("spatialDock").objectReferenceValue = dock;
                soManager.FindProperty("spatialPointer").objectReferenceValue = pointer;
                soManager.FindProperty("videoRealityManager").objectReferenceValue = vidManager;
                soManager.ApplyModifiedProperties();

                // SerializedObject for SpatialPointer
                SerializedObject soPointer = new SerializedObject(pointer);
                soPointer.FindProperty("headCameraTransform").objectReferenceValue = mainCam.transform;
                if (rightHand != null)
                {
                    soPointer.FindProperty("rightHandTransform").objectReferenceValue = rightHand;
                }
                soPointer.ApplyModifiedProperties();

                // SerializedObject for SpatialDock
                SerializedObject soDock = new SerializedObject(dock);
                soDock.FindProperty("headCamera").objectReferenceValue = mainCam.transform;
                soDock.FindProperty("osManager").objectReferenceValue = osManager;
                soDock.ApplyModifiedProperties();

                // SerializedObject for VideoRealityManager
                SerializedObject soVid = new SerializedObject(vidManager);
                soVid.FindProperty("userHeadTransform").objectReferenceValue = mainCam.transform;
                soVid.ApplyModifiedProperties();
            }

            // Disable legacy VRMarkerSpawner to prevent input collisions
            var legacySpawners = Object.FindObjectsByType<VRMarkerSpawner>(FindObjectsSortMode.None);
            foreach (var spawner in legacySpawners)
            {
                spawner.enabled = false;
                EditorUtility.SetDirty(spawner);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);
            Debug.Log("<color=cyan>[OmniOS Pro]</color> Successfully configured OmniOS Core with Spatial Dock, Pointer, Apps, and Camera!");
        }

        [MenuItem("Holobridge/Build OmniOS Standalone (No Auto-Run)", false, 3)]
        public static void BuildStandalone()
        {
            SetupOmniOS();

            // Ensure StreamingAssets video is bundled
            string srcVideo = "Assets/StreamingAssets/Videos/onboarding_video_final.mp4";
            if (!System.IO.File.Exists(srcVideo))
            {
                string oldVideo = "Assets/VRTemplateAssets/Videos/onboarding_video_final.mp4";
                if (System.IO.File.Exists(oldVideo))
                {
                    System.IO.Directory.CreateDirectory("Assets/StreamingAssets/Videos");
                    System.IO.File.Copy(oldVideo, srcVideo, true);
                    AssetDatabase.Refresh();
                }
            }

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = new[] { "Assets/Scenes/SampleScene.unity" };
            buildPlayerOptions.locationPathName = "Build/SpatialSync_3DUI.exe";
            buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
            buildPlayerOptions.options = BuildOptions.None;

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            Debug.Log($"<color=cyan>[OmniOS Pro]</color> Build finished with result: {report.summary.result}, errors: {report.summary.totalErrors}");
        }

        [MenuItem("Holobridge/Build and Run OmniOS Standalone", false, 2)]
        public static void BuildAndRunStandalone()
        {
            SetupOmniOS();

            // Ensure StreamingAssets video is bundled
            string srcVideo = "Assets/StreamingAssets/Videos/onboarding_video_final.mp4";
            if (!System.IO.File.Exists(srcVideo))
            {
                string oldVideo = "Assets/VRTemplateAssets/Videos/onboarding_video_final.mp4";
                if (System.IO.File.Exists(oldVideo))
                {
                    System.IO.Directory.CreateDirectory("Assets/StreamingAssets/Videos");
                    System.IO.File.Copy(oldVideo, srcVideo, true);
                    AssetDatabase.Refresh();
                }
            }

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = new[] { "Assets/Scenes/SampleScene.unity" };
            buildPlayerOptions.locationPathName = "Build/SpatialSync_3DUI.exe";
            buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
            buildPlayerOptions.options = BuildOptions.AutoRunPlayer;

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            Debug.Log($"<color=cyan>[OmniOS Pro]</color> Build finished with result: {report.summary.result}");
        }
    }
}
