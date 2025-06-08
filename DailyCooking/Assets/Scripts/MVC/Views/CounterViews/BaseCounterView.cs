using MVC;
using Observer;
using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public interface IContainerCounter
{
    public abstract KitchenObjectSO GetContainerKitchenObjectType();
}
public class BaseCounterView : MonoBehaviour, IContainerCounter
{
    public Action OnUpdate;
    public event EventHandler<PlayerStateMachine> OnInteract;
    public event EventHandler<PlayerStateMachine> OnInteractAlternate;
    public event EventHandler<PlayerStateMachine> OnRestartGame;
    [SerializeField] private Transform counterTopPoint;
    [SerializeField] private GameObject[] visualGameObjectArray;

    public Transform CounterTopPoint { get => counterTopPoint; set => counterTopPoint = value; }
    public virtual object CreateControllerFromView()
    {
        return new BaseCounterController(this, new BaseCounterModel());
    }
    private void Start()
    {
        CounterModules.Instance.OnSelectedCounterChanged += Player_OnSelectedCounterChanged;
        KitchenGameManager.Instance.OnStateChanged += KitchenGameManager_OnStateChanged;
    }
    public void UnsubEvent()
    {
        CounterModules.Instance.OnSelectedCounterChanged -= Player_OnSelectedCounterChanged;
        KitchenGameManager.Instance.OnStateChanged -= KitchenGameManager_OnStateChanged;
    }
    private void Update()
    {
        OnUpdate?.Invoke();
    }
    
    private void KitchenGameManager_OnStateChanged(object sender, EventArgs e)
    {
        if (KitchenGameManager.Instance.IsGameOver())
        {
            Hide();
            OnRestartGame?.Invoke(this, PlayerStateMachine.Instance);
        }
    }

    private void Player_OnSelectedCounterChanged(object sender, CounterModules.OnSelectedCounterChangedEventArgs e)
    {
        if (e.selectedCounterView == this)
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

    internal void FireInteractAlternateEvent(PlayerStateMachine playerStateMachine)
    {
        OnInteractAlternate?.Invoke(this, playerStateMachine);
    }

    internal void FireInteractEvent(PlayerStateMachine playerStateMachine)
    {
        OnInteract?.Invoke(this, playerStateMachine);
    }

    internal virtual void UpdateView(object baseCounterModel)
    {

    }

    public virtual KitchenObjectSO GetContainerKitchenObjectType()
    {
        return null;
    }
}