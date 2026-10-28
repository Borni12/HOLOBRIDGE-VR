using UnityEngine;
using Fusion;

/// <summary>
/// Spawns networked VR players when joining the Photon Fusion session.
/// </summary>
public class PlayerSpawner : SimulationBehaviour, IPlayerJoined, IPlayerLeft
{
    [SerializeField] private NetworkPrefabRef playerNetworkPrefab;

    public void PlayerJoined(PlayerRef player)
    {
        if (Runner.IsServer || Runner.IsSharedModeMasterClient)
        {
            // Spawn network avatar at random spawn offset
            Vector3 spawnPoint = new Vector3(Random.Range(-1.5f, 1.5f), 0, Random.Range(-1f, 1f));
            Runner.Spawn(playerNetworkPrefab, spawnPoint, Quaternion.identity, player);
            Debug.Log($"[Network] Player {player.PlayerId} joined and spawned successfully.");
        }
    }

    public void PlayerLeft(PlayerRef player)
    {
        Debug.Log($"[Network] Player {player.PlayerId} disconnected.");
    }
}
