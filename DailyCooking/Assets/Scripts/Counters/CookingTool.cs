using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CookingTool : NetworkBehaviour, IHasProgress, IKitchenObjectParent, IDestroyable, IPlaceable
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

    private readonly NetworkVariable<State> _netState = new NetworkVariable<State>(State.Idle, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<float> _netCookingTimer = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<float> _netBurningTimer = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<int> _netCombineRecipeIndex = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<NetworkObjectReference> _netFood = new NetworkVariable<NetworkObjectReference>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly FoodSlot _slot = new FoodSlot();
    private readonly RecipeResolver _resolver = new RecipeResolver();
    private readonly CookingClock _clock = new CookingClock();
    private readonly CookingPresenter _presenter = new CookingPresenter();

    private bool _configResolved;
    public event Action OnDestroySelf;

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

    private void Awake()
    {
        _presenter.Init(progressBarUI, burnWarningUI, combineDetailUI, burnShowProgressAmount);
        _resolver.CombineResolved += _presenter.OnCombineResolved;
        InitializeConfig();
    }

    public override void OnDestroy()
    {
        _resolver.CombineResolved -= _presenter.OnCombineResolved;
        base.OnDestroy();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        InitializeConfig();
        _presenter.Init(progressBarUI, burnWarningUI, combineDetailUI, burnShowProgressAmount);
        _netState.OnValueChanged += HandleStateChanged;
        _netCombineRecipeIndex.OnValueChanged += HandleCombineIndexChanged;
        _netFood.OnValueChanged += HandleFoodChanged;
        TryResolveFoodReference();
        _clock.Reset(_netCookingTimer.Value, _netBurningTimer.Value);
        _presenter.Reset();
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
        if (_slot.HasFood)
            return;
        if (_slot.TryResolveFrom(_netFood.Value))
        {
            _resolver.ResetSlot();
            _resolver.EnsureResolved(_slot.Current.GetKitchenObjectSO());
            _clock.Reset(_netCookingTimer.Value, _netBurningTimer.Value);
            _presenter.Reset();
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
            if (!_slot.HasFood)
                return false;
            _resolver.EnsureResolved(_slot.Current.GetKitchenObjectSO());
            return _resolver.IsCutting;
        }
    }

    private void HandleStateChanged(State previousValue, State newValue)
    {
        _clock.Reset(_netCookingTimer.Value, _netBurningTimer.Value);
        _presenter.Reset();
        FireOnStateChange();
    }

    private void HandleCombineIndexChanged(int previousValue, int newValue)
    {
        if (!_slot.HasFood)
            return;
        _resolver.EnsureCombine(_slot.Current.GetKitchenObjectSO(), newValue);
    }

    // Recipe delegates (resolver owns state; callers read resolver.Output, not facade getters).
    public bool HasRecipeWithInput(KitchenObjectSO input)
    {
        return _resolver.HasRecipe(input);
    }

    public void SetCookingRecipeSO()
    {
        _resolver.ResolveFor(_slot.Current.GetKitchenObjectSO());
    }

    public void SetBurningRecipeSO(KitchenObjectSO kitchenObjectSO)
    {
        _resolver.ResolveBurning(kitchenObjectSO);
    }

    // Server only: one cut by actor, at the actor's own cooking speed.
    public void Cut(PlayerStateMachine actor)
    {
        if (!IsServer) return;
        float cookingSpeed = 1f;
        PlayerStats stats = actor != null ? actor.GetOwnerStats() : null;
        if (stats != null)
            cookingSpeed = stats.CookingSpeed;
        else
            Debug.LogWarning("CookingTool.Cut: missing player stats, default CookingSpeed=1.", this);
        if (cookingSpeed <= 0f) return;
        if (!_slot.HasFood || !HasRecipeWithInput(_slot.Current.GetKitchenObjectSO()))
            return;
        _resolver.EnsureResolved(_slot.Current.GetKitchenObjectSO());
        if (!_resolver.IsCutting || !_resolver.HasValidRecipe)
            return;

        _netCookingTimer.Value += cookingSpeed;
        _clock.Reset(_netCookingTimer.Value, _netBurningTimer.Value);
        CutFeedbackClientRpc();

        if (_netCookingTimer.Value >= _resolver.CookingTimeMax)
        {
            KitchenObjectSO output = _resolver.CookingOutput;
            KitchenObject current = _slot.Current;
            if (output == null || current == null)
            {
                _resolver.ResetSlot();
                _clock.Reset(_netCookingTimer.Value, _netBurningTimer.Value);
                _presenter.Reset();
                return;
            }
            current.DestroySelf();
            KitchenObject.SpawnKitchenObject(output, this);
            _netCookingTimer.Value = 0f;
            _clock.Reset(0f, 0f);
            _presenter.Reset();
            KitchenObject fresh = _slot.Current;
            _resolver.ResolveFor(fresh != null ? fresh.GetKitchenObjectSO() : null);
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

    // Server only.
    public void UpdateCookingState(State state)
    {
        if (!IsServer) return;
        if (state != State.Idle && state != State.Cooking)
            return;
        if (state == State.Cooking && !_slot.HasFood)
            return;
        if (state == State.Cooking)
        {
            KitchenObjectSO currentSO = _slot.Current != null ? _slot.Current.GetKitchenObjectSO() : null;
            _resolver.EnsureCombine(currentSO, _netCombineRecipeIndex.Value);
            _resolver.EnsureResolved(currentSO);
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
        if (!_slot.HasFood)
        {
            TryResolveFoodReference();
            if (!_slot.HasFood)
                return;
        }

        KitchenObject currentItem = _slot.Current;
        KitchenObjectSO currentSO = currentItem != null ? currentItem.GetKitchenObjectSO() : null;
        _resolver.EnsureCombine(currentSO, _netCombineRecipeIndex.Value);
        _clock.Tick(_netState.Value, Time.deltaTime, IsServer, _netCookingTimer.Value, _netBurningTimer.Value);

        if (IsServer)
            AdvanceCooking(currentSO);

        UpdateUI(currentSO);
    }

    private void AdvanceCooking(KitchenObjectSO currentSO = null)
    {
        switch (_netState.Value)
        {
            case State.Idle:
                break;
            case State.Cooking:
                _resolver.EnsureResolved(currentSO ?? _slot.Current.GetKitchenObjectSO());
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
                    KitchenObject current = _slot.Current;
                    if (output == null || current == null)
                    {
                        _resolver.ResetSlot();
                        _clock.Reset(_netCookingTimer.Value, _netBurningTimer.Value);
                        _presenter.Reset();
                        break;
                    }
                    current.DestroySelf();
                    KitchenObject.SpawnKitchenObject(output, this);
                    _netState.Value = State.Cooked;
                    _netBurningTimer.Value = 0f;
                    _clock.Reset(_clock.LocalCook, 0f);
                    _presenter.Reset();
                    KitchenObject fresh = _slot.Current;
                    _resolver.ResolveBurning(fresh != null ? fresh.GetKitchenObjectSO() : null);
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
                    KitchenObject current = _slot.Current;
                    if (output == null || current == null)
                        break;
                    current.DestroySelf();
                    KitchenObject.SpawnKitchenObject(output, this);
                    _netState.Value = State.Burned;
                }
                break;
            case State.Burned:
                break;
        }
    }

    private void UpdateUI(KitchenObjectSO currentSO = null)
    {
        if (_netState.Value == State.Idle && _slot.HasFood)
        {
            _resolver.EnsureResolved(currentSO ?? _slot.Current.GetKitchenObjectSO());
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
        _resolver.ResolveFor(_slot.Current.GetKitchenObjectSO());
        _clock.Reset(_netCookingTimer.Value, _netBurningTimer.Value);
        _presenter.Reset();
        progressBarUI.Hide();
        burnWarningUI.Hide();
        if (IsServer && kitchenObject != null && kitchenObject.NetworkObject != null)
            _netFood.Value = kitchenObject.NetworkObject;
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
        {
            _netFood.Value = new NetworkObjectReference();
            if (_netCombineRecipeIndex.Value != -1)
                _netCombineRecipeIndex.Value = -1;
        }
    }

    public bool HasKitchenObject(int index = 0)
    {
        return _slot.HasFood;
    }

    // IHasProgress
    public bool IsDone()
    {
        return _netState.Value == State.Cooked;
    }

    public float GetProgress()
    {
        if (_netState.Value == State.Idle && _slot.HasFood)
        {
            _resolver.EnsureResolved(_slot.Current.GetKitchenObjectSO());
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


    // Server only: picks the combine option for the food in the slot. Returns false for a bad index.
    public bool ApplyOption(int index)
    {
        if (!IsServer || !_slot.HasFood)
            return false;
        if (!_resolver.ApplyOption(_slot.Current.GetKitchenObjectSO(), index))
            return false;
        _netCombineRecipeIndex.Value = index;
        return true;
    }

    // Server: shows the combine options for input to the actor, if this tool offers a menu.
    public void ShowOptionMenu(PlayerStateMachine actor, IHasOptionalSO sender, KitchenObjectSO input)
    {
        if (cookingToolConfig == null || !cookingToolConfig.supportsOptionMenu || !_resolver.Supports(CookingToolConfigSO.CookingToolType.Combine))
            return;
        UIManager.Instance.ShowOptionMenu(actor, sender, _resolver.GetOptions(input), CookingPresenter.OptionTitle);
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
