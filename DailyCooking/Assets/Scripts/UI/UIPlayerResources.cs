using Observer;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIPlayerResources : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI coinText;

    private void Start()
    {
        GameManager.Instance.GameData.OnResourceChange += OnResourceChange;
        coinText.text = GameManager.Instance.GameData.playerData.coins.ToString();
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
