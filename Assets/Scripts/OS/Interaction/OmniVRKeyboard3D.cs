using System;
using System.Collections.Generic;
using UnityEngine;
using OmniOS.Core;
using OmniOS.Apps.MediaHub;

namespace OmniOS.Interaction
{
    /// <summary>
    /// OmniVRKeyboard3D - Full 3D In-World Virtual Keyboard for VR & Spatial Computing.
    /// Features:
    /// - Complete 5-row QWERTY layout with real 3D interactive keys (SpatialButton3D).
    /// - High-fidelity frosted glass chassis matching VisionOS / Android 15 Material You.
    /// - Shift toggle (dynamically switches letter keys between uppercase & lowercase).
    /// - Real-time typing display bar with blinking cursor.
    /// - Audio-haptic feedback on every single key press.
    /// - Quick domain shortcuts (https://, www., .com).
    /// - 100% interactive via VR Controller Laser Pointer (Quest 3/Pro) & PC Desktop Mouse.
    /// </summary>
    public class OmniVRKeyboard3D : MonoBehaviour
    {
        [Header("Keyboard Configuration")]
        [SerializeField] private bool isOpen = false;
        [SerializeField] private bool isShiftActive = true; // Default uppercase for easy typing

        private OmniAppMediaHub targetHub;
        private GameObject keyboardRoot;
        private TextMesh livePreviewText;
        private SpatialButton3D shiftButton;
        private string currentBuffer = "";

        private readonly List<(SpatialButton3D btn, char upperChar, char lowerChar)> letterKeyList = 
            new List<(SpatialButton3D, char, char)>();

        public bool IsOpen => isOpen;

        public void Initialize(OmniAppMediaHub hub)
        {
            targetHub = hub;
            BuildKeyboardUI();
            Hide(); // Initially closed until opened by search/URL focus or keyboard button
        }

        private void BuildKeyboardUI()
        {
            // 1. Keyboard Chassis Root
            keyboardRoot = new GameObject("Keyboard_Chassis_Root");
            keyboardRoot.transform.SetParent(transform, false);
            keyboardRoot.transform.localPosition = Vector3.zero;

            // 2. Frosted Glass Chassis Backplate
            GameObject chassis = GameObject.CreatePrimitive(PrimitiveType.Quad);
            chassis.name = "VR_Keyboard_Backplate";
            chassis.transform.SetParent(keyboardRoot.transform, false);
            chassis.transform.localScale = new Vector3(1.48f, 0.54f, 1f);
            chassis.transform.localPosition = new Vector3(0, 0, 0.005f);
            var colliders = chassis.GetComponents<Collider>();
            foreach (var c in colliders) DestroyImmediate(c);

            Material glassMat = SpatialTheme.CreateGlassMaterial(
                new Color(0.04f, 0.07f, 0.12f, 0.94f),
                new Color(0.0f, 0.75f, 1.0f, 0.90f)
            );
            chassis.GetComponent<Renderer>().material = glassMat;

            // 3. Live Preview / Header Ribbon
            GameObject headerQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            headerQuad.name = "VR_Keyboard_Header";
            headerQuad.transform.SetParent(keyboardRoot.transform, false);
            headerQuad.transform.localScale = new Vector3(1.42f, 0.055f, 1f);
            headerQuad.transform.localPosition = new Vector3(0, 0.215f, -0.01f);
            foreach (var c in headerQuad.GetComponents<Collider>()) DestroyImmediate(c);
            headerQuad.GetComponent<Renderer>().material = SpatialTheme.CreateGlassMaterial(
                new Color(0.08f, 0.14f, 0.22f, 0.92f),
                new Color(0.20f, 0.85f, 1.0f, 0.80f)
            );

            // Live Typing Preview Text
            GameObject textObj = new GameObject("LivePreview_Text");
            textObj.transform.SetParent(keyboardRoot.transform, false);
            textObj.transform.localPosition = new Vector3(-0.68f, 0.215f, -0.02f);
            livePreviewText = textObj.AddComponent<TextMesh>();
            livePreviewText.text = "⌨️ VR Virtual Keyboard: Click keys to type...";
            livePreviewText.fontSize = 24;
            livePreviewText.characterSize = 0.0024f;
            livePreviewText.fontStyle = FontStyle.Bold;
            livePreviewText.anchor = TextAnchor.MiddleLeft;
            livePreviewText.alignment = TextAlignment.Left;
            livePreviewText.color = new Color(0.92f, 0.96f, 1.0f, 1.0f);

            // 4. Construct Key Rows
            BuildRow0_Numbers(keyboardRoot.transform);
            BuildRow1_QWERTY(keyboardRoot.transform);
            BuildRow2_HomeRow(keyboardRoot.transform);
            BuildRow3_BottomRow(keyboardRoot.transform);
            BuildRow4_ActionRow(keyboardRoot.transform);

            UpdateLetterKeyLabels();
        }

        // ==================== ROW 0: NUMBERS & BASIC SYMBOLS ====================
        private void BuildRow0_Numbers(Transform parent)
        {
            string[] keys = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "/", "⌫ Back" };
            float yPos = 0.135f;
            float startX = -0.64f;
            float keyW = 0.088f;
            float spacing = 0.012f;

            for (int i = 0; i < keys.Length; i++)
            {
                string keyVal = keys[i];
                float currentW = (keyVal.Contains("Back")) ? 0.16f : keyW;
                float x = startX + i * (keyW + spacing) + (keyVal.Contains("Back") ? 0.035f : 0f);

                Color accent = keyVal.Contains("Back") 
                    ? new Color(1.0f, 0.35f, 0.35f) 
                    : new Color(0.35f, 0.70f, 1.0f);

                CreateKey(keyVal, new Vector3(x, yPos, -0.025f), new Vector2(currentW, 0.055f), parent, () => {
                    if (keyVal.Contains("Back")) DispatchKey("BACKSPACE");
                    else DispatchKey(keyVal);
                }, accent);
            }
        }

        // ==================== ROW 1: Q W E R T Y U I O P ====================
        private void BuildRow1_QWERTY(Transform parent)
        {
            string upper = "QWERTYUIOP";
            float yPos = 0.065f;
            float keyW = 0.122f;
            float spacing = 0.016f;
            float totalW = upper.Length * keyW + (upper.Length - 1) * spacing;
            float startX = -totalW * 0.5f + keyW * 0.5f;

            for (int i = 0; i < upper.Length; i++)
            {
                char u = upper[i];
                char l = char.ToLowerInvariant(u);
                float x = startX + i * (keyW + spacing);

                var btn = CreateKey(u.ToString(), new Vector3(x, yPos, -0.025f), new Vector2(keyW, 0.058f), parent, () => {
                    DispatchKey(isShiftActive ? u.ToString() : l.ToString());
                }, new Color(0.0f, 0.85f, 1.0f));

                letterKeyList.Add((btn, u, l));
            }
        }

        // ==================== ROW 2: A S D F G H J K L : ====================
        private void BuildRow2_HomeRow(Transform parent)
        {
            string upper = "ASDFGHJKL";
            float yPos = -0.005f;
            float keyW = 0.120f;
            float spacing = 0.016f;
            int count = upper.Length + 1; // + colon key
            float totalW = count * keyW + (count - 1) * spacing;
            float startX = -totalW * 0.5f + keyW * 0.5f;

            for (int i = 0; i < upper.Length; i++)
            {
                char u = upper[i];
                char l = char.ToLowerInvariant(u);
                float x = startX + i * (keyW + spacing);

                var btn = CreateKey(u.ToString(), new Vector3(x, yPos, -0.025f), new Vector2(keyW, 0.058f), parent, () => {
                    DispatchKey(isShiftActive ? u.ToString() : l.ToString());
                }, new Color(0.0f, 0.85f, 1.0f));

                letterKeyList.Add((btn, u, l));
            }

            // Colon / Semicolon key
            float lastX = startX + upper.Length * (keyW + spacing);
            CreateKey(":", new Vector3(lastX, yPos, -0.025f), new Vector2(keyW, 0.058f), parent, () => {
                DispatchKey(":");
            }, new Color(0.40f, 0.75f, 1.0f));
        }

        // ==================== ROW 3: SHIFT + Z X C V B N M . @ + CLEAR ====================
        private void BuildRow3_BottomRow(Transform parent)
        {
            float yPos = -0.075f;

            // 1. Shift Key
            shiftButton = CreateKey("⇧ Shift", new Vector3(-0.60f, yPos, -0.025f), new Vector2(0.15f, 0.058f), parent, () => {
                ToggleShift();
            }, new Color(0.85f, 0.45f, 1.0f));

            // 2. Letters Z-M
            string upper = "ZXCVBNM";
            float keyW = 0.106f;
            float spacing = 0.014f;
            float startX = -0.44f;

            for (int i = 0; i < upper.Length; i++)
            {
                char u = upper[i];
                char l = char.ToLowerInvariant(u);
                float x = startX + i * (keyW + spacing);

                var btn = CreateKey(u.ToString(), new Vector3(x, yPos, -0.025f), new Vector2(keyW, 0.058f), parent, () => {
                    DispatchKey(isShiftActive ? u.ToString() : l.ToString());
                }, new Color(0.0f, 0.85f, 1.0f));

                letterKeyList.Add((btn, u, l));
            }

            // 3. Dot key
            float dotX = startX + upper.Length * (keyW + spacing);
            CreateKey(".", new Vector3(dotX, yPos, -0.025f), new Vector2(0.08f, 0.058f), parent, () => {
                DispatchKey(".");
            }, new Color(0.40f, 0.75f, 1.0f));

            // 4. At Symbol @
            float atX = dotX + 0.08f * 0.5f + 0.08f * 0.5f + spacing;
            CreateKey("@", new Vector3(atX, yPos, -0.025f), new Vector2(0.08f, 0.058f), parent, () => {
                DispatchKey("@");
            }, new Color(0.40f, 0.75f, 1.0f));

            // 5. Clear Key
            float clearX = 0.60f;
            CreateKey("❌ Clear", new Vector3(clearX, yPos, -0.025f), new Vector2(0.14f, 0.058f), parent, () => {
                DispatchKey("CLEAR");
            }, new Color(1.0f, 0.35f, 0.35f));
        }

        // ==================== ROW 4: ACTION / SPACE / URL SHORTCUTS ====================
        private void BuildRow4_ActionRow(Transform parent)
        {
            float yPos = -0.155f;

            // 1. https:// shortcut
            CreateKey("https://", new Vector3(-0.58f, yPos, -0.025f), new Vector2(0.14f, 0.060f), parent, () => {
                DispatchKey("https://");
            }, new Color(0.25f, 0.70f, 1.0f));

            // 2. www. shortcut
            CreateKey("www.", new Vector3(-0.43f, yPos, -0.025f), new Vector2(0.12f, 0.060f), parent, () => {
                DispatchKey("www.");
            }, new Color(0.25f, 0.70f, 1.0f));

            // 3. Spacebar
            CreateKey("␣ [ SPACE ]", new Vector3(-0.11f, yPos, -0.025f), new Vector2(0.44f, 0.060f), parent, () => {
                DispatchKey("SPACE");
            }, new Color(0.15f, 0.60f, 0.95f));

            // 4. .com shortcut
            CreateKey(".com", new Vector3(0.20f, yPos, -0.025f), new Vector2(0.12f, 0.060f), parent, () => {
                DispatchKey(".com");
            }, new Color(0.25f, 0.70f, 1.0f));

            // 5. Search / Go ↵
            CreateKey("🔍 Go ↵", new Vector3(0.38f, yPos, -0.025f), new Vector2(0.18f, 0.060f), parent, () => {
                DispatchKey("ENTER");
            }, new Color(0.15f, 0.92f, 0.45f));

            // 6. Close / Hide Keyboard 🔽
            CreateKey("🔽 Hide", new Vector3(0.58f, yPos, -0.025f), new Vector2(0.14f, 0.060f), parent, () => {
                Hide();
            }, new Color(1.0f, 0.65f, 0.20f));
        }

        // ==================== KEYBOARD CONTROLS & LOGIC ====================

        public void ToggleShift()
        {
            isShiftActive = !isShiftActive;
            UpdateLetterKeyLabels();
            if (shiftButton != null)
            {
                shiftButton.ButtonText = isShiftActive ? "⇧ SHIFT" : "shift";
                shiftButton.SetColors(
                    isShiftActive ? new Color(0.25f, 0.12f, 0.38f, 0.95f) : new Color(0.08f, 0.12f, 0.20f, 0.95f),
                    isShiftActive ? new Color(1.0f, 0.50f, 1.0f) : new Color(0.85f, 0.45f, 1.0f)
                );
            }
            SpatialAudioEngine.Instance?.PlayButtonClick();
        }

        private void UpdateLetterKeyLabels()
        {
            foreach (var item in letterKeyList)
            {
                if (item.btn != null)
                {
                    item.btn.ButtonText = isShiftActive ? item.upperChar.ToString() : item.lowerChar.ToString();
                }
            }
        }

        private void DispatchKey(string key)
        {
            SpatialAudioEngine.Instance?.PlayButtonClick();
            if (targetHub != null)
            {
                targetHub.OnVirtualKeyTyped(key);
            }
        }

        public void SetPreviewText(string text)
        {
            currentBuffer = text ?? "";
            UpdatePreviewText();
        }

        private void Update()
        {
            if (!isOpen || livePreviewText == null) return;
            UpdatePreviewText();
        }

        private void UpdatePreviewText()
        {
            if (livePreviewText != null)
            {
                string cursorSymbol = ((int)(Time.unscaledTime * 3.0f) % 2 == 0) ? "|" : " ";
                string disp = string.IsNullOrEmpty(currentBuffer) ? "Click 3D keys below to type" : currentBuffer;
                livePreviewText.text = $"⌨️ Live Input: \"{disp}{cursorSymbol}\"";
            }
        }

        public void Show()
        {
            isOpen = true;
            gameObject.SetActive(true);
            if (keyboardRoot != null) keyboardRoot.SetActive(true);
            UpdatePreviewText();
            SpatialAudioEngine.Instance?.PlayWindowOpen();
        }

        public void Hide()
        {
            isOpen = false;
            gameObject.SetActive(false);
            if (keyboardRoot != null) keyboardRoot.SetActive(false);
            SpatialAudioEngine.Instance?.PlayWindowMinimize();
        }

        public void Toggle()
        {
            if (isOpen) Hide();
            else Show();
        }

        private SpatialButton3D CreateKey(string label, Vector3 localPos, Vector2 size, Transform parent, Action onClick, Color accent)
        {
            GameObject keyObj = new GameObject($"VRKey_{label.Replace(" ", "_").Replace("/", "_").Replace("|", "_")}");
            keyObj.transform.SetParent(parent, false);
            keyObj.transform.localPosition = localPos;

            var btn = keyObj.AddComponent<SpatialButton3D>();
            btn.ButtonText = label;
            btn.SetSize(size.x, size.y);
            btn.SetColors(new Color(0.08f, 0.12f, 0.20f, 0.95f), accent);
            btn.onClick.AddListener(() => onClick?.Invoke());
            return btn;
        }
    }
}
