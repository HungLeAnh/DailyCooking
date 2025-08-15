using UnityEngine;

public class StoveCounterModel : BaseCounterModel
{
    private AudioSource audioSource;
    private float warningSoundTimer;
    private bool playWarningSound;

    
    public float WarningSoundTimer { get => warningSoundTimer; set => warningSoundTimer = value; }
    public bool PlayWarningSound { get => playWarningSound; set => playWarningSound = value; }
}
