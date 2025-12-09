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
        GameManager.Instance.GameData.PlayerStats.OnResourceChange += OnResourceChange;
        GameManager.Instance.GameData.PlayerStats.OnLevelChange += OnLevelChange;
        GameManager.Instance.GameData.PlayerStats.OnExpChange += OnExpChange;

        if(gemText!= null)
            gemText.text = GameManager.Instance.GameData.PlayerStats.playerData.Gems.ToString();
        if(coinText != null)
            coinText.text = GameManager.Instance.GameData.PlayerStats.playerData.Coins.ToString();
        if(levelText != null)
            levelText.text = GameManager.Instance.GameData.PlayerStats.playerData.Level.ToString();
        if(expText != null)
            expText.text = GameManager.Instance.GameData.PlayerStats.playerData.Exp.ToString() + "/" + GameManager.Instance.GameData.PlayerStats.playerData.Level * EXP_PER_LEVEL_MULTIPLIER;
        if(expSlider != null)
            expSlider.value = (float)GameManager.Instance.GameData.PlayerStats.playerData.Exp / (GameManager.Instance.GameData.PlayerStats.playerData.Level * EXP_PER_LEVEL_MULTIPLIER);

    }
    private void OnDestroy()
    {
        if(GameManager.Instance == null || GameManager.Instance.GameData == null)
            return;
        GameManager.Instance.GameData.PlayerStats.OnResourceChange -= OnResourceChange;
        GameManager.Instance.GameData.PlayerStats.OnLevelChange -= OnLevelChange;
        GameManager.Instance.GameData.PlayerStats.OnExpChange -= OnExpChange;
    }
    private void OnExpChange()
    {
        if (expText != null)
            expText.text = GameManager.Instance.GameData.PlayerStats.playerData.Exp.ToString() + "/" + GameManager.Instance.GameData.PlayerStats.playerData.Level * EXP_PER_LEVEL_MULTIPLIER;
        
        if (expSlider != null)
            expSlider.value = (float)GameManager.Instance.GameData.PlayerStats.playerData.Exp / (GameManager.Instance.GameData.PlayerStats.playerData.Level * EXP_PER_LEVEL_MULTIPLIER);
    }

    private void OnLevelChange()
    {        
        if (levelText != null)
            levelText.text = GameManager.Instance.GameData.PlayerStats.playerData.Level.ToString();
    }  

    private void OnResourceChange()
    {
        UpdateResources(GameManager.Instance.GameData.PlayerStats.playerData.Coins,
            GameManager.Instance.GameData.PlayerStats.playerData.Gems);
    }

    public void UpdateResources(int coin, int gem)
    {
        if (coinText != null)
            coinText.text = coin.ToString();
        if (gemText != null)
            gemText.text = gem.ToString();

    }
}