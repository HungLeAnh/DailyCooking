using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PostBox : NetworkBehaviour, IInteractable, IHighlightable,IHasOptionalSO
{
    [SerializeField] private KitchenObjectSO postBoxKitchenSO;
    [SerializeField] private MeshRenderer[] visualGameObjectArray;

    private NetworkList<FixedString64Bytes> kitchenObjectSOGuidList = new NetworkList<FixedString64Bytes>();
    private HighlightMaterials highlight;

    private HighlightMaterials Highlight => highlight ??= new HighlightMaterials(visualGameObjectArray);

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
        else if (GameManager.Instance.GameData != null)
            Initialize();
        else
            MultiplayerManager.Instance.OnDataSyncToNewClient += MultiplayerManager_OnDataSyncToNewClient;
    }

    public override void OnNetworkDespawn()
    {
        if (MultiplayerManager.Instance != null)
            MultiplayerManager.Instance.OnDataSyncToNewClient -= MultiplayerManager_OnDataSyncToNewClient;
        base.OnNetworkDespawn();
    }

    private void MultiplayerManager_OnDataSyncToNewClient(object sender, EventArgs e)
    {
        MultiplayerManager.Instance.OnDataSyncToNewClient -= MultiplayerManager_OnDataSyncToNewClient;
        Initialize();
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

    // Runs on the server (see PlayerStateMachine.InteractServerRpc).
    public void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        if (HasKitchenObjectSO() && !playerStateMachine.HasKitchenObject())
        {
            var options = kitchenObjectSOGuidList.AsNativeArray().ToList()
                .Select(guid => KitchenGameManager.Instance.GetKitchenObjectSOByGuid(guid.ToString()))
                .ToList();
            InteractionUI.Instance.ShowOptionMenu(playerStateMachine, this, options, "PostBox");
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
        Highlight.SetActive(true);
    }

    public void Hide()
    {
        Highlight.SetActive(false);
    }

    public override void OnDestroy()
    {
        highlight?.Release();
        base.OnDestroy();
    }

    // Server: the actor picked a package; hand them a refill box for that ingredient.
    public void ApplyOption(PlayerStateMachine actor, int index)
    {
        if (actor.HasKitchenObject() || index < 0 || index >= kitchenObjectSOGuidList.Count)
            return;
        string packageGuid = kitchenObjectSOGuidList[index].ToString();
        KitchenObjectSO packageSO = KitchenGameManager.Instance.GetKitchenObjectSOByGuid(packageGuid);
        if (packageSO == null)
            return;
        if (!(KitchenObject.SpawnKitchenObject(postBoxKitchenSO, actor) is RefillerKitchenObject refillerKitchenObject))
            return;

        refillerKitchenObject.SetRefillKitchenObject(packageSO);
        GameManager.Instance.ServerRemovePostBoxPackage(packageGuid);
        kitchenObjectSOGuidList.RemoveAt(index);
    }

    public void AddPackage(string kitchenObjectSOGuid)
    {
        kitchenObjectSOGuidList.Add(kitchenObjectSOGuid);
        GameManager.Instance.GameData.PostBoxData.AddPackage(kitchenObjectSOGuid);
    }

}