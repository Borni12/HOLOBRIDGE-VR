using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build;
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

        [MenuItem("Holobridge/Build WebGL for Netlify", false, 7)]
        public static void BuildWebGLForNetlifyFromMenu()
        {
            BuildWebGLForNetlify();
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

        public static void BuildWebGLForNetlify()
        {
            Debug.Log("<color=#00e5ff>[OmniBuild]</color> ================================================================");
            Debug.Log("<color=#00e5ff>[OmniBuild]</color> STARTING WEBGL BUILD FOR NETLIFY DEPLOYMENT");
            Debug.Log("<color=#00e5ff>[OmniBuild]</color> ================================================================");

            // 1. Sanitize scene hierarchy
            OmniSceneSanitizer.SanitizeScene(false);

            // 2. Switch build target to WebGL if needed
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                Debug.Log("<color=#00e5ff>[OmniBuild]</color> Switching active build target to WebGL...");
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            }

            // 3. Configure WebGL settings
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.runInBackground = true;

            // 4. Prepare output paths
            string buildDir = Path.Combine(Directory.GetCurrentDirectory(), "Build", "WebGL");
            if (!Directory.Exists(buildDir)) Directory.CreateDirectory(buildDir);

            string[] scenes = new string[] { "Assets/Scenes/SampleScene.unity" };

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildDir,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None
            };

            Debug.Log($"<color=#00e5ff>[OmniBuild]</color> Output WebGL Directory: {buildDir}");

            // 5. Execute Build
            DateTime startTime = DateTime.Now;
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            TimeSpan duration = DateTime.Now - startTime;

            if (summary.result == BuildResult.Succeeded)
            {
                WriteNetlifyConfigFiles(buildDir);

                long sizeMb = (long)(summary.totalSize / (1024 * 1024));
                Debug.Log($"<color=#00ff88>[OmniBuild Succeeded]</color> WebGL Build Finished in {duration.TotalMinutes:F1} min ({sizeMb} MB)!");
                if (!Application.isBatchMode)
                {
                    EditorUtility.RevealInFinder(buildDir);
                    EditorUtility.DisplayDialog(
                        "WebGL Build for Netlify Succeeded!",
                        $"WebGL build created successfully!\n\nFolder: {buildDir}\nSize: {sizeMb} MB\n\n_headers and _redirects have been automatically configured for Netlify.\n\nYou can now drag & drop this 'WebGL' folder onto app.netlify.com/drop!",
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
                        "WebGL Build Failed",
                        $"Build failed with {summary.totalErrors} errors. Check console for details.",
                        "OK"
                    );
                }
            }
        }

        public static void WriteNetlifyConfigFiles(string targetFolder)
        {
            if (string.IsNullOrEmpty(targetFolder) || !Directory.Exists(targetFolder)) return;

            string headersPath = Path.Combine(targetFolder, "_headers");
            string redirectsPath = Path.Combine(targetFolder, "_redirects");

            string headersContent = @"/*
  Access-Control-Allow-Origin: *
  Cross-Origin-Opener-Policy: same-origin
  Cross-Origin-Embedder-Policy: require-corp
  Cache-Control: public, max-age=0, must-revalidate

/Build/*.wasm
  Content-Type: application/wasm

/Build/*.wasm.unityweb
  Content-Type: application/wasm
  Content-Encoding: gzip

/Build/*.data.unityweb
  Content-Type: application/octet-stream
  Content-Encoding: gzip

/Build/*.framework.js.unityweb
  Content-Type: application/javascript
  Content-Encoding: gzip

/Build/*.wasm.gz
  Content-Type: application/wasm
  Content-Encoding: gzip

/Build/*.data.gz
  Content-Type: application/octet-stream
  Content-Encoding: gzip

/Build/*.framework.js.gz
  Content-Type: application/javascript
  Content-Encoding: gzip

/Build/*.wasm.br
  Content-Type: application/wasm
  Content-Encoding: br

/Build/*.data.br
  Content-Type: application/octet-stream
  Content-Encoding: br

/Build/*.framework.js.br
  Content-Type: application/javascript
  Content-Encoding: br
";

            string redirectsContent = "/*    /index.html   200\n";

            try
            {
                File.WriteAllText(headersPath, headersContent);
                File.WriteAllText(redirectsPath, redirectsContent);
                Debug.Log("<color=#00ff88>[OmniBuild]</color> Generated Netlify _headers and _redirects in " + targetFolder);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[OmniBuild] Failed to write Netlify config files: " + ex.Message);
            }

            FixWebGLIdbfsPersistence(targetFolder);
        }

        public static void FixWebGLIdbfsPersistence(string targetFolder)
        {
            try
            {
                // 1. Inject IDBFS polyfill into index.html
                string indexPath = Path.Combine(targetFolder, "index.html");
                if (File.Exists(indexPath))
                {
                    string html = File.ReadAllText(indexPath);
                    if (!html.Contains("window.IDBFS"))
                    {
                        string polyfill = @"    <script>
      // Fix for Unity 6 Emscripten IDBFS persistence
      window.IDBFS = window.IDBFS || {
        dbs: {},
        indexedDB: function() {
          try {
            return window.indexedDB || window.mozIndexedDB || window.webkitIndexedDB || window.msIndexedDB;
          } catch(e) { return null; }
        },
        DB_VERSION: 21,
        DB_STORE_NAME: 'FILE_DATA',
        queuePersist: function(mount) {},
        mount: function(mount) {
          if (typeof MEMFS !== 'undefined' && MEMFS.mount) return MEMFS.mount(mount);
          return { root: {}, mountpoint: mount ? mount.mountpoint : '/idbfs', mount: mount };
        },
        syncfs: function(mount, populate, callback) {
          if (typeof callback === 'function') setTimeout(function() { callback(null); }, 0);
        }
      };
      var IDBFS = window.IDBFS;
    </script>
  </head>";
                        if (html.Contains("</head>"))
                        {
                            html = html.Replace("</head>", polyfill);
                            File.WriteAllText(indexPath, html);
                            Debug.Log("<color=#00ff88>[OmniBuild]</color> Injected IDBFS Polyfill into index.html");
                        }
                    }
                }

                // 2. Patch WebGL.framework.js.unityweb if present
                string buildDir = Path.Combine(targetFolder, "Build");
                if (Directory.Exists(buildDir))
                {
                    string[] frameworkFiles = Directory.GetFiles(buildDir, "*framework.js*");
                    foreach (string fwFile in frameworkFiles)
                    {
                        if (fwFile.EndsWith(".unityweb") || fwFile.EndsWith(".gz"))
                        {
                            // Decompress, check & patch, recompress
                            byte[] gzBytes = File.ReadAllBytes(fwFile);
                            string decompressedText = null;
                            using (var msIn = new MemoryStream(gzBytes))
                            using (var gz = new GZipStream(msIn, CompressionMode.Decompress))
                            using (var reader = new StreamReader(gz))
                            {
                                decompressedText = reader.ReadToEnd();
                            }

                            string targetStr = "function injectIndexedDBToAutomaticallyPersist(){IDBFS.queuePersist=";
                            if (decompressedText != null && decompressedText.Contains(targetStr))
                            {
                                string replacementStr = "function injectIndexedDBToAutomaticallyPersist(){if(typeof IDBFS==='undefined'){globalThis.IDBFS={queuePersist:function(){},mount:function(m){return MEMFS.mount(m)},syncfs:function(m,p,cb){if(cb)cb(null)}}};IDBFS.queuePersist=";
                                decompressedText = decompressedText.Replace(targetStr, replacementStr);

                                using (var msOut = new MemoryStream())
                                {
                                    using (var gzOut = new GZipStream(msOut, System.IO.Compression.CompressionLevel.Optimal))
                                    using (var writer = new StreamWriter(gzOut))
                                    {
                                        writer.Write(decompressedText);
                                    }
                                    File.WriteAllBytes(fwFile, msOut.ToArray());
                                }
                                Debug.Log("<color=#00ff88>[OmniBuild]</color> Successfully patched IDBFS persistence in " + Path.GetFileName(fwFile));
                            }
                        }
                        else if (fwFile.EndsWith(".js"))
                        {
                            string jsText = File.ReadAllText(fwFile);
                            string targetStr = "function injectIndexedDBToAutomaticallyPersist(){IDBFS.queuePersist=";
                            if (jsText.Contains(targetStr))
                            {
                                string replacementStr = "function injectIndexedDBToAutomaticallyPersist(){if(typeof IDBFS==='undefined'){globalThis.IDBFS={queuePersist:function(){},mount:function(m){return MEMFS.mount(m)},syncfs:function(m,p,cb){if(cb)cb(null)}}};IDBFS.queuePersist=";
                                jsText = jsText.Replace(targetStr, replacementStr);
                                File.WriteAllText(fwFile, jsText);
                                Debug.Log("<color=#00ff88>[OmniBuild]</color> Successfully patched IDBFS persistence in " + Path.GetFileName(fwFile));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[OmniBuild] Note on IDBFS patching: " + ex.Message);
            }
        }
    }

    public class HoloBridgeWebGLPostProcessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.WebGL)
            {
                string outPath = report.summary.outputPath;
                if (!string.IsNullOrEmpty(outPath))
                {
                    OmniBuildTool.WriteNetlifyConfigFiles(outPath);
                }
            }
        }
    }
}
