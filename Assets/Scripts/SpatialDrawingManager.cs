
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Fusion;

public class SpatialDrawingManager : MonoBehaviour
{
    [Header("Drawing Configuration")]
    [SerializeField] private NetworkPrefabRef strokePrefab;
    [SerializeField] private float minDistance = 0.02f;
    [SerializeField] private float forwardOffset = 0.8f;

    [Header("Brush Settings")]
    [SerializeField] private Color brushColor = Color.red;

    private NetworkRunner runner;
    private NetworkObject activeStrokeObj;
    private NetworkStroke activeStrokeScript;
    private Vector3 previousPoint;
    private bool isDrawingActive = false;
    private List<NetworkObject> strokeHistory = new List<NetworkObject>();

    void Start()
    {
        brushColor = Color.red;
    }

    private NetworkRunner ResolveRunner()
    {
        if (runner == null || !runner.IsRunning)
        {
            runner = FindFirstObjectByType<NetworkRunner>();
        }
        return runner;
    }

    void Update()
    {
        // ১. কিবোর্ড কালার (1, 2, 3)
        if (Input.GetKeyDown(KeyCode.Alpha1) || (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame))
            brushColor = Color.red;
        if (Input.GetKeyDown(KeyCode.Alpha2) || (Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame))
            brushColor = Color.yellow;
        if (Input.GetKeyDown(KeyCode.Alpha3) || (Keyboard.current != null && Keyboard.current.digit3Key.wasPressedThisFrame))
            brushColor = new Color(0f, 0.7f, 1f);

        // ২. কিবোর্ড F অথবা মাউসের রাইট ক্লিক
        bool startAction = Input.GetKeyDown(KeyCode.F) || Input.GetMouseButtonDown(1);
        bool holdAction = Input.GetKey(KeyCode.F) || Input.GetMouseButton(1);
        bool endAction = Input.GetKeyUp(KeyCode.F) || Input.GetMouseButtonUp(1);

        if (Keyboard.current != null)
        {
            startAction |= Keyboard.current.fKey.wasPressedThisFrame;
            holdAction |= Keyboard.current.fKey.isPressed;
            endAction |= Keyboard.current.fKey.wasReleasedThisFrame;
        }

        if (Mouse.current != null)
        {
            startAction |= Mouse.current.rightButton.wasPressedThisFrame;
            holdAction |= Mouse.current.rightButton.isPressed;
            endAction |= Mouse.current.rightButton.wasReleasedThisFrame;
        }

        if (startAction)
        {
            BeginStroke();
        }
        else if (holdAction && isDrawingActive)
        {
            ContinueStroke();
        }
        else if (endAction && isDrawingActive)
        {
            EndStroke();
        }

        // ৩. ক্লিয়ার (X)
        if (Input.GetKeyDown(KeyCode.X) || (Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame))
        {
            ClearAll();
        }
    }

    // স্ক্রিনের বাম-উপরে সরাসরি ক্লিক করার জন্য বাটন (সিমুলেটরকে সম্পূর্ণ বাইপাস করার জন্য)
    private void OnGUI()
    {
        if (GUI.Button(new Rect(20, 20, 160, 50), "DRAW TEST"))
        {
            BeginStroke();
        }

        if (isDrawingActive && GUI.Button(new Rect(190, 20, 120, 50), "STOP"))
        {
            EndStroke();
        }
    }

    private Vector3 CalculateDrawOrigin()
    {
        var rightHand = GameObject.Find("Right Controller");
        if (rightHand != null)
        {
            return rightHand.transform.position + (rightHand.transform.forward * 0.2f);
        }

        Camera activeCam = Camera.main;
        if (activeCam == null) activeCam = FindFirstObjectByType<Camera>();
        if (activeCam != null)
        {
            return activeCam.transform.position + (activeCam.transform.forward * forwardOffset);
        }

        return new Vector3(0, 1.2f, 1f);
    }

    public void BeginStroke()
    {
        var activeRunner = ResolveRunner();
        if (activeRunner == null || !activeRunner.IsRunning)
        {
            Debug.LogError("[Drawing] NetworkRunner কানেক্টেড নেই! সেশন চালু হতে দিন।");
            return;
        }

        Vector3 spawnPosition = CalculateDrawOrigin();

        // ফিউশন স্পন
        activeStrokeObj = activeRunner.Spawn(strokePrefab, spawnPosition, Quaternion.identity);

        if (activeStrokeObj != null)
        {
            strokeHistory.Add(activeStrokeObj);
            activeStrokeScript = activeStrokeObj.GetComponent<NetworkStroke>();

            if (activeStrokeScript != null)
            {
                activeStrokeScript.InitColor(brushColor);
                activeStrokeScript.AddPoint(spawnPosition);
                activeStrokeScript.Rpc_SyncPoint(spawnPosition);
                previousPoint = spawnPosition;
                isDrawingActive = true;
                Debug.Log($"[Drawing] ড্রয়িং সফলভাবে শুরু হয়েছে পজিশনে: {spawnPosition}");
            }
        }
    }

    private void ContinueStroke()
    {
        if (activeStrokeScript == null) return;

        Vector3 currentPos = CalculateDrawOrigin();
        if (Vector3.Distance(currentPos, previousPoint) >= minDistance)
        {
            previousPoint = currentPos;
            activeStrokeScript.AddPoint(currentPos);
            activeStrokeScript.Rpc_SyncPoint(currentPos);
        }
    }

    public void EndStroke()
    {
        isDrawingActive = false;
        activeStrokeScript = null;
        activeStrokeObj = null;
    }

    public void ClearAll()
    {
        var activeRunner = ResolveRunner();
        if (activeRunner != null)
        {
            foreach (var stroke in strokeHistory)
            {
                if (stroke != null) activeRunner.Despawn(stroke);
            }
        }
        strokeHistory.Clear();
    }
}