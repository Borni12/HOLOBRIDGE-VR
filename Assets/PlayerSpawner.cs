using UnityEngine;
using Fusion;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    public NetworkPrefabRef playerNetworkPrefab;

    public void PlayerJoined(PlayerRef player)
    {
        if (player == Runner.LocalPlayer)
        {
            Runner.Spawn(playerNetworkPrefab, new Vector3(0, 1, 0), Quaternion.identity, player);
        }
    }
}