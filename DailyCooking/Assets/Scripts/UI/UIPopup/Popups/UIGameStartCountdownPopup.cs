using UnityEngine;
using TMPro;

public class UIGameStartCountdownPopup : UIPopup
{
    private const string NUMBER_POPUP = "NumberPopup";

    [SerializeField] private TextMeshProUGUI countdownText;

    [SerializeField] private Animator animator;
    private int previousCountdownNumber;
    private bool isShowed = false;

    public override void ShowPopup()
    {
        base.ShowPopup();
        Show();
    }
    public override void HidePopup(object param = null)
    {
        base.HidePopup(param);
        Hide();
    }
    private void Update()
    {       
        if (!isShowed || !KitchenGameManager.Instance.IsCountdownToStartActive())
        {
            HidePopup();
            return;
        }   

        int countdownNumber = Mathf.CeilToInt(KitchenGameManager.Instance.GetCountdownToStartTimer());
        countdownText.text = countdownNumber.ToString();
        if (previousCountdownNumber != countdownNumber)
        {
            previousCountdownNumber = countdownNumber;
            animator.SetTrigger(NUMBER_POPUP);
            SoundManager.Instance.PlayCountdownSound();
        }
 
    }

    private void Hide()
    {
        gameObject.SetActive(false);
        isShowed = false;
    }
    private void Show()
    {
        gameObject.SetActive(true);
        isShowed = true;
    }
}
