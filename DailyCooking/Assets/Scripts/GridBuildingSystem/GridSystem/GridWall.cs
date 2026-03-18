using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GridWall : NetworkBehaviour, IPlaceable, IDestroyable
{
    [SerializeField] private GameObject[] visualGameObjectArray;
    [SerializeField] private GameObject[] visualGameObjectShadowArray;
    private NetworkVariable<bool> isPlaced = new NetworkVariable<bool>(false);
    private Action onDestroySelf;
    public NetworkVariable<bool> IsPlaced { get => isPlaced; set => isPlaced = value; }
    public Action OnDestroySelf { get => onDestroySelf; set => onDestroySelf += value; }

    private void Start()
    {
        Hide();
    }

    public void OnGridEdit()
    {
        Show();
    }
    public void OnExitGridEdit()
    {
        Hide();
    }   

    private void Show()
    {
        foreach (var visualGameObject in visualGameObjectShadowArray)
        {
            visualGameObject.SetActive(true);
        }        
        foreach (var visualGameObject in visualGameObjectArray)
        {
            visualGameObject.SetActive(false);
        }

    }
    private void Hide()
    {
        foreach (var visualGameObject in visualGameObjectShadowArray)
        {
            visualGameObject.SetActive(false);
        }
        foreach (var visualGameObject in visualGameObjectArray)
        {
            visualGameObject.SetActive(true);
        }
    }

    public void DestroySelf()
    {
        OnDestroySelf?.Invoke();
        NetworkObject.Despawn();
        Destroy(this);
    }

    public bool CanRemove()
    {
        return true;
    }
}