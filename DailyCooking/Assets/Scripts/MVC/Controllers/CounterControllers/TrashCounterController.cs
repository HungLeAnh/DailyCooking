using System;
public class TrashCounterController : BaseCounterController
{
    protected override void Awake()
    {
        base.Awake();
        BaseCounterModel = new BaseCounterModel();
    }

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