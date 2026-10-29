using UnityEngine;

namespace OmniOS.Core
{
    /// <summary>
    /// SpatialTheme - Standard styling, palette, and audio/haptic definitions for OmniOS.
    /// Follows visionOS-style glassmorphism and Holobridge neon accents.
    /// </summary>
    public static class SpatialTheme
    {
        // Color Palette
        public static readonly Color GlassBase = new Color(0.08f, 0.10f, 0.14f, 0.78f);
        public static readonly Color GlassBorder = new Color(0.0f, 0.85f, 1.0f, 0.95f);
        public static readonly Color AccentCyan = new Color(0.0f, 0.88f, 1.0f, 1.0f);
        public static readonly Color AccentPurple = new Color(0.68f, 0.28f, 1.0f, 1.0f);
        public static readonly Color AccentGreen = new Color(0.18f, 0.92f, 0.45f, 1.0f);
        public static readonly Color AccentOrange = new Color(1.0f, 0.58f, 0.0f, 1.0f);
        public static readonly Color TextPrimary = new Color(0.95f, 0.96f, 0.98f, 1.0f);
        public static readonly Color TextSecondary = new Color(0.65f, 0.70f, 0.78f, 1.0f);
        public static readonly Color SurfaceDark = new Color(0.05f, 0.06f, 0.08f, 0.88f);
        public static readonly Color ButtonNormal = new Color(0.12f, 0.16f, 0.22f, 0.90f);
        public static readonly Color ButtonHover = new Color(0.18f, 0.26f, 0.38f, 0.95f);
        public static readonly Color ButtonActive = new Color(0.0f, 0.75f, 0.95f, 1.0f);

        /// <summary>
        /// Universally applies base and rim/border colors across custom glass, URP Unlit, and legacy shaders.
        /// Prevents materials from ever defaulting to solid white in standalone builds.
        /// </summary>
        public static void ApplyVisualProperties(Material mat, Color baseColor, Color rimColor)
        {
            if (mat == null) return;

            // Custom Glass Shader properties
            if (mat.HasProperty("_BaseTint")) mat.SetColor("_BaseTint", baseColor);
            if (mat.HasProperty("_BorderGlowColor")) mat.SetColor("_BorderGlowColor", rimColor);
            if (mat.HasProperty("_BorderColor")) mat.SetColor("_BorderColor", rimColor);

            // URP and Standard Shader properties
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);
            mat.color = baseColor;
        }

        /// <summary>
        /// Creates a high-fidelity glass material safely falling back to transparent URP Unlit if custom shaders are stripped in standalone builds.
        /// </summary>
        public static Material CreateGlassMaterial(Color baseColor, Color rimColor)
        {
            Material baseMat = Resources.Load<Material>("Materials/M_SpatialGlassRealistic");
            Material mat = null;
            if (baseMat != null)
            {
                mat = new Material(baseMat);
            }
            else
            {
                Shader s = Shader.Find("OmniOS/SpatialGlassRealistic") ?? Shader.Find("OmniOS/SpatialGlassUI");
                if (s != null)
                {
                    mat = new Material(s);
                }
                else
                {
                    Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                    mat = new Material(unlit);
                    // Configure URP transparent alpha blend
                    if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1);
                    if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                }
            }

            ApplyVisualProperties(mat, baseColor, rimColor);
            return mat;
        }

        // Procedural Audio Cues for Spatial Feedback
        public static void PlaySpatialTone(Vector3 position, float frequency = 880f, float duration = 0.05f)
        {
            // Lightweight spatial audio beep
            GameObject audioObj = new GameObject("OmniOS_SpatialAudioCue");
            audioObj.transform.position = position;
            AudioSource source = audioObj.AddComponent<AudioSource>();
            source.spatialBlend = 1.0f; // 3D Spatial Audio
            source.minDistance = 0.5f;
            source.maxDistance = 10f;
            source.volume = 0.25f;

            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            AudioClip clip = AudioClip.Create("Cue", samples, 1, sampleRate, false);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1.0f - (float)i / samples; // Exponential decay
                data[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * envelope;
            }
            clip.SetData(data, 0);
            source.clip = clip;
            source.Play();
            Object.Destroy(audioObj, duration + 0.1f);
        }
    }
}
