using UnityEngine;
using Fusion;

/// <summary>
/// NetworkMarkerBall - Synchronizes 3D ball radius and dynamic color across all devices in Photon Fusion 2.
/// Fully optimized for Meta Quest and PC cross-play (zero runtime allocations via MaterialPropertyBlock).
/// </summary>
public class NetworkMarkerBall : NetworkBehaviour
{
    [Networked] public Color BallColor { get; set; }
    [Networked] public float BallRadius { get; set; }

    private MaterialPropertyBlock propBlock;
    private Renderer ballRenderer;
    private int baseColorId;
    private int legacyColorId;
    private int emissionColorId;

    private void Awake()
    {
        ballRenderer = GetComponentInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();
        baseColorId = Shader.PropertyToID("_BaseColor");
        legacyColorId = Shader.PropertyToID("_Color");
        emissionColorId = Shader.PropertyToID("_EmissionColor");
    }

    public void Init(Color color, float radius)
    {
        BallColor = color;
        BallRadius = radius;
        ApplyVisuals(color, radius);
    }

    public override void Render()
    {
        ApplyVisuals(BallColor, BallRadius);
    }

    private void ApplyVisuals(Color col, float rad)
    {
        if (rad > 0.005f)
        {
            transform.localScale = Vector3.one * rad;
        }

        if (ballRenderer != null)
        {
            if (propBlock == null) propBlock = new MaterialPropertyBlock();
            ballRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor(baseColorId, col);
            propBlock.SetColor(legacyColorId, col);
            propBlock.SetColor(emissionColorId, col * 1.25f);
            ballRenderer.SetPropertyBlock(propBlock);
        }
    }
}
