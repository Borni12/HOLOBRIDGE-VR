using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Fusion;

public class VRMarkerSpawner : MonoBehaviour
{
    [Header("Fusion Prefabs")]
    [SerializeField] private NetworkPrefabRef markerPrefab; // MarkerPin
    [SerializeField] private NetworkPrefabRef strokePrefab; // NetworkStroke

    [Header("Drawing Configuration")]
    [SerializeField] private float minVertexDistance = 0.02f;
    [SerializeField] private float forwardDistance = 0.6f;

    [Header("Brush Settings")]
    [SerializeField] private Color activeColor = Color.red;

    private NetworkRunner runner;
    private NetworkObject currentStrokeObj;
    private NetworkStroke currentStrokeScript;
    private Vector3 lastDrawPoint;
    private bool isDrawing = false;
    private Transform drawTip;

    private List<NetworkObject> strokeList = new List<NetworkObject>();

    void Start()
    {
        FindTip();
        activeColor = Color.red;
    }

    private void FindTip()
    {
        var rightCtrl = GameObject.Find("Right Controller");
        if (rightCtrl != null)
        {
            drawTip = rightCtrl.transform;
            return;
        }

        Camera cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();
        if (cam != null) drawTip = cam.transform;
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

        // ১. কালার নির্বাচন (১ = লাল, ২ = নীল, ৩ = হলুদ)
        bool k1 = Input.GetKeyDown(KeyCode.Alpha1) || (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame);
        bool k2 = Input.GetKeyDown(KeyCode.Alpha2) || (Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame);
        bool k3 = Input.GetKeyDown(KeyCode.Alpha3) || (Keyboard.current != null && Keyboard.current.digit3Key.wasPressedThisFrame);

        if (k1) { activeColor = Color.red; Debug.Log("<color=red>Color Selected: RED</color>"); }
        if (k2) { activeColor = new Color(0f, 0.6f, 1f); Debug.Log("<color=cyan>Color Selected: BLUE</color>"); }
        if (k3) { activeColor = Color.yellow; Debug.Log("<color=yellow>Color Selected: YELLOW</color>"); }

        // ২. হলুদ বল স্পন (Space কী)
        bool spawnMarker = Input.GetKeyDown(KeyCode.Space) || (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame);
        if (spawnMarker && markerPrefab.IsValid)
        {
            Vector3 markerPos = GetTipPosition();
            netRunner.Spawn(markerPrefab, markerPos, Quaternion.identity);
            Debug.Log($"[Marker] হলুদ মার্কার তৈরি হয়েছে: {markerPos}");
        }

        // ৩. দাগ আঁকা (F বাটন অথবা মাউস রাইট ক্লিক)
        bool fDown = Input.GetKeyDown(KeyCode.F) || Input.GetMouseButtonDown(1);
        bool fHold = Input.GetKey(KeyCode.F) || Input.GetMouseButton(1);
        bool fUp = Input.GetKeyUp(KeyCode.F) || Input.GetMouseButtonUp(1);

        if (Keyboard.current != null)
        {
            fDown |= Keyboard.current.fKey.wasPressedThisFrame;
            fHold |= Keyboard.current.fKey.isPressed;
            fUp |= Keyboard.current.fKey.wasReleasedThisFrame;
        }
        if (Mouse.current != null)
        {
            fDown |= Mouse.current.rightButton.wasPressedThisFrame;
            fHold |= Mouse.current.rightButton.isPressed;
            fUp |= Mouse.current.rightButton.wasReleasedThisFrame;
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

        // ৪. C চাপলে একটি একটি করে দাগ রিমুভ (Undo)
        bool cDown = Input.GetKeyDown(KeyCode.C) || (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame);
        if (cDown)
        {
            RemoveLastStroke(netRunner);
        }

        // ৫. X চাপলে সব দাগ একসাথে ক্লিয়ার
        bool xDown = Input.GetKeyDown(KeyCode.X) || (Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame);
        if (xDown)
        {
            ClearAll(netRunner);
        }
    }

    // অন-স্ক্রিন বাটন (সিমুলেটরকে শতভাগ পাশ কাটিয়ে নিশ্চিত টেস্ট করার জন্য)
    private void OnGUI()
    {
        if (GUI.Button(new Rect(30, 30, 200, 60), "FORCE DRAW"))
        {
            var netRunner = GetRunner();
            if (netRunner != null && netRunner.IsRunning)
            {
                StartDrawing(netRunner);
                // তাৎক্ষণিক টেস্টের জন্য একটি ছোট লাইন তৈরি
                Vector3 current = GetTipPosition();
                ContinueDrawingTestPoint(current + Vector3.right * 0.2f);
                StopDrawing();
            }
        }
    }

    private Vector3 GetTipPosition()
    {
        if (drawTip == null) FindTip();

        if (drawTip != null)
        {
            return drawTip.position + (drawTip.forward * forwardDistance);
        }

        return new Vector3(0, 1.2f, 1f);
    }

    private void StartDrawing(NetworkRunner netRunner)
    {
        if (!strokePrefab.IsValid)
        {
            Debug.LogError("[VR Drawing] Stroke Prefab অ্যাসাইন করা নেই বা ইনভ্যালিড!");
            return;
        }

        Vector3 spawnPos = GetTipPosition();
        currentStrokeObj = netRunner.Spawn(strokePrefab, spawnPos, Quaternion.identity);

        if (currentStrokeObj != null)
        {
            strokeList.Add(currentStrokeObj);
            currentStrokeScript = currentStrokeObj.GetComponent<NetworkStroke>();

            if (currentStrokeScript != null)
            {
                currentStrokeScript.InitColor(activeColor);
                currentStrokeScript.AddPoint(spawnPos);
                currentStrokeScript.Rpc_SyncPoint(spawnPos);
                lastDrawPoint = spawnPos;
                isDrawing = true;
                Debug.Log($"[VR Drawing] দাগ আঁকা শুরু হয়েছে: {spawnPos}");
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
            currentStrokeScript.Rpc_SyncPoint(currentPos);
        }
    }

    private void ContinueDrawingTestPoint(Vector3 pt)
    {
        if (currentStrokeScript == null) return;
        currentStrokeScript.AddPoint(pt);
        currentStrokeScript.Rpc_SyncPoint(pt);
    }

    private void StopDrawing()
    {
        isDrawing = false;
        currentStrokeScript = null;
        currentStrokeObj = null;
    }

    public void RemoveLastStroke(NetworkRunner netRunner)
    {
        if (strokeList.Count > 0)
        {
            int lastIndex = strokeList.Count - 1;
            NetworkObject stroke = strokeList[lastIndex];
            strokeList.RemoveAt(lastIndex);

            if (stroke != null && netRunner.IsRunning)
            {
                netRunner.Despawn(stroke);
                Debug.Log("[VR Drawing] একটি দাগ মুছে ফেলা হয়েছে।");
            }
        }
    }

    public void ClearAll(NetworkRunner netRunner)
    {
        foreach (var stroke in strokeList)
        {
            if (stroke != null && netRunner.IsRunning)
            {
                netRunner.Despawn(stroke);
            }
        }
        strokeList.Clear();
        Debug.Log("[VR Drawing] সমস্ত দাগ ক্লিয়ার করা হয়েছে।");
    }
}
