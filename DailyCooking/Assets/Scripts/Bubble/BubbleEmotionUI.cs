using System;
using UnityEngine;
using UnityEngine.UI;

public class BubbleEmotionUI: MonoBehaviour
{
    [SerializeField] private BotCustomerController botCustomerController;
    [SerializeField] private Image imageEmotion;
    [SerializeField] private Image imageEmotionClock;

    private void Start()
    {
        botCustomerController.OnEmotionChanged += BotCustomerController_OnEmotionChanged;
        botCustomerController.OnClockTimerChanged += BotCustomerController_OnClockTimerChanged;
    }

    private void OnDestroy()
    {
        if (botCustomerController == null)
            return;
        botCustomerController.OnEmotionChanged -= BotCustomerController_OnEmotionChanged;
        botCustomerController.OnClockTimerChanged -= BotCustomerController_OnClockTimerChanged;
    }
    private void BotCustomerController_OnClockTimerChanged(float fillAmount)
    {
        imageEmotionClock.fillAmount = fillAmount;
    }

    private void BotCustomerController_OnEmotionChanged(EmotionType type)
    {
        imageEmotion.sprite = EmotionManager.Instance.GetEmotionSprite(type);
        imageEmotionClock.fillAmount = 1f;
    }
}
