using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotManager : SimpleSingleton<BotManager>
{
    

    [SerializeField] private GameObject botPrefab;
    [SerializeField] private int poolSize = 10;

    private List<GameObject> botPool;

    

    private void Awake()
    {
        botPool = new List<GameObject>();
    }

    private void Start()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject bot = Instantiate(botPrefab, transform.position, Quaternion.identity, transform);
            bot.SetActive(false);
            botPool.Add(bot);
        }

    }
    public void Initialize()
    {
        StartCoroutine(SpawnBotRoutine());
    }
    private IEnumerator SpawnBotRoutine()
    {
        while (true)
        {
            GetBot();
            yield return new WaitForSeconds(20f);
        }
    }

    public GameObject GetBot()
    {
        foreach (var bot in botPool)
        {
            if (!bot.gameObject.activeInHierarchy)
            {
                bot.gameObject.SetActive(true);
                return bot.gameObject;
            }
        }

        // If no inactive bot is found, create a new one
        var newBot = Instantiate(botPrefab, transform.position, Quaternion.identity, transform);
        botPool.Add(newBot);
        return newBot.gameObject;
    }

    public void ReturnBotToPool(GameObject bot)
    {
        bot.SetActive(false);
    }
}