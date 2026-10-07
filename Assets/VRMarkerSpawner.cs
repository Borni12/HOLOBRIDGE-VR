
using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;

public class VRMarkerSpawner : MonoBehaviour
{
    [Header("Network Prefab")]
    public NetworkPrefabRef markerNetworkPrefab;

    [Header("Input Action")]
    public InputActionProperty spawnAction;

    [Header("Ray Settings")]
    public Transform rayOriginTransform;
    public LayerMask hitLayers = ~0;
    public float maxRayDistance = 30.0f;

    void OnEnable()
    {
        if (spawnAction.action != null)
        {
            spawnAction.action.Enable();
        }
    }

    void OnDisable()
    {
        if (spawnAction.action != null)
        {
            spawnAction.action.Disable();
        }
    }

    void Update()
    {
        if (spawnAction.action != null && spawnAction.action.WasPressedThisFrame())
        {
            SpawnMarkerFromVR();
        }
    }

    void SpawnMarkerFromVR()
    {
        Transform origin = rayOriginTransform != null ? rayOriginTransform : transform;
        Ray ray = new Ray(origin.position, origin.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxRayDistance, hitLayers))
        {
            NetworkRunner runner = FindObjectOfType<NetworkRunner>();
            if (runner != null && runner.IsRunning && markerNetworkPrefab.IsValid)
            {
                runner.Spawn(markerNetworkPrefab, hit.point + hit.normal * 0.05f, Quaternion.LookRotation(hit.normal));
            }
        }
    }
}