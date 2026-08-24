using Unity.Netcode;
using UnityEngine;

public class StorageBox : NetworkBehaviour, IInteractable, IHighlightable
{
    [SerializeField] private MeshRenderer[] visualGameObjectArray;

    public void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        if (playerStateMachine == null)
            return;

        KitchenObject held = playerStateMachine.GetKitchenObject();
        if (held != null && held is CookingToolItem toolItem)
        {
            KitchenGameManager.Instance.ReturnToolItemToInventory(toolItem);
            return;
        }

        UIPopupManager.Instance.ShowPopup(UIPopupType.UIInventoryPopup);
        UIInventoryPopup popup = UIPopupManager.Instance.GetTopShownUIPopup() as UIInventoryPopup;
        popup?.FillInventory(InventoryTabType.Tool);
    }

    public void InteractAlternateEvent(PlayerStateMachine playerStateMachine)
    {
    }

    public void OnSelected()
    {
        Show();
    }

    public void OnDeselected()
    {
        Hide();
    }

    private void Show()
    {
        if (visualGameObjectArray == null)
            return;
        foreach (var visualGameObject in visualGameObjectArray)
        {
            if (visualGameObject == null || visualGameObject.sharedMaterials == null || visualGameObject.sharedMaterials.Length == 0)
                continue;
            Material[] mats = visualGameObject.materials;
            int index = mats.Length - 1;
            if (mats[index] != null)
                mats[index].SetFloat("_IsActive", 1f);
        }
    }

    private void Hide()
    {
        if (visualGameObjectArray == null)
            return;
        foreach (var visualGameObject in visualGameObjectArray)
        {
            if (visualGameObject == null || visualGameObject.sharedMaterials == null || visualGameObject.sharedMaterials.Length == 0)
                continue;
            Material[] mats = visualGameObject.materials;
            int index = mats.Length - 1;
            if (mats[index] != null)
                mats[index].SetFloat("_IsActive", 0f);
        }
    }
}
