using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum TutorialType
{
    None,
    FirstTimePlaying,
    GameMechanic
}


public class TutorialManager : PersistentSingleton<TutorialManager>
{
    [SerializeField] private List<TutorialPanel> tutorialPanelList;

    private Dictionary<TutorialType, TutorialPanel> tutorialPanelDictionary = new Dictionary<TutorialType, TutorialPanel>();

    private TutorialPanel currentPanel;
    private int currentPanelIndex = -1;

    protected override void Awake()
    {
        base.Awake();
        foreach (var panel in tutorialPanelList)
        {
            if (!tutorialPanelDictionary.ContainsKey(panel.GetPanelType()))
            {
                tutorialPanelDictionary.Add(panel.GetPanelType(), panel);
                panel.gameObject.SetActive(false);
            }
        }
    }

    private void Start()
    {
        GameManager.Instance.OnStateChange += GameManager_OnStateChange;
    }

    private void GameManager_OnStateChange(object sender, EventArgs e)
    {
        if(GameManager.Instance.GameState == GameState.InGame)
        {
            if (!GameManager.Instance.GameData.tutorialData.HasPlayedFirstTime)
            {
                ShowFirstTimeTutorial();
            }

        }
    }

    private void ShowFirstTimeTutorial()
    {
        tutorialPanelDictionary[TutorialType.FirstTimePlaying].StartTutorial();
        tutorialPanelDictionary[TutorialType.FirstTimePlaying].OnTutorialClosed += TutorialManager_OnTutorialClosed;

    }

    private void TutorialManager_OnTutorialClosed(object sender, EventArgs e)
    {
        TutorialPanel panel = (TutorialPanel)sender;
        switch(panel.GetPanelType()){
            case TutorialType.FirstTimePlaying:
                GridBuildingSystem.Instance.UnlockGrid();
                GameManager.Instance.GameData.tutorialData.HasPlayedFirstTime = true;
                GameManager.Instance.SaveGame();
                break;
            case TutorialType.GameMechanic:
                break;
        }
    }
}

