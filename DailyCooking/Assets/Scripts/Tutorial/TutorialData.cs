using System;
using System.Collections.Generic;

[Serializable]
public class TutorialData
{
    [System.NonSerialized]
    public Action OnTutorialDataChanged;
    private bool hasPlayedFirstTime = false;
    
    public bool HasPlayedFirstTime { get => hasPlayedFirstTime; set => hasPlayedFirstTime = value; }

    public void SetHasPlayedFirstTime(bool value)
    {
        hasPlayedFirstTime = value;
        OnTutorialDataChanged?.Invoke();
    }
}