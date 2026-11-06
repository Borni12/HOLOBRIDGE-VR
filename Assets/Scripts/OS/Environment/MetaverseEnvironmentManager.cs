using System;
using UnityEngine;

namespace OmniOS.Environment
{
    /// <summary>
    /// MetaverseEnvironmentManager - Procedural High-Fidelity Metaverse Room & Cyber Lounge.
    /// Creates an expansive, architectural VR Metaverse environment:
    /// - High-tech reflective observation platform with neon radial grid
    /// - 8 architectural boundary pillars with ambient volumetric light
    /// - Floating atmospheric dust particles
    /// - 3 dynamic environment themes (Cyber Penthouse, Cosmic Nebula, Sunset Horizon)
    /// </summary>
    [DisallowMultipleComponent]
    public class MetaverseEnvironmentManager : MonoBehaviour
    {
        public enum EnvironmentTheme
        {
            CyberPenthouse = 0,
            CosmicNebula = 1,
            SunsetHorizon = 2
        }

        [Header("Theme Configuration")]
        [SerializeField] private EnvironmentTheme currentTheme = EnvironmentTheme.CyberPenthouse;
        [SerializeField] private float platformRadius = 14.0f;

        // Visual Hierarchy
        private GameObject environmentRoot;
        private GameObject platformFloor;
        private Material floorMaterial;
        private readonly GameObject[] pillars = new GameObject[8];
        private Light centerAmbientLight;
        private ParticleSystem ambientParticles;

        private void Awake()
        {
            BuildMetaverseRoom();
            ApplyTheme(currentTheme);
        }

        private void BuildMetaverseRoom()
        {
            // Clean lighting that honors the existing scene environment
            RenderSettings.fog = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.22f, 0.25f, 0.35f, 1f);

            environmentRoot = new GameObject("Metaverse_VirtualRoom_Root");
            environmentRoot.transform.SetParent(transform, false);

            // Note: Standard natural floor from Starter Assets (Grid) is preserved.
            // We do NOT spawn an artificial elevated cylinder floor to keep the environment completely normal and clean.

            // Central Atmospheric Ambient Light (Soft illumination)
            GameObject lightObj = new GameObject("Metaverse_AmbientLight");
            lightObj.transform.SetParent(environmentRoot.transform, false);
            lightObj.transform.localPosition = new Vector3(0, 4.0f, 0);
            centerAmbientLight = lightObj.AddComponent<Light>();
            centerAmbientLight.type = LightType.Point;
            centerAmbientLight.range = 25.0f;
            centerAmbientLight.intensity = 1.8f;
            centerAmbientLight.color = new Color(0.0f, 0.82f, 1.0f);

            // Subtle Ambient Floating Dust Motes
            GameObject partObj = new GameObject("Metaverse_AmbientParticles");
            partObj.transform.SetParent(environmentRoot.transform, false);
            partObj.transform.localPosition = new Vector3(0, 1.8f, 0);

            ambientParticles = partObj.AddComponent<ParticleSystem>();
            var main = ambientParticles.main;
            main.loop = true;
            main.startLifetime = 6.0f;
            main.startSpeed = 0.05f;
            main.startSize = 0.02f;
            main.maxParticles = 60;
            main.startColor = new Color(0.0f, 0.9f, 1.0f, 0.35f);

            var emission = ambientParticles.emission;
            emission.rateOverTime = 8f;

            var shape = ambientParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(platformRadius * 1.2f, 3.0f, platformRadius * 1.2f);
        }

        public void NextTheme()
        {
            int next = ((int)currentTheme + 1) % 3;
            ApplyTheme((EnvironmentTheme)next);
        }

        public void ApplyTheme(EnvironmentTheme theme)
        {
            currentTheme = theme;
            Color gridColor;
            Color lightColor;

            switch (theme)
            {
                case EnvironmentTheme.CyberPenthouse:
                    gridColor = new Color(0.0f, 0.85f, 1.0f, 1.0f); // Vibrant Cyan
                    lightColor = new Color(0.1f, 0.8f, 1.0f);
                    break;
                case EnvironmentTheme.CosmicNebula:
                    gridColor = new Color(0.72f, 0.25f, 1.0f, 1.0f); // Neon Purple
                    lightColor = new Color(0.65f, 0.2f, 1.0f);
                    break;
                case EnvironmentTheme.SunsetHorizon:
                    gridColor = new Color(1.0f, 0.55f, 0.15f, 1.0f); // Warm Amber
                    lightColor = new Color(1.0f, 0.65f, 0.3f);
                    break;
                default:
                    gridColor = Color.cyan;
                    lightColor = Color.cyan;
                    break;
            }

            if (floorMaterial != null)
            {
                if (floorMaterial.HasProperty("_GridColor")) floorMaterial.SetColor("_GridColor", gridColor);
                if (floorMaterial.HasProperty("_BaseColor")) floorMaterial.SetColor("_BaseColor", new Color(0.02f, 0.04f, 0.08f, 1.0f));
            }
            if (centerAmbientLight != null)
            {
                centerAmbientLight.color = lightColor;
                centerAmbientLight.intensity = 2.2f;
            }
            for (int i = 0; i < pillars.Length; i++)
            {
                if (pillars[i] != null)
                {
                    var rend = pillars[i].GetComponent<Renderer>();
                    if (rend != null && rend.material != null)
                    {
                        rend.material.color = gridColor * 0.9f;
                        if (rend.material.HasProperty("_BaseColor")) rend.material.SetColor("_BaseColor", gridColor * 0.9f);
                    }
                }
            }
        }

        private void Update()
        {
            // Shortcut 'T' to cycle Metaverse Room themes
            if (Input.GetKeyDown(KeyCode.T))
            {
                NextTheme();
            }
        }
    }
}
