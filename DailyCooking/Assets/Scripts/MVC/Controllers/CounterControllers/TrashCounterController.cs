using System;
[Serializable]
public class TrashCounterController : BaseCounterController
{
    public static event EventHandler OnAnyObjectTrashed;


    public TrashCounterController(TrashCounterView view,TrashCounterModel model) : base(view,model)
    {

    }

    public TrashCounterView TrashCounterView { get => (TrashCounterView)BaseCounterView;}

    new public static void ResetStaticData()
    {
        OnAnyObjectTrashed = null;
    }

    public override void Interact(PlayerStateMachine playerStateMachine)
    {
        if (playerStateMachine.HasKitchenObject())
        {
            playerStateMachine.GetKitchenObject().DestroySelf();

            OnAnyObjectTrashed?.Invoke(this, EventArgs.Empty);
        }
    }
}