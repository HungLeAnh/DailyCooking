using System;
using UnityEngine;

public abstract class CookingTool: MonoBehaviour, IHasProgress, IKitchenObjectParent
{
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
    [SerializeField] private ProgressBarUI progressBarUI;
    [SerializeField] private BurnWarningUI burnWarningUI;
    [SerializeField] private float burnShowProgressAmount = 0.5f;
    private State _state;
    private float _cookingTimer;
    private float _burningTimer;
    private float _cookingTimeMax;
    private float _burningTimeMax;
    private KitchenObject _kitchenObject;
    private AudioSource cookingSound;
    private AudioSource warningSound;

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
    public virtual void UpdateCookingState(State state)
    {
        CurrentState = state;

        CookingTimer = 0;

        BurningTimer = 0;

        FireOnStateChange();
    }
    public virtual void FireOnStateChange()
    { 
        if(CurrentState == State.Idle || CurrentState == State.Burned)
        {
            SoundManager.Instance.StopSound(cookingSound);
            SoundManager.Instance.StopSound(warningSound);
        }
        if (CurrentState == State.Cooking || CurrentState == State.Cooked)
        {
            if(cookingSound == null || !cookingSound.isPlaying)
                cookingSound = SoundManager.Instance.PlayCookingSound(gameObject.transform.position);
        }
        

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

                    progressBarUI.OnProgressChanged(CookingTimer / CookingTimeMax);
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

                    progressBarUI.OnProgressChanged(BurningTimer / BurningTimeMax);
                    burnWarningUI.OnProgressChanged(this, BurningTimer / BurningTimeMax);

                    if (GetProgress() >= burnShowProgressAmount && ( warningSound == null || !warningSound.isPlaying))
                    {
                        warningSound = SoundManager.Instance.PlayWarningSound(gameObject.transform.position);
                    }

                    if (BurningTimer > BurningTimeMax)
                    {
                        //Fried
                        GetKitchenObject().DestroySelf();
                        KitchenObject.SpawnKitchenObject(GetBurningOutput(), this);

                        CurrentState = CookingTool.State.Burned;

                        FireOnStateChange();

                    }
                    break;
                case CookingTool.State.Burned:
                    progressBarUI.Hide();               
                    burnWarningUI.Hide();
                    break;
            }

        }
    }
    public virtual void SetKitchenObject(KitchenObject kitchenObject)
    {
        _kitchenObject = kitchenObject;
        progressBarUI.Hide();
        burnWarningUI.Hide();
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
        progressBarUI.Hide();
        burnWarningUI.Hide();
    }
    public bool HasKitchenObject()
    {
        return _kitchenObject != null;
    }
    public bool IsDone()
    {
        return CurrentState == CookingTool.State.Cooked;   
    }

    public float GetProgress()
    {
        if(CurrentState == CookingTool.State.Cooking)
        {
            return CookingTimer / CookingTimeMax;
        }
        else if (CurrentState == CookingTool.State.Cooked)
        {
            return BurningTimer / BurningTimeMax;
        }
        else
        {
            return 0;
        }
    }
}