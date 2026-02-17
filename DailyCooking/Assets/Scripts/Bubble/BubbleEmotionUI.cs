using System;
using UnityEngine;
using UnityEngine.UI;

public class BubbleEmotionUI: MonoBehaviour
{
    public Action<EmotionType> OnEmotionEnd;
    public Action<EmotionType> OnEmotionChanged;
    [SerializeField] private Image imageEmotion;
    [SerializeField] private Image imageEmotionClock;

    private float clockTimer = 0f;
    private float clockTimerMax = GameDefine.EMOTION_DURATION;
    private EmotionType currentEmotion = EmotionType.None;

    public void StartEmotion()
    {
        imageEmotion.sprite = EmotionManager.Instance.GetEmotionSprite(EmotionType.Happy);
        clockTimer = 0f;
        imageEmotionClock.fillAmount = 1f;
        currentEmotion = EmotionType.Happy;
    }
    public void StopEmotion()
    {
        currentEmotion = EmotionType.None;
        imageEmotion.sprite = null;
        imageEmotionClock.fillAmount = 0f;
    }

    private void Update()
    {
        if (clockTimer < clockTimerMax)
        {
            clockTimer += Time.deltaTime;
            imageEmotionClock.fillAmount = (clockTimerMax - clockTimer) / clockTimerMax;
            if(imageEmotionClock.fillAmount <= 0)
            {
                SetNextEmotion();
            }
        }
    }
    private void SetNextEmotion()
    {
        EmotionType nextEmotion = EmotionManager.Instance.GetNextEmotion(currentEmotion);
        if(nextEmotion == EmotionType.None && currentEmotion != EmotionType.None)
        {
            currentEmotion = EmotionType.None;
            OnEmotionEnd?.Invoke(currentEmotion);
            return;
        }
        if(currentEmotion == EmotionType.None)
        {
            return;
        }

        imageEmotion.sprite = EmotionManager.Instance.GetEmotionSprite(nextEmotion);
        clockTimer = 0f;
        imageEmotionClock.fillAmount = 1f;
        currentEmotion = nextEmotion;
        OnEmotionChanged?.Invoke(currentEmotion);
    }
}
