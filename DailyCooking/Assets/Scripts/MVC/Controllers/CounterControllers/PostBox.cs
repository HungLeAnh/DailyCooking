using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PostBox : NetworkBehaviour, IInteractable, IHighlightable,IHasOptionalSO
{
    [SerializeField] private KitchenObjectSO postBoxKitchenSO;
    [SerializeField] private GameObject[] visualGameObjectArray;

    private List<KitchenObjectSO> kitchenObjectSOList = new List<KitchenObjectSO>();
    private PlayerStateMachine playerStateMachine;
    private int selectedIndex = 0;
    public void ClearKitchenObjectSO(int index = 0)
    {
        kitchenObjectSOList[index] = null;
    }

    public KitchenObjectSO GetKitchenObjectSO(int index = 0)
    {
        return kitchenObjectSOList[index];
    }
    public void SetKitchenObjectSO(KitchenObjectSO kitchenObject, int index = 0)
    {
        kitchenObjectSOList[index] = kitchenObject;
    }

    public NetworkObject GetNetworkObject()
    {
        return NetworkObject;
    }

    public bool HasKitchenObjectSO(int index = 0)
    {
        if (kitchenObjectSOList.Count <= index)
        {
            return false;
        }
        return kitchenObjectSOList[index] != null;
    }

    public void InteractAlternateEvent(PlayerStateMachine playerStateMachine)
    {

    }

    public void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        //Debug.Log("PostBox InteractEvent");
        //Debug.Log("HasKitchenObjectSO: " + HasKitchenObjectSO());
        //Debug.Log("Player HasKitchenObject: " + playerStateMachine.HasKitchenObject());
        if (HasKitchenObjectSO() && !playerStateMachine.HasKitchenObject())
        {
            //Debug.Log("Show Option Menu");
            this.playerStateMachine = playerStateMachine;
            OnShowOptionMenu(kitchenObjectSOList);
        }
    }

    public void OnSelected()
    {
        Show();
    }

    public void OnDeselected()
    {
        Hide();
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

    public void SetOptionKitchenObjectSO(int index)
    {
        if (!playerStateMachine.HasKitchenObject())
        {
            selectedIndex = index;
            KitchenGameManager.Instance.OnSpawnKitchenObjectCompleted += SpawnKitchenObject;
            KitchenObject.SpawnKitchenObject(postBoxKitchenSO, playerStateMachine);
        }
    }

    private void SpawnKitchenObject()
    {
        KitchenGameManager.Instance.OnSpawnKitchenObjectCompleted -= SpawnKitchenObject;
        if (playerStateMachine.GetKitchenObject() is RefillerKitchenObject refillerKitchenObject)
        {
            refillerKitchenObject.SetRefillKitchenObject(kitchenObjectSOList[selectedIndex]);
            kitchenObjectSOList.RemoveAt(selectedIndex);
        }
        playerStateMachine = null;

    }

    public void OnShowOptionMenu(List<KitchenObjectSO> kitchenObjectSOList)
    {
        if (GridBuildingSystem.Instance.BuildingPlacementManager.IsBuilding)
            return;

        UIPopupManager.Instance.ShowPopup(
        UIPopupType.UIOptionMenuPopup,
        new UIOptionMenuPopup.Param
        {
            sender = this,
            optionalList = kitchenObjectSOList,
            Title = "PostBox"
        });

    }

    public void AddPackage(KitchenObjectSO kitchenObjectSO)
    {

        kitchenObjectSOList.Add(kitchenObjectSO);
    }
}