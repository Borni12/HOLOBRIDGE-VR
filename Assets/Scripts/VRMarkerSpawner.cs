using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Fusion;

public class VRMarkerSpawner : MonoBehaviour
{
    [Header("Fusion Prefabs")]
    [SerializeField] private NetworkPrefabRef markerPrefab;   // আপনার বিদ্যমান হলুদ মার্কার পিন
    [SerializeField] private NetworkPrefabRef strokePrefab;   // নতুন লাইন ড্রয়িং প্রিফ্যাব (NetworkStroke)

    [Header("Drawing Parameters")]
    [SerializeField] private float minVertexDistance = 0.02f;
    [SerializeField] private float forwardOffset = 0.5f;

    [Header("Current Color Settings")]
    [SerializeField] private Color activeColor = Color.red;

    private NetworkRunner runner;
    private NetworkObject currentActiveStroke;
    private NetworkStroke currentStrokeScript;
    private Vector3 lastDrawPoint;
    private bool isDrawing = false;
    private Transform drawingTip;

    // একটার পর একটা দাগ মুছার জন্য হিস্ট্রি লিস্ট
    private List<NetworkObject> strokeHistory = new List<NetworkObject>();

    void Start()
    {
        FindTipSource();
        activeColor = Color.red; // ডিফল্ট লাল রঙ
    }

    private void FindTipSource()
    {
        // কন্ট্রোলার বা ক্যামেরা ট্রেস করা
        var rightController = GameObject.Find("Right Controller");
        if (rightController != null)
        {
            drawingTip = rightController.transform;
            return;
        }

        Camera cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();
        if (cam != null) drawingTip = cam.transform;
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
        // ১. রঙ পরিবর্তন: ১ = লাল, ২ = নীল, ৩ = হলুদ
        bool press1 = Input.GetKeyDown(KeyCode.Alpha1) || (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame);
        bool press2 = Input.GetKeyDown(KeyCode.Alpha2) || (Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame);
        bool press3 = Input.GetKeyDown(KeyCode.Alpha3) || (Keyboard.current != null && Keyboard.current.digit3Key.wasPressedThisFrame);

        if (press1)
        {
            activeColor = Color.red;
            Debug.Log("[VR Drawing] Color Selected: RED");
        }
        else if (press2)
        {
            activeColor = new Color(0f, 0.5f, 1f); // নীল
            Debug.Log("[VR Drawing] Color Selected: BLUE");
        }
        else if (press3)
        {
            activeColor = Color.yellow;
            Debug.Log("[VR Drawing] Color Selected: YELLOW");
        }

        // ২. F কী দিয়ে দাগ ড্রয়িং (Down, Hold, Up)
        bool fDown = Input.GetKeyDown(KeyCode.F) || (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame);
        bool fHold = Input.GetKey(KeyCode.F) || (Keyboard.current != null && Keyboard.current.fKey.isPressed);
        bool fUp = Input.GetKeyUp(KeyCode.F) || (Keyboard.current != null && Keyboard.current.fKey.wasReleasedThisFrame);

        if (fDown)
        {
            StartDrawingStroke();
        }
        else if (fHold && isDrawing)
        {
            ContinueDrawingStroke();
        }
        else if (fUp && isDrawing)
        {
            StopDrawingStroke();
        }

        // ৩. C কী চাপলে একটি একটি করে দাগ রিমুভ (Undo)
        bool cDown = Input.GetKeyDown(KeyCode.C) || (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame);
        if (cDown)
        {
            RemoveLastStrokeOneByOne();
        }
    }

    private Vector3 GetCurrentTipPosition()
    {
        if (drawingTip == null) FindTipSource();

        if (drawingTip != null)
        {
            return drawingTip.position + (drawingTip.forward * forwardOffset);
        }

        return new Vector3(0, 1.2f, 1f);
    }

    private void StartDrawingStroke()
    {
        var netRunner = GetRunner();
        if (netRunner == null || !netRunner.IsRunning)
        {
            Debug.LogWarning("[VR Drawing] NetworkRunner সক্রিয় নেই!");
            return;
        }

        if (!strokePrefab.IsValid)
        {
            Debug.LogError("[VR Drawing] Stroke Prefab অ্যাসাইন করা নেই বা ইনভ্যালিড!");
            return;
        }

        Vector3 startPos = GetCurrentTipPosition();

        // নেটওয়ার্কে স্পন
        currentActiveStroke = netRunner.Spawn(strokePrefab, startPos, Quaternion.identity);

        if (currentActiveStroke != null)
        {
            strokeHistory.Add(currentActiveStroke);
            currentStrokeScript = currentActiveStroke.GetComponent<NetworkStroke>();

            if (currentStrokeScript != null)
            {
                currentStrokeScript.InitColor(activeColor);
                currentStrokeScript.AddPoint(startPos);
                currentStrokeScript.Rpc_SyncPoint(startPos);
                lastDrawPoint = startPos;
                isDrawing = true;
            }
        }
    }

    private void ContinueDrawingStroke()
    {
        if (currentStrokeScript == null) return;

        Vector3 currentPos = GetCurrentTipPosition();
        if (Vector3.Distance(currentPos, lastDrawPoint) >= minVertexDistance)
        {
            lastDrawPoint = currentPos;
            currentStrokeScript.AddPoint(currentPos);
            currentStrokeScript.Rpc_SyncPoint(currentPos);
        }
    }

    private void StopDrawingStroke()
    {
        isDrawing = false;
        currentStrokeScript = null;
        currentActiveStroke = null;
    }

    // একটি একটি করে দাগ মোছার ফাংশন
    public void RemoveLastStrokeOneByOne()
    {
        if (strokeHistory.Count > 0)
        {
            int lastIndex = strokeHistory.Count - 1;
            NetworkObject strokeToRemove = strokeHistory[lastIndex];
            strokeHistory.RemoveAt(lastIndex);

            var netRunner = GetRunner();
            if (strokeToRemove != null && netRunner != null && netRunner.IsRunning)
            {
                netRunner.Despawn(strokeToRemove);
                Debug.Log("[VR Drawing] সর্বশেষ দাগটি সফলভাবে ডিলিট করা হয়েছে।");
            }
        }
    }
}