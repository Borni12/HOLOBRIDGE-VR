using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;

/// <summary>
/// Holographic Spatial Interaction & Multiplayer Controller.
/// 1. Dynamic Color Palette with custom RGB sliders.
/// 2. 3D Spatial Stroke Drawing with line width control.
/// 3. Dynamic 3D Ball Spawning with diameter/radius control.
/// 4. Step-by-Step Undo ('C' key) to erase strokes and balls one-by-one.
/// 5. Seamless Multi-Device / Cross-Platform Replication via Photon Fusion 2 (Quest & PC).
/// 6. Dynamic Cybersickness Reduction: Smooth peripheral FOV tunneling vignette during head/camera motion.
/// 7. Auto-cleanup of stray test objects.
/// All user-facing text, comments, and logs are in English.
/// </summary>
public class VRMarkerSpawner : NetworkBehaviour
{
    public enum InteractionMode
    {
        DrawStroke,
        SpawnBall
    }

    [Header("Network Prefabs")]
    [SerializeField] private NetworkPrefabRef markerPrefab;
    [SerializeField] private NetworkPrefabRef networkStrokePrefab;

    [Header("Interaction Mode & Tools")]
    [SerializeField] private InteractionMode currentMode = InteractionMode.DrawStroke;
    [Range(0.02f, 0.40f)]
    [SerializeField] private float ballRadius = 0.08f; // Ball diameter / size (default: 8cm)
    [Range(0.005f, 0.06f)]
    [SerializeField] private float strokeWidth = 0.02f; // Stroke thickness (default: 2cm)
    [SerializeField] private float defaultDistance = 1.2f;
    [SerializeField] private float minVertexDistance = 0.015f;

    public InteractionMode CurrentInteractionMode
    {
        get => currentMode;
        set => currentMode = value;
    }
    public float BallRadius
    {
        get => ballRadius;
        set => ballRadius = Mathf.Clamp(value, 0.02f, 0.40f);
    }
    public float StrokeWidth
    {
        get => strokeWidth;
        set => strokeWidth = Mathf.Clamp(value, 0.005f, 0.06f);
    }
    public void SetInteractionMode(InteractionMode mode) => currentMode = mode;
    public void SetBallRadius(float radius) => ballRadius = Mathf.Clamp(radius, 0.02f, 0.40f);
    public void SetStrokeWidth(float width) => strokeWidth = Mathf.Clamp(width, 0.005f, 0.06f);

    [Header("Active Dynamic Color")]
    [SerializeField] private Color activeColor = new Color(0f, 0.85f, 1f, 1f); // Default: Vibrant Cyan

    [Header("Cybersickness Comfort Settings")]
    [SerializeField] private bool enableCybersicknessComfort = true;
    [Range(0.2f, 0.9f)]
    [SerializeField] private float maxComfortVignetteAlpha = 0.65f;
    [SerializeField] private float comfortFadeSpeed = 5.0f;
    [SerializeField] private float angularMotionThreshold = 35.0f; // deg/sec
    [SerializeField] private float linearMotionThreshold = 1.2f;   // m/sec

    // Preset Swatches
    private static readonly Color[] PresetColors = new Color[]
    {
        new Color(1f, 0.22f, 0.22f),    // Red
        new Color(1f, 0.58f, 0f),       // Orange
        new Color(1f, 0.88f, 0.05f),    // Yellow
        new Color(0.2f, 0.85f, 0.35f),  // Green
        new Color(0f, 0.85f, 1f),       // Cyan
        new Color(0.15f, 0.45f, 1f),    // Blue
        new Color(0.68f, 0.28f, 1f),    // Purple
        new Color(1f, 0.25f, 0.65f),    // Pink
        new Color(1f, 1f, 1f),          // White
        new Color(0.15f, 0.15f, 0.18f)  // Dark/Slate
    };

    // Unified History Stack for Step-by-Step Undo
    private enum HistoryType { Stroke, Ball }
    private struct HistoryRecord
    {
        public HistoryType type;
        public GameObject offlineObj;
        public NetworkObject networkObj;
    }
    private readonly Stack<HistoryRecord> historyStack = new Stack<HistoryRecord>();

    // Stroke Drawing State
    private bool isDrawing = false;
    private NetworkObject currentNetworkStroke;
    private GameObject currentOfflineStrokeObj;
    private LineRenderer currentLineRenderer;
    private readonly List<Vector3> activeStrokePoints = new List<Vector3>();
    private Vector3 lastRecordedPoint;

    // Cybersickness Motion Tracker State
    private Texture2D comfortVignetteTexture;
    private float currentVignetteAlpha = 0f;
    private Vector3 prevCamPosition;
    private Quaternion prevCamRotation;
    private bool isKinematicsInitialized = false;

    // Cached Resources & Camera
    private GameObject cachedMarkerResource;
    private GameObject cachedStrokeResource;
    private Camera activeCamera;

    // UI Window
    [SerializeField] private bool showOnScreenGui = false; // Default: clean view without screen-blocking overlays
    private Rect paletteWindowRect = new Rect(20, 20, 270, 460);
    private bool showRgbSliders = false;

    private void Awake()
    {
        // 1. Remove all old dummy test objects and static blue quads
        CleanupUnwantedTestObjects();

        cachedMarkerResource = Resources.Load<GameObject>("MarkerPin");
        cachedStrokeResource = Resources.Load<GameObject>("NetworkStroke");
        activeCamera = Camera.main ?? UnityEngine.Object.FindAnyObjectByType<Camera>();

        // 2. Generate smooth circular vignette mask for Cybersickness Comfort
        comfortVignetteTexture = GenerateComfortVignetteTexture();
    }

    private void OnDestroy()
    {
        if (comfortVignetteTexture != null)
        {
            Destroy(comfortVignetteTexture);
        }
    }

    private void CleanupUnwantedTestObjects()
    {
        GameObject testRoot = GameObject.Find("--- AEGIS_SPATIAL_SYSTEM ---");
        if (testRoot != null)
        {
            Destroy(testRoot);
        }

        var blueGlows = UnityEngine.Object.FindObjectsByType<AegisSpatial.Comfort.AdaptiveRestFrame>(FindObjectsInactive.Include);
        foreach (var bg in blueGlows)
        {
            Destroy(bg.gameObject);
        }
    }

    private void Update()
    {
        if (activeCamera == null)
        {
            activeCamera = Camera.main ?? UnityEngine.Object.FindAnyObjectByType<Camera>();
        }

        // 1. Update Cybersickness Comfort Dynamics
        UpdateCybersicknessComfort();

        // If OmniOS Spatial Operating System is active, let OmniOS manage spatial pointer and app interactions
        if (OmniOS.Core.OmniOSManager.Instance != null)
        {
            return;
        }

        // 2. Check Undo Input ('C' Key)
        HandleUndoInput();

        // 3. Check Ball Spawn Shortcut ('B' Key or Mouse Right Click)
        HandleBallShortcutInput();

        // 4. Check Main Drawing / Interaction Input (Left Mouse Click & Drag)
        HandleMainInteractionInput();
    }

    #region Cybersickness Comfort Reducer
    private void UpdateCybersicknessComfort()
    {
        if (!enableCybersicknessComfort || activeCamera == null)
        {
            currentVignetteAlpha = Mathf.MoveTowards(currentVignetteAlpha, 0f, comfortFadeSpeed * Time.deltaTime);
            return;
        }

        Transform camTransform = activeCamera.transform;
        Vector3 camPos = camTransform.position;
        Quaternion camRot = camTransform.rotation;
        float dt = Time.deltaTime;

        if (dt <= 0.0001f) return;

        if (!isKinematicsInitialized)
        {
            prevCamPosition = camPos;
            prevCamRotation = camRot;
            isKinematicsInitialized = true;
            return;
        }

        // Calculate instantaneous motion rates
        float linearSpeed = Vector3.Distance(camPos, prevCamPosition) / dt;
        float angularSpeed = Quaternion.Angle(camRot, prevCamRotation) / dt;

        prevCamPosition = camPos;
        prevCamRotation = camRot;

        // Evaluate motion excess beyond comfort threshold
        float targetAlpha = 0f;
        if (angularSpeed > angularMotionThreshold || linearSpeed > linearMotionThreshold)
        {
            float angularExcess = Mathf.Clamp01((angularSpeed - angularMotionThreshold) / 50f);
            float linearExcess = Mathf.Clamp01((linearSpeed - linearMotionThreshold) / 2.5f);
            float intensity = Mathf.Max(angularExcess, linearExcess);
            targetAlpha = intensity * maxComfortVignetteAlpha;
        }

        // Smooth transition (quick fade-in, gentle fade-out)
        float currentSpeed = (targetAlpha > currentVignetteAlpha) ? (comfortFadeSpeed * 1.5f) : comfortFadeSpeed;
        currentVignetteAlpha = Mathf.MoveTowards(currentVignetteAlpha, targetAlpha, currentSpeed * dt);
    }

    private static Texture2D GenerateComfortVignetteTexture()
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float maxDist = size * 0.5f;

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                // Center remains 100% transparent and clear; outer edges smoothly blend to black
                float alpha = Mathf.SmoothStep(0.38f, 0.98f, dist);
                pixels[y * size + x] = new Color(0f, 0f, 0f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
    #endregion

    private void HandleUndoInput()
    {
        bool undoTriggered = Input.GetKeyDown(KeyCode.C) ||
                             (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame);

        if (undoTriggered)
        {
            UndoLastAction();
        }
    }

    private void HandleBallShortcutInput()
    {
        bool bPressed = Input.GetKeyDown(KeyCode.B) ||
                        (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame);

        bool rightClick = Input.GetMouseButtonDown(1) ||
                          (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame);

        if (bPressed || (rightClick && !IsMouseOverUI()))
        {
            SpawnBallAtPointer();
        }
    }

    private void HandleMainInteractionInput()
    {
        if (IsMouseOverUI()) return;

        bool leftDown = Input.GetMouseButtonDown(0) || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
        bool leftHold = Input.GetMouseButton(0) || (Mouse.current != null && Mouse.current.leftButton.isPressed);
        bool leftUp = Input.GetMouseButtonUp(0) || (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame);

        if (currentMode == InteractionMode.DrawStroke)
        {
            if (leftDown) StartStroke();
            if (leftHold && isDrawing) UpdateStroke();
            if (leftUp && isDrawing) EndStroke();
        }
        else if (currentMode == InteractionMode.SpawnBall)
        {
            if (leftDown) SpawnBallAtPointer();
        }
    }

    private bool IsMouseOverUI()
    {
        Vector2 mousePos = Input.mousePosition;
        if (Mouse.current != null)
        {
            mousePos = Mouse.current.position.ReadValue();
        }
        float flippedY = Screen.height - mousePos.y;
        return paletteWindowRect.Contains(new Vector2(mousePos.x, flippedY));
    }

    #region 3D Ball Spawning & Dynamic Size
    public void SpawnBallAtPointer()
    {
        Vector3 spawnPos = CalculatePointerPosition(out Quaternion spawnRot);
        NetworkRunner runner = Runner ?? UnityEngine.Object.FindAnyObjectByType<NetworkRunner>();

        // 1. Photon Fusion Multi-User Mode
        if (runner != null && runner.IsRunning && markerPrefab.IsValid)
        {
            runner.Spawn(markerPrefab, spawnPos, spawnRot, runner.LocalPlayer, (r, netObj) =>
            {
                var ballSync = netObj.GetComponent<NetworkMarkerBall>();
                if (ballSync == null) ballSync = netObj.gameObject.AddComponent<NetworkMarkerBall>();
                ballSync.Init(activeColor, ballRadius);

                historyStack.Push(new HistoryRecord
                {
                    type = HistoryType.Ball,
                    networkObj = netObj
                });
                Debug.Log($"[Multiplayer Ball] Spawned on network (Size: {ballRadius * 100f:F1}cm)");
            });
            return;
        }

        // 2. Offline / Standalone Fallback
        GameObject instance = null;
        if (cachedMarkerResource != null)
        {
            instance = Instantiate(cachedMarkerResource, spawnPos, spawnRot);
        }
        else
        {
            instance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            instance.name = "SpatialBall";
        }

        instance.transform.position = spawnPos;
        instance.transform.rotation = spawnRot;
        instance.transform.localScale = Vector3.one * ballRadius;

        var localSync = instance.GetComponent<NetworkMarkerBall>();
        if (localSync == null) localSync = instance.AddComponent<NetworkMarkerBall>();
        localSync.Init(activeColor, ballRadius);

        historyStack.Push(new HistoryRecord
        {
            type = HistoryType.Ball,
            offlineObj = instance
        });
        Debug.Log($"[Local Ball] Spawned sphere (Size: {ballRadius * 100f:F1}cm)");
    }
    #endregion

    #region 3D Stroke Drawing
    public void StartStroke()
    {
        Vector3 initialPoint = CalculatePointerPosition(out _);
        NetworkRunner runner = Runner ?? UnityEngine.Object.FindAnyObjectByType<NetworkRunner>();

        // 1. Photon Fusion Multi-User Mode
        if (runner != null && runner.IsRunning && networkStrokePrefab.IsValid)
        {
            runner.Spawn(networkStrokePrefab, initialPoint, Quaternion.identity, runner.LocalPlayer, (r, netObj) =>
            {
                currentNetworkStroke = netObj;
                currentLineRenderer = netObj.GetComponent<LineRenderer>();
                SetupLineRenderer(currentLineRenderer, initialPoint);
                isDrawing = true;
            });
            return;
        }

        // 2. Offline Fallback
        if (cachedStrokeResource != null)
        {
            currentOfflineStrokeObj = Instantiate(cachedStrokeResource, initialPoint, Quaternion.identity);
            currentLineRenderer = currentOfflineStrokeObj.GetComponent<LineRenderer>();
        }
        else
        {
            currentOfflineStrokeObj = new GameObject("SpatialStroke");
            currentLineRenderer = currentOfflineStrokeObj.AddComponent<LineRenderer>();
            currentLineRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        }

        SetupLineRenderer(currentLineRenderer, initialPoint);
        isDrawing = true;
    }

    private void SetupLineRenderer(LineRenderer lr, Vector3 startPoint)
    {
        if (lr == null) return;

        lr.useWorldSpace = true;
        lr.alignment = LineAlignment.View;
        lr.numCornerVertices = 5;
        lr.numCapVertices = 5;
        lr.widthMultiplier = strokeWidth;
        lr.startColor = activeColor;
        lr.endColor = activeColor;

        if (lr.material != null)
        {
            lr.material.color = activeColor;
            if (lr.material.HasProperty("_BaseColor")) lr.material.SetColor("_BaseColor", activeColor);
            if (lr.material.HasProperty("_EmissionColor")) lr.material.SetColor("_EmissionColor", activeColor * 1.3f);
        }

        activeStrokePoints.Clear();
        activeStrokePoints.Add(startPoint);
        lastRecordedPoint = startPoint;

        lr.positionCount = 1;
        lr.SetPosition(0, startPoint);
    }

    private void UpdateStroke()
    {
        if (currentLineRenderer == null) return;

        Vector3 newPoint = CalculatePointerPosition(out _);
        if (Vector3.Distance(newPoint, lastRecordedPoint) >= minVertexDistance)
        {
            activeStrokePoints.Add(newPoint);
            lastRecordedPoint = newPoint;

            currentLineRenderer.positionCount = activeStrokePoints.Count;
            currentLineRenderer.SetPosition(activeStrokePoints.Count - 1, newPoint);

            if (currentNetworkStroke != null)
            {
                var strokeComp = currentNetworkStroke.GetComponent<NetworkStroke>();
                if (strokeComp != null)
                {
                    strokeComp.Rpc_SyncPoint(newPoint);
                }
            }
        }
    }

    public void EndStroke()
    {
        if (!isDrawing) return;

        historyStack.Push(new HistoryRecord
        {
            type = HistoryType.Stroke,
            offlineObj = currentOfflineStrokeObj,
            networkObj = currentNetworkStroke
        });

        isDrawing = false;
        currentOfflineStrokeObj = null;
        currentNetworkStroke = null;
        currentLineRenderer = null;
        activeStrokePoints.Clear();
    }
    #endregion

    #region Step-by-Step Undo ('C' Key)
    public void UndoLastAction()
    {
        if (historyStack.Count == 0)
        {
            Debug.Log("[AegisSpatial] No items to undo.");
            return;
        }

        HistoryRecord lastItem = historyStack.Pop();

        // Offline removal
        if (lastItem.offlineObj != null)
        {
            Destroy(lastItem.offlineObj);
            Debug.Log($"[AegisSpatial] Undone 1 {lastItem.type}. Remaining: {historyStack.Count}");
            return;
        }

        // Network removal across all devices
        if (lastItem.networkObj != null)
        {
            NetworkRunner runner = Runner ?? UnityEngine.Object.FindAnyObjectByType<NetworkRunner>();
            if (runner != null && runner.IsRunning)
            {
                runner.Despawn(lastItem.networkObj);
                Debug.Log($"[AegisSpatial] Undone network {lastItem.type}. Remaining: {historyStack.Count}");
            }
        }
    }
    #endregion

    private Vector3 CalculatePointerPosition(out Quaternion orientation)
    {
        if (activeCamera != null)
        {
            Vector3 mousePos = Input.mousePosition;
            if (Mouse.current != null)
            {
                var mPos = Mouse.current.position.ReadValue();
                mousePos = new Vector3(mPos.x, mPos.y, 0f);
            }

            Ray ray = activeCamera.ScreenPointToRay(mousePos);
            if (Physics.Raycast(ray, out RaycastHit hit, 25f))
            {
                orientation = Quaternion.LookRotation(hit.normal);
                return hit.point + (hit.normal * (ballRadius * 0.5f));
            }

            orientation = activeCamera.transform.rotation;
            return activeCamera.transform.position + (ray.direction * defaultDistance);
        }

        orientation = transform.rotation;
        return transform.position + (transform.forward * defaultDistance);
    }

    #region Modern On-Screen UI & Fullscreen Vignette (100% English)
    private void OnGUI()
    {
        // 1. Render Cybersickness Comfort Vignette (Fullscreen Peripheral Mask)
        if (enableCybersicknessComfort && currentVignetteAlpha > 0.01f && comfortVignetteTexture != null)
        {
            Color prevColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, currentVignetteAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), comfortVignetteTexture, ScaleMode.StretchToFill);
            GUI.color = prevColor;
        }

        // 2. Render On-Screen Palette Window only if explicitly enabled and OmniOS is not running
        if (showOnScreenGui && OmniOS.Core.OmniOSManager.Instance == null)
        {
            paletteWindowRect = GUI.Window(999, paletteWindowRect, DrawPaletteWindow, "HOLOBRIDGE - 3D Creator");
        }
    }

    private void DrawPaletteWindow(int windowID)
    {
        Color prevGuiColor = GUI.color;
        GUILayout.Space(2);

        // 1. Multiplayer Status Header (Zero-LINQ enumeration)
        NetworkRunner runner = Runner ?? UnityEngine.Object.FindAnyObjectByType<NetworkRunner>();
        bool isOnline = (runner != null && runner.IsRunning);

        GUILayout.BeginHorizontal();
        if (isOnline)
        {
            int playerCount = 0;
            if (runner.ActivePlayers != null)
            {
                foreach (var _ in runner.ActivePlayers) playerCount++;
            }
            GUI.color = new Color(0.2f, 1f, 0.3f);
            GUILayout.Label($"<b>* Online:</b> {runner.SessionInfo.Name} ({playerCount} Players)");
        }
        else
        {
            GUI.color = new Color(0.85f, 0.85f, 0.85f);
            GUILayout.Label("<b>* Offline:</b> Local Play Mode");
        }
        GUI.color = prevGuiColor;
        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        // 2. Active Mode Switcher (Draw Strokes vs Spawn Balls)
        GUILayout.BeginHorizontal();
        GUI.color = (currentMode == InteractionMode.DrawStroke) ? new Color(0.3f, 1f, 0.6f) : new Color(0.7f, 0.7f, 0.7f);
        if (GUILayout.Button("<b>Draw Mode</b>", GUILayout.Height(30)))
        {
            currentMode = InteractionMode.DrawStroke;
        }

        GUI.color = (currentMode == InteractionMode.SpawnBall) ? new Color(0.3f, 1f, 0.6f) : new Color(0.7f, 0.7f, 0.7f);
        if (GUILayout.Button("<b>Ball Mode</b>", GUILayout.Height(30)))
        {
            currentMode = InteractionMode.SpawnBall;
        }
        GUI.color = prevGuiColor;
        GUILayout.EndHorizontal();

        GUILayout.Space(6);

        // 3. Size Sliders
        if (currentMode == InteractionMode.SpawnBall)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b>Ball Size:</b> {(ballRadius * 100f):F1} cm", GUILayout.Width(110));
            ballRadius = GUILayout.HorizontalSlider(ballRadius, 0.02f, 0.40f);
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b>Line Width:</b> {(strokeWidth * 100f):F1} cm", GUILayout.Width(110));
            strokeWidth = GUILayout.HorizontalSlider(strokeWidth, 0.005f, 0.06f);
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(6);

        // 4. Color Swatches Grid
        GUILayout.Label("<b>Color Palette:</b>");
        int columns = 5;
        for (int i = 0; i < PresetColors.Length; i += columns)
        {
            GUILayout.BeginHorizontal();
            for (int j = 0; j < columns; j++)
            {
                int index = i + j;
                if (index < PresetColors.Length)
                {
                    Color swatchCol = PresetColors[index];
                    GUI.color = swatchCol;
                    bool isSelected = (Vector4.Distance((Vector4)activeColor, (Vector4)swatchCol) < 0.08f);
                    string label = isSelected ? "OK" : "";

                    if (GUILayout.Button(label, GUILayout.Width(46), GUILayout.Height(28)))
                    {
                        activeColor = swatchCol;
                    }
                }
            }
            GUILayout.EndHorizontal();
        }
        GUI.color = prevGuiColor;

        GUILayout.Space(4);

        // 5. Custom RGB Sliders Toggle
        showRgbSliders = GUILayout.Toggle(showRgbSliders, "<b>Custom RGB Sliders</b>");
        if (showRgbSliders)
        {
            GUILayout.BeginVertical("box");
            float r = activeColor.r;
            float g = activeColor.g;
            float b = activeColor.b;

            GUILayout.BeginHorizontal();
            GUILayout.Label("<color=#FF4444><b>R</b></color>", GUILayout.Width(15));
            r = GUILayout.HorizontalSlider(r, 0f, 1f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("<color=#44FF44><b>G</b></color>", GUILayout.Width(15));
            g = GUILayout.HorizontalSlider(g, 0f, 1f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("<color=#4488FF><b>B</b></color>", GUILayout.Width(15));
            b = GUILayout.HorizontalSlider(b, 0f, 1f);
            GUILayout.EndHorizontal();

            activeColor = new Color(r, g, b, 1f);
            GUILayout.EndVertical();
        }

        GUILayout.Space(4);

        // 6. Cybersickness Comfort Toggle
        enableCybersicknessComfort = GUILayout.Toggle(enableCybersicknessComfort, "<b>Anti-Cybersickness Comfort</b>");

        GUILayout.Space(4);

        // 7. Undo Button ('C' Key)
        GUI.color = new Color(1f, 0.4f, 0.35f);
        if (GUILayout.Button($"<b>Undo Last Item (C)</b> [{historyStack.Count}]", GUILayout.Height(30)))
        {
            UndoLastAction();
        }
        GUI.color = prevGuiColor;

        GUILayout.Space(4);
        GUILayout.Label("<size=10>* <b>Left Click:</b> Draw Stroke / Spawn Ball\n* <b>Right Click / 'B':</b> Quick Spawn Ball\n* <b>'C' Key:</b> Undo Items One-by-One</size>");

        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }
    #endregion
}
