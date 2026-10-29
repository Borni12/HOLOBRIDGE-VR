using System;
using System.Collections.Generic;
using UnityEngine;

namespace OmniOS.Core
{
    /// <summary>
    /// SpatialAudioEngine - Procedural Sound Effects & Synthesizer Engine for OmniOS.
    /// Synthesizes high-fidelity audio clips in memory without external audio file dependencies:
    /// - Cinematic OS Boot Chime (Harmonic C-Major 9th chord swell)
    /// - Window Open Swoosh & Minimize Swoosh
    /// - Button Hover Ticks & Haptic Click Pops
    /// - Camera Shutter & Xenon Flash Charge sound
    /// - Notification Bell Chimes
    /// - Ambient Cosmic Room Drone
    /// </summary>
    [DisallowMultipleComponent]
    public class SpatialAudioEngine : MonoBehaviour
    {
        private static SpatialAudioEngine instance;
        public static SpatialAudioEngine Instance => instance;

        private AudioSource sfxSource;
        private AudioSource ambientSource;

        // Cached synthesized clips
        private AudioClip bootChimeClip;
        private AudioClip windowOpenClip;
        private AudioClip windowMinimizeClip;
        private AudioClip buttonClickClip;
        private AudioClip hoverTickClip;
        private AudioClip shutterClip;
        private AudioClip notificationClip;
        private AudioClip themeChimeClip;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.spatialBlend = 0.0f; // 2D Head-relative UI audio
            sfxSource.playOnAwake = false;

            ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.spatialBlend = 0.5f;
            ambientSource.loop = true;
            ambientSource.volume = 0.18f;

            SynthesizeAllClips();
        }

        private void SynthesizeAllClips()
        {
            bootChimeClip = GenerateBootChimeClip();
            windowOpenClip = GenerateFrequencySweep(420f, 1350f, 0.18f, true);
            windowMinimizeClip = GenerateFrequencySweep(1250f, 380f, 0.18f, false);
            buttonClickClip = GenerateClickClip(850f, 0.025f);
            hoverTickClip = GenerateClickClip(1600f, 0.008f);
            shutterClip = GenerateShutterClip();
            notificationClip = GenerateDoubleChime(880f, 1320f, 0.22f);
            themeChimeClip = GenerateThemeChime();
        }

        public void PlayBootChime()
        {
            if (sfxSource != null && bootChimeClip != null)
            {
                sfxSource.PlayOneShot(bootChimeClip, 0.85f);
            }
        }

        public void PlayWindowOpen()
        {
            if (sfxSource != null && windowOpenClip != null)
            {
                sfxSource.PlayOneShot(windowOpenClip, 0.45f);
            }
        }

        public void PlayWindowMinimize()
        {
            if (sfxSource != null && windowMinimizeClip != null)
            {
                sfxSource.PlayOneShot(windowMinimizeClip, 0.45f);
            }
        }

        public void PlayButtonClick()
        {
            if (sfxSource != null && buttonClickClip != null)
            {
                sfxSource.PlayOneShot(buttonClickClip, 0.5f);
            }
        }

        public void PlayHoverTick()
        {
            if (sfxSource != null && hoverTickClip != null)
            {
                sfxSource.PlayOneShot(hoverTickClip, 0.22f);
            }
        }

        public void PlayShutterClick()
        {
            if (sfxSource != null && shutterClip != null)
            {
                sfxSource.PlayOneShot(shutterClip, 0.9f);
            }
        }

        public void PlayNotificationChime()
        {
            if (sfxSource != null && notificationClip != null)
            {
                sfxSource.PlayOneShot(notificationClip, 0.6f);
            }
        }

        public void PlayThemeChime()
        {
            if (sfxSource != null && themeChimeClip != null)
            {
                sfxSource.PlayOneShot(themeChimeClip, 0.7f);
            }
        }

        public void StartAmbientDrone()
        {
            if (ambientSource != null && !ambientSource.isPlaying)
            {
                AudioClip drone = GenerateAmbientDroneClip();
                ambientSource.clip = drone;
                ambientSource.Play();
            }
        }

        public void StopAmbientDrone()
        {
            if (ambientSource != null && ambientSource.isPlaying)
            {
                ambientSource.Stop();
            }
        }

        // --- Procedural Audio Synthesizers ---

        private AudioClip GenerateBootChimeClip()
        {
            int sampleRate = 44100;
            float duration = 2.8f;
            int samples = (int)(sampleRate * duration);
            AudioClip clip = AudioClip.Create("BootChime", samples, 1, sampleRate, false);
            float[] data = new float[samples];

            // Harmonic C-Major 9th (C3, G3, E4, B4, D5)
            float[] freqs = new float[] { 130.81f, 196.00f, 329.63f, 493.88f, 587.33f };

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 1.4f) * Mathf.Clamp01(t * 8f); // Gentle attack, exponential decay

                float sample = 0f;
                for (int f = 0; f < freqs.Length; f++)
                {
                    sample += Mathf.Sin(2f * Mathf.PI * freqs[f] * t) * (1f / (f + 1));
                }
                data[i] = sample * envelope * 0.32f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateFrequencySweep(float startFreq, float endFreq, float duration, bool rising)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            AudioClip clip = AudioClip.Create("Swoosh", samples, 1, sampleRate, false);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = (float)i / samples;
                float currentFreq = Mathf.Lerp(startFreq, endFreq, progress);
                float envelope = Mathf.Sin(progress * Mathf.PI); // Windowed smooth peak

                data[i] = Mathf.Sin(2f * Mathf.PI * currentFreq * t) * envelope * 0.4f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateClickClip(float freq, float duration)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            AudioClip clip = AudioClip.Create("Click", samples, 1, sampleRate, false);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1f - (float)i / samples;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * envelope * 0.6f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateShutterClip()
        {
            int sampleRate = 44100;
            float duration = 0.28f;
            int samples = (int)(sampleRate * duration);
            AudioClip clip = AudioClip.Create("Shutter", samples, 1, sampleRate, false);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                // Dual mechanical clicks (curtain open at 0.02s, curtain close at 0.12s)
                float click1 = Mathf.Exp(-Mathf.Abs(t - 0.02f) * 180f) * Mathf.Sin(2f * Mathf.PI * 1800f * t);
                float click2 = Mathf.Exp(-Mathf.Abs(t - 0.12f) * 160f) * Mathf.Sin(2f * Mathf.PI * 1400f * t);
                data[i] = (click1 + click2 * 0.8f) * 0.6f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateDoubleChime(float freq1, float freq2, float duration)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            AudioClip clip = AudioClip.Create("Notification", samples, 1, sampleRate, false);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float bell1 = Mathf.Exp(-t * 14f) * Mathf.Sin(2f * Mathf.PI * freq1 * t);
                float bell2 = t > 0.08f ? Mathf.Exp(-(t - 0.08f) * 12f) * Mathf.Sin(2f * Mathf.PI * freq2 * t) : 0f;
                data[i] = (bell1 + bell2) * 0.45f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateThemeChime()
        {
            int sampleRate = 44100;
            float duration = 1.6f;
            int samples = (int)(sampleRate * duration);
            AudioClip clip = AudioClip.Create("ThemeChime", samples, 1, sampleRate, false);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 2.2f) * Mathf.Clamp01(t * 12f);
                float sample = Mathf.Sin(2f * Mathf.PI * 528f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * 792f * t) * 0.35f;
                data[i] = sample * envelope * 0.45f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateAmbientDroneClip()
        {
            int sampleRate = 22050;
            float duration = 6.0f; // Looping drone
            int samples = (int)(sampleRate * duration);
            AudioClip clip = AudioClip.Create("AmbientDrone", samples, 1, sampleRate, false);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                // Deep soothing resonant frequencies (55Hz, 110Hz, 165Hz)
                float wave = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.4f
                           + Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.25f
                           + Mathf.Sin(2f * Mathf.PI * 165f * t + Mathf.Sin(t * 0.5f)) * 0.15f;
                data[i] = wave * 0.25f;
            }

            clip.SetData(data, 0);
            return clip;
        }
    }
}
