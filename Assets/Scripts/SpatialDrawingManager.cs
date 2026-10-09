using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class SpatialDrawingManager : MonoBehaviour
{
    [Header("Drawing Configuration")]
    [SerializeField] private GameObject strokePrefab;
    [SerializeField] private float minVertexDistance = 0.02f;
    [SerializeField] private float drawDistance = 1.0f; // চোখের সামনে ১ মিটার দূরত্ব

    private LineRenderer currentLine;
    private List<Vector3> pointBuffer = new List<Vector3>();
    private bool isDrawing = false;
    private Camera activeCam;

    void Start()
    {
        FindCam();
    }

    private void FindCam()
    {
        activeCam = Camera.main;
        if (activeCam == null) activeCam = FindFirstObjectByType<Camera>();
    }

    void Update()
    {
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

        if (fPressed)
        {
            StartDrawing();
        }
        else if (fHeld && isDrawing)
        {
            UpdateDrawing();
        }
        else if (fReleased && isDrawing)
        {
            StopDrawing();
        }
    }

    private Vector3 GetDrawPoint()
    {
        if (activeCam == null) FindCam();
        if (activeCam == null) return new Vector3(0, 1.2f, 1f);

        Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;

        // স্ক্রিন পয়েন্ট থেকে সরাসরি ক্যামেরার ভিউ ফ্রাস্টামে ওয়ার্ল্ড পজিশন নেওয়া
        Vector3 screenPos = new Vector3(mousePos.x, mousePos.y, drawDistance);
        return activeCam.ScreenToWorldPoint(screenPos);
    }

    public void StartDrawing()
    {
        if (strokePrefab == null)
        {
            Debug.LogError("Stroke Prefab খালি রয়েছে!");
            return;
        }

        FindCam();
        Vector3 spawnPos = GetDrawPoint();

        // নতুন স্ট্রোক তৈরি
        GameObject strokeObj = Instantiate(strokePrefab, Vector3.zero, Quaternion.identity);
        currentLine = strokeObj.GetComponent<LineRenderer>();

        if (currentLine != null)
        {
            currentLine.useWorldSpace = true;
            pointBuffer.Clear();
            pointBuffer.Add(spawnPos);
            pointBuffer.Add(spawnPos + Vector3.up * 0.01f); // দৃশ্যমান পয়েন্ট

            currentLine.positionCount = 2;
            currentLine.SetPositions(pointBuffer.ToArray());
        }

        isDrawing = true;
        Debug.Log("Drawing Started at: " + spawnPos);
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
}
