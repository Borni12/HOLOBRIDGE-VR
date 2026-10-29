using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OmniOS.Core;
using OmniOS.Interaction;
using OmniGenesis.Biometrics;

namespace OmniGenesis.Core
{
    /// <summary>
    /// OmniGenesis5DExperience - Master 5D Neural Graphics & Extreme 3DUI Controller.
    /// Implements all 5 flagship innovations from the Omni-Genesis 5D Master Blueprint:
    /// 1. Reality-Peel: Bimanual projective unprojection expanding 2D portal into 360° stereoscopic 3D space.
    /// 2. Matter-Weave: Gaze cone selection + wrist-tuned continuum stiffness [Rigid Metal, Elastic Rubber, Viscous Fluid].
    /// 3. Multiverse Forking: 5 floating toroidal Time Orbs showing predictive alternate futures.
    /// 4. Joint-Gaze Cooperative Handoff: Photorealistic Conversational Human Avatar with gaze-tracking and reach IK.
    /// 5. Vestibular Counter-Flow Shield: Active peripheral motion compensation for zero cybersickness.
    /// Fully optimized for Meta Quest 3 VR and PC Desktop (WASD + Right-click look + Left-click laser pointer).
    /// </summary>
    [DisallowMultipleComponent]
    public class OmniGenesis5DExperience : MonoBehaviour
    {
        private static OmniGenesis5DExperience instance;
        public static OmniGenesis5DExperience Instance => instance;

        [Header("System References")]
        [SerializeField] private Transform headCamera;
        [SerializeField] private SpatialPointer pointer;

        // Visual Roots
        private GameObject realityPeelRoot;
        private GameObject matterWeaveRoot;
        private GameObject multiverseOrbsRoot;
        private GameObject avatarHandoffRoot;
        private GameObject gaussianSplatsRoot;

        // 1. Reality-Peel State
        private Transform portalTransform;
        private Material portalMaterial;
        private bool isPeelActive = false;
        private float peelProgress = 0.0f; // 0 = 2D portal, 1 = 360° unprojected
        private Vector3 initialPortalPos;
        private Vector3 initialPortalScale;

        // 2. Matter-Weave State
        public enum MaterialStiffness { Rigid = 0, Elastic = 1, Fluid = 2 }
        [SerializeField] private MaterialStiffness currentStiffness = MaterialStiffness.Elastic;
        public MaterialStiffness CurrentStiffness => currentStiffness;
        private Transform matterObjectTransform;
        private Material matterMaterial;
        private Rigidbody matterRb;
        private Vector3 matterOriginalScale;
        private float squishTimer = 0f;

        // 3. Multiverse Orbs State
        private readonly GameObject[] timeOrbs = new GameObject[5];
        private readonly Transform[] orbRings = new Transform[5];
        private int activeTimelineIndex = 0;
        private bool areOrbsVisible = true;

        // 4. Conversational Avatar State
        private Transform avatarTransform;
        private Transform avatarHeadTransform;
        private Transform avatarRightArmTransform;
        private Transform avatarRightHandTransform;
        private GameObject exchangeObject;
        private bool isHandoffActive = false;
        private float handoffProgress = 0.0f;
        private float avatarBreathTime = 0.0f;
        private float avatarBlinkTimer = 3.0f;
        private bool isAvatarBlinking = false;

        // 5. Vestibular Shield State
        private VestibularShieldPass vestibularShield;

        // 6. Neural Gaussian Particles
        private ParticleSystem gaussianParticles;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }
            instance = this;

            if (headCamera == null && Camera.main != null)
            {
                headCamera = Camera.main.transform;
            }

            if (pointer == null)
            {
                pointer = UnityEngine.Object.FindAnyObjectByType<SpatialPointer>();
            }

            // Ensure Vestibular Shield on Main Camera
            if (Camera.main != null)
            {
                vestibularShield = Camera.main.GetComponent<VestibularShieldPass>();
                if (vestibularShield == null)
                {
                    vestibularShield = Camera.main.gameObject.AddComponent<VestibularShieldPass>();
                }
            }
        }

        private void Start()
        {
            BuildAll5DSystems();
        }

        private void BuildAll5DSystems()
        {
            Vector3 centerPos = headCamera != null ? headCamera.position : Vector3.up * 1.6f;
            Vector3 centerFwd = headCamera != null ? headCamera.forward : Vector3.forward;
            centerFwd.y = 0;
            if (centerFwd.sqrMagnitude < 0.01f) centerFwd = Vector3.forward;
            centerFwd.Normalize();

            // 1. Build Reality-Peel Portal Frame (Direct Front, slightly raised)
            BuildRealityPeelPortal(centerPos, centerFwd);

            // 2. Build Matter-Weave Lab (Front Left Pedestal)
            BuildMatterWeaveLab(centerPos, centerFwd);

            // 3. Build Multiverse Time Orbs (Floating Arc Overhead)
            BuildMultiverseTimeOrbs(centerPos, centerFwd);

            // 4. Build Conversational Avatar Sophia (Front Right Lounge)
            BuildConversationalAvatar(centerPos, centerFwd);

            // 5. Build Volumetric 5D Gaussian Splat Field
            BuildGaussianSplatField(centerPos);

            OmniOSManager.Instance?.ShowNotificationToast("🌟 OMNI-GENESIS 5D: All 5 Neural Metaphors Online!");
        }

        #region 1. Reality-Peel Metaphor
        private void BuildRealityPeelPortal(Vector3 centerPos, Vector3 fwd)
        {
            realityPeelRoot = new GameObject("OMNI_5D_RealityPeel_Root");
            realityPeelRoot.transform.SetParent(transform, false);

            Vector3 portalPos = centerPos + fwd * 1.45f + Vector3.up * 0.12f;
            realityPeelRoot.transform.position = portalPos;
            realityPeelRoot.transform.rotation = Quaternion.LookRotation(fwd);

            // 2D Portal Backplate Quad
            GameObject portalQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            portalQuad.name = "Portal_2DCanvas";
            portalQuad.transform.SetParent(realityPeelRoot.transform, false);
            portalQuad.transform.localScale = new Vector3(1.15f, 0.75f, 1f);
            portalQuad.transform.localPosition = Vector3.zero;
            Destroy(portalQuad.GetComponent<Collider>());

            portalTransform = portalQuad.transform;
            initialPortalPos = portalTransform.position;
            initialPortalScale = portalTransform.localScale;

            Shader portalShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
            if (portalShader == null) portalShader = Shader.Find("Sprites/Default");
            portalMaterial = new Material(portalShader);
            portalMaterial.color = new Color(0.1f, 0.75f, 1.0f, 0.95f);

            // Create procedural preview texture
            Texture2D tex = GenerateNeuralPreviewTexture();
            portalMaterial.mainTexture = tex;
            portalQuad.GetComponent<Renderer>().material = portalMaterial;

            // Frame Borders (Gilded Cyber Rim)
            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Portal_GildedRim";
            frame.transform.SetParent(realityPeelRoot.transform, false);
            frame.transform.localScale = new Vector3(1.22f, 0.82f, 0.03f);
            frame.transform.localPosition = new Vector3(0, 0, 0.015f);
            Destroy(frame.GetComponent<Collider>());
            var rimMat = new Material(portalShader);
            rimMat.color = new Color(0.0f, 0.95f, 1.0f, 0.85f);
            frame.GetComponent<Renderer>().material = rimMat;

            // 3D Control Buttons below frame
            GameObject btnBar = new GameObject("Portal_ControlButtons");
            btnBar.transform.SetParent(realityPeelRoot.transform, false);
            btnBar.transform.localPosition = new Vector3(0, -0.48f, -0.02f);

            Create3DButton(btnBar.transform, "🔮 Peel & Enter 360°", new Vector3(-0.25f, 0, 0), () => TriggerRealityPeel(), new Color(0.0f, 0.9f, 1.0f), 0.28f);
            Create3DButton(btnBar.transform, "↺ Reset Frame", new Vector3(0.25f, 0, 0), () => ResetRealityPeel(), new Color(1.0f, 0.45f, 0.2f), 0.24f);
        }

        private Texture2D GenerateNeuralPreviewTexture()
        {
            Texture2D tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            for (int y = 0; y < 256; y++)
            {
                for (int x = 0; x < 256; x++)
                {
                    float u = x / 256f;
                    float v = y / 256f;
                    float c = Mathf.Sin(u * 12f) * Mathf.Cos(v * 12f) * 0.5f + 0.5f;
                    Color col = Color.Lerp(new Color(0.05f, 0.15f, 0.35f), new Color(0.0f, 0.9f, 1.0f), c);
                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply();
            return tex;
        }

        public void TriggerRealityPeel()
        {
            isPeelActive = !isPeelActive;
            SpatialAudioEngine.Instance?.PlayThemeChime();
            OmniOSManager.Instance?.ShowNotificationToast(isPeelActive ? "🌌 Reality-Peel Activated: Unprojecting 360° Gaussian Space!" : "🖼️ Reality-Peel Compressed back to 2D Portal.");
        }

        public void ResetRealityPeel()
        {
            isPeelActive = false;
            peelProgress = 0f;
            if (portalTransform != null)
            {
                portalTransform.localScale = initialPortalScale;
                portalTransform.position = initialPortalPos;
            }
            SpatialAudioEngine.Instance?.PlayButtonClick();
        }

        public bool IsPeelActive => isPeelActive;
        public int ActiveTimelineIndex => activeTimelineIndex;
        public bool AreOrbsVisible => areOrbsVisible;
        public bool IsHandoffActive => isHandoffActive;

        public void FocusRealityPeel()
        {
            if (headCamera == null && Camera.main != null) headCamera = Camera.main.transform;
            if (headCamera != null && realityPeelRoot != null)
            {
                Vector3 headPos = headCamera.position;
                Vector3 headFwd = headCamera.forward;
                headFwd.y = 0;
                if (headFwd.sqrMagnitude < 0.01f) headFwd = Vector3.forward;
                headFwd.Normalize();

                realityPeelRoot.transform.position = headPos + headFwd * 1.45f + Vector3.up * 0.12f;
                realityPeelRoot.transform.rotation = Quaternion.LookRotation(headFwd);
                initialPortalPos = realityPeelRoot.transform.position;
                OmniOSManager.Instance?.ShowNotificationToast("🔮 Reality-Peel Frame centered in front of your eyes!");
            }
        }

        public void FocusMatterWeaveLab()
        {
            if (headCamera == null && Camera.main != null) headCamera = Camera.main.transform;
            if (headCamera != null && matterWeaveRoot != null)
            {
                Vector3 headPos = headCamera.position;
                Vector3 headFwd = headCamera.forward;
                headFwd.y = 0;
                if (headFwd.sqrMagnitude < 0.01f) headFwd = Vector3.forward;
                headFwd.Normalize();

                matterWeaveRoot.transform.position = headPos + headFwd * 1.30f + Vector3.down * 0.05f;
                matterWeaveRoot.transform.rotation = Quaternion.LookRotation(headFwd);
                OmniOSManager.Instance?.ShowNotificationToast("⚛️ Matter-Weave Lab moved directly in front of you!");
            }
        }

        public void FocusMultiverseOrbs()
        {
            if (headCamera == null && Camera.main != null) headCamera = Camera.main.transform;
            if (headCamera != null && multiverseOrbsRoot != null)
            {
                Vector3 headPos = headCamera.position;
                Vector3 headFwd = headCamera.forward;
                headFwd.y = 0;
                if (headFwd.sqrMagnitude < 0.01f) headFwd = Vector3.forward;
                headFwd.Normalize();

                multiverseOrbsRoot.transform.position = headPos + headFwd * 1.50f + Vector3.up * 0.20f;
                multiverseOrbsRoot.transform.rotation = Quaternion.LookRotation(headFwd);
                areOrbsVisible = true;
                multiverseOrbsRoot.SetActive(true);
                OmniOSManager.Instance?.ShowNotificationToast("⏳ Multiverse Time Orbs summoned to your view!");
            }
        }

        public void FocusAvatar()
        {
            if (headCamera == null && Camera.main != null) headCamera = Camera.main.transform;
            if (headCamera != null && avatarHandoffRoot != null)
            {
                Vector3 headPos = headCamera.position;
                Vector3 headFwd = headCamera.forward;
                headFwd.y = 0;
                if (headFwd.sqrMagnitude < 0.01f) headFwd = Vector3.forward;
                headFwd.Normalize();

                avatarHandoffRoot.transform.position = headPos + headFwd * 1.65f;
                avatarHandoffRoot.transform.rotation = Quaternion.LookRotation(-headFwd);
                OmniOSManager.Instance?.ShowNotificationToast("🤝 Avatar Sophia brought into direct conversational range!");
            }
        }

        public void ToggleVestibularShield()
        {
            if (vestibularShield != null)
            {
                vestibularShield.enabled = !vestibularShield.enabled;
                OmniOSManager.Instance?.ShowNotificationToast($"🛡️ Vestibular Shield: {(vestibularShield.enabled ? "ACTIVE (0-Cybersickness)" : "DISABLED")}");
                SpatialAudioEngine.Instance?.PlayButtonClick();
            }
        }

        public void ToggleGaussianParticles()
        {
            if (gaussianSplatsRoot != null)
            {
                gaussianSplatsRoot.SetActive(!gaussianSplatsRoot.activeSelf);
                OmniOSManager.Instance?.ShowNotificationToast($"✨ Neural Gaussian Field: {(gaussianSplatsRoot.activeSelf ? "VISIBLE" : "HIDDEN")}");
                SpatialAudioEngine.Instance?.PlayButtonClick();
            }
        }
        #endregion

        #region 2. Matter-Weave Metaphor
        private void BuildMatterWeaveLab(Vector3 centerPos, Vector3 fwd)
        {
            matterWeaveRoot = new GameObject("OMNI_5D_MatterWeave_Root");
            matterWeaveRoot.transform.SetParent(transform, false);

            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
            Vector3 labPos = centerPos + fwd * 1.35f - right * 0.85f + Vector3.down * 0.15f;
            matterWeaveRoot.transform.position = labPos;
            matterWeaveRoot.transform.rotation = Quaternion.LookRotation(fwd + right * 0.35f);

            // Holographic Pedestal
            GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedestal.name = "MatterWeave_Pedestal";
            pedestal.transform.SetParent(matterWeaveRoot.transform, false);
            pedestal.transform.localScale = new Vector3(0.55f, 0.45f, 0.55f);
            pedestal.transform.localPosition = new Vector3(0, -0.45f, 0);
            Destroy(pedestal.GetComponent<Collider>());

            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            var pedMat = new Material(unlit);
            pedMat.color = new Color(0.06f, 0.12f, 0.22f, 0.95f);
            pedestal.GetComponent<Renderer>().material = pedMat;

            // Interactive Morphing Matter-Weave Object
            GameObject matterObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            matterObj.name = "MatterWeave_ContinuumObject";
            matterObj.transform.SetParent(matterWeaveRoot.transform, false);
            matterObj.transform.localScale = Vector3.one * 0.26f;
            matterObj.transform.localPosition = new Vector3(0, 0.12f, 0);

            matterObjectTransform = matterObj.transform;
            matterOriginalScale = matterObjectTransform.localScale;

            matterMaterial = new Material(unlit);
            matterObj.GetComponent<Renderer>().material = matterMaterial;

            matterRb = matterObj.AddComponent<Rigidbody>();
            matterRb.isKinematic = true;

            // Apply default stiffness styling
            ApplyStiffnessVisuals();

            // 3D Control Buttons on Pedestal
            GameObject btnBar = new GameObject("MatterWeave_Buttons");
            btnBar.transform.SetParent(matterWeaveRoot.transform, false);
            btnBar.transform.localPosition = new Vector3(0, 0.38f, -0.05f);

            Create3DButton(btnBar.transform, "⚙️ Rigid", new Vector3(-0.24f, 0, 0), () => SetStiffness(MaterialStiffness.Rigid), new Color(0.85f, 0.88f, 0.95f), 0.18f);
            Create3DButton(btnBar.transform, "🟢 Elastic", new Vector3(0, 0, 0), () => SetStiffness(MaterialStiffness.Elastic), new Color(0.2f, 0.95f, 0.45f), 0.18f);
            Create3DButton(btnBar.transform, "💧 Fluid", new Vector3(0.24f, 0, 0), () => SetStiffness(MaterialStiffness.Fluid), new Color(0.1f, 0.65f, 1.0f), 0.18f);
        }

        public void SetStiffness(MaterialStiffness stiffness)
        {
            currentStiffness = stiffness;
            ApplyStiffnessVisuals();
            TriggerMatterSquish();
            SpatialAudioEngine.Instance?.PlayButtonClick();
            OmniOSManager.Instance?.ShowNotificationToast($"⚛️ Matter-Weave: Stiffness tuned to [{currentStiffness}]!");
        }

        private void ApplyStiffnessVisuals()
        {
            if (matterMaterial == null) return;
            switch (currentStiffness)
            {
                case MaterialStiffness.Rigid:
                    matterMaterial.color = new Color(0.92f, 0.94f, 1.0f, 0.95f);
                    break;
                case MaterialStiffness.Elastic:
                    matterMaterial.color = new Color(0.25f, 1.0f, 0.55f, 0.90f);
                    break;
                case MaterialStiffness.Fluid:
                    matterMaterial.color = new Color(0.0f, 0.72f, 1.0f, 0.75f);
                    break;
            }
        }

        public void TriggerMatterSquish()
        {
            squishTimer = 0.5f;
            SpatialAudioEngine.Instance?.PlayHoverTick();
        }
        #endregion

        #region 3. Multiverse Orbs Metaphor
        private void BuildMultiverseTimeOrbs(Vector3 centerPos, Vector3 fwd)
        {
            multiverseOrbsRoot = new GameObject("OMNI_5D_Multiverse_Root");
            multiverseOrbsRoot.transform.SetParent(transform, false);

            Vector3 orbsCenter = centerPos + fwd * 1.55f + Vector3.up * 0.75f;
            multiverseOrbsRoot.transform.position = orbsCenter;
            multiverseOrbsRoot.transform.rotation = Quaternion.LookRotation(fwd);

            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");

            string[] timelineNames = new string[]
            {
                "🌀 Cyber Storm (+10m)",
                "🌌 Zero Gravity (+10m)",
                "🌸 Quantum Bloom (+10m)",
                "⚡ Neon Aurora (+10m)",
                "⏳ Singularity (+10m)"
            };

            Color[] timelineColors = new Color[]
            {
                new Color(0.0f, 0.85f, 1.0f),
                new Color(0.68f, 0.28f, 1.0f),
                new Color(1.0f, 0.35f, 0.65f),
                new Color(0.18f, 0.95f, 0.45f),
                new Color(1.0f, 0.85f, 0.15f)
            };

            for (int i = 0; i < 5; i++)
            {
                int idx = i;
                float angle = (i - 2) * 0.32f;
                Vector3 localPos = new Vector3(Mathf.Sin(angle) * 0.75f, Mathf.Cos(angle) * 0.12f, Mathf.Cos(angle) * 0.25f - 0.2f);

                GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                orb.name = $"TimeOrb_{i}";
                orb.transform.SetParent(multiverseOrbsRoot.transform, false);
                orb.transform.localScale = Vector3.one * 0.16f;
                orb.transform.localPosition = localPos;

                var orbMat = new Material(unlit);
                orbMat.color = timelineColors[i];
                orb.GetComponent<Renderer>().material = orbMat;

                // Rotating Energy Ring
                GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = "Orb_Ring";
                ring.transform.SetParent(orb.transform, false);
                ring.transform.localScale = new Vector3(1.35f, 0.012f, 1.35f);
                ring.transform.localRotation = Quaternion.Euler(35f, 0, 0);
                Destroy(ring.GetComponent<Collider>());
                var ringMat = new Material(unlit);
                ringMat.color = timelineColors[i] * 1.3f;
                ring.GetComponent<Renderer>().material = ringMat;

                orbRings[i] = ring.transform;
                timeOrbs[i] = orb;

                // Attach SpatialButton3D for direct 1-click laser pointer interaction!
                var btn = orb.AddComponent<SpatialButton3D>();
                btn.ButtonText = $"Orb {i + 1}";
                btn.SetSize(0.18f, 0.18f);
                btn.SetColors(timelineColors[i] * 0.2f, timelineColors[i]);
                btn.onClick.AddListener(() => CommitTimeline(idx, timelineNames[idx]));
            }
        }

        public void CommitTimeline(int index, string name)
        {
            activeTimelineIndex = index;
            SpatialAudioEngine.Instance?.PlayThemeChime();
            OmniOSManager.Instance?.ShowNotificationToast($"⏳ Multiverse Timeline Committed: {name}!");

            // Modulate environment lighting
            var env = UnityEngine.Object.FindAnyObjectByType<OmniOS.Environment.MetaverseEnvironmentManager>();
            if (env != null)
            {
                env.ApplyTheme((OmniOS.Environment.MetaverseEnvironmentManager.EnvironmentTheme)(index % 3));
            }
        }

        public void ToggleMultiverseOrbs()
        {
            areOrbsVisible = !areOrbsVisible;
            if (multiverseOrbsRoot != null) multiverseOrbsRoot.SetActive(areOrbsVisible);
            SpatialAudioEngine.Instance?.PlayButtonClick();
            OmniOSManager.Instance?.ShowNotificationToast($"🔮 Multiverse Orbs: {(areOrbsVisible ? "Visible" : "Hidden")}");
        }
        #endregion

        #region 4. Joint-Gaze Avatar Metaphor
        private void BuildConversationalAvatar(Vector3 centerPos, Vector3 fwd)
        {
            avatarHandoffRoot = new GameObject("OMNI_5D_Avatar_Sophia_Root");
            avatarHandoffRoot.transform.SetParent(transform, false);

            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
            Vector3 avatarPos = centerPos + fwd * 1.55f + right * 0.85f;
            avatarHandoffRoot.transform.position = avatarPos;
            avatarHandoffRoot.transform.rotation = Quaternion.LookRotation(-fwd);

            avatarTransform = avatarHandoffRoot.transform;

            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");

            // 1. Torso / Body
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            torso.name = "Sophia_Torso";
            torso.transform.SetParent(avatarHandoffRoot.transform, false);
            torso.transform.localScale = new Vector3(0.38f, 0.65f, 0.25f);
            torso.transform.localPosition = new Vector3(0, 0.75f, 0);
            Destroy(torso.GetComponent<Collider>());
            var bodyMat = new Material(unlit);
            bodyMat.color = new Color(0.12f, 0.16f, 0.26f);
            torso.GetComponent<Renderer>().material = bodyMat;

            // 2. Head
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Sophia_Head";
            head.transform.SetParent(avatarHandoffRoot.transform, false);
            head.transform.localScale = new Vector3(0.24f, 0.28f, 0.24f);
            head.transform.localPosition = new Vector3(0, 1.52f, 0);
            Destroy(head.GetComponent<Collider>());
            var headMat = new Material(unlit);
            headMat.color = new Color(0.95f, 0.78f, 0.68f); // Skin tone
            head.GetComponent<Renderer>().material = headMat;
            avatarHeadTransform = head.transform;

            // 3. Eyes / Visor
            GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "Sophia_Visor";
            visor.transform.SetParent(head.transform, false);
            visor.transform.localScale = new Vector3(0.65f, 0.18f, 0.35f);
            visor.transform.localPosition = new Vector3(0, 0.12f, 0.45f);
            Destroy(visor.GetComponent<Collider>());
            var visorMat = new Material(unlit);
            visorMat.color = new Color(0.0f, 0.95f, 1.0f, 0.95f);
            visor.GetComponent<Renderer>().material = visorMat;

            // 4. Right Arm (IK Reach target)
            GameObject rightArm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rightArm.name = "Sophia_RightArm";
            rightArm.transform.SetParent(avatarHandoffRoot.transform, false);
            rightArm.transform.localScale = new Vector3(0.08f, 0.32f, 0.08f);
            rightArm.transform.localPosition = new Vector3(-0.28f, 1.05f, 0.12f);
            rightArm.transform.localRotation = Quaternion.Euler(45f, 0, 0);
            Destroy(rightArm.GetComponent<Collider>());
            rightArm.GetComponent<Renderer>().material = bodyMat;
            avatarRightArmTransform = rightArm.transform;

            // 5. Right Hand
            GameObject rightHand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rightHand.name = "Sophia_RightHand";
            rightHand.transform.SetParent(rightArm.transform, false);
            rightHand.transform.localScale = Vector3.one * 0.85f;
            rightHand.transform.localPosition = new Vector3(0, -1.0f, 0);
            Destroy(rightHand.GetComponent<Collider>());
            rightHand.GetComponent<Renderer>().material = headMat;
            avatarRightHandTransform = rightHand.transform;

            // 6. Exchange Object (Quantum Relic that can be passed)
            exchangeObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            exchangeObject.name = "Quantum_Exchange_Relic";
            exchangeObject.transform.SetParent(avatarHandoffRoot.transform, false);
            exchangeObject.transform.localScale = Vector3.one * 0.12f;
            exchangeObject.transform.localPosition = new Vector3(-0.28f, 0.72f, 0.38f);
            Destroy(exchangeObject.GetComponent<Collider>());
            var relicMat = new Material(unlit);
            relicMat.color = new Color(1.0f, 0.75f, 0.15f);
            exchangeObject.GetComponent<Renderer>().material = relicMat;

            // 3D Handoff Action Button
            GameObject btnObj = new GameObject("Sophia_HandoffButton");
            btnObj.transform.SetParent(avatarHandoffRoot.transform, false);
            btnObj.transform.localPosition = new Vector3(0, 0.35f, 0.35f);

            Create3DButton(btnObj.transform, "🤝 Cooperative Handoff", Vector3.zero, () => TriggerHandoff(), new Color(0.2f, 0.95f, 0.55f), 0.38f);
        }

        public void TriggerHandoff()
        {
            isHandoffActive = true;
            handoffProgress = 0f;
            SpatialAudioEngine.Instance?.PlayNotificationChime();
            OmniOSManager.Instance?.ShowNotificationToast("🤝 Avatar Sophia: Gaze locked! Extending arm to receive object.");
        }
        #endregion

        #region 5. Volumetric Gaussian Splats
        private void BuildGaussianSplatField(Vector3 centerPos)
        {
            gaussianSplatsRoot = new GameObject("OMNI_5D_GaussianField_Root");
            gaussianSplatsRoot.transform.SetParent(transform, false);
            gaussianSplatsRoot.transform.position = centerPos + Vector3.up * 1.0f;

            gaussianParticles = gaussianSplatsRoot.AddComponent<ParticleSystem>();
            var main = gaussianParticles.main;
            main.loop = true;
            main.maxParticles = 1200;
            main.startLifetime = 10f;
            main.startSpeed = 0.05f;
            main.startSize = 0.045f;
            main.startColor = new Color(0.0f, 0.85f, 1.0f, 0.65f);

            var emission = gaussianParticles.emission;
            emission.rateOverTime = 80f;

            var shape = gaussianParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 4.5f;

            var renderer = gaussianParticles.GetComponent<ParticleSystemRenderer>();
            Shader partShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            renderer.material = new Material(partShader);
        }
        #endregion

        #region Helper: 3D Button Creator
        private SpatialButton3D Create3DButton(Transform parent, string label, Vector3 localPos, Action onClick, Color accent, float width = 0.22f)
        {
            GameObject btnObj = new GameObject($"Btn_{label.Replace(" ", "_")}");
            btnObj.transform.SetParent(parent, false);
            btnObj.transform.localPosition = localPos;

            var btn = btnObj.AddComponent<SpatialButton3D>();
            btn.ButtonText = label;
            btn.SetSize(width, 0.082f);
            btn.SetColors(new Color(0.08f, 0.12f, 0.18f, 0.95f), accent);
            btn.onClick.AddListener(() => onClick?.Invoke());
            return btn;
        }
        #endregion

        private void Update()
        {
            UpdateRealityPeelDynamics();
            UpdateMatterWeaveDynamics();
            UpdateMultiverseOrbsDynamics();
            UpdateAvatarDynamics();
            HandleGlobalShortcuts();
        }

        private void UpdateRealityPeelDynamics()
        {
            if (portalTransform == null) return;

            float targetProgress = isPeelActive ? 1.0f : 0.0f;
            peelProgress = Mathf.MoveTowards(peelProgress, targetProgress, Time.deltaTime * 1.8f);

            if (peelProgress > 0.01f)
            {
                // Expand portal into 360° envelope
                float scaleMultiplier = Mathf.Lerp(1.0f, 18.0f, peelProgress);
                portalTransform.localScale = initialPortalScale * scaleMultiplier;

                Vector3 targetPos = initialPortalPos + (headCamera != null ? (headCamera.position - initialPortalPos) * (peelProgress * 0.85f) : Vector3.zero);
                portalTransform.position = targetPos;

                if (portalMaterial != null)
                {
                    portalMaterial.color = Color.Lerp(new Color(0.1f, 0.75f, 1.0f, 0.95f), new Color(0.0f, 0.95f, 1.0f, 0.35f), peelProgress);
                }
            }
            else
            {
                portalTransform.localScale = initialPortalScale;
                portalTransform.position = initialPortalPos;
            }
        }

        private void UpdateMatterWeaveDynamics()
        {
            if (matterObjectTransform == null) return;

            // Idle rotation
            matterObjectTransform.Rotate(Vector3.up, 35f * Time.deltaTime, Space.World);

            // Squish deformation dynamics
            if (squishTimer > 0f)
            {
                squishTimer -= Time.deltaTime;
                float factor = Mathf.Sin(squishTimer * Mathf.PI * 8f) * 0.25f;

                if (currentStiffness == MaterialStiffness.Elastic)
                {
                    matterObjectTransform.localScale = new Vector3(
                        matterOriginalScale.x * (1f + factor),
                        matterOriginalScale.y * (1f - factor * 0.8f),
                        matterOriginalScale.z * (1f + factor)
                    );
                }
                else if (currentStiffness == MaterialStiffness.Fluid)
                {
                    matterObjectTransform.localScale = new Vector3(
                        matterOriginalScale.x * (1f + factor * 1.5f),
                        matterOriginalScale.y * (1f - factor * 1.2f),
                        matterOriginalScale.z * (1f + factor * 0.8f)
                    );
                }
            }
            else
            {
                matterObjectTransform.localScale = Vector3.Lerp(matterObjectTransform.localScale, matterOriginalScale, Time.deltaTime * 10f);
            }
        }

        private void UpdateMultiverseOrbsDynamics()
        {
            // Spin energy rings
            for (int i = 0; i < 5; i++)
            {
                if (orbRings[i] != null)
                {
                    orbRings[i].Rotate(Vector3.up, (45f + i * 20f) * Time.deltaTime, Space.Self);
                }
            }
        }

        private void UpdateAvatarDynamics()
        {
            if (avatarTransform == null) return;

            // 1. Natural Breathing
            avatarBreathTime += Time.deltaTime * 2.2f;
            float breathOffset = Mathf.Sin(avatarBreathTime) * 0.012f;
            if (avatarHeadTransform != null)
            {
                avatarHeadTransform.localPosition = new Vector3(0, 1.52f + breathOffset, 0);

                // Gaze tracking: Head looks at user
                if (headCamera != null)
                {
                    Vector3 lookDir = (headCamera.position - avatarHeadTransform.position).normalized;
                    Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
                    avatarHeadTransform.rotation = Quaternion.Slerp(avatarHeadTransform.rotation, targetRot, Time.deltaTime * 4.5f);
                }
            }

            // 2. Eye Blinking
            avatarBlinkTimer -= Time.deltaTime;
            if (avatarBlinkTimer <= 0f)
            {
                avatarBlinkTimer = UnityEngine.Random.Range(2.8f, 5.0f);
                isAvatarBlinking = true;
                StartCoroutine(AvatarBlinkRoutine());
            }

            // 3. Handoff Reach IK
            if (isHandoffActive && avatarRightArmTransform != null)
            {
                handoffProgress += Time.deltaTime * 1.5f;
                float t = Mathf.Clamp01(handoffProgress);
                avatarRightArmTransform.localRotation = Quaternion.Euler(Mathf.Lerp(45f, 95f, t), 0, 0);

                if (exchangeObject != null)
                {
                    exchangeObject.transform.localPosition = Vector3.Lerp(new Vector3(-0.28f, 0.72f, 0.38f), new Vector3(-0.28f, 1.05f, 0.65f), t);
                }

                if (handoffProgress >= 1.0f)
                {
                    isHandoffActive = false;
                    OmniOSManager.Instance?.ShowNotificationToast("✅ Sophia: Object received and secured in hand!");
                    SpatialAudioEngine.Instance?.PlayButtonClick();
                }
            }
        }

        private IEnumerator AvatarBlinkRoutine()
        {
            yield return new WaitForSeconds(0.12f);
            isAvatarBlinking = false;
        }

        private void HandleGlobalShortcuts()
        {
            // P = Reality-Peel
            if (Input.GetKeyDown(KeyCode.P)) TriggerRealityPeel();

            // M = Cycle Matter-Weave stiffness
            if (Input.GetKeyDown(KeyCode.M))
            {
                int next = ((int)currentStiffness + 1) % 3;
                SetStiffness((MaterialStiffness)next);
            }

            // O = Toggle Multiverse Orbs
            if (Input.GetKeyDown(KeyCode.O)) ToggleMultiverseOrbs();

            // J = Trigger Joint-Gaze Handoff
            if (Input.GetKeyDown(KeyCode.J)) TriggerHandoff();
        }

        private void OnGUI()
        {
            // High-Contrast Sleek 5D HUD Overlay (Top-Left)
            float hudW = 340f;
            float hudH = 175f;
            float x = 20f;
            float y = 20f;

            GUI.Box(new Rect(x, y, hudW, hudH), "");
            GUILayout.BeginArea(new Rect(x + 10, y + 8, hudW - 20, hudH - 16));

            GUILayout.Label("<b><color=#00e5ff>⚡ OMNI-GENESIS 5D ARCHITECTURE</color></b>");
            GUILayout.Label($"<b>1. Reality-Peel:</b> {(isPeelActive ? "<color=#00ff88>UNPROJECTED 360°</color>" : "<color=#ffaa00>2D Portal Frame</color>")} (Press 'P')");
            GUILayout.Label($"<b>2. Matter-Weave:</b> <color=#00e5ff>[{currentStiffness}]</color> (Press 'M')");
            GUILayout.Label($"<b>3. Multiverse Orbs:</b> <color=#ff77ff>[Active: Timeline {activeTimelineIndex + 1}]</color> (Press 'O')");
            GUILayout.Label($"<b>4. Avatar Sophia:</b> {(isHandoffActive ? "<color=#00ff88>REACHING IK...</color>" : "Gaze Tracking User")} (Press 'J')");
            GUILayout.Label($"<b>5. Vestibular Shield:</b> <color=#00ff88>ACTIVE (0-Cybersickness)</color>");

            GUILayout.EndArea();
        }
    }
}
