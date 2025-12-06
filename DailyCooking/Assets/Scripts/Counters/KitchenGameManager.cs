using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KitchenGameManager : SimpleSingleton<KitchenGameManager>
{
    private const string PLAYER_DAY = "PlayerDay";
    private const float COUNTDOWN_TO_START_TIMER_INITIAL = 3f;
    private const float GAME_PLAYING_TIMER_MAX_INITIAL = 20f;
    private const int PLAYER_EXP_MULTIPLIER = 10;
    private const float TIME_SCALE_PAUSED = 0f;
    private const float TIME_SCALE_UNPAUSED = 1f;

    
    public event EventHandler OnStateChanged;

    public event EventHandler OnServeFood;

    public enum State
    {
        Editing,
        GamePlaying,
    }
    [SerializeField] private long earnGoalMultiply = 1000;
    [SerializeField] private long serveGoalMultiply = 10;
    [SerializeField] private long gamePlayingTimeMultiply = 60;
    [SerializeField] private List<CuttingRecipeSO> cuttingRecipeSOList;
    [SerializeField] private List<FryingRecipeSO> fryingRecipeSOList;

    private State state;
    private float gamePlayingTimer;
    private float gamePlayingTimerMax = GAME_PLAYING_TIMER_MAX_INITIAL;
    private long earnGoal;
    private long serveGoal;
    private long earnCount;
    private long serveCount;
    private int playerDay = -1;
    private List<KitchenObjectSO> unlockIngredient;

    public long EarnCount => earnCount;
    public long ServeCount => serveCount;
    public long EarnGoal { get => earnGoal; set => earnGoal = value; }
    public long ServeGoal { get => serveGoal; set => serveGoal = value; }
    public int PlayerDay => playerDay;

    public List<CuttingRecipeSO> CuttingRecipeSOList { get => cuttingRecipeSOList; set => cuttingRecipeSOList = value; }
    public List<FryingRecipeSO> FryingRecipeSOList { get => fryingRecipeSOList; set => fryingRecipeSOList = value; }

    protected override void Awake()
    {
        base.Awake();
        state = State.Editing;
        unlockIngredient = new List<KitchenObjectSO>();

        playerDay = GameManager.Instance.GameData.PlayerStats.playerData.DaysPlayed;
    }
    public void OnDestroy()
    {
        unlockIngredient.Clear();
    }
    public void Start()
    {
        Init();
    }
    public void Init()
    {
        unlockIngredient.Clear();
        foreach (var counterController in CounterModules.Instance.BaseCounterControllers)
        {
            if (counterController == null)
                continue;
            AddUnlockIngredient(counterController);
        }
        BotManager.Instance.Initialize();        
        ChangeState(State.GamePlaying);
        BotManager.Instance.StartSpawnBot();
    }

    //private void CreateDailytask()
    //{
    //    EarnGoal = playerDay * earnGoalMultiply;
    //    ServeGoal = playerDay * serveGoalMultiply;
    //    gamePlayingTimerMax = ServeGoal * gamePlayingTimeMultiply;
    //    earnCount = 0;
    //    serveCount = 0;
    //}
    public void ChangeState(State newState)
    {
        state = newState;
        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }
    private void RewardPlayer()
    {
        //Win game

        //playerDay++;
        //GameManager.Instance.GameData.PlayerStats.UpdatePlayedDay(playerDay);
        //GameManager.Instance.GameData.PlayerStats.UpdatePlayerCoins((int)earnCount);
        //GameManager.Instance.GameData.PlayerStats.UpdatePlayerExp(playerDay * PLAYER_EXP_MULTIPLIER);

        ChangeState(State.Editing);
    }


    private void Update()
    {
        switch (state)
        {
            //ChangeState(State.GamePlaying);
            //gamePlayingTimer = gamePlayingTimerMax;

            case State.GamePlaying:
                gamePlayingTimer -= Time.deltaTime;
                if (gamePlayingTimer < 0f)
                {

                }
                break;
        }
    }
    public bool IsGamePlaying()
    {
        return state == State.GamePlaying;
    }

    public bool IsEditing()
    {
        return state == State.Editing;

    }

    public float GetGamePlayingTimerNomalized()
    {
        return 1 - (gamePlayingTimer / gamePlayingTimerMax);
    }

    public void ServeFood(long foodPrice)
    {
        earnCount += foodPrice;
        serveCount++;
        OnServeFood?.Invoke(this,EventArgs.Empty);

    }


    public void AddUnlockIngredient(BaseCounterController counterController)
    {
        IContainerCounter containerCounter = counterController as IContainerCounter;
        if (containerCounter == null) return;

        List<KitchenObjectSO> kitchenObjectSOList = containerCounter.GetContainerKitchenObjectType();
        if (kitchenObjectSOList != null)
        {
            foreach (var item in kitchenObjectSOList)
            {
                if (!unlockIngredient.Contains(item))
                    unlockIngredient.Add(item);
            }
        }
        GetUnlockFood();

    }

    private void GetUnlockFood()
    {
        foreach (var foodSO in ConfigManager.Instance.ConfigFood.FoodItems)
        {
            bool isUnlocked = true;
            foreach (var ingredient in foodSO.kitchenObjectSOList)
            {
                isUnlocked = isUnlocked && IsIngredientUnlocked(ingredient);
                if (!isUnlocked)
                {
                    break;
                }
            }
            if (isUnlocked)
            {
                GameManager.Instance.GameData.MenuData.AddUnlockedDish(foodSO);
                 
            }
        }
    }

    private bool IsIngredientUnlocked(KitchenObjectSO ingredient)
    {
        foreach (var unlockIngredient in unlockIngredient)
        {
            if (unlockIngredient == ingredient)
            {
                return true;
            }
            else
            {
                var cuttingRecipeSO = KitchenGameManager.Instance.CuttingRecipeSOList.Find(x => x.input == unlockIngredient &&
                                                                       x.output == ingredient);
                var fryingRecipeSO = KitchenGameManager.Instance.FryingRecipeSOList.Find(x => x.input == unlockIngredient &&
                                                                       x.output == ingredient);
                if (cuttingRecipeSO != null || fryingRecipeSO != null)
                    return true;
            }

        }
        return false;
    }
    public FoodSO GetUnlockedFood()
    {
        return GameManager.Instance.GameData.
            MenuData.menuDished[UnityEngine.Random.Range(0, 
                GameManager.Instance.GameData.MenuData.menuDished.Count)];
    }
}
