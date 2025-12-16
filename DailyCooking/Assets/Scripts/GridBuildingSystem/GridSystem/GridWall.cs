using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridWall : MonoBehaviour, IPlaceable, IDestroyable
{
    [SerializeField] private GameObject[] visualGameObjectArray;
    [SerializeField] private GameObject[] visualGameObjectShadowArray;
    private bool isPlaced;
    private Action onDestroySelf;
    public bool IsPlaced { get => isPlaced; set => isPlaced = value; }
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
    }

    public bool CanRemove()
    {
        return true;
    }
}
