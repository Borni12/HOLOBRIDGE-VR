using UnityEngine;
using Fusion;

[RequireComponent(typeof(LineRenderer))]
public class NetworkStroke : NetworkBehaviour
{
    private LineRenderer lineRenderer;
    private MaterialPropertyBlock propBlock;

    [Networked] public Color StrokeColor { get; set; }

    private void Awake()
    {
        EnsureSetup();
    }

    private void EnsureSetup()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.alignment = LineAlignment.View;
            lineRenderer.numCornerVertices = 5; // অতিরিক্ত ওভারহেড কমাতে ৫ মান আদর্শ
            lineRenderer.numCapVertices = 5;
            if (lineRenderer.widthMultiplier < 0.04f)
            {
                lineRenderer.widthMultiplier = 0.05f;
            }
        }
        if (propBlock == null)
        {
            propBlock = new MaterialPropertyBlock();
        }
    }

    public void InitColor(Color col)
    {
        StrokeColor = col;
        ApplyStrokeColor(col);
    }

    public override void Render()
    {
        ApplyStrokeColor(StrokeColor);
    }

    private void ApplyStrokeColor(Color col)
    {
        EnsureSetup();
        if (lineRenderer == null) return;

        lineRenderer.startColor = col;
        lineRenderer.endColor = col;

        if (lineRenderer.material != null)
        {
            lineRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor("_BaseColor", col);
            propBlock.SetColor("_Color", col);
            propBlock.SetColor("_EmissionColor", col * 1.5f);
            lineRenderer.SetPropertyBlock(propBlock);
        }
    }

    public void AddPoint(Vector3 pt)
    {
        EnsureSetup();
        if (lineRenderer != null)
        {
            lineRenderer.positionCount++;
            lineRenderer.SetPosition(lineRenderer.positionCount - 1, pt);
        }

        // নেটওয়ার্কের অন্য ক্লায়েন্টদের সাথে সিঙ্ক
        if (Object != null && Object.IsValid && HasStateAuthority)
        {
            Rpc_SyncPoint(pt);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)]
    public void Rpc_SyncPoint(Vector3 pt)
    {
        EnsureSetup();
        if (lineRenderer != null)
        {
            lineRenderer.positionCount++;
            lineRenderer.SetPosition(lineRenderer.positionCount - 1, pt);
        }
    }
}
