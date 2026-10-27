
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class VRMarkerSpawner : NetworkBehaviour
{
    [Header("Prefabs to Spawn")]
    [SerializeField] private NetworkPrefabRef markerPrefab;
    [SerializeField] private NetworkPrefabRef networkStrokePrefab;

    [Header("Spawn Settings")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float minVertexDistance = 0.02f;
    [SerializeField] private float spawnForwardOffset = 0.05f;

    [Header("Color Settings")]
    [SerializeField] private Color activeColor = Color.yellow; // ডিফল্ট হলুদ মার্কার

    // স্পন করা মার্কার বলগুলো ট্র্যাক করার স্ট্যাক (C চেপে একটি একটি করে মুছার জন্য)
    private readonly Stack<NetworkObject> spawnedMarkersStack = new Stack<NetworkObject>();

    private NetworkObject currentActiveStroke;
    private LineRenderer currentLineRenderer;
    private readonly List<Vector3> strokePoints = new List<Vector3>();
    private Vector3 lastPointPos;
    private bool isDrawing = false;

    private void Awake()
    {
        if (spawnPoint == null)
        {
            var foundHand = GameObject.FindWithTag("HandPoint");
            if (foundHand != null)
            {
                spawnPoint = foundHand.transform;
            }
            else
            {
                spawnPoint = transform;
            }
        }
    }

    private void Update()
    {
        // ১. কিবোর্ড শর্টকাট দিয়ে কালার সুইচ ও মার্কার রিমুভ হ্যান্ডলিং
        HandleKeyboardInput();

        // ২. মাউসের Left Click করলে মার্কার বল তৈরি
        if (Input.GetMouseButtonDown(0))
        {
            SpawnMarkerPin();
        }

        // ৩. মাউসের Right Click ধরে রাখলে বাতাসে ড্রয়িং স্ট্রোক আঁকা
        HandleDrawingStrokeInput();
    }

    private void HandleKeyboardInput()
    {
        bool isFDown = Input.GetKey(KeyCode.F);

        // '1 + F' চাপলে লাল (Red)
        if (isFDown && (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) ||
      (Input.GetKey(KeyCode.Alpha1) || Input.GetKey(KeyCode.Keypad1)) && Input.GetKeyDown(KeyCode.F))
        {
            SetRedColor();
        }
        // '2 + F' চাপলে নীল (Blue)
        else if (isFDown && (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) ||
        (Input.GetKey(KeyCode.Alpha2) || Input.GetKey(KeyCode.Keypad2)) && Input.GetKeyDown(KeyCode.F))
        {
            SetBlueColor();
        }
        // '3 + F' চাপলে হলুদ (Yellow)
        else if (isFDown && (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) ||
        (Input.GetKey(KeyCode.Alpha3) || Input.GetKey(KeyCode.Keypad3)) && Input.GetKeyDown(KeyCode.F))
        {
            SetYellowColor();
        }

        // 'C' চাপলে শেষ তৈরি হওয়া মার্কারটি একটি একটি করে রিমুভ হবে (Undo)
        if (Input.GetKeyDown(KeyCode.C))
        {
            RemoveLastMarker();
        }
    }

    private void HandleDrawingStrokeInput()
    {
        if (Input.GetMouseButtonDown(1))
        {
            StartDrawingStroke();
        }

        if (Input.GetMouseButton(1) && isDrawing)
        {
            UpdateDrawingStroke();
        }

        if (Input.GetMouseButtonUp(1))
        {
            StopDrawingStroke();
        }
    }

    /// <summary>
    /// স্ক্রিনের ওপর কালার প্যালেট UI বাটন
    /// </summary>
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(20, 20, 190, 250));
        GUILayout.BeginVertical("box");

        GUILayout.Label("<b>Color & Marker Controls</b>");

        // লাল বাটন (1 + F)
        GUI.color = new Color(1f, 0.25f, 0.25f);
        if (GUILayout.Button("RED (1 + F)", GUILayout.Height(30)))
        {
            SetRedColor();
        }

        // নীল বাটন (2 + F)
        GUI.color = new Color(0.25f, 0.65f, 1f);
        if (GUILayout.Button("BLUE (2 + F)", GUILayout.Height(30)))
        {
            SetBlueColor();
        }

        // হলুদ বাটন (3 + F)
        GUI.color = new Color(1f, 0.92f, 0.016f);
        if (GUILayout.Button("YELLOW (3 + F)", GUILayout.Height(30)))
        {
            SetYellowColor();
        }

        // মার্কার ডিলিট বাটন (C)
        GUI.color = new Color(0.85f, 0.85f, 0.85f);
        if (GUILayout.Button("Undo Marker (C)", GUILayout.Height(30)))
        {
            RemoveLastMarker();
        }

        GUI.color = Color.white;
        GUILayout.Space(5);
        GUILayout.Label($"Active: {GetColorName()}");
        GUILayout.Label($"Spawned: {spawnedMarkersStack.Count}");

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private string GetColorName()
    {
        if (activeColor == Color.red || (activeColor.r > 0.8f && activeColor.g < 0.3f)) return "Red (লাল)";
        if (activeColor == Color.cyan || activeColor.b > 0.8f) return "Blue (নীল)";
        return "Yellow (হলুদ)";
    }

    /// <summary>
    /// মাউসের Left Click দিয়ে মার্কার বল স্পন করা
    /// </summary>
    public void SpawnMarkerPin()
    {
        NetworkRunner runner = Runner ?? FindFirstObjectByType<NetworkRunner>();
        if (runner == null || !runner.IsRunning || !markerPrefab.IsValid) return;

        Vector3 spawnPos = GetTipPosition();
        Quaternion spawnRot = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

        runner.Spawn(markerPrefab, spawnPos, spawnRot, runner.LocalPlayer, (r, spawnedObj) =>
        {
            var renderer = spawnedObj.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = activeColor;
            }

            // ট্র্যাকিং স্ট্যাকে অবজেক্টটি জমা রাখা
            spawnedMarkersStack.Push(spawnedObj);
            Debug.Log($"[Marker] নতুন {GetColorName()} বল তৈরি হয়েছে! মোট: {spawnedMarkersStack.Count}");
        });
    }

    /// <summary>
    /// 'C' চাপলে সর্বশেষ তৈরি করা মার্কারটি নেটওয়ার্ক থেকে মুছে ফেলা (Undo)
    /// </summary>
    public void RemoveLastMarker()
    {
        NetworkRunner runner = Runner ?? FindFirstObjectByType<NetworkRunner>();
        if (runner == null || !runner.IsRunning) return;

        while (spawnedMarkersStack.Count > 0)
        {
            NetworkObject lastMarker = spawnedMarkersStack.Pop();
            if (lastMarker != null)
            {
                runner.Despawn(lastMarker);
                Debug.Log($"[Marker] একটি মার্কার মুছে ফেলা হয়েছে! অবশিষ্ট: {spawnedMarkersStack.Count}");
                return;
            }
        }

        Debug.Log("[Marker] মুছে ফেলার মতো কোনো মার্কার অবশিষ্ট নেই!");
    }

    /// <summary>
    /// বাতাসে ড্রয়িং স্ট্রোক শুরু
    /// </summary>
    public void StartDrawingStroke()
    {
        NetworkRunner runner = Runner ?? FindFirstObjectByType<NetworkRunner>();
        if (runner == null || !runner.IsRunning || !networkStrokePrefab.IsValid) return;

        Vector3 tipPos = GetTipPosition();

        runner.Spawn(networkStrokePrefab, tipPos, Quaternion.identity, runner.LocalPlayer, (r, strokeObj) =>
        {
            currentActiveStroke = strokeObj;
            currentLineRenderer = strokeObj.GetComponent<LineRenderer>();
            strokePoints.Clear();

            if (currentLineRenderer != null)
            {
                currentLineRenderer.startColor = activeColor;
                currentLineRenderer.endColor = activeColor;
                currentLineRenderer.positionCount = 0;

                if (currentLineRenderer.material != null)
                {
                    currentLineRenderer.material.color = activeColor;
                }
            }

            AddPointToStroke(tipPos);
            isDrawing = true;
        });
    }

    private void UpdateDrawingStroke()
    {
        if (currentLineRenderer == null) return;

        Vector3 tipPos = GetTipPosition();
        if (Vector3.Distance(tipPos, lastPointPos) >= minVertexDistance)
        {
            AddPointToStroke(tipPos);
        }
    }

    private void AddPointToStroke(Vector3 newPoint)
    {
        strokePoints.Add(newPoint);
        lastPointPos = newPoint;

        if (currentLineRenderer != null)
        {
            currentLineRenderer.positionCount = strokePoints.Count;
            currentLineRenderer.SetPosition(strokePoints.Count - 1, newPoint);
        }
    }

    public void StopDrawingStroke()
    {
        isDrawing = false;
        currentActiveStroke = null;
        currentLineRenderer = null;
    }

    private Vector3 GetTipPosition()
    {
        return spawnPoint != null ? spawnPoint.position + (spawnPoint.forward * spawnForwardOffset) : transform.position;
    }

    public void SetRedColor()
    {
        activeColor = new Color(1f, 0.15f, 0.15f, 1f);
        Debug.Log("[VR Color] Selected: RED (লাল)");
    }

    public void SetBlueColor()
    {
        activeColor = new Color(0.1f, 0.55f, 1f, 1f);
        Debug.Log("[VR Color] Selected: BLUE (নীল)");
    }

    public void SetYellowColor()
    {
        activeColor = new Color(1f, 0.92f, 0.016f, 1f);
        Debug.Log("[VR Color] Selected: YELLOW (হলুদ)");
    }
}