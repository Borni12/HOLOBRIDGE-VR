using System;
using UnityEngine;
using OmniOS.Core;
using OmniGenesis.Core;

namespace OmniOS.Apps.MatterWeave
{
    /// <summary>
    /// OmniAppMatterWeave - 5D Continuum Physics Lab Window for OmniOS.
    /// Provides interactive modulation of material stiffness k in [Rigid, Elastic, Fluid, Skin]
    /// with real-time holographic 3D mesh feedback.
    /// </summary>
    [RequireComponent(typeof(SpatialWindow))]
    public class OmniAppMatterWeave : MonoBehaviour
    {
        public enum MaterialState
        {
            Fluid = 0,
            ElasticRubber = 1,
            RigidMetal = 2,
            OrganicSkin = 3
        }

        [Header("State")]
        [SerializeField] private MaterialState currentState = MaterialState.ElasticRubber;
        [SerializeField] private float stiffness = 1.0f;

        // Visual References
        private SpatialWindow spatialWindow;
        private GameObject holoShape;
        private Material holoMaterial;
        private OmniGenesisClient client;

        private void Awake()
        {
            spatialWindow = GetComponent<SpatialWindow>();
            spatialWindow.SetDimensions(1.1f, 0.85f);
            client = UnityEngine.Object.FindAnyObjectByType<OmniGenesisClient>();

            BuildHologram();
        }

        private void BuildHologram()
        {
            holoShape = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            holoShape.name = "MatterWeave_HoloOrb";
            holoShape.transform.SetParent(spatialWindow.ContentRoot, false);
            holoShape.transform.localPosition = new Vector3(0, 0.05f, -0.15f);
            holoShape.transform.localScale = Vector3.one * 0.28f;

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            holoMaterial = new Material(unlitShader);
            holoShape.GetComponent<Renderer>().material = holoMaterial;
            UpdateVisuals();
        }

        private void Update()
        {
            if (holoShape == null) return;

            // Idle rotation and breathing/jiggle based on stiffness
            holoShape.transform.Rotate(Vector3.up, 24f * Time.deltaTime, Space.World);

            float time = Time.time;
            if (currentState == MaterialState.Fluid)
            {
                float wave = Mathf.Sin(time * 6f) * 0.08f;
                holoShape.transform.localScale = new Vector3(0.28f + wave, 0.28f - wave, 0.28f + wave * 0.5f);
            }
            else if (currentState == MaterialState.ElasticRubber)
            {
                float bounce = Mathf.Sin(time * 3f) * 0.03f;
                holoShape.transform.localScale = new Vector3(0.28f + bounce, 0.28f - bounce * 0.5f, 0.28f + bounce);
            }
            else
            {
                holoShape.transform.localScale = Vector3.one * 0.28f;
            }
        }

        public void SetState(MaterialState newState)
        {
            currentState = newState;
            stiffness = (float)newState;

            if (client != null)
            {
                client.MaterialStiffness = stiffness;
                client.IsMatterWeaveActive = true;
            }

            UpdateVisuals();
            SpatialTheme.PlaySpatialTone(transform.position, 800f + (int)newState * 250f);
        }

        private void UpdateVisuals()
        {
            if (holoMaterial == null) return;

            switch (currentState)
            {
                case MaterialState.Fluid:
                    holoMaterial.color = new Color(0.0f, 0.65f, 1.0f, 0.75f);
                    break;
                case MaterialState.ElasticRubber:
                    holoMaterial.color = new Color(0.18f, 0.92f, 0.45f, 0.9f);
                    break;
                case MaterialState.RigidMetal:
                    holoMaterial.color = new Color(0.85f, 0.88f, 0.92f, 1.0f);
                    break;
                case MaterialState.OrganicSkin:
                    holoMaterial.color = new Color(1.0f, 0.65f, 0.55f, 0.95f);
                    break;
            }
        }

        private void OnGUI()
        {
            if (!OmniOSManager.Show2DOverlay) return;
            if (!spatialWindow.IsOpen || spatialWindow.IsMinimized) return;

            GUILayout.BeginArea(new Rect(Screen.width - 340, 20, 320, 240), GUI.skin.box);
            GUILayout.Label("<b>⚛️ MATTER-WEAVE PHYSICS LAB</b>");
            GUILayout.Space(6);

            GUILayout.Label($"Active Continuum State: <b><color=cyan>{currentState}</color></b>");
            GUILayout.Label($"Stiffness Coefficient (k): {stiffness:F2}");

            GUILayout.Space(6);
            if (GUILayout.Button("Fluid / Liquid (k = 0)", GUILayout.Height(28))) SetState(MaterialState.Fluid);
            if (GUILayout.Button("Elastic Rubber (k = 1)", GUILayout.Height(28))) SetState(MaterialState.ElasticRubber);
            if (GUILayout.Button("Rigid Metal (k = 2)", GUILayout.Height(28))) SetState(MaterialState.RigidMetal);
            if (GUILayout.Button("Organic Skin (k = 3)", GUILayout.Height(28))) SetState(MaterialState.OrganicSkin);

            GUILayout.EndArea();
        }
    }
}
