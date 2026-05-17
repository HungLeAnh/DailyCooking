using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum TutorialType
{
    None,
    FirstTimePlaying,
    GameMechanic,
    BuildingTutorial,
    MenuTutorial,
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
        GameManager.Instance.OnPlayerSpawned += Instance_OnPlayerSpawned;
    }

    private void Instance_OnPlayerSpawned(object sender, EventArgs e)
    {
        if (GameManager.Instance.GameData.TutorialData.HasPlayedFirstTime == false)
        {
            ShowFirstTimeTutorial();
        }
    }

    public void ShowFirstTimeTutorial()
    {
        GameManager.Instance.HideJoyStick();
        UIHUDManager.Instance.HideAllUIElement();
        tutorialPanelDictionary[TutorialType.FirstTimePlaying].StartTutorial();
        tutorialPanelDictionary[TutorialType.FirstTimePlaying].OnTutorialClosed
            += TutorialManager_OnTutorialFirstTimePlayingClosed;
    }

    private void TutorialManager_OnTutorialFirstTimePlayingClosed(object sender, EventArgs e)
    {
        ShowGameMachanicTutorial();
    }
    public void ShowBuildingTutorial()
    {
        tutorialPanelDictionary[TutorialType.BuildingTutorial].StartTutorial();
        tutorialPanelDictionary[TutorialType.BuildingTutorial].OnTutorialClosed += TutorialManager_OnBuildingTutorialClosed;
    }

    private void TutorialManager_OnBuildingTutorialClosed(object sender, EventArgs e)
    {
        ShowMenuTutorial();
    }

    public void ShowMenuTutorial()
    {
        tutorialPanelDictionary[TutorialType.MenuTutorial].StartTutorial();
        tutorialPanelDictionary[TutorialType.MenuTutorial].OnTutorialClosed
            += TutorialManager_OnTutorialMenuClosed;
    }

    private void TutorialManager_OnTutorialMenuClosed(object sender, EventArgs e)
    {
        ShowGameMachanicTutorial();
    }    
    public void ShowGameMachanicTutorial()
    {
        UIHUDManager.Instance.HideAllUIElement();
        tutorialPanelDictionary[TutorialType.GameMechanic].StartTutorial();
        tutorialPanelDictionary[TutorialType.GameMechanic].OnTutorialClosed
            += TutorialManager_OnTutorialGameMechanicClosed;
    }

    private void TutorialManager_OnTutorialGameMechanicClosed(object sender, EventArgs e)
    {
        UIHUDManager.Instance.ShowAllUIElement();
    }
}

