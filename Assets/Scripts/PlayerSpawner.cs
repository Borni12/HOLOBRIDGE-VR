
using UnityEngine;
using Fusion;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined, IPlayerLeft
{
    [SerializeField] private NetworkPrefabRef playerNetworkPrefab;

    public void PlayerJoined(PlayerRef player)
    {
        if (Runner.IsServer || Runner.IsSharedModeMasterClient)
        {
            // নতুন প্লেয়ারকে কিছুটা রেন্ডম দূরত্বে স্পন করা যাতে একে অপরের ওপর না পড়ে
            Vector3 spawnPoint = new Vector3(Random.Range(-1.5f, 1.5f), 0, Random.Range(-1f, 1f));
            Runner.Spawn(playerNetworkPrefab, spawnPoint, Quaternion.identity, player);
            Debug.Log($"[Network] প্লেয়ার {player.PlayerId} যুক্ত হয়েছে এবং অবতার স্পন করা হয়েছে।");
        }
    }

    public void PlayerLeft(PlayerRef player)
    {
        // প্লেয়ার রুম থেকে বের হয়ে গেলে হ্যান্ডলিং
    }
}