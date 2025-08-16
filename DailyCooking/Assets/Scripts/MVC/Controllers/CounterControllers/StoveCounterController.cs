using System;
using UnityEngine;
public class StoveCounterController : BaseCounterController, IHasProgress
{
    [SerializeField] private CookingRecipeSO[] _cookingRecipeSOArray;
    [SerializeField] private BurningRecipeSO[] _burningRecipeSOArray;
    [SerializeField] private AudioSource _audioSource;
    private StoveCounterModel _stoveCounterModel;
    private StoveCounterService _stoveCounterService;

    public event EventHandler<float> OnProgressChanged;
    public event EventHandler<StoveCounterService.State> OnStateChanged;

    private void Awake()
    {
        _stoveCounterModel = new StoveCounterModel();
        BaseCounterModel = _stoveCounterModel;
        _stoveCounterService = new StoveCounterService(_cookingRecipeSOArray, _burningRecipeSOArray);

        _stoveCounterService.OnStateChanged += (sender, state) =>
        {
            bool playSound = state == StoveCounterService.State.Cooking || state == StoveCounterService.State.Cooked;
            if (playSound)
            {
                _audioSource.Play();
            }
            else
            {
                _audioSource.Pause();
            }
            OnStateChanged?.Invoke(this, state);
        };
        _stoveCounterService.OnProgressChanged += (sender, progress) => OnProgressChanged?.Invoke(this, progress);
    }

    private void Update()
    {
        _stoveCounterService.Update(_stoveCounterModel, this);
    }

    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        _stoveCounterService.Interact(_stoveCounterModel, this, playerStateMachine);
    }

    public float GetProgress()
    {
        return _stoveCounterModel.GetProgress();
    }

    public bool IsDone()
    {
        return _stoveCounterModel.IsDone();
    }
}
