using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PostBox : NetworkBehaviour, IInteractable, IHighlightable,IHasOptionalSO
{
    [SerializeField] private KitchenObjectSO postBoxKitchenSO;
    [SerializeField] private GameObject[] visualGameObjectArray;

    private NetworkList<FixedString64Bytes> kitchenObjectSOGuidList = new NetworkList<FixedString64Bytes>();
    private IKitchenObjectParent playerStateMachine;
    private int selectedIndex = 0;
    public override void OnNetworkSpawn()
    {
        if (IsHost || IsServer || MultiplayerManager.Instance.IsSinglePlayerMode)
        {
            Initialize();            
            GameManager.Instance.GameData.PostBoxData.KitchenObjectSOGuidList.ForEach(guid =>
            {
                var kitchenObjectSO = KitchenGameManager.Instance.GetKitchenObjectSOByGuid(guid);
                if (kitchenObjectSO != null)
                {
                    kitchenObjectSOGuidList.Add(kitchenObjectSO.Guid);
                }
            });
        }
        else
            MultiplayerManager.Instance.OnDataSyncToNewClient += (object sender, EventArgs e) => Initialize();
    }

    private void Initialize()
    {
        GridBuildingSystem.Instance.PostBox = this;
    }

    public bool HasKitchenObjectSO(int index = 0)
    {
        if (kitchenObjectSOGuidList.Count <= index)
        {
            return false;
        }
        return KitchenGameManager.Instance.GetKitchenObjectSOByGuid(kitchenObjectSOGuidList[index].ToString()) != null;
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
            SetIKitchenObjectParentServerRpc(playerStateMachine.GetNetworkObject());
            OnShowOptionMenu(kitchenObjectSOGuidList.AsNativeArray().ToList().Select(guid => KitchenGameManager.Instance.GetKitchenObjectSOByGuid(guid.ToString())).ToList());
        }
    }
    [Rpc(SendTo.Server)]
    private void SetIKitchenObjectParentServerRpc(NetworkObjectReference playerReference)
    {
        SetIKitchenObjectParentClientRpc(playerReference);
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void SetIKitchenObjectParentClientRpc(NetworkObjectReference playerReference)
    {
        if (playerReference.TryGet(out NetworkObject playerNetworkObject))
        {
            var playerStateMachine = playerNetworkObject.GetComponent<PlayerStateMachine>();
            if (playerStateMachine != null)
            {
                this.playerStateMachine = playerStateMachine;
            }
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
            SubToOnSpawnKitchenObjectCompletedServerRpc();
            KitchenObject.SpawnKitchenObject(postBoxKitchenSO, playerStateMachine);
        }
    }
    [Rpc(SendTo.Server)]
    private void SubToOnSpawnKitchenObjectCompletedServerRpc()
    {
        KitchenGameManager.Instance.OnSpawnKitchenObjectCompleted += SpawnKitchenObject;
    }
    private void SpawnKitchenObject()
    {
        KitchenGameManager.Instance.OnSpawnKitchenObjectCompleted -= SpawnKitchenObject;
        if (playerStateMachine.GetKitchenObject() is RefillerKitchenObject refillerKitchenObject)
        {
            Debug.Log("Refilling kitchen object with SO guid: " + kitchenObjectSOGuidList[selectedIndex].ToString());
            refillerKitchenObject.SetRefillKitchenObject(KitchenGameManager.Instance.GetKitchenObjectSOByGuid(kitchenObjectSOGuidList[selectedIndex].ToString()));
            GameManager.Instance.RemovePostBoxDataServerRpc(kitchenObjectSOGuidList[selectedIndex].ToString());
            kitchenObjectSOGuidList.RemoveAt(selectedIndex);
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

    public void AddPackage(string kitchenObjectSOGuid)
    {
        kitchenObjectSOGuidList.Add(kitchenObjectSOGuid);
        GameManager.Instance.GameData.PostBoxData.AddPackage(kitchenObjectSOGuid);
    }

}