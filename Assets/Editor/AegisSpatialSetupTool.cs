using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace AegisSpatial.EditorTools
{
    /// <summary>
    /// AegisSpatial Editor Tools.
    /// Automatically cleans up dummy test objects (blue glow / red balls) 
    /// to keep the scene clean, open, and production-ready for gameplay.
    /// All text and logs are strictly English.
    /// </summary>
    public static class AegisSpatialSetupTool
    {
        [MenuItem("AegisSpatial/Clean All Test Objects & Blue Glow", false, 1)]
        [InitializeOnLoadMethod]
        public static void CleanTestObjects()
        {
            GameObject root = GameObject.Find("--- AEGIS_SPATIAL_SYSTEM ---");
            if (root != null)
            {
                UnityEngine.Object.DestroyImmediate(root);
                var activeScene = EditorSceneManager.GetActiveScene();
                if (activeScene.IsValid())
                {
                    EditorSceneManager.MarkSceneDirty(activeScene);
                    EditorSceneManager.SaveOpenScenes();
                }
                Debug.Log("[AegisSpatial] Cleaned test objects and blue glow. Scene is clean.");
            }
        }
    }
}
