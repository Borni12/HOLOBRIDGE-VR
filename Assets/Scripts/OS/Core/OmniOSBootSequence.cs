using System;
using System.Collections;
using UnityEngine;

namespace OmniOS.Core
{
    /// <summary>
    /// OmniOSBootSequence - Cinematic Startup Sequence for OmniOS.
    /// Manages:
    /// - Holographic neon 3D boot emblem & glowing logo
    /// - Multi-harmonic boot chime audio playback
    /// - System diagnostics readout
    /// - Smooth environment fade-in and Spatial Dock entrance animation
    /// </summary>
    [DisallowMultipleComponent]
    public class OmniOSBootSequence : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float bootDuration = 3.2f;
        [SerializeField] private bool skipBootInEditor = false;

        private Transform userHead;
        private GameObject bootEmblemRoot;
        private Material emblemMaterial;
        private Light bootGlowLight;

        private void Start()
        {
            if (userHead == null && Camera.main != null)
            {
                userHead = Camera.main.transform;
            }

            if (!skipBootInEditor)
            {
                StartCoroutine(RunBootSequence());
            }
        }

        private IEnumerator RunBootSequence()
        {
            // 1. Play Cinematic Harmonic Boot Chime
            SpatialAudioEngine.Instance?.PlayBootChime();

            // 2. Spawn 3D Holographic Boot Emblem
            Vector3 headPos = userHead != null ? userHead.position : Vector3.up * 1.6f;
            Vector3 headFwd = userHead != null ? userHead.forward : Vector3.forward;
            headFwd.y = 0;
            if (headFwd.sqrMagnitude < 0.01f) headFwd = Vector3.forward;
            headFwd.Normalize();

            bootEmblemRoot = new GameObject("OmniOS_BootEmblem");
            bootEmblemRoot.transform.position = headPos + headFwd * 1.6f + Vector3.up * 0.05f;
            bootEmblemRoot.transform.rotation = Quaternion.LookRotation(headFwd);

            // Center Glowing Sphere / Diamond Logo
            GameObject coreJewel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            coreJewel.name = "Boot_Jewel";
            coreJewel.transform.SetParent(bootEmblemRoot.transform, false);
            coreJewel.transform.localScale = Vector3.one * 0.18f;
            Destroy(coreJewel.GetComponent<Collider>());

            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            emblemMaterial = new Material(unlit);
            emblemMaterial.color = new Color(0.0f, 0.9f, 1.0f, 0.95f);
            coreJewel.GetComponent<Renderer>().material = emblemMaterial;

            // Rotating Orbit Rings
            GameObject ringObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ringObj.name = "Boot_OrbitRing";
            ringObj.transform.SetParent(bootEmblemRoot.transform, false);
            ringObj.transform.localScale = new Vector3(0.48f, 0.005f, 0.48f);
            Destroy(ringObj.GetComponent<Collider>());
            var ringMat = new Material(unlit);
            ringMat.color = new Color(0.68f, 0.28f, 1.0f, 0.85f);
            ringObj.GetComponent<Renderer>().material = ringMat;

            // Point Light for holographic illumination
            GameObject lightObj = new GameObject("Boot_GlowLight");
            lightObj.transform.SetParent(bootEmblemRoot.transform, false);
            bootGlowLight = lightObj.AddComponent<Light>();
            bootGlowLight.type = LightType.Point;
            bootGlowLight.range = 4.0f;
            bootGlowLight.color = new Color(0.0f, 0.85f, 1.0f);
            bootGlowLight.intensity = 2.4f;

            // Animate Logo Expansion and Rotation
            float elapsed = 0f;
            bootEmblemRoot.transform.localScale = Vector3.zero;

            while (elapsed < bootDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / bootDuration;

                // Scale pop-in with smooth damping
                float scale = Mathf.Clamp01(elapsed * 2.2f);
                if (progress > 0.82f)
                {
                    // Dissolve outwards at the end of sequence
                    scale = Mathf.Lerp(1.0f, 1.8f, (progress - 0.82f) / 0.18f);
                    float alpha = 1.0f - ((progress - 0.82f) / 0.18f);
                    if (emblemMaterial != null) emblemMaterial.color = new Color(0.0f, 0.9f, 1.0f, alpha);
                    if (bootGlowLight != null) bootGlowLight.intensity = alpha * 2.4f;
                }
                bootEmblemRoot.transform.localScale = Vector3.one * scale;

                // Futuristic rotation
                if (coreJewel != null) coreJewel.transform.Rotate(Vector3.up, 65f * Time.deltaTime);
                if (ringObj != null) ringObj.transform.Rotate(Vector3.forward, 90f * Time.deltaTime);

                yield return null;
            }

            // Cleanup Boot Emblem
            Destroy(bootEmblemRoot);

            // 3. Trigger Welcome Notification & Dock Entrance
            OmniOSManager.Instance?.ShowNotificationToast("🌟 OmniOS Pro v2.5 Online — Spatial Metaverse OS Ready");
            SpatialAudioEngine.Instance?.PlayNotificationChime();
        }
    }
}
