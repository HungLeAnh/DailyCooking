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
    [Tooltip("Optional extra ingredient drop points for multi-ingredient tools. Missing entries are generated at runtime, offset from placePoint.")]
    [SerializeField] private List<Transform> extraPlacePoints = new List<Transform>();
    [SerializeField] private float multiSlotSpacing = 0.4f;
    [SerializeField] private ProgressBarUI progressBarUI;
    [SerializeField] private BurnWarningUI burnWarningUI;
    [SerializeField] private float burnShowProgressAmount = 0.5f;
    [SerializeField] private CombineDetailUI combineDetailUI;
    [SerializeField] private CookingToolConfigSO cookingToolConfig;

    private readonly NetworkVariable<State> _netState = new NetworkVariable<State>(State.Idle, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<float> _netCookingTimer = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<float> _netBurningTimer = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<int> _netCombineRecipeIndex = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<int> _netCuttingRecipeIndex = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // One reference per slot, so every peer (and late joiners) can rebuild every ingredient.
    private readonly NetworkList<NetworkObjectReference> _netFoods = new NetworkList<NetworkObjectReference>();

    private readonly FoodSlot _slot = new FoodSlot();
    private readonly List<KitchenObjectSO> _inputSOs = new List<KitchenObjectSO>();
    private readonly RecipeResolver _resolver = new RecipeResolver();
    private readonly CookingClock _clock = new CookingClock();
    private readonly CookingPresenter _presenter = new CookingPresenter();

    private bool _configResolved;
    private readonly List<KitchenObject> _hiddenFoods = new List<KitchenObject>();
    private Transform[] _slotPoints;
    public event Action OnDestroySelf;

    public event Action OnCut;

    public State CurrentState => _netState.Value;
    public float CookingTimer => _netCookingTimer.Value;
    public float BurningTimer => _netBurningTimer.Value;
    public float CookingTimeMax => _resolver.CookingTimeMax;
    public float BurningTimeMax => _resolver.BurningTimeMax;

    // Number of ingredients this tool accepts at once. 1 keeps the classic single slot.
    public int MaxFoodSlots => cookingToolConfig != null ? Mathf.Max(1, cookingToolConfig.maxFoodSlots) : 1;
    // Multi-input tools such as the Blender wait for the player to press start instead of
    // beginning as soon as the ingredient set matches a recipe.
    public bool RequiresManualStart => cookingToolConfig != null && cookingToolConfig.requiresManualStart;
    public bool HasAnyFood => _slot.HasFood;
    public bool IsFull => _slot.FoodCount >= MaxFoodSlots;

    private KitchenObjectSO FirstInputSO => _inputSOs.Count > 0 ? _inputSOs[0] : null;

    private void InitializeConfig()
    {
        if (_configResolved) return;
        _configResolved = true;
        _resolver.SetConfig(cookingToolConfig);
        EnsurePlacePoints();
    }

    private void Awake()
    {
        InitializeConfig();
        _presenter.Init(progressBarUI, burnWarningUI, combineDetailUI, burnShowProgressAmount);
        _resolver.CombineResolved += _presenter.OnCombineResolved;
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
        _netCuttingRecipeIndex.OnValueChanged += HandleCuttingRecipeIndexChanged;
        _netFoods.OnListChanged += HandleFoodListChanged;
        TryResolveFoodsFromNetwork();
        _clock.Reset(_netCookingTimer.Value, _netBurningTimer.Value);
        _presenter.Reset();
    }

    public override void OnNetworkDespawn()
    {
        _netState.OnValueChanged -= HandleStateChanged;
        _netCombineRecipeIndex.OnValueChanged -= HandleCombineIndexChanged;
        _netCuttingRecipeIndex.OnValueChanged -= HandleCuttingRecipeIndexChanged;
        _netFoods.OnListChanged -= HandleFoodListChanged;
        base.OnNetworkDespawn();
    }

    private void HandleFoodListChanged(NetworkListEvent<NetworkObjectReference> changeEvent)
    {
        TryResolveFoodsFromNetwork();
    }

    // Rebuilds the local slots from the replicated references. The parenting of each
    // KitchenObject normally does this too; this covers the case where the tool spawns
    // after its food, and late joiners.
    private bool TryResolveFoodsFromNetwork()
    {
        bool changed = false;
        for (int i = 0; i < _netFoods.Count; i++)
        {
            if (_slot.Get(i) != null) continue;
            if (_slot.TryResolveFrom(_netFoods[i], i)) changed = true;
        }

        if (!changed)
        {
            RefreshFoodVisibility();
            return false;
        }

        RebuildInputs();
        _resolver.ResetSlot();
        if (_inputSOs.Count > 0)
        {
            _resolver.ResolveFor(_inputSOs);
            if (_netCuttingRecipeIndex.Value >= 0)
                _resolver.ApplyCuttingOption(FirstInputSO, _netCuttingRecipeIndex.Value);
            // Clients and late joiners need the burning recipe too, otherwise the warning
            // and progress cannot render when the server later changes the state to Cooked.
            _resolver.EnsureBurning(FirstInputSO);
        }
        _clock.Reset(_netCookingTimer.Value, _netBurningTimer.Value);
        _presenter.Reset();
        RefreshFoodVisibility();
        return true;
    }

    private void RebuildInputs()
    {
        _inputSOs.Clear();
        for (int i = 0; i < _slot.SlotCount; i++)
        {
            KitchenObject food = _slot.Get(i);
            if (food == null) continue;
            KitchenObjectSO foodSO = food.GetKitchenObjectSO();
            if (foodSO != null) _inputSOs.Add(foodSO);
        }
    }

    public bool RequiresOptionChoice(KitchenObjectSO input)
    {
        return _resolver.RequiresCuttingChoice(input) ||
               _resolver.RequiresOption(input, cookingToolConfig != null && cookingToolConfig.supportsOptionMenu);
    }

    public bool HasValidRecipe() => _resolver.HasValidRecipe;

    public bool IsCuttingTool
    {
        get
        {
            return _resolver.Supports(CookingToolConfigSO.CookingToolType.Cutting);
        }
    }

    // Beverage tools (blender) consume the ingredient while cooking and only show the result.
    public bool IsBeverageTool
    {
        get
        {
            return _resolver.Supports(CookingToolConfigSO.CookingToolType.Beverage);
        }
    }

    public bool IsCuttingActive
    {
        get
        {
            if (!_slot.HasFood)
                return false;
            _resolver.EnsureResolved(_inputSOs);
            return _resolver.IsCutting;
        }
    }

    // Server only: can this ingredient be added right now without wasting it?
    public bool CanAcceptInput(KitchenObjectSO input)
    {
        if (input == null || IsFull)
            return false;
        InitializeConfig();
        if (IsBeverageTool)
            return _resolver.CanAcceptDrinkIngredient(_inputSOs, input);
        return !_slot.HasFood && _resolver.HasRecipe(input);
    }

    public int GetFirstFreeSlotIndex()
    {
        return _slot.FirstFreeIndex(MaxFoodSlots);
    }

    // True when a manual-start tool holds a complete recipe and only waits for the press.
    public bool IsReadyToStart
    {
        get
        {
            if (!RequiresManualStart || !_slot.HasFood || _netState.Value != State.Idle)
                return false;
            RebuildInputs();
            _resolver.EnsureResolved(_inputSOs);
            return _resolver.HasValidRecipe && !_resolver.IsCutting;
        }
    }

    // Server only: presses the start button of a manual-start tool (the Blender).
    public bool TryStartCooking()
    {
        if (!IsServer || !IsReadyToStart)
            return false;
        UpdateCookingState(State.Cooking);
        return _netState.Value == State.Cooking;
    }

    public bool TryGetFirstFood(out KitchenObject food, out int index)
    {
        for (int i = 0; i < _slot.SlotCount; i++)
        {
            KitchenObject candidate = _slot.Get(i);
            if (candidate == null) continue;
            food = candidate;
            index = i;
            return true;
        }
        food = null;
        index = -1;
        return false;
    }

    private void HandleStateChanged(State previousValue, State newValue)
    {
        if (newValue == State.Cooked && FirstInputSO != null)
            _resolver.EnsureBurning(FirstInputSO);
        _clock.Reset(_netCookingTimer.Value, _netBurningTimer.Value);
        _presenter.Reset();
        RefreshFoodVisibility();
        FireOnStateChange();
    }

    // Beverage tools swallow their input while cooking: hide the ingredients and keep them
    // hidden until the cooked output replaces them. Other tools always show their food.
    public void RefreshFoodVisibility()
    {
        if (!IsBeverageTool)
        {
            ShowHiddenFood();
            return;
        }

        switch (_netState.Value)
        {
            case State.Cooking:
                HideCookingInputs();
                break;
            case State.Cooked:
                // Keep the old inputs hidden until the cooked output takes their place.
                PruneHiddenFoods();
                break;
            default:
                ShowHiddenFood();
                break;
        }
    }

    private void HideCookingInputs()
    {
        if (_hiddenFoods.Count == 0)
        {
            // First frame of cooking: remember and hide every ingredient that goes in.
            for (int i = 0; i < _slot.SlotCount; i++)
            {
                KitchenObject food = _slot.Get(i);
                if (food == null) continue;
                _hiddenFoods.Add(food);
                food.SetVisualsVisible(false);
            }
            return;
        }

        // A new output may already sit in the slot: only re-hide the remembered inputs.
        for (int i = 0; i < _hiddenFoods.Count; i++)
        {
            KitchenObject food = _hiddenFoods[i];
            if (food != null && _slot.Contains(food))
                food.SetVisualsVisible(false);
        }
    }

    private void PruneHiddenFoods()
    {
        for (int i = _hiddenFoods.Count - 1; i >= 0; i--)
        {
            KitchenObject food = _hiddenFoods[i];
            if (food == null || !_slot.Contains(food))
                _hiddenFoods.RemoveAt(i);
        }
    }

    private void ShowHiddenFood()
    {
        if (_hiddenFoods.Count == 0)
            return;
        for (int i = 0; i < _hiddenFoods.Count; i++)
        {
            KitchenObject food = _hiddenFoods[i];
            // A parentless item is being despawned (already replaced by the cooked output);
            // re-enabling it would flash the raw ingredient for a frame.
            if (food != null && food.GetKitchenObjectParent() != null)
                food.SetVisualsVisible(true);
        }
        _hiddenFoods.Clear();
    }

    private void HandleCombineIndexChanged(int previousValue, int newValue)
    {
        if (!_slot.HasFood)
            return;
        _resolver.EnsureCombine(_slot.Current.GetKitchenObjectSO(), newValue);
    }

    private void HandleCuttingRecipeIndexChanged(int previousValue, int newValue)
    {
        if (!_slot.HasFood)
            return;
        KitchenObjectSO foodSO = _slot.Current.GetKitchenObjectSO();
        if (newValue < 0)
            _resolver.ResolveFor(foodSO);
        else
            _resolver.ApplyCuttingOption(foodSO, newValue);
        _clock.Reset(_netCookingTimer.Value, _netBurningTimer.Value);
        _presenter.Reset();
    }

    // Recipe delegates (resolver owns state; callers read resolver.Output, not facade getters).
    public bool HasRecipeWithInput(KitchenObjectSO input)
    {
        return _resolver.HasRecipe(input);
    }

    public void SetCookingRecipeSO()
    {
        _resolver.ResolveFor(_inputSOs);
    }

    // Server only: one cut by actor, at the actor's own cutting speed.
    public void Cut(PlayerStateMachine actor)
    {
        if (!IsServer) return;
        float cuttingSpeed = 1f;
        PlayerStats stats = actor != null ? actor.GetOwnerStats() : null;
        if (stats != null)
            cuttingSpeed = stats.CuttingSpeed;
        else
            Debug.LogWarning("CookingTool.Cut: missing player stats, default CuttingSpeed=1.", this);
        if (cuttingSpeed <= 0f) return;
        KitchenObject current = _slot.Current;
        if (current == null || !HasRecipeWithInput(current.GetKitchenObjectSO()))
            return;
        _resolver.EnsureResolved(_inputSOs);
        if (!_resolver.IsCutting || !_resolver.HasValidRecipe)
            return;

        _netCookingTimer.Value += cuttingSpeed;
        _clock.Reset(_netCookingTimer.Value, _netBurningTimer.Value);
        CutFeedbackClientRpc();

        if (_netCookingTimer.Value >= _resolver.CookingTimeMax)
        {
            KitchenObjectSO output = _resolver.CookingOutput;
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
            RebuildInputs();
            _resolver.ResolveFor(_inputSOs);
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
            RebuildInputs();
            KitchenObjectSO currentSO = _slot.Current != null ? _slot.Current.GetKitchenObjectSO() : null;
            _resolver.EnsureCombine(currentSO, _netCombineRecipeIndex.Value);
            _resolver.EnsureResolved(_inputSOs);
            if (!_resolver.HasValidRecipe)
                return;
            // Cutting is driven by clicks, never by the timed cooking clock.
            if (_resolver.IsCutting)
                state = State.Idle;
        }
        _netCookingTimer.Value = 0f;
        _netBurningTimer.Value = 0f;
        _clock.Reset(0f, 0f);
        _presenter.Reset();
        _netState.Value = state;
        RefreshFoodVisibility();
    }

    public void FireOnStateChange()
    {
        _presenter.OnStateChanged(_netState.Value, transform.position);
    }

    private void Update()
    {
        if (!_slot.HasFood)
        {
            TryResolveFoodsFromNetwork();
            if (!_slot.HasFood)
                return;
        }

        RebuildInputs();
        KitchenObjectSO currentSO = _slot.Current != null ? _slot.Current.GetKitchenObjectSO() : null;
        _resolver.EnsureCombine(currentSO, _netCombineRecipeIndex.Value);
        _resolver.EnsureResolved(_inputSOs);
        // Defensive: an old Cooking state on a cutting tool must not advance the timer.
        State clockState = _netState.Value == State.Cooking && _resolver.IsCutting
            ? State.Idle
            : _netState.Value;
        _clock.Tick(clockState, Time.deltaTime, IsServer, _netCookingTimer.Value, _netBurningTimer.Value);

        if (IsServer)
            AdvanceCooking();

        UpdateUI();
    }

    private void AdvanceCooking()
    {
        switch (_netState.Value)
        {
            case State.Idle:
                break;
            case State.Cooking:
                _resolver.EnsureResolved(_inputSOs);
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
                    if (output == null || !_slot.HasFood)
                    {
                        _resolver.ResetSlot();
                        _clock.Reset(_netCookingTimer.Value, _netBurningTimer.Value);
                        _presenter.Reset();
                        break;
                    }
                    ConsumeInputsAndSpawnOutput(output);
                    _netState.Value = State.Cooked;
                    _netBurningTimer.Value = 0f;
                    _clock.Reset(_clock.LocalCook, 0f);
                    _presenter.Reset();
                    _resolver.ResolveBurning(FirstInputSO);
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
                    if (output == null || !_slot.HasFood)
                        break;
                    ConsumeInputsAndSpawnOutput(output);
                    _netState.Value = State.Burned;
                }
                break;
            case State.Burned:
                break;
        }
    }

    // Server only. Replaces every input with the finished item (the Blender eats all of them).
    private void ConsumeInputsAndSpawnOutput(KitchenObjectSO output)
    {
        List<KitchenObject> inputs = _slot.CopyFoods();
        for (int i = 0; i < inputs.Count; i++)
            if (inputs[i] != null) inputs[i].DestroySelf();
        KitchenObject.SpawnKitchenObject(output, this);
    }

    private void UpdateUI()
    {
        if ((_netState.Value == State.Idle || _netState.Value == State.Cooking) && _slot.HasFood)
        {
            _resolver.EnsureResolved(_inputSOs);
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
        if (index < 0) index = 0;
        if (kitchenObject == null)
        {
            RefreshFoodVisibility();
            return;
        }
        if (_slot.Get(index) == kitchenObject)
        {
            RefreshFoodVisibility();
            return;
        }

        _slot.Set(kitchenObject, index);
        RebuildInputs();
        _resolver.ResetSlot();
        if (IsServer && _netCuttingRecipeIndex.Value != -1)
            _netCuttingRecipeIndex.Value = -1;
        if (_inputSOs.Count > 0)
        {
            _resolver.ResolveFor(_inputSOs);
            if (!IsServer && _netCuttingRecipeIndex.Value >= 0)
                _resolver.ApplyCuttingOption(FirstInputSO, _netCuttingRecipeIndex.Value);
            _resolver.EnsureBurning(FirstInputSO);
        }
        _clock.Reset(_netCookingTimer.Value, _netBurningTimer.Value);
        _presenter.Reset();
        _presenter.HideCombineDetail();
        progressBarUI?.Hide();
        burnWarningUI?.Hide();
        if (IsServer && kitchenObject != null && kitchenObject.NetworkObject != null)
            SetNetworkFood(index, kitchenObject.NetworkObject);
        RefreshFoodVisibility();
    }

    public Transform GetKitchenObjectFollowTransform(int index = 0)
    {
        EnsurePlacePoints();
        if (_slotPoints != null && index >= 0 && index < _slotPoints.Length && _slotPoints[index] != null)
            return _slotPoints[index];
        return placePoint;
    }

    public KitchenObject GetKitchenObject(int index = 0)
    {
        return _slot.Get(index);
    }

    public void ClearKitchenObject(int index = 0)
    {
        if (_slot.Get(index) == null)
            return;

        _slot.ClearAt(index);
        RebuildInputs();
        _resolver.ResetSlot();
        if (_inputSOs.Count > 0)
        {
            _resolver.ResolveFor(_inputSOs);
            _resolver.EnsureBurning(FirstInputSO);
        }
        _clock.Reset(_netCookingTimer.Value, _netBurningTimer.Value);
        _presenter.Reset();
        _presenter.HideCombineDetail();
        progressBarUI?.Hide();
        burnWarningUI?.Hide();

        if (IsServer)
        {
            ClearNetworkFood(index);
            if (_inputSOs.Count == 0)
            {
                if (_netCombineRecipeIndex.Value != -1)
                    _netCombineRecipeIndex.Value = -1;
                if (_netCuttingRecipeIndex.Value != -1)
                    _netCuttingRecipeIndex.Value = -1;
            }
        }
        RefreshFoodVisibility();
    }

    public bool HasKitchenObject(int index = 0)
    {
        return _slot.Get(index) != null;
    }

    private void SetNetworkFood(int index, NetworkObjectReference reference)
    {
        if (!IsServer) return;
        while (_netFoods.Count <= index)
            _netFoods.Add(default);
        _netFoods[index] = reference;
    }

    private void ClearNetworkFood(int index)
    {
        if (!IsServer) return;
        if (index < 0 || index >= _netFoods.Count) return;
        _netFoods[index] = default;
        while (_netFoods.Count > 0 && !_netFoods[_netFoods.Count - 1].TryGet(out _))
            _netFoods.RemoveAt(_netFoods.Count - 1);
    }

    // Builds the drop points for multi-ingredient tools once. The base prefab only ships a
    // single PlacePoint, so extra slots are offset from it instead of requiring prefab work.
    private void EnsurePlacePoints()
    {
        int needed = MaxFoodSlots;
        if (needed <= 1 || placePoint == null)
        {
            if (_slotPoints == null || _slotPoints.Length != 1)
                _slotPoints = new[] { placePoint };
            return;
        }
        if (_slotPoints != null && _slotPoints.Length >= needed)
            return;

        Transform[] points = new Transform[needed];
        for (int i = 0; i < needed; i++)
        {
            if (i == 0)
            {
                points[0] = placePoint;
                continue;
            }
            if (extraPlacePoints != null && i < extraPlacePoints.Count && extraPlacePoints[i] != null)
            {
                points[i] = extraPlacePoints[i];
                continue;
            }
            if (_slotPoints != null && i < _slotPoints.Length && _slotPoints[i] != null)
            {
                points[i] = _slotPoints[i];
                continue;
            }
            Transform point = new GameObject($"{name} PlacePoint {i}").transform;
            point.SetParent(placePoint, false);
            // Extras sit on a ring around the main point so several ingredients stay
            // readable on top of the tool instead of overlapping in a single line.
            int extraIndex = i - 1;
            int extraCount = needed - 1;
            float angle = 2f * Mathf.PI * extraIndex / extraCount;
            point.localPosition = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * multiSlotSpacing;
            points[i] = point;
        }
        _slotPoints = points;
    }

    // IHasProgress
    public bool IsDone()
    {
        return _netState.Value == State.Cooked;
    }

    public float GetProgress()
    {
        if ((_netState.Value == State.Idle || _netState.Value == State.Cooking) && _slot.HasFood)
        {
            _resolver.EnsureResolved(_inputSOs);
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


    // Server only: picks a cutting option or the combine option for the food in the slot.
    // Returns false for a bad index or when cutting has already started.
    public bool ApplyOption(int index)
    {
        if (!IsServer || !_slot.HasFood)
            return false;
        KitchenObjectSO input = _slot.Current.GetKitchenObjectSO();
        if (_resolver.HasCuttingOptions(input))
        {
            // The choice is made when the food is placed; do not let a late RPC replace
            // the recipe after cutting has started.
            if (_netCookingTimer.Value > 0f || !_resolver.ApplyCuttingOption(input, index))
                return false;
            _netCuttingRecipeIndex.Value = index;
            return true;
        }
        if (!_resolver.ApplyOption(input, index))
            return false;
        _netCombineRecipeIndex.Value = index;
        return true;
    }

    // Server: shows the cutting options, or the combine options when this tool offers them.
    public void ShowOptionMenu(PlayerStateMachine actor, IHasOptionalSO sender, KitchenObjectSO input)
    {
        List<CuttingRecipeSO> cuttingOptions = _resolver.GetCuttingOptions(input);
        if (cuttingOptions != null && cuttingOptions.Count > 1)
        {
            var outputs = new List<KitchenObjectSO>(cuttingOptions.Count);
            foreach (CuttingRecipeSO recipe in cuttingOptions)
            {
                if (recipe != null && recipe.output != null)
                    outputs.Add(recipe.output);
            }
            if (outputs.Count > 1)
            {
                UIManager.Instance.ShowOptionMenu(actor, sender, outputs, CookingPresenter.OptionTitle);
                return;
            }
        }
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
        return !HasAnyFood;
    }
}
