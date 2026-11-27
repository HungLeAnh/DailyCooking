using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Host only: "Who watches?" — pick the client who watches the host's rewarded ad.
public class UIAdWatcherPopup : UIPopup
{
    public class Option
    {
        public string Label;
        public bool Enabled = true;
        public Action OnPick;
    }

    public class Param
    {
        public string Title;
        public string Status;
        public List<Option> Options = new List<Option>();
        public Action OnCancel;
    }

    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Transform optionsRoot;
    [SerializeField] private Button optionTemplate;
    [SerializeField] private Button cancelButton;

    private readonly List<Button> optionButtons = new List<Button>();

    public override void ShowPopup(object param = null)
    {
        base.ShowPopup(param);
        Build(_openParam as Param);
    }

    public override void HidePopup(object param = null)
    {
        base.HidePopup(param);
        cancelButton.onClick.RemoveAllListeners();
        for (int i = 0; i < optionButtons.Count; i++)
            optionButtons[i].onClick.RemoveAllListeners();
    }

    private void Build(Param p)
    {
        if (p == null) p = new Param();
        titleText.text = p.Title ?? "Who watches?";
        statusText.text = p.Status ?? "";
        optionTemplate.gameObject.SetActive(false);

        int count = p.Options.Count;
        while (optionButtons.Count < count)
        {
            Button button = Instantiate(optionTemplate, optionsRoot);
            optionButtons.Add(button);
        }
        for (int i = 0; i < optionButtons.Count; i++)
        {
            Button button = optionButtons[i];
            button.onClick.RemoveAllListeners();
            bool used = i < count;
            button.gameObject.SetActive(used);
            if (!used) continue;
            Option option = p.Options[i];
            button.GetComponentInChildren<TextMeshProUGUI>(true).text = option.Label;
            button.interactable = option.Enabled;
            button.onClick.AddListener(() =>
            {
                HidePopup();
                option.OnPick?.Invoke();
            });
        }

        cancelButton.onClick.RemoveAllListeners();
        cancelButton.onClick.AddListener(() =>
        {
            HidePopup();
            p.OnCancel?.Invoke();
        });
    }
}
