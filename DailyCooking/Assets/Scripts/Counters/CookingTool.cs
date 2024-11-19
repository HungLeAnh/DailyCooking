using System;
using UnityEngine;

public abstract class CookingTool: MonoBehaviour, IHasProgress, IKitchenObjectParent
{
    public event EventHandler<OnStageChangeEventArgs> OnStageChanged;
    public event EventHandler<IHasProgress.OnProgressChangedEventArgs> OnProgressChanged;

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

    [SerializeField] private Transform placePoint;

    private State _state;
    private float _cookingTimer;
    private float _burningTimer;
    private float _cookingTimeMax;
    private float _burningTimeMax;
    private KitchenObject _kitchenObject;

    public State CurrentState { get => _state; set => _state = value; }
    public float CookingTimer { get => _cookingTimer; set => _cookingTimer = value; }
    public float BurningTimer { get => _burningTimer; set => _burningTimer = value; }
    public float CookingTimeMax { get => _cookingTimeMax; set => _cookingTimeMax = value; }
    public float BurningTimeMax { get => _burningTimeMax; set => _burningTimeMax = value; }

    public abstract bool HasRecipeWithInput(KitchenObjectSO inputKitchenObjectSO);

    public abstract KitchenObjectSO GetCookingOutput();
    public abstract KitchenObjectSO GetBurningOutput();
    public abstract void SetCookingRecipeSO();
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
    public virtual void FireOnProgressChanged(float progressNormalized)
    {
        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs
        {
            progressNormalized = progressNormalized
        });
    }
    private void Update()
    {
        if (HasKitchenObject())
        {
            switch (CurrentState)
            {
                case CookingTool.State.Idle:
                    break;
                case CookingTool.State.Cooking:
                    CookingTimer += Time.deltaTime;

                    FireOnProgressChanged(CookingTimer / CookingTimeMax);

                    if (CookingTimer > CookingTimeMax)
                    {
                        //Fried
                        GetKitchenObject().DestroySelf();
                        KitchenObject.SpawnKitchenObject(GetCookingOutput(), this);
                        CurrentState = CookingTool.State.Cooked;
                        BurningTimer = 0f;
                        SetBurningRecipeSO(GetKitchenObject().GetKitchenObjectSO());

                        FireOnStateChange();

                    }
                    break;

                case CookingTool.State.Cooked:
                    BurningTimer += Time.deltaTime;

                    FireOnProgressChanged(BurningTimer / BurningTimeMax);

                    if (BurningTimer > BurningTimeMax)
                    {
                        //Fried
                        GetKitchenObject().DestroySelf();
                        KitchenObject.SpawnKitchenObject(GetBurningOutput(), this);

                        CurrentState = CookingTool.State.Burned;

                        FireOnStateChange();

                        FireOnProgressChanged(0f);

                    }
                    break;
                case CookingTool.State.Burned:
                    break;
            }

        }
    }
    public virtual void SetKitchenObject(KitchenObject kitchenObject)
    {
        _kitchenObject = kitchenObject;
    }

    public Transform GetKitchenObjectFollowTransform()
    {
        return placePoint;
}
    public KitchenObject GetKitchenObject()
    {
        return _kitchenObject;
    }
    public void ClearKitchenObject()
    {
        _kitchenObject = null;
    }
    public bool HasKitchenObject()
    {
        return _kitchenObject != null;
    }
    public bool IsDone()
    {
        return CurrentState == CookingTool.State.Cooked;   
    }
}
