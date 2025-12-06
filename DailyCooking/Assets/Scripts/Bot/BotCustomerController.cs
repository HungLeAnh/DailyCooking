using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BotCustomerController : MonoBehaviour,IInteractable
{
    public Action<PlayerStateMachine> OnInteract;

    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent navMeshAgent;
    [SerializeField] private GameObject foodBubble;
    [SerializeField] private GameObject orderBubble;
    [SerializeField] private GameObject emotionBubble;
    [SerializeField] private GameObject BubbleFrame;

    [SerializeField] private BubbleEmotionUI bubbleEmotionUI;
    [SerializeField] private BubbleFoodUI bubbleFoodUI;

    [SerializeField] private GameObject[] visualGameObjectArray;

    private BotStateMachine stateMachine;
    private List<FoodSO> waitingFood;

    public Table TargetTable { get; set; }
    public int TargetSeatIndex { get; set; }

    public NavMeshAgent NavMeshAgent { get => navMeshAgent; }

    private void Awake()
    {
        stateMachine = new BotStateMachine(this);

        foodBubble.SetActive(false);
        orderBubble.SetActive(false);
        emotionBubble.SetActive(false);
        BubbleFrame.SetActive(false);
        waitingFood = new List<FoodSO>();
        bubbleEmotionUI.OnEmotionEnd += OnEmotionEnd;
    }

    public void PlayAnimation(BotAnimation.State animationState)
    {
        animator.Play(animationState.ToString());
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
        stateMachine.SetState(new LeavingState(stateMachine));
        TargetTable.ClearKitchenObject(TargetSeatIndex);
        StopBubble();
    }
    public void OrderFood()
    {
        waitingFood.Add(KitchenGameManager.Instance.GetUnlockedFood());

        BubbleFrame.SetActive(true);
        orderBubble.SetActive(false);
        foodBubble.SetActive(true);

        bubbleFoodUI.SetOrder(waitingFood);
        bubbleEmotionUI.StartEmotion();

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

        stateMachine.SetState(new BotIdleState(stateMachine));
    }
    public void InitBot()
    {
        stateMachine.SetState(new WaitForTableState(stateMachine));

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
        Show();
    }

    public void OnDeselected()
    {
        Hide();
    }
    public void Show()
    {
        foreach (var visualGameObject in visualGameObjectArray)
        {
            visualGameObject.SetActive(true);
        }

    }
    public void Hide()
    {
        foreach (var visualGameObject in visualGameObjectArray)
        {
            visualGameObject.SetActive(false);
        }
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
        TargetTable.SetEatenViual(TargetSeatIndex,cash,exp);
    }
}