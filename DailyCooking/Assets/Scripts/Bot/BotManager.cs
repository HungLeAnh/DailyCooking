using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEngine;
using UnityEngine.UIElements;

public class BotManager : NetworkPersistentSingleton<BotManager>
{
    [SerializeField] private GameObject botPrefab;
    [SerializeField] private int poolSize = 10;
    [SerializeField] private Vector3 spawnPosition;
    
    private int MaxBots = 10;

    private NetworkList<NetworkObjectReference> botPool = new NetworkList<NetworkObjectReference>();
    private NetworkList<NetworkObjectReference> activeBots = new NetworkList<NetworkObjectReference>();

    protected override void Awake()
    {
        base.Awake();
        botPool = new NetworkList<NetworkObjectReference>();
        activeBots = new NetworkList<NetworkObjectReference>();
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    private void Start()
    {
        if(!IsHost||!IsServer) return;

        GameManager.Instance.OnStateChanged += GameManager_OnStateChanged;
    }

    private void GameManager_OnStateChanged(object sender, EventArgs e)
    {
        foreach (var bot in activeBots)
        {
            if(bot.TryGet(out NetworkObject networkObject))
            {
                if (networkObject.gameObject != null)
                {
                    networkObject.GetComponent<BotCustomerController>().ResetBot();
                    ReturnBotToPool(bot);
                }

            }
        }
    }

    private void OnDestroy()
    {
        botPool.Clear();
    }
    public void StartSpawnBot()
    {
        if (!IsHost || !IsServer) return;

        StartCoroutine(WaitForSecond(10, () => {
            StartCoroutine(SpawnBotRoutine());
        }));
    }
    public void StopSpawnBot()
    {
        if (!IsHost || !IsServer) return;

        StopCoroutine(SpawnBotRoutine());
        foreach (var bot in botPool)
        {
            if(bot.TryGet(out NetworkObject networkObject))
            {
                GameObject botGameObject = networkObject.gameObject;
                ReturnBotToPool(botGameObject);
            }
        }

    }

    public void Initialize()
    {
        if (!IsHost || !IsServer) return;

        for (int i = 0; i < poolSize; i++)
        {
            GameObject bot = Instantiate(botPrefab,Vector3.zero, Quaternion.identity, transform);
            bot.GetComponent<NetworkObject>().Spawn();
            AddBotToPool(bot);

        }
    }

    public void AddBotToPool(GameObject bot)
    {
        bot.GetComponent<BotCustomerController>().IsActiveInGame.Value = false;
        botPool.Add(bot);
        
    }

    private IEnumerator WaitForSecond(int seconds, Action action)
    {
        yield return new WaitForSeconds(seconds);
        action?.Invoke();
    }
    private IEnumerator SpawnBotRoutine()
    {
        while (GameManager.Instance.State is InGameState)
        {
            if (activeBots.Count < MaxBots)
            {
                if (KitchenGameManager.Instance.CurrentState == KitchenGameManager.State.Open)
                {
                    //Debug.Log("Spawning Bot");
                    var bot = GetBot();
                    SpawnBot(bot);

                }
            }
            yield return new WaitForSeconds(15f);
        }
    }

    public GameObject GetBot()
    {
        if (!IsHost || !IsServer) return null;

        foreach (var bot in botPool)
        {
            if(bot.TryGet(out NetworkObject networkObject))
            {
                var botController = networkObject.GetComponent<BotCustomerController>();
                if (!botController.IsActiveInGame.Value)
                {
                    return networkObject.gameObject;
                }

            }
        }

        // If no inactive bot is found, create a new one
        var newBot = Instantiate(botPrefab, Vector3.zero, Quaternion.identity, transform);
        newBot.GetComponent<NetworkObject>().Spawn();
        AddBotToPool(newBot);
        return newBot.gameObject;
    }
    public void SpawnBot(GameObject bot)
    {
        SpawnBotServerRpc(bot);
    }

    [Rpc(SendTo.Server)]
    private void SpawnBotServerRpc(NetworkObjectReference targetRef)
    {
        if (targetRef.TryGet(out NetworkObject networkObject))
        {
            GameObject bot = networkObject.gameObject;
            if (IsHost || IsServer)
            {
                var posX = new Vector3(-3f, 0f, GridBuildingSystem.Instance.GridManager.GetHeightMax() * GridBuildingSystem.Instance.GridManager.GetCellSize() + 5f);
                var posZ = new Vector3(GridBuildingSystem.Instance.GridManager.GetWidthMax() * GridBuildingSystem.Instance.GridManager.GetCellSize() + 5f, 0f, -3f);
                bot.GetComponent<BotCustomerController>().InitBot(posX, posZ);
                bot.GetComponent<BotCustomerController>().IsActiveInGame.Value = true;
                activeBots.Add(bot);
            }
        }
    }

    public void ReturnBotToPool(GameObject bot)
    {
        ReturnBotToPoolServerRpc(bot);
    }

    [Rpc(SendTo.Server)]
    private void ReturnBotToPoolServerRpc(NetworkObjectReference targetRef)
    {
        if (targetRef.TryGet(out NetworkObject networkObject))
        {
            GameObject bot = networkObject.gameObject;
            bot.GetComponent<BotCustomerController>().IsActiveInGame.Value = false;
            activeBots.Remove(bot);            
        }
    }
    public void KickAllBots()
    {
        foreach(var bot in activeBots)
        {
            if(bot.TryGet(out NetworkObject networkObject))
            {
                var botController = networkObject.GetComponent<BotCustomerController>();
                botController.ResetSeat();
                networkObject.GetComponent<BotCustomerController>().ResetBot();
                networkObject.GetComponent<BotCustomerController>().IsActiveInGame.Value = false;

            }
        }
    }
}