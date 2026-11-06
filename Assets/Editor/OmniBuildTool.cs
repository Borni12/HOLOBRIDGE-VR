using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace OmniOS.EditorTools
{
    public static class OmniBuildTool
    {
        [MenuItem("Holobridge/Build Windows 64 Standalone Player", false, 5)]
        public static void BuildWindowsPlayerFromMenu()
        {
            BuildWindowsPlayer();
        }

        [MenuItem("Holobridge/Build Meta Quest 3 Android APK", false, 6)]
        public static void BuildQuestApkFromMenu()
        {
            BuildQuestApk();
        }

        public static void BuildWindowsPlayer()
        {
            Debug.Log("<color=#00e5ff>[OmniBuild]</color> Starting Windows 64 Standalone Build (Universal PC Compatibility)...");

            // 1. Sanitize the active scene and ensure all components are wired
            OmniSceneSanitizer.SanitizeScene(false);

            // 2. Switch build target to Standalone Windows 64 if currently on Android or another target
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
            {
                Debug.Log("<color=#00e5ff>[OmniBuild]</color> Switching active build target to StandaloneWindows64...");
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
            }

            // 3. Configure Universal Player Settings for 100% Cross-PC Compatibility
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.runInBackground = true;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);

            // 4. Prepare build parameters
            string buildDir = Path.Combine(Directory.GetCurrentDirectory(), "Build");
            if (!Directory.Exists(buildDir))
            {
                Directory.CreateDirectory(buildDir);
            }

            string exePath = Path.Combine(buildDir, "SpatialSync_3DUI.exe");
            string[] scenes = new string[] { "Assets/Scenes/SampleScene.unity" };

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };

            Debug.Log($"<color=#00e5ff>[OmniBuild]</color> Target: {exePath}");

            // 5. Execute Build
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                long sizeMb = (long)(summary.totalSize / (1024 * 1024));
                Debug.Log($"<color=#00ff88>[OmniBuild Succeeded]</color> Size: {sizeMb} MB, Duration: {summary.totalTime.TotalSeconds:F1}s");
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Build Succeeded", $"Standalone Windows 64 build completed successfully!\n\nLocation: {exePath}\nSize: {sizeMb} MB", "OK");
                }
            }
            else
            {
                Debug.LogError($"<color=#ff4444>[OmniBuild Failed]</color> Result: {summary.result}, Errors: {summary.totalErrors}");
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Build Failed", $"Standalone build failed with {summary.totalErrors} errors. Check console for details.", "OK");
                }
            }
        }

        public static void BuildQuestApk()
        {
            Debug.Log("<color=#00e5ff>[OmniBuild]</color> ================================================================");
            Debug.Log("<color=#00e5ff>[OmniBuild]</color> STARTING META QUEST 3 ANDROID APK BUILD (ARM64 • Vulkan • OpenXR)");
            Debug.Log("<color=#00e5ff>[OmniBuild]</color> ================================================================");

            // 1. Ensure Embedded Android SDK/NDK/JDK are used
            EditorPrefs.SetBool("AndroidSdkRootUseEmbedded", true);
            EditorPrefs.SetBool("AndroidNdkRootUseEmbedded", true);
            EditorPrefs.SetBool("JdkUseEmbedded", true);

            // 2. Sanitize scene hierarchy
            OmniSceneSanitizer.SanitizeScene(false);

            // 3. Switch build target to Android if needed
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.Log("<color=#00e5ff>[OmniBuild]</color> Switching active build target to Android...");
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }

            // 4. Configure Meta Quest 3 Player Settings
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32; // Meta Quest OS requirement
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.tamjid.holobridgevr");
            PlayerSettings.productName = "HOLOBRIDGE VR";
            PlayerSettings.companyName = "Holobridge";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

            // 5. Prepare output paths
            string buildDir = Path.Combine(Directory.GetCurrentDirectory(), "Build");
            if (!Directory.Exists(buildDir)) Directory.CreateDirectory(buildDir);

            string apkPath = Path.Combine(buildDir, "HOLOBRIDGE_VR_Quest3.apk");
            string[] scenes = new string[] { "Assets/Scenes/SampleScene.unity" };

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            Debug.Log($"<color=#00e5ff>[OmniBuild]</color> Output APK: {apkPath}");

            // 6. Execute Build
            DateTime startTime = DateTime.Now;
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            TimeSpan duration = DateTime.Now - startTime;

            if (summary.result == BuildResult.Succeeded)
            {
                long sizeMb = (long)(summary.totalSize / (1024 * 1024));
                Debug.Log($"<color=#00ff88>[OmniBuild Succeeded]</color> Meta Quest 3 APK generated successfully!");
                Debug.Log($"<color=#00ff88>[OmniBuild Succeeded]</color> APK Path: {apkPath} ({sizeMb} MB) in {duration.TotalMinutes:F1} min");
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog(
                        "Meta Quest 3 APK Build Succeeded",
                        $"Meta Quest 3 APK Build Finished Successfully!\n\nLocation: {apkPath}\nSize: {sizeMb} MB\nDuration: {duration.TotalMinutes:F1} minutes\n\nReady for SideQuest / Meta Quest Developer Hub sideloading!",
                        "OK"
                    );
                }
            }
            else
            {
                Debug.LogError($"<color=#ff4444>[OmniBuild Failed]</color> Result: {summary.result}, Errors: {summary.totalErrors}");
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog(
                        "Meta Quest 3 Build Failed",
                        $"Build failed with {summary.totalErrors} errors. Check console or BuildLog.txt for details.",
                        "OK"
                    );
                }
            }
        }
    }
}
