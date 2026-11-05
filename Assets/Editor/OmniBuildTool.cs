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

        public static void BuildWindowsPlayer()
        {
            Debug.Log("<color=#00e5ff>[OmniBuild]</color> Starting Windows 64 Standalone Build...");

            // 1. Sanitize the active scene and ensure all components are wired
            OmniSceneSanitizer.SanitizeScene(false);

            // 2. Prepare build parameters
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
                options = BuildOptions.None
            };

            Debug.Log($"<color=#00e5ff>[OmniBuild]</color> Target: {exePath}");

            // 3. Execute Build
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"<color=#00ff88>[OmniBuild Succeeded]</color> Size: {summary.totalSize / 1024 / 1024} MB, Duration: {summary.totalTime.TotalSeconds:F1}s");
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Build Succeeded", $"Standalone Windows 64 build completed successfully!\n\nLocation: {exePath}\nSize: {summary.totalSize / 1024 / 1024} MB", "OK");
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
    }
}
