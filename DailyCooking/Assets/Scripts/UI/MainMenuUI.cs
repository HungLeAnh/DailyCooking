using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;

    private void Awake()
    {
        playButton.onClick.AddListener(() =>
        {
            Loader.Load(Loader.Scene.GameScene);
        });
        optionsButton.onClick.AddListener(() => {
            Hide();
            UIOptionsPopup.Instance.Show(Show);
        });
        quitButton.onClick.AddListener(() =>
        {
            Application.Quit();
        });

        Time.timeScale = 1f;
    }
    public void Show()
    {
        gameObject.SetActive(true);
    }
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}