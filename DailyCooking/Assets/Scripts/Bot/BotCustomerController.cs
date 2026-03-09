using System;
using System.Collections.Generic;
using System.Net.WebSockets;
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
    [SerializeField] private GameObject[] highlightGameObjectArray;

    private BotStateMachine stateMachine;
    private List<FoodSO> waitingFood;
    private float clockTimerMax = GameDefine.EMOTION_DURATION;
    private NetworkVariable<float> clockTimer = new NetworkVariable<float>(0f);
    private NetworkVariable<float> tipPercentage = new NetworkVariable<float>();
    private NetworkVariable<bool> isActiveInGame = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> isBubbleFrameActive = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> isFoodBubbleActive = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> isOrderBubbleActive = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> isEmotionBubbleActive = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> isNavMeshStopped = new NetworkVariable<bool>(false);
    private NetworkVariable<EmotionType> currentEmotion = new NetworkVariable<EmotionType>(EmotionType.None);
    private NetworkVariable<BotStateType> currentStateType = new NetworkVariable<BotStateType>(BotStateType.Idle);

    public Table TargetTable { get; set; }
    public int TargetSeatIndex { get; set; }
    public NavMeshAgent NavMeshAgent { get => navMeshAgent; }
    public NetworkVariable<bool> IsActiveInGame { get => isActiveInGame; set => isActiveInGame = value; }
    public NetworkVariable<BotStateType> CurrentStateType { get => currentStateType; set => currentStateType = value; }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();        

        IsActiveInGame.OnValueChanged += (oldVal, newVal) => {
            SetVisualActive(newVal);
        };
        isEmotionBubbleActive.OnValueChanged += (oldVal, newVal) => {
            emotionBubble.SetActive(newVal);
        };
        isFoodBubbleActive.OnValueChanged += (oldVal, newVal) => {
            foodBubble.SetActive(newVal);
        };
        isOrderBubbleActive.OnValueChanged += (oldVal, newVal) => {
            orderBubble.SetActive(newVal);
        };
        isBubbleFrameActive.OnValueChanged += (oldVal, newVal) => {
            BubbleFrame.SetActive(newVal);
        };
        isNavMeshStopped.OnValueChanged += (oldVal, newVal) => {
            NavMeshAgent.isStopped = isNavMeshStopped.Value;
            NavMeshAgent.updatePosition = !isNavMeshStopped.Value;
            NavMeshAgent.updateRotation = !isNavMeshStopped.Value;
            
        };
        currentEmotion.OnValueChanged += (oldVal, newVal) => {
            OnEmotionChanged?.Invoke(newVal);
        };
        clockTimer.OnValueChanged += (oldVal, newVal) => {
            OnClockTimerChanged?.Invoke((clockTimerMax - newVal) / clockTimerMax);
        };
        currentStateType.OnValueChanged += (oldVal, newVal) => {
            SetStateMachineStateClientRpc(newVal);
        };
        SetVisualActive(IsActiveInGame.Value);
        BubbleFrame.SetActive(isBubbleFrameActive.Value);
        orderBubble.SetActive(isOrderBubbleActive.Value);
        foodBubble.SetActive(isFoodBubbleActive.Value);
        emotionBubble.SetActive(isEmotionBubbleActive.Value);
        SetStateMachineState(currentStateType.Value);
    }
    private void Awake()
    {
        stateMachine = new BotStateMachine(this);

        if (IsHost||IsServer)
        {
            isFoodBubbleActive.Value = false;
            isOrderBubbleActive.Value = false;
            isEmotionBubbleActive.Value = false;
            isBubbleFrameActive.Value = false;

        }
        waitingFood = new List<FoodSO>();


        OnDeselected();
    }

    public void PlayAnimation(BotAnimation.State animationState)
    {
        if (!IsHost || !IsServer) return;
        networkAnimator.Animator.StopPlayback();
        networkAnimator.Animator.Play(animationState.ToString());
    }

    private void Update()
    {
        if(!IsHost || !IsServer) return;

        stateMachine.Update();
        if(currentEmotion.Value != EmotionType.None)
        {
            if (clockTimer.Value < clockTimerMax)
            {
                clockTimer.Value += Time.deltaTime;
                if ((clockTimerMax - clockTimer.Value) / clockTimerMax <= 0)
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

        clockTimer.Value = 0f;
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
    public bool OrderFood()
    {
        var food = KitchenGameManager.Instance.GetUnlockedFood();
        if(food == null)
        {
            Leave();
            return false;
        }
        var foodIndex = KitchenGameManager.Instance.GetFoodIndex(food);
        OrderFoodClientRpc(foodIndex);

        return true;
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void OrderFoodClientRpc(int foodIndex)
    {
        var food = KitchenGameManager.Instance.GetFoodByIndex(foodIndex);
        waitingFood.Add(food);

        bubbleFoodUI.SetOrder(waitingFood);
        if(IsServer || IsHost)
        {
            isBubbleFrameActive.Value = true;
            isOrderBubbleActive.Value = false;
            isFoodBubbleActive.Value = true;
            currentEmotion.Value = EmotionType.Happy;
            clockTimer.Value = 0f;
        }

    }
    public void ShowOrder()
    {
        ShowOrderServerRpc();
    }
    [Rpc(SendTo.Server)]
    private void ShowOrderServerRpc()
    {
        isBubbleFrameActive.Value = true;
        isOrderBubbleActive.Value = true;
        isEmotionBubbleActive.Value = true;
        currentEmotion.Value = EmotionType.Happy;
    }
    public void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        OnInteract?.Invoke(playerStateMachine);
        Debug.Log("Bot Interacted with player");
        Debug.Log("Current Bot State: " + stateMachine.CurrentState.GetType().Name);
    }

    public void InteractAlternateEvent(PlayerStateMachine playerStateMachine)
    {
        
    }

    public void ResetBot()
    {
        ResetBotClientRpc();
        StopBubbleServerRpc();
        currentStateType.Value = BotStateType.Idle;
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void ResetBotClientRpc()
    {
        waitingFood.Clear();
        TargetTable = null;
        TargetSeatIndex = -1;
    }
    public void InitBot()
    {
        isNavMeshStopped.Value = false;
        currentStateType.Value = BotStateType.WaitForTable;
        tipPercentage.Value = GameDefine.TIP_PERCENTAGE + GameManager.Instance.GameData.PlayerStats.statsData.TipIncrease;
    }
    [Rpc(SendTo.Server)]
    public void StopBubbleServerRpc()
    {
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
        foreach (var visualGameObject in highlightGameObjectArray)
        {
            visualGameObject.SetActive(true);
        }

    }
    public void HideHighlight()
    {
        foreach (var visualGameObject in highlightGameObjectArray)
        {
            visualGameObject.SetActive(false);
        }
    }
    public void SetVisualActive(bool active)
    {
        visual.SetActive(active);
    }

    public void FinishEating()
    {
        int cash = 0;
        int exp = 0;
        foreach (var food in waitingFood)
        {
            cash += (int)food.price;
            exp += food.exp;
        }
        cash += (int)(cash * tipPercentage.Value);
        TargetTable.SetEatenVisualServerRpc(TargetSeatIndex,cash,exp);
        ResetSeat();
    }
    [Rpc(SendTo.Server)]
    public void FinishEatingServerRpc()
    {
        FinishEatingClientRpc();
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void FinishEatingClientRpc()
    {
        FinishEating();
    }
    public void StopNavMesh()
    {
        if (IsHost || IsServer)
            isNavMeshStopped.Value = true;
    }
    public void StartNavMesh()
    {
        if(IsHost || IsServer)
            isNavMeshStopped.Value = false;    
    }

    public void ResetSeat()
    {
        if(TargetTable != null && TargetSeatIndex >= 0)
            TargetTable.ResetSeatServerRpc(TargetSeatIndex);
    }
    public void Leave()
    {
        ResetSeat();
        currentStateType.Value = BotStateType.Leaving;
        StopBubbleServerRpc();
    }
    [Rpc(SendTo.Server)]
    public void SetCurrentStateServerRpc(BotStateType botStateType)
    {
        CurrentStateType.Value = botStateType;
    }
    [Rpc(SendTo.ClientsAndHost)]
    public void SetStateMachineStateClientRpc(BotStateType botStateType)
    {
        SetStateMachineState(botStateType);
    }
    private void SetStateMachineState(BotStateType botStateType)
    {
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
    [Rpc(SendTo.Server)]
    public void SetSeatServerRpc(NetworkBehaviourReference networkBehaviourReference, int seatIndex)
    {
        SetSeatClientRpc(networkBehaviourReference, seatIndex);
    }
    [Rpc(SendTo.ClientsAndHost)]
    public void SetSeatClientRpc(NetworkBehaviourReference networkBehaviourReference, int seatIndex)
    {
        if (networkBehaviourReference.TryGet(out Table table)) {
            TargetTable = table;
            Debug.Log("Bot " + gameObject.name + " is assigned to Table " + table.gameObject.name + " Seat Index: " + seatIndex);
            TargetSeatIndex = seatIndex;
        }
        else
        {
            Debug.LogError("Failed to get Table from NetworkBehaviourReference");
        }
    }
}