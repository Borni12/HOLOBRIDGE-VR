using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using OmniOS.Core;
using OmniOS.Apps.CameraApp;
using OmniOS.Apps.VideoReality;

namespace OmniOS.Apps.Gallery
{
    /// <summary>
    /// OmniAppGallery - Meta OS Style Spatial Media Gallery.
    /// Manages captured spatial photos and video memories with instant 3D Reality unprojection.
    /// </summary>
    [RequireComponent(typeof(SpatialWindow))]
    public class OmniAppGallery : MonoBehaviour
    {
        public struct GalleryItem
        {
            public string title;
            public string filePath;
            public Texture2D thumbnail;
        }

        private SpatialWindow spatialWindow;
        private readonly List<GalleryItem> galleryItems = new List<GalleryItem>();
        private int selectedIndex = -1;

        // Visual Preview Quad inside window
        private GameObject previewQuad;
        private Material previewMaterial;

        private void Awake()
        {
            spatialWindow = GetComponent<SpatialWindow>();
            spatialWindow.SetDimensions(1.35f, 0.95f);

            BuildPreviewScreen();
            LoadSavedPhotos();
        }

        private void OnEnable()
        {
            SpatialCameraDevice.OnPhotoCaptured += HandleNewPhoto;
        }

        private void OnDisable()
        {
            SpatialCameraDevice.OnPhotoCaptured -= HandleNewPhoto;
        }

        private void BuildPreviewScreen()
        {
            previewQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            previewQuad.name = "Gallery_PreviewScreen";
            previewQuad.transform.SetParent(spatialWindow.ContentRoot, false);
            previewQuad.transform.localScale = new Vector3(1.18f, 0.68f, 1f);
            previewQuad.transform.localPosition = new Vector3(0, 0.05f, -0.01f);

            var colliders = previewQuad.GetComponents<Collider>();
            foreach (var c in colliders) Destroy(c);

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
            previewMaterial = new Material(unlitShader);
            previewMaterial.color = new Color(0.08f, 0.12f, 0.18f, 0.95f);
            previewQuad.GetComponent<Renderer>().material = previewMaterial;
        }

        private void LoadSavedPhotos()
        {
            galleryItems.Clear();
            string dir = Path.Combine(Application.persistentDataPath, "SpatialPhotos");
            if (Directory.Exists(dir))
            {
                string[] files = Directory.GetFiles(dir, "*.png");
                foreach (string f in files)
                {
                    byte[] bytes = File.ReadAllBytes(f);
                    Texture2D tex = new Texture2D(2, 2);
                    if (tex.LoadImage(bytes))
                    {
                        galleryItems.Add(new GalleryItem
                        {
                            title = Path.GetFileNameWithoutExtension(f),
                            filePath = f,
                            thumbnail = tex
                        });
                    }
                }
            }

            if (galleryItems.Count > 0)
            {
                SelectPhoto(galleryItems.Count - 1);
            }
        }

        private void HandleNewPhoto(Texture2D photo, string path)
        {
            galleryItems.Add(new GalleryItem
            {
                title = Path.GetFileNameWithoutExtension(path),
                filePath = path,
                thumbnail = photo
            });
            SelectPhoto(galleryItems.Count - 1);
        }

        public void SelectPhoto(int index)
        {
            if (index < 0 || index >= galleryItems.Count) return;
            selectedIndex = index;
            if (previewMaterial != null && galleryItems[selectedIndex].thumbnail != null)
            {
                previewMaterial.mainTexture = galleryItems[selectedIndex].thumbnail;
                previewMaterial.color = Color.white;
            }
            SpatialTheme.PlaySpatialTone(transform.position, 900f);
        }

        public void LiftSelectedPhotoToReality()
        {
            if (selectedIndex < 0 || selectedIndex >= galleryItems.Count) return;

            Texture2D tex = galleryItems[selectedIndex].thumbnail;
            var vidManager = UnityEngine.Object.FindAnyObjectByType<VideoRealityManager>();
            if (vidManager != null)
            {
                vidManager.ImmersionLevel = 1.0f;
                vidManager.DepthRelief = 0.70f;
            }

            OmniOSManager.Instance?.ShowNotificationToast($"🌐 Memory Lifted: '{galleryItems[selectedIndex].title}' is now your 3D VR World!");
            SpatialTheme.PlaySpatialTone(transform.position, 1400f, 0.12f);
        }

        private void OnGUI()
        {
            if (!OmniOSManager.Show2DOverlay) return;
            if (!spatialWindow.IsOpen || spatialWindow.IsMinimized) return;

            GUILayout.BeginArea(new Rect(20, Screen.height - 380, 320, 360), GUI.skin.box);
            GUILayout.Label("<b>🖼️ SPATIAL MEDIA GALLERY</b>");
            GUILayout.Space(4);

            GUILayout.Label($"Saved Captures: <b>{galleryItems.Count}</b> photos/memories");

            if (galleryItems.Count == 0)
            {
                GUILayout.Label("<color=yellow>No photos saved yet.\nUse OmniCam (Press 'K') to capture photos!</color>");
            }
            else
            {
                GUILayout.Label($"Selected: <b>{galleryItems[selectedIndex].title}</b>");

                // Thumbnail strip / selector
                GUILayout.BeginHorizontal();
                int startIdx = Mathf.Max(0, galleryItems.Count - 4);
                for (int i = startIdx; i < galleryItems.Count; i++)
                {
                    GUI.color = (i == selectedIndex) ? Color.cyan : Color.white;
                    if (GUILayout.Button($"IMG_{i + 1}", GUILayout.Width(65), GUILayout.Height(36)))
                    {
                        SelectPhoto(i);
                    }
                }
                GUI.color = Color.white;
                GUILayout.EndHorizontal();

                GUILayout.Space(8);
                GUI.color = new Color(0.0f, 0.88f, 1.0f);
                if (GUILayout.Button("🌐 ENTER 360° PHOTO REALITY", GUILayout.Height(40)))
                {
                    LiftSelectedPhotoToReality();
                }
                GUI.color = Color.white;
            }

            GUILayout.Space(6);
            if (GUILayout.Button("Refresh Gallery from Disk", GUILayout.Height(28)))
            {
                LoadSavedPhotos();
            }

            GUILayout.EndArea();
        }
    }
}
