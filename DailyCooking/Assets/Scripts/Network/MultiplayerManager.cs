using System;
using Unity.Netcode;
using Unity.Netcode.Transports.SinglePlayer;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class MultiplayerManager : PersistentSingleton<MultiplayerManager>
{
    [SerializeField] private UnityTransport unityTransport;
    [SerializeField] private SinglePlayerTransport singlePlayerTransport;
    [SerializeField] private NetworkManager networkManager;
    public void StartSinglePlayer()
    {
        // Use the single player transport when starting a single player session.
        networkManager.NetworkConfig.NetworkTransport = singlePlayerTransport;
        if (!networkManager.StartHost())
        {
            NetworkLog.LogError("Failed to start single player session!");
        }
    }

    public void StartHostedSession()
    {
        try
        {
            // Use the network transport when starting a multiplayer session.
            networkManager.NetworkConfig.NetworkTransport = unityTransport;
            if (!networkManager.StartHost())
            {
                NetworkLog.LogError("Failed to start hosted session!");
            }
            Debug.Log("NetworkManager started as Host.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create session: {e.Message}");
        }
    }
    public void StartClientSession()
    {
        try
        {
            // Use the network transport when starting a multiplayer session.
            networkManager.NetworkConfig.NetworkTransport = unityTransport;
            if (!networkManager.StartClient())
            {
                NetworkLog.LogError("Failed to start client session!");
            }
            Debug.Log("NetworkManager started as Client.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create session: {e.Message}");
        }
    }
}