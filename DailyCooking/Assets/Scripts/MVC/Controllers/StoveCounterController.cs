using System;
using UnityEngine;

[Serializable]
public class StoveCounterController : BaseCounterController
{
    private StoveCounterView _view;
    private StoveCounterModel _model;
    public StoveCounterController(StoveCounterView view,StoveCounterModel model) : base(view,model)
    {
        _view = view;
        _model = model;
    }
    private void Awake()
    {
        //_model.AudioSource = GetComponent<AudioSource>();
    }
    private void Init()
    {
        _view.CookingTool.CurrentState = CookingTool.State.Idle;
        _view.CookingTool.OnStageChanged += CookingTool_OnStageChanged;
        _view.CookingTool.OnProgressChanged += StoveCounter_OnProgressChanged;
    }

    public override void Interact(PlayerStateMachine playerStateMachine)
    {
        if (!_view.CookingTool.HasKitchenObject())
        {
            //There is no kitchen object
            if (playerStateMachine.HasKitchenObject())
            {
                KitchenObjectSO kitchenObjectSO = playerStateMachine.GetKitchenObject().GetKitchenObjectSO();
                //Player is carrying something
                if (_view.CookingTool.HasRecipeWithInput(kitchenObjectSO))
                {
                    //Player is carrying something that can be fried
                    //IHasOptionalSO option = (IHasOptionalSO)_cookingTool;
                    //if (option != null)
                    //{
                    //    FireOnShowCombineRecipe(option.GetListKitchenObjectList(kitchenObjectSO));
                    //}

                    playerStateMachine.GetKitchenObject().SetKitchenObjectParent(_view.CookingTool);

                    _view.CookingTool.SetCookingRecipeSO();

                    _view.CookingTool.UpdateCookingState(CookingTool.State.Cooking, 0f);
                    _view.CookingTool.FireOnProgressChanged(_view.CookingTool.CookingTimer / _view.CookingTool.CookingTimeMax);
                }
            }
            else
            {
                //Player is not carrying anything
            }
        }
        else
        {
            //There is kitchen object here
            if (playerStateMachine.HasKitchenObject())
            {
                //Player is carrying something
                if (playerStateMachine.GetKitchenObject().TryGetTableware(out TablewareKitchenObject plateKitchenObject))
                {
                    //Player is holding a plate
                    if (plateKitchenObject.TryAddIngredient(_view.CookingTool.GetKitchenObject().GetKitchenObjectSO()))
                    {
                        _view.CookingTool.GetKitchenObject().DestroySelf();

                        _view.CookingTool.UpdateCookingState(CookingTool.State.Idle, 0f);
                        _view.CookingTool.FireOnProgressChanged(0f);
                    }
                }
            }
            else
            {
                //Player is not carrying anything
                _view.CookingTool.GetKitchenObject().SetKitchenObjectParent(playerStateMachine);

                _view.CookingTool.UpdateCookingState(CookingTool.State.Idle, 0f);

                _view.CookingTool.FireOnProgressChanged(0f);

            }
        }
    }
    private void CookingTool_OnStageChanged(object sender, CookingTool.OnStageChangeEventArgs e)
    {
        bool playSound = e.state == CookingTool.State.Cooking || e.state == CookingTool.State.Cooked;
        if (playSound)
        {
            _model.AudioSource.Play();
        }
        else
        {
            _model.AudioSource.Pause();
        }
    }

    private void StoveCounter_OnProgressChanged(object sender, IHasProgress.OnProgressChangedEventArgs e)
    {

        float burnShowProgressAmount = .5f;

        _model.PlayWarningSound = _view.CookingTool.IsDone() && e.progressNormalized >= burnShowProgressAmount;


    }

    private void Update()
    {
        if (_model.PlayWarningSound)
        {
            _model.WarningSoundTimer -= Time.deltaTime;
            if (_model.WarningSoundTimer <= 0f)
            {
                float warningSoundTimerMax = .2f;
                _model.WarningSoundTimer = warningSoundTimerMax;
                SoundManager.Instance.PlayWarningSound(_view.transform.position);
            }
        }

    }
}