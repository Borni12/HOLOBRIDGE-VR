using UnityEngine;
using Fusion;

[RequireComponent(typeof(LineRenderer))]
public class NetworkStroke : NetworkBehaviour
{
    private LineRenderer lineRenderer;

    [Networked] public Color StrokeColor { get; set; }

    private void Awake()
    {
        EnsureLineRenderer();
    }

    private void EnsureLineRenderer()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.alignment = LineAlignment.View;
            if (lineRenderer.widthMultiplier < 0.04f)
            {
                lineRenderer.widthMultiplier = 0.05f;
            }
        }
    }

    public void InitColor(Color col)
    {
        StrokeColor = col;
        UpdateVisualColor(col);
    }

    public override void Render()
    {
        UpdateVisualColor(StrokeColor);
    }

    private void UpdateVisualColor(Color col)
    {
        EnsureLineRenderer();
        if (lineRenderer != null)
        {
            lineRenderer.startColor = col;
            lineRenderer.endColor = col;
            if (lineRenderer.material != null)
            {
                lineRenderer.material.color = col;
            }
        }
    }

    public void AddPoint(Vector3 pt)
    {
        EnsureLineRenderer();
        if (lineRenderer != null)
        {
            lineRenderer.positionCount++;
            lineRenderer.SetPosition(lineRenderer.positionCount - 1, pt);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_SyncPoint(Vector3 pt)
    {
        AddPoint(pt);
    }
}
