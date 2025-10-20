using System;
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

    private BotStateMachine stateMachine;
    private FoodSO waitingFood;

    public Table TargetTable { get; set; }
    public int TargetSeatIndex { get; set; }

    public NavMeshAgent NavMeshAgent { get => navMeshAgent; }

    private void Awake()
    {
        stateMachine = new BotStateMachine(this);
        foodBubble.SetActive(false);
        orderBubble.SetActive(false);
        //emotionBubble.SetActive(false);
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
                // Player delivered correct recipe 
                KitchenGameManager.Instance.ServeFood(waitingFood);
                return true;
            }
        }

        //No matches found
        //Player did not deliver correct recipe
        return false;
    }
    public void OrderFood()
    {
        waitingFood = DeliveryManager.Instance.GetUnlockedFood();
        foodBubble.SetActive(true);
        foodBubble.GetComponent<BubbleFoodUI>().SetOrder(new[] { waitingFood.Sprite });
    }
    public void ShowOrder()
    {
        orderBubble.SetActive(true);
    }
    public void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        OnInteract?.Invoke(playerStateMachine);
    }

    public void InteractAlternateEvent(PlayerStateMachine playerStateMachine)
    {
        
    }

    internal void HideOrder()
    {
        orderBubble.SetActive(false);
    }
}