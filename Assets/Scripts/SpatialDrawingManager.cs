
//using UnityEngine;
//using UnityEngine.InputSystem;
//using System.Collections.Generic;
//using Fusion;

//public class SpatialDrawingManager : MonoBehaviour
//{
//    [Header("Drawing Configuration")]
//    [SerializeField] private NetworkPrefabRef strokePrefab;
//    [SerializeField] private float minDistance = 0.02f;
//    [SerializeField] private float forwardOffset = 0.8f;

//    [Header("Brush Settings")]
//    [SerializeField] private Color brushColor = Color.red;

//    private NetworkRunner runner;
//    private NetworkObject activeStrokeObj;
//    private NetworkStroke activeStrokeScript;
//    private Vector3 previousPoint;
//    private bool isDrawingActive = false;
//    private List<NetworkObject> strokeHistory = new List<NetworkObject>();

//    void Start()
//    {
//        brushColor = Color.red;
//    }

//    private NetworkRunner ResolveRunner()
//    {
//        if (runner == null || !runner.IsRunning)
//        {
//            runner = FindFirstObjectByType<NetworkRunner>();
//        }
//        return runner;
//    }

//    void Update()
//    {
//        // ১. কিবোর্ড কালার (1, 2, 3)
//        if (Input.GetKeyDown(KeyCode.Alpha1) || (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame))
//            brushColor = Color.red;
//        if (Input.GetKeyDown(KeyCode.Alpha2) || (Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame))
//            brushColor = Color.yellow;
//        if (Input.GetKeyDown(KeyCode.Alpha3) || (Keyboard.current != null && Keyboard.current.digit3Key.wasPressedThisFrame))
//            brushColor = new Color(0f, 0.7f, 1f);

//        // ২. কিবোর্ড F অথবা মাউসের রাইট ক্লিক
//        bool startAction = Input.GetKeyDown(KeyCode.F) || Input.GetMouseButtonDown(1);
//        bool holdAction = Input.GetKey(KeyCode.F) || Input.GetMouseButton(1);
//        bool endAction = Input.GetKeyUp(KeyCode.F) || Input.GetMouseButtonUp(1);

//        if (Keyboard.current != null)
//        {
//            startAction |= Keyboard.current.fKey.wasPressedThisFrame;
//            holdAction |= Keyboard.current.fKey.isPressed;
//            endAction |= Keyboard.current.fKey.wasReleasedThisFrame;
//        }

//        if (Mouse.current != null)
//        {
//            startAction |= Mouse.current.rightButton.wasPressedThisFrame;
//            holdAction |= Mouse.current.rightButton.isPressed;
//            endAction |= Mouse.current.rightButton.wasReleasedThisFrame;
//        }

//        if (startAction)
//        {
//            BeginStroke();
//        }
//        else if (holdAction && isDrawingActive)
//        {
//            ContinueStroke();
//        }
//        else if (endAction && isDrawingActive)
//        {
//            EndStroke();
//        }

//        // ৩. ক্লিয়ার (X)
//        if (Input.GetKeyDown(KeyCode.X) || (Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame))
//        {
//            ClearAll();
//        }
//    }

//    // স্ক্রিনের বাম-উপরে সরাসরি ক্লিক করার জন্য বাটন (সিমুলেটরকে সম্পূর্ণ বাইপাস করার জন্য)
//    private void OnGUI()
//    {
//        if (GUI.Button(new Rect(20, 20, 160, 50), "DRAW TEST"))
//        {
//            BeginStroke();
//        }

//        if (isDrawingActive && GUI.Button(new Rect(190, 20, 120, 50), "STOP"))
//        {
//            EndStroke();
//        }
//    }

//    private Vector3 CalculateDrawOrigin()
//    {
//        var rightHand = GameObject.Find("Right Controller");
//        if (rightHand != null)
//        {
//            return rightHand.transform.position + (rightHand.transform.forward * 0.2f);
//        }

//        Camera activeCam = Camera.main;
//        if (activeCam == null) activeCam = FindFirstObjectByType<Camera>();
//        if (activeCam != null)
//        {
//            return activeCam.transform.position + (activeCam.transform.forward * forwardOffset);
//        }

//        return new Vector3(0, 1.2f, 1f);
//    }

//    public void BeginStroke()
//    {
//        var activeRunner = ResolveRunner();
//        if (activeRunner == null || !activeRunner.IsRunning)
//        {
//            Debug.LogError("[Drawing] NetworkRunner কানেক্টেড নেই! সেশন চালু হতে দিন।");
//            return;
//        }

//        Vector3 spawnPosition = CalculateDrawOrigin();

//        // ফিউশন স্পন
//        activeStrokeObj = activeRunner.Spawn(strokePrefab, spawnPosition, Quaternion.identity);

//        if (activeStrokeObj != null)
//        {
//            strokeHistory.Add(activeStrokeObj);
//            activeStrokeScript = activeStrokeObj.GetComponent<NetworkStroke>();

//            if (activeStrokeScript != null)
//            {
//                activeStrokeScript.InitColor(brushColor);
//                activeStrokeScript.AddPoint(spawnPosition);
//                activeStrokeScript.Rpc_SyncPoint(spawnPosition);
//                previousPoint = spawnPosition;
//                isDrawingActive = true;
//                Debug.Log($"[Drawing] ড্রয়িং সফলভাবে শুরু হয়েছে পজিশনে: {spawnPosition}");
//            }
//        }
//    }

//    private void ContinueStroke()
//    {
//        if (activeStrokeScript == null) return;

//        Vector3 currentPos = CalculateDrawOrigin();
//        if (Vector3.Distance(currentPos, previousPoint) >= minDistance)
//        {
//            previousPoint = currentPos;
//            activeStrokeScript.AddPoint(currentPos);
//            activeStrokeScript.Rpc_SyncPoint(currentPos);
//        }
//    }

//    public void EndStroke()
//    {
//        isDrawingActive = false;
//        activeStrokeScript = null;
//        activeStrokeObj = null;
//    }

//    public void ClearAll()
//    {
//        var activeRunner = ResolveRunner();
//        if (activeRunner != null)
//        {
//            foreach (var stroke in strokeHistory)
//            {
//                if (stroke != null) activeRunner.Despawn(stroke);
//            }
//        }
//        strokeHistory.Clear();
//    }
//}



using UnityEngine;
using Fusion;

public class SpatialDrawingManager : MonoBehaviour
{
    [SerializeField] private NetworkPrefabRef strokePrefab;
    private NetworkRunner runner;

    private NetworkRunner GetRunner()
    {
        if (runner == null || !runner.IsRunning)
            runner = FindFirstObjectByType<NetworkRunner>();
        return runner;
    }

    //void Update()
    //{
    //    // সরাসরি কীবোর্ড F বাটন চেক
    //    if (Input.GetKeyDown(KeyCode.F))
    //    {
    //        Debug.Log("<color=green>[TEST 1] কীবোর্ডের F বাটন সফলভাবে চাপ পড়েছে!</color>");
    //        TriggerDraw();
    //    }
    //}

    private void OnGUI()
    {
        // স্ক্রিনের উপরে বড় বাটন (সিমুলেটরকে পুরোপুরি পাশ কাটিয়ে টেস্ট করার জন্য)
        if (GUI.Button(new Rect(30, 30, 220, 70), "FORCE DRAW TEST"))
        {
            Debug.Log("<color=green>[TEST 1] অন-স্ক্রিন বাটন সফলভাবে চাপ পড়েছে!</color>");
            TriggerDraw();
        }
    }

    private void TriggerDraw()
    {
        var netRunner = GetRunner();
        if (netRunner == null)
        {
            Debug.LogError("<color=red>[TEST 2 ব্যর্থ] সিনে কোনো NetworkRunner পাওয়া যায়নি!</color>");
            return;
        }

        if (!netRunner.IsRunning)
        {
            Debug.LogError("<color=red>[TEST 2 ব্যর্থ] NetworkRunner চালু নেই (ক্লাউড সেশনে জয়েন করেনি)!</color>");
            return;
        }

        Debug.Log("<color=cyan>[TEST 2 সফল] NetworkRunner সক্রিয় আছে এবং সেশন রানিং।</color>");

        // ক্যামেরার ঠিক ১ মিটার সামনে স্পন করার চেষ্টা
        Vector3 spawnPos = Vector3.forward * 1.5f;
        if (Camera.main != null)
        {
            spawnPos = Camera.main.transform.position + (Camera.main.transform.forward * 1.5f);
        }

        // নেটওয়ার্কে স্পন
        var spawnedObj = netRunner.Spawn(strokePrefab, spawnPos, Quaternion.identity);

        if (spawnedObj == null)
        {
            Debug.LogError("<color=red>[TEST 3 ব্যর্থ] Spawn কল হয়েছে কিন্তু কোনো অবজেক্ট তৈরি হয়নি!</color>");
        }
        else
        {
            Debug.Log($"<color=yellow>[TEST 3 সফল] অবজেক্ট নেটওয়ার্কে স্পন হয়েছে! নাম: {spawnedObj.name}</color>");

            var strokeScript = spawnedObj.GetComponent<NetworkStroke>();
            if (strokeScript != null)
            {
                strokeScript.InitColor(Color.red);
                strokeScript.AddPoint(spawnPos);
                strokeScript.AddPoint(spawnPos + Vector3.right * 0.3f); // টেস্টের জন্য ০.৩ মিটার সোজা একটি লাল রেখা
                strokeScript.Rpc_SyncPoint(spawnPos);
                strokeScript.Rpc_SyncPoint(spawnPos + Vector3.right * 0.3f);
                Debug.Log("<color=white>[TEST 4 সফল] লাইনে ২টি পয়েন্ট পাঠানো হয়েছে!</color>");
            }
            else
            {
                Debug.LogError("<color=red>[TEST 4 ব্যর্থ] স্পন হওয়া অবজেক্টে NetworkStroke.cs স্ক্রিপ্ট পাওয়া যায়নি!</color>");
            }
        }
    }
}