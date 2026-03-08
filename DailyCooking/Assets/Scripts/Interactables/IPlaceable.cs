using Unity.Netcode;

public interface IPlaceable
{
    public NetworkVariable<bool> IsPlaced { get; set; }
    public bool CanRemove();
}