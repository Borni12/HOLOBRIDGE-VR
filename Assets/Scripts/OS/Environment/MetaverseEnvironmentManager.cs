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
            environmentRoot = new GameObject("Metaverse_VirtualRoom_Root");
            environmentRoot.transform.SetParent(transform, false);

            // 1. Center Observation Platform Floor
            platformFloor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            platformFloor.name = "Metaverse_PlatformFloor";
            platformFloor.transform.SetParent(environmentRoot.transform, false);
            platformFloor.transform.localScale = new Vector3(platformRadius * 2f, 0.2f, platformRadius * 2f);
            platformFloor.transform.localPosition = new Vector3(0, -0.1f, 0);

            Shader floorShader = Shader.Find("OmniOS/MetaverseFloor");
            if (floorShader == null) floorShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            floorMaterial = new Material(floorShader);
            platformFloor.GetComponent<Renderer>().material = floorMaterial;

            // 2. 8 Architectural Holographic Pillars around perimeter
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            for (int i = 0; i < 8; i++)
            {
                float angle = i * (Mathf.PI * 2f / 8f);
                float x = Mathf.Cos(angle) * (platformRadius * 0.92f);
                float z = Mathf.Sin(angle) * (platformRadius * 0.92f);

                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = $"Metaverse_Pillar_{i}";
                pillar.transform.SetParent(environmentRoot.transform, false);
                pillar.transform.localScale = new Vector3(0.28f, 5.0f, 0.28f);
                pillar.transform.localPosition = new Vector3(x, 2.4f, z);

                // Destroy pillar collider to allow smooth movement
                Destroy(pillar.GetComponent<Collider>());

                Material pillarMat = new Material(unlitShader);
                pillarMat.color = new Color(0.0f, 0.85f, 1.0f, 0.65f);
                pillar.GetComponent<Renderer>().material = pillarMat;
                pillars[i] = pillar;
            }

            // 3. Central Atmospheric Ambient Light
            GameObject lightObj = new GameObject("Metaverse_AmbientLight");
            lightObj.transform.SetParent(environmentRoot.transform, false);
            lightObj.transform.localPosition = new Vector3(0, 4.5f, 0);
            centerAmbientLight = lightObj.AddComponent<Light>();
            centerAmbientLight.type = LightType.Point;
            centerAmbientLight.range = 35.0f;
            centerAmbientLight.intensity = 1.6f;
            centerAmbientLight.color = new Color(0.0f, 0.82f, 1.0f);

            // 4. Ambient Floating Dust Motes
            GameObject partObj = new GameObject("Metaverse_AmbientParticles");
            partObj.transform.SetParent(environmentRoot.transform, false);
            partObj.transform.localPosition = new Vector3(0, 1.8f, 0);

            ambientParticles = partObj.AddComponent<ParticleSystem>();
            var main = ambientParticles.main;
            main.loop = true;
            main.startLifetime = 8.0f;
            main.startSpeed = 0.08f;
            main.startSize = 0.025f;
            main.maxParticles = 120;
            main.startColor = new Color(0.0f, 0.9f, 1.0f, 0.45f);

            var emission = ambientParticles.emission;
            emission.rateOverTime = 14f;

            var shape = ambientParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(platformRadius * 1.5f, 4.0f, platformRadius * 1.5f);
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
                floorMaterial.SetColor("_GridColor", gridColor);
            }
            if (centerAmbientLight != null)
            {
                centerAmbientLight.color = lightColor;
            }
            for (int i = 0; i < pillars.Length; i++)
            {
                if (pillars[i] != null)
                {
                    pillars[i].GetComponent<Renderer>().material.color = gridColor * 0.8f;
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
