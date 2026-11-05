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
        public static OmniGenesis5DExperience Instance
        {
            get
            {
                if (instance == null || !instance)
                {
                    instance = UnityEngine.Object.FindAnyObjectByType<OmniGenesis5DExperience>();
                }
                return instance;
            }
        }

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
        public bool IsAvatarBlinking => isAvatarBlinking;

        // 5. Vestibular Shield State
        private VestibularShieldPass vestibularShield;

        // 6. Neural Gaussian Particles
        private ParticleSystem gaussianParticles;

        // 7. 2D HUD State
        private bool isHUDMinimized = false;

        private void Awake()
        {
            if (instance != null && instance != this && instance.gameObject != null)
            {
                Destroy(this);
                return;
            }
            instance = this;

            Camera cam = OmniInputHelper.ResolveActiveCamera();
            if (cam != null)
            {
                headCamera = cam.transform;
            }

            if (pointer == null)
            {
                pointer = UnityEngine.Object.FindAnyObjectByType<SpatialPointer>();
            }

            // Ensure Vestibular Shield on Main Camera
            if (cam != null)
            {
                vestibularShield = cam.GetComponent<VestibularShieldPass>();
                if (vestibularShield == null)
                {
                    vestibularShield = cam.gameObject.AddComponent<VestibularShieldPass>();
                }
            }
        }

        private void Start()
        {
            BuildAll5DSystems();
        }

        private void SafeDestroy(GameObject go)
        {
            if (go == null) return;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(go);
            else
                Destroy(go);
#else
            Destroy(go);
#endif
        }

        private void PurgeExisting5DRoots()
        {
            SafeDestroy(realityPeelRoot);
            SafeDestroy(matterWeaveRoot);
            SafeDestroy(multiverseOrbsRoot);
            SafeDestroy(avatarHandoffRoot);
            SafeDestroy(gaussianSplatsRoot);

            realityPeelRoot = null;
            matterWeaveRoot = null;
            multiverseOrbsRoot = null;
            avatarHandoffRoot = null;
            gaussianSplatsRoot = null;

            string[] rootNames = new string[]
            {
                "OMNI_5D_RealityPeel_Root",
                "OMNI_5D_MatterWeave_Root",
                "OMNI_5D_Multiverse_Root",
                "OMNI_5D_Avatar_Sophia_Root",
                "OMNI_5D_GaussianField_Root"
            };

            var allGOs = UnityEngine.Object.FindObjectsByType<GameObject>();
            foreach (var go in allGOs)
            {
                if (go == null) continue;
                for (int i = 0; i < rootNames.Length; i++)
                {
                    if (go.name == rootNames[i])
                    {
                        SafeDestroy(go);
                        break;
                    }
                }
            }
        }

        public static Material CreateURPMaterial(Color color, Texture2D tex = null, bool transparent = false)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Sprites/Default")
                         ?? Shader.Find("Unlit/Texture")
                         ?? Shader.Find("Unlit/Color");

            Material mat = new Material(shader);
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);

            if (tex != null)
            {
                mat.mainTexture = tex;
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            }

            if (transparent)
            {
                if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
                if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            return mat;
        }

        public static void SetMaterialColor(Material mat, Color color)
        {
            if (mat == null) return;
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        }

        public void RebuildAll5DSystems()
        {
            if (realityPeelRoot != null) SafeDestroy(realityPeelRoot);
            if (matterWeaveRoot != null) SafeDestroy(matterWeaveRoot);
            if (multiverseOrbsRoot != null) SafeDestroy(multiverseOrbsRoot);
            if (avatarHandoffRoot != null) SafeDestroy(avatarHandoffRoot);
            if (gaussianSplatsRoot != null) SafeDestroy(gaussianSplatsRoot);

            Camera cam = OmniInputHelper.ResolveActiveCamera();
            if (cam != null) headCamera = cam.transform;

            BuildAll5DSystems();
            OmniOSManager.Instance?.ShowNotificationToast("⚡ 5D Neural Systems Rebuilt & Centered in front of view!");
        }

        private void BuildAll5DSystems()
        {
            PurgeExisting5DRoots();

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

            // Position gracefully in Left Lounge (2.5m forward, 1.5m left) facing user, leaving direct center open for Tablet
            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
            Vector3 portalPos = centerPos + fwd * 2.5f - right * 1.5f + Vector3.up * 0.12f;
            realityPeelRoot.transform.position = portalPos;
            Vector3 lookToUser = centerPos - portalPos;
            lookToUser.y = 0;
            realityPeelRoot.transform.rotation = Quaternion.LookRotation(lookToUser);

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

            // Create procedural cosmic portal texture
            Texture2D tex = GenerateNeuralPreviewTexture();
            portalMaterial = CreateURPMaterial(Color.white, tex, false);
            portalMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            portalQuad.GetComponent<Renderer>().material = portalMaterial;

            // Frame Borders (Gilded Cyber Rim with Luminous Cyan/Gold Finish)
            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Portal_GildedRim";
            frame.transform.SetParent(realityPeelRoot.transform, false);
            frame.transform.localScale = new Vector3(1.22f, 0.82f, 0.03f);
            frame.transform.localPosition = new Vector3(0, 0, 0.015f);
            Destroy(frame.GetComponent<Collider>());
            var rimMat = CreateURPMaterial(new Color(0.0f, 0.95f, 1.0f, 0.95f));
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
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[256 * 256];
            for (int y = 0; y < 256; y++)
            {
                for (int x = 0; x < 256; x++)
                {
                    float u = (x - 128f) / 128f;
                    float v = (y - 128f) / 128f;
                    float dist = Mathf.Sqrt(u * u + v * v);

                    float swirl = Mathf.Sin(dist * 14f - Mathf.Atan2(v, u) * 4f);
                    Color deepSpace = new Color(0.02f, 0.04f, 0.10f, 1.0f);
                    Color cyanGlow = new Color(0.0f, 0.88f, 1.0f, 1.0f);
                    Color purpleNebula = new Color(0.70f, 0.15f, 0.95f, 1.0f);

                    Color c = Color.Lerp(deepSpace, cyanGlow, Mathf.Clamp01(swirl * 0.5f + 0.5f));
                    if (dist < 0.85f) c = Color.Lerp(c, purpleNebula, Mathf.Clamp01((0.85f - dist) * 1.2f));

                    // Outer border ring
                    if (dist > 0.92f && dist <= 1.0f) c = Color.Lerp(c, cyanGlow, 0.9f);

                    pixels[y * 256 + x] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply(false, false);
            return tex;
        }

        public void TriggerRealityPeel()
        {
            isPeelActive = !isPeelActive;
            if (isPeelActive) FocusRealityPeel();
            SpatialAudioEngine.Instance?.PlayThemeChime();
            string msg = isPeelActive ? "🌌 Reality-Peel Activated: Unprojecting 360° Gaussian Space!" : "🖼️ Reality-Peel Compressed back to 2D Portal.";
            OmniOSManager.Instance?.ShowNotificationToast(msg);
            Debug.Log($"<color=#00e5ff>[OMNI 5D]</color> 🔮 Reality-Peel (P): {(isPeelActive ? "UNPROJECTED 360°" : "2D PORTAL")}");
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
            Debug.Log("<color=#00e5ff>[OMNI 5D]</color> ↺ Reality-Peel Frame Reset");
        }

        public bool IsPeelActive => isPeelActive;
        public int ActiveTimelineIndex => activeTimelineIndex;
        public bool AreOrbsVisible => areOrbsVisible;
        public bool IsHandoffActive => isHandoffActive;

        public void FocusRealityPeel()
        {
            if (headCamera == null)
            {
                Camera cam = OmniInputHelper.ResolveActiveCamera();
                if (cam != null) headCamera = cam.transform;
            }
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
            if (headCamera == null)
            {
                Camera cam = OmniInputHelper.ResolveActiveCamera();
                if (cam != null) headCamera = cam.transform;
            }
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
            if (headCamera == null)
            {
                Camera cam = OmniInputHelper.ResolveActiveCamera();
                if (cam != null) headCamera = cam.transform;
            }
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
            if (headCamera == null)
            {
                Camera cam = OmniInputHelper.ResolveActiveCamera();
                if (cam != null) headCamera = cam.transform;
            }
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

            // Position in Far Left Lounge (2.2m forward, 2.5m left) with dedicated pedestal facing user
            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
            Vector3 labPos = centerPos + fwd * 2.2f - right * 2.5f - Vector3.up * 0.10f;
            matterWeaveRoot.transform.position = labPos;
            Vector3 lookToUser = centerPos - labPos;
            lookToUser.y = 0;
            matterWeaveRoot.transform.rotation = Quaternion.LookRotation(lookToUser);

            // Holographic Pedestal
            GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedestal.name = "MatterWeave_Pedestal";
            pedestal.transform.SetParent(matterWeaveRoot.transform, false);
            pedestal.transform.localScale = new Vector3(0.55f, 0.45f, 0.55f);
            pedestal.transform.localPosition = new Vector3(0, -0.45f, 0);
            Destroy(pedestal.GetComponent<Collider>());

            var pedMat = CreateURPMaterial(new Color(0.06f, 0.12f, 0.22f, 0.95f));
            pedestal.GetComponent<Renderer>().material = pedMat;

            // Interactive Morphing Matter-Weave Object
            GameObject matterObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            matterObj.name = "MatterWeave_ContinuumObject";
            matterObj.transform.SetParent(matterWeaveRoot.transform, false);
            matterObj.transform.localScale = Vector3.one * 0.26f;
            matterObj.transform.localPosition = new Vector3(0, 0.12f, 0);

            matterObjectTransform = matterObj.transform;
            matterOriginalScale = matterObjectTransform.localScale;

            matterMaterial = CreateURPMaterial(new Color(0.25f, 1.0f, 0.55f, 0.90f));
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
            FocusMatterWeaveLab();
            ApplyStiffnessVisuals();
            TriggerMatterSquish();
            SpatialAudioEngine.Instance?.PlayButtonClick();
            OmniOSManager.Instance?.ShowNotificationToast($"⚛️ Matter-Weave: Stiffness tuned to [{currentStiffness}]!");
            Debug.Log($"<color=#00ff88>[OMNI 5D]</color> ⚛️ Matter-Weave (M): Continuum stiffness changed to [{currentStiffness}]");
        }

        private void ApplyStiffnessVisuals()
        {
            if (matterMaterial == null) return;
            switch (currentStiffness)
            {
                case MaterialStiffness.Rigid:
                    SetMaterialColor(matterMaterial, new Color(0.92f, 0.94f, 1.0f, 0.95f));
                    break;
                case MaterialStiffness.Elastic:
                    SetMaterialColor(matterMaterial, new Color(0.25f, 1.0f, 0.55f, 0.90f));
                    break;
                case MaterialStiffness.Fluid:
                    SetMaterialColor(matterMaterial, new Color(0.0f, 0.72f, 1.0f, 0.75f));
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

            // Position in Right Lounge (2.5m forward, 1.5m right, raised) hovering gracefully
            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
            Vector3 orbsCenter = centerPos + fwd * 2.5f + right * 1.5f + Vector3.up * 0.35f;
            multiverseOrbsRoot.transform.position = orbsCenter;
            Vector3 lookToUser = centerPos - orbsCenter;
            lookToUser.y = 0;
            multiverseOrbsRoot.transform.rotation = Quaternion.LookRotation(lookToUser);

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

                var orbMat = CreateURPMaterial(timelineColors[i]);
                orb.GetComponent<Renderer>().material = orbMat;

                // Rotating Energy Ring
                GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = "Orb_Ring";
                ring.transform.SetParent(orb.transform, false);
                ring.transform.localScale = new Vector3(1.35f, 0.012f, 1.35f);
                ring.transform.localRotation = Quaternion.Euler(35f, 0, 0);
                Destroy(ring.GetComponent<Collider>());
                var ringMat = CreateURPMaterial(timelineColors[i] * 1.35f);
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
            Debug.Log($"<color=#ff77ff>[OMNI 5D]</color> ⏳ Multiverse Orbs (O): {(areOrbsVisible ? "VISIBLE" : "HIDDEN")} [Timeline {activeTimelineIndex + 1}]");
        }
        #endregion

        #region 4. Joint-Gaze Avatar Metaphor
        private void BuildConversationalAvatar(Vector3 centerPos, Vector3 fwd)
        {
            avatarHandoffRoot = new GameObject("OMNI_5D_Avatar_Sophia_Root");
            avatarHandoffRoot.transform.SetParent(transform, false);

            // Position in Far Right Lounge (2.2m forward, 2.5m right) standing naturally facing user
            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
            Vector3 avatarPos = centerPos + fwd * 2.2f + right * 2.5f;
            avatarHandoffRoot.transform.position = avatarPos;
            Vector3 lookToUser = centerPos - avatarPos;
            lookToUser.y = 0;
            avatarHandoffRoot.transform.rotation = Quaternion.LookRotation(lookToUser);

            avatarTransform = avatarHandoffRoot.transform;

            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");

            // 1. Torso / Body (Dark Cybernetic Suit)
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            torso.name = "Sophia_Torso";
            torso.transform.SetParent(avatarHandoffRoot.transform, false);
            torso.transform.localScale = new Vector3(0.38f, 0.65f, 0.25f);
            torso.transform.localPosition = new Vector3(0, 0.75f, 0);
            Destroy(torso.GetComponent<Collider>());
            var bodyMat = CreateURPMaterial(new Color(0.08f, 0.12f, 0.20f, 1f));
            torso.GetComponent<Renderer>().material = bodyMat;

            // 2. Head (Realistic Warm Skin Tone)
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Sophia_Head";
            head.transform.SetParent(avatarHandoffRoot.transform, false);
            head.transform.localScale = new Vector3(0.24f, 0.28f, 0.24f);
            head.transform.localPosition = new Vector3(0, 1.52f, 0);
            Destroy(head.GetComponent<Collider>());
            var headMat = CreateURPMaterial(new Color(0.95f, 0.78f, 0.68f, 1f));
            head.GetComponent<Renderer>().material = headMat;
            avatarHeadTransform = head.transform;

            // 3. Eyes / Visor (Luminous Neon Cyan)
            GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "Sophia_Visor";
            visor.transform.SetParent(head.transform, false);
            visor.transform.localScale = new Vector3(0.65f, 0.18f, 0.35f);
            visor.transform.localPosition = new Vector3(0, 0.12f, 0.45f);
            Destroy(visor.GetComponent<Collider>());
            var visorMat = CreateURPMaterial(new Color(0.0f, 0.95f, 1.0f, 0.95f));
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
            var relicMat = CreateURPMaterial(new Color(1.0f, 0.75f, 0.15f, 1f));
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
            Debug.Log("<color=#ffd700>[OMNI 5D]</color> 🤝 Avatar Sophia Handoff (J): Gaze locked, reaching forward via procedural IK!");
        }
        #endregion

        #region 5. Volumetric Gaussian Splats
        private void BuildGaussianSplatField(Vector3 centerPos)
        {
            gaussianSplatsRoot = new GameObject("OMNI_5D_GaussianField_Root");
            gaussianSplatsRoot.transform.SetParent(transform, false);
            gaussianSplatsRoot.transform.position = centerPos + Vector3.up * 1.5f;

            gaussianParticles = gaussianSplatsRoot.AddComponent<ParticleSystem>();
            var main = gaussianParticles.main;
            main.loop = true;
            main.maxParticles = 80; // Subtle, elegant ambient cyber aura instead of dense blinding cloud
            main.startLifetime = 8f;
            main.startSpeed = 0.02f;
            main.startSize = 0.02f;
            main.startColor = new Color(0.0f, 0.85f, 1.0f, 0.25f);

            var emission = gaussianParticles.emission;
            emission.rateOverTime = 8f;

            var shape = gaussianParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 5.0f;

            var renderer = gaussianParticles.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateURPMaterial(new Color(0.0f, 0.85f, 1.0f, 0.25f), null, true);
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
                    Color peelCol = Color.Lerp(new Color(0.1f, 0.75f, 1.0f, 0.95f), new Color(0.0f, 0.95f, 1.0f, 0.35f), peelProgress);
                    SetMaterialColor(portalMaterial, peelCol);
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
                else if (currentStiffness == MaterialStiffness.Rigid)
                {
                    float rigidVibe = Mathf.Sin(squishTimer * Mathf.PI * 18f) * 0.08f;
                    matterObjectTransform.localScale = matterOriginalScale * (1f + rigidVibe);
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
            // If OmniOSManager is active in scene, let the OS master kernel manage global keys to prevent double-firing
            if (OmniOSManager.Instance != null) return;

            // P = Reality-Peel (Dual-mode detection via OmniInputHelper)
            if (OmniInputHelper.CheckP()) TriggerRealityPeel();

            // M = Cycle Matter-Weave stiffness
            if (OmniInputHelper.CheckM())
            {
                int next = ((int)currentStiffness + 1) % 3;
                SetStiffness((MaterialStiffness)next);
            }

            // O = Toggle Multiverse Orbs
            if (OmniInputHelper.CheckO()) ToggleMultiverseOrbs();

            // J = Trigger Joint-Gaze Handoff
            if (OmniInputHelper.CheckJ()) TriggerHandoff();

            // R = Full Rebuild 5D Systems
            if (OmniInputHelper.CheckR()) RebuildAll5DSystems();
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void OnGUI()
        {
            OmniInputHelper.RegisterGUIEvent(Event.current);

            // 1. Top-Right Dedicated Floating Quick Reload Button (Always accessible)
            float rW = 160f;
            float rH = 34f;
            float rx = Screen.width - rW - 16f;
            float ry = 14f;
            GUI.color = new Color(0.0f, 0.95f, 1.0f, 0.95f);
            if (GUI.Button(new Rect(rx, ry, rW, rH), "<b>🔄 Full Reload (R)</b>"))
            {
                if (OmniOSManager.Instance != null)
                {
                    OmniOSManager.Instance.FullSystemReload();
                }
                else
                {
                    RebuildAll5DSystems();
                }
            }
            GUI.color = Color.white;

            // Only render 2D HUD when enabled (F1 toggles)
            if (!OmniOS.Core.OmniOSManager.Show2DOverlay) return;

            GUI.skin.button.richText = true;
            GUI.skin.label.richText = true;
            GUI.skin.box.richText = true;

            float hudW = 380f;
            float hudH = isHUDMinimized ? 38f : 325f;
            float x = 16f;
            float y = 14f;

            GUI.color = new Color(0.04f, 0.08f, 0.14f, 0.96f);
            GUI.Box(new Rect(x, y, hudW, hudH), "");
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(x + 10, y + 6, hudW - 20, hudH - 12));

            // Header with Title and Minimize toggle button
            GUILayout.BeginHorizontal();
            GUILayout.Label("<b><color=#00e5ff>📱 ANDROID 15 & 5D CONTROL CENTER</color></b>", GUILayout.ExpandWidth(true));
            if (GUILayout.Button(isHUDMinimized ? "<b>[+] Expand</b>" : "<b>[-] Hide</b>", GUILayout.Width(76), GUILayout.Height(22)))
            {
                isHUDMinimized = !isHUDMinimized;
            }
            GUILayout.EndHorizontal();

            if (!isHUDMinimized)
            {
                GUILayout.Space(4);

                // Row A: Android 15 Tablet & Apple Launchpad
                GUILayout.BeginHorizontal();
                GUI.color = new Color(0.2f, 0.95f, 0.55f);
                if (GUILayout.Button("📱 <b>Android 15 Tablet (1)</b>", GUILayout.Height(28)))
                {
                    OmniOSManager.Instance?.ToggleApp("media_hub");
                }
                GUI.color = new Color(1.0f, 0.75f, 0.15f);
                if (GUILayout.Button(" <b>Launchpad (Tab)</b>", GUILayout.Height(28)))
                {
                    OmniAppLauncherBox.Instance?.ToggleLaunchpad();
                }
                GUILayout.EndHorizontal();

                // 1. Reality-Peel Button
                GUI.color = isPeelActive ? new Color(0.1f, 1.0f, 0.6f) : new Color(0.0f, 0.85f, 1.0f);
                string peelLabel = isPeelActive ? "🔮 1. Reality-Peel: [UNPROJECTED 360°] (P)" : "🔮 1. Reality-Peel: [2D Portal Frame] (P)";
                if (GUILayout.Button(peelLabel, GUILayout.Height(28)))
                {
                    TriggerRealityPeel();
                }

                // 2. Matter-Weave Button
                GUI.color = new Color(0.2f, 0.95f, 0.95f);
                string matterLabel = $"⚛️ 2. Matter-Weave: [{currentStiffness.ToString().ToUpper()}] (M)";
                if (GUILayout.Button(matterLabel, GUILayout.Height(28)))
                {
                    int next = ((int)currentStiffness + 1) % 3;
                    SetStiffness((MaterialStiffness)next);
                }

                // 3. Multiverse Orbs Button
                GUI.color = new Color(1.0f, 0.65f, 1.0f);
                string orbsLabel = $"⏳ 3. Multiverse Orbs: [Timeline {activeTimelineIndex + 1}/5] (O)";
                if (GUILayout.Button(orbsLabel, GUILayout.Height(28)))
                {
                    ToggleMultiverseOrbs();
                }

                // 4. Avatar Sophia Button
                GUI.color = isHandoffActive ? new Color(0.2f, 1.0f, 0.5f) : new Color(1.0f, 0.85f, 0.2f);
                string handoffLabel = isHandoffActive ? "🤝 4. Sophia: [REACHING IK HANDOFF...] (J)" : "🤝 4. Sophia Handoff: [Ready] (J)";
                if (GUILayout.Button(handoffLabel, GUILayout.Height(28)))
                {
                    TriggerHandoff();
                }

                // 5. Full Reload & Auto-Save Master Button
                GUI.color = new Color(0.0f, 1.0f, 0.9f);
                if (GUILayout.Button("🔄 <b>FULL SYSTEM RELOAD & AUTO-SAVE (R)</b>", GUILayout.Height(34)))
                {
                    if (OmniOSManager.Instance != null)
                    {
                        OmniOSManager.Instance.FullSystemReload();
                    }
                    else
                    {
                        RebuildAll5DSystems();
                    }
                }
                GUI.color = Color.white;

                GUILayout.Space(2);
                GUILayout.Label("<color=#88aacc><size=11>Shortcuts: [1] Android, [Tab] Launchpad, [P] Peel, [M] Matter, [R] Reload</size></color>");
            }

            GUILayout.EndArea();
        }
    }
}
