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
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        if(arg0.buildIndex != Loader.Scene.GameScene.GetHashCode())
            return;
        if (GameManager.Instance.GameData.TutorialData.HasPlayedFirstTime == false)
        {
            ShowFirstTimeTutorial();
        }
    }

    public void ShowFirstTimeTutorial()
    {
        UIHUDManager.Instance.HideAllUIElement();
        tutorialPanelDictionary[TutorialType.FirstTimePlaying].StartTutorial();
        tutorialPanelDictionary[TutorialType.FirstTimePlaying].OnTutorialClosed
            += TutorialManager_OnTutorialFirstTimePlayingClosed;
    }

    private void TutorialManager_OnTutorialFirstTimePlayingClosed(object sender, EventArgs e)
    {
        TutorialManager.Instance.ShowGameMachanicTutorial();
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

