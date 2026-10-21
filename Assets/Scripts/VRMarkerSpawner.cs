
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Fusion;

public class VRMarkerSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private NetworkPrefabRef markerPrefab;
    [SerializeField] private NetworkPrefabRef strokePrefab;

    [Header("Drawing Setup")]
    [SerializeField] private float minVertexDistance = 0.01f;
    [SerializeField] private float spawnForwardOffset = 0.6f;

    [Header("Colors")]
    [SerializeField] private Color activeColor = Color.red;

    private NetworkRunner runner;
    private NetworkObject currentStrokeObj;
    private NetworkStroke currentStrokeScript;
    private Vector3 lastDrawPoint;
    private bool isDrawing = false;
    private List<NetworkObject> strokeHistory = new List<NetworkObject>();
    private bool showPalette = true;

    void Start()
    {
        activeColor = new Color(1f, 0.05f, 0.05f);
    }

    private NetworkRunner GetRunner()
    {
        if (runner == null || !runner.IsRunning)
        {
            runner = FindFirstObjectByType<NetworkRunner>();
        }
        return runner;
    }

    private Vector3 GetTipPosition()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();

        if (cam != null)
        {
            return cam.transform.position + (cam.transform.forward * spawnForwardOffset) - (cam.transform.up * 0.08f);
        }

        return new Vector3(0, 1.2f, 1f);
    }

    void Update()
    {
        var netRunner = GetRunner();
        if (netRunner == null || !netRunner.IsRunning) return;

        // ১. প্যালেট টগল (P কী)
        if (Input.GetKeyDown(KeyCode.P) || (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame))
        {
            showPalette = !showPalette;
        }

        // ২. কালার নির্বাচন (১ = লাল, ২ = নীল, ৩ = হলুদ)
        if (Input.GetKeyDown(KeyCode.Alpha1) || (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame))
            activeColor = new Color(1f, 0.05f, 0.05f);
        if (Input.GetKeyDown(KeyCode.Alpha2) || (Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame))
            activeColor = new Color(0f, 0.55f, 1f);
        if (Input.GetKeyDown(KeyCode.Alpha3) || (Keyboard.current != null && Keyboard.current.digit3Key.wasPressedThisFrame))
            activeColor = new Color(1f, 0.92f, 0.016f);

        // ৩. হলুদ মার্কার বল (Space অথবা মাউস Left Click)
        bool spaceKey = Input.GetKeyDown(KeyCode.Space) || (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame);
        bool leftMouse = Input.GetMouseButtonDown(0);
        if (Mouse.current != null) leftMouse |= Mouse.current.leftButton.wasPressedThisFrame;

        // প্যালেটের ওপরে ক্লিক পড়লে বল তৈরি বন্ধ রাখা
        if (showPalette && Input.mousePosition.x < 240 && Input.mousePosition.y > Screen.height - 240)
        {
            leftMouse = false;
        }

        if (spaceKey || leftMouse)
        {
            SpawnMarker(netRunner);
        }

        // ৪. ৩D ড্রয়িং (F কী চেপে ধরে টানা)
        bool fDown = Input.GetKeyDown(KeyCode.F);
        bool fHold = Input.GetKey(KeyCode.F);
        bool fUp = Input.GetKeyUp(KeyCode.F);

        if (Keyboard.current != null)
        {
            fDown |= Keyboard.current.fKey.wasPressedThisFrame;
            fHold |= Keyboard.current.fKey.isPressed;
            fUp |= Keyboard.current.fKey.wasReleasedThisFrame;
        }

        if (fDown)
        {
            StartDrawing(netRunner);
        }
        else if (fHold && isDrawing)
        {
            ContinueDrawing();
        }
        else if (fUp && isDrawing)
        {
            StopDrawing();
        }

        // ৫. আনডু (C কী)
        if (Input.GetKeyDown(KeyCode.C) || (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame))
        {
            RemoveLastStroke(netRunner);
        }
    }

    private void SpawnMarker(NetworkRunner netRunner)
    {
        if (!markerPrefab.IsValid) return;
        Vector3 pos = GetTipPosition();
        netRunner.Spawn(markerPrefab, pos, Quaternion.identity);
        Debug.Log($"[Marker] হলুদ বল তৈরি হয়েছে: {pos}");
    }

    private void StartDrawing(NetworkRunner netRunner)
    {
        if (!strokePrefab.IsValid) return;

        Vector3 pos = GetTipPosition();
        currentStrokeObj = netRunner.Spawn(strokePrefab, pos, Quaternion.identity);

        if (currentStrokeObj != null)
        {
            strokeHistory.Add(currentStrokeObj);
            currentStrokeScript = currentStrokeObj.GetComponent<NetworkStroke>();

            if (currentStrokeScript != null)
            {
                currentStrokeScript.InitColor(activeColor);
                currentStrokeScript.AddPoint(pos);
                currentStrokeScript.AddPoint(pos + new Vector3(0.001f, 0, 0));
                lastDrawPoint = pos;
                isDrawing = true;
                Debug.Log($"[Drawing] দাগ আঁকা শুরু: {pos}");
            }
        }
    }

    private void ContinueDrawing()
    {
        if (currentStrokeScript == null) return;

        Vector3 currentPos = GetTipPosition();
        if (Vector3.Distance(currentPos, lastDrawPoint) >= minVertexDistance)
        {
            lastDrawPoint = currentPos;
            currentStrokeScript.AddPoint(currentPos);
        }
    }

    private void StopDrawing()
    {
        isDrawing = false;
        currentStrokeScript = null;
        currentStrokeObj = null;
    }

    public void RemoveLastStroke(NetworkRunner netRunner)
    {
        if (strokeHistory.Count > 0)
        {
            int lastIndex = strokeHistory.Count - 1;
            NetworkObject stroke = strokeHistory[lastIndex];
            strokeHistory.RemoveAt(lastIndex);

            if (stroke != null && netRunner.IsRunning)
            {
                netRunner.Despawn(stroke);
            }
        }
    }

    private void OnGUI()
    {
        if (!showPalette) return;

        GUI.Box(new Rect(20, 20, 200, 190), "🎨 3D Color Palette (P: Toggle)");

        GUI.backgroundColor = Color.red;
        if (GUI.Button(new Rect(35, 55, 170, 35), "RED (Press 1)"))
            activeColor = new Color(1f, 0.05f, 0.05f);

        GUI.backgroundColor = new Color(0f, 0.55f, 1f);
        if (GUI.Button(new Rect(35, 95, 170, 35), "BLUE (Press 2)"))
            activeColor = new Color(0f, 0.55f, 1f);

        GUI.backgroundColor = Color.yellow;
        if (GUI.Button(new Rect(35, 135, 170, 35), "YELLOW (Press 3)"))
            activeColor = new Color(1f, 0.92f, 0.016f);

        GUI.backgroundColor = activeColor;
        GUI.Button(new Rect(35, 175, 170, 20), "Current Brush");
    }
}