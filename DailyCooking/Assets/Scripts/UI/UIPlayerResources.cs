using Observer;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPlayerResources : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI expText;
    [SerializeField] private Slider expSlider;

    private void Start()
    {
        GameManager.Instance.GameData.OnResourceChange += OnResourceChange;
        GameManager.Instance.GameData.OnLevelChange += OnLevelChange;
        GameManager.Instance.GameData.OnExpChange += OnExpChange;

        coinText.text = GameManager.Instance.GameData.playerData.coins.ToString();
        levelText.text = GameManager.Instance.GameData.playerData.level.ToString();
        expText.text = GameManager.Instance.GameData.playerData.exp.ToString() + "/" + GameManager.Instance.GameData.playerData.level * 100;
        expSlider.value = (float)GameManager.Instance.GameData.playerData.exp / (GameManager.Instance.GameData.playerData.level * 100);

    }
    private void OnDestroy()
    {
        if(GameManager.Instance == null || GameManager.Instance.GameData == null)
            return;
        GameManager.Instance.GameData.OnResourceChange -= OnResourceChange;
        GameManager.Instance.GameData.OnLevelChange -= OnLevelChange;
        GameManager.Instance.GameData.OnExpChange -= OnExpChange;
    }
    private void OnExpChange()
    {
        if (expText != null)
        {
            expText.text = GameManager.Instance.GameData.playerData.exp.ToString() + "/" + GameManager.Instance.GameData.playerData.level * 100;
            expSlider.value = (float)GameManager.Instance.GameData.playerData.exp / (GameManager.Instance.GameData.playerData.level * 100);
        }
    }

    private void OnLevelChange()
    {        
        if (levelText != null)
            levelText.text = GameManager.Instance.GameData.playerData.level.ToString();
    }  

    private void OnResourceChange()
    {
        UpdateResources(GameManager.Instance.GameData.playerData.coins);
    }

    public void UpdateResources(int coin)
    {
        if (coinText != null)
            coinText.text = coin.ToString();
        
    }
}
