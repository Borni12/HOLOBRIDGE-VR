using System;
using UnityEngine;
using UnityEngine.Events;
using OmniOS.Core;

namespace OmniOS.Interaction
{
    /// <summary>
    /// SpatialSlider3D - Meta Quest Pro / Quest 3 style 3D interactive slider.
    /// Lets users smoothly drag values (0.0 to 1.0) using 6-DoF pointer laser or mouse.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class SpatialSlider3D : MonoBehaviour
    {
        [Header("Slider Configuration")]
        [SerializeField] private float sliderLength = 0.35f;
        [Range(0f, 1f)]
        [SerializeField] private float currentValue = 0.5f;
        [SerializeField] private string sliderLabel = "Slider";

        [System.Serializable]
        public class SliderEvent : UnityEvent<float> { }
        public SliderEvent onValueChanged = new SliderEvent();

        // Components
        private BoxCollider trackCollider;
        private GameObject thumbBead;
        private TextMesh labelMesh;

        public float Value
        {
            get => currentValue;
            set
            {
                currentValue = Mathf.Clamp01(value);
                UpdateThumbPosition();
            }
        }

        private void Awake()
        {
            trackCollider = GetComponent<BoxCollider>();
            trackCollider.size = new Vector3(sliderLength + 0.05f, 0.045f, 0.04f);

            BuildVisuals();
            UpdateThumbPosition();
        }

        private void BuildVisuals()
        {
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");

            // 1. Slider Track Capsule
            GameObject track = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            track.name = "Slider_Track";
            track.transform.SetParent(transform, false);
            track.transform.localScale = new Vector3(0.012f, sliderLength * 0.5f, 0.012f);
            track.transform.localRotation = Quaternion.Euler(0, 0, 90f);
            Destroy(track.GetComponent<Collider>());

            var trackMat = new Material(unlit);
            trackMat.color = new Color(0.18f, 0.22f, 0.28f, 0.9f);
            track.GetComponent<Renderer>().material = trackMat;

            // 2. Draggable Thumb Bead (Glowing Cyan Sphere)
            thumbBead = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            thumbBead.name = "Slider_ThumbBead";
            thumbBead.transform.SetParent(transform, false);
            thumbBead.transform.localScale = Vector3.one * 0.038f;
            Destroy(thumbBead.GetComponent<Collider>());

            var thumbMat = new Material(unlit);
            thumbMat.color = new Color(0.0f, 0.88f, 1.0f, 1.0f);
            thumbBead.GetComponent<Renderer>().material = thumbMat;

            // 3. Label Text
            GameObject textObj = new GameObject("Slider_Label");
            textObj.transform.SetParent(transform, false);
            textObj.transform.localPosition = new Vector3(0, 0.035f, -0.01f);

            labelMesh = textObj.AddComponent<TextMesh>();
            labelMesh.text = $"{sliderLabel}: {(currentValue * 100f):F0}%";
            labelMesh.fontSize = 28;
            labelMesh.characterSize = 0.003f;
            labelMesh.alignment = TextAlignment.Center;
            labelMesh.anchor = TextAnchor.MiddleCenter;
            labelMesh.color = Color.white;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font != null)
            {
                labelMesh.font = font;
                var mr = textObj.GetComponent<MeshRenderer>();
                if (mr != null && font.material != null) mr.material = font.material;
            }
        }

        public void ProcessDrag(Vector3 hitPoint)
        {
            Vector3 localHit = transform.InverseTransformPoint(hitPoint);
            float normalized = Mathf.Clamp01((localHit.x + sliderLength * 0.5f) / sliderLength);

            if (Mathf.Abs(normalized - currentValue) > 0.005f)
            {
                currentValue = normalized;
                UpdateThumbPosition();
                onValueChanged?.Invoke(currentValue);
                if (labelMesh != null) labelMesh.text = $"{sliderLabel}: {(currentValue * 100f):F0}%";
            }
        }

        private void UpdateThumbPosition()
        {
            if (thumbBead != null)
            {
                float x = (currentValue - 0.5f) * sliderLength;
                thumbBead.transform.localPosition = new Vector3(x, 0, -0.01f);
            }
        }
    }
}
