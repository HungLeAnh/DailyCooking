using System;
using System.Collections.Generic;
using UnityEngine;

public class KitchenGameManager : SimpleSingleton<KitchenGameManager>
{
    private const string PLAYER_DAY = "PlayerDay";
    private const float COUNTDOWN_TO_START_TIMER_INITIAL = 3f;
    private const float GAME_PLAYING_TIMER_MAX_INITIAL = 20f;
    private const int PLAYER_EXP_MULTIPLIER = 10;
    private const float TIME_SCALE_PAUSED = 0f;
    private const float TIME_SCALE_UNPAUSED = 1f;

    
    public event EventHandler OnStateChanged;

    public event EventHandler OnGamePaused;

    public event EventHandler OnGameUnpaused;

    public event EventHandler OnServeFood;

    public enum State
    {
        Editing,
        WaitingToStart,
        CountdownToStart,
        GamePlaying,
        GameOver,
    }
    [SerializeField] private long earnGoalMultiply = 1000;
    [SerializeField] private long serveGoalMultiply = 10;
    [SerializeField] private long gamePlayingTimeMultiply = 60;
    [SerializeField] private GameObject kitchenManagerUI;
    [SerializeField] private List<CuttingRecipeSO> cuttingRecipeSOList;
    [SerializeField] private List<FryingRecipeSO> fryingRecipeSOList;
    [SerializeField] private List<FoodSO> FoodSOList;

    private State state;
    private float countdownToStartTimer = COUNTDOWN_TO_START_TIMER_INITIAL;
    private float gamePlayingTimer;
    private float gamePlayingTimerMax = GAME_PLAYING_TIMER_MAX_INITIAL;
    private bool isGamePaused = false;
    private long earnGoal;
    private long serveGoal;
    private long earnCount;
    private long serveCount;
    private int playerDay = -1;
    private List<FoodSO> unlockFoodList;
    private List<KitchenObjectSO> unlockIngredient;

    public long EarnCount => earnCount;
    public long ServeCount => serveCount;
    public long EarnGoal { get => earnGoal; set => earnGoal = value; }
    public long ServeGoal { get => serveGoal; set => serveGoal = value; }
    public int PlayerDay => playerDay;

    public List<CuttingRecipeSO> CuttingRecipeSOList { get => cuttingRecipeSOList; set => cuttingRecipeSOList = value; }
    public List<FryingRecipeSO> FryingRecipeSOList { get => fryingRecipeSOList; set => fryingRecipeSOList = value; }

    private void Awake()
    {
        state = State.Editing;
        unlockFoodList = new List<FoodSO>();
        unlockIngredient = new List<KitchenObjectSO>();

        playerDay = GameManager.Instance.GameData.PlayerStats.playerData.DaysPlayed;
        CreateDailytask();
        kitchenManagerUI.SetActive(false);
    }
    public void OnDestroy()
    {
        unlockFoodList.Clear();
        unlockIngredient.Clear();
    }
    public void Init()
    {
        unlockFoodList.Clear();
        unlockFoodList.Clear();
        unlockIngredient.Clear();
        foreach (var counterController in CounterModules.Instance.BaseCounterControllers)
        {
            if (counterController == null)
                continue;
            AddUnlockIngredient(counterController);
        }

    }

    private void CreateDailytask()
    {
        EarnGoal = playerDay * earnGoalMultiply;
        ServeGoal = playerDay * serveGoalMultiply;
        gamePlayingTimerMax = ServeGoal * gamePlayingTimeMultiply;
        earnCount = 0;
        serveCount = 0;
    }

    public void StartGame()
    {
        CreateDailytask();
        kitchenManagerUI.SetActive(true);
        UIPopupManager.Instance.ShowPopup(UIPopupType.UIDayTaskPopup);
        countdownToStartTimer = COUNTDOWN_TO_START_TIMER_INITIAL;
        Init();
        BotManager.Instance.Initialize();
    }
    public void EndGame()
    {
        RewardPlayer();
        kitchenManagerUI.SetActive(false);

    }
    public void ChangeState(State newState)
    {
        state = newState;
        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }
    private void RewardPlayer()
    {
        if (state == State.GameOver)
        {
            if (serveGoal <= ServeCount && earnGoal <= earnCount)
            {
                //Win game

                playerDay++;
                GameManager.Instance.GameData.PlayerStats.UpdatePlayedDay(playerDay);
                GameManager.Instance.GameData.PlayerStats.UpdatePlayerCoins((int)earnCount);
                GameManager.Instance.GameData.PlayerStats.UpdatePlayerExp(playerDay * PLAYER_EXP_MULTIPLIER);
            }

        }
        ChangeState(State.Editing);
    }


    private void Update()
    {
        switch (state)
        {
            case State.WaitingToStart:

                break;
            case State.CountdownToStart:
                countdownToStartTimer -= Time.deltaTime;
                if (countdownToStartTimer < 0f)
                {
                    ChangeState(State.GamePlaying);
                    gamePlayingTimer = gamePlayingTimerMax;
                }
                break;
            case State.GamePlaying:
                gamePlayingTimer -= Time.deltaTime;
                if (gamePlayingTimer < 0f)
                {
                    ChangeState(State.GameOver);
                    UIPopupManager.Instance.ShowPopup(UIPopupType.UIGameOverPopup);
                }
                break;
            case State.GameOver:
                break;
        }
    }
    public bool IsGamePlaying()
    {
        return state == State.GamePlaying;
    }
    public bool IsCountdownToStartActive()
    {
        return state == State.CountdownToStart;
    }
    public bool IsGameOver()
    {
        return state == State.GameOver;
    }
    public bool IsEditing()
    {
        return state == State.Editing;

    }
    public float GetCountdownToStartTimer()
    {
        return countdownToStartTimer;
    }

    public float GetGamePlayingTimerNomalized()
    {
        return 1 - (gamePlayingTimer / gamePlayingTimerMax);
    }

    public void TogglePauseGame()
    {
        isGamePaused = !isGamePaused;
        if (isGamePaused)
        {
            Time.timeScale = TIME_SCALE_PAUSED;
            OnGamePaused?.Invoke(this, EventArgs.Empty);
            UIPopupManager.Instance.ShowPopup(UIPopupType.UIGamePausePopup);
        }
        else
        {
            Time.timeScale = TIME_SCALE_UNPAUSED;
            OnGameUnpaused?.Invoke(this, EventArgs.Empty);
        }
    }

    public void ServeFood(long foodPrice)
    {
        earnCount += foodPrice;
        serveCount++;
        OnServeFood.Invoke(this,EventArgs.Empty);

        if (IsTaskComplete()) 
        {
            ChangeState(State.GameOver);
            UIPopupManager.Instance.ShowPopup(UIPopupType.UIGameOverPopup);
        }
    }
    public bool IsTaskComplete()
    {
        return earnCount >= earnGoal && serveCount >= serveGoal;
    }


    public void AddUnlockIngredient(BaseCounterController counterController)
    {
        IContainerCounter containerCounter = counterController as IContainerCounter;
        if (containerCounter == null) return;

        KitchenObjectSO kitchenObjectSO = containerCounter.GetContainerKitchenObjectType();
        if (kitchenObjectSO != null)
        {
            if (!unlockIngredient.Contains(kitchenObjectSO))
                unlockIngredient.Add(kitchenObjectSO);
        }
        GetUnlockFood();

    }

    private void GetUnlockFood()
    {
        foreach (var foodSO in FoodSOList)
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
                if (!unlockFoodList.Contains(foodSO))
                    unlockFoodList.Add(foodSO);
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
        return unlockFoodList[UnityEngine.Random.Range(0, unlockFoodList.Count)];
    }
}
