using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridWall : MonoBehaviour
{
    [SerializeField] private GameObject[] visualGameObjectArray;
    [SerializeField] private GameObject[] visualGameObjectShadowArray;

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
}
