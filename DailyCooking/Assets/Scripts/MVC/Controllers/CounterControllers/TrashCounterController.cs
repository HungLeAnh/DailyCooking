using System;
[Serializable]
public class TrashCounterController : BaseCounterController
{
    public static event EventHandler OnAnyObjectTrashed;

    private TrashCounterView _trashCounterView;

    public TrashCounterController(TrashCounterView view,TrashCounterModel model) : base(view,model)
    {
        _trashCounterView = view;
    }

    public TrashCounterView TrashCounterView { get => _trashCounterView;}

    public static void ResetStaticData()
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