using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Facade: owns NetworkVariables + RPCs + Unity lifecycle only.
// Logic lives in FoodSlot / RecipeResolver / CookingClock / CookingPresenter.
public class CookingTool : NetworkBehaviour, IHasProgress, IKitchenObjectParent, IHasOptionalSO, IDestroyable, IPlaceable
{
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
    [SerializeField] private CombineDetailUI combineDetailUI;
    [SerializeField] private CookingToolConfigSO cookingToolConfig;

    private readonly NetworkVariable<State> _netState = new NetworkVariable<State>(State.Idle);
    private readonly NetworkVariable<float> _netCookingTimer = new NetworkVariable<float>(0f);
    private readonly NetworkVariable<float> _netBurningTimer = new NetworkVariable<float>(0f);
    private readonly NetworkVariable<int> _netCombineRecipeIndex = new NetworkVariable<int>(-1);
    private readonly NetworkVariable<NetworkObjectReference> _netFood = new NetworkVariable<NetworkObjectReference>();

    private readonly FoodSlot _slot = new FoodSlot();
    private readonly RecipeResolver _resolver = new RecipeResolver();
    private readonly CookingClock _clock = new CookingClock();
    private readonly CookingPresenter _presenter = new CookingPresenter();

    private bool _configResolved;
    private Action onDestroySelf;
    public Action OnDestroySelf { get => onDestroySelf; set => onDestroySelf += value; }

    public event Action OnCut;

    public State CurrentState => _netState.Value;
    public float CookingTimer => _netCookingTimer.Value;
    public float BurningTimer => _netBurningTimer.Value;
    public float CookingTimeMax => _resolver.CookingTimeMax;
    public float BurningTimeMax => _resolver.BurningTimeMax;

    private void InitializeConfig()
    {
        if (_configResolved) return;
        _configResolved = true;
        _resolver.SetConfig(cookingToolConfig);
    }

    private void InitializeDependencies()
    {
        InitializeConfig();
    }

    private void Awake()
    {
        _presenter.Init(progressBarUI, burnWarningUI, combineDetailUI, burnShowProgressAmount);
        _resolver.CombineResolved += _presenter.OnCombineResolved;
        InitializeConfig();
    }

    private void OnDestroy()
    {
        _resolver.CombineResolved -= _presenter.OnCombineResolved;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        InitializeDependencies();
        _presenter.Init(progressBarUI, burnWarningUI, combineDetailUI, burnShowProgressAmount);
        _netState.OnValueChanged += HandleStateChanged;
        _netCombineRecipeIndex.OnValueChanged += HandleCombineIndexChanged;
        _netFood.OnValueChanged += HandleFoodChanged;
        TryResolveFoodReference();
        _clock.Reset(_netCookingTimer.Value, _netBurningTimer.Value);
        _presenter.Reset();
        UpdateTickEnabled();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        _netState.OnValueChanged -= HandleStateChanged;
        _netCombineRecipeIndex.OnValueChanged -= HandleCombineIndexChanged;
        _netFood.OnValueChanged -= HandleFoodChanged;
    }

    private void HandleFoodChanged(NetworkObjectReference previousValue, NetworkObjectReference newValue)
    {
        TryResolveFoodReference();
    }

    private void TryResolveFoodReference()
    {
        if (_slot.Has)
            return;
        if (_slot.TryResolveFrom(_netFood.Value))
        {
            _resolver.ResetSlot();
            _resolver.EnsureResolved(_slot.CurrentSO);
            _clock.Reset(_netCookingTimer.Value, _netBurningTimer.Value);
            _presenter.Reset();
            UpdateTickEnabled();
        }
    }

    public bool RequiresOptionChoice(KitchenObjectSO input)
    {
        return _resolver.RequiresOption(input, cookingToolConfig != null && cookingToolConfig.supportsOptionMenu);
    }

    public bool HasValidRecipe() => _resolver.HasValidRecipe;

    public bool IsCuttingTool
    {
        get
        {
            return _resolver.Supports(CookingToolConfigSO.CookingToolType.Cutting);
        }
    }

    public bool IsCuttingActive
    {
        get
        {
            _resolver.EnsureResolved(_slot.CurrentSO);
            return _resolver.IsCutting;
        }
    }

    private void UpdateTickEnabled()
    {
        enabled = !_slot.Has || _netState.Value == State.Cooking || _netState.Value == State.Cooked;
    }

    private void HandleStateChanged(State previousValue, State newValue)
    {
        _clock.Reset(_netCookingTimer.Value, _netBurningTimer.Value);
        _presenter.Reset();
        UpdateTickEnabled();
        FireOnStateChange();
    }

    private void HandleCombineIndexChanged(int previousValue, int newValue)
    {
        _resolver.EnsureCombine(_slot.CurrentSO, newValue);
    }

    // Recipe delegates (resolver owns state; callers read resolver.Output, not facade getters).
    public bool HasRecipeWithInput(KitchenObjectSO input)
    {
        return _resolver.HasRecipe(input);
    }

    public void SetCookingRecipeSO()
    {
        _resolver.ResolveFor(_slot.CurrentSO);
    }

    public void SetBurningRecipeSO(KitchenObjectSO kitchenObjectSO)
    {
        _resolver.ResolveBurning(kitchenObjectSO);
    }

    public void Cut()
    {
        float cookingSpeed = 1f;
        try { cookingSpeed = GameManager.Instance.GameData.GetPlayerStatsById(SessionManager.Instance.PlayerId).CookingSpeed; }
        catch { cookingSpeed = 1f; }
        CutServerRpc(cookingSpeed);
    }

    [Rpc(SendTo.Server)]
    private void CutServerRpc(float cookingSpeed)
    {
        if (!_slot.Has || !HasRecipeWithInput(_slot.CurrentSO))
            return;
        _resolver.EnsureResolved(_slot.CurrentSO);
        if (!_resolver.IsCutting || !_resolver.HasValidRecipe)
            return;

        _netCookingTimer.Value += (int)cookingSpeed;
        _clock.Reset(_netCookingTimer.Value, _netBurningTimer.Value);
        CutFeedbackClientRpc();

        if (_netCookingTimer.Value >= _resolver.CookingTimeMax)
        {
            KitchenObjectSO output = _resolver.CookingOutput;
            _slot.Current.DestroySelf();
            KitchenObject.SpawnKitchenObject(output, this);
            _netCookingTimer.Value = 0f;
            _clock.Reset(0f, 0f);
            _presenter.Reset();
            _resolver.ResolveFor(_slot.CurrentSO);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void CutFeedbackClientRpc()
    {
        _clock.Reset(_netCookingTimer.Value, _netBurningTimer.Value);
        if (progressBarUI != null && _resolver.CookingTimeMax > 0f)
            progressBarUI.OnProgressChanged(Mathf.Clamp01(_netCookingTimer.Value / _resolver.CookingTimeMax));
        OnCut?.Invoke();
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayChopSound(transform.position);
    }

    public void UpdateCookingState(State state)
    {
        UpdateCookingStateServerRpc(state);
    }

    [Rpc(SendTo.Server)]
    private void UpdateCookingStateServerRpc(State state)
    {
        if (state != State.Idle && state != State.Cooking)
            return;
        if (state == State.Cooking && !_slot.Has)
            return;
        if (state == State.Cooking)
        {
            _resolver.EnsureCombine(_slot.CurrentSO, _netCombineRecipeIndex.Value);
            _resolver.EnsureResolved(_slot.CurrentSO);
            if (!_resolver.HasValidRecipe)
                return;
        }
        _netCookingTimer.Value = 0f;
        _netBurningTimer.Value = 0f;
        _clock.Reset(0f, 0f);
        _presenter.Reset();
        _netState.Value = state;
    }

    public void FireOnStateChange()
    {
        _presenter.OnStateChanged(_netState.Value, transform.position);
    }

    private void Update()
    {
        if (!_slot.Has)
        {
            TryResolveFoodReference();
            if (!_slot.Has)
            {
                UpdateTickEnabled();
                return;
            }
        }

        _resolver.EnsureCombine(_slot.CurrentSO, _netCombineRecipeIndex.Value);
        _clock.Tick(_netState.Value, Time.deltaTime, IsServer, _netCookingTimer.Value, _netBurningTimer.Value);

        if (IsServer)
            AdvanceCooking();

        UpdateUI();
        UpdateTickEnabled();
    }

    private void AdvanceCooking()
    {
        switch (_netState.Value)
        {
            case State.Idle:
                break;
            case State.Cooking:
                _resolver.EnsureResolved(_slot.CurrentSO);
                if (!_resolver.HasValidRecipe)
                    break;
                if (_resolver.IsCutting)
                    break;
                _clock.AddSync(Time.deltaTime);
                if (_clock.ShouldSync())
                {
                    _netCookingTimer.Value = _clock.LocalCook;
                    _clock.MarkSynced();
                }
                if (_clock.LocalCook > _resolver.CookingTimeMax)
                {
                    _netCookingTimer.Value = _clock.LocalCook;
                    KitchenObjectSO output = _resolver.CookingOutput;
                    _slot.Current.DestroySelf();
                    KitchenObject.SpawnKitchenObject(output, this);
                    _netState.Value = State.Cooked;
                    _netBurningTimer.Value = 0f;
                    _clock.Reset(_clock.LocalCook, 0f);
                    _presenter.Reset();
                    _resolver.ResolveBurning(_slot.CurrentSO);
                    _resolver.MarkBurningResolved();
                }
                break;
            case State.Cooked:
                if (_resolver.BurningRecipe == null)
                    break;
                _clock.AddSync(Time.deltaTime);
                if (_clock.ShouldSync())
                {
                    _netBurningTimer.Value = _clock.LocalBurn;
                    _clock.MarkSynced();
                }
                if (_clock.LocalBurn > _resolver.BurningTimeMax)
                {
                    _netBurningTimer.Value = _clock.LocalBurn;
                    KitchenObjectSO output = _resolver.BurningOutput;
                    _slot.Current.DestroySelf();
                    KitchenObject.SpawnKitchenObject(output, this);
                    _netState.Value = State.Burned;
                }
                break;
            case State.Burned:
                break;
        }
    }

    private void UpdateUI()
    {
        if (_netState.Value == State.Idle && _slot.Has)
        {
            _resolver.EnsureResolved(_slot.CurrentSO);
            if (_resolver.IsCutting && _resolver.CookingTimeMax > 0f)
            {
                if (progressBarUI != null)
                    progressBarUI.OnProgressChanged(Mathf.Clamp01(_netCookingTimer.Value / _resolver.CookingTimeMax));
                return;
            }
        }
        _presenter.Draw(_netState.Value, _clock, _resolver, this, progressBarUI, burnWarningUI, transform.position);
    }

    // IKitchenObjectParent
    public virtual void SetKitchenObject(KitchenObject kitchenObject, int index = 0)
    {
        _slot.Set(kitchenObject);
        _resolver.ResetSlot();
        _resolver.ResolveFor(_slot.CurrentSO);
        _clock.Reset(_netCookingTimer.Value, _netBurningTimer.Value);
        _presenter.Reset();
        progressBarUI.Hide();
        burnWarningUI.Hide();
        if (IsServer && kitchenObject != null && kitchenObject.NetworkObject != null)
            _netFood.Value = kitchenObject.NetworkObject;
        enabled = true;
    }

    public Transform GetKitchenObjectFollowTransform(int index = 0)
    {
        return placePoint;
    }

    public KitchenObject GetKitchenObject(int index = 0)
    {
        return _slot.Current;
    }

    public void ClearKitchenObject(int index = 0)
    {
        _slot.Clear();
        _resolver.ResetSlot();
        _clock.Clear();
        _presenter.Reset();
        progressBarUI.Hide();
        burnWarningUI.Hide();
        if (IsServer)
            _netFood.Value = new NetworkObjectReference();
        if (cookingToolConfig != null && _resolver.Supports(CookingToolConfigSO.CookingToolType.Combine))
            ResetCombineIndexServerRpc();
        UpdateTickEnabled();
    }

    [Rpc(SendTo.Server)]
    private void ResetCombineIndexServerRpc()
    {
        if (_netCombineRecipeIndex.Value != -1)
            _netCombineRecipeIndex.Value = -1;
    }

    public bool HasKitchenObject(int index = 0)
    {
        return _slot.Has;
    }

    // IHasProgress
    public bool IsDone()
    {
        return _netState.Value == State.Cooked;
    }

    public float GetProgress()
    {
        if (_netState.Value == State.Idle && _slot.Has)
        {
            _resolver.EnsureResolved(_slot.CurrentSO);
            if (_resolver.IsCutting)
                return _resolver.CookingTimeMax > 0f ? _netCookingTimer.Value / _resolver.CookingTimeMax : 0f;
        }
        if (_netState.Value == State.Cooking)
            return _resolver.CookingTimeMax > 0f ? _clock.LocalCook / _resolver.CookingTimeMax : 0f;
        if (_netState.Value == State.Cooked)
            return _resolver.BurningRecipe != null && _resolver.BurningTimeMax > 0f
                ? _clock.LocalBurn / _resolver.BurningTimeMax : 1f;
        return 0f;
    }

    public NetworkObject GetNetworkObject()
    {
        return NetworkObject;
    }


    // IHasOptionalSO
    public void SetOptionKitchenObjectSO(int index)
    {
        if (_resolver.ApplyOption(_slot.CurrentSO, index))
            SetOptionKitchenObjectServerRpc(index);
    }

    [Rpc(SendTo.Server)]
    private void SetOptionKitchenObjectServerRpc(int index)
    {
        _netCombineRecipeIndex.Value = index;
    }

    public void ShowLocalOptionMenu(KitchenObjectSO input)
    {
        if (cookingToolConfig == null || !cookingToolConfig.supportsOptionMenu || !_resolver.Supports(CookingToolConfigSO.CookingToolType.Combine))
            return;
        _presenter.ShowOptionMenu(input, _resolver.GetOptions(input), this);
    }

    public List<KitchenObjectSO> GetListKitchenObjectList(KitchenObjectSO kitchenObjectSO)
    {
        return _resolver.GetOptions(kitchenObjectSO);
    }

    // IDestroyable / IPlaceable
    public void DestroySelf()
    {
        OnDestroySelf?.Invoke();
        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn();
        else if (gameObject != null)
            Destroy(gameObject);
        else
            Destroy(this);
    }

    public bool CanRemove()
    {
        return !HasKitchenObject();
    }
}
