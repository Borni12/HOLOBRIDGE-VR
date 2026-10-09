
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class SpatialDrawingManager : MonoBehaviour
{
    [Header("Drawing Configuration")]
    [SerializeField] private GameObject strokePrefab;
    [SerializeField] private float minVertexDistance = 0.02f;
    [SerializeField] private float drawDistance = 1.0f;

    // ডিফল্ট সলিড লাল
    private Color currentColor = Color.red;

    private LineRenderer currentLine;
    private List<Vector3> pointBuffer = new List<Vector3>();
    private bool isDrawing = false;
    private Camera activeCam;
    private List<GameObject> spawnedStrokes = new List<GameObject>();

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

        if (key1) { currentColor = Color.red; Debug.Log("Selected Solid Red"); }
        if (key2) { currentColor = Color.green; Debug.Log("Selected Solid Green"); }
        if (key3) { currentColor = Color.blue; Debug.Log("Selected Solid Blue"); }

        // ২. F কী চেপে ড্রয়িং
        bool fPressed = false;
        bool fHeld = false;
        bool fReleased = false;

        if (Keyboard.current != null)
        {
            fPressed = Keyboard.current.fKey.wasPressedThisFrame;
            fHeld = Keyboard.current.fKey.isPressed;
            fReleased = Keyboard.current.fKey.wasReleasedThisFrame;
        }
        else
        {
            fPressed = Input.GetKeyDown(KeyCode.F);
            fHeld = Input.GetKey(KeyCode.F);
            fReleased = Input.GetKeyUp(KeyCode.F);
        }

        if (fPressed) StartDrawing();
        else if (fHeld && isDrawing) UpdateDrawing();
        else if (fReleased && isDrawing) StopDrawing();

        // ৩. C কী চাপলে একটি একটি করে শেষ দাগ মুছবে
        bool cPressed = (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame) || Input.GetKeyDown(KeyCode.C);
        if (cPressed)
        {
            RemoveLastStroke();
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
        if (strokePrefab == null) return;
        FindCam();

        Vector3 spawnPos = GetDrawPoint();
        GameObject strokeObj = Instantiate(strokePrefab, Vector3.zero, Quaternion.identity);
        spawnedStrokes.Add(strokeObj);

        currentLine = strokeObj.GetComponent<LineRenderer>();
        if (currentLine != null)
        {
            currentLine.useWorldSpace = true;

            // মেটেরিয়ালের প্রিফ্যাব লক ভাঙতে নতুন ইনস্ট্যান্স তৈরি ও কালার পাস
            Material lineMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            lineMat.color = currentColor;
            lineMat.SetColor("_BaseColor", currentColor);
            currentLine.material = lineMat;

            // LineRenderer-এর নিজস্ব কালার সেট
            currentLine.startColor = currentColor;
            currentLine.endColor = currentColor;

            pointBuffer.Clear();
            pointBuffer.Add(spawnPos);
            pointBuffer.Add(spawnPos + Vector3.up * 0.005f);
            currentLine.positionCount = 2;
            currentLine.SetPositions(pointBuffer.ToArray());
        }

        isDrawing = true;
    }

    private void UpdateDrawing()
    {
        if (currentLine == null) return;

        Vector3 currentPos = GetDrawPoint();
        if (pointBuffer.Count > 0 && Vector3.Distance(pointBuffer[pointBuffer.Count - 1], currentPos) >= minVertexDistance)
        {
            pointBuffer.Add(currentPos);
            currentLine.positionCount = pointBuffer.Count;
            currentLine.SetPosition(pointBuffer.Count - 1, currentPos);
        }
    }

    public void StopDrawing()
    {
        isDrawing = false;
        currentLine = null;
        pointBuffer.Clear();
    }

    public void RemoveLastStroke()
    {
        if (spawnedStrokes.Count > 0)
        {
            int lastIndex = spawnedStrokes.Count - 1;
            GameObject lastStroke = spawnedStrokes[lastIndex];
            spawnedStrokes.RemoveAt(lastIndex);

            if (lastStroke != null)
            {
                Destroy(lastStroke);
            }
        }
    }
}