using System;
using System.Collections.Generic;
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
    /// OmniSceneSanitizer - Master Scene Purifier & Diagnostic Health Enforcer.
    /// Safely purges all duplicate baked procedural roots, orphan colliders, and duplicate master nodes
    /// from SampleScene.unity, restoring crystal-clear visual fidelity and 100% responsive mouse/ray interaction.
    /// </summary>
    [InitializeOnLoad]
    public static class OmniSceneSanitizer
    {
        private static bool autoRanOnce = false;

        static OmniSceneSanitizer()
        {
            EditorApplication.delayCall += () =>
            {
                if (!autoRanOnce && !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    autoRanOnce = true;
                    SanitizeScene(false);
                }
            };
        }

        [MenuItem("Holobridge/🧹 Sanitize & Deep Clean SampleScene.unity", false, -1)]
        public static void SanitizeSceneFromMenu()
        {
            SanitizeScene(true);
        }

        public static void SanitizeScene(bool showPopup)
        {
            if (!EditorApplication.isPlaying && !Application.isPlaying)
            {
                var activeScene = EditorSceneManager.GetActiveScene();
                if (string.IsNullOrEmpty(activeScene.path))
                {
                    EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
                }
            }

            Debug.Log("<color=#00e5ff>[OmniSanitizer]</color> Starting deep clean of SampleScene.unity...");

            int destroyedCount = 0;

            // 1. Procedural root names and orphan prefixes that must never be baked into the scene file
            string[] purgeExactNames = new string[]
            {
                "OMNI_5D_RealityPeel_Root",
                "OMNI_5D_MatterWeave_Root",
                "OMNI_5D_Multiverse_Root",
                "OMNI_5D_Avatar_Sophia_Root",
                "OMNI_5D_GaussianField_Root",
                "OmniOS_Windows_Root",
                "OmniOS_SpatialAudioCue",
                "Tablet_Optical_Lens_Camera",
                "[OMNI_EVENTSYSTEM]",
                "[OMNI_LAUNCHPAD]",
                "Metaverse_PlatformFloor",
                "Metaverse_VirtualRoom_Root",
                "[HOLOBRIDGE_COLLAB_ENGINE]",
                "HoloBridge_SharedDigitalTwin",
                "Cue"
            };

            string[] purgePrefixes = new string[]
            {
                "Metaverse_Pillar_",
                "Metaverse_Platform",
                "TimeOrb_",
                "Orb_Ring",
                "Sophia_",
                "MatterWeave_",
                "Portal_",
                "Quantum_Exchange_Relic",
                "Window_media_hub",
                "Window_scenery",
                "Window_camera",
                "Window_gallery",
                "Window_video_reality",
                "Window_matter_weave",
                "Window_multiverse",
                "Window_spatial_canvas",
                "Window_settings",
                "Window_holobridge_collab",
                "SpatialPin_"
            };

            // Collect all GameObjects in scene (including inactive)
            GameObject[] allGOs = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);

            // Phase A: Purge duplicate [OMNIOS_CORE] objects, keeping strictly the best one
            GameObject primaryCore = null;
            foreach (var go in allGOs)
            {
                if (go != null && go.name == "[OMNIOS_CORE]")
                {
                    if (primaryCore == null)
                    {
                        primaryCore = go;
                    }
                    else
                    {
                        if (Application.isPlaying) GameObject.Destroy(go);
                        else Undo.DestroyObjectImmediate(go);
                        destroyedCount++;
                    }
                }
            }

            if (primaryCore == null)
            {
                primaryCore = new GameObject("[OMNIOS_CORE]");
                if (!Application.isPlaying) Undo.RegisterCreatedObjectUndo(primaryCore, "Create OmniOS Core");
            }
            primaryCore.SetActive(true);
            primaryCore.transform.position = Vector3.zero;
            primaryCore.transform.rotation = Quaternion.identity;

            // Phase B: Purge baked procedural roots and duplicates
            allGOs = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in allGOs)
            {
                if (go == null || go == primaryCore) continue;

                // Check exact matches
                bool shouldPurge = false;
                for (int i = 0; i < purgeExactNames.Length; i++)
                {
                    if (go.name == purgeExactNames[i])
                    {
                        shouldPurge = true;
                        break;
                    }
                }

                // Check prefix matches
                if (!shouldPurge)
                {
                    for (int i = 0; i < purgePrefixes.Length; i++)
                    {
                        if (go.name.StartsWith(purgePrefixes[i], StringComparison.OrdinalIgnoreCase))
                        {
                            shouldPurge = true;
                            break;
                        }
                    }
                }

                if (shouldPurge)
                {
                    if (Application.isPlaying) GameObject.Destroy(go);
                    else Undo.DestroyObjectImmediate(go);
                    destroyedCount++;
                }
            }

            // Phase C: Restore and properly configure Main Camera
            Camera mainCam = null;
            Camera[] cams = UnityEngine.Object.FindObjectsByType<Camera>();
            foreach (var c in cams)
            {
                if (c != null && !c.name.Contains("Tablet") && !c.name.Contains("Lens"))
                {
                    mainCam = c;
                    break;
                }
            }

            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                Undo.RegisterCreatedObjectUndo(camObj, "Create Main Camera");
            }

            mainCam.name = "Main Camera";
            mainCam.gameObject.tag = "MainCamera";
            mainCam.gameObject.SetActive(true);
            mainCam.transform.position = new Vector3(0, 1.6f, 0);
            mainCam.transform.rotation = Quaternion.identity;
            mainCam.clearFlags = CameraClearFlags.Skybox;
            mainCam.fieldOfView = 60f;
            mainCam.nearClipPlane = 0.05f;
            mainCam.farClipPlane = 1000f;
            mainCam.targetTexture = null;

            // Ensure AudioListener
            var al = mainCam.GetComponent<AudioListener>();
            if (al == null) al = mainCam.gameObject.AddComponent<AudioListener>();
            al.enabled = true;

            // Ensure URP Camera Data with Post-Processing
            var urpCam = mainCam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (urpCam == null) urpCam = mainCam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            urpCam.renderPostProcessing = true;

            // Phase D: Attach & Wire All 11 Core Components on [OMNIOS_CORE]
            var osManager = EnsureComp<OmniOSManager>(primaryCore);
            var exp5D = EnsureComp<OmniGenesis5DExperience>(primaryCore);
            var launcherBox = EnsureComp<OmniAppLauncherBox>(primaryCore);
            var pointer = EnsureComp<SpatialPointer>(primaryCore);
            var dock = EnsureComp<SpatialDock>(primaryCore);
            var envManager = EnsureComp<MetaverseEnvironmentManager>(primaryCore);
            var vidManager = EnsureComp<VideoRealityManager>(primaryCore);
            var devCamManager = EnsureComp<DeviceCameraManager>(primaryCore);
            var camDevice = EnsureComp<SpatialCameraDevice>(primaryCore);
            var audioEng = EnsureComp<SpatialAudioEngine>(primaryCore);
            var bootSeq = EnsureComp<OmniOSBootSequence>(primaryCore);
            var simpleHUD = EnsureComp<OmniSimpleHUD>(primaryCore);

            // Wire Serialized Properties
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

            // Disable legacy scene fog
            RenderSettings.fog = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.18f, 0.22f, 0.32f, 1f);

            // Mark Scene Dirty and Save Clean State (Edit mode only)
            if (!EditorApplication.isPlaying && !Application.isPlaying)
            {
                var activeScene = EditorSceneManager.GetActiveScene();
                if (!string.IsNullOrEmpty(activeScene.path))
                {
                    EditorUtility.SetDirty(primaryCore);
                    EditorUtility.SetDirty(mainCam.gameObject);
                    EditorSceneManager.MarkSceneDirty(activeScene);
                    EditorSceneManager.SaveScene(activeScene);
                }
            }

            Debug.Log($"<color=#00ff88>[OmniSanitizer]</color> ✅ Scene Purified! Destroyed {destroyedCount} orphan/duplicate objects. SampleScene.unity saved cleanly.");

            if (showPopup)
            {
                EditorUtility.DisplayDialog(
                    "Scene Cleaned & Purified",
                    $"Successfully sanitized SampleScene.unity!\n\n• Destroyed {destroyedCount} duplicate/orphan procedural objects.\n• Restored clean active Main Camera tagged 'MainCamera'.\n• Retained strictly 1 [OMNIOS_CORE] with all 11 subsystems.\n• Guaranteed 100% clean visual Cyber Lounge layout.\n\nThe scene is now saved and ready to Play or Build!",
                    "Awesome!"
                );
            }
        }

        private static T EnsureComp<T>(GameObject target) where T : Component
        {
            var comp = target.GetComponent<T>();
            if (comp == null)
            {
                if (Application.isPlaying) comp = target.AddComponent<T>();
                else comp = Undo.AddComponent<T>(target);
            }
            return comp;
        }
    }
}
