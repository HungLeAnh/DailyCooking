using Observer;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPlayerResources : MonoBehaviour
{
    private const int EXP_PER_LEVEL_MULTIPLIER = 100;

    [SerializeField] private TextMeshProUGUI gemText;
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI expText;
    [SerializeField] private Slider expSlider;

    private void Start()
    {
        if(MultiplayerManager.Instance.IsHost|| MultiplayerManager.Instance.IsServer)
        {
                Initialize();
        }
        else
        {
            MultiplayerManager.Instance.OnDataSyncToNewClient += MultiplayerManager_OnDataSyncToNewClient;
        }
        
    }
    private void MultiplayerManager_OnDataSyncToNewClient(object sender, EventArgs e)
    {
        Initialize();
    }
    private void Initialize()
    {
        if (GameManager.Instance.GameData == null)
        {
            Debug.LogError("GameData is null in UIPlayerResources");
            return;
        }
        GameManager.Instance.GameData.RestaurantData.OnResourceChange -= OnResourceChange;
        GameManager.Instance.GameData.RestaurantData.OnLevelChange -= OnLevelChange;
        GameManager.Instance.GameData.RestaurantData.OnExpChange -= OnExpChange;
        GameManager.Instance.GameData.RestaurantData.OnResourceChange += OnResourceChange;
        GameManager.Instance.GameData.RestaurantData.OnLevelChange += OnLevelChange;
        GameManager.Instance.GameData.RestaurantData.OnExpChange += OnExpChange;

        if (gemText != null)
            gemText.text = GameManager.Instance.GameData.RestaurantData.Gems.ToString();
        if (coinText != null)
            coinText.text = GameManager.Instance.GameData.RestaurantData.Coins.ToString();
        if (levelText != null)
            levelText.text = GameManager.Instance.GameData.RestaurantData.Level.ToString();
        if (expText != null)
            expText.text = GameManager.Instance.GameData.RestaurantData.Exp.ToString() + "/" + GameManager.Instance.GameData.RestaurantData.Level * EXP_PER_LEVEL_MULTIPLIER;
        if (expSlider != null)
            expSlider.value = (float)GameManager.Instance.GameData.RestaurantData.Exp / (GameManager.Instance.GameData.RestaurantData.Level * EXP_PER_LEVEL_MULTIPLIER);

    }
    private void OnDestroy()
    {
        if (MultiplayerManager.Instance != null)
        {
            MultiplayerManager.Instance.OnDataSyncToNewClient -= MultiplayerManager_OnDataSyncToNewClient;
        }
        if(GameManager.Instance == null || GameManager.Instance.GameData == null)
            return;
        GameManager.Instance.GameData.RestaurantData.OnResourceChange -= OnResourceChange;
        GameManager.Instance.GameData.RestaurantData.OnLevelChange -= OnLevelChange;
        GameManager.Instance.GameData.RestaurantData.OnExpChange -= OnExpChange;
    }
    private void OnExpChange()
    {
        if (expText != null)
            expText.text = GameManager.Instance.GameData.RestaurantData.Exp.ToString() + "/" + GameManager.Instance.GameData.RestaurantData.Level * EXP_PER_LEVEL_MULTIPLIER;
        
        if (expSlider != null)
            expSlider.value = (float)GameManager.Instance.GameData.RestaurantData.Exp / (GameManager.Instance.GameData.RestaurantData.Level * EXP_PER_LEVEL_MULTIPLIER);
    }

    private void OnLevelChange()
    {        
        if (levelText != null)
            levelText.text = GameManager.Instance.GameData.RestaurantData.Level.ToString();
    }  

    private void OnResourceChange()
    {
        UpdateResources(GameManager.Instance.GameData.RestaurantData.Coins,
            GameManager.Instance.GameData.RestaurantData.Gems);
    }

    public void UpdateResources(int coin, int gem)
    {
        if (coinText != null)
            coinText.text = coin.ToString();
        if (gemText != null)
            gemText.text = gem.ToString();

    }
}