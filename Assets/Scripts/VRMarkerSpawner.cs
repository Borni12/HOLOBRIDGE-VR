using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Fusion;

public class VRMarkerSpawner : MonoBehaviour
{
    [Header("Fusion Prefabs")]
    [SerializeField] private NetworkPrefabRef markerPrefab;
    [SerializeField] private NetworkPrefabRef strokePrefab;

    [Header("Drawing Optimization")]
    [SerializeField] private float minVertexDistance = 0.04f; // নেটওয়ার্ক অপ্টিমাইজেশনের জন্য ভার্টেক্স দূরত্ব বাড়ানো হলো
    [SerializeField] private float forwardOffset = 0.5f;

    [Header("Color Settings")]
    [SerializeField] private Color activeColor = Color.red;

    private NetworkRunner runner;
    private NetworkObject currentStrokeObj;
    private NetworkStroke currentStrokeScript;
    private Vector3 lastDrawPoint;
    private bool isDrawing = false;
    private Transform tipSource;

    private List<NetworkObject> strokeHistory = new List<NetworkObject>();
    private bool showPalette = true;

    void Start()
    {
        FindTipSource();
        activeColor = new Color(1f, 0.05f, 0.05f); // ডিফল্ট লাল
    }

    private void FindTipSource()
    {
        var rightCtrl = GameObject.Find("Right Controller");
        if (rightCtrl != null)
        {
            tipSource = rightCtrl.transform;
            return;
        }

        Camera cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();
        if (cam != null) tipSource = cam.transform;
    }

    private NetworkRunner GetRunner()
    {
        if (runner == null || !runner.IsRunning)
        {
            runner = FindFirstObjectByType<NetworkRunner>();
        }
        return runner;
    }

    void Update()
    {
        var netRunner = GetRunner();
        if (netRunner == null || !netRunner.IsRunning) return;

        // ১. প্যালেট টগল (P কী)
        bool pDown = Input.GetKeyDown(KeyCode.P) || (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame);
        if (pDown) showPalette = !showPalette;

        // ২. কালার সিলেকশন (১ = লাল, ২ = নীল, ৩ = হলুদ)
        bool k1 = Input.GetKeyDown(KeyCode.Alpha1) || (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame);
        bool k2 = Input.GetKeyDown(KeyCode.Alpha2) || (Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame);
        bool k3 = Input.GetKeyDown(KeyCode.Alpha3) || (Keyboard.current != null && Keyboard.current.digit3Key.wasPressedThisFrame);

        if (k1) activeColor = new Color(1f, 0.05f, 0.05f);
        if (k2) activeColor = new Color(0f, 0.55f, 1f);
        if (k3) activeColor = new Color(1f, 0.92f, 0.016f);

        // ৩. মাউসের Left Click = শুধুমাত্র হলুদ মার্কার বল
        bool leftClickDown = Input.GetMouseButtonDown(0);
        if (Mouse.current != null) leftClickDown |= Mouse.current.leftButton.wasPressedThisFrame;

        // প্যালেটের ওপর ক্লিক পড়লে যাতে মার্কার স্পন না হয়
        if (showPalette && Input.mousePosition.x < 240 && Input.mousePosition.y > Screen.height - 240)
        {
            leftClickDown = false;
        }

        if (leftClickDown && markerPrefab.IsValid)
        {
            Vector3 markerPos = GetCurrentTipPosition();
            netRunner.Spawn(markerPrefab, markerPos, Quaternion.identity);
        }

        // ৪. শুধুমাত্র F কী = দাগ কাটা (মাউস রাইট ক্লিক পুরোপুরি মুক্ত থাকবে VR ক্যামেরার জন্য)
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

        // ৫. C কী = একটি একটি করে দাগ মুছে ফেলা
        bool cDown = Input.GetKeyDown(KeyCode.C) || (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame);
        if (cDown)
        {
            RemoveLastStroke(netRunner);
        }
    }

    private void OnGUI()
    {
        if (!showPalette) return;

        GUI.Box(new Rect(20, 20, 200, 190), "🎨 3D Color Palette (P: Hide)");

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

    private Vector3 GetCurrentTipPosition()
    {
        if (tipSource == null) FindTipSource();
        if (tipSource != null)
        {
            return tipSource.position + (tipSource.forward * forwardOffset);
        }
        return new Vector3(0, 1.2f, 1f);
    }

    private void StartDrawing(NetworkRunner netRunner)
    {
        if (!strokePrefab.IsValid) return;

        Vector3 startPos = GetCurrentTipPosition();
        currentStrokeObj = netRunner.Spawn(strokePrefab, startPos, Quaternion.identity);

        if (currentStrokeObj != null)
        {
            strokeHistory.Add(currentStrokeObj);
            currentStrokeScript = currentStrokeObj.GetComponent<NetworkStroke>();

            if (currentStrokeScript != null)
            {
                currentStrokeScript.InitColor(activeColor);
                currentStrokeScript.AddPoint(startPos);
                lastDrawPoint = startPos;
                isDrawing = true;
            }
        }
    }

    private void ContinueDrawing()
    {
        if (currentStrokeScript == null) return;

        Vector3 currentPos = GetCurrentTipPosition();
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
}
