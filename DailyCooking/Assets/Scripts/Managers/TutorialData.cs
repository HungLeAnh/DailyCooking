using System;
using System.Collections.Generic;

[Serializable]
public class TutorialData
{
    private bool hasPlayedFirstTime = false;
    
    public bool HasPlayedFirstTime { get => hasPlayedFirstTime; set => hasPlayedFirstTime = value; }

}