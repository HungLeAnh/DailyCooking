using Observer;
using System;
using System.Collections.Generic;
using UnityEngine;


public class BaseCounterView : MonoBehaviour
{
        public event EventHandler<PlayerStateMachine> OnRestartGame;
    [SerializeField] private Transform counterTopPoint;
    [SerializeField] private GameObject[] visualGameObjectArray;

    public Transform CounterTopPoint => counterTopPoint;
    public T GetController<T>() where T : BaseCounterController
    {
        return GetComponent<T>();
    }
    private void Start()
    {
        CounterModules.Instance.OnSelectedCounterChanged += Player_OnSelectedCounterChanged;
        KitchenGameManager.Instance.OnStateChanged += KitchenGameManager_OnStateChanged;
    }
    private void OnDestroy()
    {
        Hide();
        CounterModules.Instance.OnSelectedCounterChanged -= Player_OnSelectedCounterChanged;
        KitchenGameManager.Instance.OnStateChanged -= KitchenGameManager_OnStateChanged;
    }

    private void KitchenGameManager_OnStateChanged(object sender, EventArgs e)
    {
        if (KitchenGameManager.Instance.IsGameOver()||
            KitchenGameManager.Instance.IsEditing())
        {
            Hide();
            OnRestartGame?.Invoke(this, PlayerStateMachine.Instance);
        }
    }

    private void Player_OnSelectedCounterChanged(object sender, CounterModules.OnSelectedCounterChangedEventArgs e)
    {
        if (e.selectedCounterController.BaseCounterView == this)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }
    private void Show()
    {
        foreach (var visualGameObject in visualGameObjectArray)
        {
            visualGameObject.SetActive(true);
        }

    }
    private void Hide()
    {
        foreach (var visualGameObject in visualGameObjectArray)
        {
            visualGameObject.SetActive(false);
        }
    }   

}