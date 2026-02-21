using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;

public class BotCustomerController : NetworkBehaviour,IInteractable,IHighlightable
{
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
    private float tipPercentage;
    private NetworkVariable<bool> isActiveInGame = new NetworkVariable<bool>(false);

    public Table TargetTable { get; set; }
    public int TargetSeatIndex { get; set; }
    public float TipPercentage { get => tipPercentage; }
    public NavMeshAgent NavMeshAgent { get => navMeshAgent; }
    public NetworkVariable<bool> IsActiveInGame { get => isActiveInGame; set => isActiveInGame = value; }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        IsActiveInGame.OnValueChanged += (oldVal, newVal) => {
            SetVisualActive(newVal);
        };
        SetVisualActive(IsActiveInGame.Value);
    }
    private void Awake()
    {
        stateMachine = new BotStateMachine(this);

        foodBubble.SetActive(false);
        orderBubble.SetActive(false);
        emotionBubble.SetActive(false);
        BubbleFrame.SetActive(false);
        waitingFood = new List<FoodSO>();
        bubbleEmotionUI.OnEmotionEnd += OnEmotionEnd;
        bubbleEmotionUI.OnEmotionChanged += OnEmotionChanged;

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
        stateMachine.Update();
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
    private void OnEmotionEnd(EmotionType emotionType)
    {
        //Debug.LogError("BotCustomerController: OnEmotionEnd called");   
        Leave();
    }
    private void OnEmotionChanged(EmotionType type)
    {
        switch (type)
        {
            case EmotionType.Happy:
                break;
            case EmotionType.Sad:
                tipPercentage -= tipPercentage * 0.2f;
                break;
            case EmotionType.Angry:
                if (stateMachine.CurrentState is WaitingForFoodState)
                    tipPercentage = 0f;
                else
                    tipPercentage -= tipPercentage * 0.5f;
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

        waitingFood.Add(food);

        BubbleFrame.SetActive(true);
        orderBubble.SetActive(false);
        foodBubble.SetActive(true);

        bubbleFoodUI.SetOrder(waitingFood);
        bubbleEmotionUI.StartEmotion();
        return true;
    }
    public void ShowOrder()
    {
        BubbleFrame.SetActive(true);
        orderBubble.SetActive(true);
        emotionBubble.SetActive(true);
        bubbleEmotionUI.StartEmotion();
        
    }
    public void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        OnInteract?.Invoke(playerStateMachine);
    }

    public void InteractAlternateEvent(PlayerStateMachine playerStateMachine)
    {
        
    }

    public void ResetBot()
    {
        TargetTable = null;
        TargetSeatIndex = -1;
        waitingFood.Clear();
        StopBubble();
        stateMachine.SetState(new BotIdleState(stateMachine));
    }
    public void InitBot()
    {
        navMeshAgent.isStopped = false;
        stateMachine.SetState(new WaitForTableState(stateMachine));
        tipPercentage = GameDefine.TIP_PERCENTAGE + GameManager.Instance.GameData.PlayerStats.statsData.TipIncrease;
    }

    public void StopBubble()
    {
        bubbleEmotionUI.StopEmotion();

        foodBubble.SetActive(false);
        orderBubble.SetActive(false);
        emotionBubble.SetActive(false);
        BubbleFrame.SetActive(false);
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
        cash += (int)(cash * tipPercentage);
        TargetTable.SetEatenViual(TargetSeatIndex,cash,exp);
        ResetSeat();
    }
    public void StopNavMesh()
    {
        NavMeshAgent.isStopped = true;
        NavMeshAgent.updatePosition = false;
        NavMeshAgent.updateRotation = false;
    }
    public void StartNavMesh()
    {
        NavMeshAgent.isStopped = false;
        NavMeshAgent.updatePosition = true;
        NavMeshAgent.updateRotation = true;    
    }

    public void ResetSeat()
    {
        if(TargetTable != null && TargetSeatIndex >= 0)
            TargetTable.ResetSeat(TargetSeatIndex);
    }
    public void Leave()
    {
        ResetSeat();
        stateMachine.SetState(new LeavingState(stateMachine));
        StopBubble();
    }

}