using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class BotManager : NetworkPersistentSingleton<BotManager>
{
    [SerializeField] private GameObject botPrefab;
    [SerializeField] private int poolSize = 10;
    [SerializeField] private Vector3 spawnPosition;

    private NetworkList<NetworkObjectReference> botPool = new NetworkList<NetworkObjectReference>();
    private List<GameObject> activeBots;

    public List<GameObject> ActiveBots { get => activeBots; set => activeBots = value; }

    protected override void Awake()
    {
        base.Awake();
        botPool = new NetworkList<NetworkObjectReference>();
        ActiveBots = new List<GameObject>();
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    private void Start()
    {
        if(!IsHost||!IsServer) return;

        GameManager.Instance.GameData.TutorialData.OnTutorialDataChanged += OnTutorialDataChanged;
        GameManager.Instance.OnStateChanged += GameManager_OnStateChanged;
    }

    private void GameManager_OnStateChanged(object sender, EventArgs e)
    {
        foreach (var bot in ActiveBots)
        {
            if (bot != null)
            {
                bot.GetComponent<BotCustomerController>().ResetBot();
                ReturnBotToPool(bot);
            }
        }
    }

    private void OnTutorialDataChanged()
    {
        StartSpawnBot();
    }

    private void OnDestroy()
    {
        botPool.Clear();
        if(GameManager.Instance != null)
            GameManager.Instance.GameData.TutorialData.OnTutorialDataChanged -= OnTutorialDataChanged;

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
            GameObject bot = Instantiate(botPrefab,spawnPosition, Quaternion.identity, transform);
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
            if(KitchenGameManager.Instance.CurrentState == KitchenGameManager.State.Open)
            {
                //Debug.Log("Spawning Bot");
                var bot = GetBot();
                SpawnBot(bot);

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
        var newBot = Instantiate(botPrefab, spawnPosition, Quaternion.identity, transform);
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
        SpawnBotClientRpc(targetRef);
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void SpawnBotClientRpc(NetworkObjectReference targetRef)
    {
        if (targetRef.TryGet(out NetworkObject networkObject))
        {
            GameObject bot = networkObject.gameObject;
            bot.transform.position = spawnPosition;
    
            if(IsHost||IsServer)
                bot.GetComponent<BotCustomerController>().IsActiveInGame.Value = true;
            
            bot.GetComponent<BotCustomerController>().InitBot();
            ActiveBots.Add(bot);
        }
    }

    public void ReturnBotToPool(GameObject bot)
    {
        ReturnBotToPoolServerRpc(bot);
    }

    [Rpc(SendTo.Server)]
    private void ReturnBotToPoolServerRpc(NetworkObjectReference targetRef)
    {
        ReturnBotToPoolClientRpc(targetRef);
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void ReturnBotToPoolClientRpc(NetworkObjectReference targetRef)
    {
        if (targetRef.TryGet(out NetworkObject networkObject))
        {
            GameObject bot = networkObject.gameObject;
            if(IsHost||IsServer)
                bot.GetComponent<BotCustomerController>().IsActiveInGame.Value = false;
            ActiveBots.Remove(bot);
        }
    }
    public void KickAllBots()
    {
        foreach(var bot in activeBots)
        {
            var botController = bot.GetComponent<BotCustomerController>();
            botController.ResetSeat();
            bot.GetComponent<BotCustomerController>().ResetBot();
            bot.GetComponent<BotCustomerController>().IsActiveInGame.Value = false;
        }
    }
}