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
    [Header("Graphics / FPS (MobilePerformanceManager)")]
    [SerializeField] private Button graphicsPrevButton;
    [SerializeField] private Button graphicsNextButton;
    [SerializeField] private TMP_Text graphicsValueText;
    [SerializeField] private Button fpsPrevButton;
    [SerializeField] private Button fpsNextButton;
    [SerializeField] private TMP_Text fpsValueText;
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
        GameManager.Instance.CheatAddServerRpc(CHEAT_COIN_AMOUNT, 0);
    }
    public void OnCheatExp()
    {
        GameManager.Instance.CheatAddServerRpc(0, CHEAT_EXP_AMOUNT);
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
            HidePopup();
            MultiplayerManager.Instance.LeaveSession();
        });

        AddClick(graphicsPrevButton, () => ChangeGraphics(GraphicsPreferencesRules.Previous));
        AddClick(graphicsNextButton, () => ChangeGraphics(GraphicsPreferencesRules.Next));
        AddClick(fpsPrevButton, ToggleFps);
        AddClick(fpsNextButton, ToggleFps);
    }
    private static void AddClick(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
            button.onClick.AddListener(action);
    }
    private void ChangeGraphics(Func<GraphicsMode, GraphicsMode> step)
    {
        var manager = MobilePerformanceManager.Instance;
        if (manager == null)
            return;
        GraphicsPreferences p = manager.Preferences;
        p.Mode = step(p.Mode);
        manager.SetPreferences(p);
        UpdatePerformanceVisual();
    }
    // Two values only: either arrow picks the other one, and the choice becomes fixed.
    private void ToggleFps()
    {
        var manager = MobilePerformanceManager.Instance;
        if (manager == null)
            return;
        GraphicsPreferences p = manager.Preferences;
        p.ChosenFps = GraphicsPreferencesRules.ToggleFps(DisplayedFps(manager));
        manager.SetPreferences(p);
        UpdatePerformanceVisual();
    }
    private static int DisplayedFps(MobilePerformanceManager manager) =>
        manager.Preferences.ChosenFps != 0 ? manager.Preferences.ChosenFps : manager.CurrentFps;
    private void UpdatePerformanceVisual()
    {
        var manager = MobilePerformanceManager.Instance;
        if (graphicsValueText != null)
            graphicsValueText.text = (manager != null ? manager.Preferences.Mode : GraphicsMode.Auto).ToString();
        if (fpsValueText != null)
            fpsValueText.text = (manager != null ? DisplayedFps(manager) : 60).ToString();
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
        UpdatePerformanceVisual();

    }
}