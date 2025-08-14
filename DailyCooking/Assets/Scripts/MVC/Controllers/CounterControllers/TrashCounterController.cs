using System;
public class TrashCounterController : BaseCounterController
{
    public static event EventHandler OnAnyObjectTrashed;

    

    public static void ResetStaticData()
    {
        OnAnyObjectTrashed = null;
    }

    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        if (playerStateMachine.HasKitchenObject())
        {
            playerStateMachine.GetKitchenObject().DestroySelf();

            OnAnyObjectTrashed?.Invoke(this, EventArgs.Empty);
        }
    }
}