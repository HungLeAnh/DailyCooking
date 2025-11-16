using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotManager : SimpleSingleton<BotManager>
{
    

    [SerializeField] private GameObject botPrefab;
    [SerializeField] private int poolSize = 10;

    private List<GameObject> botPool;

    

    protected override void Awake()
    {
        base.Awake();
        botPool = new List<GameObject>();
    }

    private void Start()
    {
        KitchenGameManager.Instance.OnStateChanged += KitchenGameManager_OnStateChanged;
    }
    private void OnDestroy()
    {
        KitchenGameManager.Instance.OnStateChanged -= KitchenGameManager_OnStateChanged;
        botPool.Clear();
    }

    private void KitchenGameManager_OnStateChanged(object sender, EventArgs e)
    {
        if (KitchenGameManager.Instance.IsGamePlaying())
        {
            StartCoroutine(SpawnBotRoutine());

        }
        else
        {
            StopCoroutine(SpawnBotRoutine());
            foreach (var bot in botPool)
            {
                bot.SetActive(false);
            }
        }
    }

    public void Initialize()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject bot = Instantiate(botPrefab, transform.position, Quaternion.identity, transform);
            bot.SetActive(false);
            botPool.Add(bot);
        }
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