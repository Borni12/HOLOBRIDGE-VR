using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using OmniOS.Core;
using OmniOS.Interaction;
using OmniOS.Environment;
using OmniOS.Apps.VideoReality;
using OmniOS.Apps.CameraApp;
using OmniGenesis.Core;

namespace OmniOS.EditorTools
{
    /// <summary>
    /// OmniAutoSaveTool - Professional Unity Editor Auto-Save & Full System Reload Engine.
    /// Provides:
    /// 1. Background Scene Auto-Save (Every 60s & prior to entering Play Mode).
    /// 2. One-Click Full Reload button in Unity Menu and Editor Window.
    /// 3. Complete auto-repair and serialization of all OmniOS and 5D systems in SampleScene.
    /// </summary>
    [InitializeOnLoad]
    public class OmniAutoSaveTool : EditorWindow
    {
        private static double nextAutoSaveTime = 0;
        private static double autoSaveInterval = 60.0; // Seconds
        private static bool isAutoSaveEnabled = true;
        private static DateTime lastSaveTimestamp = DateTime.Now;
        private static string lastSaveMessage = "Initialized";

        static OmniAutoSaveTool()
        {
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            nextAutoSaveTime = EditorApplication.timeSinceStartup + autoSaveInterval;
        }

        private static void OnEditorUpdate()
        {
            if (!isAutoSaveEnabled || Application.isPlaying) return;

            if (EditorApplication.timeSinceStartup >= nextAutoSaveTime)
            {
                TriggerAutoSave();
                nextAutoSaveTime = EditorApplication.timeSinceStartup + autoSaveInterval;
            }
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                // Auto-save before entering Play Mode to guarantee zero data loss
                TriggerAutoSave();
            }
        }

        public static void TriggerAutoSave()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.isDirty && !string.IsNullOrEmpty(activeScene.path))
            {
                EditorSceneManager.SaveScene(activeScene);
                lastSaveTimestamp = DateTime.Now;
                lastSaveMessage = $"Auto-Saved '{activeScene.name}' at {lastSaveTimestamp:HH:mm:ss}";
                Debug.Log($"<color=#00e5ff>[OmniAutoSave]</color> {lastSaveMessage}");
            }
        }

        [MenuItem("Holobridge/⚡ Full Reload & Auto-Save OmniOS Scene %#r", false, 1)]
        public static void ExecuteFullReloadAndAutoSave()
        {
            PerformFullReload(true);
        }

        [MenuItem("Holobridge/🛠️ OmniOS Auto-Save & System Control Center", false, 2)]
        public static void OpenWindow()
        {
            var win = GetWindow<OmniAutoSaveTool>("Omni Control Center");
            win.minSize = new Vector2(420, 480);
            win.Show();
        }

        public static void PerformFullReload(bool showDialog = false)
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(activeScene.path))
            {
                activeScene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            }

            Debug.Log("<color=#00e5ff>[OmniOS Full Reload]</color> Beginning full system rebuild & auto-save...");

            // 1. Resolve and Tag Main Camera
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                mainCam = UnityEngine.Object.FindAnyObjectByType<Camera>();
                if (mainCam != null)
                {
                    mainCam.gameObject.tag = "MainCamera";
                    if (!mainCam.gameObject.activeSelf) mainCam.gameObject.SetActive(true);
                }
            }

            // 2. Find or Create [OMNIOS_CORE] Master Root
            GameObject osRoot = GameObject.Find("[OMNIOS_CORE]");
            if (osRoot == null)
            {
                osRoot = new GameObject("[OMNIOS_CORE]");
                Undo.RegisterCreatedObjectUndo(osRoot, "Create OmniOS Core");
            }
            osRoot.transform.position = Vector3.zero;
            osRoot.transform.rotation = Quaternion.identity;

            // 3. Attach and Configure All 11 Core Components
            var osManager = EnsureComponent<OmniOSManager>(osRoot);
            var exp5D = EnsureComponent<OmniGenesis5DExperience>(osRoot);
            var launcherBox = EnsureComponent<OmniAppLauncherBox>(osRoot);
            var pointer = EnsureComponent<SpatialPointer>(osRoot);
            var dock = EnsureComponent<SpatialDock>(osRoot);
            var envManager = EnsureComponent<MetaverseEnvironmentManager>(osRoot);
            var vidManager = EnsureComponent<VideoRealityManager>(osRoot);
            var devCamManager = EnsureComponent<DeviceCameraManager>(osRoot);
            var camDevice = EnsureComponent<SpatialCameraDevice>(osRoot);
            var audioEng = EnsureComponent<SpatialAudioEngine>(osRoot);
            var bootSeq = EnsureComponent<OmniOSBootSequence>(osRoot);

            // 4. Find XR RightHand Controller in Scene Hierarchy
            var allTransforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
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

            // 5. Wire Serialized References
            if (mainCam != null)
            {
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

                SerializedObject soPointer = new SerializedObject(pointer);
                soPointer.FindProperty("headCameraTransform").objectReferenceValue = mainCam.transform;
                if (rightHand != null) soPointer.FindProperty("rightHandTransform").objectReferenceValue = rightHand;
                soPointer.ApplyModifiedProperties();

                SerializedObject soDock = new SerializedObject(dock);
                soDock.FindProperty("headCamera").objectReferenceValue = mainCam.transform;
                soDock.FindProperty("osManager").objectReferenceValue = osManager;
                soDock.ApplyModifiedProperties();

                SerializedObject soVid = new SerializedObject(vidManager);
                soVid.FindProperty("userHeadTransform").objectReferenceValue = mainCam.transform;
                soVid.ApplyModifiedProperties();

                SerializedObject so5D = new SerializedObject(exp5D);
                so5D.FindProperty("headCamera").objectReferenceValue = mainCam.transform;
                so5D.FindProperty("pointer").objectReferenceValue = pointer;
                so5D.ApplyModifiedProperties();
            }

            // 6. Disable legacy duplicate spawners
            var legacySpawners = UnityEngine.Object.FindObjectsByType<VRMarkerSpawner>(FindObjectsSortMode.None);
            foreach (var spawner in legacySpawners)
            {
                spawner.enabled = false;
                EditorUtility.SetDirty(spawner);
            }

            // 7. Auto-Save Scene
            EditorUtility.SetDirty(osRoot);
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            lastSaveTimestamp = DateTime.Now;
            lastSaveMessage = $"Full Reload Completed & Auto-Saved at {lastSaveTimestamp:HH:mm:ss}";
            Debug.Log($"<color=#00ff88>[OmniOS Full Reload]</color> ✅ Success! All systems serialized & scene auto-saved.");

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "OmniOS & 5D Full Reload Succeeded",
                    "All OmniOS and 5D flagship components (Reality-Peel, Matter-Weave, Multiverse, Avatar Sophia, Launcher Box, Dock, Pointer) have been completely reloaded and serialized.\n\nThe scene was AUTOMATICALLY SAVED.\nYou can now hit Play and test every shortcut (P, M, O, J, 1-9, Tab, Space, H, T)!",
                    "Awesome!"
                );
            }
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            var comp = target.GetComponent<T>();
            if (comp == null)
            {
                comp = Undo.AddComponent<T>(target);
            }
            return comp;
        }

        private void OnGUI()
        {
            GUILayout.Space(12);
            GUILayout.Label("<b><color=#00e5ff><size=16>⚡ OMNIOS & 5D CONTROL CENTER</size></color></b>", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Label("Unified System Reload, Component Diagnostics & Scene Auto-Save", EditorStyles.miniLabel);

            GUILayout.Space(14);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            isAutoSaveEnabled = EditorGUILayout.Toggle("Enable Background Auto-Save", isAutoSaveEnabled);
            autoSaveInterval = EditorGUILayout.Slider("Auto-Save Interval (Sec)", (float)autoSaveInterval, 15f, 300f);

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Status:", lastSaveMessage);
            EditorGUILayout.LabelField("Last Saved:", lastSaveTimestamp.ToString("HH:mm:ss"));
            EditorGUILayout.EndVertical();

            GUILayout.Space(16);

            // Master Reload Button
            GUI.backgroundColor = new Color(0.0f, 0.95f, 1.0f);
            if (GUILayout.Button("<b><size=13>🔄 FULL SYSTEM RELOAD & AUTO-SAVE SCENE</size></b>", GUILayout.Height(46)))
            {
                PerformFullReload(true);
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(12);
            EditorGUILayout.HelpBox(
                "Keyboard Shortcuts active in Play Mode:\n" +
                "• [P] Reality-Peel (2D to 360° Unprojection)\n" +
                "• [M] Matter-Weave (Cycles Stiffness: Rigid/Elastic/Fluid)\n" +
                "• [O] Multiverse Chronos (5 Time Orbs)\n" +
                "• [J] Avatar Sophia (Reach IK Handoff)\n" +
                "• [R / F5] Full Runtime Reload & Reset\n" +
                "• [Tab] Open/Hide Launchpad Menu\n" +
                "• [Space] Recenter Camera & Apps\n" +
                "• [1 to 9] Toggle Spatial OS Apps\n" +
                "• [T] Switch Scenery Theme\n" +
                "• [H] Zen / Clean View",
                MessageType.Info
            );
        }
    }
}
