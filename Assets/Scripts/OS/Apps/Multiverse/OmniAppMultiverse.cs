using System;
using UnityEngine;
using OmniOS.Core;

namespace OmniOS.Apps.Multiverse
{
    /// <summary>
    /// OmniAppMultiverse - 5-Branch Temporal Phase Space Scrubber Window for OmniOS.
    /// Manages 5 floating miniature Multiverse Orbs and timeline scrubbing.
    /// </summary>
    [RequireComponent(typeof(SpatialWindow))]
    public class OmniAppMultiverse : MonoBehaviour
    {
        [Header("Multiverse State")]
        [SerializeField] private int selectedBranch = 2; // Default center branch
        [Range(-5.0f, 5.0f)]
        [SerializeField] private float timelineOffsetSeconds = 0.0f;

        private SpatialWindow spatialWindow;
        private readonly GameObject[] orbs = new GameObject[5];
        private readonly Material[] orbMaterials = new Material[5];

        private void Awake()
        {
            spatialWindow = GetComponent<SpatialWindow>();
            spatialWindow.SetDimensions(1.2f, 0.85f);
            BuildOrbs();
        }

        private void BuildOrbs()
        {
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");

            for (int i = 0; i < 5; i++)
            {
                GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                orb.name = $"Multiverse_Orb_{i}";
                orb.transform.SetParent(spatialWindow.ContentRoot, false);

                float xPos = (i - 2) * 0.22f;
                orb.transform.localPosition = new Vector3(xPos, 0.05f, -0.12f);
                orb.transform.localScale = Vector3.one * (i == selectedBranch ? 0.14f : 0.09f);

                orbMaterials[i] = new Material(unlitShader);
                orb.GetComponent<Renderer>().material = orbMaterials[i];
                orbs[i] = orb;
            }

            UpdateOrbColors();
        }

        private void Update()
        {
            float time = Time.time;
            for (int i = 0; i < 5; i++)
            {
                if (orbs[i] == null) continue;
                float bob = Mathf.Sin(time * 2f + i * 0.8f) * 0.015f;
                float xPos = (i - 2) * 0.22f;
                orbs[i].transform.localPosition = new Vector3(xPos, 0.05f + bob, -0.12f);
            }
        }

        public void SelectBranch(int index)
        {
            selectedBranch = Mathf.Clamp(index, 0, 4);
            for (int i = 0; i < 5; i++)
            {
                if (orbs[i] != null)
                {
                    orbs[i].transform.localScale = Vector3.one * (i == selectedBranch ? 0.14f : 0.09f);
                }
            }
            UpdateOrbColors();
            SpatialTheme.PlaySpatialTone(transform.position, 600f + selectedBranch * 150f);
        }

        private void UpdateOrbColors()
        {
            Color[] branchColors = new Color[]
            {
                new Color(1f, 0.3f, 0.3f, 0.85f),  // Alpha branch (Red)
                new Color(1f, 0.7f, 0.2f, 0.85f),  // Beta branch (Orange)
                new Color(0f, 0.9f, 1f, 0.95f),    // Prime branch (Cyan)
                new Color(0.3f, 0.9f, 0.4f, 0.85f), // Delta branch (Green)
                new Color(0.7f, 0.3f, 1f, 0.85f)   // Omega branch (Purple)
            };

            for (int i = 0; i < 5; i++)
            {
                if (orbMaterials[i] != null)
                {
                    Color baseCol = branchColors[i];
                    if (i != selectedBranch) baseCol.a = 0.45f;
                    orbMaterials[i].color = baseCol;
                }
            }
        }

        private void OnGUI()
        {
            if (!OmniOSManager.Show2DOverlay) return;
            if (!spatialWindow.IsOpen || spatialWindow.IsMinimized) return;

            GUILayout.BeginArea(new Rect(Screen.width - 340, 280, 320, 220), GUI.skin.box);
            GUILayout.Label("<b>⏳ MULTIVERSE TEMPORAL CHRONOS</b>");
            GUILayout.Space(4);

            string[] names = { "Branch Alpha", "Branch Beta", "Branch Prime (Now)", "Branch Delta", "Branch Omega" };
            GUILayout.Label($"Selected: <b><color=cyan>{names[selectedBranch]}</color></b>");
            GUILayout.Label($"Time Offset: {timelineOffsetSeconds:+0.00;-0.00;0.00} sec");

            timelineOffsetSeconds = GUILayout.HorizontalSlider(timelineOffsetSeconds, -5.0f, 5.0f);

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            for (int i = 0; i < 5; i++)
            {
                GUI.color = (i == selectedBranch) ? Color.cyan : Color.white;
                if (GUILayout.Button($"B{i + 1}", GUILayout.Height(28)))
                {
                    SelectBranch(i);
                }
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            if (GUILayout.Button("Commit Timeline to Reality", GUILayout.Height(30)))
            {
                SpatialTheme.PlaySpatialTone(transform.position, 1300f, 0.1f);
            }

            GUILayout.EndArea();
        }
    }
}
