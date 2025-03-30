public class DeliveryCounterView : BaseCounterView
{
    public override object CreateControllerFromView()
    {
        return new DeliveryCounterController(this,new DeliveryCounterModel());
    }

}