using System;
using UnityEngine;
using OmniOS.Core;

namespace OmniOS.Apps.SpatialCanvas
{
    /// <summary>
    /// OmniAppSpatialCanvas - 3D Air Drawing & Marker Spawner Window for OmniOS.
    /// Integrates VRMarkerSpawner into the OS window system:
    /// - 3D Stroke Drawing mode vs 3D Ball Spawning mode
    /// - 10-swatch Color Palette + RGB Color Sliders
    /// - Ball radius slider (2cm - 40cm) & Stroke width slider
    /// - Step-by-Step Undo ('C' key)
    /// </summary>
    [RequireComponent(typeof(SpatialWindow))]
    public class OmniAppSpatialCanvas : MonoBehaviour
    {
        private SpatialWindow spatialWindow;
        private VRMarkerSpawner markerSpawner;

        private void Awake()
        {
            spatialWindow = GetComponent<SpatialWindow>();
            spatialWindow.SetDimensions(1.1f, 0.95f);
            markerSpawner = UnityEngine.Object.FindAnyObjectByType<VRMarkerSpawner>();
        }

        private void Update()
        {
            if (markerSpawner == null)
            {
                markerSpawner = UnityEngine.Object.FindAnyObjectByType<VRMarkerSpawner>();
            }
        }

        public void TriggerUndo()
        {
            if (markerSpawner != null)
            {
                markerSpawner.UndoLastAction();
                SpatialTheme.PlaySpatialTone(transform.position, 650f, 0.05f);
            }
        }

        private void OnGUI()
        {
            if (!OmniOSManager.Show2DOverlay) return;
            if (!spatialWindow.IsOpen || spatialWindow.IsMinimized) return;
            if (markerSpawner == null) return;

            GUILayout.BeginArea(new Rect(20, Screen.height - 350, 310, 330), GUI.skin.box);
            GUILayout.Label("<b>🎨 SPATIAL CANVAS (3D Air Art)</b>");
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            GUI.color = markerSpawner.CurrentInteractionMode == VRMarkerSpawner.InteractionMode.DrawStroke ? Color.cyan : Color.white;
            if (GUILayout.Button("Draw 3D Stroke", GUILayout.Height(30)))
            {
                markerSpawner.SetInteractionMode(VRMarkerSpawner.InteractionMode.DrawStroke);
            }

            GUI.color = markerSpawner.CurrentInteractionMode == VRMarkerSpawner.InteractionMode.SpawnBall ? Color.cyan : Color.white;
            if (GUILayout.Button("Spawn 3D Ball", GUILayout.Height(30)))
            {
                markerSpawner.SetInteractionMode(VRMarkerSpawner.InteractionMode.SpawnBall);
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label($"Ball Diameter: {markerSpawner.BallRadius * 200f:F1} cm");
            float newRadius = GUILayout.HorizontalSlider(markerSpawner.BallRadius, 0.02f, 0.40f);
            if (Mathf.Abs(newRadius - markerSpawner.BallRadius) > 0.005f)
            {
                markerSpawner.SetBallRadius(newRadius);
            }

            GUILayout.Label($"Stroke Width: {markerSpawner.StrokeWidth * 100f:F1} cm");
            float newWidth = GUILayout.HorizontalSlider(markerSpawner.StrokeWidth, 0.005f, 0.06f);
            if (Mathf.Abs(newWidth - markerSpawner.StrokeWidth) > 0.002f)
            {
                markerSpawner.SetStrokeWidth(newWidth);
            }

            GUILayout.Space(8);
            GUI.color = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("UNDO LAST ACTION ('C')", GUILayout.Height(34)))
            {
                TriggerUndo();
            }
            GUI.color = Color.white;

            GUILayout.EndArea();
        }
    }
}
