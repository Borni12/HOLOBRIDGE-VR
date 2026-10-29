using System;
using UnityEngine;

namespace OmniOS.Core
{
    /// <summary>
    /// SpatialWindow - Standard 3D Spatial Computing Window for OmniOS.
    /// Implements:
    /// 1. VisionOS-style frosted glass backplate with subtle neon rim glow.
    /// 2. Header Bar with 3D Minimize (-), Pin, and Close (X) buttons.
    /// 3. Bottom Grab Bar (Pill Handle) for 6-DoF repositioning and depth placement.
    /// 4. Billboard orientation facing user with smooth spring damping.
    /// 5. Smooth scale transitions on open, minimize, and close.
    /// </summary>
    [DisallowMultipleComponent]
    public class SpatialWindow : MonoBehaviour
    {
        [Header("Window Metadata")]
        [SerializeField] private string appTitle = "Spatial Application";
        [SerializeField] private string appId = "app_default";
        [SerializeField] private Vector2 windowDimensions = new Vector2(1.2f, 0.8f);

        [Header("Window Behavior")]
        [SerializeField] private bool faceCamera = true;
        [SerializeField] private float rotationDamping = 8.0f;
        [SerializeField] private bool isPinned = false;

        // Hierarchy References
        public Transform ContentRoot { get; private set; }
        public GameObject GrabBar { get; private set; }
        public GameObject HeaderBar { get; private set; }
        public GameObject BtnMinimize { get; private set; }
        public GameObject BtnClose { get; private set; }
        public GameObject BtnPin { get; private set; }

        private Transform cameraTransform;

        // Window Lifecycle State
        public bool IsOpen { get; private set; } = true;
        public bool IsMinimized { get; private set; } = false;
        private Vector3 targetLocalScale = Vector3.one;
        private Vector3 initialPosition;
        private Quaternion initialRotation;

        public event Action OnWindowClosed;
        public event Action OnWindowMinimized;
        public event Action OnWindowRestored;

        public string AppId => appId;
        public string AppTitle => appTitle;

        private void Awake()
        {
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            BuildWindowChrome();
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }

        private void Update()
        {
            // Smooth Billboard to face user camera
            if (faceCamera && !isPinned && cameraTransform != null)
            {
                Vector3 toCam = transform.position - cameraTransform.position;
                if (toCam.sqrMagnitude > 0.04f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(toCam.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationDamping);
                }
            }

            // Smooth scale interpolation (open/minimize animation)
            if (transform.localScale != targetLocalScale)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, targetLocalScale, Time.deltaTime * 14.0f);
            }
        }

        private void BuildWindowChrome()
        {
            // 1. Backplate Quad (Glass UI)
            GameObject backplate = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backplate.name = "Window_GlassBackplate";
            backplate.transform.SetParent(transform, false);
            backplate.transform.localScale = new Vector3(windowDimensions.x, windowDimensions.y, 1f);
            backplate.transform.localPosition = Vector3.zero;

            // Destroy all colliders on decorative backplate so it NEVER intercepts raycast clicks meant for buttons
            var backplateColliders = backplate.GetComponents<Collider>();
            foreach (var c in backplateColliders) Destroy(c);

            Material glassMat = SpatialTheme.CreateGlassMaterial(SpatialTheme.GlassBase, SpatialTheme.GlassBorder);
            backplate.GetComponent<Renderer>().material = glassMat;

            // 2. Header Bar
            HeaderBar = new GameObject("Window_HeaderBar");
            HeaderBar.transform.SetParent(transform, false);
            HeaderBar.transform.localPosition = new Vector3(0, windowDimensions.y * 0.5f + 0.045f, -0.01f);

            BuildHeaderButtons();

            // 3. Bottom Grab Bar (Pill Handle for moving window)
            GrabBar = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            GrabBar.name = "Window_GrabBar";
            GrabBar.transform.SetParent(transform, false);
            GrabBar.transform.localScale = new Vector3(0.025f, windowDimensions.x * 0.35f, 0.025f);
            GrabBar.transform.localRotation = Quaternion.Euler(0, 0, 90f);
            GrabBar.transform.localPosition = new Vector3(0, -windowDimensions.y * 0.5f - 0.045f, -0.01f);

            Material grabMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
            grabMat.color = new Color(0.0f, 0.85f, 1.0f, 0.75f);
            GrabBar.GetComponent<Renderer>().material = grabMat;

            // 4. Content Root
            GameObject contentObj = new GameObject("Window_ContentRoot");
            contentObj.transform.SetParent(transform, false);
            contentObj.transform.localPosition = new Vector3(0, 0, -0.02f);
            ContentRoot = contentObj.transform;
        }

        private void BuildHeaderButtons()
        {
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            float buttonSize = 0.038f;
            float rightEdge = windowDimensions.x * 0.5f - 0.06f;

            // Close Button (Red)
            BtnClose = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            BtnClose.name = "Window_Btn_Close";
            BtnClose.transform.SetParent(HeaderBar.transform, false);
            BtnClose.transform.localScale = Vector3.one * buttonSize;
            BtnClose.transform.localPosition = new Vector3(rightEdge, 0, -0.01f);
            var closeMat = new Material(unlit);
            closeMat.color = new Color(1.0f, 0.28f, 0.28f, 0.95f);
            BtnClose.GetComponent<Renderer>().material = closeMat;

            // Minimize Button (Amber / Yellow)
            BtnMinimize = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            BtnMinimize.name = "Window_Btn_Minimize";
            BtnMinimize.transform.SetParent(HeaderBar.transform, false);
            BtnMinimize.transform.localScale = Vector3.one * buttonSize;
            BtnMinimize.transform.localPosition = new Vector3(rightEdge - 0.055f, 0, -0.01f);
            var minMat = new Material(unlit);
            minMat.color = new Color(1.0f, 0.78f, 0.15f, 0.95f);
            BtnMinimize.GetComponent<Renderer>().material = minMat;

            // Pin Button (Cyan)
            BtnPin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            BtnPin.name = "Window_Btn_Pin";
            BtnPin.transform.SetParent(HeaderBar.transform, false);
            BtnPin.transform.localScale = Vector3.one * buttonSize;
            BtnPin.transform.localPosition = new Vector3(rightEdge - 0.11f, 0, -0.01f);
            var pinMat = new Material(unlit);
            pinMat.color = new Color(0.0f, 0.85f, 1.0f, 0.95f);
            BtnPin.GetComponent<Renderer>().material = pinMat;
        }

        public void SetPinned(bool pinned)
        {
            isPinned = pinned;
            SpatialTheme.PlaySpatialTone(transform.position, pinned ? 1200f : 700f);
        }

        public void ToggleMinimize()
        {
            if (IsMinimized) Restore();
            else Minimize();
        }

        public void Minimize()
        {
            IsMinimized = true;
            targetLocalScale = Vector3.one * 0.001f;
            SpatialTheme.PlaySpatialTone(transform.position, 600f, 0.06f);
            OnWindowMinimized?.Invoke();
        }

        public void Restore()
        {
            IsMinimized = false;
            gameObject.SetActive(true);
            targetLocalScale = Vector3.one;
            SpatialTheme.PlaySpatialTone(transform.position, 950f, 0.06f);
            OnWindowRestored?.Invoke();
        }

        public void CloseWindow()
        {
            IsOpen = false;
            targetLocalScale = Vector3.zero;
            SpatialTheme.PlaySpatialTone(transform.position, 400f, 0.08f);
            OnWindowClosed?.Invoke();
            gameObject.SetActive(false);
        }

        public void OpenWindow()
        {
            IsOpen = true;
            IsMinimized = false;
            gameObject.SetActive(true);
            transform.localScale = Vector3.one * 0.05f;
            targetLocalScale = Vector3.one;
            SpatialTheme.PlaySpatialTone(transform.position, 1100f, 0.08f);
        }

        public void SetDimensions(float width, float height)
        {
            windowDimensions = new Vector2(width, height);
            Transform backplate = transform.Find("Window_GlassBackplate");
            if (backplate != null) backplate.localScale = new Vector3(width, height, 1f);
            if (HeaderBar != null)
            {
                HeaderBar.transform.localPosition = new Vector3(0, height * 0.5f + 0.045f, -0.01f);
                float rightEdge = width * 0.5f - 0.06f;
                if (BtnClose != null) BtnClose.transform.localPosition = new Vector3(rightEdge, 0, -0.01f);
                if (BtnMinimize != null) BtnMinimize.transform.localPosition = new Vector3(rightEdge - 0.055f, 0, -0.01f);
                if (BtnPin != null) BtnPin.transform.localPosition = new Vector3(rightEdge - 0.11f, 0, -0.01f);
            }
            if (GrabBar != null)
            {
                GrabBar.transform.localScale = new Vector3(0.025f, width * 0.35f, 0.025f);
                GrabBar.transform.localPosition = new Vector3(0, -height * 0.5f - 0.045f, -0.01f);
            }
        }
    }
}
