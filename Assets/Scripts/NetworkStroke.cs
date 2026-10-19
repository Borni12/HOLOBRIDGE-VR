using UnityEngine;
using Fusion;

[RequireComponent(typeof(LineRenderer))]
public class NetworkStroke : NetworkBehaviour
{
    private LineRenderer lr;

    [Networked] public Color StrokeColor { get; set; }

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = true;
    }

    public void InitColor(Color col)
    {
        StrokeColor = col;
        UpdateLineVisual(col);
    }

    public override void Render()
    {
        UpdateLineVisual(StrokeColor);
    }

    private void UpdateLineVisual(Color col)
    {
        if (lr == null) lr = GetComponent<LineRenderer>();
        if (lr != null)
        {
            lr.startColor = col;
            lr.endColor = col;
            if (lr.material != null) lr.material.color = col;
        }
    }

    public void AddPoint(Vector3 pt)
    {
        if (lr == null) lr = GetComponent<LineRenderer>();
        if (lr != null)
        {
            lr.positionCount++;
            lr.SetPosition(lr.positionCount - 1, pt);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_SyncPoint(Vector3 pt)
    {
        AddPoint(pt);
    }
}