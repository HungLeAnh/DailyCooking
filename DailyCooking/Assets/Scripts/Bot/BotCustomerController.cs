using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;

public class BotCustomerController : NetworkBehaviour,IInteractable,IHighlightable
{
    public Action<float> OnClockTimerChanged;
    public Action<EmotionType> OnEmotionChanged;
    public Action<PlayerStateMachine> OnInteract;
    [SerializeField] private NetworkAnimator networkAnimator;
    [SerializeField] private NavMeshAgent navMeshAgent;
    [SerializeField] private GameObject foodBubble;
    [SerializeField] private GameObject orderBubble;
    [SerializeField] private GameObject emotionBubble;
    [SerializeField] private GameObject BubbleFrame;

    [SerializeField] private BubbleEmotionUI bubbleEmotionUI;
    [SerializeField] private BubbleFoodUI bubbleFoodUI;

    [SerializeField] private GameObject visual;
    [SerializeField] private SkinnedMeshRenderer[] highlightGameObjectArray;

    private BotStateMachine stateMachine;
    // The order, replicated as food guids (menu indices shift when the menu changes);
    // waitingFood is the local resolved copy used by the bubble and the serve check.
    private NetworkList<FixedString64Bytes> orderedFoodGuids;
    private List<FoodSO> waitingFood;
    private float clockTimerMax = GameDefine.EMOTION_DURATION;
    private NetworkVariable<float> clockTimer = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // The patience timer counts on the server every frame but is only sent a few times a second;
    // writing the NetworkVariable each frame would send an update per bot per tick.
    private const float CLOCK_SYNC_INTERVAL = 0.25f;
    private float serverClockTimer;
    private float clockSyncCountdown;
    private NetworkVariable<float> tipPercentage = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isActiveInGame = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isBubbleFrameActive = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isFoodBubbleActive = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isOrderBubbleActive = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isEmotionBubbleActive = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isNavMeshStopped = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<EmotionType> currentEmotion = new NetworkVariable<EmotionType>(EmotionType.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> targetSeatIndex = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<ulong> targetTableNetworkVariable = new NetworkVariable<ulong>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<BotStateType> currentStateType = new NetworkVariable<BotStateType>(BotStateType.Idle, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Vector3> roamPosX = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Vector3> roamPosZ = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


    private Table targetTable = null;
    private int _poolIndex = -1;
    private HighlightMaterials highlight;
    public Table TargetTable { get => targetTable; set => targetTable = value; }
    public NetworkVariable<ulong> TargetTableNetworkVariable { get => targetTableNetworkVariable; set => targetTableNetworkVariable.Value = value.Value;}
    public NetworkVariable<int> TargetSeatIndex { get => targetSeatIndex; set => targetSeatIndex = value;}
    public NavMeshAgent NavMeshAgent { get => navMeshAgent; }
    public NetworkVariable<bool> IsActiveInGame { get => isActiveInGame; set => isActiveInGame = value; }
    public NetworkVariable<BotStateType> CurrentStateType { get => currentStateType; set => currentStateType = value; }
    public Vector3 RoamPosX { get => roamPosX.Value; set => roamPosX.Value = value; }
    public Vector3 RoamPosZ { get => roamPosZ.Value; set => roamPosZ.Value = value; }
    public int PoolIndex { get => _poolIndex; set => _poolIndex = value; }
    private HighlightMaterials Highlight => highlight ??= new HighlightMaterials(highlightGameObjectArray);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        IsActiveInGame.OnValueChanged += HandleActiveChanged;
        isEmotionBubbleActive.OnValueChanged += HandleEmotionBubbleChanged;
        isFoodBubbleActive.OnValueChanged += HandleFoodBubbleChanged;
        isOrderBubbleActive.OnValueChanged += HandleOrderBubbleChanged;
        isBubbleFrameActive.OnValueChanged += HandleBubbleFrameChanged;
        isNavMeshStopped.OnValueChanged += HandleNavMeshStoppedChanged;
        currentEmotion.OnValueChanged += HandleEmotionChanged;
        clockTimer.OnValueChanged += HandleClockChanged;
        targetTableNetworkVariable.OnValueChanged += HandleTargetTableChanged;
        currentStateType.OnValueChanged += HandleStateChanged;
        orderedFoodGuids.OnListChanged += OrderedFoodGuids_OnListChanged;
        RebuildWaitingFood();

        SetVisualActive(IsActiveInGame.Value);
        BubbleFrame.SetActive(isBubbleFrameActive.Value);
        orderBubble.SetActive(isOrderBubbleActive.Value);
        foodBubble.SetActive(isFoodBubbleActive.Value);
        emotionBubble.SetActive(isEmotionBubbleActive.Value);
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetTableNetworkVariable.Value, out NetworkObject netObj))
        {
            //Debug.Log("Found object: " + netObj.name);
            if (netObj.TryGetComponent(out Table table))
            {
                targetTable = table;
                //Debug.Log("Bot " + gameObject.name + " is assigned to Table " + table.gameObject.name + " Seat Index: " + TargetSeatIndex);
            }
            else
            {
                targetTable = null;

            }
        }
        SetStateMachineState(currentStateType.Value);
    }

    private void HandleActiveChanged(bool oldVal, bool newVal) => SetVisualActive(newVal);
    private void HandleEmotionBubbleChanged(bool oldVal, bool newVal) => emotionBubble.SetActive(newVal);
    private void HandleFoodBubbleChanged(bool oldVal, bool newVal) => foodBubble.SetActive(newVal);
    private void HandleOrderBubbleChanged(bool oldVal, bool newVal) => orderBubble.SetActive(newVal);
    private void HandleBubbleFrameChanged(bool oldVal, bool newVal) => BubbleFrame.SetActive(newVal);
    private void HandleNavMeshStoppedChanged(bool oldVal, bool newVal)
    {
        if (NavMeshAgent == null) return;
        NavMeshAgent.isStopped = newVal;
        NavMeshAgent.updatePosition = !newVal;
        NavMeshAgent.updateRotation = !newVal;
    }
    private void HandleEmotionChanged(EmotionType oldVal, EmotionType newVal) => OnEmotionChanged?.Invoke(newVal);
    private void HandleClockChanged(float oldVal, float newVal) => OnClockTimerChanged?.Invoke((clockTimerMax - newVal) / clockTimerMax);
    // Server only.
    private void ResetClockTimer()
    {
        serverClockTimer = 0f;
        clockSyncCountdown = CLOCK_SYNC_INTERVAL;
        clockTimer.Value = 0f;
    }
    private void HandleTargetTableChanged(ulong oldVal, ulong newVal)
    {
        if (NetworkManager.Singleton == null) return;
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(newVal, out NetworkObject netObj)
            && netObj.TryGetComponent(out Table table))
        {
            targetTable = table;
        }
        else
        {
            targetTable = null;
        }
    }
    private void HandleStateChanged(BotStateType oldVal, BotStateType newVal) => SetStateMachineState(newVal);

    public override void OnNetworkDespawn()
    {
        IsActiveInGame.OnValueChanged -= HandleActiveChanged;
        isEmotionBubbleActive.OnValueChanged -= HandleEmotionBubbleChanged;
        isFoodBubbleActive.OnValueChanged -= HandleFoodBubbleChanged;
        isOrderBubbleActive.OnValueChanged -= HandleOrderBubbleChanged;
        isBubbleFrameActive.OnValueChanged -= HandleBubbleFrameChanged;
        isNavMeshStopped.OnValueChanged -= HandleNavMeshStoppedChanged;
        currentEmotion.OnValueChanged -= HandleEmotionChanged;
        clockTimer.OnValueChanged -= HandleClockChanged;
        targetTableNetworkVariable.OnValueChanged -= HandleTargetTableChanged;
        currentStateType.OnValueChanged -= HandleStateChanged;
        orderedFoodGuids.OnListChanged -= OrderedFoodGuids_OnListChanged;
        base.OnNetworkDespawn();
    }
    private void Awake()
    {
        stateMachine = new BotStateMachine(this);

        if (IsServer)
        {
            isFoodBubbleActive.Value = false;
            isOrderBubbleActive.Value = false;
            isEmotionBubbleActive.Value = false;
            isBubbleFrameActive.Value = false;

        }
        waitingFood = new List<FoodSO>();
        orderedFoodGuids = new NetworkList<FixedString64Bytes>();


        OnDeselected();
    }

    public void PlayAnimation(BotAnimation.State animationState)
    {
        if (!IsServer) return;
        networkAnimator.Animator.StopPlayback();
        networkAnimator.Animator.Play(animationState.ToString());
    }

    private void Update()
    {
        if(!IsServer) return;

        stateMachine.Update();
        if(currentEmotion.Value != EmotionType.None)
        {
            if (serverClockTimer < clockTimerMax)
            {
                serverClockTimer += Time.deltaTime;
                clockSyncCountdown -= Time.deltaTime;
                if (clockSyncCountdown <= 0f || serverClockTimer >= clockTimerMax)
                {
                    clockSyncCountdown = CLOCK_SYNC_INTERVAL;
                    clockTimer.Value = serverClockTimer;
                }
                if ((clockTimerMax - serverClockTimer) / clockTimerMax <= 0)
                {
                    SetNextEmotion();
                }
            }
        }
    }
    public bool IsServerCorrectFood(TablewareKitchenObject tablewareKitchenObject)
    {
        foreach (FoodSO waitingFood in waitingFood)
        {
            if (waitingFood.kitchenObjectSOList.Count == tablewareKitchenObject.GetKitchenObjectSOList().Count)
            {
                //Has the same number of ingredients
                bool plateContentMathesRecipe = true;

                foreach (KitchenObjectSO recipeKitchenObjectSO in waitingFood.kitchenObjectSOList)
                {
                    //Cycling through all ingredients in recipe
                    bool ingredientFound = false;
                    foreach (KitchenObjectSO plateKitchenObjectSO in tablewareKitchenObject.GetKitchenObjectSOList())
                    {
                        //Cycling through all ingredients in recipe
                        if (plateKitchenObjectSO == recipeKitchenObjectSO)
                        {
                            ingredientFound = true;
                            break;
                        }
                    }
                    if (!ingredientFound)
                    {
                        // This Recipe ingredient was not found on the plate
                        plateContentMathesRecipe = false;

                    }
                }

                if (plateContentMathesRecipe)
                {
                    return plateContentMathesRecipe;
                }
            }
        }

        //No matches found
        //Player did not deliver correct recipe
        return false;
    }
    private void SetNextEmotion()
    {
        EmotionType nextEmotion = EmotionManager.Instance.GetNextEmotion(currentEmotion.Value);
        if (nextEmotion == EmotionType.None && currentEmotion.Value != EmotionType.None)
        {
            currentEmotion.Value = EmotionType.None;
            Leave();
            return;
        }
        if (currentEmotion.Value == EmotionType.None)
        {
            return;
        }

        ResetClockTimer();
        currentEmotion.Value = nextEmotion;
        UpdateTipPercentage(currentEmotion.Value);
    }

    private void UpdateTipPercentage(EmotionType type)
    {
        switch (type)
        {
            case EmotionType.Happy:
                break;
            case EmotionType.Sad:
                tipPercentage.Value -= tipPercentage.Value * 0.2f;
                break;
            case EmotionType.Angry:
                if (stateMachine.CurrentState is WaitingForFoodState)
                    tipPercentage.Value = 0f;
                else
                    tipPercentage.Value -= tipPercentage.Value * 0.5f;
                break;
        }
    }
    // Server only (bot states run on the server): take the order and wait for it.
    public void OrderFoodAndWait()
    {
        if (!IsServer) return;
        if (OrderFood())
        {
            SetCurrentState(BotStateType.WaitingForFood);
        }
    }

    // Server only.
    public bool OrderFood()
    {
        var food = KitchenGameManager.Instance.GetUnlockedFood();
        if(food == null)
        {
            Leave();
            return false;
        }
        orderedFoodGuids.Add(food.Guid);

        isBubbleFrameActive.Value = true;
        isOrderBubbleActive.Value = false;
        isFoodBubbleActive.Value = true;
        currentEmotion.Value = EmotionType.Happy;
        ResetClockTimer();

        return true;
    }
    private void OrderedFoodGuids_OnListChanged(NetworkListEvent<FixedString64Bytes> changeEvent)
    {
        RebuildWaitingFood();
    }
    private void RebuildWaitingFood()
    {
        waitingFood.Clear();
        foreach (FixedString64Bytes guid in orderedFoodGuids)
        {
            FoodSO food = ConfigManager.Instance.ConfigFood.FoodItems.Find(x => x.Guid == guid.ToString());
            if (food != null)
                waitingFood.Add(food);
        }
        bubbleFoodUI.SetOrder(waitingFood);
    }
    // Server only.
    public void ShowOrder()
    {
        if (!IsServer) return;
        isBubbleFrameActive.Value = true;
        isOrderBubbleActive.Value = true;
        isEmotionBubbleActive.Value = true;
        currentEmotion.Value = EmotionType.Happy;
    }
    public void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        OnInteract?.Invoke(playerStateMachine);
        //Debug.Log("Bot Interacted with player");
        //Debug.Log("Current Bot State: " + stateMachine.CurrentState.GetType().Name);
    }

    public void InteractAlternateEvent(PlayerStateMachine playerStateMachine)
    {
        
    }

    public void ResetBot()
    {
        targetTableNetworkVariable.Value = 0;
        targetSeatIndex.Value = -1;
        orderedFoodGuids.Clear();
        StopBubble();
        currentStateType.Value = BotStateType.Idle;
    }
    public void InitBot(Vector3 roamPositionX, Vector3 roamPositionZ)
    {
        roamPosX.Value = roamPositionX;
        roamPosZ.Value = roamPositionZ;
        isNavMeshStopped.Value = false;
        currentStateType.Value = BotStateType.WaitForTable;
        PlayerStats hostStats = GameManager.Instance.GameData.GetPlayerStatsById(SessionManager.Instance.PlayerId);
        tipPercentage.Value = GameDefine.TIP_PERCENTAGE + (hostStats != null ? hostStats.TipIncrease : 0f);
        var spawnPos = UnityEngine.Random.Range(0, 2) == 0 ? roamPositionX : roamPositionZ;
        navMeshAgent.Warp(spawnPos);
        GetComponent<Unity.Netcode.Components.NetworkTransform>().Teleport(spawnPos, transform.rotation, transform.localScale);
        IsActiveInGame.Value = true;
    }
    // Server only.
    public void StopBubble()
    {
        if (!IsServer) return;
        isFoodBubbleActive.Value = false;
        isOrderBubbleActive.Value = false;
        isEmotionBubbleActive.Value = false;
        isBubbleFrameActive.Value = false;
    }

    public void OnSelected()
    {
        ShowHighlight();
    }

    public void OnDeselected()
    {
        HideHighlight();
    }
    public void ShowHighlight()
    {
        Highlight.SetActive(true);
    }
    public void HideHighlight()
    {
        Highlight.SetActive(false);
    }

    public override void OnDestroy()
    {
        highlight?.Release();
        base.OnDestroy();
    }
    public void SetVisualActive(bool active)
    {
        visual.SetActive(active);
    }

    // Server only: the customer pays once; the plate keeps the payment until a player collects it.
    public void FinishEating()
    {
        if (!IsServer) return;
        int cash = 0;
        int exp = 0;
        foreach (var food in waitingFood)
        {
            cash += (int)food.price;
            exp += food.exp;
        }
        cash += (int)(cash * tipPercentage.Value);
        if (targetTable != null)
            targetTable.SetEatenViual(TargetSeatIndex.Value, cash, exp);
        ResetSeat();
    }
    public void StopNavMesh()
    {
        if (IsServer)
            isNavMeshStopped.Value = true;
    }
    public void StartNavMesh()
    {
        if(IsServer)
            isNavMeshStopped.Value = false;    
    }

    public void ResetSeat()
    {
        if(targetTable != null && TargetSeatIndex.Value >= 0)
        {
            targetTable.ResetSeat(TargetSeatIndex.Value);
        }
    }
    public void Leave()
    {
        ResetSeat();
        currentStateType.Value = BotStateType.Leaving;
        StopBubble();
    }
    // Server only: the state is replicated through currentStateType.
    public void SetCurrentState(BotStateType botStateType)
    {
        if (!IsServer) return;
        CurrentStateType.Value = botStateType;
    }
    // The state machine (pathing, seats, orders) runs on the server only; clients get the bot's
    // position, animation and bubbles from NetworkTransform/NetworkAnimator/NetworkVariables.
    private void SetStateMachineState(BotStateType botStateType)
    {
        if (!IsServer)
            return;
        switch (botStateType)
        {
            case BotStateType.Idle:
                stateMachine.SetState(new BotIdleState(stateMachine));
                break;
            case BotStateType.WaitForTable:
                stateMachine.SetState(new WaitForTableState(stateMachine));
                break;
            case BotStateType.WalkToTable:
                stateMachine.SetState(new WalkToTableState(stateMachine));
                break;
            case BotStateType.OrderFood:
                stateMachine.SetState(new OrderFoodState(stateMachine));
                break;
            case BotStateType.WaitingForFood:
                stateMachine.SetState(new WaitingForFoodState(stateMachine));
                break;
            case BotStateType.Eating:
                stateMachine.SetState(new EatingState(stateMachine));
                break;
            case BotStateType.Leaving:
                stateMachine.SetState(new LeavingState(stateMachine));
                break;
        }
    }
    // Server only.
    public void SetSeat(Table table, int seatIndex)
    {
        if (!IsServer || table == null) return;
        targetTableNetworkVariable.Value = table.NetworkObjectId;
        targetSeatIndex.Value = seatIndex;
    }
}