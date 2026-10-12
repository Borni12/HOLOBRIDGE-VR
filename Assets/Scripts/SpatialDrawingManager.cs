using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class SpatialDrawingManager : MonoBehaviour
{
    [Header("Drawing Configuration")]
    [SerializeField] private GameObject strokePrefab;
    [SerializeField] private float minVertexDistance = 0.02f;
    [SerializeField] private float drawDistance = 1.0f;

    [Header("Brush Settings")]
    [SerializeField] private Color currentColor = Color.red;

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

        if (key1) SetColorRed();
        if (key2) SetColorYellow();
        if (key3) SetColorBlue();

        // ২. F কী চেপে অথবা মাউসের লেফট ক্লিক দিয়ে ড্রয়িং
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
        if (strokePrefab == null) return;
        FindCam();

        Vector3 spawnPos = GetDrawPoint();
        GameObject strokeObj = Instantiate(strokePrefab, Vector3.zero, Quaternion.identity);
        spawnedStrokes.Add(strokeObj);

        currentLine = strokeObj.GetComponent<LineRenderer>();
        if (currentLine != null)
        {
            currentLine.useWorldSpace = true;

            // মেটেরিয়াল ইন্সট্যান্স তৈরি ও কালার পাস
            Material lineMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            lineMat.color = currentColor;
            lineMat.SetColor("_BaseColor", currentColor);
            currentLine.material = lineMat;

            // LineRenderer কালার সেট
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

    // -------------------------------------------------------------
    // 3D UI Hand Menu বাটনগুলোর সাথে যুক্ত করার মেথডসমূহ
    // -------------------------------------------------------------

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
        currentColor = new Color(0f, 0.7f, 1f); // Neon Cyan / Blue
        Debug.Log("Spatial UI: Selected Color Blue");
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
                Debug.Log("Spatial UI: Undo Last Stroke");
            }
        }
    }

    public void ClearAllStrokes()
    {
        foreach (var stroke in spawnedStrokes)
        {
            if (stroke != null)
            {
                Destroy(stroke);
            }
        }
        spawnedStrokes.Clear();
        Debug.Log("Spatial UI: Cleared All Strokes");
    }
}
