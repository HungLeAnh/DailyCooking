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
        GameManager.Instance.GameData.PlayerStats.OnResourceChange += Initialize;
        GameManager.Instance.GameData.RestaurantData.OnResourceChange += Initialize;
    }
    private void OnDestroy()
    {
        if(KitchenGameManager.Instance != null)
            KitchenGameManager.Instance.OnStateChanged -= Instance_OnStateChanged;
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
        BotManager.Instance.KickAllBots();
    }

    public override void ShowPopup(object param = null)
    {
        base.ShowPopup(param);
        Initialize();
    }

    private void Initialize()
    {
        restaurantNameText.text = GameManager.Instance.GameData.RestaurantData.RestaurantName;
        moveSpeedText.text = GameManager.Instance.GameData.PlayerStats.MoveSpeed.ToString("F2");
        cookingSpeedText.text = GameManager.Instance.GameData.PlayerStats.CookingSpeed.ToString("F2");
        carryingCapacityText.text = GameManager.Instance.GameData.PlayerStats.CarryingCapacity.ToString();
        tipIncreaseText.text = GameManager.Instance.GameData.PlayerStats.TipIncrease.ToString("F2") + "%";
    }

    public override void HidePopup(object param = null)
    {
        base.HidePopup(param);
    }
    public void OnCloseClick()
    {
        UIPopupManager.Instance.HidePopup(UIPopupType.UIRestaurantPopup);
    }
}