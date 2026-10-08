using UnityEngine;
using Fusion;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    [Header("Network Prefab")]
    public NetworkPrefabRef playerNetworkPrefab;

    public void PlayerJoined(PlayerRef player)
    {
        if (player == Runner.LocalPlayer)
        {
            
            Vector3 spawnPos = new Vector3(Random.Range(-1.5f, 1.5f), 1f, Random.Range(-1.5f, 1.5f));
            Runner.Spawn(playerNetworkPrefab, spawnPos, Quaternion.identity, player);
        }
    }
}
