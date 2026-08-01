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

    private void OnGetBot(GameObject bot)
    {
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
                var pool = new ObjectPool<GameObject>(
                    createFunc: () => CreateBot(prefab),
                    actionOnGet: OnGetBot,
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
        foreach (var pool in pools)
        {
            for (int i = 0; i < botsPerPool; i++)
            {
                var bot = pool.Get();
                pool.Release(bot);
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
        //Debug.Log("GetBot called. pool bots: " + pools.Count);
        if (!IsHost || !IsServer || pools.Count == 0) return null;

        int randomIndex = UnityEngine.Random.Range(0, pools.Count);
        return pools[randomIndex].Get();
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

        string originalName = bot.name.Replace("(Clone)", "").Trim();
        foreach (var prefab in botPrefabs)
        {
            if (prefab.name == originalName)
            {
                int index = Array.IndexOf(botPrefabs, prefab);
                pools[index].Release(bot);
                return;
            }
        }
        
        Destroy(bot);
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