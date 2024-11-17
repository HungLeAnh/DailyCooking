using System;
using UnityEngine;

public abstract class CookingTool: MonoBehaviour
{
    public event EventHandler<OnStageChangeEventArgs> OnStageChanged;

    public class OnStageChangeEventArgs : EventArgs
    {
        public State state;
    }
    public enum State
    {
        Idle,
        Cooking,
        Cooked,
        Burned,
    }

    private State _state;
    private float _cookingTimer;
    private float _burningTimer;
    private float _cookingTimeMax;
    private float _burningTimeMax;

    public State CurrentState { get => _state; set => _state = value; }
    public float CookingTimer { get => _cookingTimer; set => _cookingTimer = value; }
    public float BurningTimer { get => _burningTimer; set => _burningTimer = value; }
    public float CookingTimeMax { get => _cookingTimeMax; set => _cookingTimeMax = value; }
    public float BurningTimeMax { get => _burningTimeMax; set => _burningTimeMax = value; }

    public abstract bool HasRecipeWithInput(KitchenObjectSO inputKitchenObjectSO);

    public abstract KitchenObjectSO GetCookingOutput();
    public abstract KitchenObjectSO GetBurningOutput();
    public abstract void SetCookingRecipeSO(KitchenObjectSO kitchenObjectSO);
    public abstract void SetBurningRecipeSO(KitchenObjectSO kitchenObjectSO);
    public virtual void UpdateCookingState(State state,float cookingtime = 0)
    {
        CurrentState = state;

        CookingTimer = cookingtime;

        FireOnStateChange();
    }
    public virtual void FireOnStateChange()
    {
        OnStageChanged?.Invoke(this, new OnStageChangeEventArgs
        {
            state = CurrentState
        });
    }
}
