using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;

public class BotManager : NetworkPersistentSingleton<BotManager>
{
    [SerializeField] private GameObject[] botPrefabs;
    [SerializeField] private int poolSize = 10;
    [SerializeField] private Transform spawnPosition;
    
    private int MaxBots = 10;

    private List<IObjectPool<GameObject>> pools = new List<IObjectPool<GameObject>>();
    private List<GameObject> activeBots = new List<GameObject>();

    protected override void Awake()
    {
        base.Awake();
    }

    private GameObject CreateBot(GameObject prefab)
    {
        GameObject bot = Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
        bot.GetComponent<NetworkObject>().Spawn();
        return bot;
    }

    private void OnGetBot(GameObject bot, int poolIndex)
    {
        var botController = bot.GetComponent<BotCustomerController>();
        if (botController != null)
        {
            botController.PoolIndex = poolIndex;
        }
        activeBots.Add(bot);
    }

    private void OnReleaseBot(GameObject bot)
    {
        bot.GetComponent<BotCustomerController>().IsActiveInGame.Value = false;
        activeBots.Remove(bot);
    }

    private void OnDestroyBot(GameObject bot)
    {
        if (bot.GetComponent<NetworkObject>().IsSpawned)
        {
            bot.GetComponent<NetworkObject>().Despawn();
        }
        Destroy(bot);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsHost || IsServer)
        {
            //Debug.Log("Initializing BotManager pools...");
            foreach (var prefab in botPrefabs)
            {
                int poolIndex = pools.Count;
                var pool = new ObjectPool<GameObject>(
                    createFunc: () => CreateBot(prefab),
                    actionOnGet: (bot) => OnGetBot(bot, poolIndex),
                    actionOnRelease: OnReleaseBot,
                    actionOnDestroy: OnDestroyBot,
                    defaultCapacity: poolSize / botPrefabs.Length,
                    maxSize: MaxBots
                );
                pools.Add(pool);
            }
        }
    }

    private void Start()
    {
        if(!IsHost||!IsServer) return;

        GameManager.Instance.OnStateChanged += GameManager_OnStateChanged;
    }

    private void GameManager_OnStateChanged(object sender, EventArgs e)
    {
        foreach (var bot in activeBots.ToList())
        {
            if (bot != null)
            {
                bot.GetComponent<BotCustomerController>().ResetBot();
                ReturnBotToPool(bot);
            }
        }
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
        var botsToReturn = new List<GameObject>(activeBots);
        foreach (var bot in botsToReturn)
        {
            ReturnBotToPool(bot);
        }
    }

    public void Initialize()
    {
        if (!IsHost || !IsServer || pools.Count == 0) return;

        int botsPerPool = poolSize / pools.Count;
        for (int i = 0; i < pools.Count; i++)
        {
            for (int j = 0; j < botsPerPool; j++)
            {
                var bot = pools[i].Get();
                bot.GetComponent<BotCustomerController>().PoolIndex = i;
                pools[i].Release(bot);
            }
        }
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
                    var bot = GetBot();
                    if (bot != null)
                    {
                        SpawnBot(bot);
                    }
                }
            }
            yield return new WaitForSeconds(15f);
        }
    }

    public GameObject GetBot()
    {
        if (!IsHost || !IsServer || pools.Count == 0) return null;

        int randomIndex = UnityEngine.Random.Range(0, pools.Count);
        var bot = pools[randomIndex].Get();
        if (bot != null)
        {
            bot.GetComponent<BotCustomerController>().PoolIndex = randomIndex;
        }
        return bot;
    }

    public void SpawnBot(GameObject bot)
    {
        if (!IsHost || !IsServer) return;

        var posX = new Vector3(-3f, 0f, GridBuildingSystem.Instance.GridManager.GetHeightMax() * GridBuildingSystem.Instance.GridManager.GetCellSize() + 5f);
        var posZ = new Vector3(GridBuildingSystem.Instance.GridManager.GetWidthMax() * GridBuildingSystem.Instance.GridManager.GetCellSize() + 5f, 0f, -3f);
        bot.GetComponent<BotCustomerController>().InitBot(posX, posZ);
    }

    public void ReturnBotToPool(GameObject bot)
    {
        if (!IsHost || !IsServer || bot == null) return;
        if (!activeBots.Contains(bot)) return;

        var botController = bot.GetComponent<BotCustomerController>();
        int poolIndex = botController.PoolIndex;

        if (poolIndex >= 0 && poolIndex < pools.Count)
        {
            pools[poolIndex].Release(bot);
        }
        else
        {
            Destroy(bot);
        }
    }

    public void KickAllBots()
    {
        if (!IsHost || !IsServer) return;

        var botsToReturn = new List<GameObject>(activeBots);
        foreach(var bot in botsToReturn)
        {
            var botController = bot.GetComponent<BotCustomerController>();
            botController.ResetSeat();
            botController.ResetBot();
            ReturnBotToPool(bot);
        }
    }
}