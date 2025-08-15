using Observer;
using System;
using System.Collections.Generic;
using UnityEngine;


public class BaseCounterView : MonoBehaviour
{
    [SerializeField] private Transform counterTopPoint;
    [SerializeField] private GameObject[] visualGameObjectArray;

    public Transform CounterTopPoint => counterTopPoint;
    public T GetController<T>() where T : BaseCounterController
    {
        return GetComponent<T>();
    }

    public void Show()
    {
        foreach (var visualGameObject in visualGameObjectArray)
        {
            visualGameObject.SetActive(true);
        }

    }
    public void Hide()
    {
        foreach (var visualGameObject in visualGameObjectArray)
        {
            visualGameObject.SetActive(false);
        }
    }   

}