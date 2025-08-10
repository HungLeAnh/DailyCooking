using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISettingsPopup : UIPopup
{
    private const float SOUND_VOLUME_MULTIPLIER = 10f;
    private const int CHEAT_COIN_AMOUNT = 1000;
    private const int CHEAT_EXP_AMOUNT = 100;

    [SerializeField] private Button soundEffectsButton;
    [SerializeField] private Button musicButton;
    [SerializeField] private Button closeButton;
 
    [SerializeField] private TextMeshProUGUI soundEffectText;
    [SerializeField] private TextMeshProUGUI musicText;

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
        GameManager.Instance.GameData.PlayerStats.UpdatePlayerCoins(CHEAT_COIN_AMOUNT);
    }
    public void OnCheatExp()
    {
        GameManager.Instance.GameData.PlayerStats.UpdatePlayerExp(CHEAT_EXP_AMOUNT);
    }
#endif

    private Action onCloseButtonAction;

    private void Awake()
    {
        soundEffectsButton.onClick.AddListener(() =>
        {
            SoundManager.Instance.ChangeVolume();
            UpdateVisual();

        });

        musicButton.onClick.AddListener(() =>
        {
            MusicManager.Instance.ChangeVolume();
            UpdateVisual();
        });
        closeButton.onClick.AddListener(() =>
        {
            Hide();
            onCloseButtonAction?.Invoke();
        });

    }
    private void Start()
    {
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        soundEffectText.text = "Sound Effects: " + Mathf.Round(SoundManager.Instance.GetVolume() * SOUND_VOLUME_MULTIPLIER);
        musicText.text = "Music: " + Mathf.Round(MusicManager.Instance.GetVolume() * SOUND_VOLUME_MULTIPLIER);

    }
    public void Show(Action onCloseButtonAction)
    {
        this.onCloseButtonAction = onCloseButtonAction;
        gameObject.SetActive(true);
        soundEffectsButton.Select();
    }
    public void Hide()
    {
        base.HidePopup();
    }
}