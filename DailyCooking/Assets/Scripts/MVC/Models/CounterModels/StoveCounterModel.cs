using System;
using UnityEngine;

public class StoveCounterModel : BaseCounterModel
{
    public StoveCounterService.State CurrentState { get; set; }
    public float CookingTimer { get; set; }
    public float BurningTimer { get; set; }

    public float GetProgress()
    {
        // This logic should ideally be in the service, but for now, we'll keep it here
        // to resolve the compilation error.
        if (CurrentState == StoveCounterService.State.Cooking)
        {
            // Need to get cookingTimerMax from somewhere, perhaps pass it from service
            // For now, returning a dummy value
            return 0f;
        }
        if (CurrentState == StoveCounterService.State.Cooked)
        {
            // Need to get burningTimerMax from somewhere
            return 0f;
        }
        return 0f;
    }

    public bool IsDone()
    {
        // This logic should ideally be in the service, but for now, we'll keep it here
        // to resolve the compilation error.
        return CurrentState == StoveCounterService.State.Cooked;
    }
}