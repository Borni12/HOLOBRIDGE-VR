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

        [MenuItem("Holobridge/🥽 Toggle VR Headset Simulator Mode (No Headset) %v", false, 3)]
        public static void ToggleVRSimulatorFromMenu()
        {
            if (Application.isPlaying)
            {
                if (OmniVRHeadsetSimulator.Instance != null)
                {
                    OmniVRHeadsetSimulator.Instance.CycleNextMode();
                }
                else
                {
                    var sim = UnityEngine.Object.FindAnyObjectByType<OmniVRHeadsetSimulator>();
                    if (sim != null) sim.CycleNextMode();
                }
            }
            else
            {
                EditorUtility.DisplayDialog("VR Headset Simulator", 
                    "Press Play in Unity, then press 'V' key or click [🥽 VR Headset View] on screen to test the stereoscopic VR headset dual-eye view without requiring any physical VR headset!", 
                    "Got It");
            }
        }

        public static void PerformFullReload(bool showDialog = false)
        {
            OmniSceneSanitizer.SanitizeScene(showDialog);
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
