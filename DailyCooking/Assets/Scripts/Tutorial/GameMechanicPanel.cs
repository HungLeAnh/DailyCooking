using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

public class GameMechanicPanel : TutorialPanel
{
    [SerializeField] private VideoPlayer videoPlayer;

    protected override void Awake()
    {
        base.Awake();
        foreach(var step in tutorialSteps)
        {
            step.OnStepStart.AddListener(OnStepStart);
        }
    }

    private void OnStepStart()
    {
        videoPlayer.clip = tutorialSteps[currentStepIndex].Clip;
        videoPlayer.Prepare();
    }
}