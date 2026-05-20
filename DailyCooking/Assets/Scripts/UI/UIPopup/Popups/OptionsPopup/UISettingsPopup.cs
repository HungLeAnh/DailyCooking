using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISettingsPopup : UIPopup
{
    private const float SOUND_VOLUME_MULTIPLIER = 10f;
    private const int CHEAT_COIN_AMOUNT = 1000;
    private const int CHEAT_EXP_AMOUNT = 100;

    [SerializeField] private Slider soundEffectsSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button guideButton;
    [SerializeField] private Button quitButton;
#if UNITY_EDITOR
    [Header("Cheat")]
    [SerializeField] private Transform cheatTransform;

    private static int cheatTapCount = 0;

    public void OnTitleClick()
    {
        cheatTapCount++;
        if (cheatTapCount >= 10)
        {
            ShowCheat();
        }
    }

    private void ShowCheat()
    {
        cheatTransform.gameObject.SetActive(true);

    }
    public void OnCheatCoin()
    {
        GameManager.Instance.UpdateRestaurantCoinServerRpc(CHEAT_COIN_AMOUNT);
    }
    public void OnCheatExp()
    {
        GameManager.Instance.UpdateRestaurantExpServerRpc(CHEAT_EXP_AMOUNT);
    }
#endif

    private void Awake()
    {
        soundEffectsSlider.onValueChanged.AddListener((newValue) =>
        {
            SoundManager.Instance.ChangeVolume(newValue);
        });

        musicSlider.onValueChanged.AddListener((newValue) =>
        {
            MusicManager.Instance.ChangeVolume(newValue);
        });

        closeButton.onClick.AddListener(() =>
        {
            UIPopupManager.Instance.HidePopup(UIPopupType.UISettingPopup);
        });
        guideButton.onClick.AddListener(() => {
            TutorialManager.Instance.ShowGameMachanicTutorial();
        });
        quitButton.onClick.AddListener(() =>
        {
            MultiplayerManager.Instance.ShutdownAndReset();
            Loader.Load(Loader.Scene.MainMenuScene);
            GameManager.Instance.SwitchState(new MainMenuState(GameManager.Instance));
            HidePopup();
        });
    }
    public override void ShowPopup(object param = null)
    {
        base.ShowPopup(param);
        UpdateVisual();
    }
    public override void HidePopup(object param = null)
    {
        base.HidePopup(param);

    }
    private void UpdateVisual()
    {
        soundEffectsSlider.value = SoundManager.Instance.GetVolume();
        musicSlider.value = MusicManager.Instance.GetVolume();

    }
}