using System;
using System.Collections.Generic;
using UnityEngine;
using OmniOS.Core;

namespace HoloBridge.Collaboration
{
    /// <summary>
    /// SpatialAnnotationSystem - Collaborative 3D Spatial Annotations & Pins for IEEE VR 2027.
    /// Allows both Desktop and VR collaborators to annotate, inspect, and organize shared 3D content.
    /// Features:
    /// - 3D Pin Callouts attached directly to component geometry (move with exploded views)
    /// - Author identification tags ([VR User] vs [Desktop User])
    /// - Live engineering notes (Clearance warnings, alignment verification, inspection markers)
    /// - One-click pin templates
    /// </summary>
    public class SpatialAnnotationSystem : MonoBehaviour
    {
        [System.Serializable]
        public class SpatialAnnotationPin
        {
            public string pinId;
            public string authorName;
            public string noteText;
            public Color badgeColor;
            public GameObject pinGameObject;
            public Transform parentPart;
        }

        [Header("State")]
        [SerializeField] private List<SpatialAnnotationPin> activePins = new List<SpatialAnnotationPin>();

        public List<SpatialAnnotationPin> ActivePins => activePins;

        private SharedDigitalTwinAssembly targetAssembly;

        private void Start()
        {
            targetAssembly = UnityEngine.Object.FindAnyObjectByType<SharedDigitalTwinAssembly>();
            SpawnDefaultEngineeringAnnotations();
        }

        private void SpawnDefaultEngineeringAnnotations()
        {
            if (targetAssembly == null) return;

            // Pin 1: Bearing Clearance Callout
            var bearing = targetAssembly.GetPart(SharedDigitalTwinAssembly.ComponentType.InternalBearing);
            if (bearing != null && bearing.gameObject != null)
            {
                CreateAnnotationPin(
                    bearing.gameObject.transform,
                    new Vector3(0.12f, 0.12f, 0f),
                    "VR Collaborator",
                    "⚠️ Clearance < 0.20 mm (PCDD Focus Area)",
                    new Color(1.0f, 0.35f, 0.25f)
                );
            }

            // Pin 2: Drive Shaft Torque Inspection
            var shaft = targetAssembly.GetPart(SharedDigitalTwinAssembly.ComponentType.DriveShaft);
            if (shaft != null && shaft.gameObject != null)
            {
                CreateAnnotationPin(
                    shaft.gameObject.transform,
                    new Vector3(0.08f, 0.15f, 0.05f),
                    "Desktop Collaborator",
                    "✅ Torque Verified (850 Nm ± 5%)",
                    new Color(0.20f, 0.95f, 0.45f)
                );
            }
        }

        /// <summary>
        /// Spawns a 3D annotation pin anchored to a specific component.
        /// </summary>
        public SpatialAnnotationPin CreateAnnotationPin(Transform parentPart, Vector3 localOffset, string author, string note, Color color)
        {
            string id = "PIN_" + Guid.NewGuid().ToString().Substring(0, 6);

            GameObject pinObj = new GameObject($"SpatialPin_{id}");
            pinObj.transform.SetParent(parentPart, false);
            pinObj.transform.localPosition = localOffset;

            // 1. 3D Pin Point (Sphere + Needle)
            GameObject needle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            needle.name = "Pin_Needle";
            needle.transform.SetParent(pinObj.transform, false);
            needle.transform.localScale = new Vector3(0.008f, 0.045f, 0.008f);
            needle.transform.localPosition = new Vector3(0, 0.022f, 0);

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Pin_Head";
            head.transform.SetParent(pinObj.transform, false);
            head.transform.localScale = Vector3.one * 0.035f;
            head.transform.localPosition = new Vector3(0, 0.052f, 0);

            // Shaders
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            var headMat = new Material(unlit);
            headMat.color = color;
            head.GetComponent<Renderer>().material = headMat;
            needle.GetComponent<Renderer>().material = headMat;

            // Remove needle colliders so raycasts hit head or callout cleanly
            Destroy(needle.GetComponent<Collider>());

            // 2. 3D Callout Billboard Tag
            GameObject tagObj = new GameObject("Pin_Tag");
            tagObj.transform.SetParent(pinObj.transform, false);
            tagObj.transform.localPosition = new Vector3(0, 0.095f, 0);

            // Billboard Tag Backplate
            GameObject tagBackplate = GameObject.CreatePrimitive(PrimitiveType.Quad);
            tagBackplate.name = "Tag_Backdrop";
            tagBackplate.transform.SetParent(tagObj.transform, false);
            tagBackplate.transform.localScale = new Vector3(0.38f, 0.075f, 1f);
            Destroy(tagBackplate.GetComponent<Collider>());
            tagBackplate.GetComponent<Renderer>().material = SpatialTheme.CreateGlassMaterial(
                new Color(0.06f, 0.10f, 0.16f, 0.92f),
                color
            );

            // Text Label
            GameObject textObj = new GameObject("Tag_Text");
            textObj.transform.SetParent(tagObj.transform, false);
            textObj.transform.localPosition = new Vector3(0, 0, -0.005f);

            var textMesh = textObj.AddComponent<TextMesh>();
            textMesh.text = $"[{author}]\n{note}";
            textMesh.fontSize = 24;
            textMesh.characterSize = 0.0018f;
            textMesh.fontStyle = FontStyle.Bold;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.color = Color.white;

            // Add billboard script to keep tag facing user
            tagObj.AddComponent<BillboardLookAtCamera>();

            var pin = new SpatialAnnotationPin
            {
                pinId = id,
                authorName = author,
                noteText = note,
                badgeColor = color,
                pinGameObject = pinObj,
                parentPart = parentPart
            };

            activePins.Add(pin);
            SpatialAudioEngine.Instance?.PlayHoverTick();
            return pin;
        }

        public void CreateDemoPin()
        {
            if (targetAssembly == null) targetAssembly = UnityEngine.Object.FindAnyObjectByType<SharedDigitalTwinAssembly>();
            if (targetAssembly == null) return;

            var nozzle = targetAssembly.GetPart(SharedDigitalTwinAssembly.ComponentType.CombustionNozzle);
            if (nozzle != null && nozzle.gameObject != null)
            {
                CreateAnnotationPin(
                    nozzle.gameObject.transform,
                    new Vector3(0.05f, 0.12f, 0.05f),
                    "Collaborative Pin",
                    "🔥 Combustion Stage Inspection",
                    new Color(1f, 0.8f, 0.1f)
                );
            }
        }

        public void ClearAllPins()
        {
            foreach (var p in activePins)
            {
                if (p.pinGameObject != null) Destroy(p.pinGameObject);
            }
            activePins.Clear();
            SpatialAudioEngine.Instance?.PlayButtonClick();
            OmniOSManager.Instance?.ShowNotificationToast("🗑️ All 3D Annotations Cleared");
        }
    }

    /// <summary>
    /// Helper component to orient callout tags towards the active camera.
    /// </summary>
    public class BillboardLookAtCamera : MonoBehaviour
    {
        private Camera activeCam;

        private void Update()
        {
            if (activeCam == null) activeCam = Camera.main ?? UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (activeCam != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - activeCam.transform.position);
            }
        }
    }
}
