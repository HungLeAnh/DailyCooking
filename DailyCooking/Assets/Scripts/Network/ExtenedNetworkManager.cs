using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode.Transports.SinglePlayer;
using UnityEngine;

public class ExtendedNetworkManager : NetworkManager
{
    private UnityTransport m_UnityTransport;
    private SinglePlayerTransport m_SinglePlayerTransport;

    private void Awake()
    {
        m_UnityTransport = GetComponent<UnityTransport>();
        m_SinglePlayerTransport = GetComponent<SinglePlayerTransport>();
    }

    public void StartSinglePlayer()
    {
        // Use the single player transport when starting a single player session.
        NetworkConfig.NetworkTransport = m_SinglePlayerTransport;
        if (!StartHost())
        {
            NetworkLog.LogError("Failed to start single player session!");
        }
    }

    public void StartHostedSession()
    {
        // Use the network transport when starting a multiplayer session.
        NetworkConfig.NetworkTransport = m_UnityTransport;
        if (!StartHost())
        {
            NetworkLog.LogError("Failed to start hosted session!");
        }
    }
}