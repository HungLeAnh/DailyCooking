using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotManager : PersistentSingleton<BotManager>
{
    

    [SerializeField] private GameObject botPrefab;
    [SerializeField] private int poolSize = 10;
    [SerializeField] private Vector3 spawnPosition;

    private List<GameObject> botPool;

    

    protected override void Awake()
    {
        base.Awake();
        botPool = new List<GameObject>();
    }

    private void OnDestroy()
    {
        botPool.Clear();
    }
    public void StartSpawnBot()
    {
        StartCoroutine(WaitForSecond(10, () => {
            StartCoroutine(SpawnBotRoutine());
        }));
    }
    public void StopSpawnBot()
    {
        StopCoroutine(SpawnBotRoutine());
        foreach (var bot in botPool)
        {
            bot.SetActive(false);
        }
        
    }

    public void Initialize()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject bot = Instantiate(botPrefab,spawnPosition, Quaternion.identity, transform);
            bot.SetActive(false);
            botPool.Add(bot);
        }
    }
    private IEnumerator WaitForSecond(int seconds, Action action)
    {
        yield return new WaitForSeconds(seconds);
        action?.Invoke();
    }
    private IEnumerator SpawnBotRoutine()
    {
        while (true)
        {
            var bot = GetBot();
            bot.GetComponent<BotCustomerController>().InitBot();
            yield return new WaitForSeconds(20f);
        }
    }

    public GameObject GetBot()
    {
        foreach (var bot in botPool)
        {
            if (!bot.gameObject.activeInHierarchy)
            {
                bot.transform.position = spawnPosition;
                bot.gameObject.SetActive(true);
                return bot.gameObject;
            }
        }

        // If no inactive bot is found, create a new one
        var newBot = Instantiate(botPrefab, spawnPosition, Quaternion.identity, transform);
        botPool.Add(newBot);
        return newBot.gameObject;
    }

    public void ReturnBotToPool(GameObject bot)
    {
        bot.SetActive(false);
    }
}