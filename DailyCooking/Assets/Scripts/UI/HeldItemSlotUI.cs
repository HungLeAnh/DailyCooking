using UnityEngine;
using UnityEngine.UI;

// Close-up HUD slot showing what the local player holds; drag from it to put the item in the tool,
// drag food onto it to take the food out (CloseUpPointer).
public class HeldItemSlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;

    private KitchenObjectSO shown;

    private void Update()
    {
        PlayerStateMachine player = PlayerStateMachine.LocalInstance;
        KitchenObject held = player != null ? player.GetKitchenObject() : null;
        KitchenObjectSO so = held != null ? held.GetKitchenObjectSO() : null;
        if (so == shown)
            return;
        shown = so;
        if (icon == null)
            return;
        icon.enabled = so != null && so.Sprite != null;
        icon.sprite = so != null ? so.Sprite : null;
    }
}
