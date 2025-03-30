using MVC;
using Observer;
using System;
using System.Collections.Generic;
using UnityEngine;
public class BaseCounterView : MonoBehaviour
{
    public event EventHandler<PlayerStateMachine> OnInteract;
    public event EventHandler<PlayerStateMachine> OnInteractAlternate;
    [SerializeField] private Transform counterTopPoint;
    [SerializeField] private GameObject[] visualGameObjectArray;

    public Transform CounterTopPoint { get => counterTopPoint; set => counterTopPoint = value; }
    private void Start()
    {
        PlayerStateMachine.Instance.OnSelectedCounterChanged += Player_OnSelectedCounterChanged;
    }
    public virtual object CreateControllerFromView()
    {
        return new BaseCounterController(this, new BaseCounterModel());
    }
    private void Player_OnSelectedCounterChanged(object sender, PlayerStateMachine.OnSelectedCounterChangedEventArgs e)
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
}