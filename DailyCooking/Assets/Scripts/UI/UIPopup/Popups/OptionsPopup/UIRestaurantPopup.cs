using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIRestaurantPopup : UIPopup
{
    [SerializeField] private Button closeButton;
    [SerializeField] private Button kickAllButton;

    [SerializeField] private TextMeshProUGUI restaurantStatusText;
    [SerializeField] private Button restaurantStatusButton;
    [SerializeField] private TextMeshProUGUI restaurantStatusButtonText;
    [SerializeField] private Sprite openButtonImage;
    [SerializeField] private Sprite closeButtonImage;

    [SerializeField] private Button restaurantNameChangeButton;
    [SerializeField] private TextMeshProUGUI restaurantNameText;
    [SerializeField] private TextMeshProUGUI playerNameText;

    [SerializeField] private TextMeshProUGUI moveSpeedText;
    [SerializeField] private TextMeshProUGUI cookingSpeedText;
    [SerializeField] private TextMeshProUGUI carryingCapacityText;
    [SerializeField] private TextMeshProUGUI tipIncreaseText;

    // This popup outlives restaurants (UIPopupManager keeps it), so it follows the current game
    // data only while shown: GameData is replaced when another restaurant loads or is joined.
    private RestaurantData subscribedRestaurantData;
    private PlayerStats subscribedPlayerStats;

    private void Awake()
    {
        Instance_OnStateChanged(this,EventArgs.Empty);
        closeButton.onClick.AddListener(OnCloseClick);
        kickAllButton.onClick.AddListener(OnKickAll);
        restaurantStatusButton.onClick.AddListener(OnChangeRestaurantStatus);
        restaurantNameChangeButton.onClick.AddListener(OnChangeRestaurantName);

    }
    private void Start()
    {
        KitchenGameManager.Instance.OnStateChanged += Instance_OnStateChanged;
    }
    private void OnDestroy()
    {
        if(KitchenGameManager.Instance != null)
            KitchenGameManager.Instance.OnStateChanged -= Instance_OnStateChanged;
        UnsubscribeData();
    }

    private void SubscribeData()
    {
        UnsubscribeData();
        GameData gameData = GameManager.Instance.GameData;
        if (gameData == null) return;
        subscribedRestaurantData = gameData.RestaurantData;
        subscribedRestaurantData.OnResourceChange += Initialize;
        subscribedPlayerStats = gameData.GetPlayerStatsById(SessionManager.Instance.PlayerId);
        if (subscribedPlayerStats != null)
            subscribedPlayerStats.OnResourceChange += Initialize;
    }

    private void UnsubscribeData()
    {
        if (subscribedRestaurantData != null)
            subscribedRestaurantData.OnResourceChange -= Initialize;
        if (subscribedPlayerStats != null)
            subscribedPlayerStats.OnResourceChange -= Initialize;
        subscribedRestaurantData = null;
        subscribedPlayerStats = null;
    }

    private void Instance_OnStateChanged(object sender, EventArgs e)
    {
        if (KitchenGameManager.Instance.IsOpening())
        {
            restaurantStatusButtonText.text = "Close Restaurant";
            restaurantStatusButton.GetComponent<Image>().sprite = closeButtonImage;

            restaurantStatusText.text = "Restaurant Status: Opened";
        }
        else if(KitchenGameManager.Instance.IsClosing())
        {
            restaurantStatusButtonText.text = "Open Restaurant";
            restaurantStatusButton.GetComponent<Image>().sprite = openButtonImage;

            restaurantStatusText.text =  "Restaurant Status: Closed";
        }
    }

    private void OnChangeRestaurantName()
    {
        HidePopup();
        UIPopupManager.Instance.ShowPopup(UIPopupType.UIInputNamePopup,
            new UIInputNamePopup.Param
            {
                Title = "Input your restaurant name",
                callback = () =>
                {
                    UIPopupManager.Instance.ShowPopup(UIPopupType.UIRestaurantPopup);
                }
            });
    }

    private void OnChangeRestaurantStatus()
    {
        if (KitchenGameManager.Instance.IsOpening())
        {
            KitchenGameManager.Instance.ChangeState(KitchenGameManager.State.Close);
        }
        else
        {
            KitchenGameManager.Instance.ChangeState(KitchenGameManager.State.Open);
        }
    }

    private void OnKickAll()
    {
        if (!BotManager.Instance.IsServer)
        {
            UIManager.Instance.ShowAlertMessage("Only the restaurant owner can send customers away.");
            return;
        }
        BotManager.Instance.KickAllBots();
    }

    public override void ShowPopup(object param = null)
    {
        base.ShowPopup(param);
        SubscribeData();
        Instance_OnStateChanged(this, EventArgs.Empty);
        Initialize();
    }

    private void Initialize()
    {
        GameData gameData = GameManager.Instance.GameData;
        if (gameData == null) return;
        restaurantNameText.text = gameData.RestaurantData.RestaurantName;
        PlayerStats stats = gameData.GetPlayerStatsById(SessionManager.Instance.PlayerId);
        if (stats == null) return;
        moveSpeedText.text = stats.MoveSpeed.ToString("F2");
        cookingSpeedText.text = stats.CookingSpeed.ToString("F2");
        carryingCapacityText.text = stats.CarryingCapacity.ToString();
        tipIncreaseText.text = stats.TipIncrease.ToString("F2") + "%";
    }

    public override void HidePopup(object param = null)
    {
        base.HidePopup(param);
        UnsubscribeData();
    }
    public void OnCloseClick()
    {
        UIPopupManager.Instance.HidePopup(UIPopupType.UIRestaurantPopup);
    }
}