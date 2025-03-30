using System;
using System.Collections.Generic;
using UnityEngine;

public class KitchenGameManager : MonoBehaviour
{
    private const string PLAYER_DAY = "PlayerDay";
    public static KitchenGameManager Instance { get; private set; }
    
    public event EventHandler OnStateChanged;

    public event EventHandler OnGamePaused;

    public event EventHandler OnGameUnpaused;

    public event EventHandler OnServeFood;

    private enum State
    {
        WaitingToStart,
        CountdownToStart,
        GamePlaying,
        GameOver,
    }
    [SerializeField] private long earnGoalMultiply = 1000;
    [SerializeField] private long serveGoalMultiply = 10;
    [SerializeField] private long gamePlayingTimeMultiply = 120;
    private State state;
    private float countdownToStartTimer = 3f;
    private float gamePlayingTimer;
    private float gamePlayingTimerMax = 20f;
    private bool isGamePaused = false;
    private long earnGoal;
    private long serveGoal;
    private long earnCount;
    private long serveCount;
    private int playerDay = -1;

    public long EarnCount => earnCount;
    public long ServeCount => serveCount;
    public long EarnGoal { get => earnGoal; set => earnGoal = value; }
    public long ServeGoal { get => serveGoal; set => serveGoal = value; }
    public int PlayerDay => playerDay;
    private void Awake()
    {
        Instance = this;
        state = State.WaitingToStart;


        playerDay = PlayerPrefs.GetInt(PLAYER_DAY,1);
        CreateDailytask();
    }
    private void Start()
    {
        //GameInput.Instance.OnPauseAction += GameInput_OnPauseAction;
        //GameInput.Instance.OnInteractAction += GameInput_OnInteractAction;

        //dayTaskUI.Show();
    }

    private void CreateDailytask()
    {
        EarnGoal = playerDay * earnGoalMultiply;
        ServeGoal = playerDay * serveGoalMultiply;
        gamePlayingTimerMax = ServeGoal * gamePlayingTimeMultiply;
        earnCount = 0;
        serveCount = 0;
    }

    private void GameInput_OnInteractAction(object sender, EventArgs e)
    {
        if (state == State.WaitingToStart)
        {
            state = State.CountdownToStart;
            OnStateChanged?.Invoke(this, EventArgs.Empty);
        }
        if (state == State.GameOver)
        {
            if (serveGoal <= ServeCount && earnGoal <= earnCount)
            {
                playerDay++;
                PlayerPrefs.SetInt(PLAYER_DAY, playerDay);
            }
            Loader.Load(Loader.Scene.GameScene);
        }
    }

    private void GameInput_OnPauseAction(object sender, EventArgs e)
    {
        TogglePauseGame();
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
                    state = State.GamePlaying;
                    gamePlayingTimer = gamePlayingTimerMax;
                    OnStateChanged?.Invoke(this, EventArgs.Empty);

                }
                break;
            case State.GamePlaying:
                gamePlayingTimer -= Time.deltaTime;
                if (gamePlayingTimer < 0f)
                {
                    state = State.GameOver;
                    OnStateChanged?.Invoke(this, EventArgs.Empty);

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
            Time.timeScale = 0f;
            OnGamePaused?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Time.timeScale = 1f;
            OnGameUnpaused?.Invoke(this, EventArgs.Empty);
        }
    }

    public void ServeFood(FoodSO waitingRecipeSO)
    {
        earnCount += waitingRecipeSO.price;
        serveCount++;
        OnServeFood.Invoke(this,EventArgs.Empty);

        if (IsTaskComplete()) 
        { 
            state = State.GameOver;
            OnStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public bool IsTaskComplete()
    {
        if (earnCount >= earnGoal && serveCount >= serveGoal)
            return true;
        else
            return false;
    }
}
