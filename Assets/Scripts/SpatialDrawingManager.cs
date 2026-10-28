using UnityEngine;
using Fusion;

/// <summary>
/// SpatialDrawingManager - Core backend logic for spawning networked strokes.
/// Cleaned of all debug/FORCE DRAW UI elements as requested.
/// </summary>
public class SpatialDrawingManager : MonoBehaviour
{
    [SerializeField] private NetworkPrefabRef strokePrefab;
    private NetworkRunner runner;

    private NetworkRunner GetRunner()
    {
        if (runner == null || !runner.IsRunning)
        {
            runner = UnityEngine.Object.FindAnyObjectByType<NetworkRunner>();
        }
        return runner;
    }

    public NetworkObject SpawnNetworkStroke(Vector3 position, Color strokeColor)
    {
        var netRunner = GetRunner();
        if (netRunner == null || !netRunner.IsRunning || !strokePrefab.IsValid)
        {
            return null;
        }

        var spawnedObj = netRunner.Spawn(strokePrefab, position, Quaternion.identity);
        if (spawnedObj != null)
        {
            var strokeScript = spawnedObj.GetComponent<NetworkStroke>();
            if (strokeScript != null)
            {
                strokeScript.InitColor(strokeColor);
            }
        }
        return spawnedObj;
    }
}
