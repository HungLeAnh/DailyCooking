using System.Collections.Generic;
using UnityEngine;
public enum EmotionType
{
    None,
    Happy,
    Sad,
    Angry,
}

public class EmotionManager : PersistentSingleton<EmotionManager>
{
    [SerializeField] private EmotionData[] emotionData;
    private Dictionary<EmotionType, Sprite> emotionDictionary;
    protected override void Awake()
    {
        base.Awake();

        if (emotionDictionary == null)
            emotionDictionary = new Dictionary<EmotionType, Sprite>();
        foreach (var data in emotionData)
        {
            if (!emotionDictionary.ContainsKey(data.EmotionType))
            {
                emotionDictionary.Add(data.EmotionType, data.EmotionSprite);
            }
            else 
            { 
                Debug.LogWarning($"EmotionManager: Duplicate emotion type {data.EmotionType} found in emotionData array.");
            }
        }
    }
    public Sprite GetEmotionSprite(EmotionType emotionType)
    {
        if (emotionDictionary.TryGetValue(emotionType,out Sprite emotionSprite))
        {
            return emotionSprite;
        }
        else
            return null;
    }
    public EmotionType GetNextEmotion(EmotionType currentEmotion)
    {
        EmotionType[] emotionTypes = (EmotionType[])System.Enum.GetValues(typeof(EmotionType));
        int currentIndex = System.Array.IndexOf(emotionTypes, currentEmotion);
        int nextIndex = (currentIndex + 1) % emotionTypes.Length;
        return emotionTypes[nextIndex];
    }
}

[System.Serializable]
public class EmotionData 
{
    [SerializeField] private EmotionType emotionType;
    [SerializeField] private Sprite emotionSprite;

    public EmotionType EmotionType { get => emotionType; set => emotionType = value; }
    public Sprite EmotionSprite { get => emotionSprite; set => emotionSprite = value; }
}
