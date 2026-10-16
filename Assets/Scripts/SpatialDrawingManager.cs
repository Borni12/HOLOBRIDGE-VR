using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Fusion;

public class SpatialDrawingManager : NetworkBehaviour
{
    [Header("Drawing Configuration")]
    [SerializeField] private NetworkPrefabRef strokePrefab;
    [SerializeField] private float minVertexDistance = 0.02f;
    [SerializeField] private float drawDistance = 1.0f;

    [Header("Brush Settings")]
    [SerializeField] private Color currentColor = Color.red;

    private NetworkObject currentNetworkStroke;
    private LineRenderer currentLine;
    private List<Vector3> pointBuffer = new List<Vector3>();
    private bool isDrawing = false;
    private Camera activeCam;
    private List<NetworkObject> spawnedStrokes = new List<NetworkObject>();

    void Start()
    {
        FindCam();
        currentColor = Color.red;
    }

    private void FindCam()
    {
        activeCam = Camera.main;
        if (activeCam == null)
        {
            activeCam = FindFirstObjectByType<Camera>();
        }
    }

    void Update()
    {
        // ১. কিবোর্ডের ১, ২, ৩ চেপে সরাসরি কালার পরিবর্তন
        bool key1 = false;
        bool key2 = false;
        bool key3 = false;

        if (Keyboard.current != null)
        {
            key1 = Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame;
            key2 = Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame;
            key3 = Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame;
        }
        else
        {
            key1 = Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1);
            key2 = Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2);
            key3 = Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3);
        }

        if (key1) SetColorRed();
        if (key2) SetColorYellow();
        if (key3) SetColorBlue();

        // ২. F কী চেপে অথবা মাউস দিয়ে ড্রয়িং
        bool drawPressed = false;
        bool drawHeld = false;
        bool drawReleased = false;

        if (Keyboard.current != null)
        {
            drawPressed = Keyboard.current.fKey.wasPressedThisFrame;
            drawHeld = Keyboard.current.fKey.isPressed;
            drawReleased = Keyboard.current.fKey.wasReleasedThisFrame;
        }
        else
        {
            drawPressed = Input.GetKeyDown(KeyCode.F);
            drawHeld = Input.GetKey(KeyCode.F);
            drawReleased = Input.GetKeyUp(KeyCode.F);
        }

        if (drawPressed) StartDrawing();
        else if (drawHeld && isDrawing) UpdateDrawing();
        else if (drawReleased && isDrawing) StopDrawing();

        // ৩. কিবোর্ডের C চেপে Undo এবং X চেপে Clear All
        bool cPressed = (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame) || Input.GetKeyDown(KeyCode.C);
        if (cPressed)
        {
            RemoveLastStroke();
        }

        bool xPressed = (Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame) || Input.GetKeyDown(KeyCode.X);
        if (xPressed)
        {
            ClearAllStrokes();
        }
    }

    private Vector3 GetDrawPoint()
    {
        if (activeCam == null) FindCam();
        if (activeCam == null) return new Vector3(0, 1.2f, 1f);

        Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;
        Vector3 screenPos = new Vector3(mousePos.x, mousePos.y, drawDistance);
        return activeCam.ScreenToWorldPoint(screenPos);
    }

    public void StartDrawing()
    {
        FindCam();
        Vector3 spawnPos = GetDrawPoint();

        // লোকাল Instantiate এর বদলে ফিউশন স্পন কল
        if (Runner != null)
        {
            currentNetworkStroke = Runner.Spawn(strokePrefab, spawnPos, Quaternion.identity, Runner.LocalPlayer);
            if (currentNetworkStroke != null)
            {
                spawnedStrokes.Add(currentNetworkStroke);
                currentLine = currentNetworkStroke.GetComponent<LineRenderer>();

                if (currentLine != null)
                {
                    currentLine.useWorldSpace = true;
                    pointBuffer.Clear();
                    pointBuffer.Add(spawnPos);
                    pointBuffer.Add(spawnPos + Vector3.up * 0.005f);

                    currentLine.positionCount = 2;
                    currentLine.SetPositions(pointBuffer.ToArray());

                    // সবার স্ক্রিনে লাইনের কালার সেট করার আরপিসি
                    Rpc_SetupStrokeColor(currentNetworkStroke, currentColor);
                }
            }
        }

        isDrawing = true;
    }

    private void UpdateDrawing()
    {
        if (currentLine == null || currentNetworkStroke == null) return;

        Vector3 currentPos = GetDrawPoint();
        if (pointBuffer.Count > 0 && Vector3.Distance(pointBuffer[pointBuffer.Count - 1], currentPos) >= minVertexDistance)
        {
            pointBuffer.Add(currentPos);
            currentLine.positionCount = pointBuffer.Count;
            currentLine.SetPosition(pointBuffer.Count - 1, currentPos);

            // সব প্লেয়ারের কাছে পয়েন্ট পাঠানো
            Rpc_AddPointToLine(currentNetworkStroke, currentPos);
        }
    }

    public void StopDrawing()
    {
        isDrawing = false;
        currentLine = null;
        currentNetworkStroke = null;
        pointBuffer.Clear();
    }

    // --- নেটওয়ার্ক আরপিসি (RPC) মেথডসমূহ ---

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void Rpc_SetupStrokeColor(NetworkObject strokeNetObj, Color color)
    {
        if (strokeNetObj != null)
        {
            LineRenderer lr = strokeNetObj.GetComponent<LineRenderer>();
            if (lr != null)
            {
                lr.startColor = color;
                lr.endColor = color;
                if (lr.material != null)
                {
                    lr.material.color = color;
                    lr.material.SetColor("_BaseColor", color);
                }
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void Rpc_AddPointToLine(NetworkObject strokeNetObj, Vector3 newPoint)
    {
        if (strokeNetObj != null)
        {
            LineRenderer lr = strokeNetObj.GetComponent<LineRenderer>();
            if (lr != null)
            {
                int nextIndex = lr.positionCount;
                lr.positionCount++;
                lr.SetPosition(nextIndex, newPoint);
            }
        }
    }

    // --- কালার ও ক্লিনিং মেথডসমূহ ---

    public void SetColorRed()
    {
        currentColor = Color.red;
        Debug.Log("Spatial UI: Selected Color Red");
    }

    public void SetColorYellow()
    {
        currentColor = Color.yellow;
        Debug.Log("Spatial UI: Selected Color Yellow");
    }

    public void SetColorBlue()
    {
        currentColor = new Color(0f, 0.7f, 1f);
        Debug.Log("Spatial UI: Selected Color Blue");
    }

    public void RemoveLastStroke()
    {
        if (spawnedStrokes.Count > 0)
        {
            int lastIndex = spawnedStrokes.Count - 1;
            NetworkObject lastStroke = spawnedStrokes[lastIndex];
            spawnedStrokes.RemoveAt(lastIndex);

            if (lastStroke != null && Runner != null)
            {
                // নেটওয়ার্ক থেকে অবজেক্ট সরানো
                Runner.Despawn(lastStroke);
                Debug.Log("Spatial UI: Despawned Last Stroke");
            }
        }
    }

    public void ClearAllStrokes()
    {
        if (Runner != null)
        {
            foreach (var stroke in spawnedStrokes)
            {
                if (stroke != null)
                {
                    Runner.Despawn(stroke);
                }
            }
        }
        spawnedStrokes.Clear();
        Debug.Log("Spatial UI: Cleared All Network Strokes");
    }
}