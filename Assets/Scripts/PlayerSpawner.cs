using UnityEngine;
using Fusion;
using System.Threading.Tasks;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined, IPlayerLeft
{
    [Header("Network Prefab")]
    public NetworkPrefabRef playerNetworkPrefab;

    [Header("Session Settings")]
    public string sessionName = "IEEE_3DUI_Room";

    private bool hasSpawned = false; // বারবার অতিরিক্ত প্লেয়ার স্পন বন্ধ করার ফ্ল্যাগ

    private async void Start()
    {
        if (Runner == null)
        {
            var runner = GetComponent<NetworkRunner>();
            if (runner == null)
            {
                runner = gameObject.AddComponent<NetworkRunner>();
            }

            await StartSharedSession(runner);
        }
    }

    private async Task StartSharedSession(NetworkRunner runner)
    {
        runner.ProvideInput = true;

        var result = await runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = sessionName,
            SceneManager = runner.GetComponent<NetworkSceneManagerDefault>() ?? runner.gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok)
        {
            Debug.Log($"[Fusion] সফলভাবে রুমে কানেক্টেড: {sessionName}");
        }
        else
        {
            Debug.LogError($"[Fusion] কানেক্ট ফেইল: {result.ShutdownReason}");
        }
    }

    public void PlayerJoined(PlayerRef player)
    {
        // লোকাল প্লেয়ার শুধুমাত্র একবারই স্পন হবে
        if (player == Runner.LocalPlayer && !hasSpawned)
        {
            hasSpawned = true;
            Vector3 spawnPos = new Vector3(0, 0, 0);
            Runner.Spawn(playerNetworkPrefab, spawnPos, Quaternion.identity, player);
            Debug.Log($"[Fusion] লোকাল প্লেয়ার স্পন হয়েছে: {player}");
        }
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (Runner.IsServer || Runner.IsSharedModeMasterClient)
        {
            Debug.Log($"[Fusion] প্লেয়ার রুম থেকে বের হয়ে গেছে: {player}");
        }
    }
}